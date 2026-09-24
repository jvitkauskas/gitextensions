using CommonTestUtils;
using GitCommands;
using GitCommands.UserRepositoryHistory;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI;
using GitUI.Avalonia.CommandsDialogs.BrowseDialog;
using GitUI.Avalonia.Hosting;
using GitUI.AvaloniaHosting;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs.BrowseDialog;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 7: the Avalonia main window (named in <c>GE_AVALONIA</c> until it is complete) on a real repository.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void StartBrowseDialog_shows_the_revisions_the_commit_and_its_diff()
    {
        _referenceRepository.CreateCommit("Second commit", "changed content", "file.txt");
        string head = _referenceRepository.CommitHash!;

        string? title = null;
        List<string> subjects = [];
        List<string> files = [];
        string? commitInfo = null;
        bool closed = false;
        DriveNextDialog(window =>
        {
            window.Should().BeOfType<BrowseWindow>();
            window.Closed += (_, _) => closed = true;
            BrowseViewModel viewModel = (BrowseViewModel)window.DataContext!;
            WaitUntil(
                () => viewModel.Grid.Rows.Count > 0 && viewModel.Files.AllEntries.Any() && viewModel.CommitInfo.RevisionInfo.Length > 0,
                () =>
                {
                    title = window.Title;
                    subjects.AddRange(viewModel.Grid.Rows.Select(r => r.Subject));
                    files.AddRange(viewModel.Files.AllEntries.Select(e => e.Item.Name));
                    commitInfo = viewModel.CommitInfo.RevisionInfo;
                    ((BrowseWindow)window).Tabs.SelectedIndex = 1;
                    Capture(window, "browse");
                    window.Close();
                });
        });

        _commands.StartBrowseDialog(_owner, new BrowseArguments { SelectedId = GitExtensions.Extensibility.Git.ObjectId.Parse(head) }).Should().BeTrue();

        WaitForMainWindowToClose(() => closed);

        title.Should().NotBeNull("the title of IAppTitleGenerator (a mock here)");
        subjects.Should().Contain("Second commit");
        files.Should().Contain("file.txt", "the diff of the selected commit");
        commitInfo.Should().NotBeNullOrEmpty();
    }

    [Test]
    public void Browse_switches_to_the_dashboard_and_to_other_repositories_in_the_same_window()
    {
        string? recentWorkingDir = AppSettings.RecentWorkingDir;
        using ReferenceRepository other = new();
        other.CreateCommit("Commit of the other repository", "other content", "other.txt");
        ThreadHelper.JoinableTaskFactory.Run(() => _history.AddAsMostRecentAsync(other.Module.WorkingDir));
        try
        {
            List<BrowseViewModel> shown = [];
            List<string> dashboardMenus = [];
            List<string> dashboardPaths = [];
            List<string> otherSubjects = [];
            List<string> backSubjects = [];
            bool closed = false;
            DriveNextDialog(window =>
            {
                BrowseWindow browse = (BrowseWindow)window;
                window.Closed += (_, _) => closed = true;
                BrowseViewModel first = (BrowseViewModel)window.DataContext!;
                shown.Add(first);
                WaitUntil(
                    () => first.Grid.Rows.Count > 0,
                    () =>
                    {
                        // Repository > Close (go to Dashboard).
                        first.RunCommand.Execute(BrowseCommand.CloseRepository);
                        WaitUntil(
                            () => window.DataContext is BrowseViewModel { Dashboard: { } dashboard } && dashboard.Repositories.Count() >= 2 && dashboard.LoadingStatuses.IsCompleted,
                            () =>
                            {
                                BrowseViewModel dashboardViewModel = (BrowseViewModel)window.DataContext!;
                                shown.Add(dashboardViewModel);
                                dashboardMenus.AddRange(browse.MainMenu.Items.OfType<Avalonia.Controls.MenuItem>().Select(m => (string)m.Header!));
                                dashboardPaths.AddRange(dashboardViewModel.Dashboard!.Repositories.Select(r => r.Path));
                                Capture(window, "browse-dashboard");

                                // A tile of the dashboard opens its repository in this window.
                                DashboardRepositoryItem item = dashboardViewModel.Dashboard.Repositories.First(r => SamePath(r.Path, other.Module.WorkingDir));
                                dashboardViewModel.Dashboard.OpenCommand.Execute(item);
                                WaitUntil(
                                    () => window.DataContext is BrowseViewModel { IsDashboard: false } viewModel && viewModel.Grid.Rows.Count > 0,
                                    () =>
                                    {
                                        BrowseViewModel otherViewModel = (BrowseViewModel)window.DataContext!;
                                        shown.Add(otherViewModel);
                                        otherSubjects.AddRange(otherViewModel.Grid.Rows.Select(r => r.Subject));

                                        // As WorktreeSwitch: the main window owning the dialog shows the directory.
                                        AvaloniaDialogs.TrySetBrowseWorkingDir(new AvaloniaDialogs.NativeWindowOwner(window), _referenceRepository.Module.WorkingDir).Should().BeTrue();
                                        WaitUntil(
                                            () => window.DataContext is BrowseViewModel viewModel && viewModel != otherViewModel && viewModel.Grid.Rows.Count > 0,
                                            () =>
                                            {
                                                backSubjects.AddRange(((BrowseViewModel)window.DataContext!).Grid.Rows.Select(r => r.Subject));
                                                window.Close();
                                            });
                                    });
                            });
                    });
            });

            _commands.StartBrowseDialog(_owner, new BrowseArguments()).Should().BeTrue();

            WaitForMainWindowToClose(() => closed, seconds: 60);

            shown.Should().HaveCount(3);
            shown[0].IsDashboard.Should().BeFalse();
            shown[1].IsDashboard.Should().BeTrue("Close (go to Dashboard) shows the dashboard");
            dashboardMenus.Should().Equal("_Start", "_Dashboard", "_Tools", "_Help");
            dashboardPaths.Should().Contain(p => SamePath(p, other.Module.WorkingDir)).And.Contain(p => SamePath(p, _referenceRepository.Module.WorkingDir));
            otherSubjects.Should().Contain("Commit of the other repository");
            backSubjects.Should().NotContain("Commit of the other repository").And.NotBeEmpty();

            // The opened repositories are the most recent ones (as the working directory button of FormBrowse).
            IList<Repository> recent = ThreadHelper.JoinableTaskFactory.Run(_history.LoadRecentHistoryAsync);
            SamePath(recent[0].Path, _referenceRepository.Module.WorkingDir).Should().BeTrue();
            SamePath(recent[1].Path, other.Module.WorkingDir).Should().BeTrue();
        }
        finally
        {
            AppSettings.RecentWorkingDir = recentWorkingDir!;
        }
    }

    [Test]
    public void Browse_shows_the_dashboard_for_a_directory_that_is_not_a_repository()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"ge-dashboard-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        ThreadHelper.JoinableTaskFactory.Run(() => _history.AddAsMostRecentAsync(_referenceRepository.Module.WorkingDir));
        try
        {
            System.ComponentModel.Design.ServiceContainer services = GlobalServiceContainer.CreateDefaultMockServiceContainer();
            GitUICommands commands = new(services, new GitModule(services.GetRequiredService<IGitExecutorProvider>(), directory));
            bool isDashboard = false;
            bool tabsVisible = true;
            List<string> paths = [];
            bool closed = false;
            DriveNextDialog(window =>
            {
                window.Closed += (_, _) => closed = true;
                BrowseViewModel viewModel = (BrowseViewModel)window.DataContext!;
                WaitUntil(
                    () => viewModel.Dashboard?.Repositories.Any() is true,
                    () =>
                    {
                        isDashboard = viewModel.IsDashboard;
                        tabsVisible = ((BrowseWindow)window).Tabs.IsVisible;
                        paths.AddRange(viewModel.Dashboard!.Repositories.Select(r => r.Path));
                        window.Close();
                    });
            });

            commands.StartBrowseDialog(_owner, new BrowseArguments()).Should().BeTrue();

            WaitForMainWindowToClose(() => closed);

            isDashboard.Should().BeTrue();
            tabsVisible.Should().BeFalse("the dashboard replaces the grid and the tabs");
            paths.Should().ContainSingle().Which.Should().Match(p => SamePath(p, _referenceRepository.Module.WorkingDir));
            ThreadHelper.JoinableTaskFactory.Run(_history.LoadRecentHistoryAsync).Should().ContainSingle("a directory without a repository is not added");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    // With a message loop (as here, and once the application runs), the main window is modeless: wait for it.
    private void WaitForMainWindowToClose(Func<bool> closed, int seconds = 30)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(seconds);
        while (!closed() && DateTime.UtcNow < deadline)
        {
            MessagePump.DoEvents();
            Thread.Sleep(10);
        }

        closed().Should().BeTrue("the window was driven and closed");
        _driveFailure.Should().BeNull();
    }

    private static bool SamePath(string path, string other)
        => string.Equals(Path.GetFullPath(path).TrimEnd('\\', '/'), Path.GetFullPath(other).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);

    /// <summary>The storage of the repository history, in memory.</summary>
    private sealed class InMemoryRepositoryStorage : IRepositoryStorage
    {
        private readonly Dictionary<string, List<Repository>> _histories = [];

        public IReadOnlyList<Repository> Load(string key) => _histories.TryGetValue(key, out List<Repository>? history) ? [.. history] : [];

        public void Save(string key, IEnumerable<Repository> repositories) => _histories[key] = [.. repositories];
    }

    /// <summary>No legacy history (which the real migrator reads from the settings).</summary>
    private sealed class NoHistoryMigration : GitCommands.UserRepositoryHistory.Legacy.IRepositoryHistoryMigrator
    {
        public Task<(IList<Repository> history, bool changed)> MigrateAsync(IEnumerable<Repository> currentHistory)
            => Task.FromResult<(IList<Repository>, bool)>(([.. currentHistory], false));
    }
}
