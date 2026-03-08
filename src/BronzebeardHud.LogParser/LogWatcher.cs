using System.Threading.Channels;

namespace BronzebeardHud.LogParser;

public class LogWatcher
{
    private readonly string _logsDir;

    public LogWatcher(string logsDir)
    {
        _logsDir = logsDir;
    }

    public ChannelReader<WatcherEvent> Watch(CancellationToken ct = default)
    {
        var channel = Channel.CreateUnbounded<WatcherEvent>();
        _ = Task.Run(() => WatchLoop(channel.Writer, ct), ct);
        return channel.Reader;
    }

    private async Task WatchLoop(ChannelWriter<WatcherEvent> writer, CancellationToken ct)
    {
        var lexer = new Lexer();
        string? currentPath = null;
        long readPos = 0;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(500, ct);

                var latest = FindLatestPowerLog();
                if (latest == null) continue;

                if (currentPath != latest)
                {
                    if (currentPath != null)
                    {
                        await writer.WriteAsync(new WatcherEvent.SessionChanged(), ct);
                    }
                    currentPath = latest;
                    readPos = FindLastCreateGame(latest);
                }

                using var stream = new FileStream(currentPath, FileMode.Open,
                    FileAccess.Read, FileShare.ReadWrite);
                stream.Seek(readPos, SeekOrigin.Begin);
                using var reader = new StreamReader(stream);

                while (reader.ReadLine() is { } line)
                {
                    readPos = stream.Position;
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    if (line.Contains("CREATE_GAME") && line.Contains("GameState"))
                    {
                        await writer.WriteAsync(new WatcherEvent.SessionChanged(), ct);
                    }

                    var logLine = lexer.ParseLine(line);
                    if (logLine?.IsGameState == true)
                    {
                        await writer.WriteAsync(new WatcherEvent.Line(logLine), ct);
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            writer.Complete();
        }
    }

    private string? FindLatestPowerLog()
    {
        if (!Directory.Exists(_logsDir)) return null;

        var latest = Directory.GetDirectories(_logsDir, "Hearthstone_*")
            .OrderDescending()
            .FirstOrDefault();

        if (latest == null) return null;
        var powerLog = Path.Combine(latest, "Power.log");
        return File.Exists(powerLog) ? powerLog : null;
    }

    private static long FindLastCreateGame(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open,
                FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);

            long lastOffset = 0;
            long currentOffset = 0;

            while (reader.ReadLine() is { } line)
            {
                if (line.Contains("CREATE_GAME") && line.Contains("GameState"))
                    lastOffset = currentOffset;
                currentOffset = stream.Position;
            }

            return lastOffset;
        }
        catch
        {
            return 0;
        }
    }
}
