using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using GitUI.Avalonia.Hosting;

namespace GitUI.Avalonia;

/// <summary>
///  The Avalonia application object. While WinForms owns the process (docs/avalonia-port/PLAN.md, phases 1-6)
///  it is set up without starting a lifetime, see <see cref="AvaloniaUi"/>.
/// </summary>
public partial class GitExtensionsAvaloniaApp : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    ///  Applies the Git Extensions appearance settings (theme, colors and font) to all Avalonia windows.
    /// </summary>
    internal void ApplyOptions(AvaloniaUiOptions options)
    {
        ThemeVariant variant = options.IsDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
        RequestedThemeVariant = variant;

        if (!string.IsNullOrWhiteSpace(options.FontFamily))
        {
            Resources["ContentControlThemeFontFamily"] = new FontFamily(options.FontFamily);
        }

        if (options.FontSize > 0)
        {
            // Fluent sizes most controls from this resource.
            Resources["ControlContentThemeFontSize"] = options.FontSize;
        }

        if (options.Colors is { } colors)
        {
            ApplyColors(variant, colors);
        }
    }

    private void ApplyColors(ThemeVariant variant, IReadOnlyDictionary<string, uint> colors)
    {
        // The Git Extensions theme drives Fluent's palette, so that Avalonia dialogs match the WinForms ones
        // (e.g. the gray dialog background of the default theme, or a custom theme's colors).
        ColorPaletteResources palette = new();
        if (TryGetColor(ThemeColors.Control, out Color region))
        {
            palette.RegionColor = region;
        }

        if (TryGetColor(ThemeColors.Highlight, out Color accent))
        {
            palette.Accent = accent;
        }

        if (Styles.OfType<FluentTheme>().FirstOrDefault() is { } fluentTheme)
        {
            fluentTheme.Palettes[variant] = palette;
        }

        if (TryGetColor(ThemeColors.HotTrack, out Color link))
        {
            Resources["SystemControlHyperlinkTextBrush"] = new SolidColorBrush(link);
        }

        // AppColor values (diff colors, graph lanes, ...) for views to use as {DynamicResource AppColor.<name>}.
        foreach ((string key, uint argb) in colors)
        {
            if (key.StartsWith(ThemeColors.AppColorPrefix, StringComparison.Ordinal))
            {
                Resources[key] = new SolidColorBrush(Color.FromUInt32(argb));
            }
        }

        bool TryGetColor(string key, out Color color)
        {
            bool found = colors.TryGetValue(key, out uint argb);
            color = Color.FromUInt32(argb);
            return found;
        }
    }
}
