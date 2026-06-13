using UnityEngine;
using UnityEngine.UIElements;
using Data;
using Gameplay;

namespace BlackMarketTrader
{
    [RequireComponent(typeof(UIDocument))]
    public class StockChartUIController : MonoBehaviour
    {
        [SerializeField] private StockMarketManager _marketManager;
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private GameFlowController _gameFlowController;
        [SerializeField] private ChartDrawSettings _chartSettings = new ChartDrawSettings();

        private UIDocument _uiDocument;
        private StockChartElement _chartElement;
        private Label[] _stockNameLabels;
        private Label[] _stockTrendLabels;
        private Label[] _stockPriceLabels;
        private Label[] _yAxisLabels;
        private Label _timerLabel;
        private VisualElement _eventLabelContainer;

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
                _chartElement.SetDrawSettings(_chartSettings);
                chartArea.Insert(0, _chartElement);
            }

            _stockNameLabels = new Label[3];
            _stockTrendLabels = new Label[3];
            _stockPriceLabels = new Label[3];
            for (int i = 0; i < 3; i++)
            {
                _stockNameLabels[i] = root.Q<Label>($"stock-name-{i}");
                _stockTrendLabels[i] = root.Q<Label>($"stock-trend-{i}");
                _stockPriceLabels[i] = root.Q<Label>($"stock-price-{i}");
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

            if (_marketManager != null)
            {
                _marketManager.OnPriceUpdated += RefreshChart;
                _marketManager.OnEventTriggered += OnEventTriggered;
            }

            if (_gameFlowController != null)
            {
                _gameFlowController.OnTraderEventResolved += OnTraderResolved;
                _gameFlowController.OnAudienceEventResolved += OnAudienceResolved;
            }
        }

        private void OnDisable()
        {
            if (_marketManager != null)
            {
                _marketManager.OnPriceUpdated -= RefreshChart;
                _marketManager.OnEventTriggered -= OnEventTriggered;
            }

            if (_gameFlowController != null)
            {
                _gameFlowController.OnTraderEventResolved -= OnTraderResolved;
                _gameFlowController.OnAudienceEventResolved -= OnAudienceResolved;
            }
        }

        private void Update()
        {
            UpdateTimerDisplay();
        }

        private void UpdateTimerDisplay()
        {
            if (_timerLabel == null || _gameManager == null) return;
            float time = _gameManager.TimeRemaining;
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
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
            UpdateEventLabels();

            for (int i = 0; i < 3 && i < _marketManager.Stocks.Length; i++)
            {
                var stock = _marketManager.Stocks[i];

                if (_stockNameLabels[i] != null)
                    _stockNameLabels[i].text = stock.Name;

                if (_stockPriceLabels[i] != null)
                {
                    _stockPriceLabels[i].text = $"${stock.CurrentPrice:F0}";
                    _stockPriceLabels[i].style.color = stock.GetTrendColor();
                }

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

private void UpdateEventLabels()
        {
            if (_eventLabelContainer == null || _marketManager == null) return;

            _eventLabelContainer.Clear();

            var events = _marketManager.Events;
            if (events == null || events.Count == 0) return;

            int maxDataPoints = 60;
            int visibleStart = Mathf.Max(0, _marketManager.CurrentTimeIndex - maxDataPoints + 1);
            float containerWidth = _eventLabelContainer.resolvedStyle.width;
            if (containerWidth <= 0) return;

            float paddingLeft = _chartSettings.PaddingLeft;
            float paddingRight = _chartSettings.PaddingRight;
            float chartWidth = containerWidth - paddingLeft - paddingRight;

            foreach (var evt in events)
            {
                if (evt.TimeIndex < visibleStart || evt.TimeIndex > _marketManager.CurrentTimeIndex) continue;

                int relativeIndex = evt.TimeIndex - visibleStart;
                float x = paddingLeft + (chartWidth / (maxDataPoints - 1)) * relativeIndex;

                var label = new Label(evt.EventName);
                label.AddToClassList("event-marker-label");
                label.style.position = Position.Absolute;
                label.style.left = x;
                label.style.bottom = 8;
                _eventLabelContainer.Add(label);
            }
        }


        private void OnEventTriggered(StockEvent stockEvent)
        {
            // label 位置在 RefreshChart 中統一更新
            RefreshChart();
        }

        public void ForceRefresh()
        {
            RefreshChart();
        }

        /// <summary>
        /// 玩家事件結果 — 顯示選項名稱在線圖上
        /// </summary>
        private void OnTraderResolved(ChoiceInfo choice)
        {
            if (_marketManager == null || choice == null) return;
            ApplyEffectsAndMark(choice.name, choice.effects);
        }

        /// <summary>
        /// 觀眾事件結果 — 顯示事件名稱在線圖上
        /// </summary>
        private void OnAudienceResolved(AudienceEventInfo audienceEvent)
        {
            if (_marketManager == null || audienceEvent == null) return;
            ApplyEffectsAndMark(audienceEvent.name, audienceEvent.effects);
        }

        private void ApplyEffectsAndMark(string eventName, StockEffect[] effects)
        {
            if (effects == null) return;

            foreach (var effect in effects)
            {
                for (int i = 0; i < _marketManager.Stocks.Length; i++)
                {
                    string code = i switch
                    {
                        0 => "NARC",
                        1 => "LOCK",
                        2 => "BYTE",
                        _ => ""
                    };

                    if (effect.stockCode == code)
                    {
                        _marketManager.Stocks[i].CurrentPrice = Mathf.Clamp(
                            _marketManager.Stocks[i].CurrentPrice + effect.value,
                            _marketManager.MinPrice,
                            _marketManager.MaxPrice
                        );
                    }
                }
            }

            // 在線圖上標記事件（顯示選項/事件名稱）
            _marketManager.TriggerEvent(eventName, -1, _marketManager.Stocks[0].CurrentTrend);
            RefreshChart();
        }
    }
}