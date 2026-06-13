using System.Collections.Generic;
using UnityEngine;

namespace EditorScripts
{
    /// <summary>
    /// Google 試算表同步設定檔。
    /// 請在 Unity 專案內對著任何資料夾點右鍵 -> Create -> Google Sheets -> Settings 建立此檔案。
    /// 只能建立一個，Importer 會自動尋找並讀取它。
    /// </summary>
    [CreateAssetMenu(fileName = "GoogleSheetSettings", menuName = "Google Sheets/Settings")]
    public class GoogleSheetSettings : ScriptableObject
    {
        [System.Serializable]
        public class SheetTarget
        {
            [Tooltip("CSV 存檔的名稱，例如 CharacterData 或 WeaponData")]
            public string SheetName;
            
            [Tooltip("請直接貼上瀏覽器上的工作表網址 (包含 gid)")]
            public string Url;
        }

        [Header("要同步的工作表清單")]
        public List<SheetTarget> SheetsToSync = new List<SheetTarget>();
    }
}
