using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

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

        [Header("結果動畫設定")]
        [SerializeField] private float resultAnimDuration = 0.5f;
        [SerializeField] private Ease resultAnimEase = Ease.OutCubic;

        [Header("倒數計時")]
        [SerializeField] private TextMeshPro timerText;

        [Header("觀眾事件圖片")]
        [SerializeField] private Image audienceEventImage;

        /// <summary>目前是否顯示中</summary>
        public bool IsShowing { get; private set; }

        /// <summary>結果動畫完成時觸發。</summary>
        public event Action OnResultAnimationComplete;

        // 快取已載入的觀眾事件圖片，避免重複從 Resources 讀取
        private System.Collections.Generic.Dictionary<string, Sprite> _audienceImageCache
            = new System.Collections.Generic.Dictionary<string, Sprite>();

        // 結果動畫內部狀態
        private Vector3[] _optionOriginalPositions;
        private Tween _activeResultTween;

        private void Awake()
        {
            // Cache original positions before any animation moves them
            _optionOriginalPositions = new Vector3[3];
            var texts = new TextMeshPro[] { option1Text, option2Text, option3Text };
            for (int i = 0; i < 3; i++)
            {
                if (texts[i] != null)
                    _optionOriginalPositions[i] = texts[i].transform.localPosition;
            }

            Hide();
        }

        /// <summary>
        /// 設定投票數字是否顯示（觀眾事件才顯示）。
        /// </summary>
        public void SetVoteCountVisible(bool visible)
        {
            if (vote1CountText != null) vote1CountText.gameObject.SetActive(visible);
            if (vote2CountText != null) vote2CountText.gameObject.SetActive(visible);
            if (vote3CountText != null) vote3CountText.gameObject.SetActive(visible);
        }

        /// <summary>
        /// 顯示三選一選項。
        /// </summary>
        public void Show(string title, string opt1, string opt2, string opt3)
        {
            if (tvScreenRoot != null)
                tvScreenRoot.gameObject.SetActive(true);

            HideAudienceImage();

            // Kill any in-progress tweens on option texts and title
            var texts = new TextMeshPro[] { option1Text, option2Text, option3Text };
            foreach (var t in texts)
            {
                if (t != null) DOTween.Kill(t.transform);
            }
            if (titleText != null) DOTween.Kill(titleText.transform);
            _activeResultTween = null;

            // Reactivate all option GameObjects
            foreach (var t in texts)
            {
                if (t != null) t.gameObject.SetActive(true);
            }

            // Restore cached positions
            for (int i = 0; i < 3; i++)
            {
                if (texts[i] != null)
                    texts[i].transform.localPosition = _optionOriginalPositions[i];
            }

            // Reactivate title
            if (titleText != null)
                titleText.gameObject.SetActive(true);

            // Set text content
            if (titleText != null) titleText.text = title;
            if (option1Text != null) option1Text.text = $"1. {opt1}";
            if (option2Text != null) option2Text.text = $"2. {opt2}";
            if (option3Text != null) option3Text.text = $"3. {opt3}";

            // Reset alpha to 1.0
            ResetOptionColors();

            // Reactivate vote count GameObjects
            if (vote1CountText != null) vote1CountText.gameObject.SetActive(true);
            if (vote2CountText != null) vote2CountText.gameObject.SetActive(true);
            if (vote3CountText != null) vote3CountText.gameObject.SetActive(true);

            // Reset vote counts
            if (vote1CountText != null) vote1CountText.text = "0";
            if (vote2CountText != null) vote2CountText.text = "0";
            if (vote3CountText != null) vote3CountText.text = "0";

            // Clear timer
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
        /// 顯示結果：隱藏落選選項和標題，將獲選選項平滑移動到目標位置。
        /// </summary>
        public void ShowResult(int winnerIndex, string resultText)
        {
            var optionTexts = new TextMeshPro[] { option1Text, option2Text, option3Text };
            var voteTexts = new TextMeshPro[] { vote1CountText, vote2CountText, vote3CountText };

            // Validate winnerIndex
            if (winnerIndex < 0 || winnerIndex > 2 || optionTexts[winnerIndex] == null)
            {
                Debug.LogWarning($"[TVChoiceUI] ShowResult: invalid winnerIndex={winnerIndex} or null option Transform");
                return;
            }

            // --- Hide losers ---
            for (int i = 0; i < 3; i++)
            {
                if (i == winnerIndex) continue;
                if (optionTexts[i] != null) optionTexts[i].gameObject.SetActive(false);
                if (voteTexts[i] != null) voteTexts[i].gameObject.SetActive(false);
            }

            // --- Hide title ---
            if (titleText != null)
                titleText.gameObject.SetActive(false);

            // --- Hide ALL vote counts (including winner's) ---
            foreach (var vt in voteTexts)
            {
                if (vt != null) vt.gameObject.SetActive(false);
            }

            // --- Animate winner to target Y ---
            Transform winnerTransform = optionTexts[winnerIndex].transform;

            // Kill previous tween if still running
            if (_activeResultTween != null && _activeResultTween.IsActive())
            {
                _activeResultTween.Kill();
            }

            float targetY = -0.67f;
            float duration = Mathf.Clamp(resultAnimDuration, 0.1f, 2.0f);

            _activeResultTween = winnerTransform
                .DOLocalMoveY(targetY, duration)
                .SetEase(resultAnimEase)
                .OnComplete(() =>
                {
                    _activeResultTween = null;
                    OnResultAnimationComplete?.Invoke();
                });
        }

        /// <summary>
        /// 顯示觀眾事件對應的圖片。從 Resources/AudienPic/{imageId} 載入。
        /// </summary>
        public void ShowAudienceImage(string imageId)
        {
            if (audienceEventImage == null) return;

            if (string.IsNullOrEmpty(imageId))
            {
                audienceEventImage.gameObject.SetActive(false);
                return;
            }

            Sprite sprite;
            if (!_audienceImageCache.TryGetValue(imageId, out sprite))
            {
                sprite = Resources.Load<Sprite>($"AudienPic/{imageId}");
                if (sprite == null)
                {
                    Debug.LogWarning($"[TVChoiceUI] 找不到觀眾事件圖片: AudienPic/{imageId}");
                    audienceEventImage.gameObject.SetActive(false);
                    return;
                }
                _audienceImageCache[imageId] = sprite;
            }

            audienceEventImage.sprite = sprite;
            audienceEventImage.gameObject.SetActive(true);
        }

        /// <summary>
        /// 隱藏觀眾事件圖片。
        /// </summary>
        public void HideAudienceImage()
        {
            if (audienceEventImage != null)
                audienceEventImage.gameObject.SetActive(false);
        }

        /// <summary>
        /// 隱藏電視選項面板。
        /// </summary>
        public void Hide()
        {
            if (tvScreenRoot != null)
                tvScreenRoot.gameObject.SetActive(false);

            HideAudienceImage();

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
