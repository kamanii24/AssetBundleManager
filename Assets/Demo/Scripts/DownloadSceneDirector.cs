using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class DownloadSceneDirector : MonoBehaviour
{
    // 起動時にDL済みのAssetBundleを削除するかどうか
    public bool isCleanCache = false;
    // AssetBundleのベースURL
    public string baseURL = "http://";
    // ダウンロード対象のアセットバンドル
    public string[] bundleNames = { "unitychan_std", "unitychan_crs", "unitychan_baseassets" };
    // プログレス
    [Header ("[UI Contents]")]
    [SerializeField] private Text notice;
    [SerializeField] private Text count;
    [SerializeField] private Text per;
    [SerializeField] private Image progressImg;
    [SerializeField] private GameObject retryBtn;

    // Use this for initialization
    void Start()
    {
        // AssetBundleキャッシュを削除
        if (isCleanCache)
        {
            Caching.ClearCache();
        }

        // ダウンロード開始 ====
        // 初期値設定
        AssetBundleManager.Initialize(baseURL);
        // ダウンロード開始
        AssetBundleManager.DownloadAssetBundle(bundleNames, OnDownloading);

        // リトライボタンを無効化
        retryBtn.SetActive(false);
        notice.text = "NOW LOADING";
    }

    // Update is called once per frame
    void Update()
    {

    }

    // ダウンロード実行中
    private void OnDownloading(float progress, int fileIndex, bool isComplete, string error)
    {
        // エラー処理
        if (!string.IsNullOrEmpty(error))
        {
            // リトライボタンアクティブ
            retryBtn.SetActive(true);
            notice.text = "FAILED";

            // その他無効化
            count.enabled = false;
            per.enabled = false;
            progressImg.enabled = false;

            Debug.Log("ダウンロードエラー : " + error);
        }

        // 進捗更新
        if (!isComplete)
        {
            // テキストプログレス更新
            int prg = (int)(progress * 100f);
            per.text = prg.ToString() + "%";
            // プログレスバー更新
            progressImg.fillAmount = progress;
            // ファイル数更新
            int index = fileIndex + 1;
            count.text = index + "/" + bundleNames.Length;
        }
        else
        {
            // ダウンロード完了
            notice.text = "COMPLETE";
            per.text = "100%";
            progressImg.fillAmount = 1f;
            Debug.Log("ダウンロード完了");

            // ダウンロード完了テキストを見せるための遅延処理
            StartCoroutine(WaitForScene());
        }
    }
    
    // 遅延処理
    IEnumerator WaitForScene()
    {
        // 1秒待ってから遷移
        yield return new WaitForSeconds(1f);

        // 遷移
        SceneManager.LoadScene ("MainScene");
    }
}
