using GitExtUtils.GitUI.Theming;

namespace GitExtUtilsTests.GitUI;

/// <summary>The dark mode of the system off Windows, from the output of its tools (docs/avalonia-port/CROSS-PLATFORM.md, phase 2).</summary>
public sealed class SystemThemeTests
{
    [TestCase("Dark\n", null, null, true)] // defaults read -g AppleInterfaceStyle
    [TestCase(null, null, null, false)] // the key is missing in the light appearance: no output
    [TestCase(null, "'prefer-dark'\n", "'Adwaita'\n", true)] // gsettings get org.gnome.desktop.interface color-scheme
    [TestCase(null, "'default'\n", "'Adwaita'\n", false)]
    [TestCase(null, "'prefer-light'\n", "'Yaru'\n", false)]
    [TestCase(null, "'default'\n", "'Adwaita-dark'\n", true)] // before color-scheme, the GTK theme told it
    [TestCase(null, null, "'Yaru-dark'\n", true)]
    public void IsDarkModeOutput(string? macOSAppearance, string? gnomeColorScheme, string? gtkTheme, bool expected)
    {
        SystemTheme.IsDarkModeOutput(macOSAppearance, gnomeColorScheme, gtkTheme).Should().Be(expected);
    }
}
