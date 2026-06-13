# Implementation Plan: Character Select Redesign

## Overview

將角色選擇頁面從 uGUI 重寫為 UI Toolkit 實作，包含兩步驟流程（選角色 → 選目標）、音訊系統、動畫轉場。共 10 個任務，按依賴順序排列。

## Tasks

- [x] 1. 擴充 GameData 以支援目標選擇
  - Add `SelectedGoalIndex` property to `Assets/Scripts/Core/GameData.cs`
  - Add reset logic for `SelectedGoalIndex` in `GameData.Reset()`
  - Requirements: R7

- [x] 2. 建立 UI Toolkit 樣式文件 (USS)
  - Create `Assets/UI/CharacterSelect.uss` with all styling classes
  - Include: `.page`, `.page-hidden`, `.page-right`, `.background-image`, `.title`
  - Include: `.grid-2x3`, `.character-card`, `.character-card--selected`
  - Include: `.goal-option`, `.key-hint`, `.tooltip`, `.tooltip.hidden`
  - Include: `.fade-overlay`, `.fade-overlay--active`
  - Ensure goal-option has NO :hover or :active pseudo-class styling
  - Requirements: R9

- [x] 3. 建立 UI Toolkit 佈局文件 (UXML)
  - Create `Assets/UI/CharacterSelect.uxml` with two-page layout structure
  - Include containers: `character-page`, `goal-page`, `tooltip`, `fade-overlay`
  - Include sub-containers: `character-grid`, `goal-grid`, `title`, `goal-title`
  - Link `CharacterSelect.uss` via `<ui:Style>`
  - Requirements: R1, R9

- [x] 4. 建立 CharacterSelectController 腳本 - 基礎結構
  - Create `Assets/Scripts/UI/CharacterSelectController.cs`
  - Implement state machine enum (SelectingCharacter, SelectingGoal, TransitioningOut)
  - Implement `Start()`: call CharacterDataManager.LoadData(), CSVLoader.LoadGoals()
  - Implement `SetupUI()`: query VisualElements from UIDocument root
  - Implement `SetupAudio()`: configure bgmSource (loop, play bgmClip)
  - Requirements: R8, R9, R10

- [x] 5. 實作角色選擇頁面 - 卡片生成與 Hover
  - Dynamically create 6 character-card elements in character-grid
  - Set character Name and Nickname labels on each card
  - Load character image from Resources using CharacterInfo.Id pattern
  - RegisterCallback<MouseEnterEvent>: show tooltip with Description, position to right of card
  - RegisterCallback<MouseLeaveEvent>: hide tooltip
  - Handle missing image (placeholder) and empty Description (no tooltip)
  - Requirements: R1, R2

- [x] 6. 實作角色選擇頁面 - 點擊選取與音效
  - RegisterCallback<ClickEvent> on each character card
  - On click: add `character-card--selected` class, remove from previous selection
  - On click: store index in GameData.SelectedCharacterIndex
  - On click: play click SFX via sfxSource.PlayOneShot(clickSfxClip)
  - On click: load and play character voice (Resources.Load NarratorVoice/{id}_Hello)
  - Handle missing voice clip gracefully (log warning, skip)
  - After selection: set state to SelectingGoal, trigger goal page slide-in
  - Requirements: R3, R4

- [x] 7. 實作目標選擇頁面 - 滑入動畫與選項生成
  - Populate 6 goal-option elements in goal-grid with stockCode, targetPercent
  - Add key hint labels (Q/W/E/A/S/D) below each goal option
  - Show goal-page (display: flex), trigger slide-in by removing `page-right` class (USS transition 0.4s)
  - Handle empty GoalData: display error message
  - Requirements: R5, R6

- [x] 8. 實作目標按鍵選擇與 Fade Out 轉場
  - In Update(), detect Q/W/E/A/S/D via Keyboard.current when state is SelectingGoal
  - On key press: store goal index in GameData.SelectedGoalIndex, play click SFX
  - Set state to TransitioningOut (ignore further input)
  - Trigger fade overlay (add `fade-overlay--active` class, opacity 0→1 over 0.5s)
  - Fade BGM to 0 over 1 second using DOTween (bgmSource.DOFade(0, 1f))
  - After fade completes, call SceneLoader.LoadGame()
  - Requirements: R7, R8

- [x] 9. 設定場景 GameObject 與元件
  - Open CharacterSelect.unity scene
  - Remove old CharacterSelectUI component and uGUI references from CharSelectManager
  - Add UIDocument component, assign CharacterSelect.uxml and PanelSettings
  - Add CharacterSelectController component
  - Ensure 3 AudioSource components (BGM: loop=true; SFX; Voice)
  - Assign audio clips (bgmClip, clickSfxClip) via Inspector/MCP
  - Delete old `Assets/Scripts/UI/CharacterSelectUI.cs`
  - Save scene
  - Requirements: R9, R10

- [x] 10. 整合測試與驗證
  - Play CharacterSelect scene in Unity Editor
  - Verify BGM starts on scene load and loops seamlessly
  - Verify 6 character cards display with correct names/nicknames
  - Verify hovering a card shows tooltip with description
  - Verify clicking a card highlights it, plays click SFX and character voice
  - Verify goal page slides in from right after selection
  - Verify 6 goal options show stockCode, targetPercent, key hints
  - Verify pressing Q/W/E/A/S/D selects goal, plays SFX, triggers fade
  - Verify no visual highlight on goal options when keys pressed
  - Verify BGM fades out during transition
  - Verify Game scene loads after fade completes
  - Requirements: R1-R10

## Task Dependency Graph

```json
{
  "waves": [
    {"tasks": [1, 2, 3]},
    {"tasks": [4]},
    {"tasks": [5]},
    {"tasks": [6]},
    {"tasks": [7]},
    {"tasks": [8]},
    {"tasks": [9]},
    {"tasks": [10]}
  ]
}
```

Tasks 1, 2, 3 can be done in parallel (wave 1). Tasks 4-8 are sequential. Task 9 depends on all code tasks. Task 10 is final verification.

## Notes

- DOTween 已安裝在 `Assets/Plugins/Demigiant/DOTween`，可用於 BGM fade 和備案動畫
- 角色語音命名規則為 `{CharacterId}_Hello.mp3`（如 C01_Hello.mp3），放在 `Resources/NarratorVoice/` 下
- USS transition 支援 `translate` 和 `opacity`，可直接用 class toggle 觸發動畫
- 若 USS transition 在運行時不穩定，可改用 DOTween 操作 `VisualElement.style` 作為備案
