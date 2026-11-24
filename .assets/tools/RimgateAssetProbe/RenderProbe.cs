using System.Reflection;
using System.Diagnostics;
using RimBridgeServer.Sdk;
using RimWorld;
using UnityEngine;
using UnityStubs.Rendering;
using Verse;
using Waylith;
using Waylith.Windowing;
using Time = UnityEngine.Time;

namespace Rimgate.AssetProbe;

// Private-profile diagnostics and a bounded fixture; the redraw hook is separate below.
public sealed class RenderProbe
{
    private static readonly FieldInfo PendingFiles = typeof(ScreenCapture).GetField("PendingFiles", BindingFlags.Static | BindingFlags.NonPublic)!;
    private static OcclusionFixture fixture;

    // The SDK constructs providers with instance tools during discovery, before any UI calls.
    public RenderProbe() => BackgroundRendering.Initialize();

    [Tool("rimgate/start_render_occlusion_fixture", Description = "Temporarily report the owned hidden window as occluded inside the engine, optionally suppressing the companion redraw fix for a negative control. Does not minimize, lock, show or focus any OS window. Automatically resynchronizes with actual OS state within 60 seconds.")]
    public static Task<object> StartFixture(IRimBridgeContext context, string expectedConfigPath,
        string expectedModRoot, bool suppressRedraw = false, int durationMs = 20000)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);
        var app = EngineApplication.Instance;
        if (!BackgroundRendering.Installed || !app.Options.HideWindow || app.Options.Headless
            || app.Window.GetType().FullName != "Waylith.Windowing.Backends.Win32.Win32Window")
            throw new InvalidOperationException("Use the installed hidden Windows development companion.");
        if (fixture != null && !fixture.Completed)
            throw new InvalidOperationException("End the existing render fixture first.");
        if (durationMs is < 1000 or > 60000)
            throw new ArgumentOutOfRangeException(nameof(durationMs), "Use 1000..60000 milliseconds.");
        var window = app.Window;
        var report = Method(window, "ReportOcclusion");
        var refresh = Method(window, "UpdateOcclusion");
        var next = new OcclusionFixture(window, refresh, durationMs);
        fixture = next;
        try
        {
            BackgroundRendering.Suppressed = suppressRedraw;
            report.Invoke(window, [true]);
            next.Worker = Task.Run(() => RestoreAfterDelay(context, next));
        }
        catch
        {
            BackgroundRendering.Suppressed = next.PreviousSuppression;
            refresh.Invoke(window, null);
            next.Completed = true;
            throw;
        }
        return FixtureStatus(next);
    });

    [Tool("rimgate/end_render_occlusion_fixture", Description = "End the temporary engine-occlusion fixture and resynchronize the owned window from actual OS state. Does not change OS visibility, focus, session lock or display power.")]
    public static async Task<object> EndFixture(IRimBridgeContext context)
    {
        var current = fixture ?? throw new InvalidOperationException("Start an occlusion fixture first.");
        current.Stop.Cancel();
        await current.Worker;
        return await context.MainThread.InvokeAsync<object>(() => FixtureStatus(current));
    }

    [Tool("rimgate/render_capture_status", Description = "Read private-profile screenshot queue, native capture flags and render extent after a capture timeout. Does not request capture, read pixels or alter rendering state.")]
    public Task<object> Status(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        Guard(expectedConfigPath, expectedModRoot);

        var backend = ScreenCaptureBackend.Current;
        var backendType = backend?.GetType();
        var expectedBackend = backendType?.FullName == "RimWorldWaylith.Rendering.ScreenCaptureFeature";
        var pending = ((List<string>)PendingFiles.GetValue(null)).ToArray();
        var app = expectedBackend ? Field(backend, "_app") : null;
        var renderContext = app?.GetType().GetProperty("RenderContext")?.GetValue(app);
        var extent = renderContext == null ? null : Field(renderContext, "Extent");
        var width = extent == null ? (int?)null : (int)Field(extent, "Width");
        var height = extent == null ? (int?)null : (int)Field(extent, "Height");
        var screen = expectedBackend ? Field(backend, "_screen") as RenderTexture : null;
        var armed = expectedBackend ? Field(backend, "_armed") : null;
        var readable = expectedBackend ? Field(backend, "_readable") : null;
        var window = app == null ? null : Property(app, "Window");
        var options = app == null ? null : Property(app, "Options");
        var platform = window == null ? null : Property(window, "Platform");
        var surfaceSize = window == null ? null : Property(window, "SurfaceSize");
        var hasSurface = window != null && Property(window, "Surface") != null;
        var surfaceIsZero = surfaceSize == null || (bool)Property(surfaceSize, "IsZero");
        var shouldPresent = window != null && (bool)Property(window, "ShouldPresent");
        var redrawRequested = app != null && (bool)Field(app, "_redrawRequested");
        var win32 = platform?.GetType().FullName == "Waylith.Windowing.Backends.Win32.Win32Platform";
        return new
        {
            success = true, frame = Time.frameCount, currentMapId = Find.CurrentMap?.uniqueID,
            backendType = backendType?.FullName, expectedBackend,
            canCapture = expectedBackend ? (bool?)backendType.GetProperty("CanCapture", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(backend) : null,
            pendingFileCount = pending.Length, pendingFiles = pending,
            captureRequested = expectedBackend ? (bool?)Field(backend, "_captureRequested") : null,
            armedPresent = armed != null, armedRecorded = armed == null ? (bool?)null : (bool)Field(armed, "Recorded"),
            readablePresent = readable != null, readableRecorded = readable == null ? (bool?)null : (bool)Field(readable, "Recorded"),
            captureTextureWidth = screen?.width, captureTextureHeight = screen?.height,
            renderExtentWidth = width, renderExtentHeight = height,
            captureTextureMatchesExtent = screen == null || width == null ? (bool?)null : screen.width == width && screen.height == height,
            screenWidth = Screen.width, screenHeight = Screen.height,
            backgroundRendering = new { BackgroundRendering.Installed, BackgroundRendering.Suppressed,
                BackgroundRendering.RedrawRequests, BackgroundRendering.LastRequestFrame },
            occlusionFixture = fixture == null ? null : FixtureStatus(fixture),
            renderLoop = new
            {
                windowType = window?.GetType().FullName, platformType = platform?.GetType().FullName,
                headless = options == null ? (bool?)null : (bool)Property(options, "Headless"),
                hideWindow = options == null ? (bool?)null : (bool)Property(options, "HideWindow"),
                shouldPresent, redrawRequested, hasSurface, surfaceIsZero,
                surfaceWidth = surfaceSize == null ? (int?)null : (int)Property(surfaceSize, "Width"),
                surfaceHeight = surfaceSize == null ? (int?)null : (int)Property(surfaceSize, "Height"),
                renderWanted = shouldPresent || (redrawRequested && hasSurface && !surfaceIsZero),
                visible = window == null ? (bool?)null : (bool)Property(window, "IsVisible"),
                occluded = window == null ? (bool?)null : (bool)Property(window, "IsOccluded"),
                focused = window == null ? (bool?)null : (bool)Property(window, "IsFocused"),
                windowState = window == null ? null : Property(window, "State").ToString(),
                noRenderSince = app == null ? null : Field(app, "_noRenderSince"),
                mainResizeSession = app == null ? (bool?)null : (bool)Field(app, "_mainResizeSession"),
                audioOutputSuspended = app == null ? (bool?)null : (bool)Field(app, "_audioOutputSuspended"),
                sessionLocked = win32 ? (bool?)Property(platform, "SessionLocked") : null,
                displayOff = win32 ? (bool?)Property(platform, "DisplayOff") : null
            },
            limits = "Read-only instant outside end-of-frame. A null readable/armed capture is normally transient. Pending files localize a timeout before PNG encoding; no pending files also permits a rejected/unavailable capture request. No pixels or files were read or written."
        };
    });

    private static void Guard(string expectedConfigPath, string expectedModRoot)
    {
        var mod = LoadedModManager.RunningModsListForReading.Single(m => m.PackageId == "mrdav30.rimgate");
        if (VersionControl.CurrentVersion.Major != 1 || VersionControl.CurrentVersion.Minor != 7
            || !Prefs.DevMode || Current.ProgramState != ProgramState.Playing
            || Path.GetFileName(GenFilePaths.SaveDataFolderPath) != "Profile-1.7"
            || !Path.GetFullPath(GenFilePaths.ModsConfigFilePath).Equals(Path.GetFullPath(expectedConfigPath), StringComparison.OrdinalIgnoreCase)
            || !Path.GetFullPath(mod.RootDir).Equals(Path.GetFullPath(expectedModRoot), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Use the expected private 1.7 profile/deployment.");
    }

    private static async Task RestoreAfterDelay(IRimBridgeContext context, OcclusionFixture current)
    {
        try { await Task.Delay(current.DurationMs, current.Stop.Token); }
        catch (OperationCanceledException) when (current.Stop.IsCancellationRequested) { }
        finally
        {
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try
            {
                await context.MainThread.InvokeAsync(() =>
                {
                    // Re-read current OS conditions instead of restoring a stale simulated flag.
                    try { current.Refresh.Invoke(current.Window, null); }
                    finally { BackgroundRendering.Suppressed = current.PreviousSuppression; }
                    current.Restored = true;
                }, cleanup.Token);
            }
            catch (Exception error) { current.Error = error.ToString(); }
            finally
            {
                // A stalled dispatcher must not leave the automatic redraw hook disabled.
                BackgroundRendering.Suppressed = current.PreviousSuppression;
                current.Completed = true;
            }
        }
    }

    private static object FixtureStatus(OcclusionFixture current) => new
    {
        success = current.Error == null && (!current.Completed || current.Restored),
        elapsedMs = current.Clock.ElapsedMilliseconds, durationMs = current.DurationMs,
        completed = current.Completed, restored = current.Restored, error = current.Error,
        occluded = current.Window.IsOccluded, shouldPresent = current.Window.ShouldPresent,
        redrawSuppressed = BackgroundRendering.Suppressed,
        limits = "Engine-window test flag only. No OS session/display/visibility/focus was changed."
    };

    private sealed class OcclusionFixture(IWindow window, MethodInfo refresh, int durationMs)
    {
        public readonly IWindow Window = window;
        public readonly MethodInfo Refresh = refresh;
        public readonly int DurationMs = durationMs;
        public readonly bool PreviousSuppression = BackgroundRendering.Suppressed;
        public readonly CancellationTokenSource Stop = new();
        public readonly Stopwatch Clock = Stopwatch.StartNew();
        public Task Worker;
        public volatile bool Completed;
        public bool Restored;
        public string Error;
    }

    private static MethodInfo Method(object owner, string name)
    {
        for (var type = owner.GetType(); type != null; type = type.BaseType)
            if (type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) is { } method)
                return method;
        throw new MissingMethodException(owner.GetType().FullName, name);
    }

    private static object Field(object owner, string name)
    {
        for (var type = owner.GetType(); type != null; type = type.BaseType)
            if (type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) is { } field)
                return field.GetValue(owner);
        throw new MissingFieldException(owner.GetType().FullName, name);
    }

    private static object Property(object owner, string name) => owner.GetType()
        .GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(owner);
}

internal static class BackgroundRendering
{
    internal static bool Installed;
    internal static volatile bool Suppressed;
    internal static long RedrawRequests;
    internal static int LastRequestFrame;
    private static string profilePath;

    internal static void Initialize()
    {
        if (Installed) return;
        var app = EngineApplication.Instance;
        if (VersionControl.CurrentVersion.Major != 1 || VersionControl.CurrentVersion.Minor != 7
            || !app.Options.HideWindow || app.Options.Headless
            || Path.GetFileName(GenFilePaths.SaveDataFolderPath) != "Profile-1.7")
            return;
        profilePath = GenFilePaths.SaveDataFolderPath;
        app.PrePollEvents += RequestHiddenFrame;
        Installed = true;
        Log.Message("[Rimgate dev] Hidden background redraw hook initialized.");
    }

    private static void RequestHiddenFrame(EngineApplication app)
    {
        if (Suppressed || !Prefs.DevMode || !app.Options.HideWindow || app.Options.Headless
            || !GenFilePaths.SaveDataFolderPath.Equals(profilePath, StringComparison.OrdinalIgnoreCase))
            return;
        var window = app.Window;
        // Updates keep running while occluded, but GUI gestures and captures need rendered frames.
        if (window != null && !window.ShouldPresent && window.Surface != null && !window.SurfaceSize.IsZero)
        {
            window.RequestRedraw();
            RedrawRequests++;
            LastRequestFrame = Time.frameCount;
        }
    }
}
