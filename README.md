# AssetBundleManager
アセットバンドルに関する必要な処理をまとめたサンプルプロジェクトです。  
*DownloadScene*、*MainScene*の２シーン構造です。  
*DownloadScene*でサーバからアセットバンドルをダウンロード後、*MainScene*で取得したアセットを表示します。  
*※サンプルでダウンロードするアセットバンドルにユニティちゃんアセットを使用しています。*

#### 自プロジェクトでの使用
**Assets/Scripts/AssetBundleManager.cs**が本体です。自プロジェクトで使用する場合は**AssetBundleManager.cs**のみを取り出してお使いください。  

#### その他ファイル
* **UnityChanScripts**  
アセットバンドルに内包されているユニティちゃんに適応されるスクリプト群です。

* **ScriptsForAssetBundleSystem**  
アセットバンドルをビルドする外部スクリプトです。  
JiYongYun様のものを改変して組み込んでいます。 <https://github.com/JiYongYun/Unity-5.0-AssetBundle-Demo>

<br>
# 使い方
#### 初期設定
初回に**Initialize**をコールし、アセットバンドルが含まれるサーバ上のディレクトリとアセットバンドルのバージョンを指定します。  
一度設定すれば以降、値の変更しない限り設定する必要はありません。

`AssetBundleManager.Instance.Initialize (bundleDirURL, 1);`

<br>
#### アセットバンドルのダウンロード
**DownloadAssetBundle**でダウンロードするアセットバンドル名を指定します。  
コールバックデリゲートでダウンロードの進捗値を取得できます。

    string[] bundleNames = { bundle_1, bundle_2, bundle_3 };`
    AssetBundleManager.Instance.DownloadAssetBundle (bundleNames, ((float progress, int fileIndex, bool isComplete, string error) => {
        // エラー処理
        if (error != null) {
            Debug.Log("ダウンロードエラー");
        }
        
        // 進捗処理
        if (isComplete) {
            Debug.Log("ダウンロード完了");
        }
        else {
            int per = (int)(progress*100f);
            Debug.Log(per+"%");
            Debug.LoG(fileIndex + "/" + bundleNames.Length);
        }
    }));`

<br>
#### アセットバンドルのロード
**LoadAssetBundle**でロードするアセットバンドル名を指定します。  
ロードの完了通知はコールバックデリゲートで受け取れます。

    string[] bundleNames = { bundle_1, bundle_2 };
    AssetBundleManager.Instance.LoadAssetBundle (bundleNames, ((bool isSuccess, string error) => {
       if (isSuccess) {
           Debug.Log("ロード成功");
        }
        else {
           Debug.Log("ロード失敗 : "+error);
        }
    }));

<br>
#### アセットの取得
ロードしたアセットバンドルから必要なアセットを取得するには**GetAsset**に取得したいアセット名と、それが含まれるアセットバンドル名をしていします。  
取得したアセットはobject型なので適当な型にキャストします。

    GameObject obj = AssetBundleManager.Instance.GetAsset (bundleName, assetName) as GameObject;

非同期で取得する場合は

    GetAssetAsync (bundleName, assetName, ((object asset, bool isSuccess) => {
       if (isSuccess) {
           Instantiate ((GameObject)asset, Vector3.zero, Quaternion.identity);
       }
    }));

<br>
#### ロードされているアセットバンドルを確認する
**GetAllAssetBundleName**で現在ロードされているアセットバンドルをstring配列で全て取得できます。

    string[] bundles = GetAllAssetBundleName();

<br>
#### アセットバンドルを破棄する
ロードしたアセットバンドルは明示的に破棄するまでメモリに保持され続けるため、不要になったアセットバンドルは**Unload**で破棄する必要があります。

    AssetBundleManager.Instance.Unload();

アセットバンドル名を指定して個別に破棄することも可能です。

    AssetBundleManager.Instance.Unload(bundleName);

#### 使用バージョン
Unity 5.1.2f1  

<br><br><br><br>
## Unity-Chan ライセンス
本リポジトリには、UnityChanがAssetsとして含まれています。 以下のライセンスに従います。

<div><img src="http://unity-chan.com/images/imageLicenseLogo.png" alt="ユニティちゃんライセンス"><p>このアセットは、『<a href="http://unity-chan.com/contents/license_jp/" target="_blank">ユニティちゃんライセンス</a>』で提供されています。このアセットをご利用される場合は、『<a href="http://unity-chan.com/contents/guideline/" target="_blank">キャラクター利用のガイドライン</a>』も併せてご確認ください。</p></div>
