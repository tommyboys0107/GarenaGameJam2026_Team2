using UnityEngine;

namespace Twitch
{
    /// <summary>
    /// Twitch 連線設定的 ScriptableObject。
    /// 在 Project 視窗右鍵 → Create → Twitch → Config 來建立。
    /// </summary>
    [CreateAssetMenu(fileName = "TwitchConfig", menuName = "Twitch/Config")]
    public class TwitchConfig : ScriptableObject
    {
        [Header("Twitch 頻道設定")]
        [Tooltip("要監聽的 Twitch 頻道名稱（小寫）")]
        public string channelName = "";

        [Header("認證設定（選填）")]
        [Tooltip("OAuth Token（若留空則使用匿名連線，僅能讀取聊天）")]
        public string oauthToken = "";

        [Tooltip("Bot 使用者名稱（若留空則使用匿名）")]
        public string botUsername = "";

        [Header("投票設定")]
        [Tooltip("每次投票的持續時間（秒）")]
        public float voteDuration = 30f;

        [Tooltip("同一位觀眾是否只能投一票")]
        public bool oneVotePerUser = true;

        /// <summary>
        /// 是否使用匿名連線（justinfan 模式，唯讀）
        /// </summary>
        public bool IsAnonymous => string.IsNullOrEmpty(oauthToken);
    }
}
