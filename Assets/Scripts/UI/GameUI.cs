using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Gameplay;
using Core;

namespace UI
{
    /// <summary>
    /// 遊戲場景的 HUD UI 控制器。
    /// 顯示倒數計時與角色資訊。
    /// 結算面板已移至 ResultPanelUI (UI Toolkit)。
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        [Header("參考")]
        [SerializeField] private GameManager gameManager;

        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI characterInfoText;

        [Header("事件描述")]
        [SerializeField] private GameObject eventDescriptionObject;

        private void Start()
        {
            // 顯示角色資訊
            if (characterInfoText != null)
                characterInfoText.text = $"角色 {GameData.SelectedCharacterIndex + 1} 號";

            // 初始隱藏事件描述 GameObject
            if (eventDescriptionObject != null)
                eventDescriptionObject.SetActive(false);

            // 監聽遊戲結束，停止計時顯示
            if (gameManager != null)
                gameManager.OnGameOver += OnGameOver;
        }

        private void OnDestroy()
        {
            if (gameManager != null)
                gameManager.OnGameOver -= OnGameOver;
        }

        private void Update()
        {
            if (gameManager == null) return;

            // 更新倒數計時顯示
            if (timerText != null && gameManager.IsPlaying)
            {
                int seconds = Mathf.CeilToInt(gameManager.TimeRemaining);
                timerText.text = $"{seconds}";
            }
        }

        private void OnGameOver()
        {
            if (timerText != null)
                timerText.text = "0";

            // 隱藏事件描述
            if (eventDescriptionObject != null)
                eventDescriptionObject.SetActive(false);
        }
    }
}
