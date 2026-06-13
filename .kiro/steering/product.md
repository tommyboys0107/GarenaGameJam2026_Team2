---
inclusion: always
---

# Product

**《黑市交易員》(Black Market Trader)** is a 2D stock-manipulation game built in Unity 6 for Garena Game Jam 2026. Players take on the role of a black market trader, manipulating stock prices through event-driven choices to hit target price goals within a 90-second time limit. Twitch chat integration allows a live audience to vote on disruptive market events.

## Core Gameplay Loop

1. Player selects a character (from 6 underworld archetypes)
2. Player receives a goal: drive a specific stock (NARC, LOCK, or BYTE) up or down by a target percentage
3. Over 90 seconds, story events appear — player picks one of three choices per event
4. Each choice shifts stock prices by fixed amounts (e.g. `[NARC,+20],[BYTE,-8]`)
5. Between player events, Twitch audience votes trigger additional market shifts
6. Game ends when time runs out; success = meeting the goal threshold

## Key Systems

| System | Namespace | Responsibility |
|--------|-----------|----------------|
| Stock Market | `BlackMarketTrader` | Price ticking, trend logic, event-driven price changes |
| Game Flow | `Gameplay` | 90s countdown, sequencing trader events and audience votes |
| Twitch Integration | `Twitch` | IRC connection, chat-based voting (3 options per round) |
| Data Layer | `Data` / `Core` | CSV loading, data models for stocks, characters, events, choices |
| UI | `UI` | Stock display, character select, choice panels, title screen |

## Timing (from TimeData.csv)

- Total game duration: 90 seconds
- Trader events: 6 events × 10 seconds each
- Audience (Twitch) events: 3 events × 10 seconds each
- Trend auto-adjustment: every 5 seconds (initialPrice × trend%)

## Data Architecture

- All game data lives in CSV files under `Assets/Resources/`
- Loaded at runtime via `CSVLoader` / `CSVParser`
- Models defined in `GameDataModels.cs` (namespace `Data`)
- Three stocks: NARC (pharma, $80), LOCK (defense, $100), BYTE (cybersec, $120)

## Scenes

| Scene | Purpose |
|-------|---------|
| Title | Main menu |
| CharacterSelect | Pick one of 6 characters |
| Game | Core gameplay (stock chart + events + voting) |
| StockChartTest | Dev testing for chart rendering |

## Development Priorities

- **Game jam rules apply** — favor working prototypes over polish
- Rapid iteration; keep systems decoupled so teammates can work in parallel
- All user-facing text is in Traditional Chinese (繁體中文)
- English is used for code identifiers, comments may be in Chinese
- Twitch integration is a core differentiator — keep it stable

## Coding Conventions

- Organize scripts by feature folder under `Assets/Scripts/` (Core, Data, Gameplay, StockMarket, Twitch, UI)
- Use C# namespaces matching the folder name (e.g. `namespace Gameplay`, `namespace BlackMarketTrader`)
- MonoBehaviour managers follow the pattern: `[SerializeField]` config → public events → public properties → lifecycle methods
- Data models are plain `[Serializable]` classes in `GameDataModels.cs`
- Use `event Action<T>` for decoupled communication between systems
- CSV is the source of truth for game balance — do not hardcode balance values in scripts
- Prefabs for runtime-instantiated objects go in `Assets/Prefabs/`
