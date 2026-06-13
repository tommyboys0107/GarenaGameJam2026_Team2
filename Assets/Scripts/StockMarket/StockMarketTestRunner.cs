using UnityEngine;
using UnityEngine.InputSystem;

namespace BlackMarketTrader
{
    /// <summary>
    /// 測試用腳本，按 Play 後自動啟動股市，並可用鍵盤觸發事件
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

            // 按 1: NARC +30
            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                _marketManager.ApplyStockEffect("NARC", 30);
                _marketManager.TriggerEvent("政策利多", -1, TrendLevel.Flat);
                Debug.Log("[StockMarket] NARC +30");
            }

            // 按 2: LOCK -20
            if (keyboard.digit2Key.wasPressedThisFrame)
            {
                _marketManager.ApplyStockEffect("LOCK", -20);
                _marketManager.TriggerEvent("市場崩盤", -1, TrendLevel.Flat);
                Debug.Log("[StockMarket] LOCK -20");
            }

            // 按 3: 全部 +20
            if (keyboard.digit3Key.wasPressedThisFrame)
            {
                _marketManager.ApplyStockEffect("NARC", 20);
                _marketManager.ApplyStockEffect("LOCK", 20);
                _marketManager.ApplyStockEffect("BYTE", 20);
                _marketManager.TriggerEvent("全面爆發", -1, TrendLevel.Flat);
                Debug.Log("[StockMarket] All +20");
            }

            // 按 4: 全部 -30
            if (keyboard.digit4Key.wasPressedThisFrame)
            {
                _marketManager.ApplyStockEffect("NARC", -30);
                _marketManager.ApplyStockEffect("LOCK", -30);
                _marketManager.ApplyStockEffect("BYTE", -30);
                _marketManager.TriggerEvent("黑天魚事件", -1, TrendLevel.Flat);
                Debug.Log("[StockMarket] All -30");
            }

            // 按 R: 重置
            if (keyboard.rKey.wasPressedThisFrame)
            {
                _marketManager.ResetMarket();
                _marketManager.StartMarket();
                Debug.Log("[StockMarket] Market reset!");
            }
        }
    }
}