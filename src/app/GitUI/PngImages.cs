using SkiaSharp;

namespace GitUI;

/// <summary>
///  Images as PNG data (the avatars, the icons of the scripts): read, resized and drawn with SkiaSharp (which Avalonia
///  renders with) instead of GDI+, so that they work on every system (docs/avalonia-port/CROSS-PLATFORM.md, phase 2).
/// </summary>
internal static class PngImages
{
    /// <summary>The part of an avatar that its initials fill at most.</summary>
    private const float TextExtent = 0.9f;

    /// <summary>The PNG data of an encoded image (PNG, JPEG, GIF, BMP, ICO, WebP), or <see langword="null"/> if it is not an image.</summary>
    public static byte[]? ToPng(byte[] data)
    {
        using SKBitmap? bitmap = Decode(data);
        return bitmap is null ? null : Encode(bitmap);
    }

    /// <summary>The width of an encoded image, or <see langword="null"/> if it is not an image.</summary>
    public static int? GetWidth(byte[] data)
    {
        using SKData encoded = SKData.CreateCopy(data);
        using SKCodec? codec = SKCodec.Create(encoded);
        return codec?.Info.Width;
    }

    /// <summary>An encoded image scaled to a square of <paramref name="size"/> pixels, as PNG data.</summary>
    public static byte[] Resize(byte[] data, int size)
    {
        using SKBitmap source = Decode(data) ?? throw new ArgumentException("Not an image.", nameof(data));
        using SKBitmap resized = source.Resize(new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul), new SKSamplingOptions(SKCubicResampler.Mitchell));
        return Encode(resized);
    }

    /// <summary>A square of <paramref name="size"/> pixels of one color.</summary>
    public static byte[] Fill(Color color, int size = 1)
    {
        using SKBitmap bitmap = new(size, size);
        bitmap.Erase(ToSkColor(color));
        return Encode(bitmap);
    }

    /// <summary>
    ///  A square of <paramref name="size"/> pixels in <paramref name="backColor"/> with <paramref name="text"/> centered in
    ///  <paramref name="foreColor"/>, as large as fits (the initials of an author).
    /// </summary>
    public static byte[] DrawText(string text, Color foreColor, Color backColor, int size, string fontFamily)
    {
        using SKBitmap bitmap = new(size, size);
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(ToSkColor(backColor));

        using SKTypeface typeface = SKTypeface.FromFamilyName(fontFamily) ?? SKTypeface.Default;
        using SKFont font = new(typeface, size) { Edging = SKFontEdging.Antialias, Subpixel = true };
        using SKPaint paint = new() { Color = ToSkColor(foreColor), IsAntialias = true };

        // As the GDI+ drawing did: the text in a square of its advance and line height (and its ink, which some fonts
        // draw beyond the advance), scaled to the avatar with a margin.
        float advance = font.MeasureText(text, out SKRect bounds);
        float extent = Math.Max(Math.Max(advance, bounds.Width), font.Spacing);
        font.Size *= size * TextExtent / Math.Max(extent, 1);
        font.MeasureText(text, out bounds);

        // Centered on the ink of the glyphs (initials have no descenders to keep room for).
        float x = ((size - bounds.Width) / 2) - bounds.Left;
        float y = ((size - bounds.Height) / 2) - bounds.Top;
        canvas.DrawText(text, x, y, SKTextAlign.Left, font, paint);
        canvas.Flush();

        return Encode(bitmap);
    }

    // SKBitmap.Decode throws for data it has no codec for, rather than returning null.
    private static SKBitmap? Decode(byte[] data)
    {
        using SKData encoded = SKData.CreateCopy(data);
        using SKCodec? codec = SKCodec.Create(encoded);
        return codec is null ? null : SKBitmap.Decode(codec);
    }

    private static byte[] Encode(SKBitmap bitmap)
    {
        using SKData data = bitmap.Encode(SKEncodedImageFormat.Png, quality: 100);
        return data.ToArray();
    }

    private static SKColor ToSkColor(Color color) => new(color.R, color.G, color.B, color.A);
}
