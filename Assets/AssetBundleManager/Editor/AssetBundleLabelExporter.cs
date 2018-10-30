// =================================
//
//	AssetBundleLabelExporter.cs
//	Created by Takuya Himeji
//
// =================================

using UnityEngine;
using UnityEditor;
using System.IO;

namespace KM2
{
    public class AssetBundleLabelExporter
    {
        /// <summary>
        /// AssetBundleラベルをcsvに書き出す
        /// </summary>
        [MenuItem("AssetBundles/AssetBundle Labels/Export", false)]
        public static void ExportToCsv()
        {
            Export();
        }

        private static void Export()
        {
            // 保存先のファイルパスを取得する
            var filePath = EditorUtility.SaveFilePanel("Export", "", "AssetBundleLabels", "csv");

            if (filePath == "") return;

            string labels = "";
            foreach (string str in AssetDatabase.GetAllAssetBundleNames())
            {
                labels += str + ",";
            }
            labels = labels.Remove(labels.Length - 1);

            StreamWriter sw = new StreamWriter(filePath, false); //true=追記 false=上書き
            sw.WriteLine(labels);
            sw.Flush();
            sw.Close();

            AssetDatabase.Refresh();
            Debug.Log("Export Success!! " + filePath);
        }
    }
}