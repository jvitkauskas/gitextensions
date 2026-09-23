using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Controls;
using GitUI.Avalonia.HelperDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.UserControls;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the commit dialogs (phase 2, batch 3).</summary>
[TestFixture]
public sealed class CommitDialogViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        Capture(
            new CherryPickWindow { DataContext = CommitDialogViewModelTests.CreateCherryPick(CommitDialogViewModelTests.Commit, new CommitDialogViewModelTests.FakeCherryPickHost()) },
            $"cherry-pick-{theme}");
        Capture(
            new CherryPickWindow { DataContext = CommitDialogViewModelTests.CreateCherryPick(CommitDialogViewModelTests.Merge, new CommitDialogViewModelTests.FakeCherryPickHost()) },
            $"cherry-pick-merge-{theme}");
        Capture(
            new RevertCommitWindow
            {
                DataContext = new RevertCommitViewModel(
                    new RevertCommitStrings(), new CommitSummaryStrings(), CommitDialogViewModelTests.Merge, new CommitDialogViewModelTests.FakeRevertHost(), new ProcessViewModelTests.FakeMessageBoxes(), "Error"),
            },
            $"revert-commit-{theme}");
        Capture(
            new ResetCurrentBranchWindow
            {
                DataContext = new ResetCurrentBranchViewModel(
                    new ResetCurrentBranchStrings(),
                    new CommitSummaryStrings(),
                    "main",
                    CommitDialogViewModelTests.Summary,
                    ResetKind.Mixed,
                    new CommitDialogViewModelTests.FakeResetCurrentBranchHost(),
                    new ProcessViewModelTests.FakeMessageBoxes()),
            },
            $"reset-current-branch-{theme}");
        Capture(
            new ResetAnotherBranchWindow { DataContext = CommitDialogViewModelTests.CreateResetAnother(new CommitDialogViewModelTests.FakeResetAnotherBranchHost(), "diverged") },
            $"reset-another-branch-{theme}");
        Capture(
            new ArchiveWindow
            {
                DataContext = CommitDialogViewModelTests.CreateArchive(
                    new CommitDialogViewModelTests.FakeArchiveHost(), new SmallDialogViewModelTests.FakeFileDialogs(), CommitDialogViewModelTests.Merge, path: null),
            },
            $"archive-{theme}");
    });

    [Test]
    public Task CherryPick_shows_the_parents_only_for_a_merge() => OnUiThreadAsync(() =>
    {
        CherryPickViewModel viewModel = CommitDialogViewModelTests.CreateCherryPick(
            CommitDialogViewModelTests.Merge, new CommitDialogViewModelTests.FakeCherryPickHost { Chosen = CommitDialogViewModelTests.Commit });
        CherryPickWindow window = Show(new CherryPickWindow { DataContext = viewModel });
        MergeParentsView parents = window.FindControl<MergeParentsView>("mergeParents")!;
        ListBox list = parents.FindControl<ListBox>("parentsListBox")!;

        parents.IsVisible.Should().BeTrue();
        list.SelectedIndex.Should().Be(0);
        list.SelectedIndex = 1;
        Dispatcher.UIThread.RunJobs();
        viewModel.SelectedParent!.Number.Should().Be(2);

        viewModel.ChooseRevisionCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();
        parents.IsVisible.Should().BeFalse();
    });

    [Test]
    public Task ResetAnotherBranch_OK_is_enabled_by_forcing_a_non_fast_forward_reset() => OnUiThreadAsync(() =>
    {
        ResetAnotherBranchViewModel viewModel = CommitDialogViewModelTests.CreateResetAnother(new CommitDialogViewModelTests.FakeResetAnotherBranchHost(), "diverged");
        ResetAnotherBranchWindow window = Show(new ResetAnotherBranchWindow { DataContext = viewModel });
        Button ok = window.FindControl<Button>("okButton")!;

        ok.IsEffectivelyEnabled.Should().BeFalse();
        window.FindControl<CheckBox>("forceCheckBox")!.IsChecked = true;
        Dispatcher.UIThread.RunJobs();
        ok.IsEffectivelyEnabled.Should().BeTrue();
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
