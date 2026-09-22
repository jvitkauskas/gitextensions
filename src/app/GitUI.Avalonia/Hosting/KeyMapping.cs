using Avalonia.Input;
using GitUI.Presentation.Services;

namespace GitUI.Avalonia.Hosting;

/// <summary>
///  Converts Avalonia key events to the WinForms <c>Keys</c> encoding in which hotkeys are configured
///  (see <see cref="HotkeyBinding.KeyData"/>).
/// </summary>
internal static class KeyMapping
{
    /// <summary>
    ///  Returns the WinForms key data (virtual-key code plus modifier flags), or 0 for keys hotkeys cannot use.
    /// </summary>
    public static int ToKeyData(Key key, KeyModifiers modifiers)
    {
        int virtualKey = ToVirtualKey(key);
        if (virtualKey == 0)
        {
            return 0;
        }

        if (modifiers.HasFlag(KeyModifiers.Shift))
        {
            virtualKey |= HotkeyBinding.Shift;
        }

        if (modifiers.HasFlag(KeyModifiers.Control))
        {
            virtualKey |= HotkeyBinding.Control;
        }

        if (modifiers.HasFlag(KeyModifiers.Alt))
        {
            virtualKey |= HotkeyBinding.Alt;
        }

        return virtualKey;
    }

    /// <summary>Returns the Win32 virtual-key code of <paramref name="key"/>, or 0 if it has none here.</summary>
    public static int ToVirtualKey(Key key)
    {
        if (key is >= Key.A and <= Key.Z)
        {
            return 0x41 + (key - Key.A);
        }

        if (key is >= Key.D0 and <= Key.D9)
        {
            return 0x30 + (key - Key.D0);
        }

        if (key is >= Key.NumPad0 and <= Key.NumPad9)
        {
            return 0x60 + (key - Key.NumPad0);
        }

        if (key is >= Key.F1 and <= Key.F24)
        {
            return 0x70 + (key - Key.F1);
        }

        return key switch
        {
            Key.Back => 0x08,
            Key.Tab => 0x09,
            Key.Enter => 0x0D,
            Key.Pause => 0x13,
            Key.Escape => 0x1B,
            Key.Space => 0x20,
            Key.PageUp => 0x21,
            Key.PageDown => 0x22,
            Key.End => 0x23,
            Key.Home => 0x24,
            Key.Left => 0x25,
            Key.Up => 0x26,
            Key.Right => 0x27,
            Key.Down => 0x28,
            Key.Insert => 0x2D,
            Key.Delete => 0x2E,
            Key.Apps => 0x5D,
            Key.Multiply => 0x6A,
            Key.Add => 0x6B,
            Key.Subtract => 0x6D,
            Key.Decimal => 0x6E,
            Key.Divide => 0x6F,
            Key.OemSemicolon => 0xBA,
            Key.OemPlus => 0xBB,
            Key.OemComma => 0xBC,
            Key.OemMinus => 0xBD,
            Key.OemPeriod => 0xBE,
            Key.OemQuestion => 0xBF,
            Key.OemTilde => 0xC0,
            Key.OemOpenBrackets => 0xDB,
            Key.OemPipe => 0xDC,
            Key.OemCloseBrackets => 0xDD,
            Key.OemQuotes => 0xDE,
            Key.OemBackslash => 0xE2,
            _ => 0,
        };
    }
}
