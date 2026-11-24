using System.Reflection;
using System.Text.Json;
using RimBridgeServer.Sdk;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimgate.AssetProbe;

// Development companion only: resolve assets without changing gameplay or playing audio.
public sealed class AssetProbe
{
    [Tool("rimgate/asset_report", Description = "Resolve the staged Rimgate texture/audio manifest, static materials and selected gate command icons. Does not prove rendered fidelity or audible playback.")]
    public static Task<object> Report(IRimBridgeContext context) => context.MainThread.InvokeAsync<object>(() =>
    {
        var mod = LoadedModManager.RunningModsListForReading.Single(m => m.PackageId == "mrdav30.rimgate");
        if (VersionControl.CurrentVersion.Major != 1 || VersionControl.CurrentVersion.Minor != 7)
            throw new InvalidOperationException("The asset probe requires the experimental RimWorld 1.7 runtime.");

        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            Path.GetDirectoryName(typeof(AssetProbe).Assembly.Location)!, "asset-manifest.json")));
        var texturePaths = ReadPaths(manifest.RootElement.GetProperty("textures"));
        var soundPaths = ReadPaths(manifest.RootElement.GetProperty("sounds"));
        var bundles = mod.assetBundles.loadedAssetBundles;
        var bundleOwners = bundles.SelectMany(b => b.GetAllAssetNames()
            .Select(p => new KeyValuePair<string, AssetBundle>(p, b)))
            .ToDictionary(p => p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase);
        var missingBundleEntries = texturePaths.Select(p => $"assets/data/mrdav30.rimgate/textures/{p}.png")
            .Concat(soundPaths.Select(p => $"assets/data/mrdav30.rimgate/sounds/{p}.ogg"))
            .Where(p => !bundleOwners.ContainsKey(p)).ToArray();
        var missingTextures = new List<string>();
        var invalidTextures = new List<string>();
        var shadowedTextures = new List<string>();
        var textureSamples = new List<object>();
        foreach (var path in texturePaths)
        {
            var texture = LoadOwned<Texture2D>(bundleOwners, $"assets/data/mrdav30.rimgate/textures/{path}.png");
            if (texture == null || texture == BaseContent.BadTex)
                missingTextures.Add(path);
            else if (texture.width <= 0 || texture.height <= 0)
                invalidTextures.Add(path);
            else if (textureSamples.Count < 3 || path.Contains("Iris", StringComparison.OrdinalIgnoreCase)
                || path.Contains("Dwarfgate", StringComparison.OrdinalIgnoreCase)
                || path.Contains("DialHome", StringComparison.OrdinalIgnoreCase))
                textureSamples.Add(new { path, texture.name, texture.width, texture.height });
            if (texture != null && ContentFinder<Texture2D>.Get(path, false) != texture)
                shadowedTextures.Add(path);
        }

        var missingSounds = new List<string>();
        var invalidSounds = new List<string>();
        var shadowedSounds = new List<string>();
        var sounds = new List<object>();
        foreach (var path in soundPaths)
        {
            var clip = LoadOwned<AudioClip>(bundleOwners, $"assets/data/mrdav30.rimgate/sounds/{path}.ogg");
            if (clip == null)
                missingSounds.Add(path);
            else
            {
                if (!float.IsFinite(clip.length) || clip.length <= 0 || clip.samples <= 0
                    || clip.channels <= 0 || clip.frequency <= 0)
                    invalidSounds.Add(path);
                sounds.Add(new { path, clip.name, clip.length, clip.samples, clip.channels, clip.frequency });
            }
            if (clip != null && ContentFinder<AudioClip>.Get(path, false) != clip)
                shadowedSounds.Add(path);
        }

        var invalidStatics = new List<string>();
        var staticAssets = new List<object>();
        var textureType = mod.assemblies.loadedAssemblies.Select(a => a.GetType("Rimgate.RimgateTex"))
            .Single(t => t != null)!;
        foreach (var field in textureType.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.FieldType != typeof(Texture2D) && field.FieldType != typeof(Material))
                continue;
            var value = field.GetValue(null);
            var texture = value as Texture2D;
            var material = value as Material;
            bool valid = texture != null ? texture != BaseContent.BadTex && texture.width > 0 && texture.height > 0
                : material != null && material.shader != null && material.mainTexture != null
                    && material.mainTexture != BaseContent.BadTex;
            if (!valid)
                invalidStatics.Add(field.Name);
            staticAssets.Add(new { field = field.Name, valid, name = (value as UnityEngine.Object)?.name,
                shader = material?.shader?.name, texture = material?.mainTexture?.name });
        }

        var commands = new List<object>();
        var irisComps = new List<object>();
        var invalidCommands = new List<string>();
        var selectedGates = Current.ProgramState == ProgramState.Playing && Find.CurrentMap != null
            ? Find.Selector.SelectedObjects.OfType<ThingWithComps>()
                .Where(t => t.GetType().FullName == "Rimgate.Building_Gate").ToArray()
            : [];
        foreach (var gate in selectedGates)
        {
            foreach (var comp in gate.AllComps.Where(c => c.GetType().FullName == "Rimgate.Comp_ToggleIris"))
            {
                var type = comp.GetType();
                var on = type.GetProperty("CommandOnTex")!.GetValue(comp) as Texture2D;
                var off = type.GetProperty("CommandOffTex")!.GetValue(comp) as Texture2D;
                bool valid = on != null && on != BaseContent.BadTex && off != null && off != BaseContent.BadTex;
                if (!valid)
                    invalidCommands.Add("Comp_ToggleIris texture pair");
                irisComps.Add(new { gate = gate.ThingID, valid, on = on?.name, off = off?.name,
                    switchIsOn = type.GetProperty("SwitchIsOn")!.GetValue(comp),
                    hasIris = gate.GetType().GetField("HasIris")!.GetValue(gate) });
            }
            foreach (var command in gate.GetGizmos().OfType<Command>())
            {
                bool defaultDevIcon = Prefs.DevMode && command.icon == null
                    && (command.defaultLabel is "Add/remove iris" or "Force close");
                bool valid = defaultDevIcon || command.icon != null && command.icon != BaseContent.BadTex;
                if (!valid)
                    invalidCommands.Add(command.defaultLabel);
                commands.Add(new { gate = gate.ThingID, label = command.defaultLabel,
                    icon = command.icon?.name, iconIsNull = command.icon == null,
                    iconIsBadTexture = command.icon == BaseContent.BadTex, defaultDevIcon, valid });
            }
        }

        return new
        {
            success = missingTextures.Count + invalidTextures.Count + missingSounds.Count + invalidSounds.Count
                + invalidStatics.Count + invalidCommands.Count + missingBundleEntries.Length
                + shadowedTextures.Count + shadowedSounds.Count == 0,
            gameVersion = VersionControl.CurrentVersionStringWithRev,
            configPath = GenFilePaths.ModsConfigFilePath, modRoot = mod.RootDir,
            width = Screen.width, height = Screen.height,
            selectedGateCount = selectedGates.Length,
            textureCount = texturePaths.Length, soundCount = soundPaths.Length,
            bundles = bundles.Select(b => new { b.name, assetCount = b.GetAllAssetNames().Length }).ToArray(),
            missingBundleEntries,
            missingTextures, invalidTextures, shadowedTextures, missingSounds, invalidSounds, shadowedSounds,
            invalidStatics, invalidCommands,
            textureSamples, sounds, staticAssets, commands, irisComps,
            limits = "Resolution and metadata checks only; rendered effects, visual fidelity and audible playback require separate checks."
        };
    });

    private static T LoadOwned<T>(Dictionary<string, AssetBundle> owners, string path) where T : UnityEngine.Object
        => owners.TryGetValue(path, out var bundle) ? bundle.LoadAsset<T>(path) : null;

    private static string[] ReadPaths(JsonElement values)
    {
        var paths = values.EnumerateArray().Select(p => p.GetString()!).ToArray();
        if (paths.Length == 0 || paths.Any(p => string.IsNullOrWhiteSpace(p) || Path.IsPathRooted(p)
            || p.Contains('\\') || p.Split('/').Any(segment => segment is "" or "." or ".."))
            || paths.Distinct(StringComparer.OrdinalIgnoreCase).Count() != paths.Length)
            throw new InvalidDataException("Asset paths must be unique, nonempty relative content paths.");
        return paths;
    }
}
