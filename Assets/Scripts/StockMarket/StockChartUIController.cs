using UnityEngine;
using UnityEngine.UIElements;
using Data;

namespace BlackMarketTrader
{
    [RequireComponent(typeof(UIDocument))]
    public class StockChartUIController : MonoBehaviour
    {
        [SerializeField] private StockMarketManager _marketManager;

        private UIDocument _uiDocument;
        private StockChartElement _chartElement;
        private Label[] _stockNameLabels;
        private Label[] _stockTrendLabels;
        private Label[] _yAxisLabels;
        private Label _timerLabel;
        private VisualElement _eventLabelContainer;

        private float _totalGameTime;
        private float _timeRemaining;
        private bool _timerRunning;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            var root = _uiDocument.rootVisualElement;
            if (root == null) return;

            var chartArea = root.Q<VisualElement>("chart-area");
            if (chartArea != null)
            {
                _chartElement = new StockChartElement();
                _chartElement.name = "stock-chart";
                _chartElement.AddToClassList("stock-chart");
                _chartElement.style.flexGrow = 1;
                chartArea.Insert(0, _chartElement);
            }

            _stockNameLabels = new Label[3];
            _stockTrendLabels = new Label[3];
            for (int i = 0; i < 3; i++)
            {
                _stockNameLabels[i] = root.Q<Label>($"stock-name-{i}");
                _stockTrendLabels[i] = root.Q<Label>($"stock-trend-{i}");
            }

            // Y軸刻度 Label
            _yAxisLabels = new Label[6];
            for (int i = 0; i < 6; i++)
            {
                _yAxisLabels[i] = root.Q<Label>($"y-label-{i}");
            }

            _eventLabelContainer = root.Q<VisualElement>("event-labels");

            // Timer label
            _timerLabel = root.Q<Label>("timer-label");
            var timeConfig = CSVLoader.LoadTimeConfig();
            _totalGameTime = timeConfig.totalGameTime;
            _timeRemaining = _totalGameTime;
            _timerRunning = true;
            UpdateTimerDisplay();

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

        private void Update()
        {
            if (!_timerRunning) return;

            _timeRemaining -= Time.deltaTime;
            if (_timeRemaining <= 0f)
            {
                _timeRemaining = 0f;
                _timerRunning = false;
            }
            UpdateTimerDisplay();
        }

        private void UpdateTimerDisplay()
        {
            if (_timerLabel == null) return;
            int minutes = Mathf.FloorToInt(_timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(_timeRemaining % 60f);
            _timerLabel.text = $"{minutes:00}:{seconds:00}";
        }

        private void RefreshChart()
        {
            if (_chartElement == null || _marketManager == null) return;

            float minPrice = _marketManager.MinPrice;
            float maxPrice = _marketManager.MaxPrice;

            _chartElement.UpdateData(
                _marketManager.Stocks,
                _marketManager.Events,
                _marketManager.CurrentTimeIndex,
                minPrice,
                maxPrice,
                60
            );

            UpdateYAxisLabels(minPrice, maxPrice);

            for (int i = 0; i < 3 && i < _marketManager.Stocks.Length; i++)
            {
                var stock = _marketManager.Stocks[i];

                if (_stockNameLabels[i] != null)
                    _stockNameLabels[i].text = stock.Name;

                if (_stockTrendLabels[i] != null)
                {
                    _stockTrendLabels[i].text = stock.GetTrendDisplayText();
                    _stockTrendLabels[i].style.color = stock.GetTrendColor();
                }
            }
        }

        private void UpdateYAxisLabels(float minPrice, float maxPrice)
        {
            if (_yAxisLabels == null) return;

            for (int i = 0; i < _yAxisLabels.Length; i++)
            {
                if (_yAxisLabels[i] == null) continue;
                // label-0 = top (max), label-5 = bottom (min)
                float t = (float)i / (_yAxisLabels.Length - 1);
                float value = Mathf.Lerp(maxPrice, minPrice, t);
                _yAxisLabels[i].text = $"{value:F0}";
            }
        }

        private void OnEventTriggered(StockEvent stockEvent)
        {
            if (_eventLabelContainer == null) return;

            var label = new Label(stockEvent.EventName);
            label.AddToClassList("event-marker-label");
            _eventLabelContainer.Add(label);

            while (_eventLabelContainer.childCount > 5)
                _eventLabelContainer.RemoveAt(0);

            RefreshChart();
        }

        public void ForceRefresh()
        {
            RefreshChart();
        }
    }
}