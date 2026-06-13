---
inclusion: auto
---

# Git Commit 規則

## Commit Message 格式

```
[類別] 中文描述
```

## 類別列表

| 類別 | 用途 |
|------|------|
| `[Init]` | 專案初始化 |
| `[Add]` | 新增功能、系統、場景 |
| `[Modify]` | 修改已有功能、設定調整 |
| `[Fix]` | 修復 Bug |
| `[Remove]` | 移除功能或檔案 |
| `[Refactor]` | 重構程式碼（不改變行為） |
| `[Kiro]` | Kiro AI 設定相關（steering、hooks、MCP） |

## Commit 分組原則

- 一個 commit 對應一個邏輯單元（一個系統、一個功能模組）
- 核心資料結構/邏輯 和 UI/顯示層 分開 commit
- 測試用的場景和測試腳本獨立一個 commit
- 不要把不相關的修改混在同一個 commit

## Push 規則

- 永遠推送到 feature branch，不直接推送到 main
- Branch 命名：使用功能名稱（如 `StockMarket`、`PlayerController`）
- 使用 `git push -u origin <branch-name>` 設定 remote tracking
- 推送前確認 working tree clean

## 範例

```
[Add] 股市系統核心資料結構與管理器
[Add] 股市線圖 UI Toolkit 元件與樣式
[Add] 股市線圖測試場景與鍵盤事件觸發腳本
[Fix] 修正趨勢文字顯示為 Unicode escape 的問題
[Modify] 調整線圖佈局配置
[Kiro] 設定 Unity Power 自動啟用與 Steering 檔案載入功能
```

## 編碼注意事項（Windows）

在 Windows CMD/PowerShell 使用中文 commit message 時，需要先設定 UTF-8：

```powershell
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
chcp 65001 | Out-Null
git commit -m "[Add] 你的中文描述"
```
