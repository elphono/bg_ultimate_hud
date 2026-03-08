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
