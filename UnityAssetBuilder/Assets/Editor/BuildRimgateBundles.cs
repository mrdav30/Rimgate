using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class BuildRimgateBundles
{
    private const string BundleName = "rimgate_core";
    private const string AssetsPath = "Assets/Data/Mrdav30.Rimgate/";

    public static void BuildBundlesFromCommandLine()
    {
        string[] arguments = Environment.GetCommandLineArgs();
        int versionIndex = Array.IndexOf(arguments, "-rimgateVersion");
        if (versionIndex < 0 || versionIndex + 1 >= arguments.Length || arguments[versionIndex + 1].StartsWith("-"))
            throw new ArgumentException("Supply -rimgateVersion <major.minor> to select the bundle target.");
        BuildBundles(arguments[versionIndex + 1]);
    }

    public static void BuildBundles(string rimWorldVersion)
    {
        if (string.IsNullOrEmpty(rimWorldVersion) || !Regex.IsMatch(rimWorldVersion, @"^\d+\.\d+$"))
            throw new ArgumentException("The RimWorld version must have the form major.minor.", nameof(rimWorldVersion));

        string versionPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Mod", rimWorldVersion));
        if (!Directory.Exists(versionPath))
            throw new InvalidOperationException($"No content folder exists for RimWorld {rimWorldVersion}: {versionPath}");
        string outputPath = Path.Combine(versionPath, "AssetBundles");
        Directory.CreateDirectory(outputPath);

        List<AssetBundleBuild> assetBundleDefinitionList = new();
        AssetBundleBuild ab = new();
        ab.assetBundleName = BundleName;
        ab.assetNames = RecursiveGetAllAssetsInDirectory(AssetsPath).ToArray();
        assetBundleDefinitionList.Add(ab);

        var targetPlatform = BuildTarget.StandaloneWindows64;
        BuildAssetBundlesParameters buildParameters = new()
        {
            outputPath = outputPath,
            bundleDefinitions = assetBundleDefinitionList.ToArray(),
            options = BuildAssetBundleOptions.ChunkBasedCompression,
            targetPlatform = targetPlatform
        };
        if (BuildPipeline.BuildAssetBundles(buildParameters) == null)
            throw new InvalidOperationException("Rimgate AssetBundle build did not produce a manifest.");

        Debug.Log($"Rimgate AssetBundle build complete for RimWorld {rimWorldVersion}: {outputPath}");
    }

    private static List<string> RecursiveGetAllAssetsInDirectory(string path)
    {
        List<string> assets = new();
        foreach (var f in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
        {
            // Only include .png and .ogg files
            if (Path.GetExtension(f) != ".png" && Path.GetExtension(f) != ".ogg") continue;
            assets.Add(f);
        }

        return assets;
    }
}
