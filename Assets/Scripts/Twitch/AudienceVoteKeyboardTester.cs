using UnityEngine;
using UnityEngine.InputSystem;

namespace Twitch
{
    /// <summary>
    /// 測試工具：按 Q/W/E 鍵強制決定觀眾投票結果。
    /// Q = 選項一, W = 選項二, E = 選項三。
    /// 僅在投票進行中時有效。正式上線前請移除或停用此腳本。
    /// </summary>
    public class AudienceVoteKeyboardTester : MonoBehaviour
    {
        [SerializeField] private TwitchVoteManager voteManager;

        private void Update()
        {
            if (voteManager == null || !voteManager.IsVoting) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.qKey.wasPressedThisFrame)
            {
                Debug.Log("[VoteKeyTest] Q 鍵按下 → 強制觀眾投票結果：選項一");
                voteManager.ForceEndVoteWithWinner(0);
            }
            else if (kb.wKey.wasPressedThisFrame)
            {
                Debug.Log("[VoteKeyTest] W 鍵按下 → 強制觀眾投票結果：選項二");
                voteManager.ForceEndVoteWithWinner(1);
            }
            else if (kb.eKey.wasPressedThisFrame)
            {
                Debug.Log("[VoteKeyTest] E 鍵按下 → 強制觀眾投票結果：選項三");
                voteManager.ForceEndVoteWithWinner(2);
            }
        }
    }
}
