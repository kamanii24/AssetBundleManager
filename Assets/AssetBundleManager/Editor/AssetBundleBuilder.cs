using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class AssetBundleBuilder
{
    private const string kAssetBundlesOutputPath = "AssetBundles";

    [MenuItem("AssetBundles/Build/LZMA", false, 1)]
    public static void BuildForLZMA()
    {
        BuildAssetBundles();
    }

    [MenuItem("AssetBundles/Build/LZ4", false, 2)]
    public static void BuildForLZ4()
    {
        BuildAssetBundles(BuildAssetBundleOptions.ChunkBasedCompression);
    }

    [MenuItem("AssetBundles/Build/Uncompressed", false, 3)]
    public static void BuildForUncompression()
    {
        BuildAssetBundles(BuildAssetBundleOptions.UncompressedAssetBundle);
    }


    public static void BuildAssetBundles(BuildAssetBundleOptions buildOptions = BuildAssetBundleOptions.None)
    {
        string outputPath = Path.Combine(kAssetBundlesOutputPath, GetPlatformFolderForAssetBundles(EditorUserBuildSettings.activeBuildTarget));
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        BuildPipeline.BuildAssetBundles(outputPath, buildOptions, EditorUserBuildSettings.activeBuildTarget);
    }


#if UNITY_EDITOR
    public static string GetPlatformFolderForAssetBundles(BuildTarget target)
    {
        switch (target)
        {
            case BuildTarget.Android:
                return "Android";
            case BuildTarget.iOS:
                return "iOS";
            case BuildTarget.StandaloneWindows:
                return "Windows";
            case BuildTarget.StandaloneOSX:
                return "OSX";
            default:
                return "Unknown";
        }
    }
#endif
}