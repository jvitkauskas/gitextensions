using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.Threading;
using GitUI.Avalonia.Editor;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.Editor;

namespace GitUI.AvaloniaTests.Views;

/// <summary>The bytes of a binary file in the file viewer (AvaloniaHex), as the hex dump of <c>FileViewer.DisplayAsHexDump</c>.</summary>
[TestFixture]
public sealed class BinaryFileViewTests : HeadlessTest
{
    [Test]
    public Task A_binary_file_is_shown_in_hexadecimal_under_its_name_and_size([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        byte[] data = [.. Enumerable.Range(0, 300).Select(i => (byte)i)];
        FileViewerViewModel viewModel = new(new DiffViewModelTests.FakeViewerHost());
        FileViewerView view = new() { DataContext = viewModel };
        Window window = new() { Content = view, Width = 760, Height = 320 };
        window.Show();

        viewModel.Show(new FileViewContent(FileViewKind.Binary, "data.bin", "data.bin", BinaryData: data));
        Dispatcher.UIThread.RunJobs();

        view.HexView.IsEffectivelyVisible.Should().BeTrue();
        view.TextView.IsVisible.Should().BeFalse();
        view.HexView.Document!.Length.Should().Be(300UL);
        view.HexView.Document.IsReadOnly.Should().BeTrue();
        viewModel.BinarySummary.Should().Be("Binary file: data.bin\r\n\r\n300 bytes:".Replace("\r\n", Environment.NewLine));
        view.GetLogicalDescendants().OfType<SelectableTextBlock>().Single(t => t.Name == "binarySummary").Text.Should().Be(viewModel.BinarySummary);
        SaveScreenshot(window.CaptureRenderedFrame(), $"file-viewer-binary-{theme}");

        viewModel.Show(new FileViewContent(FileViewKind.Text, "text", "a.txt"));
        Dispatcher.UIThread.RunJobs();
        view.HexView.IsEffectivelyVisible.Should().BeFalse();
        view.HexView.Document.Should().BeNull();
        view.TextView.IsVisible.Should().BeTrue();
        window.Close();
    });

    [Test]
    public void The_size_of_a_larger_file_is_also_in_MB()
    {
        FileViewerViewModel viewModel = new(new DiffViewModelTests.FakeViewerHost());

        viewModel.Show(new FileViewContent(FileViewKind.Binary, "big.bin", "big.bin", BinaryData: new byte[3 * 1024 * 1024 / 2]));

        viewModel.BinarySummary.Should().EndWith($"{1.5:N1} MB / {1572864:N0} bytes:");
    }
}
