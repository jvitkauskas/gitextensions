using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using GitExtUtils.GitUI.Theming;

namespace GitUI.Avalonia.Hosting;

/// <summary>The colors of the Git Extensions theme (<c>AppColor</c>) for views that draw.</summary>
public static class AppColorResources
{
    /// <summary>
    ///  The theme's color, as the host provides it as the resource <c>AppColor.&lt;name&gt;</c>; the default color otherwise
    ///  (e.g. in tests). <see langword="null"/> if the color is not set (transparent or empty).
    /// </summary>
    public static Color? GetColor(StyledElement element, AppColor appColor)
    {
        if (element.TryFindResource(ThemeColors.AppColorPrefix + appColor, element.ActualThemeVariant, out object? resource) && resource is ISolidColorBrush themed)
        {
            return themed.Color.A == 0 ? null : themed.Color;
        }

        System.Drawing.Color fallback = AppColorDefaults.GetBy(appColor);
        return fallback.IsEmpty ? null : Color.FromArgb(fallback.A, fallback.R, fallback.G, fallback.B);
    }

    public static IBrush? GetBrush(StyledElement element, AppColor appColor)
        => GetColor(element, appColor) is { } color ? new SolidColorBrush(color) : null;
}
