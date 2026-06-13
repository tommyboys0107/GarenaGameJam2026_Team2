# Requirements Document

## Introduction

重新設計角色選擇頁面，將原本的單步驟鍵盤選角流程改為兩步驟互動體驗：第一步以滑鼠選擇角色（含 hover 資訊顯示、選取高亮、音效與語音），第二步以鍵盤選擇目標（含滑入動畫、按鍵提示、fade out 轉場）。整體使用 UI Toolkit 實作，音訊透過 AudioSource 處理，資料來源為遊戲內 CharacterData 與 GoalData 表。

## Glossary

- **Character_Select_Page**: 角色選擇頁面，展示六張角色圖片供玩家以滑鼠互動選擇
- **Goal_Select_Page**: 目標選擇頁面，展示六個目標選項供玩家以鍵盤按鍵選擇
- **Character_Card**: 單一角色的圖片元素，包含角色圖與互動狀態
- **Goal_Option**: 單一目標選項元素，包含目標資訊與按鍵提示
- **BGM_Player**: 負責播放背景音樂的 AudioSource 元件
- **SFX_Player**: 負責播放點擊音效的 AudioSource 元件
- **Voice_Player**: 負責播放角色語音的 AudioSource 元件
- **CharacterDataManager**: 提供角色資料（Id、Name、Nickname、Description）的靜態管理類別
- **CSVLoader**: 負責從 CSV 檔載入 GoalData 的資料讀取工具
- **GameData**: 跨場景傳遞玩家選擇（SelectedCharacterIndex）的靜態類別
- **Fade_Overlay**: 全螢幕黑色覆蓋層，用於 fade out 轉場效果

## Requirements

### Requirement 1: 角色選擇頁面佈局

**User Story:** 身為玩家，我想看到清晰的角色選擇畫面，以便快速瀏覽所有可選角色。

#### Acceptance Criteria

1. WHEN the Character_Select_Page loads, THE Character_Select_Page SHALL display a title containing the text "選擇角色" at the top of the screen
2. WHEN the Character_Select_Page loads, THE Character_Select_Page SHALL display a background image behind all UI elements
3. WHEN the Character_Select_Page loads, THE Character_Select_Page SHALL display six Character_Card elements arranged in a 2-row by 3-column grid layout
4. WHEN the Character_Select_Page loads, THE Character_Select_Page SHALL load character images from the Resources folder using each character's ImageID field from CharacterData
5. WHEN the Character_Select_Page loads, each Character_Card SHALL display the character's Name and Nickname retrieved from CharacterDataManager
6. IF a character image fails to load from Resources, THEN THE Character_Select_Page SHALL display a placeholder image in the corresponding Character_Card

### Requirement 2: 角色 Hover 資訊顯示

**User Story:** 身為玩家，我想在滑鼠移到角色上時看到角色的背景資訊，以便了解角色特性再做選擇。

#### Acceptance Criteria

1. WHEN the mouse pointer enters a Character_Card, THE Character_Select_Page SHALL display the character's Description text in a tooltip element positioned to the right of that Character_Card without exceeding the screen viewport boundaries
2. WHEN the mouse pointer leaves a Character_Card, THE Character_Select_Page SHALL hide the Description tooltip within 1 frame
3. THE Character_Select_Page SHALL load each character's Description from CharacterDataManager on page initialization
4. IF CharacterDataManager returns null or an empty Description for a character, THEN THE Character_Select_Page SHALL not display the tooltip when that Character_Card is hovered
5. WHILE the Description tooltip is visible, THE Character_Select_Page SHALL display the Description text with a minimum font size of 14px and a maximum length of 200 characters

### Requirement 3: 角色選取狀態

**User Story:** 身為玩家，我想在點擊角色後看到明確的選取反饋，以便確認我選了哪個角色。

#### Acceptance Criteria

1. WHEN a player clicks on a Character_Card, THE Character_Card SHALL apply a USS class that visually distinguishes it from unselected cards (e.g., border color change or scale increase)
2. WHEN a player clicks on a different Character_Card, THE Character_Select_Page SHALL remove the highlighted USS class from the previously selected Character_Card and apply it to the newly clicked Character_Card
3. IF a player clicks on the already-selected Character_Card, THEN THE Character_Select_Page SHALL take no additional action and maintain the current highlighted state
4. WHEN a player clicks on a Character_Card, THE Character_Select_Page SHALL store the clicked card's index (0–5, mapping to CharacterDataManager's character list) in GameData.SelectedCharacterIndex
5. WHEN a Character_Card is selected, THE Character_Select_Page SHALL activate the Goal_Select_Page slide-in transition within 500 milliseconds of the click
6. WHILE the Goal_Select_Page transition is in progress, THE Character_Select_Page SHALL ignore all further click events on Character_Card elements

### Requirement 4: 角色選取音效

**User Story:** 身為玩家，我想在選擇角色時聽到音效回饋，以便感受互動的即時性。

#### Acceptance Criteria

1. WHEN a player clicks on a Character_Card, THE SFX_Player SHALL immediately play the button click sound effect (Assets/Sounds/Sfx/a_button_click_sound_#2-1781354692585.mp3) as a one-shot audio clip without interrupting other audio channels
2. WHEN a player clicks on a Character_Card, THE Voice_Player SHALL play the corresponding character voice audio file using the naming pattern {characterId}_Hello.mp3 (where characterId is the CharacterInfo.Id value, e.g. "C01_Hello.mp3") concurrently with the button click sound effect
3. IF the character voice audio file for the selected character is not found at the expected path (Assets/Sounds/NarratorVoice/{characterId}_Hello.mp3), THEN THE Voice_Player SHALL skip voice playback without blocking the character selection flow and without playing any fallback audio

### Requirement 5: 目標選擇頁面進場動畫

**User Story:** 身為玩家，我想看到目標頁面流暢地滑入，以便感受連貫的流程體驗。

#### Acceptance Criteria

1. WHEN the Goal_Select_Page is activated, THE Goal_Select_Page SHALL slide in from the right edge of the screen to the center over a duration of 0.4 seconds using an Ease.OutQuad easing curve
2. WHEN the Goal_Select_Page slide-in animation completes, THE Goal_Select_Page SHALL display six Goal_Option elements, each showing its stockCode and targetPercent
3. WHEN the Goal_Select_Page is activated, THE Goal_Select_Page SHALL load goal data from CSVLoader.LoadGoals() before the slide-in animation begins
4. IF CSVLoader.LoadGoals() returns an empty list or fails to load, THEN THE Goal_Select_Page SHALL display an error message indicating that goal data is unavailable

### Requirement 6: 目標選項按鍵提示

**User Story:** 身為玩家，我想看到每個目標對應的按鍵提示，以便知道要按哪個鍵來選擇。

#### Acceptance Criteria

1. THE Goal_Select_Page SHALL display the mapped key letter (Q, W, E, A, S, or D) as a text label positioned below each corresponding Goal_Option, arranged in a 2-row × 3-column grid where Q/W/E map to the top row (left to right) and A/S/D map to the bottom row (left to right)
2. THE Goal_Select_Page SHALL map keyboard keys Q, W, E, A, S, D to the six Goal_Option elements in index order (Q → Goal 1, W → Goal 2, E → Goal 3, A → Goal 4, S → Goal 5, D → Goal 6)
3. WHEN the player presses one of the six mapped keys (Q/W/E/A/S/D) on the Goal_Select_Page, THE Goal_Select_Page SHALL select the corresponding Goal_Option and register that goal as the player's chosen goal for the game session

### Requirement 7: 目標選取與轉場

**User Story:** 身為玩家，我想按下按鍵後順暢地進入遊戲，以便快速開始遊玩。

#### Acceptance Criteria

1. WHEN a player presses a mapped key on the Goal_Select_Page, THE Goal_Select_Page SHALL store the selected goal index in GameData and trigger a fade out transition to black using the Fade_Overlay with a duration of 0.5 seconds (opacity from 0 to 1)
2. WHEN the fade out transition completes, THE Goal_Select_Page SHALL load the Game scene via SceneLoader.LoadGame()
3. WHEN a player presses a mapped key on the Goal_Select_Page, THE Goal_Select_Page SHALL disable all further key input so that additional key presses during the fade out are ignored
4. WHEN a player presses a mapped key on the Goal_Select_Page, THE mapped key SHALL NOT display any visual highlight or press effect on the corresponding Goal_Option element
5. WHEN a player presses a mapped key on the Goal_Select_Page, THE SFX_Player SHALL play the button click sound effect via AudioSource.PlayOneShot

### Requirement 8: 背景音樂播放

**User Story:** 身為玩家，我想在整個角色選擇流程中聽到背景音樂，以便沉浸在遊戲氛圍中。

#### Acceptance Criteria

1. WHEN the CharacterSelect scene loads, THE BGM_Player SHALL start playing the background music file (CharacterSelectUI.mp3) at full volume (1.0) with looping enabled
2. WHILE the player is on the Character_Select_Page or Goal_Select_Page, THE BGM_Player SHALL continue playing the background music in a seamless loop with no audible gap between loop iterations
3. WHEN the scene transition to the Game scene is triggered, THE BGM_Player SHALL fade the background music volume from the current level to zero over a duration of 1 second
4. IF the background music audio file fails to load or the AudioSource reference is missing, THEN THE BGM_Player SHALL log a warning and allow the scene flow to continue without music

### Requirement 9: UI Toolkit 實作

**User Story:** 身為開發者，我想使用 UI Toolkit 來製作此頁面，以便維持技術一致性且方便後續維護。

#### Acceptance Criteria

1. THE Character_Select_Page SHALL be implemented using a .uxml file for layout structure and a .uss file for visual styling (UI Toolkit), with no uGUI components (Canvas, Image, TextMeshProUGUI) used for this page's elements
2. THE Goal_Select_Page SHALL be implemented using a .uxml file for layout structure and a .uss file for visual styling (UI Toolkit), with no uGUI components used for this page's elements
3. THE Character_Select_Page SHALL use a UIDocument component attached to a GameObject in the CharacterSelect scene, with a PanelSettings asset assigned
4. WHEN the player navigates between Character_Select_Page and Goal_Select_Page, THE system SHALL switch visibility by toggling USS display classes or VisualElement.style.display on the root containers within a shared UIDocument, so that only one page is visible at a time
5. THE Goal_Select_Page SHALL share the same UIDocument instance as the Character_Select_Page in the CharacterSelect scene, using separate root VisualElement containers for each page

### Requirement 10: 音訊架構

**User Story:** 身為開發者，我想用 AudioSource 元件來管理所有音訊播放，以便統一音訊控制方式。

#### Acceptance Criteria

1. THE BGM_Player SHALL use a dedicated AudioSource component with loop enabled and playOnAwake disabled, so that background music repeats continuously until explicitly stopped or replaced
2. WHEN a new BGM clip is requested while a BGM clip is already playing, THE BGM_Player SHALL stop the current clip and start playing the new clip from the beginning
3. THE SFX_Player SHALL use a dedicated AudioSource component with loop disabled, playing sound effects via PlayOneShot so that multiple overlapping SFX can sound simultaneously without interrupting each other
4. THE Voice_Player SHALL use a dedicated AudioSource component with loop disabled, so that character voice clips play once to completion
5. WHEN a new voice clip is requested while a voice clip is already playing, THE Voice_Player SHALL stop the current voice clip and play the new voice clip from the beginning
6. IF a null or unassigned AudioClip is passed to any audio player (BGM_Player, SFX_Player, or Voice_Player), THEN THE audio player SHALL skip playback without throwing an exception and log a warning message
7. THE BGM_Player, SFX_Player, and Voice_Player SHALL each expose an independent volume property (range 0.0 to 1.0, default 1.0) so that each audio category can be adjusted without affecting the others
