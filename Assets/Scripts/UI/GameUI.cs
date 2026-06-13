using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Gameplay;
using Core;

namespace UI
{
    /// <summary>
    /// 遊戲場景的 UI 控制器。
    /// 顯示倒數計時，遊戲結束後顯示結算面板。
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        [Header("參考")]
        [SerializeField] private GameManager gameManager;

        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI characterInfoText;

        [Header("結算面板")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private Button okButton;

        private void Start()
        {
            // 隱藏結算面板
            if (resultPanel != null)
                resultPanel.SetActive(false);

            // 顯示角色資訊
            if (characterInfoText != null)
                characterInfoText.text = $"角色 {GameData.SelectedCharacterIndex + 1} 號";

            // 監聽遊戲結束
            if (gameManager != null)
                gameManager.OnGameOver += ShowResult;

            // OK 按鈕回到 Title
            if (okButton != null)
                okButton.onClick.AddListener(OnOkClicked);
        }

        private void OnDestroy()
        {
            if (gameManager != null)
                gameManager.OnGameOver -= ShowResult;
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

        private void ShowResult()
        {
            if (timerText != null)
                timerText.text = "0";

            if (resultPanel != null)
                resultPanel.SetActive(true);

            if (resultText != null)
                resultText.text = "時間到！\n結算中...";
        }

        private void OnOkClicked()
        {
            GameData.Reset();
            SceneLoader.LoadTitle();
        }
    }
}
