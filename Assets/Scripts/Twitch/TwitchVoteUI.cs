using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Twitch
{
    /// <summary>
    /// 投票 UI 控制器。
    /// 顯示三個選項、即時票數、倒數計時。
    /// 需要搭配 Canvas 上的 UI 元件使用。
    /// </summary>
    public class TwitchVoteUI : MonoBehaviour
    {
        [Header("參考")]
        [SerializeField] private TwitchVoteManager voteManager;

        [Header("UI 元件")]
        [SerializeField] private GameObject votePanel;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("選項 1")]
        [SerializeField] private TextMeshProUGUI option1Text;
        [SerializeField] private TextMeshProUGUI option1CountText;
        [SerializeField] private Image option1FillBar;

        [Header("選項 2")]
        [SerializeField] private TextMeshProUGUI option2Text;
        [SerializeField] private TextMeshProUGUI option2CountText;
        [SerializeField] private Image option2FillBar;

        [Header("選項 3")]
        [SerializeField] private TextMeshProUGUI option3Text;
        [SerializeField] private TextMeshProUGUI option3CountText;
        [SerializeField] private Image option3FillBar;

        [Header("設定")]
        [SerializeField] private string voteTitle = "觀眾投票！輸入 1 / 2 / 3";

        private TextMeshProUGUI[] _optionTexts;
        private TextMeshProUGUI[] _countTexts;
        private Image[] _fillBars;

        private void Awake()
        {
            _optionTexts = new[] { option1Text, option2Text, option3Text };
            _countTexts = new[] { option1CountText, option2CountText, option3CountText };
            _fillBars = new[] { option1FillBar, option2FillBar, option3FillBar };

            if (votePanel != null)
                votePanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (voteManager != null)
            {
                voteManager.OnVoteStarted += HandleVoteStarted;
                voteManager.OnVoteUpdated += HandleVoteUpdated;
                voteManager.OnVoteComplete += HandleVoteComplete;
            }
        }

        private void OnDisable()
        {
            if (voteManager != null)
            {
                voteManager.OnVoteStarted -= HandleVoteStarted;
                voteManager.OnVoteUpdated -= HandleVoteUpdated;
                voteManager.OnVoteComplete -= HandleVoteComplete;
            }
        }

        private void Update()
        {
            if (voteManager == null || !voteManager.IsVoting) return;

            // 更新倒數計時
            if (timerText != null)
            {
                timerText.text = $"{Mathf.CeilToInt(voteManager.TimeRemaining)}s";
            }
        }

        private void HandleVoteStarted(string[] options)
        {
            if (votePanel != null)
                votePanel.SetActive(true);

            if (titleText != null)
                titleText.text = voteTitle;

            for (int i = 0; i < 3; i++)
            {
                if (_optionTexts[i] != null)
                    _optionTexts[i].text = $"{i + 1}. {options[i]}";

                if (_countTexts[i] != null)
                    _countTexts[i].text = "0";

                if (_fillBars[i] != null)
                    _fillBars[i].fillAmount = 0f;
            }
        }

        private void HandleVoteUpdated(int[] votes)
        {
            int total = votes[0] + votes[1] + votes[2];

            for (int i = 0; i < 3; i++)
            {
                if (_countTexts[i] != null)
                    _countTexts[i].text = votes[i].ToString();

                if (_fillBars[i] != null)
                    _fillBars[i].fillAmount = total > 0 ? (float)votes[i] / total : 0f;
            }
        }

        private void HandleVoteComplete(int winnerIndex)
        {
            // 顯示結果，延遲後隱藏面板
            if (titleText != null)
            {
                string winnerName = voteManager.CurrentOptions[winnerIndex];
                titleText.text = $"結果：{winnerName}！";
            }

            if (timerText != null)
                timerText.text = "結束";

            // 高亮獲勝選項
            for (int i = 0; i < 3; i++)
            {
                if (_fillBars[i] != null)
                {
                    var color = _fillBars[i].color;
                    color.a = (i == winnerIndex) ? 1f : 0.3f;
                    _fillBars[i].color = color;
                }
            }

            // 3 秒後隱藏
            Invoke(nameof(HidePanel), 3f);
        }

        private void HidePanel()
        {
            if (votePanel != null)
                votePanel.SetActive(false);
        }
    }
}
