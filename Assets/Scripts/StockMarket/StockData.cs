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
        public bool IsEventDriven;

        // 不含噪音的基礎價格（用於趨勢判定）
        [NonSerialized] public float BasePrice;

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
                CurrentTrend = TrendLevel.Flat;
                return;
            }

            // 用 BasePrice 計算速率（不受噪音影響）
            float diff = TargetPrice - BasePrice;
            float ratePerSecond = diff / TransitionTimeRemaining;

            // 移動 BasePrice
            float step = ratePerSecond * deltaTime;
            BasePrice += step;
            BasePrice = Mathf.Clamp(BasePrice, minPrice, maxPrice);

            // CurrentPrice 跟著 BasePrice（噪音在 RecordDataPoint 加）
            CurrentPrice = BasePrice;

            TransitionTimeRemaining -= deltaTime;
            if (TransitionTimeRemaining < 0f)
                TransitionTimeRemaining = 0f;

            // 趨勢完全由理論速率決定
            CurrentTrend = GetTrendFromRate(ratePerSecond, thresholds);
        }

        /// <summary>
        /// 噪音影響趨勢：用實際每秒價格變化重新判定，但不反轉方向
        /// </summary>
        public void ApplyNoiseToTrend(float lastPrice, float deltaTime, TrendThresholds thresholds)
        {
            if (deltaTime <= 0f) return;

            // 用實際價格變化算出真實速率
            float actualRate = (CurrentPrice - lastPrice) / deltaTime;

            // 判定基礎方向（由目標決定）
            float targetDirection = TargetPrice - CurrentPrice;

            // 用實際速率判定趨勢
            TrendLevel noisyTrend = GetTrendFromRate(actualRate, thresholds);

            // 限制不反轉：如果目標是漲，趨勢最低只到 Flat
            if (targetDirection > 0 && (noisyTrend == TrendLevel.SmallDrop || noisyTrend == TrendLevel.BigDrop))
                noisyTrend = TrendLevel.Flat;
            else if (targetDirection < 0 && (noisyTrend == TrendLevel.SmallRise || noisyTrend == TrendLevel.BigRise))
                noisyTrend = TrendLevel.Flat;

            CurrentTrend = noisyTrend;
        }

        private TrendLevel GetTrendFromRate(float ratePerSecond, TrendThresholds t)
        {
            // 用相對於初始價的比率來判定趨勢，避免高價位時漂移顯示過大
            float relativeRate = (InitialPrice > 0) ? Mathf.Abs(ratePerSecond) / InitialPrice * 100f : Mathf.Abs(ratePerSecond);
            
            if (ratePerSecond >= 0)
            {
                if (relativeRate >= t.BigRiseThreshold) return TrendLevel.BigRise;
                if (relativeRate >= t.SmallRiseThreshold) return TrendLevel.SmallRise;
            }
            else
            {
                if (relativeRate >= t.BigRiseThreshold) return TrendLevel.BigDrop;
                if (relativeRate >= t.SmallRiseThreshold) return TrendLevel.SmallDrop;
            }
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
        [Tooltip("每秒變化率（佔初始價%）超過此值判定為小漲/小跌")]
        public float SmallRiseThreshold = 0.2f;

        [Tooltip("每秒變化率（佔初始價%）超過此值判定為大漲/大跌")]
        public float BigRiseThreshold = 0.8f;
    }
}