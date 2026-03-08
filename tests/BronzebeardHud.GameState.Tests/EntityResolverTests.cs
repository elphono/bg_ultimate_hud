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
