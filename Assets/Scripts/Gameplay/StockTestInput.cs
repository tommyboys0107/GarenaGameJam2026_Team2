using UnityEngine;
using UnityEngine.InputSystem;
using BlackMarketTrader;

namespace Gameplay
{
    /// <summary>
    /// 股票測試工具。
    /// F1~F6 分別對三檔股票 +/-10%。
    /// F1=NARC+10%, F2=LOCK+10%, F3=BYTE+10%
    /// F4=NARC-10%, F5=LOCK-10%, F6=BYTE-10%
    /// Inspector 可開關。
    /// 
    /// 注意：數字鍵 4~7 已被 StockMarketTestRunner 使用。
    /// </summary>
    public class StockTestInput : MonoBehaviour
    {
        [Header("參考")]
        [SerializeField] private StockMarketManager stockMarketManager;

        [Header("開關")]
        [SerializeField] private bool enableTestKeys = true;

        private void Update()
        {
            if (!enableTestKeys) return;
            if (stockMarketManager == null) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            // F1 = NARC +10%
            if (kb.f1Key.wasPressedThisFrame)
            {
                stockMarketManager.ApplyStockEffect("NARC", 10);
                Debug.Log("[StockTest] NARC +10%");
            }

            // F2 = LOCK +10%
            if (kb.f2Key.wasPressedThisFrame)
            {
                stockMarketManager.ApplyStockEffect("LOCK", 10);
                Debug.Log("[StockTest] LOCK +10%");
            }

            // F3 = BYTE +10%
            if (kb.f3Key.wasPressedThisFrame)
            {
                stockMarketManager.ApplyStockEffect("BYTE", 10);
                Debug.Log("[StockTest] BYTE +10%");
            }

            // F4 = NARC -10%
            if (kb.f4Key.wasPressedThisFrame)
            {
                stockMarketManager.ApplyStockEffect("NARC", -10);
                Debug.Log("[StockTest] NARC -10%");
            }

            // F5 = LOCK -10%
            if (kb.f5Key.wasPressedThisFrame)
            {
                stockMarketManager.ApplyStockEffect("LOCK", -10);
                Debug.Log("[StockTest] LOCK -10%");
            }

            // F6 = BYTE -10%
            if (kb.f6Key.wasPressedThisFrame)
            {
                stockMarketManager.ApplyStockEffect("BYTE", -10);
                Debug.Log("[StockTest] BYTE -10%");
            }
        }
    }
}
