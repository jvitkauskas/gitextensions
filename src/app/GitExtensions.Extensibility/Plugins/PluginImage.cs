using System.Reflection;

namespace GitExtensions.Extensibility.Plugins;

/// <summary>
///  An image of the plugin API that works on every system (plugin API v3): the bytes of an encoded image (PNG, or another
///  format the UI reads: BMP, JPEG, GIF, ICO), instead of the GDI+ <c>System.Drawing.Image</c> of the v1 and v2 members,
///  which is only shown on Windows.
/// </summary>
public sealed class PluginImage
{
    private readonly byte[] _data;

    private PluginImage(byte[] data)
    {
        _data = data;
    }

    /// <summary>The encoded image.</summary>
    public ReadOnlyMemory<byte> Data => _data;

    /// <summary>An image of the bytes of an encoded image (copied).</summary>
    public static PluginImage FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            throw new ArgumentException("An image needs its bytes.", nameof(data));
        }

        return new PluginImage(data.ToArray());
    }

    /// <summary>An image read from the rest of <paramref name="stream"/>.</summary>
    public static PluginImage FromStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using MemoryStream copy = new();
        stream.CopyTo(copy);
        return FromBytes(copy.GetBuffer().AsSpan(0, (int)copy.Length));
    }

    /// <summary>An image embedded in <paramref name="assembly"/> (an <c>EmbeddedResource</c> with its manifest name).</summary>
    /// <exception cref="ArgumentException">The assembly has no resource of that name.</exception>
    public static PluginImage FromResource(Assembly assembly, string resourceName)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new ArgumentException($"{assembly.GetName().Name} has no resource '{resourceName}'.", nameof(resourceName));
        return FromStream(stream);
    }

    /// <summary>A copy of the encoded image.</summary>
    public byte[] ToArray() => _data.ToArray();
}
