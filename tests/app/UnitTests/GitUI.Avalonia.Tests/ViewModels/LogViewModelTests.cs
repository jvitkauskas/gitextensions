using GitExtensions.Extensibility.Git;
using GitUI.AvaloniaTests.Views;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUIPluginInterfaces;
using static GitUI.AvaloniaTests.ViewModels.FileHistoryViewModelTests;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the log window of the <c>viewdiff</c> verb (port of <c>FormLog</c>).</summary>
[TestFixture]
public sealed class LogViewModelTests
{
    [Test]
    public async Task The_selected_revisions_show_their_files_and_the_selected_file_its_diff()
    {
        (LogViewModel viewModel, FakeHost host, DiffViewModelTests.FakeViewerHost viewer) = Create();

        viewModel.Initialize();

        viewModel.Grid.Rows.Should().HaveCount(4);
        viewModel.Grid.SelectedRow!.Revision.Should().BeSameAs(Changed, "the initial revision (the current checkout) is selected");
        viewer.Shown.IsCompleted.Should().BeTrue();
        host.Diffs.Should().Equal("c3c3c3c3");
        viewModel.Files.AllEntries.Select(e => e.Item.Name).Should().Equal("src/file.cs");
        viewModel.Viewer.Editor.Text.Should().Be("diff of src/file.cs");

        Select(viewModel, Renamed, Added);
        await Task.Yield();
        host.Diffs.Should().Equal("c3c3c3c3", "a1a1a1a1,b2b2b2b2");
    }

    [Test]
    public void No_selected_revision_shows_no_files()
    {
        (LogViewModel viewModel, _, _) = Create();
        viewModel.Initialize();

        viewModel.Grid.SetSelectedRows([]);
        viewModel.Grid.SelectedRow = null;

        viewModel.Files.AllEntries.Should().BeEmpty();
    }

    [Test]
    public void Activating_a_revision_views_it_unless_it_is_artificial()
    {
        (LogViewModel viewModel, FakeHost host, _) = Create();
        viewModel.Initialize();

        viewModel.ViewSelectedRevisions();
        Select(viewModel, WorkTree);
        viewModel.ViewSelectedRevisions();

        host.Viewed.Should().Equal("c3c3c3c3");
    }

    /// <param name="reportSelection">Whether the selected row is reported as the selection, as the view does.</param>
    internal static (LogViewModel ViewModel, FakeHost Host, DiffViewModelTests.FakeViewerHost Viewer) Create(bool reportSelection = true)
    {
        FakeHost host = new();
        DiffViewModelTests.FakeViewerHost viewer = new();
        RevisionGridViewModel grid = new(new RevisionGridViewTests.FakeRevisionGridHost([WorkTree, Changed, Renamed, Added]), new RevisionGridDisplayOptions(RelativeDate: true, ShowAuthorDate: false)) { MultiSelect = true };

        // As the view: the selected row is reported as the selection.
        grid.PropertyChanged += (_, e) =>
        {
            if (reportSelection && e.PropertyName == nameof(RevisionGridViewModel.SelectedRow))
            {
                grid.SetSelectedRows(grid.SelectedRow is { } row ? [row] : []);
            }
        };
        LogViewModel viewModel = new(new LogStrings(), host, grid, viewer, new FileStatusListStrings(), new FileStatusTreeOptions(), Changed.ObjectId);
        return (viewModel, host, viewer);
    }

    /// <summary>Selects the revisions, the latest selected last (as the view reports them).</summary>
    private static void Select(LogViewModel viewModel, params GitRevision[] revisions)
        => viewModel.Grid.SetSelectedRows([.. revisions.Select(r => viewModel.Grid.Rows.Single(row => row.Revision == r))]);

    internal sealed class FakeHost : ILogHost
    {
        public List<string> Diffs { get; } = [];

        public List<string> Viewed { get; } = [];

        public Task<IReadOnlyList<FileStatusGroup>> GetDiffsAsync(IReadOnlyList<GitRevision> revisions, CancellationToken cancellationToken)
        {
            Diffs.Add(string.Join(",", revisions.Select(r => r.ObjectId.ToShortString())));
            GitRevision second = revisions[0];
            GitItemStatus file = new("src/file.cs") { IsChanged = true, IsTracked = true };
            return Task.FromResult<IReadOnlyList<FileStatusGroup>>([new FileStatusGroup(revisions.Count > 1 ? revisions[^1] : null, second, "", [file])]);
        }

        public void ViewRevisions(IReadOnlyList<GitRevision> revisions) => Viewed.Add(revisions[0].ObjectId.ToShortString());
    }
}
