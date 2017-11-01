// ========
// AssetBundleManager.cs
// v1.2.1
// Created by kamanii24
// ========

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class AssetBundleManager : MonoBehaviour
{
    // ========
    #region CALLBACK_DELEGATE

    // アセットバンドルダウンロードプログレス更新用
    public delegate void OnDownloadProgressUpdate(float progress, int fileIndex, bool isComplete, string error);
    // アセットバンドルロード完了通知用
    public delegate void OnLoadComplete(bool isSuccess, string error);
    // 非同期アセット取得完了通知用
    public delegate void OnAsyncLoadAssetComplete(UnityEngine.Object asset, bool isSuccess);

    #endregion CALLBACK_DELEGATE


    // ========
    #region PRIVATE_STATIC_MEMBER_VARIABLES

    // 初期化済みかどうか
    private static bool initialized = false;
    // 初期設定変数
    private static string baseURL = string.Empty;                   // アセットバンドルディレクトリURL   
    private static Dictionary<string, AssetBundle> bundleDic = null;// アセットバンドル保管用Dictionary

    #endregion PRIVATE_STATIC_MEMBER_VARIABLES


    // ========
    #region PRIVATE_MEMBER_VARIABLES

    private static AssetBundleManager instance;

    // ダウンロードファイルカウント
    private static int fileIndex = 0;
    private static uint ver = 1;

    #endregion PRIVATE_MEMBER_VARIABLES


    // ========
    #region PUBLIC_PROPETIES

    /// <summary>
    /// 初期化済みの場合trueを返します
    /// </summary>
    public static bool Initialized
    {
        get
        {
            if (!initialized)
            {
                if (baseURL == string.Empty)
                {
                    Debug.LogWarning("AssetBundleManager has not been Initialized.");
                }
            }
            return initialized;
        }
    }

	/// <summary>
	/// AssetBundleのBaseURLを設定、または取得します
	/// </summary>
    public static string AssetBundleDirectoryURL
    {
        get { return baseURL; }
        set { baseURL = value; }
    }

    #endregion PUBLIC_PROPETIES


    // ========
    #region PUBLIC_METHOD

    /// <summary>
    /// 初期設定をします。
    /// Initializeで設定した値はstatic変数として保存されます。
    /// </summary>
    /// <param name="assetBundleDirectoryURL">アセットバンドルのディレクトリURLを指定します。</param>
    /// <param name="version">アセットバンドルのバージョンを指定します(任意)</param>
    public static void Initialize(string assetBundleDirectoryURL, uint version = 1)
    {
        if (initialized)
        {
            Debug.Log("Has initialized.");
            return;
        }

        // インスタンス取得
        var go = new GameObject("AssetBundleManager", typeof(AssetBundleManager));
        DontDestroyOnLoad(go);
        instance = go.GetComponent<AssetBundleManager>();

        // 初期化済み
        initialized = true;
        // URLとバージョンをセット
        AssetBundleDirectoryURL = assetBundleDirectoryURL;
        ver = version;

        Debug.Log(AssetBundleDirectoryURL);

        // Dictionary初期化
        if (bundleDic == null)
            bundleDic = new Dictionary<string, AssetBundle>();
    }

    /// <summary>
    /// サーバから複数のアセットバンドルをダウンロードします。
    /// </summary>
    /// <param name="assetBundleNames">サーバからダウンロードするアセットバンドルを複数指定します。</param>
    /// <param name="update">ダウンロードの進捗状況が0.0~1.0のfloat形式で返されます。</param>
    public static void DownloadAssetBundle(string[] assetBundleNames, OnDownloadProgressUpdate update)
    {
        // 初期化済みかどうかチェック
        if (!initialized) return;
        // ダウンロード開始
        instance.StartCoroutine(Download(assetBundleNames, update));
    }

    /// <summary>
    /// サーバからアセットバンドルをダウンロードします。
    /// </summary>
    /// <param name="assetBundleName">サーバからダウンロードするアセットバンドルをひとつ指定します。</param>
    /// <param name="update">ダウンロードの進捗状況がコールバックで返されます。</param>
    public static void DownloadAssetBundle(string assetBundleName, OnDownloadProgressUpdate update)
    {
        // DownloadAssetBundleを実行
        DownloadAssetBundle(new string[] { assetBundleName }, update);
    }

    /// <summary>
    /// キャッシュから複数のアセットバンドルを取得します。
    /// </summary>
    /// <param name="assetBundleNames">取得するアセットバンドルを複数指定します。</param>
    /// <param name="cb">完了時にコールバックで結果が返されます。</param>
    public static void LoadAssetBundle(string[] assetBundleNames, OnLoadComplete cb)
    {
        // 初期化済みかどうかチェック
        if (!initialized) return;

        // URL設定
        List<string> urlList = new List<string>();
        foreach (string name in assetBundleNames)
        {
            // URLを生成
            string tmp = baseURL + name;
            // URLをセットする
            urlList.Add(tmp);
        }
        // ロード開始
        instance.StartCoroutine(Load(urlList, assetBundleNames, cb));
    }

    /// <summary>
    /// キャッシュからアセットバンドルを取得します。
    /// </summary>
    /// <param name="assetBundleName">取得するアセットバンドルをひとつ指定します。</param>
    /// <param name="cb">完了時にコールバックで結果が返されます。</param>
    public static void LoadAssetBundle(string assetBundleName, OnLoadComplete cb)
    {
        // LoadAssetBundle実行
        LoadAssetBundle(new string[] { assetBundleName }, cb);
    }

    /// <summary>
    /// 名前で指定したアセットを同期処理で取得します。
    /// </summary>
    /// <returns>ジェネリクス型で取得したアセットデータが返されます。適当な型にキャストして使用してください。</returns>
    /// <param name="bundleName">取得するアセットが含まれているアセットバンドル名を指定します。</param>
    /// <param name="assetName">取得するアセット名を指定します。</param>
    public static T GetAsset<T>(string bundleName, string assetName) where T : UnityEngine.Object
    {
        // 初期化済みかどうかチェック
        if (!initialized) return default(T);
        // アセットバンドルがロードされているか確認
        if (bundleDic == null)
        {
            Debug.LogError("Could not load " + assetName + ". because " + bundleName + " has not loaded.");
            return default(T);
        }
        else
        {
            // ロードしたリストに存在するかどうかチェック
            foreach (string name in GetAllAssetBundleName())
            {
                if (bundleName == name)
                {
                    // アセットバンドルをロード
                    AssetBundle bundle = bundleDic[bundleName];
                    // object型として返す
                    return bundle.LoadAsset<T>(assetName);
                }
            }
        }
        Debug.LogError("Could not load " + assetName + ". because " + bundleName + " has not loaded.");
        return default(T);
    }

    /// <summary>
    /// 名前で指定したアセットを同期処理で取得します。
    /// </summary>
    /// <returns>object型で取得したアセットデータが返されます。適当な型にキャストして使用してください。</returns>
    /// <param name="bundleName">取得するアセットが含まれているアセットバンドル名を指定します。</param>
    /// <param name="assetName">取得するアセット名を指定します。</param>
    [Obsolete("Use GetAsset<T>")]
    public static UnityEngine.Object GetAsset(string bundleName, string assetName)
    {
        // 初期化済みかどうかチェック
        if (!initialized) return null;
        // Object型として取得して返す
        return GetAsset<UnityEngine.Object>(bundleName, assetName);
    }

    /// <summary>
    /// 名前で指定したアセットを非同期処理で取得します。
    /// </summary>
    /// <param name="bundleName">取得するアセットが含まれているアセットバンドル名を指定します。</param>
    /// <param name="assetName">取得するアセット名を指定します。</param>
    /// <param name="cb">Cb.</param>
    public static void GetAssetAsync(string bundleName, string assetName, OnAsyncLoadAssetComplete cb)
    {
        // 初期化済みかどうかチェック
        if (!initialized) return;

        // アセットバンドルがロードされているか確認
        if (bundleDic == null)
        {
            Debug.LogError("It has not been initialized. Please be call Initialize() in advance.");
            cb(null, false);
        }
        else
        {
            foreach (string name in GetAllAssetBundleName())
            {
                if (bundleName == name)
                {
                    // アセットバンドルをロード
                    instance.StartCoroutine(AsyncLoadAsset(bundleName, assetName, cb));
                    return;
                }
            }
        }
        cb(null, false);
    }

    /// <summary>
    /// 現在ロードされているアセットバンドル名を全て取得します。
    /// </summary>
    public static string[] GetAllAssetBundleName()
    {
        // 初期化済みかどうかチェック
        if (!initialized) return null;

        // アセットバンドルがロードされているか確認
        if (bundleDic == null)
        {
            Debug.LogError("It has not been initialized. Please be call Initialize() in advance.");
            return null;
        }
        else
        {
            // アセットバンドル名を取得
            List<string> nameList = new List<string>();
            foreach (KeyValuePair<string, AssetBundle> pair in bundleDic)
            {
                // Listに追加する
                nameList.Add(pair.Key);
            }
            return nameList.ToArray();
        }
    }

    /// <summary>
    /// 名前で指定したアセットが存在するかチェックします。
    /// </summary>
    /// <returns>存在の有無がboolで返されます。</returns>
    /// <param name="bundleName">存在の確認するアセットが含まれているアセットバンドル名を指定します。</param>
    /// <param name="assetName">存在の確認するアセット名を指定します。</param>
    public static bool Contains(string bundleName, string assetName)
    {
        // 初期化済みかどうかチェック
        if (!initialized) return false;
        // アセットバンドルがロードされているか確認
        if (bundleDic == null)
        {
            Debug.LogError("Could not load " + assetName + ". because " + bundleName + " has not loaded.");
            return false;
        }
        else
        {
            // ロードしたリストに存在するかどうかチェック
            foreach (string name in GetAllAssetBundleName())
            {
                if (bundleName == name)
                {
                    // アセットバンドルをロード
                    AssetBundle bundle = bundleDic[bundleName];
                    // 存在の有無を返す
                    return bundle.Contains(name);
                }
            }
        }
        Debug.LogError("Could not load " + assetName + ". because " + bundleName + " has not loaded.");
        return false;
    }

    /// <summary>
    /// 名前で指定したアセットバンドルをメモリから破棄します。
    /// 指定がない場合は全てのアセットバンドルをメモリから破棄します。
    /// </summary>
    public static void Unload()
    {
        // 初期化済みかどうかチェック
        if (!initialized) return;

        // 全て破棄する
        foreach (KeyValuePair<string, AssetBundle> pair in bundleDic)
        {
            pair.Value.Unload(false);
        }
        // キーを破棄する
        if (bundleDic != null)
        {
            bundleDic.Clear();
        }
    }

    /// <summary>
    /// 名前で指定したアセットバンドルを破棄します。
    /// 指定がない場合は全てのアセットバンドルを破棄します。
    /// </summary>
    public static void Unload(string bundleName)
    {
        // 初期化済みかどうかチェック
        if (!initialized) return;

        // 指定されたアセットバンドルを破棄
        bundleDic[bundleName].Unload(false);
        // Dictionaryからも削除する
        bundleDic.Remove(bundleName);
    }

    #endregion PUBLIC_METHOD


    // ========
    #region PRIVATE_CORUTINE_METHOD

    // キャッシュからアセットバンドルをロードする
    private static IEnumerator Load(List<string> urlList, string[] assetBundleNames, OnLoadComplete cb)
    {
        // キャッシュできる状態か確認
        while (!Caching.ready)
            yield return null;

        // ロードする
        int index = 0;
        do
        {
            // ロードされているかどうかチェック
            if (!bundleDic.ContainsKey(assetBundleNames[index]))
            {
                // キャッシュからアセットバンドルをロードする
                UnityWebRequest www = UnityWebRequest.GetAssetBundle(urlList[index]);
                // ロードを待つ
                yield return www;

                // エラー処理
                if (www.error != null)
                {
                    cb(false, www.error);  // ロード失敗
                    // wwwを解放する
                    www.Dispose();
                    throw new Exception("error : " + www.error);
                }
                // ロードしたアセットバンドルをセット
                AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(www);
                bundleDic.Add(assetBundleNames[fileIndex], bundle);
            }
        } while (++index < assetBundleNames.Length);

        cb(true, null);
    }


    // サーバからアセットバンドルをダウンロードする
    private static IEnumerator Download(string[] assetBundleNames, OnDownloadProgressUpdate update)
    {
        // キャッシュできる状態か確認
        while (!Caching.ready)
            yield return null;

        // アセットバンドルを全てダウンロードするまで回す
        fileIndex = 0;
        do
        {
            // baseURLにAssetBuddle名を付与してURL生成
            string bundleName = assetBundleNames[fileIndex];
            string url = baseURL + bundleName;
            string manifestURL = url + ".manifest";
            // URLキャッシュ防止のためタイムスタンプを付与
            url += ((url.Contains("?")) ? "&" : "?") + "t=" + DateTime.Now.ToString("yyyyMMddHHmmss");
            manifestURL += ((manifestURL.Contains("?")) ? "&" : "?") + "t=" + DateTime.Now.ToString("yyyyMMddHHmmss");

            // CRCチェックを行うか確認
            // manifestファイルをDL
            UnityWebRequest wwwManifest = UnityWebRequest.Get(manifestURL);
            // ダウンロードを待つ
            yield return wwwManifest.SendWebRequest();
            
            // manifestが存在していた場合はCRCチェックをする
            uint latestCRC = 0;
            if (string.IsNullOrEmpty(wwwManifest.error))
            {
                // manifest内部のCRCコードを抽出する
                string[] lines = wwwManifest.downloadHandler.text.Split(new string[] { "CRC: " }, StringSplitOptions.None);
                latestCRC = uint.Parse(lines[1].Split(new string[] { "\n" }, StringSplitOptions.None)[0]);

#if UNITY_2017_1_OR_NEWER
                // キャッシュを個別削除する
                string key = "km_assetbundleversioncache_" + bundleName;
                if (PlayerPrefs.HasKey(key))
                {
                    string currentCRC = PlayerPrefs.GetString(key);
                    if (currentCRC != latestCRC.ToString())
                    {
                        PlayerPrefs.SetString(key, latestCRC.ToString()); // 新しいcrcを保存
                        Caching.ClearAllCachedVersions(bundleName); // 既存のキャッシュを削除
                    }
                    Debug.Log(bundleName + ".manifest \n"+"Latesd CRC : " + latestCRC + "  Current CRC: " + currentCRC);
                }
                else
                {
                    PlayerPrefs.SetString(key, latestCRC.ToString()); // 新しいcrcを保存
                }
                latestCRC = 0;
#endif
            }
            else
            {
                Debug.Log(bundleName + ".manifest has not found.");
            }

            // CRCチェックしてダウンロード開始
            using (UnityWebRequest www = UnityWebRequest.GetAssetBundle(url, ver, latestCRC))
            {
                // ダウンロード開始
                www.SendWebRequest();
                // ダウンロードが完了するまでプログレスを更新する
                while (www.downloadProgress < 1f)
                {
                    // progress設定
                    float progress = 0f;
                    if (www.downloadProgress > 0)
                        progress = www.downloadProgress;
                    // 更新する
                    update(progress, fileIndex, false, www.error);
                    yield return new WaitForEndOfFrame();
                }

                // エラー処理
                if (!string.IsNullOrEmpty(www.error))
                {
                    // 完了通知
                    update(0f, fileIndex, false, www.error);
                    string err = www.error;
                    Debug.Log(www.error);
                    // wwwを解放する
                    www.Dispose();
                    throw new Exception("WWW download had an error:" + err);
                }
                // ロードしたアセットバンドルをセット
                AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(www);
                bundleDic.Add(bundleName, bundle);
                // wwwを解放する
                www.Dispose();
            }
        } while (++fileIndex < assetBundleNames.Length);

        // 完了通知
        update(1f, fileIndex, true, null);
    }


    // 非同期でアセットを取得する
    private static IEnumerator AsyncLoadAsset(string bundleName, string assetName, OnAsyncLoadAssetComplete cb)
    {
        // アセットバンドルをロード
        AssetBundle bundle = bundleDic[bundleName];
        // 非同期でアセットをロードする
        AssetBundleRequest request = bundle.LoadAssetAsync(assetName);
        // 取得するまで待つ
        yield return request;

        // 取得成功
        cb(request.asset, true);
    }

    #endregion PRIVATE_CORUTINE_METHOD
}