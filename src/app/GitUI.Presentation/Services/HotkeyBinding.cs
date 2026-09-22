namespace GitUI.Presentation.Services;

/// <summary>
///  A configured hotkey: the command it executes and its key combination.
/// </summary>
/// <param name="CommandCode">The command code, as in <c>ResourceManager.Hotkey.HotkeyCommand.CommandCode</c>.</param>
/// <param name="KeyData">
///  The key combination in WinForms <c>Keys</c> encoding (virtual-key code | Shift 0x10000 | Control 0x20000 | Alt 0x40000),
///  which is how the hotkey settings are stored.
/// </param>
public sealed record HotkeyBinding(int CommandCode, int KeyData)
{
    public const int Shift = 0x10000;
    public const int Control = 0x20000;
    public const int Alt = 0x40000;
}
