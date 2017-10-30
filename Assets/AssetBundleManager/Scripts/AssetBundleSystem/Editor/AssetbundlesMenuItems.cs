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
	
	[MenuItem(kCompressionMenu)]
    static public void LZ4Compression()
    {
        var @checked = Menu.GetChecked(kCompressionMenu);
        Menu.SetChecked(kCompressionMenu, !@checked);
    }


    [MenuItem("AssetBundles/Build AssetBundles")]
    static public void BuildAssetBundles()
    {
        BuildScript.lz4Complession = Menu.GetChecked(kCompressionMenu);
        BuildScript.BuildAssetBundles();
    }
}
