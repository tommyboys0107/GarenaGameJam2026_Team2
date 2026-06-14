using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using DG.Tweening;
using Core;
using Data;

namespace UI
{
    /// <summary>
    /// 角色選擇頁面主控制器（UI Toolkit 版本）。
    /// 管理兩步驟流程：選角色 → 選目標，以及音訊播放與轉場。
    /// </summary>
    public class CharacterSelectController : MonoBehaviour
    {
        // === Serialized Fields ===
        [Header("UI")]
        [SerializeField] private UIDocument uiDocument;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource voiceSource;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip bgmClip;
        [SerializeField] private AudioClip clickSfxClip;

        // === State Machine ===
        private enum State { SelectingCharacter, SelectingGoal, TransitioningOut }
        private State _currentState;

        // === Data ===
        private IReadOnlyList<Core.CharacterInfo> _characters;
        private List<GoalInfo> _goals;
        private List<StockInfo> _stocks;
        private int _selectedCharIndex = -1;

        // === UI Element References ===
        private VisualElement _root;
        private VisualElement _characterPage;
        private VisualElement _goalPage;
        private VisualElement _characterGrid;
        private VisualElement _goalGrid;
        private VisualElement _tooltip;
        private Label _tooltipText;
        private VisualElement _fadeOverlay;

        // ====================================================
        // Lifecycle
        // ====================================================

        private void Start()
        {
            CharacterDataManager.LoadData();
            _characters = CharacterDataManager.GetAllCharacters();
            _goals = CSVLoader.LoadGoals();
            _stocks = CSVLoader.LoadStocks();

            SetupUI();
            SetupAudio();

            _currentState = State.SelectingCharacter;
            SetupCharacterPage();
        }

        private void Update()
        {
            if (_currentState == State.SelectingGoal)
                PollGoalKeyboard();
        }

        // ====================================================
        // UI Setup
        // ====================================================

        private void SetupUI()
        {
            if (uiDocument == null) return;

            _root = uiDocument.rootVisualElement;
            _characterPage = _root.Q<VisualElement>("character-page");
            _goalPage = _root.Q<VisualElement>("goal-page");
            _characterGrid = _root.Q<VisualElement>("character-grid");
            _goalGrid = _root.Q<VisualElement>("goal-grid");
            _tooltip = _root.Q<VisualElement>("tooltip");
            _tooltipText = _root.Q<Label>("tooltip-text");
            _fadeOverlay = _root.Q<VisualElement>("fade-overlay");

            // 確保 background-image 不攔截滑鼠事件
            var bg = _root.Q<VisualElement>("background-image");
            if (bg != null)
                bg.pickingMode = PickingMode.Ignore;

            // fade-overlay 初始也不攔截
            if (_fadeOverlay != null)
                _fadeOverlay.pickingMode = PickingMode.Ignore;
        }

        // ====================================================
        // Audio
        // ====================================================

        private void SetupAudio()
        {
            if (bgmSource == null) return;
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            if (bgmClip != null)
            {
                bgmSource.clip = bgmClip;
                bgmSource.Play();
            }
        }

        // ====================================================
        // Character Page
        // ====================================================

        private void SetupCharacterPage()
        {
            if (_characterGrid == null || _characters == null || _characters.Count == 0)
                return;

            for (int i = 0; i < _characters.Count; i++)
            {
                var character = _characters[i];
                int index = i;

                // Card container
                var card = new VisualElement();
                card.AddToClassList("character-card");
                card.name = $"character-card-{i}";

                // Portrait — 從 Resources/CharacterImages 載入
                var portrait = new VisualElement();
                portrait.AddToClassList("character-portrait");
                portrait.pickingMode = PickingMode.Ignore;

                string imageId = !string.IsNullOrEmpty(character.ImageID) ? character.ImageID : $"C0{i + 1}_Face";
                Sprite sprite = Resources.Load<Sprite>($"CharacterImages/{imageId}");
                if (sprite != null)
                    portrait.style.backgroundImage = new StyleBackground(sprite);
                else
                    portrait.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 0.8f));

                card.Add(portrait);

                // 卡片上不顯示名稱 — 名稱和描述只在 hover tooltip 中顯示

                // Hover: 顯示角色名稱 + 描述 tooltip
                card.RegisterCallback<MouseEnterEvent>(evt => OnCardHoverEnter(evt, index));
                card.RegisterCallback<MouseMoveEvent>(evt => OnCardHoverMove(evt));
                card.RegisterCallback<MouseLeaveEvent>(evt => OnCardHoverLeave(evt));

                // Click: 選取角色
                card.RegisterCallback<ClickEvent>(evt => OnCharacterClick(evt, index));

                _characterGrid.Add(card);
            }

            // Tooltip 初始隱藏
            if (_tooltip != null)
                _tooltip.AddToClassList("hidden");
        }

        // ====================================================
        // Hover — 顯示角色名稱 + 描述
        // ====================================================

        private void OnCardHoverEnter(MouseEnterEvent evt, int index)
        {
            if (_characters == null || index < 0 || index >= _characters.Count)
                return;

            var character = _characters[index];
            if (_tooltip == null || _tooltipText == null) return;

            // 組合名稱 + 描述
            string tooltipContent = character.Name;
            if (!string.IsNullOrEmpty(character.Description))
                tooltipContent += "\n" + character.Description;

            _tooltipText.text = tooltipContent;

            // 初始定位
            PositionTooltipAtMouse(evt.mousePosition);
            _tooltip.RemoveFromClassList("hidden");
        }

        private void OnCardHoverMove(MouseMoveEvent evt)
        {
            if (_tooltip == null || _tooltip.ClassListContains("hidden")) return;
            PositionTooltipAtMouse(evt.mousePosition);
        }

        private void PositionTooltipAtMouse(Vector2 mousePos)
        {
            float tooltipLeft = mousePos.x + 20f;
            float tooltipTop = mousePos.y + 15f;

            float panelWidth = _root.resolvedStyle.width;
            float panelHeight = _root.resolvedStyle.height;
            if (tooltipLeft + 500f > panelWidth)
                tooltipLeft = mousePos.x - 520f;
            if (tooltipLeft < 0) tooltipLeft = 0;
            if (tooltipTop + 200f > panelHeight)
                tooltipTop = mousePos.y - 200f;
            if (tooltipTop < 0) tooltipTop = 0;

            _tooltip.style.left = tooltipLeft;
            _tooltip.style.top = tooltipTop;
        }

        private void OnCardHoverLeave(MouseLeaveEvent evt)
        {
            if (_tooltip != null)
                _tooltip.AddToClassList("hidden");
        }

        // ====================================================
        // Click — 選取角色
        // ====================================================

        private void OnCharacterClick(ClickEvent evt, int index)
        {
            if (_currentState != State.SelectingCharacter)
                return;

            // 同一張卡片不重複選
            if (index == _selectedCharIndex)
                return;

            // 移除舊選取
            if (_selectedCharIndex >= 0)
            {
                var prev = _characterGrid.Q<VisualElement>($"character-card-{_selectedCharIndex}");
                prev?.RemoveFromClassList("character-card--selected");
            }

            // 加上新選取
            var card = _characterGrid.Q<VisualElement>($"character-card-{index}");
            card?.AddToClassList("character-card--selected");

            _selectedCharIndex = index;
            GameData.SelectedCharacterIndex = index;

            // 音效
            PlayClickSfx();
            PlayCharacterVoice(index);

            // 進入目標選擇
            _currentState = State.SelectingGoal;
            ShowGoalPage();
        }

        // ====================================================
        // Goal Page
        // ====================================================

        private void ShowGoalPage()
        {
            if (_goalGrid == null) return;
            _goalGrid.Clear();

            if (_goals == null || _goals.Count == 0)
            {
                var errorLabel = new Label("目標資料不可用");
                errorLabel.style.color = Color.white;
                errorLabel.style.fontSize = 24;
                errorLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                _goalGrid.Add(errorLabel);
                ShowGoalPageAnimation();
                return;
            }

            string[] keyHints = { "Q", "W", "E", "A", "S", "D" };
            int count = Mathf.Min(_goals.Count, 6);

            for (int i = 0; i < count; i++)
            {
                var goal = _goals[i];
                var option = new VisualElement();
                option.AddToClassList("goal-option");
                option.name = $"goal-option-{i}";

                // 外號
                var nicknameLabel = new Label(goal.nickname);
                nicknameLabel.style.color = new Color(0.9f, 0.85f, 0.7f, 1f);
                nicknameLabel.style.fontSize = 30;
                nicknameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                nicknameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                nicknameLabel.style.marginBottom = 8;
                option.Add(nicknameLabel);

                // stockCode + 股票名稱
                string stockName = GetStockName(goal.stockCode);
                string stockDisplay = string.IsNullOrEmpty(stockName)
                    ? goal.stockCode
                    : $"{goal.stockCode} ({stockName})";
                var stockLabel = new Label(stockDisplay);
                stockLabel.style.color = Color.white;
                stockLabel.style.fontSize = 40;
                stockLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                stockLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                option.Add(stockLabel);

                // targetPercent
                string percentText = goal.targetPercent >= 0 ? $"+{goal.targetPercent}%" : $"{goal.targetPercent}%";
                var percentLabel = new Label(percentText);
                percentLabel.style.color = goal.targetPercent >= 0
                    ? new Color(1f, 0.3f, 0.3f, 1f)
                    : new Color(0.3f, 1f, 0.3f, 1f);
                percentLabel.style.fontSize = 36;
                percentLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                percentLabel.style.marginTop = 6;
                option.Add(percentLabel);

                // 按鍵提示
                var keyLabel = new Label(keyHints[i]);
                keyLabel.AddToClassList("key-hint");
                option.Add(keyLabel);

                _goalGrid.Add(option);
            }

            ShowGoalPageAnimation();
        }

        private void ShowGoalPageAnimation()
        {
            if (_goalPage == null) return;

            // 在 goal-grid 下方加入返回按鈕
            var existingBackBtn = _goalPage.Q<Button>("back-button");
            if (existingBackBtn == null)
            {
                var backBtn = new Button(() => GoBackToCharacterPage());
                backBtn.name = "back-button";
                backBtn.text = "← 返回選擇角色";
                backBtn.AddToClassList("back-button");
                _goalPage.Add(backBtn);
            }

            _goalPage.style.display = DisplayStyle.Flex;
            _goalPage.schedule.Execute(() => _goalPage.RemoveFromClassList("page-right"));
        }

        // ====================================================
        // Goal Keyboard
        // ====================================================

        private void PollGoalKeyboard()
        {
            if (Keyboard.current == null) return;

            // Escape: 返回角色選擇頁面
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                GoBackToCharacterPage();
                return;
            }

            if (Keyboard.current.qKey.wasPressedThisFrame) SelectGoal(0);
            else if (Keyboard.current.wKey.wasPressedThisFrame) SelectGoal(1);
            else if (Keyboard.current.eKey.wasPressedThisFrame) SelectGoal(2);
            else if (Keyboard.current.aKey.wasPressedThisFrame) SelectGoal(3);
            else if (Keyboard.current.sKey.wasPressedThisFrame) SelectGoal(4);
            else if (Keyboard.current.dKey.wasPressedThisFrame) SelectGoal(5);
        }

        /// <summary>
        /// 按 Escape 返回角色選擇頁面，重置選取狀態。
        /// </summary>
        private void GoBackToCharacterPage()
        {
            // 隱藏目標頁面（加回 page-right class 觸發滑出動畫）
            if (_goalPage != null)
            {
                _goalPage.AddToClassList("page-right");
            }

            // 移除之前的角色選取高亮
            if (_selectedCharIndex >= 0 && _characterGrid != null)
            {
                var prev = _characterGrid.Q<VisualElement>($"character-card-{_selectedCharIndex}");
                prev?.RemoveFromClassList("character-card--selected");
            }

            // 重置狀態
            _selectedCharIndex = -1;
            _currentState = State.SelectingCharacter;
        }

        private void SelectGoal(int index)
        {
            GameData.SelectedGoalIndex = index;
            PlayClickSfx();
            _currentState = State.TransitioningOut;
            TriggerFadeOut();
            FadeBgmAndLoadGame();
        }

        // ====================================================
        // Audio Helpers
        // ====================================================

        private void PlayClickSfx()
        {
            if (sfxSource != null && clickSfxClip != null)
                sfxSource.PlayOneShot(clickSfxClip);
        }

        private void PlayCharacterVoice(int characterIndex)
        {
            if (voiceSource == null) return;

            // 從 CSV 的 AudioID 欄位載入（格式如 C01_Intro）
            string audioId = null;
            if (_characters != null && characterIndex < _characters.Count)
            {
                var character = _characters[characterIndex];
                // 使用 Id + "_Intro" 作為語音檔名
                audioId = $"{character.Id}_Intro";
            }

            if (string.IsNullOrEmpty(audioId)) return;

            AudioClip clip = Resources.Load<AudioClip>($"NarratorVoice/{audioId}");
            if (clip == null)
            {
                Debug.LogWarning($"[CharacterSelectController] 找不到語音: Resources/NarratorVoice/{audioId}");
                return;
            }

            if (voiceSource.isPlaying) voiceSource.Stop();
            voiceSource.clip = clip;
            voiceSource.Play();
        }

        // ====================================================
        // Helpers
        // ====================================================

        private string GetStockName(string stockCode)
        {
            if (_stocks == null) return null;
            foreach (var s in _stocks)
            {
                if (s.code == stockCode) return s.name;
            }
            return null;
        }

        // ====================================================
        // Transition
        // ====================================================

        private void FadeBgmAndLoadGame()
        {
            if (bgmSource != null)
            {
                bgmSource.DOFade(0f, 1f).OnComplete(() => SceneLoader.LoadGame());
            }
            else
            {
                DOTween.Sequence().AppendInterval(1f).AppendCallback(() => SceneLoader.LoadGame());
            }
        }

        private void TriggerFadeOut()
        {
            if (_fadeOverlay != null)
            {
                _fadeOverlay.pickingMode = PickingMode.Position;
                _fadeOverlay.AddToClassList("fade-overlay--active");
            }
        }
    }
}
