using BronzebeardHud.GameState;

namespace BronzebeardHud.LogParser;

public abstract record WatcherEvent
{
    public sealed record Line(LogLine LogLine) : WatcherEvent;
    public sealed record SessionChanged : WatcherEvent;
}
