using GitExtensions.Extensibility.Plugins;

namespace GitExtensions.ExtensibilityTests;

/// <summary>The images of plugin API v3 (docs/avalonia-port/CROSS-PLATFORM.md, decision 4).</summary>
public sealed class PluginImageTests
{
    private static readonly byte[] _bytes = [0x89, (byte)'P', (byte)'N', (byte)'G', 1, 2, 3];

    [Test]
    public void FromBytes_copies_the_bytes()
    {
        byte[] bytes = [.. _bytes];

        PluginImage image = PluginImage.FromBytes(bytes);
        bytes[0] = 0;

        image.Data.ToArray().Should().Equal(_bytes);
        image.ToArray().Should().Equal(_bytes);
    }

    [Test]
    public void FromBytes_needs_bytes()
    {
        ((Action)(() => PluginImage.FromBytes([]))).Should().Throw<ArgumentException>();
    }

    [Test]
    public void FromStream_reads_the_rest_of_the_stream()
    {
        using MemoryStream stream = new([0, .. _bytes]);
        stream.ReadByte();

        PluginImage.FromStream(stream).ToArray().Should().Equal(_bytes);
    }

    [Test]
    public void ToArray_returns_a_copy()
    {
        PluginImage image = PluginImage.FromBytes(_bytes);

        image.ToArray()[0] = 0;

        image.ToArray().Should().Equal(_bytes);
    }

    [Test]
    public void FromResource_fails_for_a_missing_resource()
    {
        ((Action)(() => PluginImage.FromResource(typeof(PluginImageTests).Assembly, "Missing.png"))).Should().Throw<ArgumentException>();
    }

    [Test]
    public void A_plugin_without_an_image_of_API_v3_has_none()
    {
        IGitPlugin plugin = NSubstitute.Substitute.For<IGitPlugin>();

        plugin.IconImage.Should().BeNull();
    }
}
