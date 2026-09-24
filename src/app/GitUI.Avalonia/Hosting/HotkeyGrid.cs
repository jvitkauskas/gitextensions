using Avalonia.Controls;

namespace GitUI.Avalonia.Hosting;

/// <summary>A grid with the hotkeys of a part of a window (e.g. a tab as a WinForms <c>GitModuleControl</c>), handled by <see cref="HotkeyHandler"/>.</summary>
public class HotkeyGrid : Grid, IHotkeyControl
{
    /// <summary>Executes the hotkey of the key combination (in WinForms <c>Keys</c> encoding); returns whether one was executed.</summary>
    public Func<int, bool>? HotkeyHandler { get; set; }

    public bool ProcessHotkey(int keyData) => HotkeyHandler?.Invoke(keyData) ?? false;
}
