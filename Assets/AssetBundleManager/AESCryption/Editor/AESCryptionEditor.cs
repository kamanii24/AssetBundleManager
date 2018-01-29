// =================================
//
//	AESCryptionEditor.cs
//	Created by Takuya Himeji
//
// =================================

using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;

public class AESCryptionEditor
{
    // 暗号化
    [MenuItem("Assets/AES Cryption/Encryption")]
    static void Encryption()
    {
        // 保存先のパス設定
        string outputPath = "AESCryption/Encrypt";
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        // エディタ上で選択されたオブジェクトを取得
        Object[] objs = Selection.objects;
        
        // 暗号化ファイルの保存
        foreach (Object obj in objs)
        {
            TextAsset t = (TextAsset)obj;
            byte[] data = AESCryption.Encryption(t.bytes);
            string path = outputPath + "/" + t.name + ".bytes";
            File.WriteAllBytes(path, data);
        }

        Debug.Log("[AESCryption]: Encryption completed.");
    }


    // 復号化
    [MenuItem("Assets/AES Cryption/Decryption")]
    static void Decryption()
    {
        // 保存先のパス設定
        string outputPath = "AESCryption/Dencrypt";
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        // エディタ上で選択されたオブジェクトを取得
        Object[] objs = Selection.objects;
        
        // 復号化ファイルの保存
        foreach (Object obj in objs)
        {
            TextAsset t = (TextAsset)obj;
            byte[] data = AESCryption.Decryption(t.bytes);
            string path = outputPath + "/" + t.name + ".bytes";
            File.WriteAllBytes(path, data);
        }

        Debug.Log("[AESCryption]: Decryption completed.");
    }
}