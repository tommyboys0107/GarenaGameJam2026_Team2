using System;
using System.Collections;
using UnityEngine;
using Twitch;

namespace Gameplay
{
    /// <summary>
    /// 遊戲流程控制器。
    /// 負責在指定時間點觸發 Twitch 投票，並將結果回傳給遊戲邏輯。
    /// 設計文件規格：5 秒思考 + 10 秒投票，共三輪。
    /// </summary>
    public class GameFlowController : MonoBehaviour
    {
        [Header("參考")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private TwitchVoteManager voteManager;

        [Header("投票時間設定")]
        [Tooltip("每一輪投票開始的時間點（以遊戲開始後幾秒計算）")]
        [SerializeField] private float[] voteStartTimes = { 10f, 25f, 40f };

        [Header("投票選項（三輪 × 三選一）")]
        [SerializeField] private VoteRound[] voteRounds = new VoteRound[3]
        {
            new() { option1 = "股市暴跌", option2 = "黑市查稅", option3 = "內線消息" },
            new() { option1 = "通貨膨脹", option2 = "交易凍結", option3 = "黑幕曝光" },
            new() { option1 = "全面崩盤", option2 = "一夜暴富", option3 = "莊家介入" }
        };

        /// <summary>
        /// 投票結果回呼：(輪次 0-based, 獲勝選項 index 0-based)
        /// </summary>
        public event Action<int, int> OnVoteResult;

        /// <summary>目前進行到第幾輪（0-based，-1 表示尚未開始）</summary>
        public int CurrentRound { get; private set; } = -1;

        private int _nextRoundIndex;
        private float _elapsedTime;
        private bool _waitingForVote;

        private void OnEnable()
        {
            if (voteManager != null)
                voteManager.OnVoteComplete += HandleVoteComplete;
        }

        private void OnDisable()
        {
            if (voteManager != null)
                voteManager.OnVoteComplete -= HandleVoteComplete;
        }

        private void Update()
        {
            if (gameManager == null || !gameManager.IsPlaying) return;

            _elapsedTime += Time.deltaTime;

            if (_waitingForVote) return;

            // 檢查是否到了下一輪投票的觸發時間
            if (_nextRoundIndex < voteStartTimes.Length &&
                _elapsedTime >= voteStartTimes[_nextRoundIndex])
            {
                TriggerVote(_nextRoundIndex);
            }
        }

        /// <summary>
        /// 手動觸發指定輪次的投票（供外部呼叫）。
        /// </summary>
        /// <param name="roundIndex">輪次索引（0, 1, 2）</param>
        public void TriggerVote(int roundIndex)
        {
            if (voteManager == null || voteManager.IsVoting) return;
            if (roundIndex < 0 || roundIndex >= voteRounds.Length) return;

            CurrentRound = roundIndex;
            _waitingForVote = true;

            var round = voteRounds[roundIndex];
            voteManager.StartVote(round.option1, round.option2, round.option3);

            Debug.Log($"[GameFlow] 第 {roundIndex + 1} 輪投票開始");
        }

        /// <summary>
        /// 手動觸發投票（使用自訂選項）。
        /// </summary>
        public void TriggerCustomVote(string option1, string option2, string option3)
        {
            if (voteManager == null || voteManager.IsVoting) return;

            CurrentRound++;
            _waitingForVote = true;

            voteManager.StartVote(option1, option2, option3);
            Debug.Log($"[GameFlow] 自訂投票開始: {option1} / {option2} / {option3}");
        }

        private void HandleVoteComplete(int winnerIndex)
        {
            _waitingForVote = false;
            _nextRoundIndex++;

            Debug.Log($"[GameFlow] 第 {CurrentRound + 1} 輪投票結束，結果: 選項 {winnerIndex + 1}");

            OnVoteResult?.Invoke(CurrentRound, winnerIndex);
        }

        [Serializable]
        public struct VoteRound
        {
            public string option1;
            public string option2;
            public string option3;
        }
    }
}
