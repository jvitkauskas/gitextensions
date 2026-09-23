using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Editor;
using static GitUI.AvaloniaTests.ViewModels.DiffViewModelTests;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the diff dialog.</summary>
[TestFixture]
public sealed class DiffViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        DiffViewModel viewModel = Create(new FakeHost(), new FakeViewerHost());
        DiffWindow window = new() { DataContext = viewModel };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        viewModel.Viewer.Show(new FileViewContent(
            FileViewKind.Diff,
            "diff --git a/docs/readme.md b/docs/readme.md\n@@ -1,3 +1,3 @@\n # Readme\n-Old line of the documentation\n+New line of the documentation\n end\n"));
        Dispatcher.UIThread.RunJobs();

        window.FindControl<TextBlock>("firstCommitText")!.Text.Should().Be("main");
        viewModel.Files.Nodes.Should().NotBeEmpty("the files are loaded once the window is shown");
        SaveScreenshot(window.CaptureRenderedFrame(), $"diff-{theme}");
        window.Close();
    });
}
