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

            // 生成目標卡片
            PopulateGoalCards();

            // 更新底部提示文字
            if (bottomText != null)
                bottomText.text = "按 Q / W / E / A / S / D 選擇你的目標，別讓市場察覺你的下一步";

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
            // 卡片容器
            var cardGO = new GameObject($"GoalCard_{index}", typeof(RectTransform), typeof(Image));
            cardGO.transform.SetParent(gridContainer, false);

            var img = cardGO.GetComponent<Image>();
            img.color = new Color(0.08f, 0.08f, 0.12f, 0.92f);

            // 使用 VerticalLayoutGroup 排列內容
            var layout = cardGO.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.spacing = 2;
            layout.padding = new RectOffset(14, 14, 10, 8);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // === 第一行：Key hint (大字) + Nickname ===
            var headerRow = new GameObject("Header", typeof(RectTransform));
            headerRow.transform.SetParent(cardGO.transform, false);
            var headerLayout = headerRow.AddComponent<HorizontalLayoutGroup>();
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.spacing = 12;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = false;
            var headerLE = headerRow.AddComponent<LayoutElement>();
            headerLE.preferredHeight = 55;

            // Key hint 大字
            CreateText(headerRow.transform, keyHint, 50, Color.white, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);

            // 外號
            CreateText(headerRow.transform, goal.nickname, 24, new Color(0.9f, 0.85f, 0.7f), FontStyles.Normal, TextAlignmentOptions.MidlineLeft);

            // === 中間：Icon（左） + 描述文字（右）===
            var midRow = new GameObject("Middle", typeof(RectTransform));
            midRow.transform.SetParent(cardGO.transform, false);
            var midLayout = midRow.AddComponent<HorizontalLayoutGroup>();
            midLayout.childAlignment = TextAnchor.UpperLeft;
            midLayout.spacing = 10;
            midLayout.childForceExpandWidth = false;
            midLayout.childForceExpandHeight = true;
            midLayout.padding = new RectOffset(0, 0, 4, 4);
            var midLE = midRow.AddComponent<LayoutElement>();
            midLE.flexibleHeight = 1;
            midLE.preferredHeight = 80;

            // 圓形 Icon（描述旁邊）
            if (!string.IsNullOrEmpty(goal.unselectIconId))
            {
                var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGO.transform.SetParent(midRow.transform, false);
                var iconImg = iconGO.GetComponent<Image>();
                Sprite iconSprite = Resources.Load<Sprite>($"GoalImages/{goal.unselectIconId}");
                if (iconSprite != null)
                {
                    iconImg.sprite = iconSprite;
                    iconImg.preserveAspect = true;
                    iconImg.color = Color.white;
                }
                else
                {
                    iconImg.color = new Color(0.4f, 0.4f, 0.4f, 0.6f);
                }
                var iconLE2 = iconGO.AddComponent<LayoutElement>();
                iconLE2.preferredWidth = 64;
                iconLE2.preferredHeight = 64;
                iconLE2.minWidth = 64;
            }

            // 描述文字
            var descText = CreateText(midRow.transform, goal.nicknameDesc, 22, new Color(0.75f, 0.75f, 0.75f), FontStyles.Normal, TextAlignmentOptions.TopLeft);
            if (descText != null)
            {
                var descLE = descText.gameObject.AddComponent<LayoutElement>();
                descLE.flexibleWidth = 1;
            }

            // === 底部：任務目標小標 + 股票 + 漲幅 ===
            var bottomRow = new GameObject("Bottom", typeof(RectTransform));
            bottomRow.transform.SetParent(cardGO.transform, false);
            var bottomLayout = bottomRow.AddComponent<HorizontalLayoutGroup>();
            bottomLayout.childAlignment = TextAnchor.MiddleLeft;
            bottomLayout.spacing = 8;
            bottomLayout.childForceExpandWidth = false;
            bottomLayout.childForceExpandHeight = false;
            var bottomLE = bottomRow.AddComponent<LayoutElement>();
            bottomLE.preferredHeight = 32;

            // "任務目標 Mission Objectives" 小字
            CreateText(bottomRow.transform, "任務目標 Mission Objectives", 12, new Color(0.5f, 0.5f, 0.5f), FontStyles.Normal, TextAlignmentOptions.MidlineLeft);

            // 股票名稱 + 漲幅
            string stockName = GetStockName(goal.stockCode);
            string pctText = goal.targetPercent >= 0 ? $"+{goal.targetPercent}%" : $"{goal.targetPercent}%";
            Color pctColor = goal.targetPercent >= 0 ? new Color(0.3f, 1f, 0.3f) : new Color(1f, 0.4f, 0.4f);
            CreateText(bottomRow.transform, $"{stockName}  {pctText}", 20, pctColor, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);

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

        private TextMeshProUGUI CreateText(Transform parent, string text, int fontSize, Color color, FontStyles style, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Overflow;
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
