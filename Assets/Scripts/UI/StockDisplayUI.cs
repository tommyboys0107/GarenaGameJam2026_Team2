using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Data;

namespace UI
{
    /// <summary>
    /// 顯示三支股票的名稱、代號和目前價格。
    /// 對應示意圖左上方的 A/B/C 股票標示。
    /// </summary>
    public class StockDisplayUI : MonoBehaviour
    {
        [Header("股票 A (NARC)")]
        [SerializeField] private TextMeshProUGUI stockALabel;
        [SerializeField] private TextMeshProUGUI stockAPrice;

        [Header("股票 B (LOCK)")]
        [SerializeField] private TextMeshProUGUI stockBLabel;
        [SerializeField] private TextMeshProUGUI stockBPrice;

        [Header("股票 C (BYTE)")]
        [SerializeField] private TextMeshProUGUI stockCLabel;
        [SerializeField] private TextMeshProUGUI stockCPrice;

        [Header("顏色設定")]
        [SerializeField] private Color upColor = new Color(0.2f, 1f, 0.2f);
        [SerializeField] private Color downColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private Color neutralColor = Color.white;

        private TextMeshProUGUI[] _labels;
        private TextMeshProUGUI[] _prices;
        private List<StockInfo> _stocks;

        public void Initialize(List<StockInfo> stocks)
        {
            _stocks = stocks;
            _labels = new[] { stockALabel, stockBLabel, stockCLabel };
            _prices = new[] { stockAPrice, stockBPrice, stockCPrice };

            for (int i = 0; i < 3 && i < stocks.Count; i++)
            {
                if (_labels[i] != null)
                    _labels[i].text = $"{stocks[i].code}";

                UpdatePrice(i);
            }
        }

        /// <summary>
        /// 更新所有股票的價格顯示。
        /// </summary>
        public void RefreshAll()
        {
            if (_stocks == null) return;
            for (int i = 0; i < 3 && i < _stocks.Count; i++)
                UpdatePrice(i);
        }

        /// <summary>
        /// 更新指定股票的價格顯示。
        /// </summary>
        public void UpdatePrice(int index)
        {
            if (_stocks == null || index < 0 || index >= _stocks.Count) return;
            if (_prices[index] == null) return;

            var stock = _stocks[index];
            _prices[index].text = $"${stock.currentPrice:F0}";

            // 根據漲跌設定顏色
            float diff = stock.currentPrice - stock.initialPrice;
            if (diff > 0.5f)
                _prices[index].color = upColor;
            else if (diff < -0.5f)
                _prices[index].color = downColor;
            else
                _prices[index].color = neutralColor;
        }
    }
}
