using System.Runtime.InteropServices;
using GitUI.Presentation.Services;

namespace GitUI.ConsoleEmulation;

/// <summary>
///  A native child window in which a console emulator (ConEmu, mintty) runs its own window (in place of the WinForms
///  panel of the console controls): the Avalonia window embeds it (<see cref="IEmbeddedNativeView"/>) and sizes it, it
///  raises <see cref="Resized"/> for the console to follow; while not shown it is parked in a message-only window.
/// </summary>
internal sealed class NativeHostWindow : IEmbeddedNativeView, IDisposable
{
    private const string ClassName = "GitExtensionsConsoleHost";
    private const int WS_CHILD = 0x40000000;
    private const int WS_CLIPCHILDREN = 0x02000000;
    private const int WS_CLIPSIBLINGS = 0x04000000;
    private const uint WM_SIZE = 0x0005;
    private const uint WM_SETFOCUS = 0x0007;
    private const uint WM_DESTROY = 0x0002;
    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;
    private const int COLOR_WINDOW = 5;
    private static readonly nint HWND_MESSAGE = -3;

    private static readonly WindowProcedure _windowProcedure = WindowProc;
    private static readonly Lazy<bool> _classRegistered = new(RegisterClass);
    private static readonly Dictionary<nint, NativeHostWindow> _windows = [];

    public NativeHostWindow()
    {
        _ = _classRegistered.Value;
        Handle = CreateWindowExW(0, ClassName, "", WS_CHILD | WS_CLIPCHILDREN | WS_CLIPSIBLINGS, 0, 0, 0, 0, HWND_MESSAGE, 0, GetModuleHandleW(null), 0);
        if (Handle == 0)
        {
            throw new InvalidOperationException($"Cannot create the window of the console (error {Marshal.GetLastWin32Error()}).");
        }

        _windows[Handle] = this;
    }

    /// <summary>The native handle of the window.</summary>
    public nint Handle { get; private set; }

    /// <summary>The size of the client area, in pixels.</summary>
    public (int Width, int Height) ClientSize
        => GetClientRect(Handle, out Rect rect) ? (rect.Right - rect.Left, rect.Bottom - rect.Top) : (0, 0);

    public bool IsDisposed => Handle == 0;

    /// <summary>Raised when the window was resized.</summary>
    public event EventHandler? Resized;

    /// <summary>Raised when the window got the keyboard focus (e.g. when the user clicks the host window), for the console to take it.</summary>
    public event EventHandler? FocusReceived;

    public nint Attach(nint parentWindow)
    {
        SetParent(Handle, parentWindow);
        ShowWindow(Handle, SW_SHOW);
        return Handle;
    }

    public void Detach()
    {
        if (Handle != 0)
        {
            ShowWindow(Handle, SW_HIDE);
            SetParent(Handle, HWND_MESSAGE);
        }
    }

    /// <summary>Gives the keyboard focus to the window, which forwards it to the console (<see cref="FocusReceived"/>).</summary>
    public void Focus()
    {
        if (Handle != 0)
        {
            SetFocus(Handle);
        }
    }

    public void Dispose()
    {
        if (Handle != 0)
        {
            nint handle = Handle;
            Handle = 0;
            _windows.Remove(handle);
            DestroyWindow(handle);
        }
    }

    private static nint WindowProc(nint window, uint message, nint wordParameter, nint longParameter)
    {
        if (_windows.TryGetValue(window, out NativeHostWindow? host))
        {
            switch (message)
            {
                case WM_SIZE:
                    host.Resized?.Invoke(host, EventArgs.Empty);
                    break;
                case WM_SETFOCUS:
                    host.FocusReceived?.Invoke(host, EventArgs.Empty);
                    break;
                case WM_DESTROY:
                    _windows.Remove(window);
                    host.Handle = 0;
                    break;
            }
        }

        return DefWindowProcW(window, message, wordParameter, longParameter);
    }

    private static bool RegisterClass()
    {
        WindowClass windowClass = new()
        {
            Size = (uint)Marshal.SizeOf<WindowClass>(),
            WindowProcedure = Marshal.GetFunctionPointerForDelegate(_windowProcedure),
            Instance = GetModuleHandleW(null),
            Background = COLOR_WINDOW + 1,
            ClassName = ClassName,
        };
        return RegisterClassExW(ref windowClass) != 0;
    }

    private delegate nint WindowProcedure(nint window, uint message, nint wordParameter, nint longParameter);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WindowClass
    {
        public uint Size;
        public uint Style;
        public nint WindowProcedure;
        public int ClassExtra;
        public int WindowExtra;
        public nint Instance;
        public nint Icon;
        public nint Cursor;
        public nint Background;
        public string? MenuName;
        public string ClassName;
        public nint SmallIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassExW(ref WindowClass windowClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowExW(uint extendedStyle, string className, string windowName, int style, int x, int y, int width, int height,
        nint parent, nint menu, nint instance, nint parameter);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(nint window);

    [DllImport("user32.dll")]
    private static extern nint DefWindowProcW(nint window, uint message, nint wordParameter, nint longParameter);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetParent(nint child, nint newParent);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(nint window, int command);

    [DllImport("user32.dll")]
    private static extern nint SetFocus(nint window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClientRect(nint window, out Rect rect);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandleW(string? moduleName);
}
