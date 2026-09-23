using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using GitUI.Avalonia.HelperDialogs;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.Editor;
using GitUI.Presentation.HelperDialogs;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the commit diff dialog.</summary>
[TestFixture]
public sealed class CommitDiffViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        CommitDiffViewModel viewModel = CommitDiffViewModelTests.Create(new CommitDiffViewModelTests.FakeHost(), new DiffViewModelTests.FakeViewerHost());
        CommitDiffWindow window = new() { DataContext = viewModel };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        viewModel.Viewer.Show(new FileViewContent(FileViewKind.Diff, "diff --git a/docs/readme.md b/docs/readme.md\n@@ -1,2 +1,2 @@\n-The old documentation\n+The new documentation\n same\n"));
        Dispatcher.UIThread.RunJobs();

        window.Title.Should().StartWith("Diff - c3c3c3c3");
        SaveScreenshot(window.CaptureRenderedFrame(), $"commit-diff-{theme}");
        window.Close();
    });
}
