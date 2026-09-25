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

    [Test]
    [Platform(Include = "Win")]
    public void On_Windows_the_colors_are_those_of_the_system()
    {
        SystemColorDefaults.Get(KnownColor.Control).ToArgb().Should().Be(SystemColors.Control.ToArgb());
    }
}
