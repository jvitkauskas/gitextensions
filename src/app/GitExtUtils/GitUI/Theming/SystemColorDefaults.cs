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

    /// <summary>The value of a system color (as a fixed color, not a system color that follows changes).</summary>
    public static Color Get(KnownColor systemColor) => Get(systemColor, OperatingSystem.IsWindows());

    internal static Color Get(KnownColor systemColor, bool onWindows)
        => !onWindows && _windowsDefaults.TryGetValue(systemColor, out int rgb)
            ? Color.FromArgb(unchecked((int)0xFF000000) | rgb)
            : Color.FromArgb(Color.FromKnownColor(systemColor).ToArgb());
}
