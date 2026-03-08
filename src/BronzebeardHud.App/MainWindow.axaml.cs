using System;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Threading;
using BronzebeardHud.App.Services;
using BronzebeardHud.App.ViewModels;
using BronzebeardHud.LogParser;

namespace BronzebeardHud.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var viewModel = new MainViewModel();
        DataContext = viewModel;

        // Start game state engine on background thread
        var service = new GameStateService(viewModel, LogPaths.DefaultLogsDir());
        _ = service.RunAsync(CancellationToken.None);

        // HS window tracking timer
        var hsService = new HsWindowService();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        timer.Tick += (_, _) =>
        {
            var rect = hsService.GetHsWindowRect();
            if (rect != null)
            {
                Position = new Avalonia.PixelPoint(rect.X + rect.Width, rect.Y);
                Height = rect.Height;
            }

            if (!hsService.IsHsForeground())
                Hide();
            else
                Show();
        };
        timer.Start();
    }
}
