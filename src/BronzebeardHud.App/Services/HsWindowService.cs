using System;
using System.Runtime.InteropServices;

namespace BronzebeardHud.App.Services;

public class HsWindowService
{
    public record WindowRect(int X, int Y, int Width, int Height);

    public WindowRect? GetHsWindowRect()
    {
        if (!OperatingSystem.IsWindows()) return null;
        return GetHsWindowRectWindows();
    }

    public bool IsHsForeground()
    {
        if (!OperatingSystem.IsWindows()) return true;
        return IsHsForegroundWindows();
    }

    private static WindowRect? GetHsWindowRectWindows()
    {
        var hwnd = FindWindowW("UnityWndClass", "Hearthstone");
        if (hwnd == IntPtr.Zero) return null;
        if (IsIconic(hwnd)) return null;
        if (!GetWindowRect(hwnd, out var rect)) return null;
        return new WindowRect(rect.Left, rect.Top,
            rect.Right - rect.Left, rect.Bottom - rect.Top);
    }

    private static bool IsHsForegroundWindows()
    {
        var fg = GetForegroundWindow();
        var hs = FindWindowW("UnityWndClass", "Hearthstone");
        return fg == hs && hs != IntPtr.Zero;
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindowW(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }
}
