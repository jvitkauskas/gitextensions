using GitExtUtils.GitUI.Theming;
using XTerm.Options;

namespace GitUI.ConsoleEmulation.BuiltIn;

/// <summary>A color scheme of the built-in terminal.</summary>
internal sealed record BuiltInTerminalTheme(string Name, ThemeOptions Options)
{
    private static readonly BuiltInTerminalTheme Tomorrow = new("Tomorrow", new ThemeOptions
    {
        Foreground = "#4D4D4C", Background = "#FFFFFF", Cursor = "#4D4D4C", Selection = "#D6D6D6",
        Black = "#000000", Red = "#C82829", Green = "#718C00", Yellow = "#EAB700",
        Blue = "#4271AE", Magenta = "#8959A8", Cyan = "#3E999F", White = "#D6D6D6",
        BrightBlack = "#8E908C", BrightRed = "#C82829", BrightGreen = "#718C00", BrightYellow = "#EAB700",
        BrightBlue = "#4271AE", BrightMagenta = "#8959A8", BrightCyan = "#3E999F", BrightWhite = "#FFFFFF",
    });

    private static readonly BuiltInTerminalTheme TomorrowNight = new("Tomorrow Night", new ThemeOptions
    {
        Foreground = "#C5C8C6", Background = "#1D1F21", Cursor = "#C5C8C6", Selection = "#373B41",
        Black = "#1D1F21", Red = "#CC6666", Green = "#B5BD68", Yellow = "#F0C674",
        Blue = "#81A2BE", Magenta = "#B294BB", Cyan = "#8ABEB7", White = "#C5C8C6",
        BrightBlack = "#969896", BrightRed = "#CC6666", BrightGreen = "#B5BD68", BrightYellow = "#F0C674",
        BrightBlue = "#81A2BE", BrightMagenta = "#B294BB", BrightCyan = "#8ABEB7", BrightWhite = "#FFFFFF",
    });

    private static readonly BuiltInTerminalTheme SolarizedLight = new("Solarized Light", Solarized("#657B83", "#FDF6E3", "#EEE8D5"));

    private static readonly BuiltInTerminalTheme SolarizedDark = new("Solarized Dark", Solarized("#839496", "#002B36", "#073642"));

    /// <summary>The themes, by name.</summary>
    public static IReadOnlyList<BuiltInTerminalTheme> All { get; } = [Tomorrow, TomorrowNight, SolarizedLight, SolarizedDark];

    /// <summary>The theme named <paramref name="name"/>, or the one that suits the theme of the application.</summary>
    public static BuiltInTerminalTheme Get(string? name)
        => All.FirstOrDefault(theme => string.Equals(theme.Name, name, StringComparison.OrdinalIgnoreCase))
            ?? (ColorHelper.IsDarkTheme ? TomorrowNight : Tomorrow);

    private static ThemeOptions Solarized(string foreground, string background, string selection) => new()
    {
        Foreground = foreground, Background = background, Cursor = foreground, Selection = selection,
        Black = "#073642", Red = "#DC322F", Green = "#859900", Yellow = "#B58900",
        Blue = "#268BD2", Magenta = "#D33682", Cyan = "#2AA198", White = "#EEE8D5",
        BrightBlack = "#002B36", BrightRed = "#CB4B16", BrightGreen = "#586E75", BrightYellow = "#657B83",
        BrightBlue = "#839496", BrightMagenta = "#6C71C4", BrightCyan = "#93A1A1", BrightWhite = "#FDF6E3",
    };
}
