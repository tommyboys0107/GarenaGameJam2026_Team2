using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Core;
using Twitch;

namespace UI
{
    /// <summary>
    /// Title 場景的 UI 控制器。
    /// 管理 Start、Credit、Exit 三個按鈕。
    /// 包含 Twitch 頻道 ID 輸入與未輸入時的警告跳窗。
    /// </summary>
    public class TitleUI : MonoBehaviour
    {
        [Header("主要按鈕")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button creditButton;
        [SerializeField] private Button exitButton;

        [Header("Credit 面板（選填）")]
        [SerializeField] private GameObject creditPanel;

        [Header("Twitch 設定")]
        [SerializeField] private TwitchConfig twitchConfig;
        [SerializeField] private TMP_InputField twitchIdInput;

        [Header("警告跳窗（無 Twitch ID 時）")]
        [SerializeField] private GameObject warningPanel;
        [SerializeField] private Button warningBackButton;
        [SerializeField] private Button warningStartButton;

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

            // 警告面板預設隱藏
            if (warningPanel != null)
                warningPanel.SetActive(false);

            // 警告面板按鈕
            if (warningBackButton != null)
                warningBackButton.onClick.AddListener(OnWarningBack);

            if (warningStartButton != null)
                warningStartButton.onClick.AddListener(OnWarningStart);

            // Twitch ID 輸入框：讀取目前值（如果從 Game 場景回來還有值的話）
            if (twitchIdInput != null)
            {
                twitchIdInput.text = TwitchConfig.channelName ?? "";
                twitchIdInput.onEndEdit.AddListener(OnTwitchIdChanged);
            }

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
        /// 當玩家修改 Twitch ID 輸入框時，即時寫入 config。
        /// </summary>
        private void OnTwitchIdChanged(string newValue)
        {
            TwitchConfig.channelName = newValue.Trim().ToLower();
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

            // 先把 InputField 的值存到 config（避免玩家沒按 Enter）
            if (twitchIdInput != null)
                TwitchConfig.channelName = twitchIdInput.text.Trim().ToLower();

            // 檢查是否有 Twitch ID
            if (string.IsNullOrWhiteSpace(TwitchConfig.channelName))
            {
                // 沒有 ID → 顯示警告
                if (warningPanel != null)
                    warningPanel.SetActive(true);
                return;
            }

            // 有 ID → 正常開始
            StartCoroutine(DelayedLoadScene());
        }

        /// <summary>
        /// 警告跳窗：返回（關閉跳窗）
        /// </summary>
        private void OnWarningBack()
        {
            PlayClickSound();
            if (warningPanel != null)
                warningPanel.SetActive(false);
        }

        /// <summary>
        /// 警告跳窗：直接開始（無 Twitch 連線的情況下繼續）
        /// </summary>
        private void OnWarningStart()
        {
            PlayClickSound();
            if (warningPanel != null)
                warningPanel.SetActive(false);

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
