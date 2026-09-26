using GitUI;
using GitUI.Avatars;
using SkiaSharp;

namespace GitUITests;

/// <summary>The images as PNG data, without GDI+ (docs/avalonia-port/CROSS-PLATFORM.md, phase 2).</summary>
public sealed class PngImagesTests
{
    private static readonly byte[] _pngSignature = [0x89, (byte)'P', (byte)'N', (byte)'G'];

    [Test]
    public void Fill_is_a_square_of_one_color()
    {
        using SKBitmap bitmap = Decode(PngImages.Fill(Color.Red, size: 3));

        bitmap.Width.Should().Be(3);
        bitmap.Height.Should().Be(3);
        bitmap.GetPixel(1, 1).Should().Be(new SKColor(255, 0, 0, 255));
    }

    [Test]
    public void ToPng_converts_an_encoded_image_and_rejects_other_data()
    {
        using SKBitmap source = new(4, 2);
        source.Erase(SKColors.Blue);
        using SKData jpeg = source.Encode(SKEncodedImageFormat.Jpeg, quality: 90);

        byte[]? png = PngImages.ToPng(jpeg.ToArray());

        png.Should().NotBeNull();
        png![..4].Should().Equal(_pngSignature);
        PngImages.GetWidth(png).Should().Be(4);

        PngImages.ToPng("<html>not an image</html>"u8.ToArray()).Should().BeNull();
        PngImages.GetWidth([1, 2, 3]).Should().BeNull();
    }

    [Test]
    public void Resize_scales_to_a_square()
    {
        using SKBitmap resized = Decode(PngImages.Resize(PngImages.Fill(Color.Green, size: 80), size: 24));

        resized.Width.Should().Be(24);
        resized.Height.Should().Be(24);
        resized.GetPixel(12, 12).Should().Be(new SKColor(0, 128, 0, 255));
    }

    [Test]
    public void DrawText_draws_the_text_centered_on_the_background()
    {
        using SKBitmap bitmap = Decode(PngImages.DrawText("AB", Color.White, Color.Black, size: 64, fontFamily: "Segoe UI"));

        bitmap.Width.Should().Be(64);
        bitmap.Height.Should().Be(64);
        bitmap.GetPixel(0, 0).Should().Be(SKColors.Black, "the corners are background");

        // The ink of the text: its bounds are about centered.
        (int left, int top, int right, int bottom) = InkBounds(bitmap, SKColors.Black);
        left.Should().BeGreaterThan(0);
        top.Should().BeGreaterThan(0);
        Math.Abs(left - (63 - right)).Should().BeLessThanOrEqualTo(4, "the text is centered horizontally");
        Math.Abs(top - (63 - bottom)).Should().BeLessThanOrEqualTo(4, "the text is centered vertically");
        (right - left).Should().BeGreaterThan(32, "the text is as large as fits");
        (right - left).Should().BeLessThan(48, "a margin is kept around the text, as the WinForms avatars have");
    }

    [Test]
    public async Task InitialsAvatarProvider_draws_an_avatar_of_the_size()
    {
        byte[]? avatar = await new InitialsAvatarProvider().GetAvatarAsync("albert.einstein@noreply.com", "Albert Einstein", 32);

        using SKBitmap bitmap = Decode(avatar!);
        bitmap.Width.Should().Be(32);
        bitmap.Height.Should().Be(32);
    }

    private static SKBitmap Decode(byte[] png)
    {
        png[..4].Should().Equal(_pngSignature);
        return SKBitmap.Decode(png);
    }

    private static (int Left, int Top, int Right, int Bottom) InkBounds(SKBitmap bitmap, SKColor background)
    {
        int left = bitmap.Width, top = bitmap.Height, right = -1, bottom = -1;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y) != background)
                {
                    left = Math.Min(left, x);
                    top = Math.Min(top, y);
                    right = Math.Max(right, x);
                    bottom = Math.Max(bottom, y);
                }
            }
        }

        return (left, top, right, bottom);
    }
}
