using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlackMarketTrader
{
    /// <summary>
    /// 線圖繪製設定，可在 Inspector 上調整
    /// </summary>
    [Serializable]
    public class ChartDrawSettings
    {
        [Header("繪圖範圍 (Padding)")]
        public float PaddingLeft = 10f;
        public float PaddingRight = 20f;
        public float PaddingTop = 30f;
        public float PaddingBottom = 20f;

        [Header("線條粗細")]
        public float StockLineWidth = 6f;
        public float EventLineWidth = 1.5f;
        public float GridLineWidth = 0.5f;

        [Header("格線設定")]
        public int HorizontalLines = 5;
        public int VerticalLines = 6;
        public Color GridColor = new Color(0.3f, 0.3f, 0.3f, 0.4f);

        [Header("事件標記")]
        public Color EventLineColor = new Color(1f, 1f, 0f, 0.7f);
    }

    /// <summary>
    /// 股市線圖 UI Toolkit 顯示元件，負責繪製三條價格曲線及事件標記
    /// </summary>
    public class StockChartElement : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<StockChartElement, UxmlTraits> { }

        private StockData[] _stocks;
        private List<StockEvent> _events;
        private int _currentTimeIndex;
        private float _minPrice = 0f;
        private float _maxPrice = 1000f;
        private int _maxDataPoints = 60;
        private ChartDrawSettings _settings = new ChartDrawSettings();

        public StockChartElement()
        {
            generateVisualContent += OnGenerateVisualContent;
        }

        /// <summary>
        /// 設定繪圖參數
        /// </summary>
        public void SetDrawSettings(ChartDrawSettings settings)
        {
            _settings = settings ?? new ChartDrawSettings();
            MarkDirtyRepaint();
        }

        /// <summary>
        /// 更新圖表數據
        /// </summary>
        public void UpdateData(StockData[] stocks, List<StockEvent> events, int currentTimeIndex,
            float minPrice, float maxPrice, int maxDataPoints)
        {
            _stocks = stocks;
            _events = events;
            _currentTimeIndex = currentTimeIndex;
            _minPrice = minPrice;
            _maxPrice = maxPrice;
            _maxDataPoints = maxDataPoints;
            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            if (_stocks == null || _stocks.Length == 0) return;

            var painter = ctx.painter2D;
            var rect = contentRect;

            if (rect.width < 1f || rect.height < 1f) return;

            float chartLeft = _settings.PaddingLeft;
            float chartRight = rect.width - _settings.PaddingRight;
            float chartTop = _settings.PaddingTop;
            float chartBottom = rect.height - _settings.PaddingBottom;
            float chartWidth = chartRight - chartLeft;
            float chartHeight = chartBottom - chartTop;

            DrawGrid(painter, chartLeft, chartTop, chartWidth, chartHeight);
            DrawEventMarkers(painter, chartLeft, chartTop, chartWidth, chartHeight);

            foreach (var stock in _stocks)
            {
                DrawStockLine(painter, stock, chartLeft, chartTop, chartWidth, chartHeight);
            }
        }

        private void DrawGrid(Painter2D painter, float left, float top, float width, float height)
        {
            painter.strokeColor = _settings.GridColor;
            painter.lineWidth = _settings.GridLineWidth;

            int hLines = _settings.HorizontalLines;
            for (int i = 0; i <= hLines; i++)
            {
                float y = top + (height / hLines) * i;
                painter.BeginPath();
                painter.MoveTo(new Vector2(left, y));
                painter.LineTo(new Vector2(left + width, y));
                painter.Stroke();
            }

            int vLines = _settings.VerticalLines;
            for (int i = 0; i <= vLines; i++)
            {
                float x = left + (width / vLines) * i;
                painter.BeginPath();
                painter.MoveTo(new Vector2(x, top));
                painter.LineTo(new Vector2(x, top + height));
                painter.Stroke();
            }
        }

        private void DrawStockLine(Painter2D painter, StockData stock,
            float left, float top, float width, float height)
        {
            if (stock.PriceHistory.Count < 2) return;

            painter.strokeColor = stock.LineColor;
            painter.lineWidth = _settings.StockLineWidth;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;
            painter.BeginPath();

            int dataCount = stock.PriceHistory.Count;
            int pointsToDraw = Mathf.Min(dataCount, _maxDataPoints);
            int startIndex = Mathf.Max(0, dataCount - _maxDataPoints);

            for (int i = 0; i < pointsToDraw; i++)
            {
                float x = left + (width / (_maxDataPoints - 1)) * i;
                float normalizedPrice = Mathf.InverseLerp(_minPrice, _maxPrice, stock.PriceHistory[startIndex + i]);
                float y = top + height * (1f - normalizedPrice);

                if (i == 0)
                    painter.MoveTo(new Vector2(x, y));
                else
                    painter.LineTo(new Vector2(x, y));
            }

            painter.Stroke();
        }

        private void DrawEventMarkers(Painter2D painter, float left, float top, float width, float height)
        {
            if (_events == null || _events.Count == 0) return;

            painter.strokeColor = _settings.EventLineColor;
            painter.lineWidth = _settings.EventLineWidth;

            int visibleStart = Mathf.Max(0, _currentTimeIndex - _maxDataPoints + 1);

            foreach (var evt in _events)
            {
                if (evt.TimeIndex < visibleStart || evt.TimeIndex > _currentTimeIndex) continue;

                int relativeIndex = evt.TimeIndex - visibleStart;
                float x = left + (width / (_maxDataPoints - 1)) * relativeIndex;

                painter.BeginPath();
                painter.MoveTo(new Vector2(x, top));
                painter.LineTo(new Vector2(x, top + height));
                painter.Stroke();
            }
        }
    }
}