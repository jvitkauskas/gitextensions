using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
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
    ///  Applies the Git Extensions appearance settings (theme and font) to all Avalonia windows.
    /// </summary>
    internal void ApplyOptions(AvaloniaUiOptions options)
    {
        RequestedThemeVariant = options.IsDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;

        if (!string.IsNullOrWhiteSpace(options.FontFamily))
        {
            Resources["ContentControlThemeFontFamily"] = new FontFamily(options.FontFamily);
        }

        if (options.FontSize > 0)
        {
            // Fluent sizes most controls from this resource.
            Resources["ControlContentThemeFontSize"] = options.FontSize;
        }
    }
}
