using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DownloadSceneDirector : MonoBehaviour {

	// プログレス
	public Text count;
	public Text per;
	public Image progressImg;


	// Use this for initialization
	void Start () {
		// デバッグ用
		#if DEBUG
		Caching.CleanCache ();
		#endif

		// アセットバンドルマネージャインスタンス取得
		AssetBundleManager bundleMng = AssetBundleManager.Instance;
		// 初期値設定
		#if UNITY_ANDROID
		bundleMng.Initialize ("https://dl.dropboxusercontent.com/u/91930162/github/Android/", 1);
		#elif UNITY_IOS
		bundleMng.Initialize ("https://dl.dropboxusercontent.com/u/91930162/github/iOS/", 1);
		#endif
		// ダウンロード開始
		string[] bundleNames = { "unitychan_std", "unitychan_crs", "unitychan_baseassets" };
		bundleMng.DownloadAssetBundle (bundleNames, ((float progress, int fileIndex, bool isComplete) => {
			if (!isComplete) {
				// テキストプログレス更新
				int prg = (int)(progress * 100f);
				per.text = prg.ToString () + "%";
				// プログレスバー更新
				progressImg.fillAmount = progress;
				// ファイル数更新
				count.text = fileIndex + "/" + bundleNames.Length;
			}
			else {
				Debug.Log ("ダウンロード完了");
				// シーンロード
				Application.LoadLevel("MainScene");
			}
		}));
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
