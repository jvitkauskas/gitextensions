namespace GitExtUtils.GitUI.Theming;

/// <summary>
///  The values of the system colors (<see cref="KnownColor"/>) of the default theme. On Windows those of the system;
///  elsewhere .NET has fixed values of Windows XP (a beige <see cref="KnownColor.Control"/>), so those of the default
///  light theme of Windows 10 and 11 are used instead, for the same look (docs/avalonia-port/CROSS-PLATFORM.md, phase 2).
/// </summary>
public static class SystemColorDefaults
{
    private static readonly Dictionary<KnownColor, int> _windowsDefaults = new()
    {
        [KnownColor.ActiveBorder] = 0xB4B4B4,
        [KnownColor.ActiveCaption] = 0x99B4D1,
        [KnownColor.ActiveCaptionText] = 0x000000,
        [KnownColor.AppWorkspace] = 0xABABAB,
        [KnownColor.ButtonFace] = 0xF0F0F0,
        [KnownColor.ButtonHighlight] = 0xFFFFFF,
        [KnownColor.ButtonShadow] = 0xA0A0A0,
        [KnownColor.Control] = 0xF0F0F0,
        [KnownColor.ControlDark] = 0xA0A0A0,
        [KnownColor.ControlDarkDark] = 0x696969,
        [KnownColor.ControlLight] = 0xE3E3E3,
        [KnownColor.ControlLightLight] = 0xFFFFFF,
        [KnownColor.ControlText] = 0x000000,
        [KnownColor.Desktop] = 0x000000,
        [KnownColor.GradientActiveCaption] = 0xB9D1EA,
        [KnownColor.GradientInactiveCaption] = 0xD7E4F2,
        [KnownColor.GrayText] = 0x6D6D6D,
        [KnownColor.Highlight] = 0x0078D7,
        [KnownColor.HighlightText] = 0xFFFFFF,
        [KnownColor.HotTrack] = 0x0066CC,
        [KnownColor.InactiveBorder] = 0xF4F7FC,
        [KnownColor.InactiveCaption] = 0xBFCDDB,
        [KnownColor.InactiveCaptionText] = 0x000000,
        [KnownColor.Info] = 0xFFFFE1,
        [KnownColor.InfoText] = 0x000000,
        [KnownColor.Menu] = 0xF0F0F0,
        [KnownColor.MenuBar] = 0xF0F0F0,
        [KnownColor.MenuHighlight] = 0x3399FF,
        [KnownColor.MenuText] = 0x000000,
        [KnownColor.ScrollBar] = 0xC8C8C8,
        [KnownColor.Window] = 0xFFFFFF,
        [KnownColor.WindowFrame] = 0x646464,
        [KnownColor.WindowText] = 0x000000,
    };

    /// <summary>
    ///  The system colors of the dark color mode of WinForms (<c>SystemColorMode.Dark</c>, listed in <c>Themes/dark.css</c>),
    ///  which the dark themes relied on for the colors they do not set. Off Windows there is no such mode, so a dark theme
    ///  would get the light values above (light backgrounds behind the light text of the theme).
    /// </summary>
    private static readonly Dictionary<KnownColor, int> _windowsDarkDefaults = new()
    {
        [KnownColor.ActiveBorder] = 0x464646,
        [KnownColor.ActiveCaption] = 0x3C5F78,
        [KnownColor.ActiveCaptionText] = 0xFFFFFF,
        [KnownColor.AppWorkspace] = 0x3C3C3C,
        [KnownColor.ButtonFace] = 0x202020,
        [KnownColor.ButtonHighlight] = 0x101010,
        [KnownColor.ButtonShadow] = 0x464646,
        [KnownColor.Control] = 0x202020,
        [KnownColor.ControlDark] = 0x4A4A4A,
        [KnownColor.ControlDarkDark] = 0x5A5A5A,
        [KnownColor.ControlLight] = 0x2E2E2E,
        [KnownColor.ControlLightLight] = 0x1F1F1F,
        [KnownColor.ControlText] = 0xFFFFFF,
        [KnownColor.Desktop] = 0x101010,
        [KnownColor.GradientActiveCaption] = 0x416482,
        [KnownColor.GradientInactiveCaption] = 0x557396,
        [KnownColor.GrayText] = 0x969696,
        [KnownColor.Highlight] = 0x2864B4,
        [KnownColor.HighlightText] = 0x000000,
        [KnownColor.HotTrack] = 0x2D5FAF,
        [KnownColor.InactiveBorder] = 0x3C3F41,
        [KnownColor.InactiveCaption] = 0x374B5A,
        [KnownColor.InactiveCaptionText] = 0xBEBEBE,
        [KnownColor.Info] = 0x50503C,
        [KnownColor.InfoText] = 0xBEBEBE,
        [KnownColor.Menu] = 0x373737,
        [KnownColor.MenuBar] = 0x373737,
        [KnownColor.MenuHighlight] = 0x2A80D2,
        [KnownColor.MenuText] = 0xF0F0F0,
        [KnownColor.ScrollBar] = 0x505050,
        [KnownColor.Window] = 0x323232,
        [KnownColor.WindowFrame] = 0x282828,
        [KnownColor.WindowText] = 0xF0F0F0,
    };

    /// <summary>The value of a system color (as a fixed color, not a system color that follows changes).</summary>
    public static Color Get(KnownColor systemColor) => Get(systemColor, OperatingSystem.IsWindows());

    /// <summary>
    ///  The value of a system color for a theme that is dark or not: off Windows those of the dark color mode of WinForms
    ///  for a dark theme; on Windows those of the system, as before.
    /// </summary>
    internal static Color Get(KnownColor systemColor, bool onWindows, bool isDarkTheme)
        => !onWindows && isDarkTheme && _windowsDarkDefaults.TryGetValue(systemColor, out int rgb)
            ? Color.FromArgb(unchecked((int)0xFF000000) | rgb)
            : Get(systemColor, onWindows);

    internal static Color Get(KnownColor systemColor, bool onWindows)
        => !onWindows && _windowsDefaults.TryGetValue(systemColor, out int rgb)
            ? Color.FromArgb(unchecked((int)0xFF000000) | rgb)
            : Color.FromArgb(Color.FromKnownColor(systemColor).ToArgb());
}
