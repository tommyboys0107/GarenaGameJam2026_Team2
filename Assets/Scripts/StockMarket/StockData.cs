using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlackMarketTrader
{
    /// <summary>
    /// 趨勢等級
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
        public int TimeIndex;       // 發生在哪個時間點
        public string EventName;    // 事件名稱
    }

    /// <summary>
    /// 單一商品的股市資料
    /// </summary>
    [Serializable]
    public class StockData
    {
        public string Name;             // 商品名稱 (A, B, C)
        public Color LineColor;         // 線的顏色
        public TrendLevel CurrentTrend; // 當前趨勢等級
        public List<float> PriceHistory = new List<float>(); // 價格歷史記錄
        public float CurrentPrice;      // 當前價格
        public float InitialPrice;      // 初始價格（各商品可不同）
        public float Volatility = 1f;   // 波動倍率（影響趨勢變化速度）

        // 趨勢對應的價格變化範圍，乘以 Volatility
        public float GetPriceChange()
        {
            float baseChange;
            switch (CurrentTrend)
            {
                case TrendLevel.BigRise:
                    baseChange = UnityEngine.Random.Range(3f, 6f);
                    break;
                case TrendLevel.SmallRise:
                    baseChange = UnityEngine.Random.Range(0.5f, 2.5f);
                    break;
                case TrendLevel.Flat:
                    baseChange = UnityEngine.Random.Range(-0.5f, 0.5f);
                    break;
                case TrendLevel.SmallDrop:
                    baseChange = UnityEngine.Random.Range(-2.5f, -0.5f);
                    break;
                case TrendLevel.BigDrop:
                    baseChange = UnityEngine.Random.Range(-6f, -3f);
                    break;
                default:
                    baseChange = 0f;
                    break;
            }
            return baseChange * Volatility;
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
                case TrendLevel.Flat:      return "→";
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
                case TrendLevel.BigRise:   return new Color(1f, 0.2f, 0.2f);   // 紅色
                case TrendLevel.SmallRise: return new Color(1f, 0.5f, 0.3f);   // 橘紅
                case TrendLevel.Flat:      return new Color(0.8f, 0.8f, 0.8f); // 灰白
                case TrendLevel.SmallDrop: return new Color(0.3f, 0.8f, 0.3f); // 淺綠
                case TrendLevel.BigDrop:   return new Color(0.2f, 1f, 0.2f);   // 綠色
                default: return Color.white;
            }
        }
    }
}