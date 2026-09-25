using Avalonia.Controls;
using Avalonia.Threading;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.Controls.RevisionGrid;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUIPluginInterfaces;

namespace GitUI.AvaloniaTests.Views;

/// <summary>The "select next fork point as diff base" hotkey of the grid (<c>SelectNextForkPointAsDiffBase</c>).</summary>
[TestFixture]
public sealed class RevisionGridForkPointViewTests : HeadlessTest
{
    [Test]
    public Task The_fork_point_is_selected_first_as_the_base_of_the_diff() => OnUiThreadAsync(() =>
    {
        // head, side, merge, feature2, fix, feature1, release (v1.0), root
        List<GitRevision> history = RevisionGridViewTests.CreateHistory();
        RevisionGridViewModel viewModel = new(new RevisionGridViewTests.FakeRevisionGridHost(history), new RevisionGridDisplayOptions(RelativeDate: true, ShowAuthorDate: false)) { MultiSelect = true };
        Window window = new() { Width = 760, Height = 260, Content = new RevisionGridView { DataContext = viewModel } };
        window.Show();
        viewModel.Load();
        Dispatcher.UIThread.RunJobs();

        // The merge has two children (the head and the experiment).
        viewModel.SelectRevision(history[1].ObjectId);
        Dispatcher.UIThread.RunJobs();
        viewModel.ExecuteHotkey(RevisionGridCommand.SelectNextForkPointAsDiffBase).Should().BeTrue();
        Dispatcher.UIThread.RunJobs();
        viewModel.SelectedRows.Select(r => r.ObjectId).Should().Equal(history[2].ObjectId, history[1].ObjectId);

        // From "Finish feature": past "Start feature" (one child, no branch) to the release, which has two children.
        viewModel.SelectRevision(history[3].ObjectId);
        Dispatcher.UIThread.RunJobs();
        viewModel.SelectNextForkPointAsDiffBase();
        Dispatcher.UIThread.RunJobs();
        viewModel.SelectedRows.Select(r => r.ObjectId).Should().Equal(history[6].ObjectId, history[3].ObjectId);
        window.Close();
    });

    [Test]
    public Task A_load_selects_the_first_revision_then_the_selected_one() => OnUiThreadAsync(() =>
    {
        List<GitRevision> history = RevisionGridViewTests.CreateHistory();
        RevisionGridViewModel viewModel = new(new RevisionGridViewTests.FakeRevisionGridHost(history), new RevisionGridDisplayOptions(RelativeDate: true, ShowAuthorDate: false)) { MultiSelect = true };
        Window window = new() { Width = 760, Height = 260, Content = new RevisionGridView { DataContext = viewModel } };
        window.Show();

        // As GetToBeSelectedRevisions with FirstId and SelectedId (FormBrowse.SetWorkingDir).
        viewModel.Load(history[1].ObjectId, firstSelected: history[4].ObjectId);
        Dispatcher.UIThread.RunJobs();
        viewModel.SelectedRows.Select(r => r.ObjectId).Should().Equal(history[4].ObjectId, history[1].ObjectId);

        // Once: a reload keeps the selection.
        viewModel.Load(history[2].ObjectId);
        Dispatcher.UIThread.RunJobs();
        viewModel.SelectedRows.Select(r => r.ObjectId).Should().Equal(history[2].ObjectId);
        window.Close();
    });

    [Test]
    public Task A_double_click_on_a_label_goes_to_its_related_branch() => OnUiThreadAsync(() =>
    {
        List<GitRevision> history = RevisionGridViewTests.CreateHistory();
        RevisionGridViewModel viewModel = new(new RevisionGridViewTests.FakeRevisionGridHost(history), new RevisionGridDisplayOptions(RelativeDate: true, ShowAuthorDate: false));
        viewModel.Load();
        Dispatcher.UIThread.RunJobs();
        viewModel.SelectRevision(history[0].ObjectId);

        // As GoToRelatedRef: the revision of the tracked (or tracking) branch.
        viewModel.GoToRelatedRef(new RevisionRefItem("feature", RevisionRefKind.Branch, IsCurrentBranch: false) { RelatedRefCompleteName = "refs/tags/v1.0" }).Should().BeTrue();
        viewModel.SelectedRow!.ObjectId.Should().Be(history[6].ObjectId);

        viewModel.GoToRelatedRef(new RevisionRefItem("main", RevisionRefKind.Branch, IsCurrentBranch: false)).Should().BeFalse("without a related branch, the double click opens the revision");
    });
}
