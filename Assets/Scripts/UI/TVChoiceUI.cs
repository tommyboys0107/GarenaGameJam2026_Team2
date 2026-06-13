using System;
using UnityEngine;
using TMPro;

namespace UI
{
    /// <summary>
    /// 右下角電視上的三選一選項 UI。
    /// 使用 RectTransform rotation 來模擬透視感（電視螢幕角度）。
    /// </summary>
    public class TVChoiceUI : MonoBehaviour
    {
        [Header("電視容器（套用透視旋轉的父物件）")]
        [SerializeField] private RectTransform tvPanel;

        [Header("透視旋轉角度")]
        [SerializeField] private Vector3 perspectiveRotation = new Vector3(2f, -15f, 1f);

        [Header("選項文字")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI option1Text;
        [SerializeField] private TextMeshProUGUI option2Text;
        [SerializeField] private TextMeshProUGUI option3Text;

        [Header("投票數顯示")]
        [SerializeField] private TextMeshProUGUI vote1CountText;
        [SerializeField] private TextMeshProUGUI vote2CountText;
        [SerializeField] private TextMeshProUGUI vote3CountText;

        [Header("倒數計時")]
        [SerializeField] private TextMeshProUGUI timerText;

        /// <summary>目前是否顯示中</summary>
        public bool IsShowing { get; private set; }

        private void Awake()
        {
            // 套用透視旋轉
            if (tvPanel != null)
                tvPanel.localRotation = Quaternion.Euler(perspectiveRotation);

            Hide();
        }

        /// <summary>
        /// 顯示三選一選項。
        /// </summary>
        /// <param name="title">事件標題</param>
        /// <param name="opt1">選項 1 文字</param>
        /// <param name="opt2">選項 2 文字</param>
        /// <param name="opt3">選項 3 文字</param>
        public void Show(string title, string opt1, string opt2, string opt3)
        {
            if (tvPanel != null)
                tvPanel.gameObject.SetActive(true);

            if (titleText != null)
                titleText.text = title;

            if (option1Text != null) option1Text.text = $"1. {opt1}";
            if (option2Text != null) option2Text.text = $"2. {opt2}";
            if (option3Text != null) option3Text.text = $"3. {opt3}";

            // 清空票數
            if (vote1CountText != null) vote1CountText.text = "0";
            if (vote2CountText != null) vote2CountText.text = "0";
            if (vote3CountText != null) vote3CountText.text = "0";

            IsShowing = true;
        }

        /// <summary>
        /// 更新票數顯示。
        /// </summary>
        public void UpdateVotes(int[] votes)
        {
            if (votes == null || votes.Length < 3) return;
            if (vote1CountText != null) vote1CountText.text = votes[0].ToString();
            if (vote2CountText != null) vote2CountText.text = votes[1].ToString();
            if (vote3CountText != null) vote3CountText.text = votes[2].ToString();
        }

        /// <summary>
        /// 更新倒數計時。
        /// </summary>
        public void UpdateTimer(float seconds)
        {
            if (timerText != null)
                timerText.text = $"{Mathf.CeilToInt(seconds)}";
        }

        /// <summary>
        /// 顯示結果。
        /// </summary>
        public void ShowResult(int winnerIndex, string resultText)
        {
            if (titleText != null)
                titleText.text = resultText;

            // 高亮獲勝選項
            var texts = new[] { option1Text, option2Text, option3Text };
            for (int i = 0; i < 3; i++)
            {
                if (texts[i] != null)
                    texts[i].alpha = (i == winnerIndex) ? 1f : 0.3f;
            }
        }

        /// <summary>
        /// 隱藏電視選項面板。
        /// </summary>
        public void Hide()
        {
            if (tvPanel != null)
                tvPanel.gameObject.SetActive(false);

            IsShowing = false;
        }
    }
}
