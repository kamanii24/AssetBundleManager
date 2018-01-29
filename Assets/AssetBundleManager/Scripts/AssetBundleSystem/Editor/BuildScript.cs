using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class BuildScript
{
    const string kAssetBundlesOutputPath = "AssetBundles";

    // 暗号化
    public static bool isAESCryption = false;

    public static void BuildAssetBundles(BuildAssetBundleOptions buildOptions = BuildAssetBundleOptions.None)
    {
        // 暗号化AssetBundle生成処理
        if (isAESCryption)
        {
            Debug.Log("AES Cryption Building.");
            BuildAssetBundlesForAES(buildOptions);
            return;
        }
        else
        {
            Debug.Log("Standard Building.");
        }

        // Choose the output path according to the build target.
        string outputPath = Path.Combine(kAssetBundlesOutputPath, BaseLoader.GetPlatformFolderForAssetBundles(EditorUserBuildSettings.activeBuildTarget));
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        BuildPipeline.BuildAssetBundles(outputPath, buildOptions, EditorUserBuildSettings.activeBuildTarget);
    }

    public static void BuildPlayer()
    {
        var outputPath = EditorUtility.SaveFolderPanel("Choose Location of the Built Game", "", "");
        if (outputPath.Length == 0)
            return;

        string[] levels = GetLevelsFromBuildSettings();
        if (levels.Length == 0)
        {
            Debug.Log("Nothing to build.");
            return;
        }

        string targetName = GetBuildTargetName(EditorUserBuildSettings.activeBuildTarget);
        if (targetName == null)
            return;

        // Build and copy AssetBundles.
        BuildScript.BuildAssetBundles();
        BuildScript.CopyAssetBundlesTo(Path.Combine(Application.streamingAssetsPath, kAssetBundlesOutputPath));

        BuildOptions option = EditorUserBuildSettings.development ? BuildOptions.Development : BuildOptions.None;
        BuildPipeline.BuildPlayer(levels, outputPath + targetName, EditorUserBuildSettings.activeBuildTarget, option);
    }

    public static string GetBuildTargetName(BuildTarget target)
    {
        switch (target)
        {
            case BuildTarget.Android:
                return "/test.apk";
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                return "/test.exe";
            case BuildTarget.StandaloneOSXIntel:
            case BuildTarget.StandaloneOSXIntel64:
            case BuildTarget.StandaloneOSX:
                return "/test.app";
#if !UNITY_2017_1_OR_NEWER
        case BuildTarget.WebPlayer:
		case BuildTarget.WebPlayerStreamed:
			return "";
			// Add more build targets for your own.
#endif
            default:
                Debug.Log("Target not implemented.");
                return null;
        }
    }

    static void CopyAssetBundlesTo(string outputPath)
    {
        // Clear streaming assets folder.
        FileUtil.DeleteFileOrDirectory(Application.streamingAssetsPath);
        Directory.CreateDirectory(outputPath);

        string outputFolder = BaseLoader.GetPlatformFolderForAssetBundles(EditorUserBuildSettings.activeBuildTarget);

        // Setup the source folder for assetbundles.
        var source = Path.Combine(Path.Combine(System.Environment.CurrentDirectory, kAssetBundlesOutputPath), outputFolder);
        if (!System.IO.Directory.Exists(source))
            Debug.Log("No assetBundle output folder, try to build the assetBundles first.");

        // Setup the destination folder for assetbundles.
        var destination = System.IO.Path.Combine(outputPath, outputFolder);
        if (System.IO.Directory.Exists(destination))
            FileUtil.DeleteFileOrDirectory(destination);

        FileUtil.CopyFileOrDirectory(source, destination);
    }

    static string[] GetLevelsFromBuildSettings()
    {
        List<string> levels = new List<string>();
        for (int i = 0; i < EditorBuildSettings.scenes.Length; ++i)
        {
            if (EditorBuildSettings.scenes[i].enabled)
                levels.Add(EditorBuildSettings.scenes[i].path);
        }

        return levels.ToArray();
    }


    /// <summery>
    /// 暗号化AssetBundle生成
    /// </summery>
    public static void BuildAssetBundlesForAES(BuildAssetBundleOptions buildOptions = BuildAssetBundleOptions.None)
    {
        // Resourcesから暗号化キーを取得
        AESConfig config = Resources.Load<AESConfig>("AESConfig");


        // ------------
        // 暗号化情報が正しいか確認

        if (config == null)
        {
            Debug.LogError("Dosen't find \"AESConfig.asset\".");
            return;
        }
        else if (string.IsNullOrEmpty(config.Password) || string.IsNullOrEmpty(config.Salt))
        {
            Debug.LogError("Please set AES Password and Salt. AES configuration menu is [AssetBundles/Open AES Config]");
            return;
        }
        else if (config.Salt.Length < 8)
        {
            Debug.LogError("AES Salt is must over 8 chars.");
            return;
        }
        // ------------


        // 一時ディレクトリ作成
        string dirName = "____CryptingABs";
        string tmpPath = "Assets/" + dirName;
        if (!Directory.Exists(tmpPath))
        {
            // 作成
            Directory.CreateDirectory(tmpPath);
        }
        // フォルダの中身が存在していた場合は削除
        else if (Directory.GetFileSystemEntries(tmpPath).Length > 0)
        {
            DeleteTemporaryFiles(tmpPath, true);
        }

        // AssetBundleBuild 1回目    
        BuildPipeline.BuildAssetBundles(tmpPath, buildOptions, EditorUserBuildSettings.activeBuildTarget);


        // ------------
        // 書き出されたAssetBundleに暗号化を施す処理

        // 保存したファイル一覧を取得
        string[] files = Directory.GetFiles(tmpPath);

        // AssetBundleのリネーム処理
        foreach (string str in files)
        {
            // AssetBundle本体に.bytes拡張子を付与しつつ、不要ファイルを削除
            if (!(str.Contains(".manifest") || str.Contains(".meta")))
            {
                // ディレクトリ名と同じファイル (____CryptingABs) だった場合は削除
                string[] s = str.Split('/');
                if (s[s.Length - 1] == dirName)
                {
                    File.Delete(str);
                }
                else
                {
                    File.Move(str, str + ".bytes"); // リネーム
                }
            }
            else
            {
                File.Delete(str);   // 削除
            }
        }
        // 再度、AssetBundle全ファイルのパスを取得        
        files = Directory.GetFiles(tmpPath);

        // ------------
        // 書き出されたAssetBundleを暗号化して、再度暗号化済みAssetBundleを書き出す処理

        AssetBundleBuild[] buildMap = new AssetBundleBuild[files.Length];
        // 暗号化処理実行
        for (int i = 0; i < files.Length; i++)
        {
            string file = files[i];

            // 暗号化符号作成
            string[] s = file.Split('/');
            string cryptoSign = Path.Combine(tmpPath, AssetBundleManager.CRYPTO_SIGN + s[s.Length - 1]);
            StreamWriter sign = File.CreateText(cryptoSign);
            sign.Close();

            byte[] plain = File.ReadAllBytes(file);         // byteデータ取得
            byte[] encData = AESCryption.Encryption(plain); // 暗号化
            File.WriteAllBytes(file, encData);              // 暗号化済みAssetBundleを書き出す

            // BuildMap設定
            string[] str = file.Split(new Char[] { '/', '.' });
            string name = str[str.Length - 2];
            Debug.Log("BuildTargetAsset : " + name);

            buildMap[i].assetBundleName = name;
            buildMap[i].assetNames = new string[] { file, cryptoSign };
        }

        // 一度プロジェクトをリセットして、暗号化したAssetBundleを反映させる
        AssetDatabase.Refresh();

        // 暗号化済みAssetBundle保存パス
        string absOutputPath = Path.Combine(kAssetBundlesOutputPath, BaseLoader.GetPlatformFolderForAssetBundles(EditorUserBuildSettings.activeBuildTarget));
        if (!Directory.Exists(absOutputPath))
            Directory.CreateDirectory(absOutputPath);

        // AssetBundleBuild 2回目
        BuildPipeline.BuildAssetBundles(absOutputPath, buildMap, buildOptions, EditorUserBuildSettings.activeBuildTarget);

        // 一時ファイルの削除
        DeleteTemporaryFiles(tmpPath);
        // 完了
        AssetDatabase.Refresh();

        Debug.Log("Successful creation of encrypted AssetBundles.");
    }


    // 一時ファイルの削除
    private static void DeleteTemporaryFiles(string tmpPath, bool isEntriesOnly = false)
    {
        // ディレクトリ以外の全ファイルを削除
        string[] filePaths = Directory.GetFiles(tmpPath);
        foreach (string p in filePaths)
        {
            File.SetAttributes(p, FileAttributes.Normal);
            File.Delete(p);
        }

        if (!isEntriesOnly) // ディレクトリの中身のみ削除したいかどうか
        {
            // 中が空になったらディレクトリ自身も削除
            Directory.Delete(tmpPath, false);
        }
    }
}