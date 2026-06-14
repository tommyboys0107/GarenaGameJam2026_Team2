using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Core;

namespace UI
{
    /// <summary>
    /// Title 場景的 UI 控制器。
    /// 管理 Start、Credit、Exit 三個按鈕。
    /// 按鈕 hover 顯示背景圖，點擊播放音效。
    /// </summary>
    public class TitleUI : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button creditButton;
        [SerializeField] private Button exitButton;

        [Header("Credit 面板（選填）")]
        [SerializeField] private GameObject creditPanel;

        [Header("音效")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip buttonClickSound;

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

            // 為每個按鈕加上 hover 效果
            SetupButtonEffect(startButton);
            SetupButtonEffect(creditButton);
            SetupButtonEffect(exitButton);
        }

        private void SetupButtonEffect(Button button)
        {
            if (button == null) return;

            var effect = button.gameObject.GetComponent<TitleButtonEffect>();
            if (effect == null)
                effect = button.gameObject.AddComponent<TitleButtonEffect>();

            effect.Init(button.GetComponent<Image>());
        }

        /// <summary>
        /// 播放按鈕點擊音效。
        /// </summary>
        private void PlayClickSound()
        {
            if (sfxSource != null && buttonClickSound != null)
                sfxSource.PlayOneShot(buttonClickSound);
        }

        private void OnStartClicked()
        {
            PlayClickSound();
            // 延遲切場景，讓音效有時間播出來
            StartCoroutine(DelayedLoadScene());
        }

        private IEnumerator DelayedLoadScene()
        {
            // 禁止重複點擊
            if (startButton != null)
                startButton.interactable = false;

            yield return new WaitForSeconds(0.15f);
            SceneLoader.LoadCharacterSelect();
        }

        private void OnCreditClicked()
        {
            PlayClickSound();
            if (creditPanel != null)
                creditPanel.SetActive(!creditPanel.activeSelf);
        }

        private void OnExitClicked()
        {
            PlayClickSound();
            SceneLoader.QuitGame();
        }
    }
}
