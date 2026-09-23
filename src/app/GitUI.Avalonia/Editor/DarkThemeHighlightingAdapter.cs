using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace GitUI.Avalonia.Editor;

/// <summary>
///  Lightens the dark colors of the syntax highlighting (which are made for light backgrounds) on a dark theme, as the
///  WinForms editor adapts its colors to the theme. Runs after the highlighting, whose definitions are shared and frozen.
/// </summary>
internal sealed class DarkThemeHighlightingAdapter : DocumentColorizingTransformer
{
    private const double MinLuminance = 0.45;

    protected override void ColorizeLine(DocumentLine line)
    {
        if (line.Length == 0)
        {
            return;
        }

        ChangeLinePart(line.Offset, line.EndOffset, element =>
        {
            if (element.TextRunProperties.ForegroundBrush is ISolidColorBrush { Color: var color } && Luminance(color) < MinLuminance)
            {
                element.TextRunProperties.SetForegroundBrush(new SolidColorBrush(Lighten(color)));
            }
        });
    }

    private static double Luminance(Color color) => ((0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B)) / 255;

    /// <summary>Blends toward white until the color is bright enough for a dark background.</summary>
    internal static Color Lighten(Color color)
    {
        double luminance = Luminance(color);
        double amount = Math.Clamp((MinLuminance + 0.25 - luminance) / (1 - luminance), 0, 1);
        return Color.FromArgb(
            color.A,
            (byte)(color.R + ((255 - color.R) * amount)),
            (byte)(color.G + ((255 - color.G) * amount)),
            (byte)(color.B + ((255 - color.B) * amount)));
    }
}
