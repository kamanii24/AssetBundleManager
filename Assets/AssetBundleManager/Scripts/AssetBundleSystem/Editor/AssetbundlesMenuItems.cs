using UnityEngine;
using UnityEditor;
using System.Collections;

public class AssetbundlesMenuItems
{
    const string kAESCryptionMenu = "AssetBundles/Enable AES Cryption";
    const string kAESCryptionConfigMenu = "AssetBundles/Open Config";
	const string kSimulateAssetBundlesMenu = "AssetBundles/Simulate AssetBundles";

    // 暗号化するかどうか
    [MenuItem(kAESCryptionMenu, false, 20)]
    public static void ToggleAESCryption()
    {
        BuildScript.IsAESCryption = !BuildScript.IsAESCryption;
    }

	[MenuItem(kAESCryptionMenu, true)]
    public static bool ToggleAESCryptionValidate()
    {
        Menu.SetChecked(kAESCryptionMenu, BuildScript.IsAESCryption);
        return true;
    }

    // 暗号情報設定アセットピックアップ
    [MenuItem(kAESCryptionConfigMenu, false, 21)]
    static void SelectionAsset()
    {
        var guids = AssetDatabase.FindAssets("t:AESConfig");
        if (guids.Length == 0)
        {
            throw new System.IO.FileNotFoundException("AESConfig does not found");
        }

        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
        var obj = AssetDatabase.LoadAssetAtPath<AESConfig>(path);
        EditorGUIUtility.PingObject(obj);
        Selection.activeObject = obj;
    }

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

    [MenuItem("AssetBundles/Build AssetBundles/LZMA (High Compressed)", false, 1)]
    static public void BuildForLZMACompression()
    {
        BuildScript.BuildAssetBundles();
    }

    [MenuItem("AssetBundles/Build AssetBundles/LZ4 (Low Compressed)", false, 2)]
    static public void BuildForLZ4Compression()
    {
        BuildScript.BuildAssetBundles(BuildAssetBundleOptions.ChunkBasedCompression);
    }

    [MenuItem("AssetBundles/Build AssetBundles/UnCompress", false, 3)]
    static public void BuildForUnCompression()
    {
        BuildScript.BuildAssetBundles(BuildAssetBundleOptions.UncompressedAssetBundle);
    }
}
