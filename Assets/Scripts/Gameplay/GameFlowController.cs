using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Data;
using Twitch;
using UI;

namespace Gameplay
{
    /// <summary>
    /// 遊戲流程控制器。
    /// 負責排程操盤手事件（玩家選擇）與觀眾投票事件，
    /// 從 CSV 載入事件資料，並透過事件對外廣播選擇結果的股價影響。
    /// 
    /// 排程：9 個 slot（每 10 秒一個）
    /// [玩家, 玩家, 觀眾, 玩家, 玩家, 觀眾, 玩家, 玩家, 觀眾]
    /// </summary>
    public class GameFlowController : MonoBehaviour
    {
        [Header("參考")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private TwitchVoteManager voteManager;
        [SerializeField] private TVChoiceUI tvChoiceUI;
        [SerializeField] private TraderChoiceUI traderChoiceUI;

        [Header("事件描述 UI")]
        [SerializeField] private TMPro.TMP_Text eventDescriptionText;

        [Header("NPC 圖片（操盤手事件時顯示）")]
        [SerializeField] private GameObject npcImage;
        [SerializeField] private GameObject npcMsg;
        [SerializeField] private GameObject npcMsgBG;
        [SerializeField] private TMPro.TMP_Text npcMsgContent;

        [Header("時間設定（從 TimeData.csv 自動讀取，Inspector 值為備用）")]
        [SerializeField] private float startDelay = 10f;
        [SerializeField] private float slotDuration = 20f;
        [SerializeField] private float audienceSlotDuration = 20f;
        [SerializeField] private float eventInterval = 5f;
        [SerializeField] private float playerChoiceTimeout = 20;

        // === 對外事件 ===

        /// <summary>
        /// 當任何事件的選擇確定後觸發，帶出該選擇的股價影響。
        /// 你的夥伴可以訂閱這個事件來套用股價變動。
        /// </summary>
        public event Action<StockEffect[]> OnStockEffectApplied;

        /// <summary>
        /// 觀眾事件結果：(獲勝的 AudienceEventInfo)
        /// </summary>
        public event Action<AudienceEventInfo> OnAudienceEventResolved;

        /// <summary>
        /// 玩家事件結果：(選中的 ChoiceInfo)
        /// </summary>
        public event Action<ChoiceInfo> OnTraderEventResolved;

        /// <summary>
        /// 新的事件 slot 開始：(slot index, 事件類型)
        /// </summary>
        public event Action<int, EventSlotType> OnSlotStarted;

        // === 公開屬性 ===

        /// <summary>目前正在進行的 slot 索引 (0-based)</summary>
        public int CurrentSlotIndex { get; private set; } = -1;

        /// <summary>目前 slot 類型</summary>
        public EventSlotType CurrentSlotType { get; private set; }

        /// <summary>是否正在等待玩家/觀眾做出選擇</summary>
        public bool IsWaitingForChoice { get; private set; }

        // === 內部資料 ===

        // CSV 載入的原始資料
        private List<AudienceEventInfo> _allAudienceEvents;
        private List<EventInfo> _allTraderEvents;
        private Dictionary<string, ChoiceInfo> _allChoices;

        // 已使用過的事件（避免重複）
        private HashSet<int> _usedAudienceIndices = new HashSet<int>();
        private HashSet<int> _usedTraderIndices = new HashSet<int>();

        // 排程
        private EventSlotType[] _schedule;
        private float _slotTimer;
        private bool _slotActive;
        private float _startDelayTimer;
        private bool _started;

        // 玩家事件暫存
        private float _playerTimer;
        private ChoiceInfo[] _currentTraderChoices;
        private EventInfo _currentTraderEvent;

        // 觀眾事件暫存
        private AudienceEventInfo[] _currentAudienceOptions;

        private void Awake()
        {
            LoadCSVData();
            ApplyTimeConfig();
            BuildSchedule();

            // 初始隱藏事件描述文字
            if (eventDescriptionText != null)
                eventDescriptionText.gameObject.SetActive(false);

            // 遊戲開始時關閉 NPC
            if (npcImage != null) npcImage.SetActive(false);
            if (npcMsg != null) npcMsg.SetActive(false);
            if (npcMsgBG != null) npcMsgBG.SetActive(false);
        }

        private void ApplyTimeConfig()
        {
            var config = CSVLoader.LoadTimeConfig();
            startDelay = config.startDuration;
            slotDuration = config.traderEventTime;
            audienceSlotDuration = config.audienceEventTime;
            eventInterval = config.eventInterval;
            playerChoiceTimeout = config.traderEventTime;

            // 同步 Twitch 投票時長到 TwitchConfig
            if (voteManager != null)
            {
                var twitchIRC = voteManager.GetComponent<Twitch.TwitchIRC>();
                if (twitchIRC != null)
                {
                    // 透過反射取得 private config 欄位
                    var field = typeof(Twitch.TwitchIRC).GetField("config",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        var twitchConfig = field.GetValue(twitchIRC) as Twitch.TwitchConfig;
                        if (twitchConfig != null)
                        {
                            twitchConfig.voteDuration = config.audienceEventTime;
                            Debug.Log($"[GameFlow] TwitchConfig.voteDuration 同步為 {config.audienceEventTime} 秒");
                        }
                    }
                }
            }

            Debug.Log($"[GameFlow] TimeConfig 載入: startDelay={startDelay}, traderSlot={slotDuration}, " +
                      $"audienceSlot={audienceSlotDuration}, interval={eventInterval}, timeout={playerChoiceTimeout}");
        }

        private void OnEnable()
        {
            if (voteManager != null)
                voteManager.OnVoteComplete += HandleAudienceVoteComplete;
            if (traderChoiceUI != null)
                traderChoiceUI.OnChoiceSelected += HandleTraderButtonChoice;
        }

        private void OnDisable()
        {
            if (voteManager != null)
                voteManager.OnVoteComplete -= HandleAudienceVoteComplete;
            if (traderChoiceUI != null)
                traderChoiceUI.OnChoiceSelected -= HandleTraderButtonChoice;
        }

        private void HandleTraderButtonChoice(int index)
        {
            SelectTraderChoice(index);
        }

        private void Update()
        {
            if (gameManager == null || !gameManager.IsPlaying) return;

            // Slot 計時
            if (_slotActive)
            {
                _slotTimer += Time.deltaTime;

                // 玩家事件倒數
                if (CurrentSlotType == EventSlotType.Trader && IsWaitingForChoice)
                {
                    _playerTimer += Time.deltaTime;

                    // 超時自動選第一個
                    if (_playerTimer >= playerChoiceTimeout)
                    {
                        SelectTraderChoice(0);
                    }
                }

                // 觀眾事件倒數（從 TwitchVoteManager 取剩餘時間）
                if (CurrentSlotType == EventSlotType.Audience && IsWaitingForChoice)
                {
                    if (voteManager != null && tvChoiceUI != null)
                        tvChoiceUI.UpdateTimer(voteManager.TimeRemaining);
                }

                // Slot 結束，等待間隔後進入下一個
                float currentSlotDuration = (CurrentSlotType == EventSlotType.Audience)
                    ? audienceSlotDuration
                    : slotDuration;

                if (_slotTimer >= currentSlotDuration + eventInterval)
                {
                    EndCurrentSlot();
                    AdvanceToNextSlot();
                }
            }
            else
            {
                // 開始延遲倒數
                if (!_started)
                {
                    _startDelayTimer += Time.deltaTime;
                    if (_startDelayTimer >= startDelay)
                    {
                        _started = true;
                        AdvanceToNextSlot();
                    }
                }
            }
        }

        // === 公開方法 ===

        /// <summary>
        /// 玩家點選選項時呼叫（0, 1, 2）。
        /// 由 UI 按鈕等外部觸發。
        /// </summary>
        public void SelectTraderChoice(int choiceIndex)
        {
            if (!IsWaitingForChoice || CurrentSlotType != EventSlotType.Trader) return;
            if (_currentTraderChoices == null || choiceIndex < 0 || choiceIndex >= _currentTraderChoices.Length) return;

            IsWaitingForChoice = false;
            var chosen = _currentTraderChoices[choiceIndex];

            // 關閉 NPC 圖片和訊息
            if (npcImage != null) npcImage.SetActive(false);
            if (npcMsg != null) npcMsg.SetActive(false);
            if (npcMsgBG != null) npcMsgBG.SetActive(false);

            // 關閉操盤手按鈕 UI
            if (traderChoiceUI != null) traderChoiceUI.Hide();

            Debug.Log($"[GameFlow] 玩家選擇: {chosen.name} → 影響: {FormatEffects(chosen.effects)}");

            // 隱藏倒數
            if (tvChoiceUI != null)
                tvChoiceUI.HideTimer();

            // 顯示結果
            if (tvChoiceUI != null)
                tvChoiceUI.ShowResult(choiceIndex, chosen.resultText);

            // 顯示詳細描述
            if (eventDescriptionText != null)
                eventDescriptionText.text = chosen.resultText;

            // 廣播事件
            OnStockEffectApplied?.Invoke(chosen.effects);
            OnTraderEventResolved?.Invoke(chosen);
        }

        // === 內部流程 ===

        private void LoadCSVData()
        {
            _allAudienceEvents = CSVLoader.LoadAudienceEvents();
            _allTraderEvents = CSVLoader.LoadEvents();
            _allChoices = CSVLoader.LoadChoices();

            Debug.Log($"[GameFlow] 載入資料: {_allAudienceEvents.Count} 觀眾事件, " +
                      $"{_allTraderEvents.Count} 操盤手事件, {_allChoices.Count} 選項");
        }

        private void BuildSchedule()
        {
            // 固定排程：玩玩觀 × 3
            _schedule = new EventSlotType[]
            {
                EventSlotType.Trader,
                EventSlotType.Trader,
                EventSlotType.Audience,
                EventSlotType.Trader,
                EventSlotType.Trader,
                EventSlotType.Audience,
                EventSlotType.Trader,
                EventSlotType.Trader,
                EventSlotType.Audience,
            };
        }

        private void AdvanceToNextSlot()
        {
            CurrentSlotIndex++;

            if (CurrentSlotIndex >= _schedule.Length)
            {
                // 所有 slot 結束
                _slotActive = false;

                // 確保 NPC 關閉
                if (npcImage != null) npcImage.SetActive(false);
                if (npcMsg != null) npcMsg.SetActive(false);
                if (npcMsgBG != null) npcMsgBG.SetActive(false);

                Debug.Log("[GameFlow] 所有事件 slot 結束");
                return;
            }

            CurrentSlotType = _schedule[CurrentSlotIndex];
            _slotTimer = 0f;
            _slotActive = true;

            Debug.Log($"[GameFlow] Slot {CurrentSlotIndex + 1}/{_schedule.Length} 開始 — 類型: {CurrentSlotType}");
            OnSlotStarted?.Invoke(CurrentSlotIndex, CurrentSlotType);

            // 顯示事件描述 GameObject 並清空文字
            if (eventDescriptionText != null)
            {
                eventDescriptionText.gameObject.SetActive(true);
                eventDescriptionText.text = "";
            }

            if (CurrentSlotType == EventSlotType.Trader)
            {
                StartTraderEvent();
            }
            else
            {
                StartAudienceEvent();
            }
        }

        private void EndCurrentSlot()
        {
            // 如果玩家還沒選（不應該走到這裡，因為有 timeout）
            if (IsWaitingForChoice && CurrentSlotType == EventSlotType.Trader)
            {
                SelectTraderChoice(0);
            }

            if (tvChoiceUI != null)
                tvChoiceUI.Hide();

            IsWaitingForChoice = false;
        }

        // === 操盤手事件（玩家選擇）===

        private void StartTraderEvent()
        {
            var eventInfo = PickRandomTraderEvent();
            if (eventInfo == null)
            {
                Debug.LogWarning("[GameFlow] 沒有可用的操盤手事件了");
                return;
            }

            _currentTraderEvent = eventInfo;

            // 從 ChooseData 拿對應的選項
            _currentTraderChoices = new ChoiceInfo[eventInfo.choiceIds.Length];
            for (int i = 0; i < eventInfo.choiceIds.Length; i++)
            {
                string choiceId = eventInfo.choiceIds[i];
                if (_allChoices.TryGetValue(choiceId, out var choice))
                {
                    _currentTraderChoices[i] = choice;
                }
                else
                {
                    Debug.LogWarning($"[GameFlow] 找不到選項 ID: {choiceId}");
                    _currentTraderChoices[i] = new ChoiceInfo { id = choiceId, name = "???", effects = Array.Empty<StockEffect>() };
                }
            }

            // 顯示在 TVChoiceUI
            string opt1 = _currentTraderChoices.Length > 0 ? _currentTraderChoices[0].name : "";
            string opt2 = _currentTraderChoices.Length > 1 ? _currentTraderChoices[1].name : "";
            string opt3 = _currentTraderChoices.Length > 2 ? _currentTraderChoices[2].name : "";

            // 操盤手事件：用中間按鈕 UI，不用 TV
            if (traderChoiceUI != null)
                traderChoiceUI.Show(opt1, opt2, opt3);

            // 顯示 NPC 圖片和訊息
            if (npcImage != null) npcImage.SetActive(true);
            if (npcMsg != null) npcMsg.SetActive(true);
            if (npcMsgBG != null) npcMsgBG.SetActive(true);
            if (npcMsgContent != null) npcMsgContent.text = eventInfo.description;

            IsWaitingForChoice = true;
            _playerTimer = 0f;

            Debug.Log($"[GameFlow] 操盤手事件: {eventInfo.name} — {opt1} / {opt2} / {opt3}");
        }

        // === 觀眾事件（Twitch 投票）===

        private void StartAudienceEvent()
        {
            _currentAudienceOptions = PickRandomAudienceEvents(3);
            if (_currentAudienceOptions == null || _currentAudienceOptions.Length == 0)
            {
                Debug.LogWarning("[GameFlow] 沒有可用的觀眾事件了");
                return;
            }

            string opt1 = _currentAudienceOptions.Length > 0 ? _currentAudienceOptions[0].name : "";
            string opt2 = _currentAudienceOptions.Length > 1 ? _currentAudienceOptions[1].name : "";
            string opt3 = _currentAudienceOptions.Length > 2 ? _currentAudienceOptions[2].name : "";

            // 顯示在 TVChoiceUI
            if (tvChoiceUI != null)
                tvChoiceUI.Show("觀眾投票", opt1, opt2, opt3);

            // 觀眾事件顯示投票數
            if (tvChoiceUI != null)
                tvChoiceUI.SetVoteCountVisible(true);

            // 啟動 Twitch 投票
            if (voteManager != null)
                voteManager.StartVote(opt1, opt2, opt3);

            IsWaitingForChoice = true;

            Debug.Log($"[GameFlow] 觀眾投票開始: {opt1} / {opt2} / {opt3}");
        }

        private void HandleAudienceVoteComplete(int winnerIndex)
        {
            if (CurrentSlotType != EventSlotType.Audience || _currentAudienceOptions == null) return;

            IsWaitingForChoice = false;

            if (winnerIndex < 0 || winnerIndex >= _currentAudienceOptions.Length) return;

            var winner = _currentAudienceOptions[winnerIndex];

            Debug.Log($"[GameFlow] 觀眾投票結果: {winner.name} → 影響: {FormatEffects(winner.effects)}");

            // 隱藏倒數
            if (tvChoiceUI != null)
                tvChoiceUI.HideTimer();

            // 顯示結果
            if (tvChoiceUI != null)
                tvChoiceUI.ShowResult(winnerIndex, winner.resultText);

            // 顯示詳細描述
            if (eventDescriptionText != null)
                eventDescriptionText.text = winner.resultText;

            // 廣播事件
            OnStockEffectApplied?.Invoke(winner.effects);
            OnAudienceEventResolved?.Invoke(winner);
        }

        // === 隨機挑選工具 ===

        private EventInfo PickRandomTraderEvent()
        {
            if (_allTraderEvents == null || _allTraderEvents.Count == 0) return null;

            // 如果全部用過就重置
            if (_usedTraderIndices.Count >= _allTraderEvents.Count)
                _usedTraderIndices.Clear();

            // 建立可用索引清單
            var available = new List<int>();
            for (int i = 0; i < _allTraderEvents.Count; i++)
            {
                if (!_usedTraderIndices.Contains(i))
                    available.Add(i);
            }

            int picked = available[UnityEngine.Random.Range(0, available.Count)];
            _usedTraderIndices.Add(picked);
            return _allTraderEvents[picked];
        }

        private AudienceEventInfo[] PickRandomAudienceEvents(int count)
        {
            if (_allAudienceEvents == null || _allAudienceEvents.Count == 0) return null;

            // 如果剩餘不夠就重置
            if (_allAudienceEvents.Count - _usedAudienceIndices.Count < count)
                _usedAudienceIndices.Clear();

            var available = new List<int>();
            for (int i = 0; i < _allAudienceEvents.Count; i++)
            {
                if (!_usedAudienceIndices.Contains(i))
                    available.Add(i);
            }

            // Shuffle and pick
            var picked = new AudienceEventInfo[Mathf.Min(count, available.Count)];
            for (int i = 0; i < picked.Length; i++)
            {
                int randIdx = UnityEngine.Random.Range(i, available.Count);
                // Swap
                (available[i], available[randIdx]) = (available[randIdx], available[i]);

                int eventIdx = available[i];
                _usedAudienceIndices.Add(eventIdx);
                picked[i] = _allAudienceEvents[eventIdx];
            }

            return picked;
        }

        // === 工具 ===

        private string FormatEffects(StockEffect[] effects)
        {
            if (effects == null || effects.Length == 0) return "(無影響)";
            var parts = new string[effects.Length];
            for (int i = 0; i < effects.Length; i++)
            {
                var e = effects[i];
                string sign = e.value >= 0 ? "+" : "";
                parts[i] = $"[{e.stockCode},{sign}{e.value}]";
            }
            return string.Join(",", parts);
        }
    }

    /// <summary>
    /// 事件 slot 類型
    /// </summary>
    public enum EventSlotType
    {
        Trader,   // 操盤手事件（玩家選擇）
        Audience  // 觀眾事件（Twitch 投票）
    }
}
