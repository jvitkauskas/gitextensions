using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.CommandsDialogs.BrowseDialog;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUIPluginInterfaces;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless tests of the main window (the port of <c>FormBrowse</c>, first part).</summary>
[TestFixture]
public sealed class BrowseViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        (BrowseWindow window, BrowseViewModel viewModel, FakeBrowseHost host) = Show();

        window.Title.Should().Be("repo (main) - Git Extensions");
        viewModel.CurrentBranch.Should().Be("main");
        viewModel.Grid.SelectedRow.Should().BeSameAs(viewModel.Grid.Rows[0], "the first revision is selected once loaded");
        viewModel.CommitInfo.RevisionInfo.Should().NotBeEmpty("the commit info of the selected revision is shown");
        host.DiffsRequested.Should().ContainSingle().Which.Should().Equal(viewModel.Grid.Rows[0].Subject);
        viewModel.Files.AllEntries.Select(e => e.Item.Name).Should().Equal("src/file.cs");
        window.MainMenu.Items.Cast<MenuItem>().Select(m => m.Header).Should().Equal("_Start", "_Repository", "_Commands", "_Tools", "_Help");
        SaveScreenshot(window.CaptureRenderedFrame(), $"browse-commit-{theme}");

        window.Tabs.SelectedIndex = 1;
        Dispatcher.UIThread.RunJobs();
        viewModel.SelectedTab.Should().Be(BrowseTab.Diff);
        SaveScreenshot(window.CaptureRenderedFrame(), $"browse-diff-{theme}");
        window.Close();
    });

    [Test]
    public Task The_menus_and_the_toolbar_run_their_commands_on_the_selection() => OnUiThreadAsync(() =>
    {
        (BrowseWindow window, BrowseViewModel viewModel, FakeBrowseHost host) = Show();
        List<GitRevision> history = [.. viewModel.Grid.Rows.Select(r => r.Revision)];
        viewModel.Grid.SetSelectedRows([viewModel.Grid.Rows[3], viewModel.Grid.Rows[1]]);
        Dispatcher.UIThread.RunJobs();

        MenuItem commands = window.MainMenu.Items.Cast<MenuItem>().Single(m => (string?)m.Header == "_Commands");
        MenuItem cherryPick = commands.Items.OfType<MenuItem>().Single(m => (string?)m.Header == "Cherr_y pick...");
        cherryPick.Command!.Execute(cherryPick.CommandParameter);
        host.Runs.Should().ContainSingle();
        (BrowseCommand command, BrowseSelection selection) = host.Runs[0];
        command.Should().Be(BrowseCommand.CherryPick);
        selection.LatestSelectedFirst.Select(r => r.Subject).Should().Equal(history[1].Subject, history[3].Subject);
        selection.Descending.Select(r => r.Subject).Should().Equal(history[3].Subject, history[1].Subject);

        viewModel.RunCommand.Execute(BrowseCommand.PullDefault);
        host.Runs[^1].Command.Should().Be(BrowseCommand.PullDefault);
        viewModel.Menus[0].Children!.Should().Contain(m => m.IsSeparator);
        viewModel.PullItems.Where(i => !i.IsSeparator).Select(i => i.Command).Should().Equal(
            BrowseCommand.PullMerge, BrowseCommand.PullRebase, BrowseCommand.Fetch, BrowseCommand.FetchAll, BrowseCommand.FetchPruneAll, BrowseCommand.OpenPullDialog);

        // A dialog changed the repository: the revisions are loaded again, keeping the selection.
        host.Branch = "feature";
        host.RaiseRepositoryChanged();
        Dispatcher.UIThread.RunJobs();
        viewModel.CurrentBranch.Should().Be("feature");
        viewModel.Grid.SelectedRow.Should().NotBeNull();

        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;
        viewModel.RunCommand.Execute(BrowseCommand.Exit);
        closed.Should().BeTrue();
        window.Close();
    });

    private static (BrowseWindow Window, BrowseViewModel ViewModel, FakeBrowseHost Host) Show()
    {
        FakeBrowseHost host = new();
        RevisionGridViewModel grid = new(new RevisionGridViewTests.FakeRevisionGridHost(RevisionGridViewTests.CreateHistory()), new RevisionGridDisplayOptions(RelativeDate: true, ShowAuthorDate: false))
        {
            MultiSelect = true,
        };
        BrowseViewModel viewModel = new(
            new BrowseStrings(),
            host,
            grid,
            new CommitInfoViewTests.FakeHost(),
            new DiffViewModelTests.FakeViewerHost(),
            new FileStatusListStrings(),
            new FileStatusTreeOptions());
        BrowseWindow window = new() { Width = 1100, Height = 760, DataContext = viewModel };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, viewModel, host);
    }

    internal sealed class FakeBrowseHost : IBrowseHost
    {
        public event EventHandler? RepositoryChanged;

        public string Branch { get; set; } = "main";

        public List<(BrowseCommand Command, BrowseSelection Selection)> Runs { get; } = [];

        public List<IReadOnlyList<string>> DiffsRequested { get; } = [];

        public IReadOnlyList<BrowseMenuItem> RecentRepositoriesMenu { get; set; } = [];

        public IReadOnlyList<BrowseMenuItem> FavouriteRepositoriesMenu { get; set; } = [];

        public string GetTitle() => $"repo ({Branch}) - Git Extensions";

        public string GetCurrentBranch() => Branch;

        public void Run(BrowseCommand command, BrowseSelection selection) => Runs.Add((command, selection));

        public Task<IReadOnlyList<FileStatusGroup>> GetDiffsAsync(IReadOnlyList<GitRevision> revisions, CancellationToken cancellationToken)
        {
            DiffsRequested.Add([.. revisions.Select(r => r.Subject)]);
            GitRevision second = revisions[0];
            GitRevision first = new(second.FirstParentId);
            GitItemStatus file = new(name: "src/file.cs") { IsTracked = true, IsChanged = true };
            return Task.FromResult<IReadOnlyList<FileStatusGroup>>([new FileStatusGroup(first, second, "Parent", [file])]);
        }

        public IReadOnlyList<BrowseMenuItem> GetRepositoriesMenu(bool favourites) => favourites ? FavouriteRepositoriesMenu : RecentRepositoriesMenu;

        public void RaiseRepositoryChanged() => RepositoryChanged?.Invoke(this, EventArgs.Empty);
    }
}
