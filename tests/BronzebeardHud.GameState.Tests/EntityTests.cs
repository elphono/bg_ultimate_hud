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
