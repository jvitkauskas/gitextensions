using Avalonia.Input;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitUI.AvaloniaTests.ViewModels;

[TestFixture]
public sealed class InfrastructureTests
{
    [TestCase("&Abort", "_Abort", "Abort")]
    [TestCase("Show &password input", "Show _password input", "Show password input")]
    [TestCase("Save && close", "Save & close", "Save & close")]
    [TestCase("snake_case &name", "snake__case _name", "snake_case name")]
    [TestCase("OK", "OK", "OK")]
    public void WinForms_mnemonics_become_Avalonia_access_keys(string winForms, string accessKeyText, string plainText)
    {
        TranslatedText text = new TestStrings(winForms).Text;

        text.AccessKeyText.Should().Be(accessKeyText);
        text.PlainText.Should().Be(plainText);
    }

    // Values of System.Windows.Forms.Keys, in which hotkeys are stored.
    [TestCase(Key.A, KeyModifiers.None, 0x41)]
    [TestCase(Key.Z, KeyModifiers.Control, 0x5A | 0x20000)]
    [TestCase(Key.D5, KeyModifiers.Shift, 0x35 | 0x10000)]
    [TestCase(Key.F12, KeyModifiers.Alt, 0x7B | 0x40000)]
    [TestCase(Key.NumPad3, KeyModifiers.None, 0x63)]
    [TestCase(Key.Delete, KeyModifiers.Control | KeyModifiers.Shift, 0x2E | 0x20000 | 0x10000)]
    [TestCase(Key.OemPlus, KeyModifiers.Control, 0xBB | 0x20000)]
    [TestCase(Key.LeftShift, KeyModifiers.Shift, 0)]
    public void Keys_map_to_the_WinForms_encoding_of_hotkeys(Key key, KeyModifiers modifiers, int expected)
    {
        KeyMapping.ToKeyData(key, modifiers, commandModifier: KeyModifiers.Control).Should().Be(expected);
    }

    // On macOS Cmd is the Control of the stored hotkeys, and Ctrl is no hotkey (it stays for the text boxes and the terminal).
    [TestCase(Key.Z, KeyModifiers.Meta, 0x5A | 0x20000)]
    [TestCase(Key.Delete, KeyModifiers.Meta | KeyModifiers.Shift, 0x2E | 0x20000 | 0x10000)]
    [TestCase(Key.F12, KeyModifiers.Alt, 0x7B | 0x40000)]
    [TestCase(Key.Z, KeyModifiers.Control, 0)]
    [TestCase(Key.A, KeyModifiers.Control | KeyModifiers.Meta, 0)]
    public void With_Cmd_as_the_command_modifier_Cmd_is_stored_as_Control(Key key, KeyModifiers modifiers, int expected)
    {
        KeyMapping.ToKeyData(key, modifiers, commandModifier: KeyModifiers.Meta).Should().Be(expected);
    }

    [Test]
    public void With_Cmd_as_the_command_modifier_the_gestures_show_Cmd()
    {
        KeyMapping.ToKeyGesture(0x53 | 0x20000 | 0x10000, commandModifier: KeyModifiers.Meta).Should().Be(new KeyGesture(Key.S, KeyModifiers.Meta | KeyModifiers.Shift));
        KeyMapping.ToKeyGesture(0x53 | 0x20000, commandModifier: KeyModifiers.Control).Should().Be(new KeyGesture(Key.S, KeyModifiers.Control));
        KeyMapping.ToPlatformGesture(KeyGesture.Parse("Ctrl+Shift+C"), KeyModifiers.Meta).Should().Be(new KeyGesture(Key.C, KeyModifiers.Meta | KeyModifiers.Shift));
        KeyMapping.ToPlatformGesture(KeyGesture.Parse("Ctrl+D1"), KeyModifiers.Control).Should().Be(new KeyGesture(Key.D1, KeyModifiers.Control));
        KeyMapping.ToPlatformGesture(KeyGesture.Parse("F3"), KeyModifiers.Meta).Should().Be(new KeyGesture(Key.F3));
    }

    // The menu bar of macOS has no access keys; the text on the right (the branch of a recent repository) follows.
    [TestCase("_Start", null, "Start")]
    [TestCase("Commit && _push", null, "Commit && push")]
    [TestCase("a__b", null, "a_b")]
    [TestCase("1: /repo", "main", "1: /repo    main")]
    public void The_texts_of_the_macOS_menu_bar_have_no_access_keys(string header, string? shortcut, string expected)
    {
        GitUI.Avalonia.CommandsDialogs.BrowseDialog.BrowseWindow.ToNativeHeader(header, shortcut).Should().Be(expected);
    }

    [Test]
    public void HotkeyBinding_modifier_flags_match_WinForms()
    {
        HotkeyBinding.Shift.Should().Be(0x10000);
        HotkeyBinding.Control.Should().Be(0x20000);
        HotkeyBinding.Alt.Should().Be(0x40000);
    }

    private sealed class TestStrings : ViewStrings
    {
        public TestStrings(string text)
            : base("Test")
        {
            Text = Add("item", "Text", text);
        }

        public TranslatedText Text { get; }
    }
}
