using System.Collections;
using System.Diagnostics;
using System.Reflection;
using RimBridgeServer.Sdk;
using RimWorld;
using Verse;

namespace Rimgate.AssetProbe;

// Observe the actual compatibility prefix; never modify its args/result or invoke it.
public sealed class AlphaClinicalProbe
{
    private const string Owner = "Rimgate.AssetProbe.AlphaClinicalWatch";
    private static readonly MethodInfo Target = typeof(AlphaGenesCompatibility).GetMethod(nameof(AlphaGenesCompatibility.SkipIfPawnIsOnSarcophagus))!;
    private static readonly MethodInfo Observer = typeof(AlphaClinicalProbe).GetMethod(nameof(Observe), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static Session current;
    private sealed class Session
    {
        public Game Game;
        public string PawnId;
        public object Harmony;
        public int DurationMs;
        public readonly Stopwatch Clock = Stopwatch.StartNew();
        public readonly List<object> Calls = [];
        public bool Ended;
        public bool SameGameWhenEnded;
        public string Error;
    }

    [Tool("rimgate/begin_alpha_clinical_watch", Description = "Observe actual calls/returned values of Rimgate's Alpha Genes sarcophagus guard for one private patient and Current.Game, up to16 calls/60seconds. Passive postfix only: does not invoke the guard or change args/results/RNG/outcomes. Attempts automatic owned unpatch; host must End in finally and verify actual observer removal.")]
    public static Task<object> Begin(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot,
        string patientId, int durationMs = 60000) => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        if (durationMs is < 1000 or > 60000) throw new ArgumentOutOfRangeException(nameof(durationMs));
        if (!LoadedModManager.RunningModsListForReading.Any(m => m.PackageId == "sarg.alphagenes"))
            throw new InvalidOperationException("The actual Alpha Genes integration must be loaded.");
        var patient = Find.Maps.SelectMany(m => m.listerThings.AllThings.OfType<Pawn>())
            .Concat(Find.Maps.SelectMany(m => m.listerThings.AllThings.OfType<Building_Sarcophagus>()).Select(s => s.PatientPawn))
            .Single(p => p?.GetUniqueLoadID() == patientId);
        if (patient.Dead || patient.DevelopmentalStage != DevelopmentalStage.Adult
            || patient.genes?.GenesListForReading.Any(g => g.def.defName == "AG_BrittleBones" && g.Active) != true)
            throw new InvalidOperationException("Observe a living adult with the actual active brittle-bones gene.");
        if (current is { Ended: false }) Finish(current);
        var harmonyType = typeof(RimgateMod).GetField("harmony")!.GetValue(null)!.GetType();
        var session = new Session { Game = Current.Game, PawnId = patientId, DurationMs = durationMs,
            Harmony = Activator.CreateInstance(harmonyType, [Owner])! };
        current = session;
        try
        {
            var hm = Activator.CreateInstance(harmonyType.Assembly.GetType("HarmonyLib.HarmonyMethod")!, [Observer]);
            harmonyType.GetMethod("Patch")!.Invoke(session.Harmony, [Target, null, hm, null, null]);
            if (!Registered(session)) throw new InvalidOperationException("The exact owned passive observer was not registered.");
            _ = Expire(context, session);
            return Snapshot(session);
        }
        catch { Finish(session); throw; }
    });

    [Tool("rimgate/alpha_clinical_watch_status", Description = "Read bounded actual guard calls, patient/holder IDs, deep ownership and original returned bool. Zero observations do not prove suppression; no guard is invoked by this tool.")]
    public static Task<object> Status(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot)
        => context.MainThread.InvokeAsync<object>(() => { Guard(expectedConfigPath, expectedModRoot); return current == null
            ? new { success = false, message = "No watch started." } : Snapshot(current); });

    [Tool("rimgate/end_alpha_clinical_watch", Description = "Unpatch only this companion's passive guard observer and return retained evidence. Does not unpatch the actual Rimgate or Alpha Genes integration.")]
    public static Task<object> End(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot)
        => context.MainThread.InvokeAsync<object>(() => { Guard(expectedConfigPath, expectedModRoot);
            if (current == null) return new { success = false, message = "No watch started." };
            Finish(current); return Snapshot(current); });

    private static void Observe(Pawn __0, bool __result)
    {
        var s = current;
        if (s == null || s.Ended || s.Clock.ElapsedMilliseconds >= s.DurationMs || !ReferenceEquals(Current.Game, s.Game)
            || __0?.GetUniqueLoadID() != s.PawnId || s.Calls.Count >= 16) return;
        try
        {
            var holder = __0.ParentHolder;
            s.Calls.Add(new { tick = Find.TickManager.TicksGame, patientId = s.PawnId, returned = __result,
                holderType = holder?.GetType().FullName, holderId = (holder as Thing)?.GetUniqueLoadID(),
                ownedBySarcophagus = holder is Building_Sarcophagus sarc && sarc.GetDirectlyHeldThings().Any(t => ReferenceEquals(t, __0)) });
        }
        catch (Exception error) { s.Error = error.Message; }
    }
    private static bool Registered(Session s)
    {
        var info = s.Harmony.GetType().GetMethod("GetPatchInfo", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, [Target]);
        return info != null && ((IEnumerable)info.GetType().GetField("Postfixes")!.GetValue(info)!).Cast<object>().Any(p =>
            (string)p.GetType().GetField("owner")!.GetValue(p)! == Owner
            && Equals(p.GetType().GetProperty("PatchMethod")!.GetValue(p), Observer));
    }
    private static object Snapshot(Session s)
    {
        bool registered = Registered(s);
        return new { success = s.Error == null, patientId = s.PawnId, observerRegistered = registered,
            sameGame = s.Ended ? s.SameGameWhenEnded : ReferenceEquals(Current.Game, s.Game),
            active = registered && !s.Ended && ReferenceEquals(Current.Game, s.Game) && s.Clock.ElapsedMilliseconds < s.DurationMs,
            unpatched = s.Ended && !registered, elapsedMs = s.Clock.ElapsedMilliseconds,
            observedCount = s.Calls.Count, calls = s.Calls.ToArray(), error = s.Error,
            limits = "Only calls observed for the bound Game before the deadline count. Require exact patient/holder IDs, deep sarc ownership and returned=false. Zero qualifying calls or calls outside that scope cannot pass. A qualifying retained call remains valid after normal End/expiry and active=false. Host must End in finally and verify observerRegistered=false/unpatched=true; expiry cleanup may fail or time out." };
    }
    private static void Finish(Session s)
    {
        if (s.Ended) return;
        s.Harmony.GetType().GetMethod("Unpatch", [typeof(MethodBase), typeof(MethodInfo)])!.Invoke(s.Harmony, [Target, Observer]);
        if (Registered(s)) { s.Error = "The owned observer remains registered after unpatch."; return; }
        s.SameGameWhenEnded = ReferenceEquals(Current.Game, s.Game);
        s.Game = null;
        s.Ended = true;
    }
    private static async Task Expire(IRimBridgeContext context, Session s)
    {
        await Task.Delay(s.DurationMs);
        using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try { await context.MainThread.InvokeAsync(() => Finish(s), cleanup.Token); }
        catch (Exception error) { s.Error = error.Message; }
    }
    private static void Guard(string expectedConfigPath, string expectedModRoot)
    {
        var mod = LoadedModManager.RunningModsListForReading.Single(m => m.PackageId == "mrdav30.rimgate");
        if (!Prefs.DevMode || Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null
            || VersionControl.CurrentVersion.Major != 1 || VersionControl.CurrentVersion.Minor != 7
            || Path.GetFileName(GenFilePaths.SaveDataFolderPath) != "Profile-1.7"
            || !Path.GetFullPath(GenFilePaths.ModsConfigFilePath).Equals(Path.GetFullPath(expectedConfigPath), StringComparison.OrdinalIgnoreCase)
            || !Path.GetFullPath(mod.RootDir).Equals(Path.GetFullPath(expectedModRoot), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Use the expected isolated 1.7 game/profile/deployment.");
    }
}
