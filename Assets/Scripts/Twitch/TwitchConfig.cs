using UnityEngine;

namespace Twitch
{
    /// <summary>
    /// Twitch 連線設定的 ScriptableObject。
    /// channelName 用 static 變數保存，跨場景不會遺失，關閉遊戲自動清除。
    /// </summary>
    [CreateAssetMenu(fileName = "TwitchConfig", menuName = "Twitch/Config")]
    public class TwitchConfig : ScriptableObject
    {
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
        /// Runtime 頻道名稱。static 確保跨場景存活，關閉遊戲自動清零。
        /// </summary>
        public static string channelName = "";

        /// <summary>
        /// 是否使用匿名連線（justinfan 模式，唯讀）
        /// </summary>
        public bool IsAnonymous => string.IsNullOrEmpty(oauthToken);

        /// <summary>
        /// 是否已設定頻道名稱
        /// </summary>
        public bool HasChannel => !string.IsNullOrWhiteSpace(channelName);
    }
}
