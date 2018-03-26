# AssetBundleManager
#### 含まれている機能
- AssetBundleのダウンロード・差分更新
- AssetBundleビルド時に圧縮形式の選択(LZMA, LZ4, 非圧縮)
- AESによるAssetBundleの暗号化・復号化

#### プロジェクトの概要
AssetBundleに関する必要な処理をまとめたサンプルプロジェクトです。  
*DownloadScene*、*AssetBundleScene*の2シーン構造です。
*DownloadScene*でサーバからAssetBundleをダウンロード後、*AssetBundleScene*で取得したアセットを表示します。  
*※サンプルでダウンロードするAssetBundleにユニティちゃんアセットを使用しています。*

# 使い方
#### 初期設定<br>
初回に**Initialize**をコールし、AssetBundleが含まれるサーバ上のディレクトリとAssetBundleのバージョンを指定します。
バージョン指定は任意です。
一度設定すれば以降、値の変更しない限り設定する必要はありません。

`AssetBundleManager.Initialize (bundleDirURL, 1);`

<br>

#### AssetBundleのダウンロード<br>
**DownloadAssetBundle**でダウンロードするAssetBundle名を指定します。  
コールバックデリゲートでダウンロードの進捗値を取得できます。

    string[] bundleNames = { bundle_1, bundle_2, bundle_3 };
    AssetBundleManager.DownloadAssetBundle (bundleNames, ((float progress, int fileIndex, bool isComplete, string error) => {
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

#### AssetBundleのロード<br>
**LoadAssetBundle**でロードするAssetBundle名を指定します。  
ロードの完了通知はコールバックデリゲートで受け取れます。

    string[] bundleNames = { bundle_1, bundle_2 };
    AssetBundleManager.LoadAssetBundle (bundleNames, ((bool isSuccess, string error) => {
       if (isSuccess) {
           Debug.Log("ロード成功");
        }
        else {
           Debug.Log("ロード失敗 : "+error);
        }
    }));

<br>

#### アセットの取得<br>
ロードしたAssetBundleから必要なアセットを取得するには**GetAsset**に取得したいアセット名と、それが含まれるAssetBundle名をしていします。  
取得したアセットはObject型なので適当な型にキャストします。<br>

*型指定なし*<br>

    Object obj = AssetBundleManager.GetAsset (bundleName, assetName);

*ジェネリック型指定*<br>

    AudioClip clip = AssetBundleManager.GetAsset<AudioClip> (bundleName, assetName);

*非同期*<br>

    GetAssetAsync (bundleName, assetName, ((Object asset, bool isSuccess) => {
       if (isSuccess) {
           Instantiate ((GameObject)asset, Vector3.zero, Quaternion.identity);
       }
    }));

<br>

#### ロードされているAssetBundleを確認する<br>
**GetAllAssetBundleName**で現在ロードされているAssetBundleをstring配列で全て取得できます。

    string[] bundles = GetAllAssetBundleName();

<br>

#### AssetBundleを破棄する<br>
ロードしたAssetBundleは明示的に破棄するまでメモリに保持され続けるため、不要になったAssetBundleは**Unload**で破棄する必要があります。

    AssetBundleManager.Unload();

AssetBundle名を指定して個別に破棄することも可能です。

    AssetBundleManager.Unload(bundleName);

<br>

#### 暗号化したAssetBundleをビルドする<br>
Unityのメニューバー *AssetBundles/Open Config* を選択し、暗号化のためのキー文字列を設定します。<br>
Saltは8文字以上である必要があります。<br>
![Imgur](https://i.imgur.com/rRgdUCq.png)<br>

キー文字列の設定後、*AssetBundles/Enable AES Cryption* にチェックをつけて *Build AssetBundles* でビルドします。<br>
![Imgur](https://i.imgur.com/X8Oc34r.png)<br>

暗号化されたAssetBundleもダウンロード時に自動的に復号され、通常のAssetBundleと同等に扱うことができます。<br>

<br>

## リリースノート
#### - 2018/3/26
* **エディタメニューのEnable AES Cryptionのチェックがビルドをする度に外れる問題を修正**<br>

#### - 2018/1/29
* **AESによるAssetBundleの暗号化機能の追加**<br>

#### - 2017/11/2
* **Unity2017.1以降のバージョンでCaching.ClearAllCachedVersionsを追加**<br>

#### - 2017/11/1
* **AssetBundleManagerをstaticクラスへ変更**<br>
前回のバージョンから使用されていた場合はInstanceを外してください。

#### - 2017/10/30
* **AssetBundleのダウンロード処理をWWWからUnityWebRequestへ変更**<br>

* **Initializeのバージョンを任意指定へ変更**<br>
指定しない場合のバージョンはデフォルトで 1 です。

* **AssetBundleビルド時に圧縮方法をメニューに追加**<br>
Build AssetbundlesにLZMA、LZ4、UnCompress(非圧縮)の3項目を追加しました。

#### - 2017/8/4
* **Unity2017対応**<br>
errorチェック処理をIsNullOrEmptyでの判定に変更。

#### - 2016/8/3
* **GetAssetの戻り値をジェネリック型で指定できるオーバーロードメソッドの追加**<br>
GetAsset<T>で取得できるオブジェクトを型指定できるようになりました。<br>
取得したオブジェクトを各型にキャストする必要がなくなります。

* **型指定なしのGetAssetの戻り値をUnityEngine.Objectへ変更**

#### - 2016/1/28<br>
* **オーバーロードメソッドの追加**<br>
DonwloadAssetBundleとLoadAssetBundleに配列ではないstring変数として引数に渡すことができるオーバーロードメソッドを追加しました。<br>
これにより単一のAssetBundleの読み込みがより効率化します。

* **初期化チェック**<br>
AssetBundleManagerのメソッドをコールしたタイミングで初期化チェックが行われるようになりました。
初期化が行われずに各メソッドが実行された場合は警告がコンソールに出力されます。

#### - 2015/10/27<br>
* **CRCを照合して更新されたAssetBundleのみをダウンロードされる機能の追加**<br>
AssetBundleのビルド時に生成される.manifestファイルをAssetBundle本体と併せて同ディレクトリに置くことで、AssetBundleのダウンロード実行時に更新されたAssetBundleのみをダウンロードされるように改修しました。<br>
指定ディレクトリに.manifestファイルがない場合は以前の通りCRCの照合なしで実行されます。

## ビルド環境
Unity 2017.3.0p3<br>
macOS High Sierra 10.13.3

<br><br>
## Unity-Chan ライセンス
本リポジトリには、UnityChanがAssetsとして含まれています。 以下のライセンスに従います。

<div><img src="http://unity-chan.com/images/imageLicenseLogo.png" alt="ユニティちゃんライセンス"><p>このアセットは、『<a href="http://unity-chan.com/contents/license_jp/" target="_blank">ユニティちゃんライセンス</a>』で提供されています。このアセットをご利用される場合は、『<a href="http://unity-chan.com/contents/guideline/" target="_blank">キャラクター利用のガイドライン</a>』も併せてご確認ください。</p></div>
