using System.Drawing.Imaging;
using System.Runtime.Versioning;
using GitExtensions.Extensibility.Plugins;

namespace GitExtensions.Extensibility;

/// <summary>The GDI+ images of the v1 and v2 plugin API as <see cref="PluginImage"/>s, on Windows.</summary>
[SupportedOSPlatform("windows6.1")]
public static class GdiImageExtensions
{
    /// <summary>The image encoded as PNG.</summary>
    public static PluginImage ToPluginImage(this Image image)
    {
        ArgumentNullException.ThrowIfNull(image);
        using MemoryStream stream = new();
        image.Save(stream, ImageFormat.Png);
        return PluginImage.FromBytes(stream.GetBuffer().AsSpan(0, (int)stream.Length));
    }
}
