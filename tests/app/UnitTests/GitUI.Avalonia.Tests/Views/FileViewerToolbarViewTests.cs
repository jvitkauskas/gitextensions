using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.Threading;
using GitUI.Avalonia.Editor;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.Editor;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless tests of the toolbar of the file viewer.</summary>
[TestFixture]
public sealed class FileViewerToolbarViewTests : HeadlessTest
{
    private const string TwoHunks = "diff --git a/f b/f\n--- a/f\n+++ b/f\n@@ -1,3 +1,3 @@\n a\n-b\n+c\n d\n@@ -10,3 +10,4 @@\n x\n+y\n+z\n w\n";

    [Test]
    public Task The_toolbar_shows_over_the_text_with_the_options_of_the_content([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        FileViewerViewModel viewModel = new(new DiffViewModelTests.FakeViewerHost());
        FileViewerView view = new() { DataContext = viewModel };
        Window window = new() { Content = view, Width = 700, Height = 300 };
        window.Show();
        viewModel.Show(new FileViewContent(FileViewKind.Diff, TwoHunks));
        Dispatcher.UIThread.RunJobs();

        view.Toolbar.IsVisible.Should().BeFalse();
        window.MouseMove(new Point(200, 150));
        Dispatcher.UIThread.RunJobs();
        view.Toolbar.IsVisible.Should().BeTrue();
        Button next = view.GetLogicalDescendants().OfType<Button>().Single(b => b.Name == "nextChangeButton");
        next.IsVisible.Should().BeTrue();
        SaveScreenshot(window.CaptureRenderedFrame(), $"file-viewer-toolbar-{theme}");

        viewModel.Show(new FileViewContent(FileViewKind.Text, "text", "a.txt"));
        Dispatcher.UIThread.RunJobs();
        next.IsVisible.Should().BeFalse("a text has no changes");
        view.GetLogicalDescendants().OfType<ComboBox>().Single(c => c.Name == "encodingComboBox").IsVisible.Should().BeTrue();
        window.Close();
    });

    [Test]
    public Task Next_and_previous_change_move_the_caret() => OnUiThreadAsync(() =>
    {
        FileViewerViewModel viewModel = new(new DiffViewModelTests.FakeViewerHost());
        FileViewerView view = new() { DataContext = viewModel };
        Window window = new() { Content = view, Width = 700, Height = 300 };
        window.Show();
        viewModel.Show(new FileViewContent(FileViewKind.Diff, TwoHunks));
        Dispatcher.UIThread.RunJobs();

        // As GoToFirstChange: shown at the first change.
        view.TextView.Editor.TextArea.Caret.Line.Should().Be(6);
        view.GoToChange(backwards: false);
        view.TextView.Editor.TextArea.Caret.Line.Should().Be(11);
        view.GoToChange(backwards: true);
        view.TextView.Editor.TextArea.Caret.Line.Should().Be(6);
        window.Close();
    });
}
