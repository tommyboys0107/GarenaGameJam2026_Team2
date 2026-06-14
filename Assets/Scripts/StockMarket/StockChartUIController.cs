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
        private VisualElement _avatarPortrait;

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

            // 角色頭像：根據選擇的角色載入對應圖片
            _avatarPortrait = root.Q<VisualElement>("avatar-portrait");
            LoadCharacterFace();

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

            // 動態調整 Y 軸範圍：根據可見數據的實際最大/最小值
            float dataMin = float.MaxValue;
            float dataMax = float.MinValue;
            foreach (var stock in _marketManager.Stocks)
            {
                foreach (var price in stock.PriceHistory)
                {
                    if (price < dataMin) dataMin = price;
                    if (price > dataMax) dataMax = price;
                }
            }

            if (dataMin < float.MaxValue && dataMax > float.MinValue)
            {
                float range = dataMax - dataMin;
                float padding = Mathf.Max(range * 0.15f, 10f); // 至少留 10 的 padding
                minPrice = Mathf.Max(_marketManager.MinPrice, dataMin - padding);
                maxPrice = Mathf.Min(_marketManager.MaxPrice, dataMax + padding);
                // 確保最小範圍
                if (maxPrice - minPrice < 20f)
                {
                    float mid = (dataMin + dataMax) * 0.5f;
                    minPrice = mid - 10f;
                    maxPrice = mid + 10f;
                }
            }

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
                    // 顯示漲跌幅百分比（相對於初始價格）
                    float changePercent = (stock.InitialPrice > 0)
                        ? (stock.CurrentPrice - stock.InitialPrice) / stock.InitialPrice * 100f
                        : 0f;
                    string sign = changePercent >= 0 ? "+" : "";
                    _stockPriceLabels[i].text = $"{sign}{changePercent:F1}%";
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
            if (effects == null || _marketManager == null) return;

            foreach (var effect in effects)
            {
                _marketManager.ApplyStockEffect(effect.stockCode, effect.value);
            }

            // 在線圖上標記事件
            _marketManager.TriggerEvent(eventName, -1, TrendLevel.Flat);
            RefreshChart();
        }

        /// <summary>
        /// 根據 GameData.SelectedCharacterIndex 載入對應角色頭像。
        /// 使用 CharacterData.csv 中的 SelectImageID 欄位。
        /// </summary>
        private void LoadCharacterFace()
        {
            if (_avatarPortrait == null) return;

            int charIndex = Core.GameData.SelectedCharacterIndex;

            // 從 CharacterDataManager 取得 SelectImageID
            Core.CharacterDataManager.LoadData();
            var character = Core.CharacterDataManager.GetCharacterByIndex(charIndex);

            string selectId;
            if (character != null && !string.IsNullOrEmpty(character.SelectImageID))
            {
                selectId = character.SelectImageID;
            }
            else
            {
                selectId = $"C{(charIndex + 1):D2}_C";
            }

            string path = $"CharacterImages/{selectId}";
            var texture = Resources.Load<Texture2D>(path);
            if (texture != null)
            {
                _avatarPortrait.style.backgroundImage = new StyleBackground(texture);
                _avatarPortrait.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            }
            else
            {
                var fallback = Resources.Load<Texture2D>("CharacterImages/C01_C");
                if (fallback != null)
                {
                    _avatarPortrait.style.backgroundImage = new StyleBackground(fallback);
                    _avatarPortrait.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                }
                Debug.LogWarning($"[StockChartUI] 找不到角色頭像: {path}");
            }
        }
    }
}