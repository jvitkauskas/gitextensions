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

    /// <summary>Halfway between the color and the background, in linear light (as <c>ColorHelper.DimColor</c> with the editor background).</summary>
    public static Color Dim(Color color, Color background)
        => Color.FromArgb(color.A, Mix(color.R, background.R), Mix(color.G, background.G), Mix(color.B, background.B));

    private static byte Mix(byte channel, byte background) => SrgbDelinearize((SrgbLinearize(channel) + SrgbLinearize(background)) * 0.5);

    private static double SrgbLinearize(byte channel)
    {
        double normalized = channel / 255.0;
        return normalized <= 0.04045 ? normalized / 12.92 : Math.Pow((normalized + 0.055) / 1.055, 2.4);
    }

    private static byte SrgbDelinearize(double linear)
    {
        double normalized = linear <= 0.0031308 ? 12.92 * linear : (1.055 * Math.Pow(linear, 1.0 / 2.4)) - 0.055;
        return (byte)Math.Round(Math.Clamp(normalized * 255.0, 0.0, 255.0));
    }
}
