# AssetBundleManager

# ![Imgur](https://i.imgur.com/GD4U1oj.gif)

## コンセプト

AssetBundleManagerは初めてAssetBundleを扱い人に向けて、とにかくシンプルに手間をかけず導入できるものとして開発しました。

スクリプト本体のコードも見通しをよくし、各々のユースケースに合わせたカスタマイズの余地も残しておりますので、じゃんじゃんForkして弄ってください。



## 初期設定
AssetBundleManagerを使用するクラスに using を追加します。

`using KM2;`

## AssetBundleManagerの初期化

AssetBundleManagerを使用する前には必ずInitializeメソッドをコールし、初期化を行う必要があります。

<<<<<<< HEAD
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
        
        // 第二引数のbool値がtrueの場合、AssetBundleダウンロード後に自動的にメモリへ読み込ませます。
        AssetBundleManager.DownloadAssetBundle(downloadAssetBundles, true, Downloading);
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
=======
### AssetBundleManifestとAssetBundleが同じ階層に存在する場合

Initializeメソッドの引数に、AssetBundleManifest(AssetBundleビルド時に生成されるプラットフォーム名が付いたファイル)のパスを指定します。

引数にManifestのパスのみを指定した場合、AssetBundleがManifestと同階層にあるとして処理されます。

一度設定すれば以降、設定する必要はありません。

```c#
AssetBundleManager.Initialize(manifestURL, (bool isComplete)=>
{
	// 初期化完了
});
```

### AssetBundleManifestとAssetBundleが異なる階層に存在する場合

AssetBundleがManifestファイルと異なる階層に存在する場合は、引数としてassetBundleDirectoryURLを指定することができます。

こちらも一度設定すれば以降、値の変更しない限り設定し直す必要はありません。

```c#
AssetBundleManager.Initialize(manifestURL, assetBundleDirectoryURL, (bool isComplete)=>
{
	// 初期化完了
});
```
## AssetBundleのダウンロード

*UnityWebRequest* によるGET通信で *AssetBundleManager.Initialize* で指定したリモートディレクトリから *downloadAssetBundles* 指定したAssetBundleをダウンロードします。  
コールバック引数でダウンロード対象のAssetBundleのファイルサイズを受け取れるので、進捗値をディスプレイすることが可能です(実装は下記参照)。
    

```C#
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
>>>>>>> dev
    }
}
```


## AssetBundleのロード
### 関数
`LoadAssetBundle(string loadAssetBundle, AssetBundleLoaded handler);`
`LoadAssetBundle(string[] loadAssetBundles, AssetBundleLoaded handler);`
### デリゲート
`AssetBundleLoaded(AssetBundle[] loadedAssetBundles, string error);`

ロードの完了通知はコールバックデリゲートで受け取れます。
**loadAssetBundles** で指定したAssetBundleがキャッシュ内に存在しない場合は、ダウンロードします。
**DownloadAssetBundle**との違いは、delegateで完了通知だけを受け取ることができるので、ダウンロードのプログレス更新が不要な場合や解放されているAssetBundleを個別にロードする場合に使用されます。

<<<<<<< HEAD
### 実装
    private void Start()
    {
        string[] loadAssetBundles = { bundle_1, bundle_2, bundle_3 };
        AssetBundleManager.LoadAssetBundle(loadAssetBundles, Loaded);
    }
    
    // AssetBundleのロード完了
    private void Loaded(AssetBundle[] assetBundles, string error)
=======
```C#
private void Start()
{
    string[] loadAssetBundles = { bundle_1, bundle_2, bundle_3 };
    AssetBundleManager.LoadAssetBundle(loadAssetBundles, Loaded);
}

// AssetBundleのロード完了
private void Loaded(AssetBundle[] assetBundles, string error)
{
    if(error != null)
>>>>>>> dev
    {
        foreach(var ab in assetBundles) print(ab.name + " is loaded.");
    }
}
```



## ロードしたAssetBundleからアセットを取得する
**型指定なし**

```C#
Object obj = AssetBundleManager.GetAsset (bundleName, assetName);
```

**ジェネリック型指定**

```C#
AudioClip clip = AssetBundleManager.GetAsset<AudioClip> (bundleName, assetName);
```

**非同期 ジェネリック型指定**

```C#
GetAssetAsync<GameObject> (bundleName, assetName, (GameObject go) => {
   if (go != null) {
       Instantiate (go, Vector3.zero, Quaternion.identity);
   }
});
```

## ロードされているAssetBundleを確認する
**AssetBundle本体**

```C#
AssetBundle[] ab = AssetBundleManager.GetAllLoadedAssetBundles();
```

**AssetBundle名**

```C#
string[] abNames = AssetBundleManager.GetAllLoadedAssetBundleNames();
```

## AssetBundleを破棄する
ロードしたAssetBundleは明示的に破棄するまでメモリに保持され続けるため、不要になったAssetBundleは**Unload**で破棄する必要があります。

```C#
AssetBundleManager.Unload(true);    // trueにした場合、AssetBundleからロード済みのアセットも破棄されます
```

AssetBundle名を指定して個別に破棄することも可能です。

```C#
AssetBundleManager.Unload(true, bundleName);
```



# ビルド環境
<<<<<<< HEAD
Unity 2018.2.11f1  
macOS Mojave 10.14.2
=======
Unity 2018.4.8f1
macOS Mojave 10.14.6
>>>>>>> dev
