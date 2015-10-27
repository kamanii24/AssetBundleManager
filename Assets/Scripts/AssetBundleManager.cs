using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class AssetBundleManager : MonoBehaviour {

	// ----------------------------------------------------------------------------
	#region SINGLETON

	// シングルトン宣言
	private static AssetBundleManager mInstance;

	// コンストラクタ
	private AssetBundleManager() {
		Debug.Log("AssetBundleManager initialized.");
	}

	/// <summary>
	/// 初回はInitializeで初期値を設定します。
	/// </summary>
	public static AssetBundleManager Instance {
		get {
			if (mInstance == null) {
				GameObject gObj = new GameObject("AssetBundleManager");
				mInstance = gObj.AddComponent<AssetBundleManager>();
			}
			return mInstance;
		}
	}

	#endregion // SINGLETON


	// ----------------------------------------------------------------------------
	#region PUBLIC_DELEGATE

	// アセットバンドルダウンロードプログレス更新用
	public delegate void OnDownloadProgressUpdate(float progress, int fileIndex, bool isComplete, string error);
	// アセットバンドルロード完了通知用
	public delegate void OnLoadComplete(bool isSuccess, string error);
	// 非同期アセット取得完了通知用
	public delegate void OnAsyncLoadAssetComplete(object asset, bool isSuccess);

	#endregion // PUBLIC_DELEGATE


	// ----------------------------------------------------------------------------
	#region PRIVATE_MEMBER_VARIABLES

	// 初期設定変数
	private static string baseURL;	// アセットバンドルディレクトリURL
	private static int ver;			// バージョン
	// アセットバンドル保管用Dictionary
	private static Dictionary<string, AssetBundle> bundleDic = null;
	// ダウンロードファイルカウント
	private int fileIndex = 0;

	#endregion // PRIVATE_MEMBER_VARIABLES


	// ----------------------------------------------------------------------------
	#region PUBLIC_PROPETY

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

	#endregion // PUBLIC_PROPETY


	// ----------------------------------------------------------------------------
	#region PUBLIC_METHOD

	/// <summary>
	/// 初期設定をします。
	/// </summary>
	public void Initialize (string assetBundleDirectoryURL, int version) {
		// URLとバージョンをセット
		AssetBundleDirectoryURL = assetBundleDirectoryURL;
		Version = version;
	}

	/// <summary>
	/// サーバからアセットバンドルをダウンロードします。
	/// </summary>
	public void DownloadAssetBundle (string[] assetBundleNames, OnDownloadProgressUpdate update) {
		//  ダウンロード開始
		StartCoroutine (Download (assetBundleNames, update));
	}

	/// <summary>
	/// キャッシュからアセットバンドルを取得します。
	/// </summary>
	public void LoadAssetBundle (string[] assetBundleNames, OnLoadComplete cb) {
		// Dictionary初期化
		if (bundleDic == null)
			bundleDic = new Dictionary<string, AssetBundle> ();
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
	/// 名前で指定したアセットを同期処理で取得します。
	/// </summary>
	public object GetAsset (string bundleName, string assetName) {

		Debug.Log (bundleName + " : " + assetName+" : loading");
		// アセットバンドルがロードされているか確認
		if (bundleDic == null) {
			Debug.LogError ("Could not load " + assetName + ". because " + bundleName + " has not loaded.");
			return null;
		}
		else {
			// ロードしたリストに存在するかどうかチェック
			foreach (string name in GetAllAssetBundleName()) {
				if (bundleName == name) {
					// アセットバンドルをロード
					AssetBundle bundle = bundleDic [bundleName];
					// object型として返す
					return bundle.LoadAsset (assetName);
				}
			}
		}
		Debug.LogError ("Could not load " + assetName + ". because " + bundleName + " has not loaded.");
		return null;
	}

	/// <summary>
	/// 名前で指定したアセットを非同期処理で取得します。
	/// </summary>
	public void GetAssetAsync (string bundleName, string assetName, OnAsyncLoadAssetComplete cb) {
		// アセットバンドルがロードされているか確認
		if (bundleDic == null) {
			Debug.LogError ("It has not been initialized. Please be call Initialized() in advance.");
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
		// アセットバンドルがロードされているか確認
		if (bundleDic == null) {
			Debug.LogError ("It has not been initialized. Please be call Initialized() in advance.");
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
		// 全て破棄する
		foreach (KeyValuePair<string, AssetBundle> pair in bundleDic) {
			pair.Value.Unload (false);
		}
	}

	/// <summary>
	/// 名前で指定したアセットバンドルを破棄します。
	/// 指定がない場合は全てのアセットバンドルを破棄します。
	/// </summary>
	public void Unload (string bundleName) {
		// 指定されたアセットバンドルを破棄
		bundleDic [bundleName].Unload (false);
		// Dictionaryからも削除する
		bundleDic.Remove (bundleName);
	}

	#endregion // PUBLIC_METHOD


	// ----------------------------------------------------------------------------
	#region PRIVATE_CORUTINE_METHOD

	// キャッシュからアセットバンドルをロードする
	IEnumerator Load (List<string> urlList, string[] assetBundleNames, OnLoadComplete cb) {
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
	IEnumerator Download (string[] assetBundleNames, OnDownloadProgressUpdate update) {
		// アセットバンドルを全てダウンロードするまで回す
		fileIndex = 0;
		do {
			// キャッシュできる状態か確認
			while (!Caching.ready)
				yield return null;

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
					// wwwを解放する
					www.Dispose ();
				}

			}
			else {
				Debug.Log(assetBundleNames[fileIndex]+".manifest has not found.");

				// CRCチェックしてダウンロード開始
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
						throw new Exception("WWW download had an error:" + www.error);
					}
					// wwwを解放する
					www.Dispose ();
				}
			}



		} while(++fileIndex < assetBundleNames.Length); 

		// 完了通知
		update (1f, fileIndex, true, null);
	}


	// 非同期でアセットを取得する
	IEnumerator AsyncLoadAsset (string bundleName, string assetName, OnAsyncLoadAssetComplete cb) {
		// アセットバンドルをロード
		AssetBundle bundle = bundleDic [bundleName];
		// 非同期でアセットをロードする
		AssetBundleRequest request = bundle.LoadAssetAsync (assetName);
		// 取得するまで待つ
		yield return request;

		// 取得成功
		cb(request.asset, true);
	}

	#endregion // PRIVATE_CORUTINE_METHOD
}
