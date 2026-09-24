using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using GitCommands.Settings;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.Editor;
using GitUI.Avalonia.Hosting;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.Editor;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless tests of the appearances of the file viewer (git word diff, difftastic, combined and range diffs, grep).</summary>
[TestFixture]
public sealed class DiffAppearancesViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        bool dark = theme == "dark";
        UseTheme(dark ? ThemeVariant.Dark : ThemeVariant.Light);

        Capture(GitOutputFixtures.WordDiff, DiffViewMode.Diff, DiffDisplayAppearance.GitWordDiff, $"diff-git-word-{theme}", dark);
        Capture(GitOutputFixtures.Patch, DiffViewMode.Diff, DiffDisplayAppearance.Patch, $"diff-patch-syntax-highlighting-{theme}", dark);
        Capture(GitOutputFixtures.Difftastic, DiffViewMode.Difftastic, DiffDisplayAppearance.Difftastic, $"diff-difftastic-{theme}", dark, width: 1500);
        Capture(GitOutputFixtures.CombinedDiff, DiffViewMode.CombinedDiff, DiffDisplayAppearance.Patch, $"diff-combined-{theme}", dark);
        Capture(GitOutputFixtures.Grep, DiffViewMode.Grep, DiffDisplayAppearance.Patch, $"diff-grep-{theme}", dark);
        Capture(GitOutputFixtures.RangeDiff, DiffViewMode.RangeDiff, DiffDisplayAppearance.Patch, $"diff-range-{theme}", dark);
    });

    [Test]
    public Task The_margin_has_one_column_for_grep_results() => OnUiThreadAsync(() =>
    {
        (Window window, FileViewerView view, FileViewerViewModel viewModel, _) = Create(DiffDisplayAppearance.Patch, width: 600);
        viewModel.Show(Content(GitOutputFixtures.Patch, DiffViewMode.Diff));
        Dispatcher.UIThread.RunJobs();
        double twoColumns = Margin(view).Bounds.Width;

        viewModel.Show(Content(GitOutputFixtures.Grep, DiffViewMode.Grep));
        Dispatcher.UIThread.RunJobs();
        Margin(view).Bounds.Width.Should().BeLessThan(twoColumns);
        view.TextView.Editor.Options.ShowColumnRulers.Should().BeFalse();

        viewModel.Show(Content(GitOutputFixtures.Difftastic, DiffViewMode.Difftastic));
        Dispatcher.UIThread.RunJobs();
        view.TextView.Editor.Options.ShowColumnRulers.Should().BeTrue("difftastic shows where the right side starts");
        view.TextView.Editor.Options.ColumnRulerPositions.Should().Equal(97);
        window.Close();

        static Control Margin(FileViewerView view) => view.TextView.Editor.TextArea.LeftMargins.OfType<Control>().First(m => m.GetType().Name == "DiffLineNumberMargin");
    });

    [Test]
    public Task The_menu_chooses_the_appearance() => OnUiThreadAsync(() =>
    {
        (Window window, FileViewerView view, FileViewerViewModel viewModel, DiffViewModelTests.FakeViewerHost host) = Create(DiffDisplayAppearance.GitWordDiff, width: 600);
        _ = viewModel.ShowChangesAsync(FileViewerContextMenuTests.Entry(StagedStatus.WorkTree));
        Dispatcher.UIThread.RunJobs();

        MenuItem appearance = view.FillContextMenu().OfType<MenuItem>().Single(i => (string?)i.Header == "Diff appea_rance");
        appearance.Items.OfType<MenuItem>().Select(i => ((string?)i.Header, i.IsChecked, i.IsEnabled)).Should().Equal(
            ("_Patch", false, true), ("Git wor_d diff", true, true), ("Diff_tastic", false, false));

        host.IsDifftasticEnabled = true;
        appearance = view.FillContextMenu().OfType<MenuItem>().Single(i => (string?)i.Header == "Diff appea_rance");
        MenuItem difftastic = appearance.Items.OfType<MenuItem>().Last();
        difftastic.IsEnabled.Should().BeTrue("difftastic is configured");
        difftastic.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
        host.DiffAppearance.Should().Be(DiffDisplayAppearance.Difftastic);

        view.ExecuteHotkeyCommand(FileViewerHotkeyCommand.ShowGitWordColoring).Should().BeTrue();
        host.DiffAppearance.Should().Be(DiffDisplayAppearance.GitWordDiff);
        view.ExecuteHotkeyCommand(FileViewerHotkeyCommand.TreatFileAsText).Should().BeTrue();
        viewModel.TreatAllFilesAsText.Should().BeTrue();

        // As FindNextAsync: F3 without a search opens the difftool.
        view.ExecuteHotkeyCommand(FileViewerHotkeyCommand.FindNextOrOpenWithDifftool).Should().BeTrue();
        host.DifftoolsOpened.Should().Equal("f");
        window.Close();
    });

    private static FileViewContent Content(string text, DiffViewMode mode)
        => new(FileViewKind.Diff, text, "calc.cs", HasGitColors: true, DiffMode: mode, DifftasticWidth: 200);

    private static (Window Window, FileViewerView View, FileViewerViewModel ViewModel, DiffViewModelTests.FakeViewerHost Host) Create(DiffDisplayAppearance appearance, double width, bool dark = false)
    {
        DiffViewModelTests.FakeViewerHost host = new() { DiffAppearance = appearance, Diff = FileViewerContextMenuTests.Patch };
        FileViewerViewModel viewModel = new(host);
        FileViewerView view = new() { DataContext = viewModel };
        Window window = new() { Content = view, Width = width, Height = 330 };
        if (dark)
        {
            // The colors of the dark theme, as the host provides them.
            host.ThemeColors = ThemeCssColors.Dark;
            ThemeCssColors.Dark.AddResources(window.Resources);
        }

        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, view, viewModel, host);
    }

    private static void Capture(string text, DiffViewMode mode, DiffDisplayAppearance appearance, string name, bool dark, double width = 640)
    {
        (Window window, _, FileViewerViewModel viewModel, _) = Create(appearance, width, dark);
        viewModel.Show(Content(text, mode));
        Dispatcher.UIThread.RunJobs();
        SaveScreenshot(window.CaptureRenderedFrame(), name);
        window.Close();
    }
}
