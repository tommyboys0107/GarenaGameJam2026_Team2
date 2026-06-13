using UnityEngine;

namespace Core
{
    /// <summary>
    /// 跨場景傳遞資料用的靜態類別。
    /// 儲存玩家選擇的角色等資訊。
    /// </summary>
    public static class GameData
    {
        /// <summary>
        /// 玩家選擇的角色索引（0~5，對應六個角色）
        /// </summary>
        public static int SelectedCharacterIndex { get; set; } = 0;

        /// <summary>
        /// 玩家選擇的目標索引（0~5，對應六個目標）
        /// </summary>
        public static int SelectedGoalIndex { get; set; } = 0;

        /// <summary>
        /// 重置所有資料（回到 Title 時呼叫）
        /// </summary>
        public static void Reset()
        {
            SelectedCharacterIndex = 0;
            SelectedGoalIndex = 0;
        }
    }
}
