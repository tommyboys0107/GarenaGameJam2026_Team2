using UnityEngine;
using UnityEngine.InputSystem;
using Twitch;

namespace UI
{
    /// <summary>
    /// 測試用面板：按下鍵盤 7/8/9 分別為選項 1/2/3 增加一票。
    /// 也提供三個 public 方法供 UI Button 的 OnClick 綁定。
    /// 掛在場景中任意 GameObject 即可使用。
    /// 
    /// Editor 模式下會自動顯示 OnGUI 按鈕（左上角），
    /// 不需另外建 Canvas 按鈕即可測試。
    /// </summary>
    public class VoteTestPanel : MonoBehaviour
    {
        [Header("投票管理器引用")]
        [SerializeField] private TwitchVoteManager voteManager;

        [Header("測試設定")]

        private int _testVoterIndex;

        private void Update()
        {
            if (voteManager == null || !voteManager.IsVoting) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            // 鍵盤 7/8/9 模擬投票
            if (kb.digit7Key.wasPressedThisFrame)
                SimulateVote(0);
            if (kb.digit8Key.wasPressedThisFrame)
                SimulateVote(1);
            if (kb.digit9Key.wasPressedThisFrame)
                SimulateVote(2);
        }

        /// <summary>
        /// 模擬為選項 1 投一票（UI Button 可綁定此方法）。
        /// </summary>
        public void VoteOption1() => SimulateVote(0);

        /// <summary>
        /// 模擬為選項 2 投一票（UI Button 可綁定此方法）。
        /// </summary>
        public void VoteOption2() => SimulateVote(1);

        /// <summary>
        /// 模擬為選項 3 投一票（UI Button 可綁定此方法）。
        /// </summary>
        public void VoteOption3() => SimulateVote(2);

        /// <summary>
        /// 透過 TwitchVoteManager.SimulateVote 模擬一次投票。
        /// </summary>
        private void SimulateVote(int optionIndex)
        {
            if (voteManager == null || !voteManager.IsVoting)
            {
                Debug.LogWarning("[VoteTest] 目前沒有進行中的投票，無法模擬。");
                return;
            }

            _testVoterIndex++;
            string fakeUser = $"test_voter_{_testVoterIndex}";

            voteManager.SimulateVote(fakeUser, optionIndex);
            Debug.Log($"[VoteTest] 模擬投票: {fakeUser} → 選項 {optionIndex + 1}");
        }

    }
}
