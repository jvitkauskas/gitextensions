using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using GitUI.Avalonia.Hosting;

namespace GitUI.AvaloniaTests.Views;

/// <summary>The images lightened on a dark theme (<c>ColorHelper.AdaptLightness</c>).</summary>
[TestFixture]
public sealed class ImageLightnessTests : HeadlessTest
{
    [Test]
    public Task The_lightness_is_mapped_onto_the_text_and_background_keeping_the_hue() => OnUiThreadAsync(() =>
    {
        // Black, white, pure red, and a transparent pixel (BGRA).
        using Bitmap source = Create([0, 0, 0, 255, 255, 255, 255, 255, 0, 0, 255, 255, 0, 0, 0, 0]);

        using Bitmap adapted = ImageLightness.Adapt(source, text: Colors.White, background: Colors.Black);
        byte[] pixels = Read(adapted);

        // As LightnessCorrection: black becomes the text, white the background; the hue is kept.
        pixels[0..4].Should().Equal(255, 255, 255, 255);
        pixels[4..8].Should().Equal(0, 0, 0, 255);
        Color red = Color.FromRgb(pixels[10], pixels[9], pixels[8]);
        red.ToHsl().H.Should().BeApproximately(0, 1);
        pixels[15].Should().Be(0, "transparent pixels stay transparent");
    });

    [Test]
    public Task An_image_is_adapted_on_a_dark_theme_only_and_keeps_its_binding() => OnUiThreadAsync(() =>
    {
        Bitmap source = Create([0, 0, 0, 255]);
        Image image = new() { Source = source };
        ImageLightness.SetAdapt(image, true);
        Window window = new() { Content = image, RequestedThemeVariant = ThemeVariant.Light };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        image.Source.Should().BeSameAs(source);

        window.RequestedThemeVariant = ThemeVariant.Dark;
        Dispatcher.UIThread.RunJobs();
        image.Source.Should().NotBeSameAs(source).And.BeOfType<WriteableBitmap>();
        Read((Bitmap)image.Source!)[0].Should().BeGreaterThan(200, "black is lightened");

        window.RequestedThemeVariant = ThemeVariant.Light;
        Dispatcher.UIThread.RunJobs();
        image.Source.Should().BeSameAs(source);

        // Not adapted without the flag.
        ImageLightness.SetAdapt(image, false);
        window.RequestedThemeVariant = ThemeVariant.Dark;
        Dispatcher.UIThread.RunJobs();
        image.Source.Should().BeSameAs(source);
        window.Close();
    });

    private static Bitmap Create(byte[] bgra)
    {
        GCHandle handle = GCHandle.Alloc(bgra, GCHandleType.Pinned);
        try
        {
            return new Bitmap(PixelFormat.Bgra8888, AlphaFormat.Unpremul, handle.AddrOfPinnedObject(), new PixelSize(bgra.Length / 4, 1), new Vector(96, 96), bgra.Length);
        }
        finally
        {
            handle.Free();
        }
    }

    private static byte[] Read(Bitmap bitmap)
    {
        using WriteableBitmap copy = new(bitmap.PixelSize, bitmap.Dpi, PixelFormat.Bgra8888, AlphaFormat.Unpremul);
        using ILockedFramebuffer buffer = copy.Lock();
        bitmap.CopyPixels(buffer);
        byte[] pixels = new byte[bitmap.PixelSize.Width * 4];
        Marshal.Copy(buffer.Address, pixels, 0, pixels.Length);
        return pixels;
    }
}
