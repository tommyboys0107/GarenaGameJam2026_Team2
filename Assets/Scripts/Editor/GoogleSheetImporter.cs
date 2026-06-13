using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace EditorScripts
{
    public static class GoogleSheetImporter
    {
        [MenuItem("Tools/Sync Google Sheets (Multiple)")]
        public static void SyncData()
        {
            // 尋找專案內的 GoogleSheetSettings
            string[] guids = AssetDatabase.FindAssets("t:GoogleSheetSettings");
            if (guids.Length == 0)
            {
                Debug.LogError("[GoogleSheetImporter] 找不到 GoogleSheetSettings 設定檔！請在 Project 視窗右鍵 -> Create -> Google Sheets -> Settings 建立一個，並填寫網址。");
                return;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            GoogleSheetSettings settings = AssetDatabase.LoadAssetAtPath<GoogleSheetSettings>(path);

            if (settings.SheetsToSync == null || settings.SheetsToSync.Count == 0)
            {
                Debug.LogWarning("[GoogleSheetImporter] GoogleSheetSettings 中沒有設定任何工作表網址！");
                return;
            }

            Debug.Log($"開始從 Google 試算表同步 {settings.SheetsToSync.Count} 張資料表...");

            int successCount = 0;
            string dir = "Assets/Resources";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            foreach (var sheet in settings.SheetsToSync)
            {
                if (string.IsNullOrEmpty(sheet.SheetName) || string.IsNullOrEmpty(sheet.Url))
                {
                    Debug.LogWarning("[GoogleSheetImporter] 有工作表的名稱或網址為空，已跳過。");
                    continue;
                }

                // 使用正規表達式從網址中解析 Document ID 和 gid
                // 網址範例: https://docs.google.com/spreadsheets/d/1v_wc7R4xe04SdoexN0yjricUyrii7aa-w0N38I8KyrM/edit#gid=0
                var match = Regex.Match(sheet.Url, @"/d/([a-zA-Z0-9-_]+).*gid=([0-9]+)");
                if (!match.Success)
                {
                    // 若沒有 gid，預設使用 export?format=csv，但不保證是哪張表
                    Debug.LogWarning($"[GoogleSheetImporter] 無法從網址解析出 Document ID 或 gid，嘗試使用原始網址下載: {sheet.SheetName}");
                    DownloadAndSave(sheet.Url, sheet.SheetName);
                }
                else
                {
                    string docId = match.Groups[1].Value;
                    string gid = match.Groups[2].Value;
                    string exportUrl = $"https://docs.google.com/spreadsheets/d/{docId}/export?format=csv&gid={gid}";
                    
                    if (DownloadAndSave(exportUrl, sheet.SheetName))
                    {
                        successCount++;
                    }
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"[GoogleSheetImporter] 同步完成！共成功更新 {successCount} 張資料表。");
        }

        private static bool DownloadAndSave(string url, string fileName)
        {
            var request = UnityWebRequest.Get(url);
            var operation = request.SendWebRequest();
            
            while (!operation.isDone) { }

            if (request.result == UnityWebRequest.Result.Success)
            {
                string savePath = $"Assets/Resources/{fileName}.csv";
                File.WriteAllText(savePath, request.downloadHandler.text);
                Debug.Log($"  ✅ 成功下載: {fileName}");
                return true;
            }
            else
            {
                Debug.LogError($"  ❌ 下載失敗 ({fileName}): {request.error}");
                return false;
            }
        }
    }
}
