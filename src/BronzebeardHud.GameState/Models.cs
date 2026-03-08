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
