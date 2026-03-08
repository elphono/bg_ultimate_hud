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
