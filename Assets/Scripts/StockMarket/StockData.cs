using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlackMarketTrader
{
    /// <summary>
    /// 趨勢等級（由每秒變化率自動判定）
    /// </summary>
    public enum TrendLevel
    {
        BigRise,    // 大漲
        SmallRise,  // 小漲
        Flat,       // 平盤
        SmallDrop,  // 小跌
        BigDrop     // 大跌
    }

    /// <summary>
    /// 事件標記資料
    /// </summary>
    [Serializable]
    public class StockEvent
    {
        public int TimeIndex;
        public string EventName;
    }

    /// <summary>
    /// 單一商品的股市資料
    /// 新算法：用目標價 + 變化時間來控制價格移動
    /// </summary>
    [Serializable]
    public class StockData
    {
        public string Name;
        public Color LineColor;
        public float InitialPrice;
        public float CurrentPrice;
        public float Volatility = 1f;
        public List<float> PriceHistory = new List<float>();

        // 目標價系統
        public float TargetPrice;
        public float TransitionTimeRemaining;

        /// <summary>
        /// 當前趨勢等級（自動由速率判定）
        /// </summary>
        public TrendLevel CurrentTrend { get; private set; } = TrendLevel.Flat;

        /// <summary>
        /// 設定新的目標價（以初始價的百分比計算）
        /// </summary>
        /// <param name="percent">目標漲跌幅，例如 +5 表示漲 5%，-10 表示跌 10%</param>
        /// <param name="transitionTime">到達目標的時間（秒）</param>
        public void SetTarget(float percent, float transitionTime)
        {
            TargetPrice = InitialPrice * (1f + percent / 100f);
            TransitionTimeRemaining = transitionTime;
        }

        /// <summary>
        /// 直接設定絕對目標價
        /// </summary>
        public void SetTargetAbsolute(float absoluteTarget, float transitionTime)
        {
            TargetPrice = absoluteTarget;
            TransitionTimeRemaining = transitionTime;
        }

        /// <summary>
        /// 累加目標價（事件效果用）
        /// </summary>
        /// <param name="deltaValue">直接加減的數值（如 +20, -15）</param>
        /// <param name="transitionTime">到達目標的時間</param>
        public void AddToTarget(float deltaValue, float transitionTime)
        {
            TargetPrice += deltaValue;
            TransitionTimeRemaining = transitionTime;
        }

        /// <summary>
        /// 每幀更新價格，平滑移動向目標
        /// </summary>
        /// <param name="deltaTime">Time.deltaTime</param>
        /// <param name="minPrice">最低價</param>
        /// <param name="maxPrice">最高價</param>
        /// <param name="trendThresholds">趨勢判定的速率門檻值</param>
        public void UpdatePrice(float deltaTime, float minPrice, float maxPrice, TrendThresholds thresholds)
        {
            if (TransitionTimeRemaining <= 0f)
            {
                // 已到達目標，不再移動
                CurrentTrend = TrendLevel.Flat;
                return;
            }

            // 計算每秒變化率
            float diff = TargetPrice - CurrentPrice;
            float ratePerSecond = diff / TransitionTimeRemaining;

            // 移動價格
            float step = ratePerSecond * deltaTime;
            CurrentPrice += step;
            CurrentPrice = Mathf.Clamp(CurrentPrice, minPrice, maxPrice);

            TransitionTimeRemaining -= deltaTime;
            if (TransitionTimeRemaining < 0f)
                TransitionTimeRemaining = 0f;

            // 根據速率判定趨勢等級
            CurrentTrend = GetTrendFromRate(ratePerSecond, thresholds);
        }

        private TrendLevel GetTrendFromRate(float ratePerSecond, TrendThresholds t)
        {
            if (ratePerSecond >= t.BigRiseThreshold) return TrendLevel.BigRise;
            if (ratePerSecond >= t.SmallRiseThreshold) return TrendLevel.SmallRise;
            if (ratePerSecond <= -t.BigRiseThreshold) return TrendLevel.BigDrop;
            if (ratePerSecond <= -t.SmallRiseThreshold) return TrendLevel.SmallDrop;
            return TrendLevel.Flat;
        }

        /// <summary>
        /// 取得趨勢的顯示文字
        /// </summary>
        public string GetTrendDisplayText()
        {
            switch (CurrentTrend)
            {
                case TrendLevel.BigRise:   return "↑↑";
                case TrendLevel.SmallRise: return "↑";
                case TrendLevel.Flat:      return "-";
                case TrendLevel.SmallDrop: return "↓";
                case TrendLevel.BigDrop:   return "↓↓";
                default: return "";
            }
        }

        /// <summary>
        /// 取得趨勢對應的顏色
        /// </summary>
        public Color GetTrendColor()
        {
            switch (CurrentTrend)
            {
                case TrendLevel.BigRise:   return new Color(1f, 0.1f, 0.1f);
                case TrendLevel.SmallRise: return new Color(1f, 0.4f, 0.4f);
                case TrendLevel.Flat:      return new Color(0.9f, 0.9f, 0.9f);
                case TrendLevel.SmallDrop: return new Color(0.4f, 1f, 0.4f);
                case TrendLevel.BigDrop:   return new Color(0.1f, 0.9f, 0.1f);
                default: return Color.white;
            }
        }
    }

    /// <summary>
    /// 趨勢判定的速率門檻值（Inspector 可調）
    /// </summary>
    [Serializable]
    public class TrendThresholds
    {
        [Tooltip("每秒變化率超過此值判定為小漲/小跌")]
        public float SmallRiseThreshold = 1f;

        [Tooltip("每秒變化率超過此值判定為大漲/大跌")]
        public float BigRiseThreshold = 3f;
    }
}