using UnityEngine;
using UnityEngine.UIElements;

namespace BlackMarketTrader
{
    /// <summary>
    /// 股市線圖 UI 控制器，連接 StockMarketManager 與 UI Toolkit
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class StockChartUIController : MonoBehaviour
    {
        [SerializeField] private StockMarketManager _marketManager;

        private UIDocument _uiDocument;
        private StockChartElement _chartElement;
        private Label[] _stockNameLabels;
        private Label[] _stockTrendLabels;
        private VisualElement _eventLabelContainer;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            var root = _uiDocument.rootVisualElement;
            if (root == null) return;

            // 程式碼建立 StockChartElement 並插入 chart-area
            var chartArea = root.Q<VisualElement>("chart-area");
            if (chartArea != null)
            {
                _chartElement = new StockChartElement();
                _chartElement.name = "stock-chart";
                _chartElement.AddToClassList("stock-chart");
                _chartElement.style.flexGrow = 1;
                chartArea.Insert(0, _chartElement);
            }

            // 取得商品名稱與趨勢標籤
            _stockNameLabels = new Label[3];
            _stockTrendLabels = new Label[3];

            for (int i = 0; i < 3; i++)
            {
                _stockNameLabels[i] = root.Q<Label>($"stock-name-{i}");
                _stockTrendLabels[i] = root.Q<Label>($"stock-trend-{i}");
            }

            _eventLabelContainer = root.Q<VisualElement>("event-labels");

            // 註冊事件
            if (_marketManager != null)
            {
                _marketManager.OnPriceUpdated += RefreshChart;
                _marketManager.OnEventTriggered += OnEventTriggered;
            }
        }

        private void OnDisable()
        {
            if (_marketManager != null)
            {
                _marketManager.OnPriceUpdated -= RefreshChart;
                _marketManager.OnEventTriggered -= OnEventTriggered;
            }
        }

        private void RefreshChart()
        {
            if (_chartElement == null || _marketManager == null) return;

            // 更新圖表
            _chartElement.UpdateData(
                _marketManager.Stocks,
                _marketManager.Events,
                _marketManager.CurrentTimeIndex,
                5f,   // minPrice
                100f, // maxPrice
                60    // maxDataPoints
            );

            // 更新商品趨勢標籤
            for (int i = 0; i < 3 && i < _marketManager.Stocks.Length; i++)
            {
                var stock = _marketManager.Stocks[i];

                if (_stockNameLabels[i] != null)
                {
                    _stockNameLabels[i].text = stock.Name;
                }

                if (_stockTrendLabels[i] != null)
                {
                    _stockTrendLabels[i].text = stock.GetTrendDisplayText();
                    _stockTrendLabels[i].style.color = stock.GetTrendColor();
                }
            }
        }

        private void OnEventTriggered(StockEvent stockEvent)
        {
            if (_eventLabelContainer == null) return;

            var label = new Label(stockEvent.EventName);
            label.AddToClassList("event-marker-label");
            _eventLabelContainer.Add(label);

            // 限制顯示數量
            while (_eventLabelContainer.childCount > 5)
            {
                _eventLabelContainer.RemoveAt(0);
            }

            RefreshChart();
        }

        public void ForceRefresh()
        {
            RefreshChart();
        }
    }
}