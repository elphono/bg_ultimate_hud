using BronzebeardHud.GameState;

namespace BronzebeardHud.LogParser.Tests;

public class LexerTests
{
    private readonly Lexer _lexer = new();

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
