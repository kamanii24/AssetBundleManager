using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
// ScriptableObjectをプレハブとして出力する汎用スクリプト
/// </summary>
// <remarks>
// 指定したScriptableObjectをプレハブに変換する。
// 1.Editorフォルダ下にCreateScriptableObjectPrefub.csを配置
// 2.ScriptableObjectのファイルを選択して右クリック→Create ScriptableObjectを選択
// </remarks>
public class ScriptableObjectToAsset
{
    readonly static string[] labels = { "Data", "ScriptableObject", string.Empty };

    [MenuItem("Assets/Create ScriptableObject")]
    static void Crate()
    {
        foreach (Object selectedObject in Selection.objects)
        {
            // get path
            string path = getSavePath(selectedObject);

            // create instance
            ScriptableObject obj = ScriptableObject.CreateInstance(selectedObject.name);
            AssetDatabase.CreateAsset(obj, path);
            labels[2] = selectedObject.name;
            // add label
            ScriptableObject sobj = AssetDatabase.LoadAssetAtPath(path, typeof(ScriptableObject)) as ScriptableObject;
            AssetDatabase.SetLabels(sobj, labels);
            EditorUtility.SetDirty(sobj);
        }
    }

    static string getSavePath(Object selectedObject)
    {
        string objectName = selectedObject.name;
        string dirPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(selectedObject));
        string path = string.Format("{0}/{1}.asset", dirPath, objectName);

        if (File.Exists(path))
            for (int i = 1; ; i++)
            {
                path = string.Format("{0}/{1}({2}).asset", dirPath, objectName, i);

                if (!File.Exists(path))
                {
                    break;
                }
            }

        return path;
    }
}


// Thank you for "tsubaki"!!!
// https://gist.github.com/tsubaki/5149402