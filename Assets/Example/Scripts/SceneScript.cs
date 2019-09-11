// =================================
//
//  SceneScript.cs
//	Created by Takuya Himeji
//
// =================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using KM2;


public class SceneScript : MonoBehaviour
{
    // AssetBundleのベースURL
    public string remoteManifestURL = "http://localhost/";
    public string localManifestPath = "";
    // ローカル(StreamingAssets)のAssetBundleをロードするかどうか
    public bool isLocal = true;
    // ダウンロード対象のアセットバンドル
    public string[] downloadAssetBundles = { "cube", "sphere" };
    
    [Space()] public bool clearAssetBundlesInCache= false;  // キャッシュを削除するかどうか
    public bool autoLoadAssetBundle = true; // AssetBundleダウンロード時、自動的にメモリへ読み込むかどうか

    private GameObject assetContainer = null;
    private string output = "";
    private int selGridInt = 0;
   
    private void Start()
    {
        if (clearAssetBundlesInCache) Caching.ClearCache();

        string manifestURL = "";
        if(isLocal)
        {
            manifestURL = "file://" + Application.streamingAssetsPath +"/"+ localManifestPath;
        }
        else
        {
            manifestURL = remoteManifestURL;
        }
        
        // AssetBundleManager初期化
        AssetBundleManager.Initialize(remoteManifestURL, (bool isComplete)=>
        {
            // ダウンロード対象のAssetBundleのファイルサイズ
            AssetBundleManager.GetDownloadFileSize(downloadAssetBundles, (ulong b, string e) =>
            {
                output = "Downloadable AssetBundles file size = " + b + " Bytes.";
            });
        });
    }

    private void OnGUI()
    {
        // 指定したアセットのダウンロード処理
        #region DOWNLOAD_ASSETBUNDLES
        if (GUILayout.Button("Download AssetBundles", GUILayout.MinWidth(256)))
        {
            // ダウンロード開始
            AssetBundleManager.DownloadAssetBundle(downloadAssetBundles, autoLoadAssetBundle, Downloading);
        }
        #endregion DOWNLOAD_ASSETBUNDLES


        // ダウンロードしたアセットのロード処理
        #region ASSETBUNDLE_LIST
        GUILayout.Label("Loaded AssetBundle List");
        List<string> bundleList = new List<string>();
        foreach (var bundle in AssetBundle.GetAllLoadedAssetBundles())
        {
            bundleList.Add(bundle.name);
        }
        if (bundleList.Count <= selGridInt) { selGridInt = bundleList.Count - 1; }
        selGridInt = GUILayout.SelectionGrid(selGridInt, bundleList.ToArray(), 2);
        #endregion ASSETBUNDLE_LIST


        // GameObjectのInstantiate
        #region PREFAB_INSTANTIATE
        GUILayout.Space(8);
        if (GUILayout.Button("Instantiate"))
        {
            // ロード処理
            string abName = bundleList[selGridInt];
            string assetName = abName;
            if(assetName.Contains("/"))     // 子階層になっている場合
            {
                string[] n = assetName.Split('/');
                assetName = n[n.Length - 1];
            }
            else
            {
                assetName = abName;
            }
            GameObject go = AssetBundleManager.GetAsset<GameObject>(abName, assetName);

            // Instantiate
            InstantiateAsset(go);
        }
        #endregion PREFAB_INSTANTIATE


        // AssetBundleのUnload
        #region UNLOAD_ASSETBUNDLES
        if (GUILayout.Button("Unload"))
        {
            string name = bundleList[selGridInt];
            AssetBundleManager.Unload(name);
        }
        #endregion UNLOAD_ASSETBUNDLES

        // ログ
        #region OUTPUT_LOG
        GUILayout.Space(32);
        GUILayout.Box(output, GUILayout.Height(128));
        #endregion OUTPUT_LOG
    }

    // ダウンロード実行中
    private void Downloading(ulong downloadedBytes, ulong totalBytes, int fileIndex, bool completed, string error)
    {
        // エラー処理
        if (!string.IsNullOrEmpty(error))
        {
            output = "error : " + error;
        }

        // ダウンロードBytesサイズ更新
        output = downloadedBytes + " bytes / "+ totalBytes + "bytes";

        // ダウンロード完了
        if (completed)
        {
            output = "Donwload completed.";
        }
    }


    // GameObjectの生成
    private void InstantiateAsset(GameObject go)
    {
        if(assetContainer != null) Destroy(assetContainer);
        assetContainer = Instantiate(go);
    }
}