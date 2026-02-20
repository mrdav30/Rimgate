using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BuildRimgateBundles
{
    private const string BundleName = "rimgate_core";
    private const string AssetsPath = "Assets/Data/Mrdav30.Rimgate/";
    private const string OutputPath = "../Mod/AssetBundles";

    public static void BuildBundles()
    {
        if (!Directory.Exists(OutputPath))
            Directory.CreateDirectory(OutputPath);

        List<AssetBundleBuild> assetBundleDefinitionList = new();
        AssetBundleBuild ab = new();
        ab.assetBundleName = BundleName;
        ab.assetNames = RecursiveGetAllAssetsInDirectory(AssetsPath).ToArray();
        assetBundleDefinitionList.Add(ab);

        var targetPlatform = BuildTarget.StandaloneWindows64;
        BuildAssetBundlesParameters buildParameters = new()
        {
            outputPath = OutputPath,
            bundleDefinitions = assetBundleDefinitionList.ToArray(),
            options = BuildAssetBundleOptions.ChunkBasedCompression,
            targetPlatform = targetPlatform
        };
        BuildPipeline.BuildAssetBundles(buildParameters);

        Debug.Log("Rimgate AssetBundle build complete.");
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
