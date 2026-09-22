namespace GitUI.Presentation.Services;

/// <summary>
///  A native (Win32) view that a host control embeds as a child window, e.g. a terminal emulator.
///  Lets Avalonia views host existing WinForms controls during the port.
/// </summary>
public interface IEmbeddedNativeView
{
    /// <summary>Parents the view into <paramref name="parentWindow"/> and returns its native handle.</summary>
    nint Attach(nint parentWindow);

    /// <summary>Releases the view from its parent (the host no longer shows it).</summary>
    void Detach();
}
