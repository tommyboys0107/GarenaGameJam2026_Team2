using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using DG.Tweening;
using TMPro;
using Core;
using Data;

namespace UI
{
    /// <summary>
    /// 角色選擇頁面主控制器（uGUI 版本）。
    /// 使用 Canvas + Image + RectTransform 進行精確 anchor 定位。
    /// </summary>
    public class CharacterSelectController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image portraitImage;
        [SerializeField] private TextMeshProUGUI characterNameText;
        [SerializeField] private TextMeshProUGUI characterDescText;
        [SerializeField] private GameObject confirmButton;
        [SerializeField] private CanvasGroup fadeOverlay;

        [Header("Card Slots (拖入 6 個 Image)")]
        [SerializeField] private Image[] cardSlots = new Image[6];

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource voiceSource;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip bgmClip;
        [SerializeField] private AudioClip clickSfxClip;

        [Header("Goal Select Panel (uGUI)")]
        [SerializeField] private GoalSelectUI goalSelectUI;

        // === State ===
        private enum State { SelectingCharacter, SelectingGoal, TransitioningOut }
        private State _currentState;

        // === Data ===
        private IReadOnlyList<Core.CharacterInfo> _characters;
        private List<GoalInfo> _goals;
        private List<StockInfo> _stocks;
        private int _selectedCharIndex = -1;

        // ====================================================
        // Lifecycle
        // ====================================================

        private void Start()
        {
            CharacterDataManager.LoadData();
            _characters = CharacterDataManager.GetAllCharacters();
            _goals = CSVLoader.LoadGoals();
            _stocks = CSVLoader.LoadStocks();

            SetupCards();
            SetupAudio();
            HideInfo();

            _currentState = State.SelectingCharacter;

            // 預設選取第一個
            SelectCharacter(0);
        }

        private void Update()
        {
            if (_currentState == State.SelectingGoal)
                PollGoalKeyboard();
        }

        // ====================================================
        // Setup
        // ====================================================

        private void SetupCards()
        {
            if (_characters == null) return;

            int count = Mathf.Min(_characters.Count, cardSlots.Length);
            for (int i = 0; i < count; i++)
            {
                if (cardSlots[i] == null) continue;

                var character = _characters[i];
                int index = i;

                // 載入未選取圖片
                string unselectId = !string.IsNullOrEmpty(character.UnselectImageID)
                    ? character.UnselectImageID : character.Id;
                Sprite sprite = Resources.Load<Sprite>($"CharacterImages/{unselectId}");
                if (sprite != null)
                    cardSlots[i].sprite = sprite;

                // 加上 Button 事件
                var btn = cardSlots[i].GetComponent<Button>();
                if (btn == null)
                    btn = cardSlots[i].gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => OnCardClick(index));
            }
        }

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
        // Card Click
        // ====================================================

        private void OnCardClick(int index)
        {
            if (_currentState != State.SelectingCharacter) return;
            if (index == _selectedCharIndex) return;

            SelectCharacter(index);
            PlayClickSfx();
            PlayCharacterVoice(index);
        }

        private void SelectCharacter(int index)
        {
            if (_characters == null || index < 0 || index >= _characters.Count) return;

            var character = _characters[index];

            // 還原前一個（只換圖，不動 scale）
            if (_selectedCharIndex >= 0 && _selectedCharIndex < cardSlots.Length && cardSlots[_selectedCharIndex] != null)
            {
                var prevChar = _characters[_selectedCharIndex];
                var prevSlot = cardSlots[_selectedCharIndex];

                // 換回未選取圖片
                string prevId = !string.IsNullOrEmpty(prevChar.UnselectImageID)
                    ? prevChar.UnselectImageID : prevChar.Id;
                Sprite prevSprite = Resources.Load<Sprite>($"CharacterImages/{prevId}");
                if (prevSprite != null) prevSlot.sprite = prevSprite;
            }

            // 設定新選取（只換圖，不動 scale）
            if (index < cardSlots.Length && cardSlots[index] != null)
            {
                var slot = cardSlots[index];

                // 換成選取圖片
                string selId = !string.IsNullOrEmpty(character.SelectImageID)
                    ? character.SelectImageID : $"{character.Id}_C";
                Sprite selSprite = Resources.Load<Sprite>($"CharacterImages/{selId}");
                if (selSprite != null) slot.sprite = selSprite;
            }

            _selectedCharIndex = index;
            GameData.SelectedCharacterIndex = index;

            // 更新右側
            UpdateInfo(character);
        }

        // ====================================================
        // Info Panel
        // ====================================================

        private void UpdateInfo(Core.CharacterInfo character)
        {
            // 大圖
            if (portraitImage != null)
            {
                string pid = !string.IsNullOrEmpty(character.PortraitImageID)
                    ? character.PortraitImageID : $"{character.Id}_F";
                Sprite ps = Resources.Load<Sprite>($"CharacterImages/{pid}");
                if (ps != null)
                {
                    portraitImage.sprite = ps;
                    portraitImage.enabled = true;
                }
            }

            // 名稱
            if (characterNameText != null)
                characterNameText.text = character.Name ?? "";

            // 描述 + 特長
            if (characterDescText != null)
            {
                string desc = character.Description ?? "";
                string specialty = FormatSpecialty(character.Specialty);
                if (!string.IsNullOrEmpty(specialty))
                    desc += "\n" + specialty;
                characterDescText.text = desc;
            }

            // 顯示確認按鈕
            if (confirmButton != null)
                confirmButton.SetActive(true);
        }

        private void HideInfo()
        {
            if (portraitImage != null) portraitImage.enabled = false;
            if (characterNameText != null) characterNameText.text = "";
            if (characterDescText != null) characterDescText.text = "";
            if (confirmButton != null) confirmButton.SetActive(false);
        }

        // ====================================================
        // Confirm
        // ====================================================

        public void OnConfirmClick()
        {
            if (_currentState != State.SelectingCharacter) return;
            if (_selectedCharIndex < 0) return;

            PlayClickSfx();
            _currentState = State.SelectingGoal;

            // 顯示目標選擇面板
            if (goalSelectUI != null)
            {
                goalSelectUI.Show(
                    _selectedCharIndex,
                    onBack: () =>
                    {
                        // 返回角色選擇
                        _currentState = State.SelectingCharacter;
                    },
                    onGoalSelected: (goalIndex) =>
                    {
                        // 目標選定，開始轉場
                        _currentState = State.TransitioningOut;
                        TriggerFadeOut();
                        FadeBgmAndLoadGame();
                    }
                );
            }
        }

        // ====================================================
        // Audio
        // ====================================================

        private void PlayClickSfx()
        {
            if (sfxSource != null && clickSfxClip != null)
                sfxSource.PlayOneShot(clickSfxClip);
        }

        private void PlayCharacterVoice(int characterIndex)
        {
            if (voiceSource == null) return;

            string audioId = null;
            if (_characters != null && characterIndex < _characters.Count)
            {
                var character = _characters[characterIndex];
                audioId = !string.IsNullOrEmpty(character.AudioID)
                    ? character.AudioID : $"{character.Id}_Intro";
            }
            if (string.IsNullOrEmpty(audioId)) return;

            AudioClip clip = Resources.Load<AudioClip>($"NarratorVoice/{audioId}");
            if (clip == null) return;

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
                if (s.code == stockCode) return s.name;
            return null;
        }

        private string FormatSpecialty(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            var matches = System.Text.RegularExpressions.Regex.Matches(raw, @"\[(\w+),([\+\-]?\d+)\]");
            if (matches.Count == 0) return "";
            var parts = new List<string>();
            foreach (System.Text.RegularExpressions.Match m in matches)
            {
                string code = m.Groups[1].Value;
                string value = m.Groups[2].Value;
                string stockName = GetStockName(code) ?? code;
                parts.Add($"{stockName} {value}%");
            }
            return string.Join(", ", parts);
        }

        // ====================================================
        // Transition
        // ====================================================

        private void FadeBgmAndLoadGame()
        {
            if (bgmSource != null)
                bgmSource.DOFade(0f, 1f).OnComplete(() => SceneLoader.LoadGame());
            else
                DOTween.Sequence().AppendInterval(1f).AppendCallback(() => SceneLoader.LoadGame());
        }

        private void TriggerFadeOut()
        {
            if (fadeOverlay != null)
            {
                fadeOverlay.blocksRaycasts = true;
                fadeOverlay.DOFade(1f, 0.5f);
            }
        }

        // ====================================================
        // Goal Keyboard (由 GoalSelectUI 處理)
        // ====================================================

        private void PollGoalKeyboard()
        {
            // 由 GoalSelectUI 內部 Update 處理按鍵
        }
    }
}
