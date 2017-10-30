using UnityEngine;
using UnityEditor;
using System.Collections;

public class AssetbundlesMenuItems
{
	const string kSimulateAssetBundlesMenu = "AssetBundles/Simulate AssetBundles";
    const string kCompressionMenu = "AssetBundles/LZ4 Compression";

	[MenuItem(kSimulateAssetBundlesMenu)]
	public static void ToggleSimulateAssetBundle ()
	{
		AssetBundleAdapter.SimulateAssetBundleInEditor = !AssetBundleAdapter.SimulateAssetBundleInEditor;
	}

	[MenuItem(kSimulateAssetBundlesMenu, true)]
	public static bool ToggleSimulateAssetBundleValidate ()
	{
		Menu.SetChecked(kSimulateAssetBundlesMenu, AssetBundleAdapter.SimulateAssetBundleInEditor);
		return true;
	}

    [MenuItem ("AssetBundles/Build Player")]
	static void BuildPlayer ()
	{
		BuildScript.BuildPlayer();
	}

    [MenuItem("AssetBundles/Build AssetBundles/LZMA", false, 1)]
    static public void BuildForLZMACompression()
    {
        BuildScript.BuildAssetBundles(BuildScript.CompressionType.LZMA);
    }

    [MenuItem("AssetBundles/Build AssetBundles/LZ4", false, 2)]
    static public void BuildForLZ4Compression()
    {
        BuildScript.BuildAssetBundles(BuildScript.CompressionType.LZ4);
    }

    [MenuItem("AssetBundles/Build AssetBundles/UnCompress", false, 3)]
    static public void BuildForUnCompression()
    {
        BuildScript.BuildAssetBundles(BuildScript.CompressionType.Uncompress);
    }
}
