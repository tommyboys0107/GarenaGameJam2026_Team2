# Design Document

## Overview

本設計文件描述角色選擇頁面重新設計的技術架構，採用 UI Toolkit (UXML + USS) 實作兩步驟流程（選角色 → 選目標），並使用 AudioSource 管理音訊。整體架構遵循 MonoBehaviour Controller + UI Toolkit View 的模式，資料層讀取 CSV 表格。

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────┐
│                  CharacterSelect Scene                    │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  ┌──────────────────────────────────────────────────┐   │
│  │         CharacterSelectController (MonoBehaviour)  │   │
│  │  - 管理整體流程狀態機                              │   │
│  │  - 初始化 UI 綁定                                  │   │
│  │  - 處理音訊播放                                    │   │
│  └──────────────┬───────────────────────┬────────────┘   │
│                 │                       │                 │
│  ┌──────────────▼──────┐  ┌────────────▼─────────────┐  │
│  │  CharacterSelectView │  │    GoalSelectView         │  │
│  │  (VisualElement)     │  │    (VisualElement)        │  │
│  │  - 6 Character Cards │  │    - 6 Goal Options       │  │
│  │  - Tooltip           │  │    - Key Hints            │  │
│  │  - Title + BG        │  │    - Slide-in animation   │  │
│  └──────────────────────┘  └──────────────────────────┘  │
│                                                          │
│  ┌──────────────────────────────────────────────────┐   │
│  │              Fade_Overlay (VisualElement)          │   │
│  └──────────────────────────────────────────────────┘   │
│                                                          │
│  ┌──────────────────────────────────────────────────┐   │
│  │            AudioManager (3x AudioSource)           │   │
│  └──────────────────────────────────────────────────┘   │
│                                                          │
├─────────────────────────────────────────────────────────┤
│  Data Layer: CharacterDataManager, CSVLoader, GameData   │
└─────────────────────────────────────────────────────────┘
```

### State Machine

```
[SelectingCharacter] ──(click card)──→ [SelectingGoal] ──(press key)──→ [TransitioningOut] ──(fade done)──→ [LoadGame]
```

## Components and Interfaces

### CharacterSelectController

主控制器 MonoBehaviour，掛在場景中的 GameObject 上。

```csharp
public class CharacterSelectController : MonoBehaviour
{
    // === Serialized Fields ===
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private AudioClip clickSfxClip;

    // === State ===
    private enum State { SelectingCharacter, SelectingGoal, TransitioningOut }
    private State _currentState;
    private int _selectedCharIndex;

    // === Public Interface ===
    private void Start();   // 初始化資料、UI、音訊
    private void Update();  // 偵測目標按鍵輸入

    // === Character Page ===
    private void SetupCharacterPage();
    private void OnCharacterHoverEnter(MouseEnterEvent evt, int index);
    private void OnCharacterHoverLeave(MouseLeaveEvent evt);
    private void OnCharacterClick(ClickEvent evt, int index);

    // === Goal Page ===
    private void ShowGoalPage();
    private void PollGoalKeyboard();
    private void SelectGoal(int index);

    // === Audio ===
    private void PlayClickSfx();
    private void PlayCharacterVoice(string characterId);
    private void FadeBgmAndLoadGame();

    // === Transition ===
    private void TriggerFadeOut();
}
```

### UI Document Structure

共用一個 UIDocument，包含兩個頁面容器和共用元素：

| Element Name | Type | 用途 |
|---|---|---|
| `character-page` | VisualElement | 角色選擇頁面容器 |
| `goal-page` | VisualElement | 目標選擇頁面容器 |
| `tooltip` | VisualElement | 角色描述 tooltip |
| `fade-overlay` | VisualElement | 全螢幕黑色轉場覆蓋層 |
| `character-grid` | VisualElement | 2×3 角色卡片網格 |
| `goal-grid` | VisualElement | 2×3 目標選項網格 |

### Audio Interface

| AudioSource | 功能 | 設定 |
|---|---|---|
| bgmSource | BGM 循環播放 + fade out | loop=true, playOnAwake=false |
| sfxSource | 點擊音效 (PlayOneShot) | loop=false, playOnAwake=false |
| voiceSource | 角色語音 (Play/Stop) | loop=false, playOnAwake=false |

### Scene GameObject Layout

```
GameObject: "CharSelectManager"
├── CharacterSelectController
├── UIDocument (source: CharacterSelect.uxml, PanelSettings)
├── AudioSource [BGM] (loop=true)
├── AudioSource [SFX]
└── AudioSource [Voice]
```

## Data Models

### Existing Models (不需修改)

```csharp
// Core.CharacterInfo (CharacterDataManager.cs)
public class CharacterInfo
{
    public string Id;          // C01, C02, ...
    public string Name;        // 角色名稱
    public string Nickname;    // 外號
    public string Description; // 角色描述
}

// Data.GoalInfo (GameDataModels.cs)
public class GoalInfo
{
    public string id;         // G01, G02, ...
    public string stockCode;  // NARC, LOCK, BYTE
    public int targetPercent; // 40 or -40
}
```

### Modified Models

```csharp
// Core.GameData - 新增 SelectedGoalIndex
public static class GameData
{
    public static int SelectedCharacterIndex { get; set; } = 0;
    public static int SelectedGoalIndex { get; set; } = 0;  // 新增

    public static void Reset()
    {
        SelectedCharacterIndex = 0;
        SelectedGoalIndex = 0;  // 新增
    }
}
```

### Data Loading Flow

```
Start() →
  CharacterDataManager.LoadData() → List<CharacterInfo> (6 entries)
  CSVLoader.LoadGoals()            → List<GoalInfo> (6 entries)
```

### Audio Asset Naming Convention

| 類型 | 路徑規則 | 範例 |
|---|---|---|
| BGM | `Sounds/BGM/CharacterSelectUI` | 固定檔名 |
| SFX | `Sounds/Sfx/a_button_click_sound_#2-1781354692585` | 固定檔名 |
| Voice | `Sounds/NarratorVoice/{CharacterId}_Hello` | C01_Hello, C02_Hello... |

語音使用 `Resources.Load<AudioClip>("NarratorVoice/{id}_Hello")` 動態載入。

## Error Handling

| 情況 | 處理方式 |
|------|----------|
| CharacterDataManager 載入失敗 | `Debug.LogWarning`，顯示 placeholder cards，流程繼續 |
| CSVLoader.LoadGoals() 回傳空 | `Debug.LogWarning`，Goal page 顯示「目標資料不可用」錯誤訊息 |
| 角色圖片 Resources.Load 回傳 null | 使用預設灰色背景替代 |
| 語音檔 Resources.Load 回傳 null | 跳過播放，`Debug.LogWarning`，不阻斷流程 |
| BGM 檔不存在或 AudioSource 為 null | `Debug.LogWarning`，允許無音樂繼續流程 |
| 多次快速點擊角色卡 | 狀態機保護：SelectingGoal 狀態下忽略角色點擊 |
| 按鍵重複按壓 | TransitioningOut 狀態下忽略所有鍵盤輸入 |

## Correctness Properties

### Property 1: 單一選取
任何時刻只有一個角色卡片處於 selected 狀態。
**Validates: Requirement 3.1, 3.2**

### Property 2: 輸入隔離
目標頁面滑入動畫完成前，鍵盤輸入不被處理。Fade out 開始後，所有 UI 互動被禁用。
**Validates: Requirement 3.6, 7.3**

### Property 3: BGM 連續性
BGM 在角色選擇和目標選擇兩個階段持續播放不中斷。
**Validates: Requirement 8.2**

### Property 4: 資料一致性
GameData.SelectedCharacterIndex 和 SelectedGoalIndex 在進入 Game 場景前已被正確設定。
**Validates: Requirement 3.4, 7.1**

## Testing Strategy

- **手動測試**：在 Unity Editor 中 Play CharacterSelect 場景，驗證完整流程
- **視覺驗證**：確認 hover tooltip 位置、選取高亮、滑入動畫、fade out 效果
- **音訊驗證**：確認 BGM 循環、點擊音效、角色語音同時播放、BGM fade out
- **邊界情況**：快速連點、連續按鍵、缺失資源檔案時的 graceful degradation
- **資料驗證**：確認 GameData 正確儲存選擇結果並帶入 Game 場景

## Alternatives Considered

| 方案 | 優點 | 缺點 | 決定 |
|------|------|------|------|
| uGUI (Canvas) | 團隊熟悉 | 不符需求規格 | ❌ 不採用 |
| UI Toolkit | 需求指定、現代化、CSS-like 樣式 | 學習曲線略高 | ✅ 採用 |
| DOTween 做所有動畫 | 精確控制 | 與 USS transition 重疊 | 備案，優先用 USS transition |
| 分離兩個 UIDocument | 各自獨立 | 多餘複雜度、共享狀態困難 | ❌ 共用一個 UIDocument |
| SceneLoader 切場景做目標選擇 | 各自獨立場景 | 載入延遲、BGM 中斷 | ❌ 同場景內切換 |
