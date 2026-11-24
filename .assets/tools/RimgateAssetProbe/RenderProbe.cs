using System.Reflection;
using RimBridgeServer.Sdk;
using RimWorld;
using UnityEngine;
using UnityStubs.Rendering;
using Verse;

namespace Rimgate.AssetProbe;

// One-shot diagnosis of the installed 1.7 screenshot queue; never requests or reads pixels.
public sealed class RenderProbe
{
    private static readonly FieldInfo PendingFiles = typeof(ScreenCapture).GetField("PendingFiles", BindingFlags.Static | BindingFlags.NonPublic)!;

    [Tool("rimgate/render_capture_status", Description = "Read private-profile screenshot queue, native capture flags and render extent after a capture timeout. Does not request capture, read pixels or alter rendering state.")]
    public static Task<object> Status(IRimBridgeContext context, string expectedConfigPath, string expectedModRoot)
        => context.MainThread.InvokeAsync<object>(() =>
    {
        var mod = LoadedModManager.RunningModsListForReading.Single(m => m.PackageId == "mrdav30.rimgate");
        if (VersionControl.CurrentVersion.Major != 1 || VersionControl.CurrentVersion.Minor != 7
            || !Prefs.DevMode || Current.ProgramState != ProgramState.Playing
            || Path.GetFileName(GenFilePaths.SaveDataFolderPath) != "Profile-1.7"
            || !Path.GetFullPath(GenFilePaths.ModsConfigFilePath).Equals(Path.GetFullPath(expectedConfigPath), StringComparison.OrdinalIgnoreCase)
            || !Path.GetFullPath(mod.RootDir).Equals(Path.GetFullPath(expectedModRoot), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Use the expected private 1.7 profile/deployment.");

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
            limits = "Read-only instant outside end-of-frame. A null readable/armed capture is normally transient. Pending files localize a timeout before PNG encoding; no pending files also permits a rejected/unavailable capture request. No pixels or files were read or written."
        };
    });

    private static object Field(object owner, string name) => owner.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(owner);
}
