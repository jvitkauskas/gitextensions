using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the log window of the <c>viewdiff</c> verb (port of <c>FormLog</c>).</summary>
[TestFixture]
public sealed class LogViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        (LogViewModel viewModel, LogViewModelTests.FakeHost host, DiffViewModelTests.FakeViewerHost viewer) = LogViewModelTests.Create(reportSelection: false);
        viewer.Diff = "diff --git a/src/file.cs b/src/file.cs\n@@ -1,2 +1,2 @@\n-old line\n+new line\n same\n";
        LogWindow window = new() { DataContext = viewModel };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        window.Title.Should().Be("Diff");
        window.RevisionGrid.GetVisualDescendants().OfType<DataGrid>().Single().ItemsSource.Cast<object>().Should().HaveCount(4);
        host.Diffs.Should().NotBeEmpty().And.OnlyContain(d => d == "c3c3c3c3", "the current checkout is selected");
        viewModel.Viewer.Editor.Text.Should().StartWith("diff --git");
        SaveScreenshot(window.CaptureRenderedFrame(), $"log-{theme}");

        window.Close();
        viewModel.Grid.Rows.Should().HaveCount(4);
    });
}
