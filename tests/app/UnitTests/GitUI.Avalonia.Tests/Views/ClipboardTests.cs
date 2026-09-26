using System.Text;
using Avalonia.Input;
using Avalonia.Input.Platform;
using GitUI.Avalonia.Hosting;
using NSubstitute;

namespace GitUI.AvaloniaTests.Views;

[TestFixture]
public sealed class ClipboardTests : HeadlessTest
{
    [TestCase("")]
    [TestCase("25a80ff9881722b9868aa02305a65e7a40c24fe0")]
    [TestCase("Žąsis 日本語 😀\nsecond line")]
    public Task Copied_text_supports_the_XWayland_plain_text_request(string text) => OnUiThreadAsync(async () =>
    {
        IClipboard clipboard = Substitute.For<IClipboard>();
        IAsyncDataTransfer? written = null;
        clipboard.SetDataAsync(Arg.Do<IAsyncDataTransfer?>(value => written = value)).Returns(Task.CompletedTask);

        await AvaloniaClipboardBackend.SetTextAsync(clipboard, text);

        written.Should().NotBeNull();
        using (written)
        {
            (await written!.TryGetTextAsync()).Should().Be(text);
            DataFormat<byte[]> x11Text = DataFormat.CreateBytesPlatformFormat("TEXT");
            if (OperatingSystem.IsLinux())
            {
                // Hyprland requests the TEXT atom when a Wayland client asks for text/plain.
                byte[]? bytes = await written.TryGetValueAsync(x11Text);
                bytes.Should().NotBeNull();
                Encoding.UTF8.GetString(bytes!).Should().Be(text);
            }
            else
            {
                written.Formats.Should().NotContain(x11Text);
            }
        }
    });
}
