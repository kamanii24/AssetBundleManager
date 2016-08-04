// ========
// AssetBundleManager.cs
// v1.1.1
// Created by kamanii24
// ========

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class AssetBundleManager : MonoBehaviour {

	// ========
	#region SINGLETON

	// シングルトン宣言
	private static AssetBundleManager instance;

	/// <summary>
	/// 初回はInitializeで初期値を設定します。
	/// </summary>
	public static AssetBundleManager Instance {
		get {
			if (instance == null) {
				GameObject gObj = new GameObject("AssetBundleManager");
				instance = gObj.AddComponent<AssetBundleManager>();
			}
			return instance;
		}
	}

	#endregion SINGLETON


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
	private static bool isEnabled = false;
	// 初期設定変数
	private static string baseURL = string.Empty;	// アセットバンドルディレクトリURL
	private static int ver = -1;					// バージョン
	// アセットバンドル保管用Dictionary
	private static Dictionary<string, AssetBundle> bundleDic = null;

	#endregion PRIVATE_STATIC_MEMBER_VARIABLES


	// ========
	#region PRIVATE_MEMBER_VARIABLES

	// ダウンロードファイルカウント
	private int fileIndex = 0;

	#endregion PRIVATE_MEMBER_VARIABLES


	// ========
	#region PUBLIC_PROPETIES

	// 初期化判定のプロパティ
	public bool IsEnabled {
		get {
			if (!isEnabled) {
				if (baseURL == string.Empty && ver < 0) {
					Debug.LogWarning ("AssetBundleManager has not been Initialized.");
				}
			}
			return isEnabled;
		}
	}

	// ディレクトリURLのプロパティ
	public string AssetBundleDirectoryURL {
		get { return baseURL; }
		set { baseURL = value; }
	}

	// アセットバンドルバージョンのプロパティ
	public int Version {
		get { return ver; }
		set { ver = value; }
	}

	#endregion PUBLIC_PROPETIES


	// ========
	#region PUBLIC_METHOD

	/// <summary>
	/// 初期設定をします。
	/// Initializeで設定した値はstatic変数として保存されます。
	/// </summary>
	/// <param name="assetBundleDirectoryURL">アセットバンドルのディレクトリURLを指定します。</param>
	/// <param name="version">アセットバンドルのバージョンを指定します。</param>
	public void Initialize (string assetBundleDirectoryURL, int version) {
		// 初期化済み
		isEnabled = true;
		// URLとバージョンをセット
		AssetBundleDirectoryURL = assetBundleDirectoryURL;
		Version = version;

		// Dictionary初期化
		if (bundleDic == null)
			bundleDic = new Dictionary<string, AssetBundle> ();
	}

	/// <summary>
	/// サーバから複数のアセットバンドルをダウンロードします。
	/// </summary>
	/// <param name="assetBundleNames">サーバからダウンロードするアセットバンドルを複数指定します。</param>
	/// <param name="update">ダウンロードの進捗状況がコールバックで返されます。</param>
	public void DownloadAssetBundle (string[] assetBundleNames, OnDownloadProgressUpdate update) {
		// 初期化済みかどうかチェック
		if (!IsEnabled) return;

		// ダウンロード開始
		StartCoroutine (Download (assetBundleNames, update));
	}

	/// <summary>
	/// サーバからアセットバンドルをダウンロードします。
	/// </summary>
	/// <param name="assetBundleName">サーバからダウンロードするアセットバンドルをひとつ指定します。</param>
	/// <param name="update">ダウンロードの進捗状況がコールバックで返されます。</param>
	public void DownloadAssetBundle (string assetBundleName, OnDownloadProgressUpdate update) {
		// DownloadAssetBundleを実行
		DownloadAssetBundle (new string[] {assetBundleName}, update);
	}

	/// <summary>
	/// キャッシュから複数のアセットバンドルを取得します。
	/// </summary>
	/// <param name="assetBundleNames">取得するアセットバンドルを複数指定します。</param>
	/// <param name="cb">完了時にコールバックで結果が返されます。</param>
	public void LoadAssetBundle (string[] assetBundleNames, OnLoadComplete cb) {
		// 初期化済みかどうかチェック
		if (!IsEnabled) return;

		// URL設定
		List<string> urlList = new List<string>();
		foreach (string name in assetBundleNames) {
			// URLを生成
			string tmp = baseURL + name;
			// URLをセットする
			urlList.Add(tmp);
		}
		// ロード開始
		StartCoroutine (Load (urlList, assetBundleNames, cb));
	}

	/// <summary>
	/// キャッシュからアセットバンドルを取得します。
	/// </summary>
	/// <param name="assetBundleName">取得するアセットバンドルをひとつ指定します。</param>
	/// <param name="cb">完了時にコールバックで結果が返されます。</param>
	public void LoadAssetBundle (string assetBundleName, OnLoadComplete cb) {
		// LoadAssetBundle実行
		LoadAssetBundle(new string[] {assetBundleName}, cb);
	}

	/// <summary>
	/// 名前で指定したアセットを同期処理で取得します。
	/// </summary>
	/// <returns>ジェネリクス型で取得したアセットデータが返されます。適当な型にキャストして使用してください。</returns>
	/// <param name="bundleName">取得するアセットが含まれているアセットバンドル名を指定します。</param>
	/// <param name="assetName">取得するアセット名を指定します。</param>
	public T GetAsset<T> (string bundleName, string assetName) where T : UnityEngine.Object {
		// 初期化済みかどうかチェック
		if (!IsEnabled) return default (T);
		// アセットバンドルがロードされているか確認
		if (bundleDic == null) {
			Debug.LogError ("Could not load " + assetName + ". because " + bundleName + " has not loaded.");
			return default (T);
		}
		else {
			// ロードしたリストに存在するかどうかチェック
			foreach (string name in GetAllAssetBundleName()) {
				if (bundleName == name) {
					// アセットバンドルをロード
					AssetBundle bundle = bundleDic [bundleName];
					// object型として返す
					return bundle.LoadAsset<T> (assetName);
				}
			}
		}
		Debug.LogError ("Could not load " + assetName + ". because " + bundleName + " has not loaded.");
		return default (T);
	}

	/// <summary>
	/// 名前で指定したアセットを同期処理で取得します。
	/// </summary>
	/// <returns>object型で取得したアセットデータが返されます。適当な型にキャストして使用してください。</returns>
	/// <param name="bundleName">取得するアセットが含まれているアセットバンドル名を指定します。</param>
	/// <param name="assetName">取得するアセット名を指定します。</param>
	[Obsolete ("Use GetAsset<T>")]
	public UnityEngine.Object GetAsset (string bundleName, string assetName) {
		// 初期化済みかどうかチェック
		if (!IsEnabled) return null;
		// Object型として取得して返す
		return GetAsset<UnityEngine.Object> (bundleName, assetName);
	}

	/// <summary>
	/// 名前で指定したアセットを非同期処理で取得します。
	/// </summary>
	/// <param name="bundleName">取得するアセットが含まれているアセットバンドル名を指定します。</param>
	/// <param name="assetName">取得するアセット名を指定します。</param>
	/// <param name="cb">Cb.</param>
	public void GetAssetAsync (string bundleName, string assetName, OnAsyncLoadAssetComplete cb) {
		// 初期化済みかどうかチェック
		if (!IsEnabled) return;

		// アセットバンドルがロードされているか確認
		if (bundleDic == null) {
			Debug.LogError ("It has not been initialized. Please be call Initialize() in advance.");
			cb (null, false);
		}
		else {
			foreach (string name in GetAllAssetBundleName()) {
				if (bundleName == name) {
					// アセットバンドルをロード
					StartCoroutine (AsyncLoadAsset (bundleName, assetName, cb));
					return;
				}
			}
		}
		cb (null, false);
	}

	/// <summary>
	/// 現在ロードされているアセットバンドル名を全て取得します。
	/// </summary>
	public string[] GetAllAssetBundleName () {
		// 初期化済みかどうかチェック
		if (!IsEnabled) return null;

		// アセットバンドルがロードされているか確認
		if (bundleDic == null) {
			Debug.LogError ("It has not been initialized. Please be call Initialize() in advance.");
			return null;
		}
		else {
			// アセットバンドル名を取得
			List<string> nameList = new List<string>();
			foreach (KeyValuePair<string, AssetBundle> pair in bundleDic) {
				// Listに追加する
				nameList.Add(pair.Key);
			}
			return nameList.ToArray ();
		}
	}


	/// <summary>
	/// 名前で指定したアセットバンドルをメモリから破棄します。
	/// 指定がない場合は全てのアセットバンドルをメモリから破棄します。
	/// </summary>
	public void Unload () {
		// 初期化済みかどうかチェック
		if (!IsEnabled) return;

		// 全て破棄する
		foreach (KeyValuePair<string, AssetBundle> pair in bundleDic) {
			pair.Value.Unload (false);
		}
		// キーを破棄する
		if (bundleDic != null) {
			bundleDic.Clear();
		}
	}

	/// <summary>
	/// 名前で指定したアセットバンドルを破棄します。
	/// 指定がない場合は全てのアセットバンドルを破棄します。
	/// </summary>
	public void Unload (string bundleName) {
		// 初期化済みかどうかチェック
		if (!IsEnabled) return;

		// 指定されたアセットバンドルを破棄
		bundleDic [bundleName].Unload (false);
		// Dictionaryからも削除する
		bundleDic.Remove (bundleName);
	}

	#endregion PUBLIC_METHOD


	// ========
	#region PRIVATE_CORUTINE_METHOD

	// キャッシュからアセットバンドルをロードする
	private IEnumerator Load (List<string> urlList, string[] assetBundleNames, OnLoadComplete cb) {
		// キャッシュできる状態か確認
		while (!Caching.ready)
			yield return null;

		// ロードする
		int index = 0;
		do {
			// ロードされているかどうかチェック
			if (!bundleDic.ContainsKey(assetBundleNames [index])) {
				// キャッシュからアセットバンドルをロードする
				WWW www = WWW.LoadFromCacheOrDownload (urlList [index], ver);
				// ロードを待つ
				yield return www;

				// エラー処理
				if (www.error != null) {
					cb (false, www.error);	// ロード失敗
					throw new Exception ("error : " + www.error);
				}
				// ロードしたアセットバンドルをセット
				bundleDic.Add (assetBundleNames [index], www.assetBundle);
				// wwwを解放する
				www.Dispose ();
			}
		} while (++index < assetBundleNames.Length);

		cb (true, null);
	}


	// サーバからアセットバンドルをダウンロードする
	private IEnumerator Download (string[] assetBundleNames, OnDownloadProgressUpdate update) {
		// キャッシュできる状態か確認
		while (!Caching.ready)
			yield return null;

		// アセットバンドルを全てダウンロードするまで回す
		fileIndex = 0;
		do {
			// iOSとAndroidでアセットバンドルのディレクトリを分ける
			string url = baseURL + assetBundleNames[fileIndex];

			// CRCチェックを行うか確認
			// manifestファイルをDL
			WWW wwwManifest = new WWW(url + ".manifest");
			// ダウンロードを待つ
			yield return wwwManifest;

			// manifestが存在していた場合はCRCチェックをする
			if (wwwManifest.error == null) {
				// manifest内部のCRCコードを抽出する
				string[] lines = wwwManifest.text.Split(new string[]{"CRC: "}, StringSplitOptions.None);
				uint crc = uint.Parse(lines[1].Split(new string[]{"\n"}, StringSplitOptions.None)[0]);

				Debug.Log("CRC : "+crc);

				// CRCチェックしてダウンロード開始
				using(WWW www = WWW.LoadFromCacheOrDownload (url, ver, crc)) {
					// ダウンロードが完了するまでプログレスを更新する
					while(!www.isDone) {
						// 更新する
						update(www.progress, fileIndex, false, www.error);
						yield return new WaitForEndOfFrame();
					}

					// エラー処理
					if (www.error != null) {
						// 完了通知
						update (www.progress, fileIndex, false, www.error);
						throw new Exception("WWW download had an error:" + www.error);
					}
					// ロードしたアセットバンドルをセット
					bundleDic.Add (assetBundleNames [fileIndex], www.assetBundle);
					// wwwを解放する
					www.Dispose ();
				}

			}
			else {
				Debug.Log(assetBundleNames[fileIndex]+".manifest has not found.");

				// ダウンロード開始
				using(WWW www = WWW.LoadFromCacheOrDownload (url, ver)){
					// ダウンロードが完了するまでプログレスを更新する
					while(!www.isDone) {
						// 更新する
						update(www.progress, fileIndex, false, www.error);
						yield return new WaitForEndOfFrame();
					}

					// エラー処理
					if (www.error != null) {
						// 完了通知
						update (www.progress, fileIndex, false, www.error);
						throw new Exception("WWW download had an error:" + www.error + "\nURL : " + AssetBundleDirectoryURL);
					}
					// ロードしたアセットバンドルをセット
					bundleDic.Add (assetBundleNames [fileIndex], www.assetBundle);
					// wwwを解放する
					www.Dispose ();
				}
			}
		} while(++fileIndex < assetBundleNames.Length); 

		// 完了通知
		update (1f, fileIndex, true, null);
	}


	// 非同期でアセットを取得する
	private IEnumerator AsyncLoadAsset (string bundleName, string assetName, OnAsyncLoadAssetComplete cb) {
		// アセットバンドルをロード
		AssetBundle bundle = bundleDic [bundleName];
		// 非同期でアセットをロードする
		AssetBundleRequest request = bundle.LoadAssetAsync (assetName);
		// 取得するまで待つ
		yield return request;

		// 取得成功
		cb(request.asset, true);
	}

	#endregion PRIVATE_CORUTINE_METHOD
}