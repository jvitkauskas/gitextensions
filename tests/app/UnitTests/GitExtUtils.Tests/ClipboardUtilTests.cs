using System.Diagnostics.CodeAnalysis;
using GitExtUtils;

namespace GitExtUtilsTests;

/// <summary>The clipboard off Windows: the one of the UI of the application (docs/avalonia-port/CROSS-PLATFORM.md, phase 2).</summary>
[NonParallelizable]
[Platform(Exclude = "Win")]
public sealed class ClipboardUtilTests
{
    [TearDown]
    public void TearDown() => ClipboardUtil.Backend = null;

    [Test]
    public void The_backend_has_the_text_of_the_clipboard()
    {
        FakeClipboard clipboard = new();
        ClipboardUtil.Backend = clipboard;

        ClipboardUtil.TrySetText("copied").Should().BeTrue();
        ClipboardUtil.TryGetText(out string? text).Should().BeTrue();

        text.Should().Be("copied");
    }

    [Test]
    public void The_backend_sets_the_html_and_its_text()
    {
        FakeClipboard clipboard = new();
        ClipboardUtil.Backend = clipboard;

        ClipboardUtil.TrySetHtml("<b>x</b>", "x").Should().BeTrue();

        clipboard.Html.Should().Be("<b>x</b>");
        clipboard.Text.Should().Be("x");
    }

    [Test]
    public void Without_a_backend_there_is_no_clipboard()
    {
        ClipboardUtil.TrySetText("lost").Should().BeFalse();
        ClipboardUtil.TryGetText(out string? text).Should().BeFalse();
        text.Should().BeNull();
    }

    private sealed class FakeClipboard : IClipboardBackend
    {
        public string? Text { get; private set; }

        public string? Html { get; private set; }

        public bool TrySetText(string text)
        {
            Text = text;
            return true;
        }

        public bool TrySetHtml(string html, string text)
        {
            Html = html;
            Text = text;
            return true;
        }

        public bool TryGetText([NotNullWhen(returnValue: true)] out string? text)
        {
            text = Text;
            return text is not null;
        }
    }
}
