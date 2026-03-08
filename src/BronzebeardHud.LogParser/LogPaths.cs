namespace BronzebeardHud.LogParser;

public static class LogPaths
{
    public static string DefaultLogsDir()
    {
        if (OperatingSystem.IsWindows())
            return @"E:\JEUX\Hearthstone\Logs";
        return "/mnt/e/JEUX/Hearthstone/Logs";
    }
}
