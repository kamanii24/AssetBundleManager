// =================================
//
//	AESConfig.cs
//	Created by Takuya Himeji
//
// =================================

using UnityEngine;
using System;

public class AESConfig : ScriptableObject
{
    [SerializeField]
    private string password = string.Empty;
    public string Password
    {
        get { return password; }
    }

    [SerializeField, Tooltip("Must over 8 chars.")]
    private string salt = string.Empty;
    public string Salt
    {
        get { return salt; }
    }
}
