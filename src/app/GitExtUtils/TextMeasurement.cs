using GitExtensions.Extensibility;

namespace GitExtUtils;

/// <summary>The size of a text drawn with a font (in place of the WinForms <c>TextRenderer.MeasureText</c>).</summary>
public static class TextMeasurement
{
    /// <summary>
    ///  The size of <paramref name="text"/> drawn with <paramref name="font"/> (the default font if none), in pixels: measured
    ///  by GDI+ on Windows, estimated from the length of the text elsewhere.
    /// </summary>
    public static Size MeasureText(string? text, FontDescriptor? font)
    {
        text ??= "";
        if (OperatingSystem.IsWindowsVersionAtLeast(6, 1))
        {
            using Bitmap bitmap = new(1, 1);
            using Graphics graphics = Graphics.FromImage(bitmap);
            using Font? gdiFont = font?.ToFont();
            return Size.Ceiling(graphics.MeasureString(text, gdiFont ?? SystemFonts.DefaultFont));
        }

        // An average character is about half as wide as the font is high (the size in pixels).
        double sizeInPixels = (font ?? new FontDescriptor("", 9)).SizeInPixels;
        return new Size((int)Math.Ceiling(text.Length * sizeInPixels / 2), (int)Math.Ceiling(sizeInPixels * 1.2));
    }
}
