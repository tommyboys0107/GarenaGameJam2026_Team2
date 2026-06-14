---
inclusion: always
---

# Product — 《黑市交易員》(Black Market Trader)

A 2D stock-manipulation game for Garena Game Jam 2026. Players manipulate stock prices through event-driven choices to hit target price goals within 90 seconds. Twitch chat integration lets a live audience vote on disruptive market events.

## Core Loop

1. Player selects a character (6 underworld archetypes)
2. Player receives a goal: drive a stock (NARC, LOCK, or BYTE) up or down by a target %
3. Over 90 seconds, 9 event slots play out (6 trader + 3 audience, pattern: `[T,T,A]×3`)
4. Trader events: player picks 1 of 3 choices → each shifts stock prices by fixed amounts
5. Audience events: Twitch chat votes among 3 options → winning option shifts prices
6. Game ends on timeout; success = goal threshold met

## System Architecture

| Folder | Namespace | Responsibility |
|--------|-----------|----------------|
| `Scripts/StockMarket/` | `BlackMarketTrader` | Price ticking, trend levels, event-driven price changes |
| `Scripts/Gameplay/` | `Gameplay` | 90s countdown, slot sequencing (trader vs. audience), choice timeout |
| `Scripts/Twitch/` | `Twitch` | IRC connection, chat-based voting, `TwitchConfig` ScriptableObject |
| `Scripts/Data/` | `Data` | CSV loading (`CSVLoader`), data models (`GameDataModels.cs`) |
| `Scripts/Core/` | `Core` | Scene loading, character data manager, CSV parsing utilities |
| `Scripts/UI/` | `UI` | Stock chart, character select, TV choice panel, title screen |
| `Scripts/Editor/` | — | Google Sheets import tooling (editor-only) |

## Timing Configuration (from TimeData.csv)

- Total game: 90 seconds
- 9 event slots × 10 seconds each
- Player choice timeout: 8 seconds (auto-selects first option)
- Trend auto-adjustment: every 5 seconds (`initialPrice × trend%`)

## Data Layer

- **Source of truth**: CSV files in `Assets/Resources/`
- **Runtime loading**: `CSVLoader` (static methods) + `CSVParser` (generic parser)
- **Models**: Plain `[Serializable]` classes in `Data.GameDataModels`

| CSV File | Model Class | Content |
|----------|-------------|---------|
| `StockData.csv` | `StockInfo` | 3 stocks: NARC ($80), LOCK ($100), BYTE ($120) |
| `CharacterData.csv` | `CharacterInfo` | 6 playable characters |
| `EventData.csv` | `EventInfo` | Trader events with choice IDs |
| `ChooseData.csv` | `ChoiceInfo` | Choices with `StockEffect[]` arrays |
| `AudienceData.csv` | `AudienceEventInfo` | Audience vote options with effects |
| `GoalData.csv` | `GoalInfo` | Target stock + target % per character |
| `TimeData.csv` | `TimeConfig` | Timing parameters |

## Scenes

| Scene | Purpose |
|-------|---------|
| `Title` | Main menu |
| `CharacterSelect` | Pick 1 of 6 characters |
| `Game` | Core gameplay (chart + events + voting) |
| `StockChartTest` | Dev testing for chart rendering |

## Coding Conventions

- **Namespaces match folder names** — `namespace Gameplay`, `namespace BlackMarketTrader`, `namespace Data`
- **Manager pattern**: `[SerializeField]` config fields → `public event Action<T>` → public properties → lifecycle methods (`Awake`, `OnEnable`, `Update`)
- **Decoupled communication**: Use `event Action<T>` between systems; never directly call methods across namespaces when avoidable
- **Data models**: Plain `[Serializable]` classes (no MonoBehaviour); all live in `GameDataModels.cs`
- **No hardcoded balance**: All game balance comes from CSV — stock prices, timing, event effects
- **Stock effects format**: `StockEffect[]` with `{ stockCode, value }` — code is "NARC"/"LOCK"/"BYTE", value is signed int
- **Comments**: Chinese (繁體中文) is fine for `<summary>` docs and inline comments; identifiers are English
- **UI text**: All user-facing strings in Traditional Chinese (繁體中文)
- **Prefabs**: Runtime-instantiated objects go in `Assets/Prefabs/`
- **Input**: New Input System only (keyboard polling via `Keyboard.current`)

## Key Patterns in Codebase

- `StockMarketManager` owns price state and exposes `OnPriceUpdated` / `OnEventTriggered`
- `GameFlowController` owns slot sequencing, broadcasts `OnStockEffectApplied` for any price change (trader or audience)
- `GameManager` controls high-level game state (`IsPlaying`, start/end conditions)
- `TwitchVoteManager` handles vote lifecycle, fires `OnVoteComplete(int winnerIndex)`
- `TVChoiceUI` is the shared choice display panel used by both trader and audience events

## Development Priorities

- **Game jam rules** — favor working prototypes over polish
- Keep systems decoupled so teammates work in parallel
- Twitch integration is the core differentiator — keep it stable
- Rapid iteration; test via `StockChartTest` scene or Play mode in `Game` scene

## MCP 操作規範

- **場景設定一律用 MCP 完成** — 建立腳本後，必須透過 MCP 工具（`manage_components`、`manage_gameobject`、`manage_scene` 等）直接將 component 掛上 GameObject、設定 SerializeField 引用、存檔場景。不要只列出手動步驟讓使用者自己做。
- **完整流程**：建立腳本 → `refresh_unity` 等編譯 → `manage_components(add)` 掛 component → `manage_components(set_property)` 設定引用 → `manage_scene(save)` 存檔
- **物件引用設定**：使用 `set_property` 搭配 `{"path": "Assets/..."}` (asset) 或同場景物件的 instance ID 來綁定引用
- **場景切換**：操作前先確認目前場景（`get_active`），需要時用 `load` 切換到正確場景再操作
