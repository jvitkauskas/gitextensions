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
    ///  The modifier of the shortcuts: Cmd on macOS, Ctrl elsewhere. The hotkeys stay stored with
    ///  <see cref="HotkeyBinding.Control"/>, which is Cmd on macOS (docs/avalonia-port/CROSS-PLATFORM.md, phase 5).
    /// </summary>
    public static KeyModifiers CommandModifier { get; } = OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;

    /// <summary>A shortcut written with Ctrl (e.g. "Ctrl+S"), with <see cref="CommandModifier"/> instead (Cmd+S on macOS).</summary>
    public static KeyGesture ToPlatformGesture(string gesture) => ToPlatformGesture(KeyGesture.Parse(gesture), CommandModifier);

    internal static KeyGesture ToPlatformGesture(KeyGesture gesture, KeyModifiers commandModifier)
        => commandModifier == KeyModifiers.Control || !gesture.KeyModifiers.HasFlag(KeyModifiers.Control)
            ? gesture
            : new KeyGesture(gesture.Key, (gesture.KeyModifiers & ~KeyModifiers.Control) | commandModifier);

    /// <summary>
    ///  Returns the WinForms key data (virtual-key code plus modifier flags), or 0 for keys hotkeys cannot use.
    /// </summary>
    public static int ToKeyData(Key key, KeyModifiers modifiers) => ToKeyData(key, modifiers, CommandModifier);

    /// <param name="commandModifier">
    ///  The modifier stored as <see cref="HotkeyBinding.Control"/>: with Cmd (macOS), the Ctrl key is not the one of the
    ///  shortcuts, so no hotkey has it (it stays for the text boxes and the terminal, e.g. Ctrl+A, Ctrl+C).
    /// </param>
    internal static int ToKeyData(Key key, KeyModifiers modifiers, KeyModifiers commandModifier)
    {
        int virtualKey = ToVirtualKey(key);
        if (virtualKey == 0 || (commandModifier != KeyModifiers.Control && modifiers.HasFlag(KeyModifiers.Control)))
        {
            return 0;
        }

        if (modifiers.HasFlag(KeyModifiers.Shift))
        {
            virtualKey |= HotkeyBinding.Shift;
        }

        if (modifiers.HasFlag(commandModifier))
        {
            virtualKey |= HotkeyBinding.Control;
        }

        if (modifiers.HasFlag(KeyModifiers.Alt))
        {
            virtualKey |= HotkeyBinding.Alt;
        }

        return virtualKey;
    }

    /// <summary>The gesture shown beside a menu item for a hotkey (the <c>ShortcutKeyDisplayString</c>), if the key maps.</summary>
    public static KeyGesture? ToKeyGesture(int keyData) => ToKeyGesture(keyData, CommandModifier);

    internal static KeyGesture? ToKeyGesture(int keyData, KeyModifiers commandModifier)
    {
        int virtualKey = keyData & 0xFFFF;
        Key? key = Enum.GetValues<Key>().Cast<Key?>().FirstOrDefault(k => k is { } candidate && ToVirtualKey(candidate) == virtualKey);
        if (key is null or Key.None)
        {
            return null;
        }

        KeyModifiers modifiers = KeyModifiers.None;
        if ((keyData & HotkeyBinding.Shift) != 0)
        {
            modifiers |= KeyModifiers.Shift;
        }

        if ((keyData & HotkeyBinding.Control) != 0)
        {
            modifiers |= commandModifier;
        }

        if ((keyData & HotkeyBinding.Alt) != 0)
        {
            modifiers |= KeyModifiers.Alt;
        }

        return new KeyGesture(key.Value, modifiers);
    }

    /// <summary>
    ///  As <c>GitExtensionsControl.IsTextEditKey</c>: the key types or edits text (hotkeys with it would prevent typing),
    ///  <paramref name="multiLine"/> also for the keys moving between lines.
    /// </summary>
    public static bool IsTextEditKey(int keyData, bool multiLine = false)
    {
        keyData &= ~HotkeyBinding.Shift;
        if (keyData is (>= 0x41 and <= 0x5A) or (>= 0x30 and <= 0x39) or (>= 0xBA and <= 0xE2) or 0x20 or 0x2D)
        {
            return true;
        }

        keyData &= ~HotkeyBinding.Control;
        return keyData switch
        {
            0x41 or 0x43 or 0x56 or 0x58 or 0x59 or 0x5A or 0x08 or 0x2E or 0x25 or 0x27 or 0x24 or 0x23 => true,
            0x26 or 0x28 or 0x21 or 0x22 => multiLine,
            _ => false,
        };
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
