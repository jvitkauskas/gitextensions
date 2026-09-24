using System.Runtime.InteropServices;
using CommonTestUtils;

// NUnit runs a message loop while it waits for an async test on an STA thread only for the synchronization contexts of
// WinForms and WPF, which it recognizes by the name of their type (MessagePumpStrategy); with any other context it blocks
// the thread, and the continuations of the test, posted to the context, never run. These types have the names it looks
// for, so that it runs the message loop of MessageWindowSynchronizationContext (Application.Run) until the test is done
// (Application.Exit), as it ran the one of WinForms before WinForms was removed (docs/avalonia-port/PLAN.md, phase 8).
namespace System.Windows.Forms;

/// <summary>The <see cref="MessageWindowSynchronizationContext"/>, with the name by which NUnit runs its message loop.</summary>
internal sealed class WindowsFormsSynchronizationContext : MessageWindowSynchronizationContext
{
}

/// <summary>The message loop that NUnit runs (by reflection) while it waits for an async test.</summary>
internal static class Application
{
    private const uint WM_NULL = 0;

    [ThreadStatic]
    private static int _exitRequests;

    /// <summary>Processes the messages of the current thread until <see cref="Exit"/>.</summary>
    public static void Run()
    {
        while (_exitRequests == 0)
        {
            if (GetMessageW(out Msg message, 0, 0, 0) <= 0)
            {
                break;
            }

            TranslateMessage(ref message);
            DispatchMessageW(ref message);
        }

        _exitRequests = Math.Max(0, _exitRequests - 1);
    }

    /// <summary>Ends <see cref="Run"/> (called on the thread of the loop, by a continuation posted to its context).</summary>
    public static void Exit()
    {
        _exitRequests++;

        // Wakes GetMessage up.
        PostThreadMessageW(GetCurrentThreadId(), WM_NULL, 0, 0);
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
    private static extern int GetMessageW(out Msg message, nint window, uint filterMin, uint filterMax);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TranslateMessage(ref Msg message);

    [DllImport("user32.dll")]
    private static extern nint DispatchMessageW(ref Msg message);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostThreadMessageW(uint threadId, uint message, nint wordParameter, nint longParameter);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
}
