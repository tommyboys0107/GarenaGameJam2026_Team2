using UnityEngine;
using UnityEngine.UI;
using Core;

namespace UI
{
    /// <summary>
    /// Title 場景的 UI 控制器。
    /// 管理 Start、Credit、Exit 三個按鈕。
    /// </summary>
    public class TitleUI : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button creditButton;
        [SerializeField] private Button exitButton;

        [Header("Credit 面板（選填）")]
        [SerializeField] private GameObject creditPanel;

        private void Start()
        {
            if (startButton != null)
                startButton.onClick.AddListener(OnStartClicked);

            if (creditButton != null)
                creditButton.onClick.AddListener(OnCreditClicked);

            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitClicked);

            if (creditPanel != null)
                creditPanel.SetActive(false);
        }

        private void OnStartClicked()
        {
            SceneLoader.LoadCharacterSelect();
        }

        private void OnCreditClicked()
        {
            if (creditPanel != null)
                creditPanel.SetActive(!creditPanel.activeSelf);
        }

        private void OnExitClicked()
        {
            SceneLoader.QuitGame();
        }
    }
}
