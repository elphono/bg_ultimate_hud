# Bronzebeard HUD — C#/Avalonia Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Migrate bg_treehudder (Rust/egui) to C#/Avalonia with direct port of game logic and fresh overlay UI.

**Architecture:** Three-project solution — GameState (pure domain logic), LogParser (regex lexer + file watcher), App (Avalonia overlay with MVVM). GameState and LogParser are direct ports of proven Rust code. App is built fresh with Avalonia idioms.

**Tech Stack:** .NET 8 LTS, Avalonia UI, xUnit, System.Text.RegularExpressions, System.Threading.Channels

---

### Task 1: Install .NET 8 SDK and Verify

**Files:** None (environment setup)

**Step 1: Install .NET 8 SDK**

Run:
```bash
wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh
/tmp/dotnet-install.sh --channel 8.0
```

**Step 2: Add dotnet to PATH**

Add to `~/.bashrc`:
```bash
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$PATH:$DOTNET_ROOT
```

Then: `source ~/.bashrc`

**Step 3: Verify installation**

Run: `dotnet --version`
Expected: `8.0.xxx`

**Step 4: Commit**

No commit needed (environment setup only).

---

### Task 2: Scaffold Solution and Projects

**Files:**
- Create: `BronzebeardHud.sln`
- Create: `src/BronzebeardHud.GameState/BronzebeardHud.GameState.csproj`
- Create: `src/BronzebeardHud.LogParser/BronzebeardHud.LogParser.csproj`
- Create: `src/BronzebeardHud.App/BronzebeardHud.App.csproj`
- Create: `tests/BronzebeardHud.GameState.Tests/BronzebeardHud.GameState.Tests.csproj`
- Create: `tests/BronzebeardHud.LogParser.Tests/BronzebeardHud.LogParser.Tests.csproj`
- Create: `.gitignore`

**Step 1: Create solution and projects**

Run from `~/workspace/bg_ultimate_hud`:
```bash
dotnet new sln -n BronzebeardHud
mkdir -p src tests

# Class libraries
dotnet new classlib -n BronzebeardHud.GameState -o src/BronzebeardHud.GameState
dotnet new classlib -n BronzebeardHud.LogParser -o src/BronzebeardHud.LogParser

# Avalonia app
dotnet new install Avalonia.Templates
dotnet new avalonia.app -n BronzebeardHud.App -o src/BronzebeardHud.App

# Test projects
dotnet new xunit -n BronzebeardHud.GameState.Tests -o tests/BronzebeardHud.GameState.Tests
dotnet new xunit -n BronzebeardHud.LogParser.Tests -o tests/BronzebeardHud.LogParser.Tests

# Add projects to solution
dotnet sln add src/BronzebeardHud.GameState
dotnet sln add src/BronzebeardHud.LogParser
dotnet sln add src/BronzebeardHud.App
dotnet sln add tests/BronzebeardHud.GameState.Tests
dotnet sln add tests/BronzebeardHud.LogParser.Tests
```

**Step 2: Add project references**

```bash
# LogParser depends on GameState
dotnet add src/BronzebeardHud.LogParser reference src/BronzebeardHud.GameState

# App depends on both
dotnet add src/BronzebeardHud.App reference src/BronzebeardHud.GameState
dotnet add src/BronzebeardHud.App reference src/BronzebeardHud.LogParser

# Test projects reference their targets
dotnet add tests/BronzebeardHud.GameState.Tests reference src/BronzebeardHud.GameState
dotnet add tests/BronzebeardHud.LogParser.Tests reference src/BronzebeardHud.LogParser
dotnet add tests/BronzebeardHud.LogParser.Tests reference src/BronzebeardHud.GameState
```

**Step 3: Create .gitignore**

```gitignore
bin/
obj/
*.user
*.suo
.vs/
*.DotSettings.user
```

**Step 4: Delete placeholder files**

Remove auto-generated `Class1.cs` from class libraries and default files.

**Step 5: Verify build**

Run: `dotnet build`
Expected: Build succeeded with 0 errors.

**Step 6: Commit**

```bash
git add -A
git commit -m "chore: scaffold solution with GameState, LogParser, App, and test projects"
```

---

### Task 3: Port GameState — Types (EntityRef, RawPacket, LogLine)

**Files:**
- Create: `src/BronzebeardHud.GameState/EntityRef.cs`
- Create: `src/BronzebeardHud.GameState/RawPacket.cs`
- Create: `src/BronzebeardHud.GameState/LogLine.cs`
- Test: `tests/BronzebeardHud.GameState.Tests/EntityRefTests.cs`

**Rust reference:** `crates/game_state/src/types.rs`

**Step 1: Write failing tests for EntityRef**

```csharp
// tests/BronzebeardHud.GameState.Tests/EntityRefTests.cs
using BronzebeardHud.GameState;

namespace BronzebeardHud.GameState.Tests;

public class EntityRefTests
{
    [Fact]
    public void GameEntity_Equals_GameEntity()
    {
        var a = EntityRef.GameEntity();
        var b = EntityRef.GameEntity();
        Assert.Equal(a, b);
    }

    [Fact]
    public void ById_Stores_Id()
    {
        var e = EntityRef.ById(42);
        Assert.Equal(EntityRefKind.Id, e.Kind);
        Assert.Equal(42u, e.Id);
    }

    [Fact]
    public void BracketRef_Stores_Fields()
    {
        var e = EntityRef.Bracket("Ragnaros the Firelord", 77, "HAND", 1,
            "TB_BaconShop_HERO_11", 3);
        Assert.Equal(EntityRefKind.BracketRef, e.Kind);
        Assert.Equal(77u, e.Id);
        Assert.Equal("TB_BaconShop_HERO_11", e.CardId);
    }

    [Fact]
    public void ByName_Stores_Name()
    {
        var e = EntityRef.ByName("BehEh#1355");
        Assert.Equal(EntityRefKind.Name, e.Kind);
        Assert.Equal("BehEh#1355", e.EntityName);
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/BronzebeardHud.GameState.Tests`
Expected: FAIL — EntityRef not found.

**Step 3: Implement EntityRef**

```csharp
// src/BronzebeardHud.GameState/EntityRef.cs
namespace BronzebeardHud.GameState;

public enum EntityRefKind
{
    GameEntity,
    Id,
    Name,
    BracketRef,
}

public sealed class EntityRef : IEquatable<EntityRef>
{
    public EntityRefKind Kind { get; }
    public uint Id { get; }
    public string EntityName { get; }
    public string Zone { get; }
    public uint ZonePos { get; }
    public string CardId { get; }
    public uint Player { get; }

    private EntityRef(EntityRefKind kind, uint id = 0, string entityName = "",
        string zone = "", uint zonePos = 0, string cardId = "", uint player = 0)
    {
        Kind = kind;
        Id = id;
        EntityName = entityName ?? "";
        Zone = zone ?? "";
        ZonePos = zonePos;
        CardId = cardId ?? "";
        Player = player;
    }

    public static EntityRef GameEntity() => new(EntityRefKind.GameEntity);
    public static EntityRef ById(uint id) => new(EntityRefKind.Id, id: id);
    public static EntityRef ByName(string name) => new(EntityRefKind.Name, entityName: name);
    public static EntityRef Bracket(string entityName, uint id, string zone,
        uint zonePos, string cardId, uint player) =>
        new(EntityRefKind.BracketRef, id, entityName, zone, zonePos, cardId, player);

    public bool Equals(EntityRef? other)
    {
        if (other is null) return false;
        if (Kind != other.Kind) return false;
        return Kind switch
        {
            EntityRefKind.GameEntity => true,
            EntityRefKind.Id => Id == other.Id,
            EntityRefKind.Name => EntityName == other.EntityName,
            EntityRefKind.BracketRef => Id == other.Id && EntityName == other.EntityName
                && Zone == other.Zone && ZonePos == other.ZonePos
                && CardId == other.CardId && Player == other.Player,
            _ => false,
        };
    }

    public override bool Equals(object? obj) => Equals(obj as EntityRef);
    public override int GetHashCode() => HashCode.Combine(Kind, Id, EntityName);
}
```

**Step 4: Implement RawPacket**

```csharp
// src/BronzebeardHud.GameState/RawPacket.cs
namespace BronzebeardHud.GameState;

public abstract record RawPacket
{
    public sealed record CreateGame : RawPacket;
    public sealed record GameEntity(uint EntityId) : RawPacket;
    public sealed record PlayerEntity(uint EntityId, uint PlayerId) : RawPacket;
    public sealed record TagValue(string Tag, string Value) : RawPacket;
    public sealed record FullEntityCreate(uint Id, string CardId) : RawPacket;
    public sealed record FullEntityUpdate(EntityRef Entity, string CardId) : RawPacket;
    public sealed record ShowEntity(EntityRef Entity, string CardId) : RawPacket;
    public sealed record HideEntity(EntityRef Entity, string Tag, string Value) : RawPacket;
    public sealed record TagChange(EntityRef Entity, string Tag, string Value, bool DefChange) : RawPacket;
    public sealed record ChangeEntity(EntityRef Entity, string CardId) : RawPacket;
    public sealed record BlockStart(string BlockType, EntityRef Entity, EntityRef Target) : RawPacket;
    public sealed record BlockEnd : RawPacket;
    public sealed record MetaData(string Meta, string Data, uint InfoCount) : RawPacket;
    public sealed record MetaDataInfo(uint Index, EntityRef Entity) : RawPacket;
    public sealed record PlayerName(uint PlayerId, string Name) : RawPacket;
}
```

**Step 5: Implement LogLine**

```csharp
// src/BronzebeardHud.GameState/LogLine.cs
namespace BronzebeardHud.GameState;

public sealed class LogLine
{
    public string Timestamp { get; init; } = "";
    public uint Indent { get; init; }
    public bool IsGameState { get; init; }
    public RawPacket? Packet { get; init; }
}
```

**Step 6: Run tests**

Run: `dotnet test tests/BronzebeardHud.GameState.Tests`
Expected: All tests PASS.

**Step 7: Commit**

```bash
git add -A
git commit -m "feat(game-state): port EntityRef, RawPacket, and LogLine types"
```

---

### Task 4: Port GameState — GamePhase

**Files:**
- Create: `src/BronzebeardHud.GameState/GamePhase.cs`
- Test: `tests/BronzebeardHud.GameState.Tests/GamePhaseTests.cs`

**Rust reference:** `crates/game_state/src/phase.rs`

**Step 1: Write failing tests**

```csharp
// tests/BronzebeardHud.GameState.Tests/GamePhaseTests.cs
namespace BronzebeardHud.GameState.Tests;

public class GamePhaseTests
{
    [Fact]
    public void FromStep_BeginMulligan_ReturnsHeroSelect()
    {
        Assert.Equal(GamePhase.HeroSelect, GamePhaseHelper.FromStep("BEGIN_MULLIGAN"));
    }

    [Fact]
    public void FromStep_MainReady_ReturnsShopping()
    {
        Assert.Equal(GamePhase.Shopping, GamePhaseHelper.FromStep("MAIN_READY"));
    }

    [Fact]
    public void FromStep_MainStartTriggers_ReturnsCombat()
    {
        Assert.Equal(GamePhase.Combat, GamePhaseHelper.FromStep("MAIN_START_TRIGGERS"));
    }

    [Fact]
    public void FromStep_FinalGameover_ReturnsGameOver()
    {
        Assert.Equal(GamePhase.GameOver, GamePhaseHelper.FromStep("FINAL_GAMEOVER"));
    }

    [Fact]
    public void FromStep_Unknown_ReturnsNull()
    {
        Assert.Null(GamePhaseHelper.FromStep("MAIN_CLEANUP"));
        Assert.Null(GamePhaseHelper.FromStep("MAIN_NEXT"));
    }
}
```

**Step 2: Run tests — verify they fail**

Run: `dotnet test tests/BronzebeardHud.GameState.Tests`

**Step 3: Implement GamePhase**

```csharp
// src/BronzebeardHud.GameState/GamePhase.cs
namespace BronzebeardHud.GameState;

public enum GamePhase
{
    NotStarted,
    HeroSelect,
    Shopping,
    Combat,
    GameOver,
}

public static class GamePhaseHelper
{
    public static GamePhase? FromStep(string stepValue) => stepValue switch
    {
        "BEGIN_MULLIGAN" => GamePhase.HeroSelect,
        "MAIN_READY" => GamePhase.Shopping,
        "MAIN_START_TRIGGERS" => GamePhase.Combat,
        "FINAL_GAMEOVER" => GamePhase.GameOver,
        _ => null,
    };
}
```

**Step 4: Run tests**

Run: `dotnet test tests/BronzebeardHud.GameState.Tests`
Expected: All PASS.

**Step 5: Commit**

```bash
git add -A
git commit -m "feat(game-state): port GamePhase enum with step-to-phase mapping"
```

---

### Task 5: Port GameState — Entity and EntityRegistry

**Files:**
- Create: `src/BronzebeardHud.GameState/Entity.cs`
- Create: `src/BronzebeardHud.GameState/EntityRegistry.cs`
- Test: `tests/BronzebeardHud.GameState.Tests/EntityTests.cs`
- Test: `tests/BronzebeardHud.GameState.Tests/EntityRegistryTests.cs`

**Rust reference:** `crates/game_state/src/entity.rs`

**Step 1: Write failing tests for Entity**

```csharp
// tests/BronzebeardHud.GameState.Tests/EntityTests.cs
namespace BronzebeardHud.GameState.Tests;

public class EntityTests
{
    [Fact]
    public void New_SetsIdAndCardId()
    {
        var e = new Entity(42, "EX1_506");
        Assert.Equal(42u, e.Id);
        Assert.Equal("EX1_506", e.CardId);
        Assert.Empty(e.Tags);
    }

    [Fact]
    public void Tag_ReturnsValue()
    {
        var e = new Entity(1, "HERO");
        e.SetTag("CARDTYPE", "HERO");
        Assert.Equal("HERO", e.Tag("CARDTYPE"));
        Assert.Null(e.Tag("MISSING"));
    }

    [Fact]
    public void TagInt_ParsesOrDefaultsZero()
    {
        var e = new Entity(1, "");
        e.SetTag("HEALTH", "40");
        Assert.Equal(40, e.TagInt("HEALTH"));
        Assert.Equal(0, e.TagInt("MISSING"));
    }

    [Fact]
    public void IsHero_ChecksCardType()
    {
        var e = new Entity(1, "");
        e.SetTag("CARDTYPE", "HERO");
        Assert.True(e.IsHero);
        Assert.False(e.IsMinion);
    }

    [Fact]
    public void IsMinion_ChecksCardType()
    {
        var e = new Entity(1, "");
        e.SetTag("CARDTYPE", "MINION");
        Assert.True(e.IsMinion);
        Assert.False(e.IsHero);
    }

    [Fact]
    public void Zone_ReturnsZoneTag()
    {
        var e = new Entity(1, "");
        e.SetTag("ZONE", "PLAY");
        Assert.Equal("PLAY", e.Zone);
    }

    [Fact]
    public void Controller_ReturnsControllerAsInt()
    {
        var e = new Entity(1, "");
        e.SetTag("CONTROLLER", "7");
        Assert.Equal(7, e.Controller);
    }
}
```

**Step 2: Write failing tests for EntityRegistry**

```csharp
// tests/BronzebeardHud.GameState.Tests/EntityRegistryTests.cs
namespace BronzebeardHud.GameState.Tests;

public class EntityRegistryTests
{
    [Fact]
    public void Create_And_Get()
    {
        var reg = new EntityRegistry();
        reg.Create(7, "TB_BaconShop_HERO_PH");
        var e = reg.Get(7);
        Assert.NotNull(e);
        Assert.Equal("TB_BaconShop_HERO_PH", e!.CardId);
    }

    [Fact]
    public void SetTag_CreatesEntityIfMissing()
    {
        var reg = new EntityRegistry();
        reg.SetTag(99, "ZONE", "PLAY");
        var e = reg.Get(99);
        Assert.NotNull(e);
        Assert.Equal("PLAY", e!.Tag("ZONE"));
    }

    [Fact]
    public void Find_FiltersEntities()
    {
        var reg = new EntityRegistry();
        reg.Create(1, "HERO_A");
        reg.SetTag(1, "CARDTYPE", "HERO");
        reg.Create(2, "MINION_A");
        reg.SetTag(2, "CARDTYPE", "MINION");
        reg.Create(3, "HERO_B");
        reg.SetTag(3, "CARDTYPE", "HERO");

        var heroes = reg.Find(e => e.IsHero);
        Assert.Equal(2, heroes.Count);
    }

    [Fact]
    public void Clear_RemovesAll()
    {
        var reg = new EntityRegistry();
        reg.Create(1, "A");
        reg.Create(2, "B");
        Assert.NotNull(reg.Get(1));
        reg.Clear();
        Assert.Null(reg.Get(1));
    }
}
```

**Step 3: Run tests — verify they fail**

Run: `dotnet test tests/BronzebeardHud.GameState.Tests`

**Step 4: Implement Entity**

```csharp
// src/BronzebeardHud.GameState/Entity.cs
namespace BronzebeardHud.GameState;

public class Entity
{
    public uint Id { get; }
    public string CardId { get; set; }
    public Dictionary<string, string> Tags { get; } = new();

    public Entity(uint id, string cardId)
    {
        Id = id;
        CardId = cardId;
    }

    public string? Tag(string key) => Tags.GetValueOrDefault(key);

    public int TagInt(string key) =>
        Tags.TryGetValue(key, out var v) && int.TryParse(v, out var n) ? n : 0;

    public void SetTag(string key, string value) => Tags[key] = value;

    public bool IsHero => Tag("CARDTYPE") == "HERO";
    public bool IsMinion => Tag("CARDTYPE") == "MINION";
    public string? Zone => Tag("ZONE");
    public int Controller => TagInt("CONTROLLER");
}
```

**Step 5: Implement EntityRegistry**

```csharp
// src/BronzebeardHud.GameState/EntityRegistry.cs
namespace BronzebeardHud.GameState;

public class EntityRegistry
{
    private readonly Dictionary<uint, Entity> _entities = new();

    public void Create(uint id, string cardId) => _entities[id] = new Entity(id, cardId);

    public Entity? Get(uint id) => _entities.GetValueOrDefault(id);

    public void SetTag(uint entityId, string tag, string value)
    {
        if (!_entities.TryGetValue(entityId, out var entity))
        {
            entity = new Entity(entityId, "");
            _entities[entityId] = entity;
        }
        entity.SetTag(tag, value);
    }

    public List<Entity> Find(Func<Entity, bool> predicate) =>
        _entities.Values.Where(predicate).ToList();

    public void Clear() => _entities.Clear();
}
```

**Step 6: Run tests**

Run: `dotnet test tests/BronzebeardHud.GameState.Tests`
Expected: All PASS.

**Step 7: Commit**

```bash
git add -A
git commit -m "feat(game-state): port Entity and EntityRegistry"
```

---

### Task 6: Port GameState — EntityResolver

**Files:**
- Create: `src/BronzebeardHud.GameState/EntityResolver.cs`
- Test: `tests/BronzebeardHud.GameState.Tests/EntityResolverTests.cs`

**Rust reference:** `crates/game_state/src/resolver.rs`

**Step 1: Write failing tests**

```csharp
// tests/BronzebeardHud.GameState.Tests/EntityResolverTests.cs
namespace BronzebeardHud.GameState.Tests;

public class EntityResolverTests
{
    [Fact]
    public void Resolve_GameEntity()
    {
        var r = new EntityResolver();
        Assert.Null(r.Resolve(EntityRef.GameEntity()));
        r.SetGameEntity(19);
        Assert.Equal(19u, r.Resolve(EntityRef.GameEntity()));
    }

    [Fact]
    public void Resolve_Id()
    {
        var r = new EntityResolver();
        Assert.Equal(42u, r.Resolve(EntityRef.ById(42)));
    }

    [Fact]
    public void Resolve_Name()
    {
        var r = new EntityResolver();
        r.RegisterName("elphono#2437", 20);
        Assert.Equal(20u, r.Resolve(EntityRef.ByName("elphono#2437")));
        Assert.Null(r.Resolve(EntityRef.ByName("unknown")));
    }

    [Fact]
    public void Resolve_BracketRef()
    {
        var r = new EntityResolver();
        var entity = EntityRef.Bracket("Marin", 88, "PLAY", 0, "BG30_HERO_304", 7);
        Assert.Equal(88u, r.Resolve(entity));
    }

    [Fact]
    public void Clear_ResetsAll()
    {
        var r = new EntityResolver();
        r.SetGameEntity(19);
        r.RegisterName("test", 20);
        r.Clear();
        Assert.Null(r.Resolve(EntityRef.GameEntity()));
        Assert.Null(r.Resolve(EntityRef.ByName("test")));
    }
}
```

**Step 2: Run tests — verify they fail**

**Step 3: Implement EntityResolver**

```csharp
// src/BronzebeardHud.GameState/EntityResolver.cs
namespace BronzebeardHud.GameState;

public class EntityResolver
{
    private uint? _gameEntityId;
    private readonly Dictionary<string, uint> _nameToId = new();

    public void SetGameEntity(uint id) => _gameEntityId = id;

    public void RegisterName(string name, uint entityId) => _nameToId[name] = entityId;

    public uint? Resolve(EntityRef entityRef) => entityRef.Kind switch
    {
        EntityRefKind.GameEntity => _gameEntityId,
        EntityRefKind.Id => entityRef.Id,
        EntityRefKind.Name => _nameToId.GetValueOrDefault(entityRef.EntityName),
        EntityRefKind.BracketRef => entityRef.Id,
        _ => null,
    };

    public void Clear()
    {
        _gameEntityId = null;
        _nameToId.Clear();
    }
}
```

**Step 4: Run tests**

Run: `dotnet test tests/BronzebeardHud.GameState.Tests`
Expected: All PASS.

**Step 5: Commit**

```bash
git add -A
git commit -m "feat(game-state): port EntityResolver"
```

---

### Task 7: Port GameState — State Models (Minion, ShopItem, PlayerState, OpponentState, GameState)

**Files:**
- Create: `src/BronzebeardHud.GameState/Models.cs`
- Test: `tests/BronzebeardHud.GameState.Tests/ModelsTests.cs`

**Rust reference:** `crates/game_state/src/state.rs`

**Step 1: Write failing tests**

```csharp
// tests/BronzebeardHud.GameState.Tests/ModelsTests.cs
namespace BronzebeardHud.GameState.Tests;

public class ModelsTests
{
    [Fact]
    public void GameStateSnapshot_DefaultValues()
    {
        var gs = new GameStateSnapshot();
        Assert.Equal(GamePhase.NotStarted, gs.Phase);
        Assert.Equal(0u, gs.Turn);
        Assert.Empty(gs.Opponents);
        Assert.Equal(0, gs.Player.TavernTier);
    }

    [Fact]
    public void Minion_StoresFields()
    {
        var m = new Minion { EntityId = 100, CardId = "EX1_506", Attack = 2, Health = 1, ZonePos = 1 };
        Assert.Equal(2, m.Attack);
        Assert.Equal(1, m.Health);
    }

    [Fact]
    public void PlayerState_DefaultsEmpty()
    {
        var ps = new PlayerState();
        Assert.Equal(0, ps.TavernTier);
        Assert.Empty(ps.Board);
        Assert.Equal(0, ps.Health);
    }
}
```

**Step 2: Run tests — verify they fail**

**Step 3: Implement models**

```csharp
// src/BronzebeardHud.GameState/Models.cs
namespace BronzebeardHud.GameState;

public class Minion
{
    public uint EntityId { get; init; }
    public string CardId { get; init; } = "";
    public int Attack { get; init; }
    public int Health { get; init; }
    public uint ZonePos { get; init; }
}

public class ShopItem
{
    public uint EntityId { get; init; }
    public string CardId { get; init; } = "";
    public int Attack { get; init; }
    public int Health { get; init; }
    public uint ZonePos { get; init; }
    public byte Tier { get; init; }
    public bool IsSpell { get; init; }
}

public class PlayerState
{
    public uint EntityId { get; init; }
    public uint PlayerId { get; init; }
    public string Name { get; init; } = "";
    public string HeroCardId { get; init; } = "";
    public uint HeroEntityId { get; init; }
    public int Health { get; init; }
    public int Armor { get; init; }
    public int TavernTier { get; init; }
    public List<Minion> Board { get; init; } = new();
    public List<ShopItem> Shop { get; init; } = new();
    public int Gold { get; init; }
}

public class OpponentState
{
    public uint EntityId { get; init; }
    public uint PlayerId { get; init; }
    public string HeroCardId { get; init; } = "";
    public uint HeroEntityId { get; init; }
    public int Health { get; init; }
    public int TavernTier { get; init; }
    public List<Minion> LastKnownBoard { get; init; } = new();
    public uint LastSeenTurn { get; init; }
}

public class GameStateSnapshot
{
    public GamePhase Phase { get; init; } = GamePhase.NotStarted;
    public uint Turn { get; init; }
    public PlayerState Player { get; init; } = new();
    public List<OpponentState> Opponents { get; init; } = new();
    public uint GameEntityId { get; init; }
}
```

**Step 4: Run tests**

Run: `dotnet test tests/BronzebeardHud.GameState.Tests`
Expected: All PASS.

**Step 5: Commit**

```bash
git add -A
git commit -m "feat(game-state): port state models (Minion, PlayerState, GameStateSnapshot)"
```

---

### Task 8: Port GameState — CardDb

**Files:**
- Copy: `bg_cards.tsv` from Rust project to `src/BronzebeardHud.GameState/Data/bg_cards.tsv`
- Create: `src/BronzebeardHud.GameState/CardDb.cs`
- Test: `tests/BronzebeardHud.GameState.Tests/CardDbTests.cs`

**Rust reference:** `crates/game_state/src/card_db.rs`

**Step 1: Copy card data**

```bash
mkdir -p ~/workspace/bg_ultimate_hud/src/BronzebeardHud.GameState/Data
cp ~/workspace/bg_treehudder/crates/game_state/data/bg_cards.tsv \
   ~/workspace/bg_ultimate_hud/src/BronzebeardHud.GameState/Data/
```

**Step 2: Mark TSV as embedded resource in .csproj**

Add to `src/BronzebeardHud.GameState/BronzebeardHud.GameState.csproj`:
```xml
<ItemGroup>
  <EmbeddedResource Include="Data/bg_cards.tsv" />
</ItemGroup>
```

**Step 3: Write failing tests**

```csharp
// tests/BronzebeardHud.GameState.Tests/CardDbTests.cs
namespace BronzebeardHud.GameState.Tests;

public class CardDbTests
{
    [Fact]
    public void CardName_KnownHero()
    {
        Assert.Equal("Varden Dawngrasp", CardDb.CardName("BG22_HERO_004"));
    }

    [Fact]
    public void CardName_KnownMinion()
    {
        Assert.Equal("Dune Dweller", CardDb.CardName("BG31_815"));
    }

    [Fact]
    public void CardName_Unknown_ReturnsNull()
    {
        Assert.Null(CardDb.CardName("TOTALLY_FAKE_CARD"));
    }

    [Fact]
    public void DisplayName_Fallback()
    {
        Assert.Equal("Varden Dawngrasp", CardDb.DisplayName("BG22_HERO_004"));
        Assert.Equal("UNKNOWN_CARD", CardDb.DisplayName("UNKNOWN_CARD"));
    }

    [Fact]
    public void Database_HasThousandsOfCards()
    {
        Assert.True(CardDb.Count > 1000);
    }
}
```

**Step 4: Run tests — verify they fail**

**Step 5: Implement CardDb**

```csharp
// src/BronzebeardHud.GameState/CardDb.cs
using System.Reflection;

namespace BronzebeardHud.GameState;

public static class CardDb
{
    private static readonly Lazy<Dictionary<string, string>> _cards = new(LoadCards);

    public static string? CardName(string cardId) =>
        _cards.Value.GetValueOrDefault(cardId);

    public static string DisplayName(string cardId) =>
        CardName(cardId) ?? cardId;

    public static int Count => _cards.Value.Count;

    private static Dictionary<string, string> LoadCards()
    {
        var dict = new Dictionary<string, string>();
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(
            "BronzebeardHud.GameState.Data.bg_cards.tsv");
        if (stream == null) return dict;
        using var reader = new StreamReader(stream);

        while (reader.ReadLine() is { } line)
        {
            var tabIndex = line.IndexOf('\t');
            if (tabIndex > 0)
            {
                dict[line[..tabIndex]] = line[(tabIndex + 1)..];
            }
        }
        return dict;
    }
}
```

**Step 6: Run tests**

Run: `dotnet test tests/BronzebeardHud.GameState.Tests`
Expected: All PASS.

**Step 7: Commit**

```bash
git add -A
git commit -m "feat(game-state): port CardDb with embedded TSV resource"
```

---

### Task 9: Port GameState — GameStateEngine

**Files:**
- Create: `src/BronzebeardHud.GameState/GameStateEngine.cs`
- Test: `tests/BronzebeardHud.GameState.Tests/GameStateEngineTests.cs`

**Rust reference:** `crates/game_state/src/engine.rs`

This is the largest and most critical port. The engine is a direct translation of the Rust state machine.

**Step 1: Write failing tests**

```csharp
// tests/BronzebeardHud.GameState.Tests/GameStateEngineTests.cs
namespace BronzebeardHud.GameState.Tests;

public class GameStateEngineTests
{
    private static LogLine Line(uint indent, RawPacket packet) => new()
    {
        Timestamp = "00:00:00.0000000",
        Indent = indent,
        IsGameState = true,
        Packet = packet,
    };

    private static LogLine Tag(uint indent, string tag, string value) =>
        Line(indent, new RawPacket.TagValue(tag, value));

    private static LogLine TagChange(EntityRef entity, string tag, string value) =>
        Line(0, new RawPacket.TagChange(entity, tag, value, false));

    private static void ProcessAll(GameStateEngine engine, params LogLine[] lines)
    {
        foreach (var l in lines) engine.Process(l);
    }

    [Fact]
    public void CreateGame_SetsGameEntity()
    {
        var engine = new GameStateEngine();
        ProcessAll(engine,
            Line(0, new RawPacket.CreateGame()),
            Line(1, new RawPacket.GameEntity(19)),
            Tag(2, "CARDTYPE", "GAME"),
            Tag(2, "ZONE", "PLAY"));

        var snap = engine.Snapshot();
        Assert.Equal(19u, snap.GameEntityId);
    }

    [Fact]
    public void IdentifyLocalPlayer_SkipsDummy()
    {
        var engine = new GameStateEngine();
        ProcessAll(engine,
            Line(0, new RawPacket.CreateGame()),
            Line(1, new RawPacket.GameEntity(19)),
            Tag(2, "CARDTYPE", "GAME"),
            Line(1, new RawPacket.PlayerEntity(20, 7)),
            Tag(2, "CONTROLLER", "7"),
            Tag(2, "CARDTYPE", "PLAYER"),
            Tag(2, "HERO_ENTITY", "37"),
            Tag(2, "PLAYER_TECH_LEVEL", "1"),
            Line(1, new RawPacket.PlayerEntity(21, 15)),
            Tag(2, "CONTROLLER", "15"),
            Tag(2, "CARDTYPE", "PLAYER"),
            Tag(2, "BACON_DUMMY_PLAYER", "1"));
        engine.IdentifyLocalPlayer();

        var snap = engine.Snapshot();
        Assert.Equal(7u, snap.Player.PlayerId);
    }

    [Fact]
    public void PhaseTransitions()
    {
        var engine = new GameStateEngine();
        ProcessAll(engine,
            Line(0, new RawPacket.CreateGame()),
            Line(1, new RawPacket.GameEntity(19)),
            Tag(2, "CARDTYPE", "GAME"),
            TagChange(EntityRef.GameEntity(), "STEP", "BEGIN_MULLIGAN"));

        Assert.Equal(GamePhase.HeroSelect, engine.Snapshot().Phase);

        engine.Process(TagChange(EntityRef.GameEntity(), "STEP", "MAIN_READY"));
        Assert.Equal(GamePhase.Shopping, engine.Snapshot().Phase);
    }

    [Fact]
    public void TurnTracking()
    {
        var engine = new GameStateEngine();
        ProcessAll(engine,
            Line(0, new RawPacket.CreateGame()),
            Line(1, new RawPacket.GameEntity(19)),
            Tag(2, "CARDTYPE", "GAME"),
            Line(1, new RawPacket.PlayerEntity(20, 7)),
            Tag(2, "CONTROLLER", "7"),
            Tag(2, "CARDTYPE", "PLAYER"),
            Tag(2, "HERO_ENTITY", "37"),
            Tag(2, "PLAYER_TECH_LEVEL", "1"),
            Line(1, new RawPacket.PlayerEntity(21, 15)),
            Tag(2, "CONTROLLER", "15"),
            Tag(2, "CARDTYPE", "PLAYER"),
            Tag(2, "BACON_DUMMY_PLAYER", "1"));
        engine.IdentifyLocalPlayer();

        engine.Process(TagChange(EntityRef.ById(20), "TURN", "3"));
        Assert.Equal(3u, engine.Snapshot().Turn);
    }

    [Fact]
    public void FullEntityCreate_Tracked()
    {
        var engine = new GameStateEngine();
        ProcessAll(engine,
            Line(0, new RawPacket.CreateGame()),
            Line(1, new RawPacket.GameEntity(19)),
            Line(0, new RawPacket.FullEntityCreate(37, "TB_BaconShop_HERO_PH")),
            Tag(1, "CONTROLLER", "7"),
            Tag(1, "CARDTYPE", "HERO"),
            Tag(1, "HEALTH", "30"),
            Tag(1, "ZONE", "PLAY"));

        // Verify entity exists with correct card_id and tags
        // (tested via snapshot when player is set up)
    }

    [Fact]
    public void Snapshot_WithHero()
    {
        var engine = new GameStateEngine();
        ProcessAll(engine,
            Line(0, new RawPacket.CreateGame()),
            Line(1, new RawPacket.GameEntity(19)),
            Tag(2, "CARDTYPE", "GAME"),
            Line(1, new RawPacket.PlayerEntity(20, 7)),
            Tag(2, "CONTROLLER", "7"),
            Tag(2, "CARDTYPE", "PLAYER"),
            Tag(2, "HERO_ENTITY", "37"),
            Tag(2, "PLAYER_TECH_LEVEL", "1"),
            Line(1, new RawPacket.PlayerEntity(21, 15)),
            Tag(2, "CONTROLLER", "15"),
            Tag(2, "CARDTYPE", "PLAYER"),
            Tag(2, "BACON_DUMMY_PLAYER", "1"),
            Line(0, new RawPacket.FullEntityCreate(37, "BG30_HERO_304")),
            Tag(1, "CONTROLLER", "7"),
            Tag(1, "CARDTYPE", "HERO"),
            Tag(1, "HEALTH", "30"),
            Tag(1, "ZONE", "PLAY"));
        engine.IdentifyLocalPlayer();

        var snap = engine.Snapshot();
        Assert.Equal("BG30_HERO_304", snap.Player.HeroCardId);
        Assert.Equal(30, snap.Player.Health);
        Assert.Equal(1, snap.Player.TavernTier);
    }

    [Fact]
    public void TavernUpgrade_TrackedViaPlayerName()
    {
        var engine = new GameStateEngine();
        ProcessAll(engine,
            Line(0, new RawPacket.CreateGame()),
            Line(1, new RawPacket.GameEntity(19)),
            Line(1, new RawPacket.PlayerEntity(20, 7)),
            Tag(2, "CONTROLLER", "7"),
            Tag(2, "CARDTYPE", "PLAYER"),
            Tag(2, "PLAYER_TECH_LEVEL", "1"),
            Tag(2, "HERO_ENTITY", "37"),
            Line(1, new RawPacket.PlayerEntity(21, 15)),
            Tag(2, "BACON_DUMMY_PLAYER", "1"));
        engine.IdentifyLocalPlayer();
        engine.RegisterPlayerName("elphono#2437", 20);

        engine.Process(TagChange(
            EntityRef.ByName("elphono#2437"), "PLAYER_TECH_LEVEL", "3"));

        Assert.Equal(3, engine.Snapshot().Player.TavernTier);
    }
}
```

**Step 2: Run tests — verify they fail**

**Step 3: Implement GameStateEngine**

```csharp
// src/BronzebeardHud.GameState/GameStateEngine.cs
namespace BronzebeardHud.GameState;

public class GameStateEngine
{
    private readonly EntityRegistry _entities = new();
    private readonly EntityResolver _resolver = new();
    private GamePhase _phase = GamePhase.NotStarted;
    private uint _turn;
    private uint _gameEntityId;
    private uint _localPlayerEntityId;
    private uint _localPlayerId;
    private readonly Dictionary<uint, (uint PlayerId, string Name)> _players = new();
    private uint? _activeEntityId;
    private uint _activeEntityIndent;

    public void Process(LogLine line)
    {
        if (line.Packet is null) return;

        // If we have an active entity and current line is deeper, treat TagValue as belonging to it
        if (_activeEntityId is { } activeId)
        {
            if (line.Indent > _activeEntityIndent && line.Packet is RawPacket.TagValue tv)
            {
                _entities.SetTag(activeId, tv.Tag, tv.Value);
                return;
            }
            if (line.Indent <= _activeEntityIndent)
            {
                _activeEntityId = null;
            }
        }

        switch (line.Packet)
        {
            case RawPacket.CreateGame:
                Reset();
                break;

            case RawPacket.GameEntity ge:
                _gameEntityId = ge.EntityId;
                _entities.Create(ge.EntityId, "");
                _resolver.SetGameEntity(ge.EntityId);
                _activeEntityId = ge.EntityId;
                _activeEntityIndent = line.Indent;
                break;

            case RawPacket.PlayerEntity pe:
                _entities.Create(pe.EntityId, "");
                _entities.SetTag(pe.EntityId, "PLAYER_ID", pe.PlayerId.ToString());
                _players[pe.EntityId] = (pe.PlayerId, "");
                _activeEntityId = pe.EntityId;
                _activeEntityIndent = line.Indent;
                break;

            case RawPacket.FullEntityCreate fec:
                _entities.Create(fec.Id, fec.CardId);
                _activeEntityId = fec.Id;
                _activeEntityIndent = line.Indent;
                break;

            case RawPacket.FullEntityUpdate feu:
                if (_resolver.Resolve(feu.Entity) is { } feuId)
                {
                    UpdateCardId(feuId, feu.CardId);
                    _activeEntityId = feuId;
                    _activeEntityIndent = line.Indent;
                }
                break;

            case RawPacket.ShowEntity se:
                if (_resolver.Resolve(se.Entity) is { } seId)
                {
                    UpdateCardId(seId, se.CardId);
                    _activeEntityId = seId;
                    _activeEntityIndent = line.Indent;
                }
                break;

            case RawPacket.ChangeEntity ce:
                if (_resolver.Resolve(ce.Entity) is { } ceId)
                {
                    UpdateCardId(ceId, ce.CardId);
                    _activeEntityId = ceId;
                    _activeEntityIndent = line.Indent;
                }
                break;

            case RawPacket.HideEntity he:
                if (_resolver.Resolve(he.Entity) is { } heId)
                    _entities.SetTag(heId, he.Tag, he.Value);
                break;

            case RawPacket.TagChange tc:
                if (_resolver.Resolve(tc.Entity) is { } tcId)
                {
                    _entities.SetTag(tcId, tc.Tag, tc.Value);

                    if (tcId == _gameEntityId && tc.Tag == "STEP")
                    {
                        if (GamePhaseHelper.FromStep(tc.Value) is { } newPhase)
                            _phase = newPhase;
                    }

                    if (tcId == _localPlayerEntityId && tc.Tag == "TURN"
                        && uint.TryParse(tc.Value, out var t))
                    {
                        _turn = t;
                    }
                }
                break;

            case RawPacket.PlayerName pn:
                var eid = _players.FirstOrDefault(
                    p => p.Value.PlayerId == pn.PlayerId).Key;
                if (eid != 0)
                {
                    _players[eid] = (_players[eid].PlayerId, pn.Name);
                    _resolver.RegisterName(pn.Name, eid);
                }
                break;

            case RawPacket.TagValue:
                // Stray tag-value not associated with active entity — ignore
                break;

            case RawPacket.BlockStart:
            case RawPacket.BlockEnd:
            case RawPacket.MetaData:
            case RawPacket.MetaDataInfo:
                _activeEntityId = null;
                break;
        }
    }

    public void IdentifyLocalPlayer()
    {
        foreach (var (entityId, (playerId, _)) in _players)
        {
            var entity = _entities.Get(entityId);
            if (entity?.Tag("BACON_DUMMY_PLAYER") != "1")
            {
                _localPlayerEntityId = entityId;
                _localPlayerId = playerId;
                return;
            }
        }
    }

    public void RegisterPlayerName(string name, uint entityId)
    {
        _resolver.RegisterName(name, entityId);
        if (_players.TryGetValue(entityId, out var info))
            _players[entityId] = (info.PlayerId, name);
    }

    public GameStateSnapshot Snapshot() => new()
    {
        Phase = _phase,
        Turn = _turn,
        Player = BuildPlayerState(),
        Opponents = BuildOpponentStates(),
        GameEntityId = _gameEntityId,
    };

    private PlayerState BuildPlayerState()
    {
        if (_localPlayerEntityId == 0) return new PlayerState();

        var playerEntity = _entities.Get(_localPlayerEntityId);
        var heroEntityId = playerEntity?.TagInt("HERO_ENTITY") ?? 0;
        var hero = heroEntityId > 0 ? _entities.Get((uint)heroEntityId) : null;

        var name = _players.TryGetValue(_localPlayerEntityId, out var info)
            ? info.Name : "";

        return new PlayerState
        {
            EntityId = _localPlayerEntityId,
            PlayerId = _localPlayerId,
            Name = name,
            HeroCardId = hero?.CardId ?? "",
            HeroEntityId = (uint)heroEntityId,
            Health = (hero?.TagInt("HEALTH") ?? 0) - (hero?.TagInt("DAMAGE") ?? 0),
            Armor = hero?.TagInt("ARMOR") ?? 0,
            TavernTier = playerEntity?.TagInt("PLAYER_TECH_LEVEL") ?? 0,
            Board = BuildBoard(_localPlayerId),
            Shop = BuildShop(_localPlayerId),
            Gold = CalculateGold(playerEntity),
        };
    }

    private List<OpponentState> BuildOpponentStates()
    {
        var opponents = new List<OpponentState>();
        foreach (var (entityId, (playerId, _)) in _players)
        {
            if (entityId == _localPlayerEntityId) continue;
            var entity = _entities.Get(entityId);
            if (entity?.Tag("BACON_DUMMY_PLAYER") == "1") continue;

            var heroEntityId = entity?.TagInt("HERO_ENTITY") ?? 0;
            var hero = heroEntityId > 0 ? _entities.Get((uint)heroEntityId) : null;

            opponents.Add(new OpponentState
            {
                EntityId = entityId,
                PlayerId = playerId,
                HeroCardId = hero?.CardId ?? "",
                HeroEntityId = (uint)heroEntityId,
                Health = (hero?.TagInt("HEALTH") ?? 0) - (hero?.TagInt("DAMAGE") ?? 0),
                TavernTier = entity?.TagInt("PLAYER_TECH_LEVEL") ?? 0,
                LastKnownBoard = new List<Minion>(),
                LastSeenTurn = 0,
            });
        }
        return opponents;
    }

    private List<Minion> BuildBoard(uint playerId)
    {
        return _entities.Find(e =>
                e.IsMinion
                && e.Zone == "PLAY"
                && e.Controller == (int)playerId
                && e.TagInt("ZONE_POSITION") > 0)
            .Select(e => new Minion
            {
                EntityId = e.Id,
                CardId = e.CardId,
                Attack = e.TagInt("ATK"),
                Health = e.TagInt("HEALTH"),
                ZonePos = (uint)e.TagInt("ZONE_POSITION"),
            })
            .OrderBy(m => m.ZonePos)
            .ToList();
    }

    private List<ShopItem> BuildShop(uint playerId)
    {
        if (_phase != GamePhase.Shopping) return new List<ShopItem>();

        return _entities.Find(e =>
                e.Zone == "PLAY"
                && e.Controller != (int)playerId
                && e.Controller > 0
                && e.TagInt("ZONE_POSITION") > 0
                && (e.IsMinion || e.Tag("CARDTYPE") == "BATTLEGROUND_SPELL"))
            .Select(e => new ShopItem
            {
                EntityId = e.Id,
                CardId = e.CardId,
                Attack = e.TagInt("ATK"),
                Health = e.TagInt("HEALTH"),
                ZonePos = (uint)e.TagInt("ZONE_POSITION"),
                Tier = (byte)e.TagInt("TECH_LEVEL"),
                IsSpell = e.Tag("CARDTYPE") == "BATTLEGROUND_SPELL",
            })
            .OrderBy(s => s.ZonePos)
            .ToList();
    }

    private static int CalculateGold(Entity? playerEntity)
    {
        if (playerEntity == null) return 0;
        var resources = playerEntity.TagInt("RESOURCES");
        var used = playerEntity.TagInt("RESOURCES_USED");
        var temp = playerEntity.TagInt("TEMP_RESOURCES");
        return Math.Max(0, resources - used + temp);
    }

    private void UpdateCardId(uint id, string cardId)
    {
        if (string.IsNullOrEmpty(cardId)) return;
        var entity = _entities.Get(id);
        if (entity != null) entity.CardId = cardId;
    }

    private void Reset()
    {
        _entities.Clear();
        _resolver.Clear();
        _phase = GamePhase.NotStarted;
        _turn = 0;
        _gameEntityId = 0;
        _localPlayerEntityId = 0;
        _localPlayerId = 0;
        _players.Clear();
        _activeEntityId = null;
        _activeEntityIndent = 0;
    }
}
```

**Step 4: Run tests**

Run: `dotnet test tests/BronzebeardHud.GameState.Tests`
Expected: All PASS.

**Step 5: Commit**

```bash
git add -A
git commit -m "feat(game-state): port GameStateEngine with full state machine"
```

---

### Task 10: Port LogParser — Lexer

**Files:**
- Create: `src/BronzebeardHud.LogParser/Lexer.cs`
- Test: `tests/BronzebeardHud.LogParser.Tests/LexerTests.cs`

**Rust reference:** `crates/log_parser/src/lexer.rs`

**Step 1: Write failing tests**

```csharp
// tests/BronzebeardHud.LogParser.Tests/LexerTests.cs
using BronzebeardHud.GameState;

namespace BronzebeardHud.LogParser.Tests;

public class LexerTests
{
    private readonly Lexer _lexer = new();

    // ── Line Wrapper Tests ──────────────────────────────────────────

    [Fact]
    public void ParseLine_CreateGame()
    {
        var line = "D 08:47:21.5643288 GameState.DebugPrintPower() - CREATE_GAME";
        var result = _lexer.ParseLine(line)!;
        Assert.Equal("08:47:21.5643288", result.Timestamp);
        Assert.True(result.IsGameState);
        Assert.Equal(0u, result.Indent);
        Assert.IsType<RawPacket.CreateGame>(result.Packet);
    }

    [Fact]
    public void ParseLine_GameEntity()
    {
        var line = "D 08:47:21.5643288 GameState.DebugPrintPower() -     GameEntity EntityID=7";
        var result = _lexer.ParseLine(line)!;
        var packet = Assert.IsType<RawPacket.GameEntity>(result.Packet);
        Assert.Equal(7u, packet.EntityId);
        Assert.Equal(1u, result.Indent);
    }

    [Fact]
    public void ParseLine_PlayerEntity()
    {
        var line = "D 08:47:21.5643288 GameState.DebugPrintPower() -     Player EntityID=8 PlayerID=3 GameAccountId=[hi=144115198130930503 lo=17412774]";
        var result = _lexer.ParseLine(line)!;
        var packet = Assert.IsType<RawPacket.PlayerEntity>(result.Packet);
        Assert.Equal(8u, packet.EntityId);
        Assert.Equal(3u, packet.PlayerId);
    }

    [Fact]
    public void ParseLine_TagValue()
    {
        var line = "D 08:47:21.5763395 GameState.DebugPrintPower() -         tag=CARDTYPE value=GAME";
        var result = _lexer.ParseLine(line)!;
        var packet = Assert.IsType<RawPacket.TagValue>(result.Packet);
        Assert.Equal("CARDTYPE", packet.Tag);
        Assert.Equal("GAME", packet.Value);
        Assert.Equal(2u, result.Indent);
    }

    [Fact]
    public void ParseLine_FullEntityCreate()
    {
        var line = "D 08:47:21.8145549 GameState.DebugPrintPower() - FULL_ENTITY - Creating ID=29 CardID=TB_BaconShop_HERO_PH";
        var result = _lexer.ParseLine(line)!;
        var packet = Assert.IsType<RawPacket.FullEntityCreate>(result.Packet);
        Assert.Equal(29u, packet.Id);
        Assert.Equal("TB_BaconShop_HERO_PH", packet.CardId);
    }

    [Fact]
    public void ParseLine_FullEntityCreate_EmptyCardId()
    {
        var line = "D 08:47:21.8145549 GameState.DebugPrintPower() - FULL_ENTITY - Creating ID=100 CardID=";
        var result = _lexer.ParseLine(line)!;
        var packet = Assert.IsType<RawPacket.FullEntityCreate>(result.Packet);
        Assert.Equal(100u, packet.Id);
        Assert.Equal("", packet.CardId);
    }

    [Fact]
    public void ParseLine_TagChange_GameEntity()
    {
        var line = "D 08:47:22.9716049 GameState.DebugPrintPower() - TAG_CHANGE Entity=GameEntity tag=STEP value=BEGIN_MULLIGAN ";
        var result = _lexer.ParseLine(line)!;
        var packet = Assert.IsType<RawPacket.TagChange>(result.Packet);
        Assert.Equal(EntityRef.GameEntity(), packet.Entity);
        Assert.Equal("STEP", packet.Tag);
        Assert.Equal("BEGIN_MULLIGAN", packet.Value);
        Assert.False(packet.DefChange);
    }

    [Fact]
    public void ParseLine_TagChange_BracketEntity()
    {
        var line = "D 08:47:49.2007407 GameState.DebugPrintPower() -     TAG_CHANGE Entity=[entityName=Ragnaros the Firelord id=77 zone=HAND zonePos=1 cardId=TB_BaconShop_HERO_11 player=3] tag=LAST_AFFECTED_BY value=8 ";
        var result = _lexer.ParseLine(line)!;
        var packet = Assert.IsType<RawPacket.TagChange>(result.Packet);
        Assert.Equal(EntityRefKind.BracketRef, packet.Entity.Kind);
        Assert.Equal(77u, packet.Entity.Id);
        Assert.Equal(1u, result.Indent);
    }

    [Fact]
    public void ParseLine_TagChange_DefChange()
    {
        var line = "D 08:47:49.2007407 GameState.DebugPrintPower() - TAG_CHANGE Entity=79 tag=TAG_SCRIPT_DATA_NUM_1 value=5 DEF CHANGE";
        var result = _lexer.ParseLine(line)!;
        var packet = Assert.IsType<RawPacket.TagChange>(result.Packet);
        Assert.True(packet.DefChange);
    }

    [Fact]
    public void ParseLine_BlockStart()
    {
        var line = "D 08:47:22.9716049 GameState.DebugPrintPower() - BLOCK_START BlockType=TRIGGER Entity=7 EffectCardId= EffectIndex=1 Target=0 SubOption=-1 TriggerKeyword=0";
        var result = _lexer.ParseLine(line)!;
        var packet = Assert.IsType<RawPacket.BlockStart>(result.Packet);
        Assert.Equal("TRIGGER", packet.BlockType);
    }

    [Fact]
    public void ParseLine_BlockEnd()
    {
        var line = "D 08:47:22.9716049 GameState.DebugPrintPower() - BLOCK_END";
        var result = _lexer.ParseLine(line)!;
        Assert.IsType<RawPacket.BlockEnd>(result.Packet);
    }

    [Fact]
    public void ParseLine_ShowEntity()
    {
        var line = "D 08:47:22.9716049 GameState.DebugPrintPower() -     SHOW_ENTITY - Updating Entity=229 CardID=TB_BaconShopBadsongE";
        var result = _lexer.ParseLine(line)!;
        var packet = Assert.IsType<RawPacket.ShowEntity>(result.Packet);
        Assert.Equal(EntityRef.ById(229), packet.Entity);
        Assert.Equal("TB_BaconShopBadsongE", packet.CardId);
    }

    [Fact]
    public void ParseLine_HideEntity()
    {
        var line = "D 08:47:22.9716049 GameState.DebugPrintPower() -     HIDE_ENTITY - Entity=[entityName=Costs 0 id=229 zone=PLAY zonePos=0 cardId=TB_BaconShopBadsongE player=3] tag=ZONE value=REMOVEDFROMGAME";
        var result = _lexer.ParseLine(line)!;
        var packet = Assert.IsType<RawPacket.HideEntity>(result.Packet);
        Assert.Equal(EntityRefKind.BracketRef, packet.Entity.Kind);
        Assert.Equal("ZONE", packet.Tag);
        Assert.Equal("REMOVEDFROMGAME", packet.Value);
    }

    [Fact]
    public void ParseLine_MetaData()
    {
        var line = "D 08:47:22.9716049 GameState.DebugPrintPower() -     META_DATA - Meta=CONTROLLER_AND_ZONE_CHANGE Data=0 InfoCount=5";
        var result = _lexer.ParseLine(line)!;
        var packet = Assert.IsType<RawPacket.MetaData>(result.Packet);
        Assert.Equal("CONTROLLER_AND_ZONE_CHANGE", packet.Meta);
    }

    [Fact]
    public void ParseLine_PlayerName_ViaDebugPrintGame()
    {
        var line = "D 08:47:21.5763395 GameState.DebugPrintGame() - PlayerID=7, PlayerName=elphono#2437";
        var result = _lexer.ParseLine(line)!;
        var packet = Assert.IsType<RawPacket.PlayerName>(result.Packet);
        Assert.Equal(7u, packet.PlayerId);
        Assert.Equal("elphono#2437", packet.Name);
    }

    [Fact]
    public void ParseLine_PowerTaskList_NotGameState()
    {
        var line = "D 08:47:22.6252904 PowerTaskList.DebugPrintPower() -     FULL_ENTITY - Updating [entityName=BaconPHhero id=29 zone=PLAY zonePos=0 cardId=TB_BaconShop_HERO_PH player=3] CardID=TB_BaconShop_HERO_PH";
        var result = _lexer.ParseLine(line)!;
        Assert.False(result.IsGameState);
    }

    [Fact]
    public void ParseLine_Unrecognized_ReturnsNull()
    {
        Assert.Null(_lexer.ParseLine("this is not a valid power.log line"));
    }

    // ── Entity Parsing Tests ────────────────────────────────────────

    [Fact]
    public void ParseEntity_GameEntity()
    {
        Assert.Equal(EntityRef.GameEntity(), _lexer.ParseEntity("GameEntity"));
    }

    [Fact]
    public void ParseEntity_NumericId()
    {
        Assert.Equal(EntityRef.ById(229), _lexer.ParseEntity("229"));
    }

    [Fact]
    public void ParseEntity_Zero()
    {
        Assert.Equal(EntityRef.ById(0), _lexer.ParseEntity("0"));
    }

    [Fact]
    public void ParseEntity_BracketRef()
    {
        var entity = _lexer.ParseEntity("[entityName=Ragnaros the Firelord id=77 zone=HAND zonePos=1 cardId=TB_BaconShop_HERO_11 player=3]");
        Assert.Equal(EntityRefKind.BracketRef, entity.Kind);
        Assert.Equal(77u, entity.Id);
    }

    [Fact]
    public void ParseEntity_PlayerName()
    {
        Assert.Equal(EntityRef.ByName("BehEh#1355"), _lexer.ParseEntity("BehEh#1355"));
    }

    [Fact]
    public void ParseEntity_Trimming()
    {
        Assert.Equal(EntityRef.GameEntity(), _lexer.ParseEntity("  GameEntity  "));
        Assert.Equal(EntityRef.ById(7), _lexer.ParseEntity("  7  "));
    }
}
```

**Step 2: Run tests — verify they fail**

**Step 3: Implement Lexer**

```csharp
// src/BronzebeardHud.LogParser/Lexer.cs
using System.Text.RegularExpressions;
using BronzebeardHud.GameState;

namespace BronzebeardHud.LogParser;

public class Lexer
{
    private static readonly Regex LineRe = new(@"^[DWE] ([\d:.]+) (.+)$", RegexOptions.Compiled);
    private static readonly Regex SourceRe = new(@"^(GameState|PowerTaskList)\.DebugPrintPower\(\) - (.+)$", RegexOptions.Compiled);
    private static readonly Regex GamePrintRe = new(@"^GameState\.DebugPrintGame\(\) - (.+)$", RegexOptions.Compiled);
    private static readonly Regex PlayerNameRe = new(@"^PlayerID=(\d+), PlayerName=(.+)$", RegexOptions.Compiled);
    private static readonly Regex GameEntityRe = new(@"^GameEntity EntityID=(\d+)$", RegexOptions.Compiled);
    private static readonly Regex PlayerEntityRe = new(@"^Player EntityID=(\d+) PlayerID=(\d+) GameAccountId=", RegexOptions.Compiled);
    private static readonly Regex TagValueRe = new(@"^tag=(\S+) value=(.+)$", RegexOptions.Compiled);
    private static readonly Regex FullEntityCreateRe = new(@"^FULL_ENTITY - Creating ID=(\d+) CardID=(\w*)$", RegexOptions.Compiled);
    private static readonly Regex FullEntityUpdateRe = new(@"^FULL_ENTITY - Updating (.+) CardID=(\w*)$", RegexOptions.Compiled);
    private static readonly Regex ShowEntityRe = new(@"^SHOW_ENTITY - Updating Entity=(.+) CardID=(\w+)$", RegexOptions.Compiled);
    private static readonly Regex ChangeEntityRe = new(@"^CHANGE_ENTITY - Updating Entity=(.+) CardID=(\w+)$", RegexOptions.Compiled);
    private static readonly Regex HideEntityRe = new(@"^HIDE_ENTITY - Entity=(.+) tag=(\w+) value=(\w+)$", RegexOptions.Compiled);
    private static readonly Regex TagChangeRe = new(@"^TAG_CHANGE Entity=(.+) tag=(\w+) value=(\w+)\s*(DEF CHANGE)?$", RegexOptions.Compiled);
    private static readonly Regex BlockStartRe = new(@"^BLOCK_START BlockType=(\w+) Entity=(.+?) EffectCardId=.* EffectIndex=[-\d]+ Target=(.+?) SubOption=[-\d]+", RegexOptions.Compiled);
    private static readonly Regex MetaDataRe = new(@"^META_DATA - Meta=(\w+) Data=(\S+) InfoCount=(\d+)$", RegexOptions.Compiled);
    private static readonly Regex MetaDataInfoRe = new(@"^Info\[(\d+)\] = (.+)$", RegexOptions.Compiled);
    private static readonly Regex BracketEntityRe = new(@"^\[entityName=(.*?) id=(\d+) zone=(\w+) zonePos=(\d+) cardId=(\S*) player=(\d+)\]$", RegexOptions.Compiled);

    public LogLine? ParseLine(string line)
    {
        // Try DebugPrintPower lines
        var lineMatch = LineRe.Match(line);
        if (!lineMatch.Success) return null;

        var timestamp = lineMatch.Groups[1].Value;
        var rest = lineMatch.Groups[2].Value;

        var sourceMatch = SourceRe.Match(rest);
        if (sourceMatch.Success)
        {
            var isGameState = sourceMatch.Groups[1].Value == "GameState";
            var rawPayload = sourceMatch.Groups[2].Value;
            var trimmed = rawPayload.TrimStart();
            var spaces = rawPayload.Length - trimmed.Length;
            var indent = (uint)(spaces / 4);

            return new LogLine
            {
                Timestamp = timestamp,
                Indent = indent,
                IsGameState = isGameState,
                Packet = ParsePacket(trimmed),
            };
        }

        // Try DebugPrintGame lines (for player names)
        var gameMatch = GamePrintRe.Match(rest);
        if (!gameMatch.Success) return null;

        var payload = gameMatch.Groups[1].Value;
        var nameMatch = PlayerNameRe.Match(payload);
        if (!nameMatch.Success) return null;

        return new LogLine
        {
            Timestamp = timestamp,
            Indent = 0,
            IsGameState = true,
            Packet = new RawPacket.PlayerName(
                uint.Parse(nameMatch.Groups[1].Value),
                nameMatch.Groups[2].Value),
        };
    }

    public EntityRef ParseEntity(string s)
    {
        s = s.Trim();
        if (s == "GameEntity") return EntityRef.GameEntity();
        if (s == "0") return EntityRef.ById(0);

        var bracketMatch = BracketEntityRe.Match(s);
        if (bracketMatch.Success)
        {
            return EntityRef.Bracket(
                bracketMatch.Groups[1].Value,
                uint.Parse(bracketMatch.Groups[2].Value),
                bracketMatch.Groups[3].Value,
                uint.Parse(bracketMatch.Groups[4].Value),
                bracketMatch.Groups[5].Value,
                uint.Parse(bracketMatch.Groups[6].Value));
        }

        if (uint.TryParse(s, out var id)) return EntityRef.ById(id);
        return EntityRef.ByName(s);
    }

    private RawPacket? ParsePacket(string payload)
    {
        payload = payload.Trim();

        if (payload == "CREATE_GAME") return new RawPacket.CreateGame();
        if (payload == "BLOCK_END") return new RawPacket.BlockEnd();

        Match m;

        if ((m = GameEntityRe.Match(payload)).Success)
            return new RawPacket.GameEntity(uint.Parse(m.Groups[1].Value));

        if ((m = PlayerEntityRe.Match(payload)).Success)
            return new RawPacket.PlayerEntity(
                uint.Parse(m.Groups[1].Value),
                uint.Parse(m.Groups[2].Value));

        if ((m = TagValueRe.Match(payload)).Success)
            return new RawPacket.TagValue(m.Groups[1].Value, m.Groups[2].Value.Trim());

        if ((m = FullEntityCreateRe.Match(payload)).Success)
            return new RawPacket.FullEntityCreate(
                uint.Parse(m.Groups[1].Value),
                m.Groups[2].Value);

        if ((m = FullEntityUpdateRe.Match(payload)).Success)
            return new RawPacket.FullEntityUpdate(
                ParseEntity(m.Groups[1].Value),
                m.Groups[2].Value);

        if ((m = ShowEntityRe.Match(payload)).Success)
            return new RawPacket.ShowEntity(
                ParseEntity(m.Groups[1].Value),
                m.Groups[2].Value);

        if ((m = ChangeEntityRe.Match(payload)).Success)
            return new RawPacket.ChangeEntity(
                ParseEntity(m.Groups[1].Value),
                m.Groups[2].Value);

        if ((m = HideEntityRe.Match(payload)).Success)
            return new RawPacket.HideEntity(
                ParseEntity(m.Groups[1].Value),
                m.Groups[2].Value,
                m.Groups[3].Value);

        if ((m = TagChangeRe.Match(payload)).Success)
            return new RawPacket.TagChange(
                ParseEntity(m.Groups[1].Value),
                m.Groups[2].Value,
                m.Groups[3].Value,
                m.Groups[4].Success);

        if ((m = BlockStartRe.Match(payload)).Success)
            return new RawPacket.BlockStart(
                m.Groups[1].Value,
                ParseEntity(m.Groups[2].Value),
                ParseEntity(m.Groups[3].Value));

        if ((m = MetaDataRe.Match(payload)).Success)
            return new RawPacket.MetaData(
                m.Groups[1].Value,
                m.Groups[2].Value,
                uint.Parse(m.Groups[3].Value));

        if ((m = MetaDataInfoRe.Match(payload)).Success)
            return new RawPacket.MetaDataInfo(
                uint.Parse(m.Groups[1].Value),
                ParseEntity(m.Groups[2].Value));

        return null;
    }
}
```

**Step 4: Run tests**

Run: `dotnet test tests/BronzebeardHud.LogParser.Tests`
Expected: All PASS.

**Step 5: Commit**

```bash
git add -A
git commit -m "feat(log-parser): port Lexer with all regex patterns"
```

---

### Task 11: Port LogParser — LogWatcher

**Files:**
- Create: `src/BronzebeardHud.LogParser/LogWatcher.cs`
- Create: `src/BronzebeardHud.LogParser/WatcherEvent.cs`
- Create: `src/BronzebeardHud.LogParser/LogPaths.cs`

No unit tests for watcher — it's I/O-bound and the Rust version had no tests either. Tested via integration.

**Rust reference:** `crates/log_parser/src/watcher.rs`

**Step 1: Create WatcherEvent**

```csharp
// src/BronzebeardHud.LogParser/WatcherEvent.cs
using BronzebeardHud.GameState;

namespace BronzebeardHud.LogParser;

public abstract record WatcherEvent
{
    public sealed record Line(LogLine LogLine) : WatcherEvent;
    public sealed record SessionChanged : WatcherEvent;
}
```

**Step 2: Create LogPaths helper**

```csharp
// src/BronzebeardHud.LogParser/LogPaths.cs
namespace BronzebeardHud.LogParser;

public static class LogPaths
{
    public static string DefaultLogsDir()
    {
        if (OperatingSystem.IsWindows())
            return @"E:\JEUX\Hearthstone\Logs";
        // WSL path
        return "/mnt/e/JEUX/Hearthstone/Logs";
    }
}
```

**Step 3: Implement LogWatcher**

```csharp
// src/BronzebeardHud.LogParser/LogWatcher.cs
using System.Threading.Channels;

namespace BronzebeardHud.LogParser;

public class LogWatcher
{
    private readonly string _logsDir;

    public LogWatcher(string logsDir)
    {
        _logsDir = logsDir;
    }

    public ChannelReader<WatcherEvent> Watch(CancellationToken ct = default)
    {
        var channel = Channel.CreateUnbounded<WatcherEvent>();
        _ = Task.Run(() => WatchLoop(channel.Writer, ct), ct);
        return channel.Reader;
    }

    private async Task WatchLoop(ChannelWriter<WatcherEvent> writer, CancellationToken ct)
    {
        var lexer = new Lexer();
        string? currentPath = null;
        long readPos = 0;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(500, ct);

                var latest = FindLatestPowerLog();
                if (latest == null) continue;

                if (currentPath != latest)
                {
                    if (currentPath != null)
                    {
                        await writer.WriteAsync(new WatcherEvent.SessionChanged(), ct);
                    }
                    currentPath = latest;
                    readPos = FindLastCreateGame(latest);
                }

                using var stream = new FileStream(currentPath, FileMode.Open,
                    FileAccess.Read, FileShare.ReadWrite);
                stream.Seek(readPos, SeekOrigin.Begin);
                using var reader = new StreamReader(stream);

                while (reader.ReadLine() is { } line)
                {
                    readPos = stream.Position;
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    if (line.Contains("CREATE_GAME") && line.Contains("GameState"))
                    {
                        await writer.WriteAsync(new WatcherEvent.SessionChanged(), ct);
                    }

                    var logLine = lexer.ParseLine(line);
                    if (logLine?.IsGameState == true)
                    {
                        await writer.WriteAsync(new WatcherEvent.Line(logLine), ct);
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            writer.Complete();
        }
    }

    private string? FindLatestPowerLog()
    {
        if (!Directory.Exists(_logsDir)) return null;

        var latest = Directory.GetDirectories(_logsDir, "Hearthstone_*")
            .OrderDescending()
            .FirstOrDefault();

        if (latest == null) return null;
        var powerLog = Path.Combine(latest, "Power.log");
        return File.Exists(powerLog) ? powerLog : null;
    }

    private static long FindLastCreateGame(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open,
                FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);

            long lastOffset = 0;
            long currentOffset = 0;

            while (reader.ReadLine() is { } line)
            {
                if (line.Contains("CREATE_GAME") && line.Contains("GameState"))
                    lastOffset = currentOffset;
                currentOffset = stream.Position;
            }

            return lastOffset;
        }
        catch
        {
            return 0;
        }
    }
}
```

**Step 4: Build**

Run: `dotnet build`
Expected: Build succeeded.

**Step 5: Commit**

```bash
git add -A
git commit -m "feat(log-parser): port LogWatcher with Channel-based async events"
```

---

### Task 12: Avalonia App — Scaffold and Wire Up

**Files:**
- Modify: `src/BronzebeardHud.App/` (Avalonia template files)
- Create: `src/BronzebeardHud.App/ViewModels/MainViewModel.cs`
- Create: `src/BronzebeardHud.App/Services/GameStateService.cs`
- Modify: `src/BronzebeardHud.App/MainWindow.axaml`
- Modify: `src/BronzebeardHud.App/MainWindow.axaml.cs`

**Step 1: Create MainViewModel**

```csharp
// src/BronzebeardHud.App/ViewModels/MainViewModel.cs
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BronzebeardHud.GameState;

namespace BronzebeardHud.App.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private GameStateSnapshot _state = new();

    public GameStateSnapshot State
    {
        get => _state;
        set { _state = value; OnPropertyChanged(); OnPropertyChanged(nameof(PhaseText)); }
    }

    public string PhaseText => State.Phase switch
    {
        GamePhase.NotStarted => "Waiting for game...",
        GamePhase.HeroSelect => "Hero selection...",
        GamePhase.Shopping => $"Turn {State.Turn} - Shopping",
        GamePhase.Combat => $"Turn {State.Turn} - Combat",
        GamePhase.GameOver => "Game Over",
        _ => "",
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
```

**Step 2: Create GameStateService**

```csharp
// src/BronzebeardHud.App/Services/GameStateService.cs
using System.Threading.Channels;
using Avalonia.Threading;
using BronzebeardHud.GameState;
using BronzebeardHud.LogParser;
using BronzebeardHud.App.ViewModels;

namespace BronzebeardHud.App.Services;

public class GameStateService
{
    private readonly MainViewModel _viewModel;
    private readonly string _logsDir;

    public GameStateService(MainViewModel viewModel, string logsDir)
    {
        _viewModel = viewModel;
        _logsDir = logsDir;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var watcher = new LogWatcher(_logsDir);
        var reader = watcher.Watch(ct);
        var engine = new GameStateEngine();
        var playerIdentified = false;

        await foreach (var evt in reader.ReadAllAsync(ct))
        {
            switch (evt)
            {
                case WatcherEvent.SessionChanged:
                    engine = new GameStateEngine();
                    playerIdentified = false;
                    Dispatcher.UIThread.Post(() => _viewModel.State = new GameStateSnapshot());
                    break;

                case WatcherEvent.Line(var logLine):
                    engine.Process(logLine);

                    if (!playerIdentified)
                    {
                        var snap = engine.Snapshot();
                        if (snap.Phase == GamePhase.HeroSelect)
                        {
                            engine.IdentifyLocalPlayer();
                            playerIdentified = true;
                        }
                    }

                    // Register player names from Name entity refs
                    if (logLine.Packet is RawPacket.TagChange { Entity: { Kind: EntityRefKind.Name } nameRef }
                        && playerIdentified)
                    {
                        engine.RegisterPlayerName(nameRef.EntityName, 0); // resolver handles it
                    }

                    var state = engine.Snapshot();
                    Dispatcher.UIThread.Post(() => _viewModel.State = state);
                    break;
            }
        }
    }
}
```

**Step 3: Update MainWindow.axaml**

```xml
<!-- src/BronzebeardHud.App/MainWindow.axaml -->
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="BronzebeardHud.App.MainWindow"
        Title="Bronzebeard HUD"
        Width="400" Height="600"
        Background="#B0000000"
        Foreground="White"
        FontFamily="Segoe UI"
        SystemDecorations="None"
        Topmost="True"
        ShowInTaskbar="False"
        TransparencyLevelHint="Transparent">

    <StackPanel Margin="12">
        <TextBlock Text="{Binding PhaseText}"
                   FontSize="16" FontWeight="Bold"
                   Margin="0,0,0,8"/>

        <!-- Hero Info -->
        <StackPanel IsVisible="{Binding State.Phase,
            Converter={x:Static converters:PhaseConverters.IsInGame}}">
            <TextBlock FontSize="14">
                <Run Text="{Binding State.Player.HeroCardId}"/>
                <Run Text=" - HP: "/><Run Text="{Binding State.Player.Health}"/>
                <Run Text=" Armor: "/><Run Text="{Binding State.Player.Armor}"/>
            </TextBlock>
            <TextBlock FontSize="12">
                <Run Text="Tier: "/><Run Text="{Binding State.Player.TavernTier}"/>
                <Run Text=" Gold: "/><Run Text="{Binding State.Player.Gold}"/>
            </TextBlock>
        </StackPanel>
    </StackPanel>
</Window>
```

**Step 4: Update MainWindow.axaml.cs to start engine**

```csharp
// src/BronzebeardHud.App/MainWindow.axaml.cs
using Avalonia.Controls;
using BronzebeardHud.App.Services;
using BronzebeardHud.App.ViewModels;
using BronzebeardHud.LogParser;

namespace BronzebeardHud.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var viewModel = new MainViewModel();
        DataContext = viewModel;

        var service = new GameStateService(viewModel, LogPaths.DefaultLogsDir());
        _ = service.RunAsync(CancellationToken.None);
    }
}
```

**Step 5: Build and run**

Run: `dotnet build src/BronzebeardHud.App`
Expected: Build succeeded.

Run: `dotnet run --project src/BronzebeardHud.App`
Expected: Window appears showing "Waiting for game..."

**Step 6: Commit**

```bash
git add -A
git commit -m "feat(app): wire up Avalonia overlay with GameStateService and MainViewModel"
```

---

### Task 13: Avalonia App — Window Positioning and HS Tracking

**Files:**
- Create: `src/BronzebeardHud.App/Services/HsWindowService.cs`
- Modify: `src/BronzebeardHud.App/MainWindow.axaml.cs`

This task adds HS window finding and overlay anchoring. On Linux/WSL this will be a no-op stub (same as Rust version). The Win32 P/Invoke only activates on Windows.

**Step 1: Implement HsWindowService**

```csharp
// src/BronzebeardHud.App/Services/HsWindowService.cs
using System.Runtime.InteropServices;

namespace BronzebeardHud.App.Services;

public class HsWindowService
{
    public record WindowRect(int X, int Y, int Width, int Height);

    public WindowRect? GetHsWindowRect()
    {
        if (!OperatingSystem.IsWindows()) return null;
        return GetHsWindowRectWindows();
    }

    public bool IsHsForeground()
    {
        if (!OperatingSystem.IsWindows()) return true; // Always show on non-Windows (dev)
        return IsHsForegroundWindows();
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static WindowRect? GetHsWindowRectWindows()
    {
        var hwnd = FindWindowW("UnityWndClass", "Hearthstone");
        if (hwnd == IntPtr.Zero) return null;
        if (IsIconic(hwnd)) return null;
        if (!GetWindowRect(hwnd, out var rect)) return null;
        return new WindowRect(rect.Left, rect.Top,
            rect.Right - rect.Left, rect.Bottom - rect.Top);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static bool IsHsForegroundWindows()
    {
        var fg = GetForegroundWindow();
        var hs = FindWindowW("UnityWndClass", "Hearthstone");
        return fg == hs && hs != IntPtr.Zero;
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindowW(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }
}
```

**Step 2: Integrate into MainWindow**

Update `MainWindow.axaml.cs` to periodically check HS window position and anchor:

```csharp
// Add to MainWindow constructor after existing code:
var hsService = new HsWindowService();
var timer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
timer.Tick += (_, _) =>
{
    var rect = hsService.GetHsWindowRect();
    if (rect != null)
    {
        Position = new Avalonia.PixelPoint(rect.X + rect.Width, rect.Y);
        Height = rect.Height;
    }

    if (!hsService.IsHsForeground())
        Hide();
    else
        Show();
};
timer.Start();
```

**Step 3: Build**

Run: `dotnet build src/BronzebeardHud.App`
Expected: Build succeeded.

**Step 4: Commit**

```bash
git add -A
git commit -m "feat(app): add HS window tracking with P/Invoke and overlay anchoring"
```

---

### Task 14: Run All Tests and Verify

**Step 1: Run full test suite**

Run: `dotnet test`
Expected: All tests pass.

**Step 2: Run the app**

Run: `dotnet run --project src/BronzebeardHud.App`
Expected: Overlay window appears, shows "Waiting for game...", reacts to Power.log if HS is running.

**Step 3: Commit any fixes**

If tests or build needed fixes, commit them.

---

### Task 15: Push to GitHub

**Step 1: Create GitHub repo** (user must do this manually or via `gh`)

**Step 2: Add remote and push**

```bash
cd ~/workspace/bg_ultimate_hud
git remote add origin git@github.com:elphono/bg_ultimate_hud.git
git push -u origin main
```
