using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.UserControls;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the fourth batch of phase 2 dialogs.</summary>
[TestFixture]
public sealed class Batch4ViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        ProcessViewModelTests.FakeMessageBoxes messageBoxes = new();

        Capture(
            new CheckoutRevisionWindow { DataContext = Batch4ViewModelTests.CreateCheckout(new Batch4ViewModelTests.FakeCheckoutHost(), CreateRefDialogViewModelTests.Commit.ToString()) },
            $"checkout-revision-{theme}");
        Capture(
            new CompareToBranchWindow
            {
                DataContext = new CompareToBranchViewModel(
                    new CompareToBranchStrings(), new LocalRemoteBranchSelectorViewModel(new LocalRemoteBranchSelectorStrings(), true, new Batch4ViewModelTests.FakeBranchSelectorHost()) { BranchName = "origin/dev" }),
            },
            $"compare-to-branch-{theme}");
        Capture(new BisectWindow { DataContext = new BisectViewModel(new BisectStrings(), new Batch4ViewModelTests.FakeBisectHost { InTheMiddle = true }, messageBoxes) }, $"bisect-{theme}");
        Capture(
            new GoToCommitWindow { DataContext = new GoToCommitViewModel(new GoToCommitStrings(), [new GitRefItem("v1.0", "a")], [new GitRefItem("main", "b")], "HEAD~1", () => { }) },
            $"go-to-commit-{theme}");
        Capture(
            new DashboardCategoryTitleWindow { DataContext = new DashboardCategoryTitleViewModel(new DashboardCategoryTitleStrings(), [], "Work", messageBoxes) },
            $"dashboard-category-title-{theme}");
        Batch4ViewModelTests.FakeGitIgnoreHost gitIgnoreHost = new();
        AddToGitIgnoreViewModel addToGitIgnore = new(new AddToGitIgnoreStrings(), false, ["*.log"], gitIgnoreHost);
        gitIgnoreHost.Report!(["logs/a.log", "logs/b.log"]);
        Capture(new AddToGitIgnoreWindow { DataContext = addToGitIgnore }, $"add-to-gitignore-{theme}");
    });

    [Test]
    public Task Bisect_enables_the_buttons_of_the_state() => OnUiThreadAsync(() =>
    {
        BisectWindow window = Show(new BisectWindow { DataContext = new BisectViewModel(new BisectStrings(), new Batch4ViewModelTests.FakeBisectHost(), new ProcessViewModelTests.FakeMessageBoxes()) });

        window.FindControl<Button>("startButton")!.IsEffectivelyEnabled.Should().BeTrue();
        window.FindControl<Button>("goodButton")!.IsEffectivelyEnabled.Should().BeFalse();
        window.FindControl<Button>("stopButton")!.IsEffectivelyEnabled.Should().BeFalse();
    });

    [Test]
    public Task GoToCommit_focused_input_supplies_the_revision() => OnUiThreadAsync(() =>
    {
        GoToCommitViewModel viewModel = new(new GoToCommitStrings(), [new GitRefItem("v1.0", "tag-guid")], [], null, () => { });
        GoToCommitWindow window = Show(new GoToCommitWindow { DataContext = viewModel });

        viewModel.TagText = "v1.0";
        window.FindControl<ComboBox>("tagsComboBox")!.Focus();
        Dispatcher.UIThread.RunJobs();

        viewModel.Source.Should().Be(GoToCommitSource.Tag);
        viewModel.SelectedRevision.Should().Be("tag-guid");
    });

    private static T Show<T>(T window)
        where T : DialogWindow
    {
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static void Capture(DialogWindow window, string name)
    {
        Show(window);
        SaveScreenshot(window.CaptureRenderedFrame(), name);
        window.Close();
    }
}
