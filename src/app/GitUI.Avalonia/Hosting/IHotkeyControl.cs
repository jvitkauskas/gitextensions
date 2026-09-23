namespace GitUI.Avalonia.Hosting;

/// <summary>
///  A control with its own hotkeys (as a WinForms <c>GitExtensionsControl</c>): <see cref="DialogWindow"/> asks the controls
///  that contain the focus first, from the focused one up, then executes its own hotkeys (as <c>ProcessCmdKey</c>).
/// </summary>
public interface IHotkeyControl
{
    /// <summary>Executes the hotkey of the key combination, if the control has one that applies.</summary>
    /// <param name="keyData">The key combination in WinForms <c>Keys</c> encoding (see <see cref="Presentation.Services.HotkeyBinding"/>).</param>
    /// <returns><see langword="true"/> if a command was executed (the key is then handled).</returns>
    bool ProcessHotkey(int keyData);
}
