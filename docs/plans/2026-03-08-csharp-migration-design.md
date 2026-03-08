# Bronzebeard HUD — C#/Avalonia Migration Design

**Date:** 2026-03-08
**Status:** Approved
**Migration from:** bg_treehudder (Rust/egui)

## Context

The Rust/egui overlay (bg_treehudder) works but suffers from:
- Cross-compile pain (WSL → Windows)
- Win32 hacks for transparent/click-through/topmost windows
- `windows` crate version churn
- glow renderer required (wgpu fails on WSL)

C#/Avalonia solves all of these: native overlay support, develop+run on WSL, deploy on Windows.

## Approach: Hybrid Migration

- **Direct port:** LogParser and GameState (proven parsing logic, no redesign needed)
- **Fresh build:** Overlay app with Avalonia/MVVM (replace egui + Win32 hacks)

## Tech Stack

- **.NET 8 LTS** (support until Nov 2026)
- **Avalonia UI** (cross-platform XAML, WPF-like API)
- **xUnit** (testing)
- **VS Code** + C# Dev Kit (IDE)

## Solution Structure

```
BronzebeardHud/
├── BronzebeardHud.sln
├── src/
│   ├── BronzebeardHud.LogParser/        # Class library
│   ├── BronzebeardHud.GameState/        # Class library
│   └── BronzebeardHud.App/              # Avalonia app
├── tests/
│   ├── BronzebeardHud.LogParser.Tests/  # xUnit
│   └── BronzebeardHud.GameState.Tests/  # xUnit
└── docs/
    └── plans/
```

**Dependency flow:** GameState ← LogParser ← App

## GameState Library (Direct Port)

### Enums
- `GamePhase` — NotStarted, HeroSelect, Shopping, Combat, GameOver
- `EntityRef` — base class + derived types (GameEntity, ById, ByName, BracketRef)

### Models
- `Entity` — Id, CardId, Tags dictionary, helper methods (IsHero, IsMinion, Zone, Controller)
- `EntityRegistry` — Dictionary<uint, Entity>, CRUD operations, Find(predicate)
- `EntityResolver` — maps EntityRef → numeric ID
- `Minion` — EntityId, CardId, Attack, Health, ZonePos
- `ShopItem` — EntityId, CardId, Attack, Health, ZonePos, Tier, IsSpell
- `PlayerState` — Name, HeroCardId, Health, Armor, TavernTier, Board, Shop, Gold
- `OpponentState` — HeroCardId, Health, TavernTier, LastKnownBoard, LastSeenTurn
- `GameState` — Phase, Turn, Player, Opponents

### Engine
- `GameStateEngine` — same state machine as Rust version
- `Process(LogLine)` — sequential packet processing
- `Snapshot()` — build immutable GameState from current engine state
- `IdentifyLocalPlayer()` — find player without BACON_DUMMY_PLAYER tag

### CardDb
- `bg_cards.tsv` embedded as assembly resource
- Lazy-loaded Dictionary<string, string>
- `CardName(cardId)`, `DisplayName(cardId)`

### Key Game Mechanics (preserved from Rust)
- HP = HEALTH - DAMAGE
- Board vs Shop: both PLAY zone, differentiated by CONTROLLER
- Turn: player entity's TURN tag (not GameEntity)
- Phase: GameEntity STEP tag → phase mapping

## LogParser Library (Direct Port)

### Lexer
- `Lexer.ParseLine(string)` → `LogLine?`
- Compiled Regex patterns (RegexOptions.Compiled)
- Same parsing pipeline: wrapper → entity → packet

### Types
- `LogLine` — Timestamp, Indent, IsGameState, Packet
- `RawPacket` — abstract record with derived types per variant

### Watcher
- `FileSystemWatcher` for session folder detection
- Polling for file content (Power.log append)
- `Channel<WatcherEvent>` for async event delivery
- Same session detection: new Hearthstone_* folder or CREATE_GAME
- Same NTFS file re-open trick

## Overlay App (Fresh Avalonia Build)

### Window
- Avalonia native: Transparent, Topmost, ShowInTaskbar=false
- Click-through: minimal P/Invoke for WS_EX_TRANSPARENT if needed
- No multi-step Win32 hacks

### MVVM Architecture
- `MainWindow.axaml` — transparent borderless window
- `MainViewModel` — exposes GameState properties with INotifyPropertyChanged
- `GameStateService` — background task, engine runner, dispatcher updates
- `ImageCacheService` — async HttpClient + disk cache → Avalonia Bitmap
- `HsWindowService` — P/Invoke for FindWindow/GetWindowRect, position tracking

### UI Panels (XAML)
- Phase-aware visibility via data binding
- `BoardPanel` — ItemsControl, horizontal WrapPanel, card images
- `ShopPanel` — same, visible during Shopping only
- `HeroInfoPanel` — name, HP, armor, tier, gold
- Semi-transparent dark background, white text

## Data Flow

```
LogWatcher (background Task)
  → Channel<WatcherEvent>
    → GameStateService (background Task)
      → Engine.Process() → Snapshot()
        → Dispatcher.Post()
          → MainViewModel (UI thread)
            → INotifyPropertyChanged → UI auto-refresh
```

No shared mutable state. No locks. Unidirectional data flow.

## Image Loading
- `HttpClient` + `async/await` (no manual thread management)
- Disk cache: `~/.bronzebeard-hud/cache/cards/{cardId}.png`
- URL fallback: render URL → orig URL (same as Rust)
- Returns Avalonia `Bitmap` for XAML binding

## Reference
- Rust source: ~/workspace/bg_treehudder
- Power.log format: ~/workspace/bg_treehudder/docs/reference/power-log-format.md
- Card data: ~/workspace/bg_treehudder/crates/game_state/src/data/bg_cards.tsv
