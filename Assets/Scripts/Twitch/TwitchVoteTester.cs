using UnityEngine;
using UnityEngine.InputSystem;

namespace Twitch
{
    /// <summary>
    /// 用來測試投票系統的簡易觸發器。
    /// 按下 V 鍵即可開始一次投票。正式遊戲中請改用你的事件系統來觸發。
    /// </summary>
    public class TwitchVoteTester : MonoBehaviour
    {
        [SerializeField] private TwitchVoteManager voteManager;

        [Header("測試用選項")]
        [SerializeField] private string testOption1 = "增加敵人數量";
        [SerializeField] private string testOption2 = "給玩家加速";
        [SerializeField] private string testOption3 = "反轉控制";

        private void Awake()
        {
            if (voteManager != null)
            {
                voteManager.OnVoteComplete += OnVoteResult;
            }
        }

        private void OnDestroy()
        {
            if (voteManager != null)
            {
                voteManager.OnVoteComplete -= OnVoteResult;
            }
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard[Key.V].wasPressedThisFrame && voteManager != null && !voteManager.IsVoting)
            {
                voteManager.StartVote(testOption1, testOption2, testOption3);
            }
        }

        private void OnVoteResult(int winnerIndex)
        {
            Debug.Log($"[VoteTester] 觀眾選擇了選項 {winnerIndex + 1}: {voteManager.CurrentOptions[winnerIndex]}");

            // 在這裡根據結果執行遊戲邏輯
            switch (winnerIndex)
            {
                case 0:
                    Debug.Log("→ 執行效果：增加敵人數量");
                    break;
                case 1:
                    Debug.Log("→ 執行效果：給玩家加速");
                    break;
                case 2:
                    Debug.Log("→ 執行效果：反轉控制");
                    break;
            }
        }
    }
}
