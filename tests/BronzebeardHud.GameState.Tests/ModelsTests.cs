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
