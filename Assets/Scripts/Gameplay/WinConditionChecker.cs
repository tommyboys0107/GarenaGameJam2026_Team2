using System.Collections.Generic;
using UnityEngine;
using Data;
using Core;
using BlackMarketTrader;

namespace Gameplay
{
    /// <summary>
    /// 勝利條件檢查器。
    /// 每次股價更新時，檢查玩家選擇的目標股票是否已達到漲跌幅目標。
    /// 達標後立即通知 GameManager 結束遊戲。
    /// </summary>
    public class WinConditionChecker : MonoBehaviour
    {
        [Header("參考")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private StockMarketManager stockMarketManager;

        private GoalInfo _currentGoal;
        private bool _goalReached = false;

        /// <summary>目前的目標資訊（供 UI 讀取）</summary>
        public GoalInfo CurrentGoal => _currentGoal;

        /// <summary>目前目標股票的實際漲跌幅百分比</summary>
        public float CurrentPercent { get; private set; }

        /// <summary>目前目標股票的價格</summary>
        public float CurrentPrice { get; private set; }

        /// <summary>目標股票的初始價格</summary>
        public float InitialPrice { get; private set; }

        private void Start()
        {
            // 載入所有目標並取得玩家選的那一個
            var goals = CSVLoader.LoadGoals();
            int goalIndex = GameData.SelectedGoalIndex;

            if (goalIndex >= 0 && goalIndex < goals.Count)
            {
                _currentGoal = goals[goalIndex];
            }
            else
            {
                Debug.LogError($"[WinCondition] 無效的目標索引: {goalIndex}");
                _currentGoal = goals[0];
            }

            Debug.Log($"[WinCondition] 目標: {_currentGoal.stockCode} {(_currentGoal.targetPercent > 0 ? "+" : "")}{_currentGoal.targetPercent}%");

            // 記錄初始價
            InitialPrice = GetStockInitialPrice(_currentGoal.stockCode);

            // 訂閱股價更新事件
            if (stockMarketManager != null)
                stockMarketManager.OnPriceUpdated += CheckWinCondition;
        }

        private void OnDestroy()
        {
            if (stockMarketManager != null)
                stockMarketManager.OnPriceUpdated -= CheckWinCondition;
        }

        private void CheckWinCondition()
        {
            if (_goalReached) return;
            if (gameManager == null || !gameManager.IsPlaying) return;

            // 計算目標股票的漲跌幅
            float currentPrice = GetStockCurrentPrice(_currentGoal.stockCode);
            float initialPrice = InitialPrice;

            if (initialPrice <= 0f) return;

            float percent = (currentPrice - initialPrice) / initialPrice * 100f;
            CurrentPercent = percent;
            CurrentPrice = currentPrice;

            // 判定勝利條件
            bool win = false;
            if (_currentGoal.targetPercent > 0)
            {
                // 目標是漲：實際漲幅 >= 目標
                win = percent >= _currentGoal.targetPercent;
            }
            else
            {
                // 目標是跌：實際跌幅 <= 目標（例如 -40，需要 <= -40）
                win = percent <= _currentGoal.targetPercent;
            }

            if (win)
            {
                _goalReached = true;
                Debug.Log($"[WinCondition] 目標達成！ {_currentGoal.stockCode} {percent:+0.0;-0.0}% (目標: {_currentGoal.targetPercent}%)");
                gameManager.TriggerWin();
            }
        }

        private float GetStockCurrentPrice(string stockCode)
        {
            int index = GetStockIndex(stockCode);
            if (index < 0 || stockMarketManager.Stocks == null) return 0f;
            return stockMarketManager.Stocks[index].CurrentPrice;
        }

        private float GetStockInitialPrice(string stockCode)
        {
            int index = GetStockIndex(stockCode);
            if (index < 0 || stockMarketManager.Stocks == null) return 0f;
            return stockMarketManager.Stocks[index].InitialPrice;
        }

        private int GetStockIndex(string stockCode)
        {
            return stockCode switch
            {
                "NARC" => 0,
                "LOCK" => 1,
                "BYTE" => 2,
                _ => -1
            };
        }
    }
}
