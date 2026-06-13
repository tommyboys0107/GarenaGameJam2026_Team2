using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    [System.Serializable]
    public class CharacterInfo
    {
        public string Id;          // 編號
        public string Name;        // 角色名稱
        public string Nickname;    // 外號
        public string Description; // 角色描述
    }

    /// <summary>
    /// 在執行期讀取並管理由 Google 試算表同步下來的角色參數。
    /// </summary>
    public static class CharacterDataManager
    {
        private static Dictionary<string, CharacterInfo> _characterDict;
        private static List<CharacterInfo> _characterList;

        public static bool IsLoaded => _characterDict != null;

        /// <summary>
        /// 載入 CSV 資料並解析
        /// </summary>
        public static void LoadData()
        {
            if (IsLoaded) return;

            _characterDict = new Dictionary<string, CharacterInfo>();
            _characterList = new List<CharacterInfo>();

            // 從 Resources 讀取 CharacterData.csv
            TextAsset csvFile = Resources.Load<TextAsset>("CharacterData");
            if (csvFile == null)
            {
                Debug.LogWarning("[CharacterDataManager] 無法載入 CharacterData.csv！請確認已透過上方選單 Tools > Sync Google Sheet 同步資料。");
                return;
            }

            var rows = CSVParser.Parse(csvFile.text);
            foreach (var row in rows)
            {
                if (!row.ContainsKey("編號")) continue;

                var info = new CharacterInfo
                {
                    Id = row.TryGetValue("編號", out var id) ? id : "",
                    Name = row.TryGetValue("角色名稱", out var name) ? name : "",
                    Nickname = row.TryGetValue("外號", out var nick) ? nick : "",
                    Description = row.TryGetValue("角色描述", out var desc) ? desc : ""
                };

                _characterDict[info.Id] = info;
                _characterList.Add(info);
            }
            
            Debug.Log($"[CharacterDataManager] 成功載入 {_characterList.Count} 筆角色資料！");
        }

        /// <summary>
        /// 透過 Key (編號，如 "C01") 取得角色資料
        /// </summary>
        public static CharacterInfo GetCharacterById(string id)
        {
            if (!IsLoaded) LoadData();
            return _characterDict != null && _characterDict.TryGetValue(id, out var info) ? info : null;
        }

        /// <summary>
        /// 透過索引 (0~5) 取得角色資料
        /// </summary>
        public static CharacterInfo GetCharacterByIndex(int index)
        {
            if (!IsLoaded) LoadData();
            if (_characterList != null && index >= 0 && index < _characterList.Count)
                return _characterList[index];
            return null;
        }

        /// <summary>
        /// 取得所有角色資料
        /// </summary>
        public static IReadOnlyList<CharacterInfo> GetAllCharacters()
        {
            if (!IsLoaded) LoadData();
            return _characterList;
        }
    }
}
