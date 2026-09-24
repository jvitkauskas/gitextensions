using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace GitExtUtils;

/// <summary>The text of the Windows clipboard (without WinForms: docs/avalonia-port/PLAN.md, phase 8).</summary>
public static class ClipboardUtil
{
    private const uint CF_UNICODETEXT = 13;
    private const uint GMEM_MOVEABLE = 0x0002;

    // Setting clipboard data can fail, as applications can lock the clipboard: retry as the WinForms Clipboard.SetDataObject
    // did (see https://github.com/gitextensions/gitextensions/issues/4542).
    private const int RetryTimes = 5;
    private const int RetryDelay = 100;

    public static bool TrySetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (!TryOpenClipboard())
        {
            // The clipboard is being used by another process
            return false;
        }

        try
        {
            if (!EmptyClipboard())
            {
                return false;
            }

            // The data stays on the clipboard, even after Git Extensions exits: the clipboard owns the memory once set.
            int size = (text.Length + 1) * sizeof(char);
            nint memory = GlobalAlloc(GMEM_MOVEABLE, (nuint)size);
            if (memory == 0)
            {
                return false;
            }

            nint target = GlobalLock(memory);
            if (target == 0)
            {
                GlobalFree(memory);
                return false;
            }

            try
            {
                Marshal.Copy(text.ToCharArray(), 0, target, text.Length);
                Marshal.WriteInt16(target, text.Length * sizeof(char), 0);
            }
            finally
            {
                GlobalUnlock(memory);
            }

            if (SetClipboardData(CF_UNICODETEXT, memory) == 0)
            {
                GlobalFree(memory);
                return false;
            }

            return true;
        }
        finally
        {
            CloseClipboard();
        }
    }

    /// <summary>Reads the text of the clipboard, if it has text (e.g. for the plugins, which need no UI type then).</summary>
    public static bool TryGetText([NotNullWhen(returnValue: true)] out string? text)
    {
        text = null;
        if (!IsClipboardFormatAvailable(CF_UNICODETEXT) || !TryOpenClipboard())
        {
            return false;
        }

        try
        {
            nint memory = GetClipboardData(CF_UNICODETEXT);
            if (memory == 0)
            {
                return false;
            }

            nint source = GlobalLock(memory);
            if (source == 0)
            {
                return false;
            }

            try
            {
                text = Marshal.PtrToStringUni(source) ?? "";
                return true;
            }
            finally
            {
                GlobalUnlock(memory);
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    private static bool TryOpenClipboard()
    {
        for (int attempt = 0; ; attempt++)
        {
            if (OpenClipboard(0))
            {
                return true;
            }

            if (attempt == RetryTimes)
            {
                return false;
            }

            Thread.Sleep(RetryDelay);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenClipboard(nint newOwner);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsClipboardFormatAvailable(uint format);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetClipboardData(uint format, nint memory);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint GetClipboardData(uint format);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint GlobalAlloc(uint flags, nuint bytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint GlobalFree(nint memory);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint GlobalLock(nint memory);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalUnlock(nint memory);
}
