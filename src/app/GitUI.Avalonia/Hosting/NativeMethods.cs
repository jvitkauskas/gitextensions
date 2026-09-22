using System.Runtime.InteropServices;

namespace GitUI.Avalonia.Hosting;

/// <summary>
///  Win32 interop used to host Avalonia windows in the WinForms process. Windows-only by design until the
///  cross-platform phase (docs/avalonia-port/PLAN.md).
/// </summary>
internal static class NativeMethods
{
    public const uint GA_ROOT = 2;
    public const int GWLP_HWNDPARENT = -8;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private delegate bool EnumWindowsProc(nint hWnd, nint lParam);

    [DllImport("user32.dll")]
    public static extern nint GetAncestor(nint hwnd, uint gaFlags);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    public static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnableWindow(nint hWnd, [MarshalAs(UnmanagedType.Bool)] bool bEnable);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowEnabled(nint hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(nint hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumThreadWindows(uint dwThreadId, EnumWindowsProc lpfn, nint lParam);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    /// <summary>
    ///  Disables the visible, enabled top-level windows of the current thread (as WinForms does for modal forms).
    /// </summary>
    public static List<nint> DisableEnabledThreadWindows(nint except)
    {
        List<nint> windows = [];
        EnumThreadWindows(
            GetCurrentThreadId(),
            (hWnd, _) =>
            {
                if (hWnd != except && IsWindowVisible(hWnd) && IsWindowEnabled(hWnd))
                {
                    windows.Add(hWnd);
                }

                return true;
            },
            0);

        foreach (nint window in windows)
        {
            EnableWindow(window, false);
        }

        return windows;
    }

    public static void EnableWindows(IEnumerable<nint> windows)
    {
        foreach (nint window in windows)
        {
            EnableWindow(window, true);
        }
    }
}
