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

        [Header("時間設定")]
        [Tooltip("每個事件 slot 的持續時間（秒）")]
        [SerializeField] private float slotDuration = 10f;

        [Tooltip("玩家選擇的時間限制（秒），超時自動選第一個")]
        [SerializeField] private float playerChoiceTimeout = 8f;

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

        // 玩家事件暫存
        private float _playerTimer;
        private ChoiceInfo[] _currentTraderChoices;
        private EventInfo _currentTraderEvent;

        // 觀眾事件暫存
        private AudienceEventInfo[] _currentAudienceOptions;

        private void Awake()
        {
            LoadCSVData();
            BuildSchedule();
        }

        private void OnEnable()
        {
            if (voteManager != null)
                voteManager.OnVoteComplete += HandleAudienceVoteComplete;
        }

        private void OnDisable()
        {
            if (voteManager != null)
                voteManager.OnVoteComplete -= HandleAudienceVoteComplete;
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

                    // 更新 TV UI 的倒數
                    float remaining = playerChoiceTimeout - _playerTimer;
                    if (tvChoiceUI != null)
                        tvChoiceUI.UpdateTimer(remaining);

                    // 偵測鍵盤輸入 1/2/3 (New Input System)
                    var kb = Keyboard.current;
                    if (kb != null)
                    {
                        if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
                            SelectTraderChoice(0);
                        else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
                            SelectTraderChoice(1);
                        else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame)
                            SelectTraderChoice(2);
                    }

                    // 超時自動選第一個
                    if (_playerTimer >= playerChoiceTimeout)
                    {
                        SelectTraderChoice(0);
                    }
                }

                // Slot 結束，進入下一個
                if (_slotTimer >= slotDuration)
                {
                    EndCurrentSlot();
                    AdvanceToNextSlot();
                }
            }
            else
            {
                // 遊戲開始後啟動第一個 slot
                AdvanceToNextSlot();
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

            Debug.Log($"[GameFlow] 玩家選擇: {chosen.name} → 影響: {FormatEffects(chosen.effects)}");

            // 顯示結果
            if (tvChoiceUI != null)
                tvChoiceUI.ShowResult(choiceIndex, chosen.resultText);

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
                Debug.Log("[GameFlow] 所有事件 slot 結束");
                return;
            }

            CurrentSlotType = _schedule[CurrentSlotIndex];
            _slotTimer = 0f;
            _slotActive = true;

            Debug.Log($"[GameFlow] Slot {CurrentSlotIndex + 1}/{_schedule.Length} 開始 — 類型: {CurrentSlotType}");
            OnSlotStarted?.Invoke(CurrentSlotIndex, CurrentSlotType);

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
            string title = eventInfo.name;
            string opt1 = _currentTraderChoices.Length > 0 ? _currentTraderChoices[0].name : "";
            string opt2 = _currentTraderChoices.Length > 1 ? _currentTraderChoices[1].name : "";
            string opt3 = _currentTraderChoices.Length > 2 ? _currentTraderChoices[2].name : "";

            if (tvChoiceUI != null)
                tvChoiceUI.Show(title, opt1, opt2, opt3);

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

            // 顯示結果
            if (tvChoiceUI != null)
                tvChoiceUI.ShowResult(winnerIndex, winner.resultText);

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
