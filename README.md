# AssetBundleManager


AssetBundleを使用するために必要な処理をシンプルに実装したスクリプトです。
![Imgur](https://i.imgur.com/GD4U1oj.gif)

## 初期設定
AssetBundleManagerを使用するクラスに using を追加します。

`using KM2;`

初回に**Initialize**をコールし、AssetBundleが含まれるサーバ上のディレクトリパスを指定します。
一度設定すれば以降、値の変更しない限り設定する必要はありません。

`AssetBundleManager.Initialize (bundleDirURL);`

## AssetBundleのダウンロード
### 関数
`DownloadAssetBundle(string downloadAssetBundle, AssetBundleDownloadProgress handler);`
`DownloadAssetBundle(string[] downloadAssetBundles, AssetBundleDownloadProgress handler);`
### デリゲート
`AssetBundleDownloadProgress(ulong downloadedBytes, ulong totalBytes, int fileIndex, bool completed, string error);`

**UnityWebRequest** によるGET通信で **AssetBundleManager.Initialize** で指定したリモートディレクトリから **downloadAssetBundles** 指定したAssetBundleをダウンロードします。  
コールバック引数でダウンロード対象のAssetBundleのファイルサイズを受け取れるので、進捗値をディスプレイすることが可能です(実装は下記参照)。

### 実装
    private void Start()
    {
        string[] downloadAssetBundles = { bundle_1, bundle_2, bundle_3 };
        AssetBundleManager.DownloadAssetBundle(downloadAssetBundles, Downloading);
    }

    // ダウンロード更新
    private void Downloading(ulong downloadedBytes, ulong totalBytes, int fileIndex, bool isComplete, string error)
    {
        // ダウンロードBytesサイズ更新
        print(downloadedBytes + " bytes / "+ totalBytes + "bytes");

        // ダウンロード完了
        if (isComplete)
        {
            print("Donwload completed.");
        }
    }


## AssetBundleのロード
### 関数
`LoadAssetBundle(string loadAssetBundle, AssetBundleLoaded handler);`
`LoadAssetBundle(string[] loadAssetBundles, AssetBundleLoaded handler);`
### デリゲート
`AssetBundleLoaded(AssetBundle[] loadedAssetBundles, string error);`

ロードの完了通知はコールバックデリゲートで受け取れます。
**loadAssetBundles** で指定したAssetBundleがキャッシュ内に存在しない場合は、ダウンロードします。
**DownloadAssetBundle**との違いは、delegateで完了通知だけを受け取ることができるので、ダウンロードのプログレス更新が不要な場合や解放されているAssetBundleを個別にロードする場合に使用されます。

### 実装
    private void Start()
    {
        string[] loadAssetBundles = { bundle_1, bundle_2, bundle_3 };
        AssetBundleManager.LoadAssetBundle(loadAssetBundles, Loaded);
    }
    
    // AssetBundleのロード完了
    private void Loaded(AssetBundle[] assetBundles, string error)
    {
        if(error != null)
        {
            foreach(var ab in assetBundles) print(ab.name + " is loaded.");
        }
    }



## ロードしたAssetBundleからアセットを取得する
**型指定なし**

    Object obj = AssetBundleManager.GetAsset (bundleName, assetName);

**ジェネリック型指定**

    AudioClip clip = AssetBundleManager.GetAsset<AudioClip> (bundleName, assetName);

**非同期 ジェネリック型指定**

    GetAssetAsync<GameObject> (bundleName, assetName, (GameObject go) => {
       if (go != null) {
           Instantiate (go, Vector3.zero, Quaternion.identity);
       }
    });

## ロードされているAssetBundleを確認する
**AssetBundle本体**

    AssetBundle[] ab = AssetBundleManager.GetAllLoadedAssetBundles();

**AssetBundle名**

    string[] abNames = AssetBundleManager.GetAllLoadedAssetBundleNames();

## AssetBundleを破棄する
ロードしたAssetBundleは明示的に破棄するまでメモリに保持され続けるため、不要になったAssetBundleは**Unload**で破棄する必要があります。

    AssetBundleManager.Unload(true);    // trueにした場合、AssetBundleからロード済みのアセットも破棄されます

AssetBundle名を指定して個別に破棄することも可能です。

    AssetBundleManager.Unload(true, bundleName);



# ビルド環境
Unity 2018.2.11f1  
macOS Mojave 10.14