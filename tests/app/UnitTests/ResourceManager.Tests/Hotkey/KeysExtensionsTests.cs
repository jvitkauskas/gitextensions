using ResourceManager.Hotkey;

namespace ResourceManagerTests.Hotkey;

[TestFixture]
public sealed class KeysExtensionsTests
{
    [TestCase(Keys.Control | Keys.Shift | Keys.C, "⇧⌘C")]
    [TestCase(Keys.Control | Keys.Alt | Keys.Shift | Keys.K, "⌥⇧⌘K")]
    [TestCase(Keys.Control | Keys.Oemcomma, "⌘,")]
    [TestCase(Keys.Alt | Keys.Left, "⌥Left")]
    [TestCase(Keys.F5, "F5")]
    [TestCase(Keys.Control | Keys.D1, "⌘1")]
    public void ToMacOSText_shows_the_symbols_of_macOS_with_Cmd_for_Control(Keys key, string expected)
    {
        key.ToMacOSText().Should().Be(expected);
    }

    [Test]
    public void ToText_keeps_the_text_that_is_parsed_as_gestures()
    {
        (Keys.Control | Keys.Shift | Keys.C).ToText().Should().Be("Ctrl+Shift+C");
    }
}
