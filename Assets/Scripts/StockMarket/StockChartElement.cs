using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlackMarketTrader
{
    /// <summary>
    /// 股市線圖 UI Toolkit 顯示元件，負責繪製三條價格曲線及事件標記
    /// </summary>
    public class StockChartElement : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<StockChartElement, UxmlTraits> { }

        private StockData[] _stocks;
        private List<StockEvent> _events;
        private int _currentTimeIndex;
        private float _minPrice = 5f;
        private float _maxPrice = 100f;
        private int _maxDataPoints = 60;

        // 繪製參數
        private const float PADDING_LEFT = 10f;
        private const float PADDING_RIGHT = 20f;
        private const float PADDING_TOP = 30f;
        private const float PADDING_BOTTOM = 20f;
        private const float LINE_WIDTH = 6f;
        private const float EVENT_LINE_WIDTH = 1.5f;
        private const float GRID_LINE_WIDTH = 0.5f;

        public StockChartElement()
        {
            generateVisualContent += OnGenerateVisualContent;
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

            float chartLeft = PADDING_LEFT;
            float chartRight = rect.width - PADDING_RIGHT;
            float chartTop = PADDING_TOP;
            float chartBottom = rect.height - PADDING_BOTTOM;
            float chartWidth = chartRight - chartLeft;
            float chartHeight = chartBottom - chartTop;

            // 繪製背景格線
            DrawGrid(painter, chartLeft, chartTop, chartWidth, chartHeight);

            // 繪製事件標記線
            DrawEventMarkers(painter, chartLeft, chartTop, chartWidth, chartHeight);

            // 繪製三條股價線
            foreach (var stock in _stocks)
            {
                DrawStockLine(painter, stock, chartLeft, chartTop, chartWidth, chartHeight);
            }
        }

        private void DrawGrid(Painter2D painter, float left, float top, float width, float height)
        {
            var gridColor = new Color(0.3f, 0.3f, 0.3f, 0.4f);
            painter.strokeColor = gridColor;
            painter.lineWidth = GRID_LINE_WIDTH;

            // 水平線 (價格刻度)
            int horizontalLines = 5;
            for (int i = 0; i <= horizontalLines; i++)
            {
                float y = top + (height / horizontalLines) * i;
                painter.BeginPath();
                painter.MoveTo(new Vector2(left, y));
                painter.LineTo(new Vector2(left + width, y));
                painter.Stroke();
            }

            // 垂直線 (時間刻度)
            int verticalLines = 6;
            for (int i = 0; i <= verticalLines; i++)
            {
                float x = left + (width / verticalLines) * i;
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
            painter.lineWidth = LINE_WIDTH;
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
                // Y軸反轉（價格高在上方）
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

            var eventLineColor = new Color(1f, 1f, 0f, 0.7f); // 黃色
            painter.strokeColor = eventLineColor;
            painter.lineWidth = EVENT_LINE_WIDTH;

            // 計算當前可見範圍
            int visibleStart = Mathf.Max(0, _currentTimeIndex - _maxDataPoints + 1);

            foreach (var evt in _events)
            {
                // 只繪製可見範圍內的事件
                if (evt.TimeIndex < visibleStart || evt.TimeIndex > _currentTimeIndex) continue;

                int relativeIndex = evt.TimeIndex - visibleStart;
                float x = left + (width / (_maxDataPoints - 1)) * relativeIndex;

                // 繪製垂直虛線
                painter.BeginPath();
                painter.MoveTo(new Vector2(x, top));
                painter.LineTo(new Vector2(x, top + height));
                painter.Stroke();
            }
        }
    }
}