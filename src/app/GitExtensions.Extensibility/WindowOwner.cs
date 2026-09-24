namespace GitExtensions.Extensibility;

/// <summary>
///  The window that owns the dialogs and message boxes a plugin shows (plugin API v2): the native handle of a window of the
///  application, whatever its UI framework (a WinForms form or an Avalonia window).
/// </summary>
/// <remarks>
///  It replaces the WinForms <c>IWin32Window</c> in the plugin API (<see cref="Git.GitUIEventArgs.Owner"/>,
///  <see cref="PluginMessageBoxes"/>, the <see cref="Git.GitUICommandsExtensions"/> overloads);
///  <see cref="WindowOwnerExtensions"/> converts it from and to the <see cref="IWin32Window"/> of the host code.
/// </remarks>
/// <param name="Handle">The native handle (HWND) of the window, or 0 for none.</param>
public readonly record struct WindowOwner(nint Handle)
{
    /// <summary>No owner: the dialog is owned by the active window of the application, if any.</summary>
    public static WindowOwner None => default;

    /// <summary>Whether there is no owner window.</summary>
    public bool IsNone => Handle == 0;
}
