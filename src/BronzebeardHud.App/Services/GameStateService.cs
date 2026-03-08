using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using BronzebeardHud.GameState;
using BronzebeardHud.LogParser;
using BronzebeardHud.App.ViewModels;

namespace BronzebeardHud.App.Services;

public class GameStateService
{
    private readonly MainViewModel _viewModel;
    private readonly string _logsDir;

    public GameStateService(MainViewModel viewModel, string logsDir)
    {
        _viewModel = viewModel;
        _logsDir = logsDir;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var watcher = new LogWatcher(_logsDir);
        var reader = watcher.Watch(ct);
        var engine = new GameStateEngine();
        var playerIdentified = false;

        await foreach (var evt in reader.ReadAllAsync(ct))
        {
            switch (evt)
            {
                case WatcherEvent.SessionChanged:
                    engine = new GameStateEngine();
                    playerIdentified = false;
                    Dispatcher.UIThread.Post(() => _viewModel.State = new GameStateSnapshot());
                    break;

                case WatcherEvent.Line(var logLine):
                    engine.Process(logLine);

                    if (!playerIdentified)
                    {
                        var snap = engine.Snapshot();
                        if (snap.Phase == GamePhase.HeroSelect)
                        {
                            engine.IdentifyLocalPlayer();
                            playerIdentified = true;
                        }
                    }

                    var state = engine.Snapshot();
                    Dispatcher.UIThread.Post(() => _viewModel.State = state);
                    break;
            }
        }
    }
}
