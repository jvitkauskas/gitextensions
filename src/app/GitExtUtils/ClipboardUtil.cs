using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace GitExtUtils;

/// <summary>The clipboard of the UI off Windows (docs/avalonia-port/CROSS-PLATFORM.md, phase 2), set by the application.</summary>
public interface IClipboardBackend
{
    bool TrySetText(string text);

    /// <summary>Sets the HTML of the clipboard and its text (or only the text, if the system has no HTML format).</summary>
    bool TrySetHtml(string html, string text);

    bool TryGetText([NotNullWhen(returnValue: true)] out string? text);
}

/// <summary>
///  The text of the clipboard: of Windows (without WinForms: docs/avalonia-port/PLAN.md, phase 8), else of the UI of the
///  application (<see cref="Backend"/>).
/// </summary>
public static class ClipboardUtil
{
    private const uint CF_UNICODETEXT = 13;
    private const uint GMEM_MOVEABLE = 0x0002;

    // Setting clipboard data can fail, as applications can lock the clipboard: retry as the WinForms Clipboard.SetDataObject
    // did (see https://github.com/gitextensions/gitextensions/issues/4542).
    private const int RetryTimes = 5;
    private const int RetryDelay = 100;

    /// <summary>The clipboard off Windows; set by the application at startup. Without it the clipboard is not available there.</summary>
    public static IClipboardBackend? Backend { get; set; }

    public static bool TrySetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return OperatingSystem.IsWindows() ? TrySetTextNative(text) : Backend?.TrySetText(text) ?? false;
    }

    /// <summary>Sets the HTML format of the clipboard (CF_HTML, e.g. for Word) and its text.</summary>
    /// <param name="html">The CF_HTML text, with its header.</param>
    public static bool TrySetHtml(string html, string text)
    {
        ArgumentNullException.ThrowIfNull(html);
        ArgumentNullException.ThrowIfNull(text);
        return OperatingSystem.IsWindows() ? TrySetHtmlNative(html, text) : Backend?.TrySetHtml(html, text) ?? false;
    }

    /// <summary>Reads the text of the clipboard, if it has text (e.g. for the plugins, which need no UI type then).</summary>
    public static bool TryGetText([NotNullWhen(returnValue: true)] out string? text)
    {
        text = null;
        return OperatingSystem.IsWindows() ? TryGetTextNative(out text) : Backend?.TryGetText(out text) ?? false;
    }

    [SupportedOSPlatform("windows")]
    private static bool TrySetTextNative(string text)
    {
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

    [SupportedOSPlatform("windows")]
    private static bool TrySetHtmlNative(string html, string text)
    {
        uint htmlFormat = RegisterClipboardFormatW("HTML Format");
        if (htmlFormat == 0 || !TryOpenClipboard())
        {
            return false;
        }

        try
        {
            if (!EmptyClipboard())
            {
                return false;
            }

            // CF_HTML is UTF-8 text; both are null-terminated.
            byte[] htmlBytes = System.Text.Encoding.UTF8.GetBytes(html + '\0');
            return SetData(htmlFormat, htmlBytes) && SetData(CF_UNICODETEXT, System.Text.Encoding.Unicode.GetBytes(text + '\0'));
        }
        finally
        {
            CloseClipboard();
        }

        static bool SetData(uint format, byte[] data)
        {
            nint memory = GlobalAlloc(GMEM_MOVEABLE, (nuint)data.Length);
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
                Marshal.Copy(data, 0, target, data.Length);
            }
            finally
            {
                GlobalUnlock(memory);
            }

            if (SetClipboardData(format, memory) == 0)
            {
                GlobalFree(memory);
                return false;
            }

            return true;
        }
    }

    [SupportedOSPlatform("windows")]
    private static bool TryGetTextNative([NotNullWhen(returnValue: true)] out string? text)
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

    [SupportedOSPlatform("windows")]
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

    [SupportedOSPlatform("windows")]
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenClipboard(nint newOwner);

    [SupportedOSPlatform("windows")]
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseClipboard();

    [SupportedOSPlatform("windows")]
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EmptyClipboard();

    [SupportedOSPlatform("windows")]
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsClipboardFormatAvailable(uint format);

    [SupportedOSPlatform("windows")]
    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetClipboardData(uint format, nint memory);

    [SupportedOSPlatform("windows")]
    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint GetClipboardData(uint format);

    [SupportedOSPlatform("windows")]
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint RegisterClipboardFormatW(string format);

    [SupportedOSPlatform("windows")]
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint GlobalAlloc(uint flags, nuint bytes);

    [SupportedOSPlatform("windows")]
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint GlobalFree(nint memory);

    [SupportedOSPlatform("windows")]
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint GlobalLock(nint memory);

    [SupportedOSPlatform("windows")]
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalUnlock(nint memory);
}
