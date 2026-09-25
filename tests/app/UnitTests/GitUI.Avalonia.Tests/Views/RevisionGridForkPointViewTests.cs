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
}
