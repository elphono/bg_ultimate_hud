namespace BronzebeardHud.GameState;

public sealed class LogLine
{
    public string Timestamp { get; init; } = "";
    public uint Indent { get; init; }
    public bool IsGameState { get; init; }
    public RawPacket? Packet { get; init; }
}
