using System.Runtime.InteropServices;
using GitExtensions.Extensibility;

namespace GitExtensions.UITests;

/// <summary>
///  A native top-level window that owns the dialogs of the tests (in place of the WinForms form of the tests before WinForms
///  was removed), centered on the screen.
/// </summary>
public sealed class TestOwnerWindow : IWin32Window, IDisposable
{
    private const string ClassName = "GitExtensionsTestOwner";
    private const int WS_OVERLAPPEDWINDOW = 0x00CF0000;
    private const int WS_VISIBLE = 0x10000000;
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;
    private const int COLOR_WINDOW = 5;

    private static readonly WindowProcedure _windowProcedure = DefWindowProcW;
    private static readonly Lazy<bool> _classRegistered = new(RegisterClass);

    public TestOwnerWindow(string text, int width, int height)
    {
        _ = _classRegistered.Value;
        int x = Math.Max(0, (GetSystemMetrics(SM_CXSCREEN) - width) / 2);
        int y = Math.Max(0, (GetSystemMetrics(SM_CYSCREEN) - height) / 2);
        Handle = CreateWindowExW(0, ClassName, text, WS_OVERLAPPEDWINDOW | WS_VISIBLE, x, y, width, height, 0, 0, GetModuleHandleW(null), 0);
        if (Handle == 0)
        {
            throw new InvalidOperationException($"Cannot create the test window (error {Marshal.GetLastWin32Error()}).");
        }
    }

    public nint Handle { get; private set; }

    /// <summary>Whether the window can be used (not disabled by a modal dialog).</summary>
    public bool IsEnabled => IsWindowEnabled(Handle);

    public void Dispose()
    {
        if (Handle != 0)
        {
            DestroyWindow(Handle);
            Handle = 0;
        }
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

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowEnabled(nint window);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandleW(string? moduleName);
}
