using GitExtUtils.GitUI.Theming;

namespace GitExtUtilsTests.GitUI;

/// <summary>The system colors of the default theme off Windows (docs/avalonia-port/CROSS-PLATFORM.md, phase 2).</summary>
public sealed class SystemColorDefaultsTests
{
    [TestCase(KnownColor.Control, 0xF0F0F0)]
    [TestCase(KnownColor.Window, 0xFFFFFF)]
    [TestCase(KnownColor.Highlight, 0x0078D7)]
    [TestCase(KnownColor.GrayText, 0x6D6D6D)]
    public void Off_Windows_the_colors_are_those_of_the_light_theme_of_Windows(KnownColor systemColor, int rgb)
    {
        Color color = SystemColorDefaults.Get(systemColor, onWindows: false);

        color.ToArgb().Should().Be(unchecked((int)0xFF000000) | rgb);
        color.IsSystemColor.Should().BeFalse("the theme keeps fixed values");
    }

    [TestCase(KnownColor.Control, 0x202020, false)]
    [TestCase(KnownColor.Window, 0x323232, false)]
    [TestCase(KnownColor.WindowText, 0xF0F0F0, false)]
    [TestCase(KnownColor.ControlText, 0xFFFFFF, false)]
    [TestCase(KnownColor.Control, 0x202020, true)]
    [TestCase(KnownColor.Window, 0x323232, true)]
    [TestCase(KnownColor.WindowText, 0xF0F0F0, true)]
    [TestCase(KnownColor.ControlText, 0xFFFFFF, true)]
    public void A_dark_theme_gets_dark_system_colors_on_every_platform(KnownColor systemColor, int rgb, bool onWindows)
    {
        Color color = SystemColorDefaults.Get(systemColor, onWindows, isDarkTheme: true);

        color.ToArgb().Should().Be(unchecked((int)0xFF000000) | rgb);
    }

    [Test]
    public void Off_Windows_a_light_theme_keeps_the_colors_of_the_light_theme_of_Windows()
    {
        SystemColorDefaults.Get(KnownColor.Control, onWindows: false, isDarkTheme: false).ToArgb().Should().Be(unchecked((int)0xFFF0F0F0));
    }

    [Test]
    [Platform(Include = "Win")]
    public void On_Windows_a_light_theme_keeps_the_colors_of_the_system()
    {
        SystemColorDefaults.Get(KnownColor.Control, onWindows: true, isDarkTheme: false).ToArgb().Should().Be(SystemColors.Control.ToArgb());
    }

    [Test]
    public void A_dark_theme_fills_missing_backgrounds_and_preserves_explicit_colors()
    {
        Theme theme = new(
            new Dictionary<AppColor, Color> { [AppColor.PanelBackground] = Color.FromArgb(32, 32, 32) },
            new Dictionary<KnownColor, Color> { [KnownColor.Window] = Color.FromArgb(40, 40, 40) },
            ThemeId.DefaultDark);

        theme.GetNonEmptyColor(KnownColor.Control).ToArgb().Should().Be(unchecked((int)0xFF202020));
        theme.GetNonEmptyColor(KnownColor.ControlText).ToArgb().Should().Be(Color.White.ToArgb());
        theme.GetNonEmptyColor(KnownColor.Window).ToArgb().Should().Be(Color.FromArgb(40, 40, 40).ToArgb());
    }

    [Test]
    [Platform(Include = "Win")]
    public void On_Windows_the_colors_are_those_of_the_system()
    {
        SystemColorDefaults.Get(KnownColor.Control).ToArgb().Should().Be(SystemColors.Control.ToArgb());
    }
}
