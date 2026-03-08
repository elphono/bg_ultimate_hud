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
