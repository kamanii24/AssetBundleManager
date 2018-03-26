// =================================
//
//	AESCryption.cs
//	Created by Takuya Himeji
//
// =================================

using UnityEngine;
using System.IO;
using System.Text;
using System.Collections;
using System.Security.Cryptography;


public class AESCryption
{
    private const int BLOCK_SIZE = 256;
    private const int KEY_SIZE = 256;

    /// <summary>
    /// ファイルを暗号化します
    /// </summary>
    public static byte[] Encryption(byte[] data)
    {
        // Resourcesから暗号化キーを取得
        AESConfig config = Resources.Load<AESConfig>("AESConfig");

        RijndaelManaged rijndael = new RijndaelManaged();
        rijndael.KeySize = BLOCK_SIZE;
        rijndael.BlockSize = KEY_SIZE;

        byte[] bSalt = Encoding.UTF8.GetBytes(config.Salt);
        Rfc2898DeriveBytes deriveBytes = new Rfc2898DeriveBytes(config.Password, bSalt);
        deriveBytes.IterationCount = 1000;

        rijndael.Key = deriveBytes.GetBytes(rijndael.KeySize / 8);
        rijndael.IV = deriveBytes.GetBytes(rijndael.BlockSize / 8);

        // 暗号化
        ICryptoTransform encryptor = rijndael.CreateEncryptor();
        byte[] encrypted = encryptor.TransformFinalBlock(data, 0, data.Length);

        encryptor.Dispose();

        return encrypted;
    }


    /// <summary>
    /// ファイルを復号します
    /// </summary>
    public static byte[] Decryption(byte[] data)
    {
        // Resourcesから暗号化キーを取得
        AESConfig config = Resources.Load<AESConfig>("AESConfig");

        RijndaelManaged rijndael = new RijndaelManaged();
        rijndael.KeySize = BLOCK_SIZE;
        rijndael.BlockSize = KEY_SIZE;

        byte[] bSalt = Encoding.UTF8.GetBytes(config.Salt);
        Rfc2898DeriveBytes deriveBytes = new Rfc2898DeriveBytes(config.Password, bSalt);
        deriveBytes.IterationCount = 1000;

        rijndael.Key = deriveBytes.GetBytes(rijndael.KeySize / 8);
        rijndael.IV = deriveBytes.GetBytes(rijndael.BlockSize / 8);

        // 復号化
        ICryptoTransform decryptor = rijndael.CreateDecryptor();
        byte[] plain = decryptor.TransformFinalBlock(data, 0, data.Length);

        decryptor.Dispose();

        return plain;
    }
}