using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlackMarketTrader
{
    /// <summary>
    /// 股市系統管理器，控制三個商品的價格變動與趨勢邏輯
    /// </summary>
    public class StockMarketManager : MonoBehaviour
    {
        [Header("Stock Settings")]
        [SerializeField] private float _tickInterval = 1f;        // 每幾秒更新一次價格
        [SerializeField] private float _trendChangeInterval = 10f; // 每幾秒隨機變更趨勢
        [SerializeField] private float _minPrice = 0f;            // 最低價格
        [SerializeField] private float _maxPrice = 1000f;          // 最高價格
        [SerializeField] private int _maxDataPoints = 60;         // 最多顯示幾個數據點

        [Header("Stock A")]
        [SerializeField] private float _initialPriceA = 500f;
        [SerializeField] private float _volatilityA = 1f;

        [Header("Stock B")]
        [SerializeField] private float _initialPriceB = 300f;
        [SerializeField] private float _volatilityB = 1.5f;

        [Header("Stock C")]
        [SerializeField] private float _initialPriceC = 700f;
        [SerializeField] private float _volatilityC = 0.8f;

        public float MinPrice => _minPrice;
        public float MaxPrice => _maxPrice;

        
public StockData[] Stocks { get; private set; }
        public List<StockEvent> Events { get; private set; } = new List<StockEvent>();
        public int CurrentTimeIndex { get; private set; } = 0;

        /// <summary>
        /// 當價格更新時觸發
        /// </summary>
        public event Action OnPriceUpdated;

        /// <summary>
        /// 當事件發生時觸發
        /// </summary>
        public event Action<StockEvent> OnEventTriggered;

        private float _tickTimer;
        private float _trendTimer;
        private bool _isRunning = false;

        private void Awake()
        {
            InitializeStocks();
        }

        private void InitializeStocks()
        {
            Stocks = new StockData[3];

            Stocks[0] = new StockData
            {
                Name = "A",
                LineColor = new Color(1f, 0.3f, 0.3f),
                CurrentTrend = TrendLevel.Flat,
                InitialPrice = _initialPriceA,
                CurrentPrice = _initialPriceA,
                Volatility = _volatilityA
            };

            Stocks[1] = new StockData
            {
                Name = "B",
                LineColor = new Color(0.3f, 0.5f, 1f),
                CurrentTrend = TrendLevel.Flat,
                InitialPrice = _initialPriceB,
                CurrentPrice = _initialPriceB,
                Volatility = _volatilityB
            };

            Stocks[2] = new StockData
            {
                Name = "C",
                LineColor = new Color(0.3f, 1f, 0.3f),
                CurrentTrend = TrendLevel.Flat,
                InitialPrice = _initialPriceC,
                CurrentPrice = _initialPriceC,
                Volatility = _volatilityC
            };

            // 加入初始數據點
            foreach (var stock in Stocks)
            {
                stock.PriceHistory.Add(stock.CurrentPrice);
            }
        }

        /// <summary>
        /// 開始運行股市
        /// </summary>
        public void StartMarket()
        {
            _isRunning = true;
            _tickTimer = 0f;
            _trendTimer = 0f;
        }

        /// <summary>
        /// 停止運行股市
        /// </summary>
        public void StopMarket()
        {
            _isRunning = false;
        }

        private void Update()
        {
            if (!_isRunning) return;

            _tickTimer += Time.deltaTime;
            _trendTimer += Time.deltaTime;

            // 價格 Tick
            if (_tickTimer >= _tickInterval)
            {
                _tickTimer -= _tickInterval;
                UpdatePrices();
            }

            // 趨勢自然變化 (只會是小幅變化)
            if (_trendTimer >= _trendChangeInterval)
            {
                _trendTimer -= _trendChangeInterval;
                NaturalTrendShift();
            }
        }

        private void UpdatePrices()
        {
            CurrentTimeIndex++;

            foreach (var stock in Stocks)
            {
                float change = stock.GetPriceChange();
                stock.CurrentPrice = Mathf.Clamp(
                    stock.CurrentPrice + change,
                    _minPrice,
                    _maxPrice
                );
                stock.PriceHistory.Add(stock.CurrentPrice);

                // 限制數據點數量（捲動視窗）
                if (stock.PriceHistory.Count > _maxDataPoints)
                {
                    stock.PriceHistory.RemoveAt(0);
                }
            }

            OnPriceUpdated?.Invoke();
        }

        /// <summary>
        /// 自然趨勢變化，只會在小漲/平盤/小跌之間切換
        /// </summary>
        private void NaturalTrendShift()
        {
            TrendLevel[] mildTrends = { TrendLevel.SmallRise, TrendLevel.Flat, TrendLevel.SmallDrop };

            foreach (var stock in Stocks)
            {
                stock.CurrentTrend = mildTrends[UnityEngine.Random.Range(0, mildTrends.Length)];
            }
        }

        /// <summary>
        /// 觸發事件，可以大幅改變趨勢
        /// </summary>
        /// <param name="eventName">事件名稱</param>
        /// <param name="stockIndex">影響的商品 (0=A, 1=B, 2=C)，-1=全部</param>
        /// <param name="newTrend">新的趨勢等級</param>
        public void TriggerEvent(string eventName, int stockIndex, TrendLevel newTrend)
        {
            var stockEvent = new StockEvent
            {
                TimeIndex = CurrentTimeIndex,
                EventName = eventName
            };
            Events.Add(stockEvent);

            if (stockIndex < 0)
            {
                // 影響全部商品
                foreach (var stock in Stocks)
                {
                    stock.CurrentTrend = newTrend;
                }
            }
            else if (stockIndex >= 0 && stockIndex < Stocks.Length)
            {
                Stocks[stockIndex].CurrentTrend = newTrend;
            }

            OnEventTriggered?.Invoke(stockEvent);
        }

        /// <summary>
        /// 重置股市
        /// </summary>
        public void ResetMarket()
        {
            _isRunning = false;
            CurrentTimeIndex = 0;
            Events.Clear();
            InitializeStocks();
        }
    }
}