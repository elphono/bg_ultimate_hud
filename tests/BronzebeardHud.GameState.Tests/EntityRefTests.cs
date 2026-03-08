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
