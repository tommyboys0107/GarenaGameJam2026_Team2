using System;
using UnityEngine;
using UnityEngine.InputSystem;
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

        /// <summary>遊戲是否暫停中</summary>
        public bool IsPaused { get; private set; }

        /// <summary>暫停狀態改變時觸發 (true=暫停, false=恢復)</summary>
        public event Action<bool> OnPauseChanged;

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
            IsPaused = false;
        }

        /// <summary>
        /// 暫停遊戲。GameFlowController 和 StockMarketManager 都會停止推進。
        /// </summary>
        public void Pause()
        {
            if (!IsPlaying || IsPaused) return;

            IsPaused = true;

            // 暫停股市（使用 IsPaused 而非 StopMarket，避免重置計時器）
            if (stockMarketManager != null)
                stockMarketManager.IsPaused = true;

            Debug.Log("[GameManager] 遊戲暫停");
            OnPauseChanged?.Invoke(true);
        }

        /// <summary>
        /// 恢復遊戲。
        /// </summary>
        public void Resume()
        {
            if (!IsPlaying || !IsPaused) return;

            IsPaused = false;

            // 恢復股市
            if (stockMarketManager != null)
                stockMarketManager.IsPaused = false;

            Debug.Log("[GameManager] 遊戲恢復");
            OnPauseChanged?.Invoke(false);
        }

        /// <summary>
        /// 切換暫停/恢復。
        /// </summary>
        public void TogglePause()
        {
            if (IsPaused) Resume();
            else Pause();
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
            // Esc 鍵切換暫停（遊戲進行中隨時可用）
            if (IsPlaying)
            {
                var kb = Keyboard.current;
                if (kb != null && kb.escapeKey.wasPressedThisFrame)
                {
                    TogglePause();
                    return;
                }
            }

            if (!IsPlaying || IsPaused) return;

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
