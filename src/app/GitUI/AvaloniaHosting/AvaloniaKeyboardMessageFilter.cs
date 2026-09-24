using System.Runtime.InteropServices;
using System.Text;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  Lets the modeless Avalonia windows (e.g. the main window) get their text input while the WinForms message loop runs.
/// </summary>
/// <remarks>
///  <c>Application.Run</c> gives the key messages of windows that are not WinForms controls to <c>IsDialogMessage</c> of their
///  root window, which handles them as dialog navigation: the characters are never translated (no <c>WM_CHAR</c>), so the
///  text boxes of an Avalonia window get no text. The modal Avalonia dialogs run their own message loop and are not
///  affected. This filter translates and dispatches the key messages of Avalonia windows itself, as their own loop does.
/// </remarks>
internal sealed class AvaloniaKeyboardMessageFilter : IMessageFilter
{
    private const int WM_KEYFIRST = 0x0100;
    private const int WM_KEYLAST = 0x0109;
    private const uint GA_ROOT = 2;
    private const string AvaloniaWindowClassPrefix = "Avalonia-";

    private static bool _installed;

    private AvaloniaKeyboardMessageFilter()
    {
    }

    /// <summary>Installs the filter on the UI thread, once.</summary>
    public static void Install()
    {
        if (!_installed)
        {
            _installed = true;
            Application.AddMessageFilter(new AvaloniaKeyboardMessageFilter());
        }
    }

    public bool PreFilterMessage(ref Message m)
    {
        if (m.Msg is < WM_KEYFIRST or > WM_KEYLAST || m.HWnd == 0 || Control.FromChildHandle(m.HWnd) is not null)
        {
            // Not a key message, or one of a WinForms control (also one embedded in an Avalonia window): WinForms handles it.
            return false;
        }

        if (!IsAvaloniaWindow(NativeMethods.GetAncestor(m.HWnd, GA_ROOT)))
        {
            return false;
        }

        NativeMethods.MSG msg = new() { hwnd = m.HWnd, message = (uint)m.Msg, wParam = m.WParam, lParam = m.LParam };
        NativeMethods.TranslateMessage(ref msg);
        NativeMethods.DispatchMessageW(ref msg);
        return true;
    }

    private static bool IsAvaloniaWindow(nint hwnd)
    {
        if (hwnd == 0)
        {
            return false;
        }

        StringBuilder className = new(64);
        return NativeMethods.GetClassNameW(hwnd, className, className.Capacity) > 0
            && className.ToString().StartsWith(AvaloniaWindowClassPrefix, StringComparison.Ordinal);
    }

    private static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct MSG
        {
            public nint hwnd;
            public uint message;
            public nint wParam;
            public nint lParam;
            public uint time;
            public int ptX;
            public int ptY;
        }

        [DllImport("user32.dll")]
        public static extern nint GetAncestor(nint hwnd, uint flags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetClassNameW(nint hwnd, StringBuilder className, int maxCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool TranslateMessage(ref MSG msg);

        [DllImport("user32.dll")]
        public static extern nint DispatchMessageW(ref MSG msg);
    }
}
