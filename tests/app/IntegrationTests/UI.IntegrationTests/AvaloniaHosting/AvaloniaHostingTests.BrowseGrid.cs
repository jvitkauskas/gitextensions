using System.Reactive.Concurrency;
using System.Reactive.Linq;
using GitCommands;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.BuildServerIntegration;
using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Settings;
using GitExtUtils;
using GitUI;
using GitUI.Avalonia.CommandsDialogs.BrowseDialog;
using GitUI.Avalonia.Hosting;
using GitUI.AvaloniaHosting;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Services;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUIPluginInterfaces;
using GitUIPluginInterfaces.BuildServerIntegration;
using NSubstitute;
using ResourceManager;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 4: the revision grid of the Avalonia main window on a real repository: stashes, hotkeys, build statuses.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void The_grid_of_the_main_window_shows_the_stashes_as_rows_and_runs_its_hotkeys()
    {
        Environment.SetEnvironmentVariable(AvaloniaUi.EnvironmentVariable, "all,FormBrowse");
        string head = _referenceRepository.CommitHash!;

        // An older stash of a tracked file, and a newer one of an untracked file only (git stash -u).
        _referenceRepository.Stash("first stash", "stashed content");
        File.WriteAllText(Path.Combine(_referenceRepository.Module.WorkingDir, "untracked.txt"), "untracked content");
        _referenceRepository.Module.GitExecutable.GetOutput("stash push -u -m \"second stash\"");

        // The "RevisionGrid" hotkeys of the settings (as HotkeySettingsLoader.LoadHotkeys).
        System.ComponentModel.Design.ServiceContainer services = GlobalServiceContainer.CreateDefaultMockServiceContainer();
        IHotkeySettingsLoader hotkeys = services.GetRequiredService<IHotkeySettingsLoader>();
        hotkeys.LoadHotkeys(RevisionGridControl.HotkeySettingsName).Returns(
            [new HotkeyCommand((int)RevisionGridControl.Command.GoToParent, "GoToParent") { KeyData = Keys.Control | Keys.P }]);
        GitUICommands commands = new(services, _referenceRepository.Module);

        List<GridRowInfo> rows = [];
        List<string> focusedMenu = [];
        List<string> otherActions = [];
        bool? showStashesChecked = null;
        string? showStashesGesture = null;
        ObjectId? afterHotkey = null;
        bool closed = false;
        DriveNextDialog(window =>
        {
            window.Closed += (_, _) => closed = true;
            BrowseViewModel viewModel = (BrowseViewModel)window.DataContext!;
            WaitUntil(
                () => viewModel.Grid.Rows.Any(r => r.ObjectId.ToString() == head) && !viewModel.Grid.IsLoading,
                () =>
                {
                    RevisionGridViewModel grid = viewModel.Grid;
                    rows.AddRange(grid.Rows.Select(r => new GridRowInfo(r.ObjectId, r.Subject, [.. r.Refs.Select(i => i.Name)], r.Revision.ParentIds ?? [], r.Index)));
                    Capture(window, "browse-grid-stashes");

                    // The View menu (not focused on a reference).
                    RevisionGridRow headRow = grid.Rows.Single(r => r.ObjectId.ToString() == head);
                    grid.SelectedRow = headRow;
                    MenuModelItem view = grid.ContextMenuProvider!().Single(m => m.Header == "View");
                    MenuModelItem showStashes = view.Children!.Single(m => m.Header == "Show stashes");
                    showStashesChecked = showStashes.IsChecked;
                    showStashesGesture = view.Children!.Single(m => m.Header == "Show git _notes").Gesture;

                    // The menu of the label of the branch: its actions, the advanced items under "Other actions".
                    IGitRef branch = headRow.Refs.First(r => r.GitRef is { IsHead: true }).GitRef!;
                    grid.RefMenuRequest = new RevisionGridRefMenuRequest(branch, Shift: false, Control: false);
                    IReadOnlyList<MenuModelItem> menu = grid.ContextMenuProvider!();
                    focusedMenu.AddRange(menu.Select(m => m.Header));
                    otherActions.AddRange(menu.Single(m => m.Header == "_Other actions").Children!.Select(m => m.Header));

                    // Ctrl+P (GoToParent) in the grid.
                    RevisionGridRow stashRow = grid.Rows.Single(r => r.Refs.Any(i => i.Name == "stash@{1}"));
                    grid.SelectedRow = stashRow;
                    ((BrowseWindow)window).RevisionGrid.ProcessHotkey((int)(Keys.Control | Keys.P)).Should().BeTrue();
                    afterHotkey = grid.SelectedRow?.ObjectId;
                    window.Close();
                });
        });

        commands.StartBrowseDialog(_owner, new BrowseArguments()).Should().BeTrue();
        WaitForMainWindowToClose(() => closed);

        // As the WinForms grid: each stash with its reflog selector, before the commit it was made on; the untracked files
        // commit (with changes) after its stash; no index commits.
        rows.Should().NotContain(r => r.Subject.StartsWith("index on"));
        GridRowInfo newest = rows.Single(r => r.Labels.Contains("stash@{0}"));
        GridRowInfo oldest = rows.Single(r => r.Labels.Contains("stash@{1}"));
        GridRowInfo untracked = rows.Single(r => r.Subject.StartsWith("untracked files on"));
        GridRowInfo headRevision = rows.Single(r => r.Id == ObjectId.Parse(head));
        newest.Subject.Should().Contain("second stash");
        oldest.Subject.Should().Contain("first stash");
        newest.Parents.Should().Equal([ObjectId.Parse(head), untracked.Id], "the index commit is not a parent in the grid");
        oldest.Parents.Should().Equal(ObjectId.Parse(head));
        new[] { newest.Index, oldest.Index, untracked.Index }.Should().OnlyContain(i => i < headRevision.Index, "before the commit they were made on");
        untracked.Index.Should().BeGreaterThan(newest.Index);
        rows.Where(r => r.Labels.Contains("stash")).Should().BeEmpty("the stash ref is shown as the stash rows");

        showStashesChecked.Should().BeTrue();
        showStashesGesture.Should().BeNull("no hotkey is configured for it");
        focusedMenu.Should().Contain("_Other actions").And.Contain("Pus_h branch...").And.NotContain("Checkout _this commit...");
        otherActions.Should().Contain("Checkout _this commit...").And.Contain("_Navigate").And.Contain("View");
        afterHotkey.Should().Be(ObjectId.Parse(head), "the parent of the stash");
    }

    [Test]
    public void The_grid_shows_the_build_statuses_and_the_build_report_tab_of_the_selected_commit()
    {
        Environment.SetEnvironmentVariable(AvaloniaUi.EnvironmentVariable, "all,FormBrowse");
        _referenceRepository.CreateCommit("Second commit", "changed content", "file.txt");
        string head = _referenceRepository.CommitHash!;
        FakeBuildServerAdapter adapter = new(new BuildInfo
        {
            Id = "42",
            Status = BuildStatus.Success,
            Description = "#42 succeeded",
            StartDate = DateTime.Now,
            CommitHashList = [ObjectId.Parse(head)],

            // A local page: no network.
            Url = "about:blank",
        });
        AvaloniaDialogs.BuildServerAdapterForTests = () => adapter;
        AvaloniaDialogs.ShowBuildResultPageForTests = true;
        try
        {
            string? symbol = null;
            bool columnShown = false;
            bool tabShown = false;
            bool reportInTab = false;
            bool webViewCreated = false;
            List<string> menu = [];
            bool closed = false;
            DriveNextDialog(window =>
            {
                window.Closed += (_, _) => closed = true;
                BrowseViewModel viewModel = (BrowseViewModel)window.DataContext!;
                WaitUntil(
                    () => viewModel.Grid.Rows.FirstOrDefault(r => r.ObjectId.ToString() == head) is { BuildStatus: not null } && viewModel.HasBuildReport,
                    () =>
                    {
                        RevisionGridRow row = viewModel.Grid.Rows.Single(r => r.ObjectId.ToString() == head);
                        symbol = row.BuildStatusSymbol;
                        columnShown = viewModel.Grid.ShowBuildStatusColumn;
                        tabShown = ((BrowseWindow)window).BuildReportTab.IsVisible;
                        reportInTab = viewModel.IsBuildReportInTab;
                        viewModel.Grid.SelectedRow = row;
                        menu.AddRange(viewModel.Grid.ContextMenuProvider!().Select(m => m.Header));

                        // The report in the embedded web browser once its tab is shown.
                        ((BrowseWindow)window).Tabs.SelectedIndex = (int)BrowseTab.BuildReport;
                        WaitUntil(
                            () => viewModel.BuildReportView is not null,
                            () =>
                            {
                                webViewCreated = viewModel.BuildReportView is not null;
                                Capture(window, "browse-build-report");
                                window.Close();
                            });
                    });
            });

            _commands.StartBrowseDialog(_owner, new BrowseArguments()).Should().BeTrue();
            WaitForMainWindowToClose(() => closed);

            adapter.Initialized.Should().BeTrue();
            symbol.Should().Be("✔");
            columnShown.Should().BeTrue("a build server was found");
            tabShown.Should().BeTrue();
            reportInTab.Should().BeTrue();
            webViewCreated.Should().BeTrue();
            menu.Should().Contain("View _build report in a browser");
            adapter.Disposed.Should().BeTrue("disposed with the window");
        }
        finally
        {
            AvaloniaDialogs.BuildServerAdapterForTests = null;
            AvaloniaDialogs.ShowBuildResultPageForTests = null;
        }
    }

    private sealed record GridRowInfo(ObjectId Id, string Subject, IReadOnlyList<string> Labels, IReadOnlyList<ObjectId> Parents, int Index);

    /// <summary>A build server reporting one finished build, and no running build.</summary>
    private sealed class FakeBuildServerAdapter(BuildInfo build) : IBuildServerAdapter
    {
        public bool Initialized { get; private set; }

        public bool Disposed { get; private set; }

        public string UniqueKey => "fake";

        public void Initialize(IBuildServerWatcher buildServerWatcher, SettingsSource config, Action openSettings, Func<ObjectId, bool>? isCommitInRevisionGrid = null)
            => Initialized = true;

        public IObservable<BuildInfo> GetFinishedBuildsSince(IScheduler scheduler, DateTime? sinceDate = null) => Observable.Return(build, scheduler);

        public IObservable<BuildInfo> GetRunningBuilds(IScheduler scheduler) => Observable.Empty<BuildInfo>(scheduler);

        public void Dispose() => Disposed = true;
    }
}
