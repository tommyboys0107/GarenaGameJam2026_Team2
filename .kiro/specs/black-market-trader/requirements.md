# 需求文件

## 簡介

黑市交易員是一個回合制競賽遊戲模式，主角（玩家）與觀眾對抗。玩家從角色選擇畫面中選擇一個角色，每個角色有不同的勝利目標（使特定黑市股票漲到指定百分比）。遊戲以回合制進行：玩家先從隨機事件池中選擇觸發一個事件，接著觀眾（MVP 階段以 AI/隨機模擬）再投票選擇一個事件觸發。每個事件同時影響多支股票。玩家必須在一分鐘內達成目標，否則判定失敗。

## 詞彙表

- **Game_System**：黑市交易員遊戲的核心系統，負責管理遊戲流程、回合邏輯與結算
- **Stock_System**：股票管理系統，負責追蹤三支黑市股票的當前價格與漲跌百分比
- **Event_System**：隨機事件系統，負責管理事件池、事件選取與套用事件效果
- **Character_Selection_Screen**：角色選擇畫面，展示可選角色與對應目標
- **Timer_System**：計時系統，負責一分鐘倒數計時與時間到期的結算觸發
- **AI_Voter**：AI 投票模擬器，在 MVP 階段代替真實觀眾進行事件投票
- **Stock**：黑市主題股票，遊戲中共有三支，各有固定初始價格
- **Event**：隨機事件，包含事件描述文字與對多支股票的漲跌影響數值
- **Character**：可選角色，每個角色綁定一個目標股票與目標漲幅百分比
- **Turn**：一個回合，包含玩家選事件與觀眾選事件兩個階段
- **Target_Percentage**：角色的勝利目標漲幅百分比（如 +30%、+50%、+80%）

## 需求

### 需求 1：股票初始化

**使用者故事：** 身為玩家，我希望遊戲開始時有三支黑市主題股票以固定價格呈現，以便我了解初始市場狀態。

#### 驗收條件

1. WHEN 遊戲開始, THE Stock_System SHALL 建立三支具有黑市主題名稱的股票
2. WHEN 遊戲開始, THE Stock_System SHALL 為每支股票設定固定的初始價格
3. THE Stock_System SHALL 以百分比形式追蹤每支股票相對於初始價格的漲跌幅

### 需求 2：角色選擇

**使用者故事：** 身為玩家，我希望從角色選擇畫面中挑選一個角色，每個角色有不同的勝利目標，以便遊戲有策略深度。

#### 驗收條件

1. WHEN 進入角色選擇畫面, THE Character_Selection_Screen SHALL 顯示所有可選角色及其對應的目標資訊（目標股票與目標漲幅百分比）
2. WHEN 玩家選擇一個角色, THE Game_System SHALL 記錄該玩家的勝利目標（目標股票與 Target_Percentage）
3. THE Character_Selection_Screen SHALL 顯示所有存在的目標組合，使觀眾知道有哪些可能的目標
4. THE Game_System SHALL 對觀眾隱藏玩家實際選擇的角色

### 需求 3：回合流程

**使用者故事：** 身為玩家，我希望每回合先從事件池中選擇一個事件觸發，再由觀眾選擇一個事件觸發，以形成對抗的回合制體驗。

#### 驗收條件

1. WHEN 回合開始, THE Event_System SHALL 從事件池中隨機抽取若干事件供玩家選擇
2. WHEN 玩家選擇一個事件, THE Event_System SHALL 立即套用該事件對各支股票的影響
3. WHEN 玩家事件套用完成, THE Event_System SHALL 從事件池中隨機抽取若干事件供觀眾投票選擇
4. WHEN 觀眾投票結束, THE Event_System SHALL 套用得票最高的事件對各支股票的影響
5. WHEN 觀眾事件套用完成, THE Game_System SHALL 進入下一回合

### 需求 4：隨機事件效果

**使用者故事：** 身為玩家，我希望每個隨機事件影響多支股票且附有結果描述文字，以便理解市場變化的原因。

#### 驗收條件

1. THE Event_System SHALL 為每個事件定義對至少兩支股票的漲跌影響數值
2. WHEN 事件被套用, THE Event_System SHALL 顯示該事件的結果描述文字
3. WHEN 事件被套用, THE Stock_System SHALL 根據事件定義的影響數值更新對應股票的當前價格
4. THE Stock_System SHALL 在股票價格更新後即時更新顯示的漲跌百分比

### 需求 5：勝利條件判定

**使用者故事：** 身為玩家，我希望當目標股票漲到指定百分比時遊戲判定我獲勝，以獲得成就感。

#### 驗收條件

1. WHEN 股票價格更新後, THE Game_System SHALL 檢查玩家目標股票的漲幅是否達到或超過 Target_Percentage
2. WHEN 玩家目標股票漲幅達到或超過 Target_Percentage, THE Game_System SHALL 立即結束遊戲並判定玩家獲勝
3. WHEN 玩家獲勝, THE Game_System SHALL 顯示勝利結算畫面

### 需求 6：計時系統

**使用者故事：** 身為玩家，我希望有一分鐘的時間限制，讓遊戲節奏緊湊並增加緊張感。

#### 驗收條件

1. WHEN 遊戲回合開始（第一回合起）, THE Timer_System SHALL 開始六十秒倒數計時
2. THE Timer_System SHALL 在畫面上持續顯示剩餘秒數
3. WHEN 計時器歸零, THE Game_System SHALL 立即結束當前遊戲並進入結算
4. WHEN 計時器歸零且玩家未達成目標, THE Game_System SHALL 判定玩家失敗並顯示失敗結算畫面

### 需求 7：AI 觀眾投票（MVP）

**使用者故事：** 身為開發者，我希望在 MVP 階段以 AI 或隨機邏輯模擬觀眾投票，以便在無需網路整合的情況下完成單機遊戲流程。

#### 驗收條件

1. WHEN 觀眾投票階段開始, THE AI_Voter SHALL 從提供的事件選項中選擇一個事件
2. THE AI_Voter SHALL 在固定延遲時間後回傳投票結果，以模擬真實投票等待時間
3. THE AI_Voter SHALL 提供可替換的介面，以便後續整合真實觀眾投票（Twitch/YouTube）

### 需求 8：股票價格顯示

**使用者故事：** 身為玩家，我希望在遊戲中即時看到每支股票的當前價格與漲跌幅，以便做出決策。

#### 驗收條件

1. THE Stock_System SHALL 在遊戲畫面上顯示三支股票的名稱、當前價格與漲跌百分比
2. WHEN 股票價格變動, THE Stock_System SHALL 即時更新畫面上的價格與漲跌百分比顯示
3. THE Stock_System SHALL 使用顏色區分漲（綠色）與跌（紅色）的狀態

### 需求 9：遊戲流程管理

**使用者故事：** 身為玩家，我希望遊戲有清楚的流程（角色選擇 → 遊戲進行 → 結算），以獲得完整的遊戲體驗。

#### 驗收條件

1. THE Game_System SHALL 依序執行以下流程：角色選擇畫面、遊戲回合進行、結算畫面
2. WHEN 角色選擇完成, THE Game_System SHALL 進入遊戲回合階段並啟動計時器
3. WHEN 遊戲結束（勝利或時間到期）, THE Game_System SHALL 顯示結算畫面並展示最終股票狀態
