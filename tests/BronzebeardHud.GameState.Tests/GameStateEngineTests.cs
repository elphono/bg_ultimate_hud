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
