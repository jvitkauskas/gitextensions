using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CommonTestUtils;

/// <summary>
///  The Windows message loop of the UI thread of the tests (in place of the WinForms <c>Application.DoEvents</c>).
/// </summary>
public static class MessagePump
{
    private const uint PM_REMOVE = 0x0001;

    /// <summary>Processes the messages waiting in the message queue of the current thread.</summary>
    public static void DoEvents()
    {
        while (PeekMessageW(out Msg message, 0, 0, 0, PM_REMOVE))
        {
            TranslateMessage(ref message);
            DispatchMessageW(ref message);
        }

        MessageWindowSynchronizationContext.CurrentThreadContext?.RunPending();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Msg
    {
        public nint Window;
        public uint Message;
        public nint WordParameter;
        public nint LongParameter;
        public uint Time;
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PeekMessageW(out Msg message, nint window, uint filterMin, uint filterMax, uint remove);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TranslateMessage(ref Msg message);

    [DllImport("user32.dll")]
    private static extern nint DispatchMessageW(ref Msg message);
}

/// <summary>
///  The synchronization context of the UI thread of the STA tests (in place of the WinForms
///  <c>WindowsFormsSynchronizationContext</c>): the posted callbacks run on the thread that created it, from any message
///  loop (e.g. <see cref="MessagePump.DoEvents"/>, a modal dialog), through a message-only window.
/// </summary>
public class MessageWindowSynchronizationContext : SynchronizationContext
{
    private const uint WM_APP_RUN = 0x8000 + 0x4745;
    private static readonly nint HWND_MESSAGE = -3;

    private static readonly WindowProcedure _windowProcedure = WindowProc;
    private static readonly Lazy<string> _className = new(RegisterClass);

    [ThreadStatic]
    private static MessageWindowSynchronizationContext? _currentThreadContext;

    private readonly ConcurrentQueue<(SendOrPostCallback Callback, object? State)> _queue = new();
    private readonly Thread _thread = Thread.CurrentThread;
    private readonly nint _window;

    public MessageWindowSynchronizationContext()
    {
        _window = CreateWindowExW(0, _className.Value, "", 0, 0, 0, 0, 0, HWND_MESSAGE, 0, GetModuleHandleW(null), 0);
        _currentThreadContext = this;
    }

    /// <summary>The context created on the current thread, if any.</summary>
    internal static MessageWindowSynchronizationContext? CurrentThreadContext => _currentThreadContext;

    public override void Post(SendOrPostCallback d, object? state)
    {
        _queue.Enqueue((d, state));
        if (_window != 0)
        {
            PostMessageW(_window, WM_APP_RUN, 0, 0);
        }
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        if (Thread.CurrentThread == _thread)
        {
            d(state);
            return;
        }

        using ManualResetEventSlim done = new();
        Exception? exception = null;
        Post(
            _ =>
            {
                try
                {
                    d(state);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                finally
                {
                    done.Set();
                }
            },
            null);
        done.Wait();
        if (exception is not null)
        {
            throw new TargetInvocationException(exception);
        }
    }

    public override SynchronizationContext CreateCopy() => this;

    /// <summary>Runs the callbacks posted so far.</summary>
    internal void RunPending()
    {
        if (Thread.CurrentThread != _thread)
        {
            return;
        }

        SynchronizationContext? previous = SynchronizationContext.Current;
        SetSynchronizationContext(this);
        try
        {
            while (_queue.TryDequeue(out (SendOrPostCallback Callback, object? State) item))
            {
                item.Callback(item.State);
            }
        }
        finally
        {
            SetSynchronizationContext(previous);
        }
    }

    private static nint WindowProc(nint window, uint message, nint wordParameter, nint longParameter)
    {
        if (message == WM_APP_RUN)
        {
            _currentThreadContext?.RunPending();
            return 0;
        }

        return DefWindowProcW(window, message, wordParameter, longParameter);
    }

    private static string RegisterClass()
    {
        const string name = "GitExtensionsTestSynchronizationContext";
        WindowClass windowClass = new()
        {
            Size = (uint)Marshal.SizeOf<WindowClass>(),
            WindowProcedure = Marshal.GetFunctionPointerForDelegate(_windowProcedure),
            Instance = GetModuleHandleW(null),
            ClassName = name,
        };
        RegisterClassExW(ref windowClass);
        return name;
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
    private static extern nint CreateWindowExW(uint extendedStyle, string className, string windowName, uint style, int x, int y, int width, int height,
        nint parent, nint menu, nint instance, nint parameter);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessageW(nint window, uint message, nint wordParameter, nint longParameter);

    [DllImport("user32.dll")]
    private static extern nint DefWindowProcW(nint window, uint message, nint wordParameter, nint longParameter);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandleW(string? moduleName);
}
