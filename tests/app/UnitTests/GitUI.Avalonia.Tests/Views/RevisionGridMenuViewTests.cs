using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.Controls.RevisionGrid;
using GitUI.Presentation.Services;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUI.UserControls.RevisionGrid.Graph;
using GitUIPluginInterfaces;

namespace GitUI.AvaloniaTests.Views;

/// <summary>The context menu, the navigation and the columns of the revision grid (phase 4).</summary>
[TestFixture]
public sealed class RevisionGridMenuViewTests : HeadlessTest
{
    [Test]
    public void The_separators_of_a_menu_are_trimmed()
    {
        MenuModelItem a = new("a", () => { });
        MenuModelItem b = new("b", () => { });
        MenuModelItem.TrimSeparators([MenuModelItem.Separator, a, MenuModelItem.Separator, MenuModelItem.Separator, b, MenuModelItem.Separator])
            .Should().Equal(a, MenuModelItem.Separator, b);
        new MenuModelItem("-", () => { }).IsSeparator.Should().BeFalse("an item with an action is not a separator");
    }

    [Test]
    public Task The_grid_opens_the_menu_of_the_provider_with_its_submenus_and_check_items() => OnUiThreadAsync(() =>
    {
        (Window window, RevisionGridView view, RevisionGridViewModel viewModel) = Show();
        int executed = 0;
        int built = 0;
        viewModel.ContextMenuProvider = () =>
        {
            built++;
            return
            [
                new("_Copy", () => executed++, "CopyToClipboard", Gesture: "Ctrl+C"),
                MenuModelItem.Separator,
                new("_View", Children: [new("Show _tags", () => { }, IsChecked: true), new("Caption", IsEnabled: false, IsBold: true)]),
            ];
        };

        view.OpenContextMenu();
        Dispatcher.UIThread.RunJobs();

        built.Should().Be(1, "the menu is built when it opens");
        ContextMenu menu = view.LastContextMenu!;
        menu.Items.Should().HaveCount(3);
        MenuItem copy = (MenuItem)menu.Items[0]!;
        copy.Header.Should().Be("_Copy");
        copy.Icon.Should().BeOfType<Image>();
        copy.InputGesture!.ToString().Should().Be("Ctrl+C");
        copy.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
        executed.Should().Be(1);
        MenuItem viewMenu = (MenuItem)menu.Items[2]!;
        List<MenuItem> children = [.. viewMenu.ItemsSource!.Cast<MenuItem>()];
        children[0].ToggleType.Should().Be(MenuItemToggleType.CheckBox);
        children[0].IsChecked.Should().BeTrue();
        children[1].IsEnabled.Should().BeFalse();
        menu.Close();
        window.Close();
    });

    [Test]
    public Task The_grid_navigates_to_parents_children_and_back() => OnUiThreadAsync(() =>
    {
        (Window window, RevisionGridView _, RevisionGridViewModel viewModel) = Show();
        List<RevisionGridRow> rows = [.. viewModel.Rows];
        viewModel.SelectedRow = rows[0];

        // The head's first parent, then back to the child it came from.
        viewModel.GoToParent();
        viewModel.SelectedRow!.ObjectId.Should().Be(rows[0].Revision.FirstParentId);
        viewModel.GoToChild();
        viewModel.SelectedRow.Should().BeSameAs(rows[0], "back to the child navigated from");

        viewModel.GoToFirstParent();
        ObjectId parent = viewModel.SelectedRow!.ObjectId;
        viewModel.NavigateBackward();
        viewModel.SelectedRow.Should().BeSameAs(rows[0]);
        viewModel.CanNavigateForward.Should().BeTrue();
        viewModel.NavigateForward();
        viewModel.SelectedRow!.ObjectId.Should().Be(parent);
        viewModel.GetChildren(parent).Should().Contain(rows[0].ObjectId);

        // A merge commit: the last parent.
        RevisionGridRow merge = rows.First(r => r.Revision.ParentIds?.Count > 1);
        viewModel.SelectedRow = merge;
        viewModel.GoToLastParent();
        viewModel.SelectedRow!.ObjectId.Should().Be(merge.Revision.ParentIds![^1]);
        window.Close();
    });

    [Test]
    public Task The_columns_follow_their_settings() => OnUiThreadAsync(() =>
    {
        (Window window, RevisionGridView view, RevisionGridViewModel viewModel) = Show();
        DataGrid grid = view.FindControl<DataGrid>("revisionsGrid")!;

        viewModel.ShowAuthorColumn = false;
        viewModel.ShowIdColumn = false;
        Dispatcher.UIThread.RunJobs();

        // Graph, message, notes, avatar, author, date, id and build status, as the columns of RevisionGridControl.
        grid.Columns.Select(c => c.IsVisible).Should().Equal(true, true, false, false, false, true, false, false);

        viewModel.ShowNotesColumn = true;
        viewModel.ShowAvatarColumn = true;
        viewModel.ShowBuildStatusColumn = true;
        Dispatcher.UIThread.RunJobs();
        grid.Columns.Select(c => c.IsVisible).Should().Equal(true, true, true, true, false, true, false, true);
        grid.Columns[7].Width.Value.Should().Be(24, "the build status icon only");
        viewModel.ShowBuildStatusText = true;
        Dispatcher.UIThread.RunJobs();
        grid.Columns[7].Width.Value.Should().Be(150, "the build status text too");
        viewModel.ShowBuildStatusIcon = false;
        viewModel.ShowBuildStatusText = false;
        Dispatcher.UIThread.RunJobs();
        grid.Columns[7].IsVisible.Should().BeFalse("neither the icon nor the text is shown");

        // Without the remote branches and the tags, the rows are loaded again without them.
        viewModel.DisplayOptions = viewModel.DisplayOptions with { ShowRemoteBranches = false, ShowTags = false };
        Dispatcher.UIThread.RunJobs();
        viewModel.Rows.SelectMany(r => r.Refs).Should().OnlyContain(r => r.Kind != RevisionRefKind.RemoteBranch && r.Kind != RevisionRefKind.Tag);
        window.Close();
    });

    [Test]
    public Task A_disposed_grid_ignores_later_loads() => OnUiThreadAsync(() =>
    {
        // E.g. a repository change (after a rebase) reaching the grid of a closed window.
        (Window window, RevisionGridView _, RevisionGridViewModel viewModel) = Show();
        window.Close();
        viewModel.Dispose();

        viewModel.Invoking(grid => grid.Load()).Should().NotThrow();
        viewModel.Rows.Should().NotBeEmpty("the rows are not loaded again");
    });

    [Test]
    public Task The_rows_follow_revisions_inserted_before_them() => OnUiThreadAsync(() =>
    {
        // As the artificial commits when HEAD is filtered out: inserted first, after the rows of the listed revisions.
        IReadOnlyList<GitRevision> history = RevisionGridViewTests.CreateHistory();
        RevisionGridViewModel viewModel = new(new InsertingHost(history), new RevisionGridDisplayOptions(RelativeDate: true, ShowAuthorDate: false));
        viewModel.Load();
        Dispatcher.UIThread.RunJobs();

        viewModel.Rows.Select(r => r.ObjectId).Should().Equal([ObjectId.WorkTreeId, ObjectId.IndexId, .. history.Select(r => r.ObjectId)]);
        viewModel.Rows.Select(r => r.Index).Should().Equal(Enumerable.Range(0, history.Count + 2));
    });

    private sealed class InsertingHost(IReadOnlyList<GitRevision> revisions) : IRevisionGridHost
    {
        public string CurrentBranch => "main";

        public bool MatchesQuickSearch(GitRevision revision, string criteria) => false;

        public void LoadRevisions(RevisionGraph graph, Action reportBatch, Action<Exception?> completed, CancellationToken cancellationToken)
        {
            foreach (GitRevision revision in revisions)
            {
                graph.Add(revision);
            }

            reportBatch();
            graph.Insert(new GitRevision(ObjectId.WorkTreeId) { ParentIds = [ObjectId.IndexId] }, new GitRevision(ObjectId.IndexId), []);
            completed(null);
        }

        public void RunInBackground(Action work, Action then)
        {
            work();
            then();
        }
    }

    private static (Window Window, RevisionGridView View, RevisionGridViewModel ViewModel) Show()
    {
        RevisionGridViewModel viewModel = new(new RevisionGridViewTests.FakeRevisionGridHost(RevisionGridViewTests.CreateHistory()), new RevisionGridDisplayOptions(RelativeDate: true, ShowAuthorDate: false))
        {
            MultiSelect = true,
        };
        RevisionGridView view = new() { DataContext = viewModel };
        Window window = new() { Width = 760, Height = 300, Content = view };
        window.Show();
        viewModel.Load();
        Dispatcher.UIThread.RunJobs();
        return (window, view, viewModel);
    }
}
