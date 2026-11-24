using System.Diagnostics;
using System.Reflection;
using RimBridgeServer.Sdk;
using UnityEngine;
using Verse;
using Verse.Sound;
using Waylith.Audio;
using RimWorld;

namespace Rimgate.AssetProbe;

// Observe selected production voices, never the device/system mix or microphone.
public sealed class AudioProbe
{
    private static readonly FieldInfo SubSustainers = typeof(Sustainer).GetField("subSustainers", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly FieldInfo SustainerSamples = typeof(SubSustainer).GetField("samples", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly FieldInfo SourceMute = typeof(AudioSource).GetField("_mute", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static Capture current;

    [Tool("rimgate/begin_audio_probe", Description = "Start a bounded per-voice PCM probe for named Rimgate production SoundDefs. Temporarily mutes game master output and enables its effect gain; restores both automatically. Does not play sounds or capture microphone/system audio.")]
    public static Task<object> Begin(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot,
        string soundDefs, int maxDurationMs = 30000) => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        if (current != null && !current.Completed)
            throw new InvalidOperationException("Finish the existing audio probe first.");
        if (maxDurationMs < 1000 || maxDurationMs > 60000)
            throw new ArgumentOutOfRangeException(nameof(maxDurationMs), "Use 1000..60000 milliseconds.");
        var requested = soundDefs.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(StringComparer.Ordinal);
        if (requested.Count == 0 || requested.Count > 16)
            throw new ArgumentException("Request 1..16 exact Rimgate SoundDef names.", nameof(soundDefs));
        foreach (var name in requested)
            if (DefDatabase<SoundDef>.GetNamedSilentFail(name)?.modContentPack?.PackageId != "mrdav30.rimgate")
                throw new ArgumentException("Every requested SoundDef must be owned by Rimgate: " + name, nameof(soundDefs));
        if (AudioBackend.Current?.IsInitialized != true)
            throw new InvalidOperationException("The native audio backend must be initialized.");
        var capture = new Capture(requested, maxDurationMs);
        current = capture;
        try
        {
            // Pref setters apply master gain; mute first so enabling effect gain is inaudible.
            Prefs.VolumeMaster = 0;
            Prefs.VolumeGame = 1;
            capture.Worker = Task.Run(() => Monitor(context, capture));
        }
        catch
        {
            Prefs.VolumeGame = capture.PreviousGame;
            Prefs.VolumeMaster = capture.PreviousMaster;
            capture.Completed = true;
            throw;
        }
        return new { success = true, captureId = capture.Id, maxDurationMs,
            requestedDefs = requested.OrderBy(x => x).ToArray(),
            gameOutputMuted = Prefs.VolumeMaster == 0 && AudioListener.volume == 0,
            backend = AudioBackend.Current.Name, sampleRate = AudioBackend.Current.SampleRate,
            scope = "Only selected production sample voices; no playback is triggered by this tool." };
    });

    [Tool("rimgate/end_audio_probe", Description = "Stop the current per-voice probe, await preference restoration and report finite/nonzero output-window statistics. Does not capture or assert the final device mix or human audibility.")]
    public static async Task<object> End(IRimBridgeContext context)
    {
        var capture = current ?? throw new InvalidOperationException("Begin an audio probe first.");
        capture.Stop.Cancel();
        await capture.Worker;
        return await context.MainThread.InvokeAsync<object>(() => Report(capture));
    }

    [Tool("rimgate/audio_probe_status", Description = "Read bounded production-voice probe progress or its completed result. The probe automatically restores preferences when its duration expires.")]
    public static Task<object> Status(IRimBridgeContext context) => context.MainThread.InvokeAsync<object>(() =>
        current == null ? new { success = false, message = "No audio probe has been started." } : Report(current));

    private static async Task Monitor(IRimBridgeContext context, Capture capture)
    {
        try
        {
            while (capture.Clock.ElapsedMilliseconds < capture.DurationMs && !capture.Stop.IsCancellationRequested)
            {
                await context.MainThread.InvokeAsync(() => Read(capture));
                await Task.Delay(20, capture.Stop.Token);
            }
        }
        catch (OperationCanceledException) when (capture.Stop.IsCancellationRequested) { }
        catch (Exception error) { capture.Error = error.ToString(); }
        finally
        {
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try
            {
                await context.MainThread.InvokeAsync(() => Prefs.VolumeGame = capture.PreviousGame, cleanup.Token);
                // Let normal sample updates restore their context gains while master remains muted.
                await context.Game.FramesAsync(2, cleanup.Token);
            }
            catch (Exception error) { capture.Error += "\nEffect-gain cleanup: " + error.Message; }
            finally
            {
                using var masterCleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                try
                {
                    await context.MainThread.InvokeAsync(() =>
                    {
                        Prefs.VolumeGame = capture.PreviousGame;
                        Prefs.VolumeMaster = capture.PreviousMaster;
                        capture.Restored = Prefs.VolumeGame == capture.PreviousGame && Prefs.VolumeMaster == capture.PreviousMaster;
                    }, masterCleanup.Token);
                }
                catch (Exception error) { capture.Error += "\nMaster-gain cleanup: " + error.Message; }
                capture.Completed = true;
            }
        }
    }

    private static void Read(Capture capture)
    {
        if (!ReferenceEquals(Current.Game, capture.Game))
            throw new InvalidOperationException("The game changed during audio probing; start a new capture after loading.");
        if (Prefs.VolumeMaster != 0 || AudioListener.volume != 0)
            throw new InvalidOperationException("The probe lost its muted master-output guard.");
        var now = capture.Clock.ElapsedMilliseconds;
        if (capture.ReadCalls > 0) capture.MaximumReadGapMs = Math.Max(capture.MaximumReadGapMs, now - capture.LastReadMs);
        capture.LastReadMs = now;
        capture.ReadCalls++;
        if (Find.TickManager.Paused) capture.PausedReadCalls++;
        foreach (var sample in Samples())
        {
            var definition = sample.subDef?.parentDef;
            if (definition?.modContentPack?.PackageId != "mrdav30.rimgate"
                || !capture.Definitions.Contains(definition.defName) || sample.source == null || !sample.source.isPlaying)
                continue;
            if (!capture.Tracks.TryGetValue(sample, out var track))
            {
                if (capture.Tracks.Count >= 128) throw new InvalidOperationException("The bounded audio probe exceeded 128 production samples.");
                track = new Track(definition.defName, sample.source.clip?.name);
                capture.Tracks.Add(sample, track);
            }
            sample.source.GetOutputData(capture.Window, 0);
            double sumSquares = 0;
            int finite = 0;
            foreach (float value in capture.Window)
            {
                if (!float.IsFinite(value)) { track.InvalidSamples++; continue; }
                finite++;
                sumSquares += (double)value * value;
                track.Peak = Math.Max(track.Peak, Math.Abs(value));
            }
            track.Windows++;
            track.MapId = sample.Map?.uniqueID;
            track.CurrentMapId = Find.CurrentMap?.uniqueID;
            track.MuteWhenPaused = sample.subDef.muteWhenPaused;
            if (Find.TickManager.Paused) track.PausedWindows++;
            if (sample.Map != null && sample.Map != Find.CurrentMap) track.OtherMapWindows++;
            if ((bool)SourceMute.GetValue(sample.source)) track.MutedWindows++;
            track.SamplesExamined += capture.Window.Length;
            var rms = finite > 0 ? Math.Sqrt(sumSquares / finite) : 0;
            track.MaximumWindowRms = Math.Max(track.MaximumWindowRms, rms);
            if (rms > 0.000001) track.NonzeroWindows++;
            track.MaximumSourceGain = Math.Max(track.MaximumSourceGain, sample.source.volume);
        }
    }

    private static IEnumerable<Sample> Samples()
    {
        foreach (var sample in Find.SoundRoot.oneShotManager.PlayingOneShots)
            yield return sample;
        foreach (var sustainer in Find.SoundRoot.sustainerManager.AllSustainers)
            foreach (var sub in (IEnumerable<SubSustainer>)SubSustainers.GetValue(sustainer) ?? [])
                foreach (var sample in (IEnumerable<SampleSustainer>)SustainerSamples.GetValue(sub) ?? [])
                    yield return sample;
    }

    private static object Report(Capture capture)
    {
        var missing = capture.Definitions.Where(name => !capture.Tracks.Values.Any(t => t.Definition == name && t.NonzeroWindows > 0))
            .OrderBy(x => x).ToArray();
        return new
        {
            success = capture.Completed && capture.Restored && capture.Error == null
                && missing.Length == 0 && capture.Tracks.Values.All(t => t.InvalidSamples == 0),
            captureId = capture.Id, completed = capture.Completed, preferencesRestored = capture.Restored,
            elapsedMs = capture.Clock.ElapsedMilliseconds, missingDefs = missing, error = capture.Error,
            readCalls = capture.ReadCalls, pausedReadCalls = capture.PausedReadCalls,
            maximumReadGapMs = capture.MaximumReadGapMs,
            requestedDefs = capture.Definitions.OrderBy(x => x).ToArray(),
            tracks = capture.Tracks.Values.Select(t => new { soundDef = t.Definition, clip = t.Clip,
                windows = t.Windows, nonzeroWindows = t.NonzeroWindows, samplesExamined = t.SamplesExamined,
                invalidSamples = t.InvalidSamples, peak = t.Peak, maximumWindowRms = t.MaximumWindowRms,
                maximumSourceGain = t.MaximumSourceGain, mapId = t.MapId, currentMapId = t.CurrentMapId,
                muteWhenPaused = t.MuteWhenPaused, pausedWindows = t.PausedWindows,
                otherMapWindows = t.OtherMapWindows, mutedWindows = t.MutedWindows }).ToArray(),
            previousGameGain = capture.PreviousGame, previousMasterGain = capture.PreviousMaster,
            captureMasterGain = 0, sampleRate = AudioBackend.Current.SampleRate, capturedChannel = 0,
            windowSamples = capture.Window.Length, sampleWindowsMayOverlap = true,
            limits = "Per-production-voice engine output tap only, with master output muted. Rolling windows may overlap. Not a continuous recording, final device mix, speaker test or human audible-fidelity check. No system/microphone audio was captured."
        };
    }

    private static void Guard(string expectedConfigPath, string expectedModRoot)
    {
        var mod = LoadedModManager.RunningModsListForReading.Single(m => m.PackageId == "mrdav30.rimgate");
        if (VersionControl.CurrentVersion.Major != 1 || VersionControl.CurrentVersion.Minor != 7
            || !Prefs.DevMode || Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null
            || Path.GetFileName(GenFilePaths.SaveDataFolderPath) != "Profile-1.7"
            || !Path.GetFullPath(GenFilePaths.ModsConfigFilePath).Equals(Path.GetFullPath(expectedConfigPath), StringComparison.OrdinalIgnoreCase)
            || !Path.GetFullPath(mod.RootDir).Equals(Path.GetFullPath(expectedModRoot), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Refusing an audio probe outside the expected isolated 1.7 game/profile/deployment.");
    }

    private sealed class Capture(HashSet<string> definitions, int durationMs)
    {
        public readonly string Id = Guid.NewGuid().ToString("N");
        public readonly Game Game = Current.Game;
        public readonly float PreviousGame = Prefs.VolumeGame;
        public readonly float PreviousMaster = Prefs.VolumeMaster;
        public readonly HashSet<string> Definitions = definitions;
        public readonly int DurationMs = durationMs;
        public readonly Stopwatch Clock = Stopwatch.StartNew();
        public readonly CancellationTokenSource Stop = new();
        public readonly Dictionary<Sample, Track> Tracks = new(ReferenceEqualityComparer.Instance);
        public readonly float[] Window = new float[16384];
        public Task Worker;
        public volatile bool Completed;
        public bool Restored;
        public string Error;
        public int ReadCalls;
        public int PausedReadCalls;
        public long LastReadMs;
        public long MaximumReadGapMs;
    }

    private sealed class Track(string definition, string clip)
    {
        public readonly string Definition = definition;
        public readonly string Clip = clip;
        public int Windows;
        public int NonzeroWindows;
        public long SamplesExamined;
        public long InvalidSamples;
        public double Peak;
        public double MaximumWindowRms;
        public float MaximumSourceGain;
        public int? MapId;
        public int? CurrentMapId;
        public bool MuteWhenPaused;
        public int PausedWindows;
        public int OtherMapWindows;
        public int MutedWindows;
    }
}
