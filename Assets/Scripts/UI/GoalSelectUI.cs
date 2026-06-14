using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using DG.Tweening;
using Core;
using Data;
using System.Collections.Generic;

namespace UI
{
    /// <summary>
    /// uGUI 版目標選擇面板。
    /// 疊在角色選擇頁面上方，半透明黑色背景 + 2×3 目標卡片網格。
    /// 由 CharacterSelectController 在選完角色後啟動。
    /// </summary>
    public class GoalSelectUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private GameObject panel;

        [Header("Goal Card Template")]
        [SerializeField] private GameObject goalCardPrefab;
        [SerializeField] private Transform gridContainer;

        [Header("Character Portrait (右側角色大圖)")]
        [SerializeField] private Image characterPortrait;

        [Header("Bottom Text")]
        [SerializeField] private TextMeshProUGUI bottomText;

        [Header("Back Button")]
        [SerializeField] private Button backButton;

        [Header("Audio")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip clickSfxClip;

        // === Data ===
        private List<GoalInfo> _goals;
        private List<StockInfo> _stocks;
        private bool _isActive;
        private System.Action _onBack;
        private System.Action<int> _onGoalSelected;
        private TMPro.TMP_FontAsset _font;

        private static readonly KeyCode[] GoalKeys = {
            KeyCode.Q, KeyCode.W, KeyCode.E,
            KeyCode.A, KeyCode.S, KeyCode.D
        };

        // ====================================================
        // Public API (由 CharacterSelectController 呼叫)
        // ====================================================

        /// <summary>
        /// 顯示目標選擇面板。
        /// </summary>
        public void Show(int selectedCharIndex, System.Action onBack, System.Action<int> onGoalSelected)
        {
            _onBack = onBack;
            _onGoalSelected = onGoalSelected;
            _isActive = true;

            // 載入資料
            if (_goals == null) _goals = CSVLoader.LoadGoals();
            if (_stocks == null) _stocks = CSVLoader.LoadStocks();

            // 載入 TMP 字型
            if (_font == null)
                _font = Resources.Load<TMPro.TMP_FontAsset>("Fonts/ChironHeiHK-Regular SDF");

            // 右側角色 portrait 不額外載入 — 前一頁的 portrait 仍然可見
            // 隱藏 GoalSelectUI 自己的 portrait 元素
            if (characterPortrait != null)
                characterPortrait.gameObject.SetActive(false);

            // 生成目標卡片
            PopulateGoalCards();

            // 顯示面板
            panel.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
                canvasGroup.DOFade(1f, 0.3f);
            }
        }

        /// <summary>
        /// 隱藏目標選擇面板。
        /// </summary>
        public void Hide()
        {
            _isActive = false;
            if (canvasGroup != null)
            {
                canvasGroup.DOFade(0f, 0.2f).OnComplete(() => panel.SetActive(false));
            }
            else
            {
                panel.SetActive(false);
            }
        }

        // ====================================================
        // Lifecycle
        // ====================================================

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
        }

        private void Update()
        {
            if (!_isActive) return;

            // Escape 返回
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                OnBackClicked();
                return;
            }

            // Q/W/E/A/S/D 選取目標
            if (Keyboard.current == null) return;
            if (Keyboard.current.qKey.wasPressedThisFrame) SelectGoal(0);
            else if (Keyboard.current.wKey.wasPressedThisFrame) SelectGoal(1);
            else if (Keyboard.current.eKey.wasPressedThisFrame) SelectGoal(2);
            else if (Keyboard.current.aKey.wasPressedThisFrame) SelectGoal(3);
            else if (Keyboard.current.sKey.wasPressedThisFrame) SelectGoal(4);
            else if (Keyboard.current.dKey.wasPressedThisFrame) SelectGoal(5);
        }

        // ====================================================
        // Goal Cards
        // ====================================================

        private void PopulateGoalCards()
        {
            // 清除舊卡片
            if (gridContainer != null)
            {
                for (int i = gridContainer.childCount - 1; i >= 0; i--)
                    Destroy(gridContainer.GetChild(i).gameObject);
            }

            if (_goals == null || _goals.Count == 0) return;

            string[] keyHints = { "Q", "W", "E", "A", "S", "D" };
            int count = Mathf.Min(_goals.Count, 6);

            for (int i = 0; i < count; i++)
            {
                var goal = _goals[i];
                GameObject card;

                if (goalCardPrefab != null)
                {
                    card = Instantiate(goalCardPrefab, gridContainer);
                }
                else
                {
                    // Fallback: 動態建立簡單卡片
                    card = CreateGoalCard(goal, keyHints[i], i);
                    continue;
                }

                // 填入資料（如果使用 prefab）
                SetupGoalCard(card, goal, keyHints[i], i);
            }
        }

        private GameObject CreateGoalCard(GoalInfo goal, string keyHint, int index)
        {
            // 動態建立卡片（無 prefab 時的 fallback）
            var cardGO = new GameObject($"GoalCard_{index}", typeof(RectTransform), typeof(Image));
            cardGO.transform.SetParent(gridContainer, false);

            var img = cardGO.GetComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

            var layout = cardGO.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 5;
            layout.padding = new RectOffset(15, 15, 10, 10);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Key hint
            CreateText(cardGO.transform, keyHint, 48, Color.white, FontStyles.Bold);

            // Nickname
            CreateText(cardGO.transform, goal.nickname, 28, new Color(0.9f, 0.85f, 0.7f), FontStyles.Bold);

            // Description
            var descText = CreateText(cardGO.transform, goal.nicknameDesc, 18, new Color(0.8f, 0.8f, 0.8f), FontStyles.Normal);
            if (descText != null)
            {
                var layoutElem = descText.gameObject.AddComponent<LayoutElement>();
                layoutElem.preferredHeight = 80;
            }

            // 任務目標 — 股票名稱 + 漲幅
            string stockName = GetStockName(goal.stockCode);
            string targetStr = goal.targetPercent >= 0
                ? $"{stockName} +{goal.targetPercent}%"
                : $"{stockName} {goal.targetPercent}%";
            Color targetColor = goal.targetPercent >= 0
                ? new Color(0.3f, 1f, 0.3f)
                : new Color(1f, 0.4f, 0.4f);
            CreateText(cardGO.transform, $"任務目標  {targetStr}", 22, targetColor, FontStyles.Bold);

            return cardGO;
        }

        private void SetupGoalCard(GameObject card, GoalInfo goal, string keyHint, int index)
        {
            // 如果 prefab 有命名規則的子元件，填入資料
            var keyText = card.transform.Find("KeyHint")?.GetComponent<TextMeshProUGUI>();
            var nickText = card.transform.Find("Nickname")?.GetComponent<TextMeshProUGUI>();
            var descText = card.transform.Find("Description")?.GetComponent<TextMeshProUGUI>();
            var targetText = card.transform.Find("Target")?.GetComponent<TextMeshProUGUI>();

            if (keyText != null) keyText.text = keyHint;
            if (nickText != null) nickText.text = goal.nickname;
            if (descText != null) descText.text = goal.nicknameDesc;

            if (targetText != null)
            {
                string stockName = GetStockName(goal.stockCode);
                string targetStr = goal.targetPercent >= 0
                    ? $"{stockName} +{goal.targetPercent}%"
                    : $"{stockName} {goal.targetPercent}%";
                targetText.text = targetStr;
                targetText.color = goal.targetPercent >= 0
                    ? new Color(0.3f, 1f, 0.3f)
                    : new Color(1f, 0.4f, 0.4f);
            }
        }

        private TextMeshProUGUI CreateText(Transform parent, string text, int fontSize, Color color, FontStyles style)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            if (_font != null) tmp.font = _font;
            return tmp;
        }

        // ====================================================
        // Selection
        // ====================================================

        private void SelectGoal(int index)
        {
            if (!_isActive) return;
            if (_goals == null || index >= _goals.Count) return;

            _isActive = false;

            // 播放音效
            if (sfxSource != null && clickSfxClip != null)
                sfxSource.PlayOneShot(clickSfxClip);

            // 儲存選擇
            GameData.SelectedGoalIndex = index;

            // 回呼
            _onGoalSelected?.Invoke(index);
        }

        private void OnBackClicked()
        {
            if (!_isActive) return;
            Hide();
            _onBack?.Invoke();
        }

        // ====================================================
        // Helpers
        // ====================================================

        private string GetStockName(string stockCode)
        {
            if (_stocks == null) return stockCode;
            foreach (var s in _stocks)
            {
                if (s.code == stockCode) return s.name;
            }
            return stockCode;
        }
    }
}
