using System;
using System.Collections.Generic;
using UnityEngine;

namespace Twitch
{
    /// <summary>
    /// 管理 Twitch 投票邏輯。
    /// 支援三選一投票，觀眾在聊天室輸入 1、2、3 來投票。
    /// </summary>
    public class TwitchVoteManager : MonoBehaviour
    {
        [SerializeField] private TwitchIRC twitchIRC;
        [SerializeField] private TwitchConfig config;

        /// <summary>投票結果回呼：傳入獲勝選項的索引（0, 1, 2）</summary>
        public event Action<int> OnVoteComplete;

        /// <summary>投票更新回呼：傳入目前三個選項的票數</summary>
        public event Action<int[]> OnVoteUpdated;

        /// <summary>投票開始回呼：傳入選項描述陣列</summary>
        public event Action<string[]> OnVoteStarted;

        public bool IsVoting { get; private set; }
        public float TimeRemaining { get; private set; }
        public string[] CurrentOptions { get; private set; }

        private int[] _votes;
        private HashSet<string> _votedUsers;
        private float _voteTimer;

        private void Awake()
        {
            _votes = new int[3];
            _votedUsers = new HashSet<string>();
        }

        private void OnEnable()
        {
            if (twitchIRC != null)
            {
                twitchIRC.OnChatMessage += HandleChatMessage;
            }
        }

        private void OnDisable()
        {
            if (twitchIRC != null)
            {
                twitchIRC.OnChatMessage -= HandleChatMessage;
            }
        }

        private void Update()
        {
            if (!IsVoting) return;

            _voteTimer -= Time.deltaTime;
            TimeRemaining = Mathf.Max(0f, _voteTimer);

            if (_voteTimer <= 0f)
            {
                EndVote();
            }
        }

        /// <summary>
        /// 開始一次三選一投票。
        /// </summary>
        /// <param name="option1">選項 1 的描述</param>
        /// <param name="option2">選項 2 的描述</param>
        /// <param name="option3">選項 3 的描述</param>
        public void StartVote(string option1, string option2, string option3)
        {
            if (IsVoting)
            {
                Debug.LogWarning("[TwitchVote] 目前已有投票進行中。");
                return;
            }

            CurrentOptions = new[] { option1, option2, option3 };
            _votes = new int[3];
            _votedUsers.Clear();
            _voteTimer = config.voteDuration;
            TimeRemaining = config.voteDuration;

            // 清空佇列中投票前的舊訊息，避免被誤算
            twitchIRC.ClearMessageQueue();

            IsVoting = true;

            OnVoteStarted?.Invoke(CurrentOptions);

            // 若有認證，可以在聊天室公布投票
            twitchIRC.SendChatMessage(
                $"🗳️ 投票開始！輸入 1、2 或 3 來投票（{config.voteDuration} 秒）" +
                $" | 1: {option1} | 2: {option2} | 3: {option3}");

            Debug.Log($"[TwitchVote] 投票開始: 1.{option1} / 2.{option2} / 3.{option3}");
        }

        /// <summary>
        /// 強制結束目前投票並回傳結果。
        /// </summary>
        public void ForceEndVote()
        {
            if (!IsVoting) return;
            EndVote();
        }

        /// <summary>
        /// 強制以指定的選項索引作為贏家結束投票（測試用）。
        /// </summary>
        /// <param name="winnerIndex">獲勝選項索引 (0, 1, 2)</param>
        public void ForceEndVoteWithWinner(int winnerIndex)
        {
            if (!IsVoting) return;
            if (winnerIndex < 0 || winnerIndex > 2) return;

            IsVoting = false;

            Debug.Log($"[TwitchVote] 強制結束投票！指定贏家: {CurrentOptions[winnerIndex]} " +
                      $"(票數: {_votes[0]}/{_votes[1]}/{_votes[2]})");

            twitchIRC.SendChatMessage(
                $"✅ 投票結束（測試）！獲勝: {CurrentOptions[winnerIndex]} " +
                $"({_votes[0]}/{_votes[1]}/{_votes[2]} 票)");

            OnVoteComplete?.Invoke(winnerIndex);
        }

        /// <summary>
        /// 取得目前票數的複本。
        /// </summary>
        public int[] GetCurrentVotes()
        {
            return (int[])_votes.Clone();
        }

        /// <summary>
        /// 模擬一筆投票（測試用）。繞過 Twitch IRC，直接以假使用者身份投票。
        /// </summary>
        /// <param name="fakeUsername">模擬的使用者名稱</param>
        /// <param name="optionIndex">選項索引 (0, 1, 2)</param>
        public void SimulateVote(string fakeUsername, int optionIndex)
        {
            if (!IsVoting) return;
            if (optionIndex < 0 || optionIndex > 2) return;
            HandleChatMessage(fakeUsername, (optionIndex + 1).ToString());
        }

        private void HandleChatMessage(string username, string message)
        {
            if (!IsVoting) return;

            // 只接受 "1", "2", "3"
            message = message.Trim();
            if (message != "1" && message != "2" && message != "3") return;

            // 每人一票檢查
            if (config.oneVotePerUser)
            {
                string lowerUser = username.ToLower();
                if (_votedUsers.Contains(lowerUser)) return;
                _votedUsers.Add(lowerUser);
            }

            int choice = int.Parse(message) - 1; // 轉為 0-based index
            _votes[choice]++;

            Debug.Log($"[TwitchVote] {username} 投了選項 {message} ({CurrentOptions[choice]}) | 目前票數: {_votes[0]}/{_votes[1]}/{_votes[2]}");

            OnVoteUpdated?.Invoke(GetCurrentVotes());
        }

        private void EndVote()
        {
            IsVoting = false;

            // 找出最高票
            int winnerIndex = 0;
            int maxVotes = _votes[0];

            for (int i = 1; i < 3; i++)
            {
                if (_votes[i] > maxVotes)
                {
                    maxVotes = _votes[i];
                    winnerIndex = i;
                }
            }

            // 如果平手，隨機選一個
            var tiedOptions = new List<int>();
            for (int i = 0; i < 3; i++)
            {
                if (_votes[i] == maxVotes) tiedOptions.Add(i);
            }

            if (tiedOptions.Count > 1)
            {
                winnerIndex = tiedOptions[UnityEngine.Random.Range(0, tiedOptions.Count)];
            }

            int totalVotes = _votes[0] + _votes[1] + _votes[2];
            Debug.Log($"[TwitchVote] 投票結束！結果: {CurrentOptions[winnerIndex]} " +
                      $"(票數: {_votes[0]}/{_votes[1]}/{_votes[2]}, 共 {totalVotes} 票)");

            twitchIRC.SendChatMessage(
                $"✅ 投票結束！獲勝: {CurrentOptions[winnerIndex]} " +
                $"({_votes[0]}/{_votes[1]}/{_votes[2]} 票)");

            OnVoteComplete?.Invoke(winnerIndex);
        }
    }
}
