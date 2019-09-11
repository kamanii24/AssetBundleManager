// =================================
//
//  AssetBundleManager.cs
//	Created by Takuya Himeji
//
// =================================

using System.Linq;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace KM2
{
    public class AssetBundleManager : MonoBehaviour
    {
        #region DEFINITION_FIELD

        // 非同期通知用デリゲート
        public delegate void AssetBundleDownloadProgress(ulong downloadedBytes, ulong totalBytes, int fileIndex, bool completed, string error);
        public delegate void AssetBundleLoaded(AssetBundle[] loadedAssetBundles, string error);
        public delegate void AsyncAssetLoaded<T>(T asset);
        public delegate void AssetBundleDownloadFileSize(ulong totalBytes, string error);

        private static AssetBundleManager instance;
        private static string baseURL = string.Empty;
        private static AssetBundleManifest manifest = null;


        /// <summary>
        /// 初期化済みの場合trueを返します。
        /// </summary>
        public static bool Initialized
        {
            get
            {
                if (manifest == null)
                {
                    Debug.LogWarning("AssetBundleManager has not been Initialized.");
                    return false;
                }
                return true;
            }
        }


        /// <summary>
        /// AssetBundleのBaseURLを設定、または取得します。
        /// </summary>
        public static string AssetBundleDirectoryURL
        {
            get { return baseURL; }
            set { baseURL = value; }
        }


        /// <summary>
        /// AssetBundleManifest
        /// </summary>
        public static AssetBundleManifest Manifest
        {
            get { return manifest; }
            set { manifest = value; }
        }

        #endregion DEFINITION_FIELD




        // ----------------




        # region IMPLEMENTATION_FIELD
        # region PUBLIC_METHODS

        /// <summary>
        /// AssetBundleManagerの初期設定をします。
        /// ダウンロード先のディレクトリパスを設定します
        /// </summary>
        public static void Initialize(string manifestURL, string assetBundleDirectoryURL, System.Action<bool> complete)
        {
            if (Initialized)
            {
                Debug.Log("Has initialized.");
                return;
            }

            // インスタンス取得
            var go = new GameObject("AssetBundleManager", typeof(AssetBundleManager));
            DontDestroyOnLoad(go);
            instance = go.GetComponent<AssetBundleManager>();

            if (assetBundleDirectoryURL == "")
            {
                List<string> spl = new List<string>(manifestURL.Split(new string[] { "/" }, System.StringSplitOptions.None));
                spl.Remove(spl.LastOrDefault());
                assetBundleDirectoryURL = string.Join("/", spl) + "/";
            }

            // URLとバージョンをセット
            AssetBundleDirectoryURL = assetBundleDirectoryURL;
            instance.StartCoroutine(GetAssetBundleManifest(manifestURL, complete));
        }

        /// <summary>
        /// AssetBundleManagerの初期設定をします。
        /// ダウンロード先のディレクトリパスを設定します
        /// </summary>
        public static void Initialize(string manifestURL, System.Action<bool> complete)
        {
            Initialize(manifestURL, "", complete);
        }

        private static IEnumerator GetAssetBundleManifest(string manifestURL, System.Action<bool> complete)
        {
#if UNITY_2018_1_OR_NEWER
            using (UnityWebRequest www = UnityWebRequestAssetBundle.GetAssetBundle(manifestURL))
#else
            using (UnityWebRequest www = UnityWebRequest.GetAssetBundle(manifestURL))
#endif
            {
                // ダウンロードを待つ
                yield return www.SendWebRequest();

                // エラー処理
                if (www.error != null)
                {
                    www.Dispose();
                    complete(false);
                    throw new System.Exception("error : " + www.error);
                }
                AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(www);
                manifest = bundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                bundle.Unload(false);
            }

            complete(true);
        }




        /// <summary>
        /// assetBundleNames で指定したAssetBundleをダウンロードします。
        /// </summary>
        public static void DownloadAssetBundle(string[] assetBundleNames, AssetBundleDownloadProgress handler)
        {
            if (!Initialized) return;
            instance.StartCoroutine(Download(assetBundleNames, true, handler));
        }



        /// <summary>
        /// assetBundleNames で指定したAssetBundleをダウンロードします。
        /// 第二引数のbool値でダウンロード / キャッシュされたAssetBundleを自動的にメモリへロードを行うか判断します。
        /// </summary>
        public static void DownloadAssetBundle(string[] assetBundleNames, bool autoLoadAssetBundle, AssetBundleDownloadProgress handler)
        {
            if (!Initialized) return;
            instance.StartCoroutine(Download(assetBundleNames, autoLoadAssetBundle, handler));
        }




        /// <summary>
        /// assetBundleName で指定したAssetBundleをダウンロードします。
        /// </summary>
        public static void DownloadAssetBundle(string assetBundleName, AssetBundleDownloadProgress handler)
        {
            DownloadAssetBundle(new string[] { assetBundleName }, handler);
        }




        /// <summary>
        /// assetBundleNames で指定したAssetBundleを読み込みます。
        /// このときCacheに存在しなければ、サーバーからダウンロードします。
        /// </summary>
        public static void LoadAssetBundle(string[] assetBundleNames, AssetBundleLoaded handler)
        {
            if (!Initialized) return;
            instance.StartCoroutine(Load(assetBundleNames, handler));
        }




        /// <summary>
        /// assetBundleName で指定したAssetBundleを取得します。
        /// このときCacheに存在しなければ、サーバーからダウンロードします。
        /// </summary>
        public static void LoadAssetBundle(string assetBundleName, AssetBundleLoaded handler)
        {
            LoadAssetBundle(new string[] { assetBundleName }, handler);
        }




        /// <summary>
        /// assetBundleName で指定したAssetBundleを取得します。
        /// </summary>
        public static AssetBundle GetAssetBundle(string assetBundleName)
        {
            foreach (AssetBundle bundle in AssetBundle.GetAllLoadedAssetBundles())
            {
                if (bundle.name == assetBundleName) { return bundle; }
            }
            Debug.Log("[" + assetBundleName + "] has not loaded.");
            return null;
        }




        /// <summary>
        /// assetBundleNames で指定したAssetBundleを複数取得します。
        /// </summary>
        public static AssetBundle[] GetAssetBundles(string[] assetBundleNames)
        {
            List<AssetBundle> abList = new List<AssetBundle>();
            foreach (string abName in assetBundleNames)
            {
                int count = abList.Count;
                foreach (AssetBundle bundle in AssetBundle.GetAllLoadedAssetBundles())
                {
                    if (bundle.name == abName)
                    {
                        abList.Add(bundle);
                        break;
                    }
                }
                if (count == abList.Count) Debug.Log("[" + abName + "] has not loaded.");
            }
            return abList.ToArray();
        }





        /// <summary>
        /// 名前で指定したAssetをロードされたAssetBundleから取得します。
        /// </summary>
        public static T GetAsset<T>(string bundleName, string assetName) where T : Object
        {
            if (!Initialized) return default(T);

            foreach (AssetBundle ab in AssetBundle.GetAllLoadedAssetBundles())
            {
                if (ab.name == bundleName) { return ab.LoadAsset<T>(assetName); }
            }

            Debug.Log("Could not load " + assetName + ". because " + bundleName + " has not loaded.");
            return default(T);
        }




        /// <summary>
        /// 名前で指定したAssetをロードされたAssetBundleから取得します。
        /// </summary>
        public static Object GetAsset(string bundleName, string assetName)
        {
            if (!Initialized) return null;
            return GetAsset<Object>(bundleName, assetName);
        }




        /// <summary>
        /// 名前で指定したAssetをロードされたAssetBundleから取得します。
        /// </summary>
        public static void GetAssetAsync<T>(string bundleName, string assetName, AsyncAssetLoaded<T> handler) where T : Object
        {
            if (!Initialized) return;

            foreach (AssetBundle bundle in AssetBundle.GetAllLoadedAssetBundles())
            {
                if (bundleName == bundle.name)
                {
                    instance.StartCoroutine(AsyncLoadAsset<T>(bundle, assetName, handler));
                    return;
                }
            }
            if (handler != null) handler(default(T));
        }




        /// <summary>
        /// 名前で指定したAssetをロードされたAssetBundleから取得します。
        /// </summary>
        public static void GetAssetAsync(string bundleName, string assetName, AsyncAssetLoaded<Object> handler)
        {
            GetAssetAsync<Object>(bundleName, assetName, handler);
        }




        /// <summary>
        /// ロードされている全てのAssetBundle本体を取得します。
        /// </summary>
        public static AssetBundle[] GetAllLoadedAssetBundles()
        {
            List<AssetBundle> bundleList = new List<AssetBundle>();
            foreach (AssetBundle bundle in AssetBundle.GetAllLoadedAssetBundles())
            {
                bundleList.Add(bundle);
            }
            return bundleList.ToArray();
        }




        /// <summary>
        /// ロードされているAssetBundleの名前を全て取得します。
        /// </summary>
        public static string[] GetAllLoadedAssetBundleNames()
        {
            if (!Initialized) return null;

            List<string> nameList = new List<string>();
            foreach (AssetBundle ab in AssetBundle.GetAllLoadedAssetBundles())
            {
                nameList.Add(ab.name);
            }
            return nameList.ToArray();
        }




        /// <summary>
        /// bundleName で指定したAssetが対象のAssetBundle内に存在するかチェックします。
        /// </summary>
        public static bool Contains(string bundleName, string assetName)
        {
            if (!Initialized) return false;

            foreach (AssetBundle bundle in AssetBundle.GetAllLoadedAssetBundles())
            {
                if (bundleName == bundle.name) { return bundle.Contains(assetName); }
            }
            Debug.Log("Could not load " + assetName + ". because " + bundleName + " has not loaded.");
            return false;
        }




        /// <summary>
        /// サーバー上にある assetBundleName で指定したダウンロード対象のAssetBundleのファイルサイズの合計値を取得します。
        /// ローカルキャッシュのAssetBundleは無視され、結果に含まれません。
        /// </summary>
        public static void GetDownloadFileSize(string[] assetBundleNames, AssetBundleDownloadFileSize handler)
        {
            if (!AssetBundleManager.Initialized) return;
            instance.StartCoroutine(GetDownloadFileSizeCoroutine(assetBundleNames, handler));
        }




        /// <summary>
        /// サーバー上にある assetBundleName で指定したダウンロード対象のAssetBundleのファイルサイズの合計値を取得します。
        /// ローカルキャッシュのAssetBundleは無視され、結果に含まれません。
        /// </summary>
        public static void GetDownloadFileSize(string assetBundleName, AssetBundleDownloadFileSize handler)
        {
            GetDownloadFileSize(new string[] { assetBundleName }, handler);
        }



        /// <summary>
        /// bundleName で指定したAssetBundleを破棄します。
        /// 指定がない場合は全てのAssetBundleを破棄します。
        /// </summary>
        public static void Unload(bool unloadAllLoadedObjects)
        {
            if (!Initialized) return;

            // 全て破棄する
            AssetBundle.UnloadAllAssetBundles(unloadAllLoadedObjects);
        }



        /// <summary>
        /// bundleName で指定したAssetBundleを破棄します。
        /// 指定がない場合は全てのAssetBundleを破棄します。
        /// </summary>
        public static void Unload(bool unloadAllLoadedObjects, params string[] bundleNames)
        {
            if (!Initialized) return;

            foreach (string bundleName in bundleNames)
            {
                // 指定したAssetBundleだけ破棄する
                foreach (AssetBundle bundle in AssetBundle.GetAllLoadedAssetBundles())
                {
                    if (bundleName == bundle.name)
                    {
                        bundle.Unload(unloadAllLoadedObjects);
                        return;
                    }
                }
            }
        }



        /// <summary>
        /// bundleName で指定したAssetBundleを破棄します。
        /// 指定がない場合は全てのAssetBundleを破棄します。
        /// </summary>
        public static void Unload(params string[] bundleNames)
        {
            Unload(false, bundleNames);
        }


        #endregion PUBLIC_METHODS


        #region  PRIVATE_METHODS


        // キャッシュからAssetBundleをロードする
        private static IEnumerator Load(string[] assetBundleNames, AssetBundleLoaded handler)
        {
            while (!Caching.ready) { yield return null; }

            // ロード済みのAssetBundleは配列から除外する
            assetBundleNames = RemoveAlreadyLoaded(assetBundleNames);

            // ロードする
            List<AssetBundle> abList = new List<AssetBundle>();
            int index = 0;
            while (index < assetBundleNames.Length)
            {
                // baseURLにAssetBuddle名を付与してURL生成
                string bundleName = assetBundleNames[index];

                // 本体のアクセスURLの作成
                string url = AssetBundleManager.AssetBundleDirectoryURL;
                url = Path.Combine(url, bundleName);

                // キャッシュからAssetBundleをロードする
#if UNITY_2018_1_OR_NEWER
                using (UnityWebRequest www = UnityWebRequestAssetBundle.GetAssetBundle(url))
#else
                using (UnityWebRequest www = UnityWebRequest.GetAssetBundle(url))
#endif
                {
                    // ダウンロードを待つ
                    yield return www.SendWebRequest();

                    // エラー処理
                    if (www.error != null)
                    {
                        if (handler != null) { handler(null, www.error); }  // ロード失敗
                        www.Dispose();
                        throw new System.Exception("error : " + www.error);
                    }
                    AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(www);
                    Debug.Log("[" + bundle.name + "] load success.");

                    abList.Add(bundle);
                }
                index++;    // Index更新
            }

            if (handler != null) handler(abList.ToArray(), null);
        }




        // 非同期でAssetを取得する
        private static IEnumerator AsyncLoadAsset<T>(AssetBundle bundle, string assetName, AsyncAssetLoaded<T> handler) where T : Object
        {
            // 非同期でAssetをロードする
            AssetBundleRequest request = bundle.LoadAssetAsync<T>(assetName);
            // 取得するまで待つ
            yield return request;
            // 取得成功 
            if (handler != null) handler(request.asset as T);
        }




        // サーバーからAssetBundleをダウンロードする
        private static IEnumerator Download(string[] assetBundleNames, bool autoLoadAssetBundle, AssetBundleDownloadProgress handler)
        {
            int fileIndex = 0;
            ulong bytes = 0;

            while (!Caching.ready) { yield return null; }

            // ロード済みのAssetBundleは配列から除外する
            assetBundleNames = RemoveAlreadyLoaded(assetBundleNames);
            // AssetBundleを全てダウンロードするまで回す
            while (fileIndex < assetBundleNames.Length)
            {
                // baseURLにAssetBuddle名を付与してURL生成
                string bundleName = assetBundleNames[fileIndex];
                string url = AssetBundleManager.AssetBundleDirectoryURL + bundleName;

                // URLキャッシュ防止のためタイムスタンプを付与
                url = SetTimestamp(url);

                // Get assetbundle hash code.
                var hash = manifest.GetAssetBundleHash(bundleName);

                // ダウンロード開始
#if UNITY_2018_1_OR_NEWER
                using (UnityWebRequest www = UnityWebRequestAssetBundle.GetAssetBundle(url, hash, 0))
#else
                using (UnityWebRequest www = UnityWebRequest.GetAssetBundle(url, hash, 0))
#endif
                {
                    www.SendWebRequest();

                    // ダウンロード更新
                    while (!www.isDone)
                    {
                        bytes = 0;
                        if (ulong.TryParse(www.GetResponseHeader("Content-Length"), out bytes))
                        {
                            if (handler != null) { handler(www.downloadedBytes, bytes, fileIndex, false, www.error); }
                        }
                        yield return new WaitForEndOfFrame();
                    }

                    // エラー処理
                    if (!string.IsNullOrEmpty(www.error))
                    {
                        if (handler != null) handler(0, 0, fileIndex, false, www.error);
                        www.Dispose();
                        throw new System.Exception("WWW download had an error:" + www.error);
                    }

                    if (autoLoadAssetBundle) { DownloadHandlerAssetBundle.GetContent(www); }

                    www.Dispose();
                }
                fileIndex++;    // Index更新
            }

            // 完了通知
            if (handler != null) handler(bytes, bytes, fileIndex, true, null);
        }




        // ダウンロードするAssetBundleのファイルサイズを取得する
        private static IEnumerator GetDownloadFileSizeCoroutine(string[] assetBundleNames, AssetBundleDownloadFileSize handler)
        {
            ulong bytes = 0;
            int fileIndex = 0;

            while (!Caching.ready) { yield return null; }

            // ロード済みのAssetBundleは配列から除外する
            assetBundleNames = RemoveAlreadyLoaded(assetBundleNames);

            // AssetBundleを全てダウンロードするまで回す
            while (fileIndex < assetBundleNames.Length)
            {
                // baseURLにAssetBuddle名を付与してURL生成
                string bundleName = assetBundleNames[fileIndex];
                string url = AssetBundleManager.AssetBundleDirectoryURL + bundleName;

                // URLキャッシュ防止のためタイムスタンプを付与
                url = SetTimestamp(url);

                // Get assetbundle hash code.
                var hash = manifest.GetAssetBundleHash(bundleName);

                // CRCチェック
#if UNITY_2018_1_OR_NEWER
                using (UnityWebRequest www = UnityWebRequestAssetBundle.GetAssetBundle(url, hash, 0))
#else
                using (UnityWebRequest www = UnityWebRequest.GetAssetBundle(url, hash, 0))
#endif
                {
                    // ダウンロード開始
                    www.SendWebRequest();
                    // ダウンロードが完了するまでプログレスを更新する
                    while (www.downloadProgress < 1f)
                    {
                        ulong len = 0;
                        if (ulong.TryParse(www.GetResponseHeader("Content-Length"), out len))
                        {
                            if (len > 0)
                            {
                                bytes += len;
                                break;
                            }
                        }
                    }
                    www.Dispose();
                }
                fileIndex++;    // Index更新
            }

            // 完了通知
            if (handler != null) handler(bytes, null);
        }
        


        // ロード済みのAssetBundleを文字入れて受から取り除く
        private static string[] RemoveAlreadyLoaded(string[] abNames)
        {
            List<string> abNameList = new List<string>(abNames);
            foreach (var ab in AssetBundle.GetAllLoadedAssetBundles())
            {
                if (abNameList.Contains(ab.name)) { abNameList.Remove(ab.name); }
            }
            return abNameList.ToArray();
        }




        // URLにタイムスタンプを付与
        private static string SetTimestamp(string url)
        {
            url += ((url.Contains("?")) ? "&" : "?") + "t=" + System.DateTime.Now.ToString("yyyyMMddHHmmss");
            return url;
        }
    }

    #endregion PRIVATE_METHODS
    #endregion IMPLEMENTATION_FIELD
}