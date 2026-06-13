using System;
using UnityEngine;
using TMPro;

namespace UI
{
    /// <summary>
    /// 右下角電視上的三選一選項 UI。
    /// 使用 3D TextMeshPro 物件放在世界空間中，對齊電視螢幕位置與角度。
    /// 透視感由 Camera 自然產生。
    /// </summary>
    public class TVChoiceUI : MonoBehaviour
    {
        [Header("電視螢幕容器（控制整體位置和旋轉）")]
        [SerializeField] private Transform tvScreenRoot;

        [Header("選項文字 (3D TextMeshPro)")]
        [SerializeField] private TextMeshPro titleText;
        [SerializeField] private TextMeshPro option1Text;
        [SerializeField] private TextMeshPro option2Text;
        [SerializeField] private TextMeshPro option3Text;

        [Header("投票數顯示")]
        [SerializeField] private TextMeshPro vote1CountText;
        [SerializeField] private TextMeshPro vote2CountText;
        [SerializeField] private TextMeshPro vote3CountText;

        [Header("倒數計時")]
        [SerializeField] private TextMeshPro timerText;

        /// <summary>目前是否顯示中</summary>
        public bool IsShowing { get; private set; }

        private void Awake()
        {
            Hide();
        }

        /// <summary>
        /// 顯示三選一選項。
        /// </summary>
        public void Show(string title, string opt1, string opt2, string opt3)
        {
            if (tvScreenRoot != null)
                tvScreenRoot.gameObject.SetActive(true);

            if (titleText != null)
                titleText.text = title;

            if (option1Text != null) option1Text.text = $"1. {opt1}";
            if (option2Text != null) option2Text.text = $"2. {opt2}";
            if (option3Text != null) option3Text.text = $"3. {opt3}";

            // 重置選項文字透明度（ShowResult 會改 alpha）
            ResetOptionColors();

            // 清空票數
            if (vote1CountText != null) vote1CountText.text = "0";
            if (vote2CountText != null) vote2CountText.text = "0";
            if (vote3CountText != null) vote3CountText.text = "0";

            // 清空倒數
            if (timerText != null) timerText.text = "";

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
        /// 隱藏倒數計時。
        /// </summary>
        public void HideTimer()
        {
            if (timerText != null)
                timerText.text = "";
        }

        /// <summary>
        /// 顯示結果。
        /// </summary>
        public void ShowResult(int winnerIndex, string resultText)
        {
            var texts = new[] { option1Text, option2Text, option3Text };
            for (int i = 0; i < 3; i++)
            {
                if (texts[i] != null)
                {
                    var color = texts[i].color;
                    color.a = (i == winnerIndex) ? 1f : 0.3f;
                    texts[i].color = color;
                }
            }
        }

        /// <summary>
        /// 隱藏電視選項面板。
        /// </summary>
        public void Hide()
        {
            if (tvScreenRoot != null)
                tvScreenRoot.gameObject.SetActive(false);

            IsShowing = false;
        }

        /// <summary>
        /// 重置選項文字顏色（alpha 回到 1）。
        /// </summary>
        private void ResetOptionColors()
        {
            var texts = new[] { option1Text, option2Text, option3Text };
            foreach (var t in texts)
            {
                if (t != null)
                {
                    var color = t.color;
                    color.a = 1f;
                    t.color = color;
                }
            }
        }
    }
}
