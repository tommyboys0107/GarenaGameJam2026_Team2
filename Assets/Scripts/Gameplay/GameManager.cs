using System;
using UnityEngine;
using Data;
using BlackMarketTrader;

namespace Gameplay
{
    /// <summary>
    /// 遊戲主流程管理器。
    /// 從 TimeData.csv 讀取遊戲時長，倒數計時，時間到後觸發結算。
    /// 支援勝利條件達成時提前結束遊戲。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("參考")]
        [SerializeField] private StockMarketManager stockMarketManager;

        /// <summary>遊戲結束時觸發（向下相容）</summary>
        public event Action OnGameOver;

        /// <summary>遊戲結束時觸發，帶成敗結果 (true=勝利, false=時間到)</summary>
        public event Action<bool> OnGameEnd;

        /// <summary>目前剩餘時間</summary>
        public float TimeRemaining { get; private set; }

        /// <summary>遊戲總時長</summary>
        public float GameDuration { get; private set; }

        /// <summary>遊戲是否正在進行</summary>
        public bool IsPlaying { get; private set; }

        private void Start()
        {
            var timeConfig = CSVLoader.LoadTimeConfig();
            GameDuration = timeConfig.totalGameTime;
            StartGame();
        }

        public void StartGame()
        {
            TimeRemaining = GameDuration;
            IsPlaying = true;
        }

        /// <summary>
        /// 由 WinConditionChecker 呼叫，目標達成時提前結束遊戲。
        /// </summary>
        public void TriggerWin()
        {
            if (!IsPlaying) return;

            IsPlaying = false;

            // 停止股市
            if (stockMarketManager != null)
                stockMarketManager.StopMarket();

            Debug.Log("[GameManager] 目標達成，遊戲勝利結束！");

            OnGameEnd?.Invoke(true);
            OnGameOver?.Invoke();
        }

        private void Update()
        {
            if (!IsPlaying) return;

            TimeRemaining -= Time.deltaTime;

            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                IsPlaying = false;

                // 停止股市
                if (stockMarketManager != null)
                    stockMarketManager.StopMarket();

                Debug.Log("[GameManager] 時間到，遊戲結束。");

                OnGameEnd?.Invoke(false);
                OnGameOver?.Invoke();
            }
        }
    }
}
