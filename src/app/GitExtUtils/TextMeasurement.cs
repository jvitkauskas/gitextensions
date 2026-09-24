namespace GitExtUtils;

/// <summary>The size of a text drawn with a font (in place of the WinForms <c>TextRenderer.MeasureText</c>).</summary>
public static class TextMeasurement
{
    /// <summary>The size of <paramref name="text"/> drawn with <paramref name="font"/> (the default font if none), in pixels.</summary>
    public static Size MeasureText(string? text, Font? font)
    {
        using Bitmap bitmap = new(1, 1);
        using Graphics graphics = Graphics.FromImage(bitmap);
        return Size.Ceiling(graphics.MeasureString(text ?? "", font ?? SystemFonts.DefaultFont));
    }
}
