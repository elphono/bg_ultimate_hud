using System.ComponentModel;
using System.Runtime.CompilerServices;
using BronzebeardHud.GameState;

namespace BronzebeardHud.App.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private GameStateSnapshot _state = new();

    public GameStateSnapshot State
    {
        get => _state;
        set { _state = value; OnPropertyChanged(); OnPropertyChanged(nameof(PhaseText)); }
    }

    public string PhaseText => State.Phase switch
    {
        GamePhase.NotStarted => "Waiting for game...",
        GamePhase.HeroSelect => "Hero selection...",
        GamePhase.Shopping => $"Turn {State.Turn} - Shopping",
        GamePhase.Combat => $"Turn {State.Turn} - Combat",
        GamePhase.GameOver => "Game Over",
        _ => "",
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
