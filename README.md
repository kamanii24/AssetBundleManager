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

* **____ASSET_BUNDLES**  
アセットバンドルのオリジナルファイルです。

<br>

# 使い方
#### 初期設定<br>
初回に**Initialize**をコールし、アセットバンドルが含まれるサーバ上のディレクトリとアセットバンドルのバージョンを指定します。  
一度設定すれば以降、値の変更しない限り設定する必要はありません。

`AssetBundleManager.Instance.Initialize (bundleDirURL, 1);`

<br>

#### アセットバンドルのダウンロード<br>
**DownloadAssetBundle**でダウンロードするアセットバンドル名を指定します。  
コールバックデリゲートでダウンロードの進捗値を取得できます。

    string[] bundleNames = { bundle_1, bundle_2, bundle_3 };
    AssetBundleManager.Instance.DownloadAssetBundle (bundleNames, ((float progress, int fileIndex, bool isComplete, string error) => {
        // エラー処理
        if (error != null) {
            Debug.Log("ダウンロードエラー");
        }
        // 進捗処理
        else if (isComplete) {
            Debug.Log("ダウンロード完了");
        }
        else {
            int per = (int)(progress*100f);
            Debug.Log(per+"%");
            Debug.LoG(fileIndex + "/" + bundleNames.Length);
        }
    }));

<br>

#### アセットバンドルのロード<br>
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

#### アセットの取得<br>
ロードしたアセットバンドルから必要なアセットを取得するには**GetAsset**に取得したいアセット名と、それが含まれるアセットバンドル名をしていします。  
取得したアセットはObject型なので適当な型にキャストします。<br>

*型指定なし*<br>

    Object obj = AssetBundleManager.Instance.GetAsset (bundleName, assetName);

*ジェネリック型指定*<br>

    AudioClip clip = AssetBundleManager.Instance.GetAsset<AudioClip> (bundleName, assetName);

*非同期*<br>

    GetAssetAsync (bundleName, assetName, ((Object asset, bool isSuccess) => {
       if (isSuccess) {
           Instantiate ((GameObject)asset, Vector3.zero, Quaternion.identity);
       }
    }));

<br>

#### ロードされているアセットバンドルを確認する<br>
**GetAllAssetBundleName**で現在ロードされているアセットバンドルをstring配列で全て取得できます。

    string[] bundles = GetAllAssetBundleName();

<br>

#### アセットバンドルを破棄する<br>
ロードしたアセットバンドルは明示的に破棄するまでメモリに保持され続けるため、不要になったアセットバンドルは**Unload**で破棄する必要があります。

    AssetBundleManager.Instance.Unload();

アセットバンドル名を指定して個別に破棄することも可能です。

    AssetBundleManager.Instance.Unload(bundleName);

<br>
## リリースノート

#### - 2015/10/27<br>
* **CRCを照合して更新されたアセットバンドルのみをダウンロードされる機能の追加**<br>
アセットバンドルのビルド時に生成される.manifestファイルをアセットバンドル本体と併せて同ディレクトリに置くことで、アセットバンドルのダウンロード実行時に更新されたアセットバンドルのみをダウンロードされるように改修しました。<br>
指定ディレクトリに.manifestファイルがない場合は以前の通りCRCの照合なしで実行されます。

#### - 2016/1/28<br>
* **オーバーロードメソッドの追加**<br>
DonwloadAssetBundleとLoadAssetBundleに配列ではないstring変数として引数に渡すことができるオーバーロードメソッドを追加しました。<br>
これにより単一のアセットバンドルの読み込みがより効率化します。

* **初期化チェック**<br>
AssetBundleManagerのメソッドをコールしたタイミングで初期化チェックが行われるようになりました。
初期化が行われずに各メソッドが実行された場合は警告がコンソールに出力されます。

####- 2016/8/4
* **GetAssetの戻り値をジェネリック型で指定できるオーバーロードメソッドの追加**<br>
GetAsset<T>で取得できるオブジェクトを型指定できるようになりました。<br>
取得したオブジェクトを各型にキャストする必要がなくなります。

* **型指定なしのGetAssetの戻り値をUnityEngine.Objectへ変更**

####- 2016/8/4
* **Unity2017対応**<br>
errorチェック処理をIsNullOrEmptyでの判定に変更。


<br>
## ビルド環境
Unity 5.5.3f1<br>
MacOSX El Capitan 10.12.5


<br><br><br><br>
## Unity-Chan ライセンス
本リポジトリには、UnityChanがAssetsとして含まれています。 以下のライセンスに従います。

<div><img src="http://unity-chan.com/images/imageLicenseLogo.png" alt="ユニティちゃんライセンス"><p>このアセットは、『<a href="http://unity-chan.com/contents/license_jp/" target="_blank">ユニティちゃんライセンス</a>』で提供されています。このアセットをご利用される場合は、『<a href="http://unity-chan.com/contents/guideline/" target="_blank">キャラクター利用のガイドライン</a>』も併せてご確認ください。</p></div>
