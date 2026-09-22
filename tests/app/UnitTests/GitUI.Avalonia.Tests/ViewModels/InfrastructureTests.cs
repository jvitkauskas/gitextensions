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
        KeyMapping.ToKeyData(key, modifiers).Should().Be(expected);
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
