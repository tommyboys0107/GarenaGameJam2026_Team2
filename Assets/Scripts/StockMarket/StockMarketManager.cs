using System;
using System.Collections.Generic;
using UnityEngine;
using Data;

namespace BlackMarketTrader
{
    /// <summary>
    /// 股市系統管理器
    /// 新算法：目標價制 — 事件/自然漂移設定目標價，價格在指定時間內平滑移動到目標
    /// 趨勢等級由「每秒變化率」自動判定
    /// </summary>
    public class StockMarketManager : MonoBehaviour
    {
        [Header("更新設定")]
        [SerializeField] private float _tickInterval = 1f;
        [SerializeField] private float _minPrice = 0f;
        [SerializeField] private float _maxPrice = 1000f;
        [SerializeField] private int _maxDataPoints = 60;

        [Header("目標價變化時間（秒）")]
        [Tooltip("價格從當前移動到目標價的時間")]
        [SerializeField] private float _transitionDuration = 5f;

        [Header("自然漂移")]
        [SerializeField] private bool _enableDrift = true;
        [SerializeField] private float _driftInterval = 10f;
        [Tooltip("自然漂移的百分比範圍 (相對於初始價)")]
        [SerializeField] private float _driftPercentMin = -3f;
        [SerializeField] private float _driftPercentMax = 3f;

        [Header("趨勢判定門檻")]
        [SerializeField] private TrendThresholds _trendThresholds = new TrendThresholds();

        [Header("波動倍率")]
        [SerializeField] private float _volatilityA = 1f;
        [SerializeField] private float _volatilityB = 1.5f;
        [SerializeField] private float _volatilityC = 0.8f;

        public float MinPrice => _minPrice;
        public float MaxPrice => _maxPrice;
        public float TransitionDuration => _transitionDuration;

        public StockData[] Stocks { get; private set; }
        public List<StockEvent> Events { get; private set; } = new List<StockEvent>();
        public int CurrentTimeIndex { get; private set; } = 0;

        public event Action OnPriceUpdated;
        public event Action<StockEvent> OnEventTriggered;

        private float _tickTimer;
        private float _driftTimer;
        private bool _isRunning = false;

        private void Awake()
        {
            InitializeStocks();
        }

        private void InitializeStocks()
        {
            var stockInfos = CSVLoader.LoadStocks();
            Stocks = new StockData[3];

            Color[] colors = {
                new Color(1f, 0.3f, 0.3f),
                new Color(0.3f, 0.5f, 1f),
                new Color(0.3f, 1f, 0.3f)
            };
            float[] volatilities = { _volatilityA, _volatilityB, _volatilityC };

            for (int i = 0; i < 3 && i < stockInfos.Count; i++)
            {
                var info = stockInfos[i];
                Stocks[i] = new StockData
                {
                    Name = info.name,
                    LineColor = colors[i],
                    InitialPrice = info.initialPrice,
                    CurrentPrice = info.initialPrice,
                    TargetPrice = info.initialPrice,
                    Volatility = volatilities[i],
                    TransitionTimeRemaining = 0f
                };
            }

            foreach (var stock in Stocks)
            {
                stock.PriceHistory.Add(stock.CurrentPrice);
            }
        }

        public void StartMarket()
        {
            _isRunning = true;
            _tickTimer = 0f;
            _driftTimer = 0f;
        }

        public void StopMarket()
        {
            _isRunning = false;
        }

        private void Update()
        {
            if (!_isRunning) return;

            // 每幀更新價格（平滑移動向目標）
            foreach (var stock in Stocks)
            {
                stock.UpdatePrice(Time.deltaTime, _minPrice, _maxPrice, _trendThresholds);
            }

            // 定期記錄數據點
            _tickTimer += Time.deltaTime;
            if (_tickTimer >= _tickInterval)
            {
                _tickTimer -= _tickInterval;
                RecordDataPoint();
            }

            // 自然漂移
            if (_enableDrift)
            {
                _driftTimer += Time.deltaTime;
                if (_driftTimer >= _driftInterval)
                {
                    _driftTimer -= _driftInterval;
                    NaturalDrift();
                }
            }
        }

        private void RecordDataPoint()
        {
            CurrentTimeIndex++;

            foreach (var stock in Stocks)
            {
                stock.PriceHistory.Add(stock.CurrentPrice);
                if (stock.PriceHistory.Count > _maxDataPoints)
                    stock.PriceHistory.RemoveAt(0);
            }

            OnPriceUpdated?.Invoke();
        }

        /// <summary>
        /// 自然漂移：隨機設定一個小幅的目標價
        /// </summary>
        private void NaturalDrift()
        {
            foreach (var stock in Stocks)
            {
                float driftPercent = UnityEngine.Random.Range(_driftPercentMin, _driftPercentMax);
                float newTarget = stock.CurrentPrice * (1f + driftPercent / 100f);
                newTarget = Mathf.Clamp(newTarget, _minPrice, _maxPrice);
                stock.SetTargetAbsolute(newTarget, _transitionDuration);
            }
        }

        /// <summary>
        /// 觸發事件，將效果加到目標價
        /// </summary>
        /// <param name="eventName">事件名稱（顯示在線圖上）</param>
        /// <param name="stockIndex">影響的商品 (0=NARC, 1=LOCK, 2=BYTE)，-1=不改變趨勢</param>
        /// <param name="newTrend">未使用（趨勢現在由速率自動判定）</param>
        public void TriggerEvent(string eventName, int stockIndex, TrendLevel newTrend)
        {
            var stockEvent = new StockEvent
            {
                TimeIndex = CurrentTimeIndex,
                EventName = eventName
            };
            Events.Add(stockEvent);
            OnEventTriggered?.Invoke(stockEvent);
        }

        /// <summary>
        /// 套用股票效果（由事件觸發）
        /// deltaValue 是相對於起始價的百分比（如 +20 = 起始價的 +20%）
        /// 效果會累加到目標價上
        /// </summary>
        public void ApplyStockEffect(string stockCode, int deltaValue)
        {
            for (int i = 0; i < Stocks.Length; i++)
            {
                string code = i switch
                {
                    0 => "NARC",
                    1 => "LOCK",
                    2 => "BYTE",
                    _ => ""
                };

                if (stockCode == code)
                {
                    // deltaValue 是起始價的百分比
                    float absoluteDelta = Stocks[i].InitialPrice * (deltaValue / 100f);
                    float newTarget = Stocks[i].TargetPrice + absoluteDelta;
                    newTarget = Mathf.Clamp(newTarget, _minPrice, _maxPrice);
                    Stocks[i].SetTargetAbsolute(newTarget, _transitionDuration);
                }
            }
        }

        public void ResetMarket()
        {
            _isRunning = false;
            CurrentTimeIndex = 0;
            Events.Clear();
            InitializeStocks();
        }
    }
}