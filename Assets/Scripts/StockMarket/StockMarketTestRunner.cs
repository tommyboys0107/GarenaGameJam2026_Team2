using UnityEngine;
using UnityEngine.InputSystem;

namespace BlackMarketTrader
{
    /// <summary>
    /// 測試用腳本，按 Play 後自動啟動股市，並可用鍵盤觸發事件
    /// 使用 New Input System
    /// </summary>
    public class StockMarketTestRunner : MonoBehaviour
    {
        [SerializeField] private StockMarketManager _marketManager;

        private void Start()
        {
            if (_marketManager != null)
            {
                _marketManager.StartMarket();
                Debug.Log("[StockMarket] Market started!");
            }
        }

        private void Update()
        {
            if (_marketManager == null) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // 按 1: 觸發事件，商品A 大漲
            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                _marketManager.TriggerEvent("政策利多", 0, TrendLevel.BigRise);
                Debug.Log("[StockMarket] Event: 政策利多 -> A 大漲");
            }

            // 按 2: 觸發事件，商品B 大跌
            if (keyboard.digit2Key.wasPressedThisFrame)
            {
                _marketManager.TriggerEvent("市場崩盤", 1, TrendLevel.BigDrop);
                Debug.Log("[StockMarket] Event: 市場崩盤 -> B 大跌");
            }

            // 按 3: 觸發事件，全部商品大漲
            if (keyboard.digit3Key.wasPressedThisFrame)
            {
                _marketManager.TriggerEvent("全面爆發", -1, TrendLevel.BigRise);
                Debug.Log("[StockMarket] Event: 全面爆發 -> All 大漲");
            }

            // 按 4: 觸發事件，全部商品大跌
            if (keyboard.digit4Key.wasPressedThisFrame)
            {
                _marketManager.TriggerEvent("黑天魚事件", -1, TrendLevel.BigDrop);
                Debug.Log("[StockMarket] Event: 黑天魚事件 -> All 大跌");
            }

            // 按 R: 重置股市
            if (keyboard.rKey.wasPressedThisFrame)
            {
                _marketManager.ResetMarket();
                _marketManager.StartMarket();
                Debug.Log("[StockMarket] Market reset and restarted!");
            }
        }
    }
}