namespace GitExtensions.Extensibility;

/// <summary>
///  The window that owns the dialogs and message boxes a plugin shows (plugin API v2): the native handle of a window of the
///  application, whatever its UI framework (a WinForms form or an Avalonia window).
/// </summary>
/// <remarks>
///  <para>
///   It replaces <see cref="IWin32Window"/> in the plugin API (<see cref="Git.GitUIEventArgs.Owner"/>,
///   <see cref="PluginMessageBoxes"/>, the <see cref="Git.GitUICommandsExtensions"/> overloads), so that plugins need no
///   WinForms type.
///  </para>
///  <para>
///   <see cref="WindowOwnerWinFormsExtensions"/> converts it from and to <see cref="IWin32Window"/> for the WinForms code
///   (the v1 API and the WinForms fallback forms of the plugins).
///  </para>
/// </remarks>
/// <param name="Handle">The native handle (HWND) of the window, or 0 for none.</param>
public readonly record struct WindowOwner(nint Handle)
{
    /// <summary>No owner: the dialog is owned by the active window of the application, if any.</summary>
    public static WindowOwner None => default;

    /// <summary>Whether there is no owner window.</summary>
    public bool IsNone => Handle == 0;
}
