using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace UI
{
    /// <summary>
    /// 操盤手事件的三選一按鈕 UI。
    /// 放在 GameCanvas 底下，畫面中下方水平排列三個按鈕。
    /// 支援滑鼠點擊和鍵盤 1/2/3 選擇。
    /// </summary>
    public class TraderChoiceUI : MonoBehaviour
    {
        [Header("面板")]
        [SerializeField] private GameObject choicePanel;

        [Header("按鈕")]
        [SerializeField] private Button buttonA;
        [SerializeField] private Button buttonB;
        [SerializeField] private Button buttonC;

        [Header("按鈕文字")]
        [SerializeField] private TextMeshProUGUI textA;
        [SerializeField] private TextMeshProUGUI textB;
        [SerializeField] private TextMeshProUGUI textC;

        /// <summary>玩家選擇了某個選項 (0, 1, 2)</summary>
        public event Action<int> OnChoiceSelected;

        /// <summary>目前是否顯示中</summary>
        public bool IsShowing { get; private set; }

        private void Awake()
        {
            if (buttonA != null) buttonA.onClick.AddListener(() => Select(0));
            if (buttonB != null) buttonB.onClick.AddListener(() => Select(1));
            if (buttonC != null) buttonC.onClick.AddListener(() => Select(2));

            Hide();
        }

        private void Update()
        {
            if (!IsShowing) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
                Select(0);
            else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
                Select(1);
            else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame)
                Select(2);
        }

        /// <summary>
        /// 顯示三個選項按鈕。
        /// </summary>
        public void Show(string optA, string optB, string optC)
        {
            if (textA != null) textA.text = optA;
            if (textB != null) textB.text = optB;
            if (textC != null) textC.text = optC;

            if (choicePanel != null)
                choicePanel.SetActive(true);

            IsShowing = true;
        }

        /// <summary>
        /// 隱藏面板。
        /// </summary>
        public void Hide()
        {
            if (choicePanel != null)
                choicePanel.SetActive(false);

            IsShowing = false;
        }

        private void Select(int index)
        {
            if (!IsShowing) return;
            IsShowing = false;
            OnChoiceSelected?.Invoke(index);
        }
    }
}
