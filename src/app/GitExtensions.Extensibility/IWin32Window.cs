namespace GitExtensions.Extensibility;

/// <summary>
///  A window that owns dialogs and message boxes, known by its native handle (HWND), whatever its UI framework (it replaces
///  the WinForms <c>System.Windows.Forms.IWin32Window</c>).
/// </summary>
public interface IWin32Window
{
    /// <summary>The native handle (HWND) of the window.</summary>
    nint Handle { get; }
}
