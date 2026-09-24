using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using GitCommands.Git.Gpg;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.CommandsDialogs.BrowseDialog;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Services;
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
        window.MainMenu.Items.Cast<MenuItem>().Where(m => m.IsVisible).Select(m => m.Header).Should().Equal("_Start", "_Repository", "_Commands", "_Plugins", "_Tools", "_Help");
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

    [Test]
    public Task The_file_tree_tab_shows_all_the_files_of_the_selected_revision() => OnUiThreadAsync(() =>
    {
        (BrowseWindow window, BrowseViewModel viewModel, FakeBrowseHost host) = Show();
        DiffViewModelTests.FakeViewerHost viewerHost = host.ViewerHost;
        GitRevision first = viewModel.Grid.SelectedRow!.Revision;
        host.TreesRequested.Should().BeEmpty("the tree is loaded when its tab is shown (FillFileTree)");

        window.Tabs.SelectedIndex = 2;
        Dispatcher.UIThread.RunJobs();
        viewModel.SelectedTab.Should().Be(BrowseTab.FileTree);
        host.TreesRequested.Should().Equal(first.Subject);
        FileStatusListViewModel tree = viewModel.FileTree!;
        tree.AllEntries.Select(e => e.Item.Name).Should().BeEquivalentTo("README.md", "src/a.cs", "src/b.cs");

        // As the WinForms file tree: the files and folders of the root, the folders collapsed, and no file selected.
        tree.Nodes.Should().HaveCount(2);
        tree.Nodes.Single(n => n.Entry is null).IsExpanded.Should().BeFalse();
        tree.SelectedEntries.Should().BeEmpty();
        tree.ShowNoFiles.Should().BeFalse();

        tree.Select(e => e.Item.Name == "src/b.cs");
        Dispatcher.UIThread.RunJobs();
        viewerHost.Requested[^1].Should().Be($"src/b.cs@{first.ObjectId.ToShortString()}", "the file is shown as it is in the revision");
        SaveScreenshot(window.CaptureRenderedFrame(), "browse-file-tree");

        // Another revision: the same file stays selected, shown in that revision.
        GitRevision other = viewModel.Grid.Rows.Select(r => r.Revision).First(r => !r.IsArtificial && r.ObjectId != first.ObjectId);
        viewModel.Grid.SelectRevision(other.ObjectId);
        Dispatcher.UIThread.RunJobs();
        host.TreesRequested.Should().Equal(first.Subject, other.Subject);
        tree.SelectedEntry!.Item.Name.Should().Be("src/b.cs");
        viewerHost.Requested[^1].Should().Be($"src/b.cs@{other.ObjectId.ToShortString()}");

        // On another tab, selecting a revision does not load the tree.
        window.Tabs.SelectedIndex = 1;
        viewModel.Grid.SelectRevision(first.ObjectId);
        Dispatcher.UIThread.RunJobs();
        host.TreesRequested.Should().HaveCount(2);
        window.Close();
    });

    [Test]
    public Task The_gpg_tab_verifies_the_signatures_of_the_selected_commit() => OnUiThreadAsync(() =>
    {
        (BrowseWindow window, BrowseViewModel viewModel, FakeBrowseHost host) = Show();
        host.GpgRequested.Should().BeEmpty("the signatures are verified when the tab is shown");
        TaskCompletionSource<GpgInfo?> verification = new();
        host.GpgResult = verification.Task;

        window.Tabs.SelectedIndex = 3;
        Dispatcher.UIThread.RunJobs();
        viewModel.SelectedTab.Should().Be(BrowseTab.Gpg);
        host.GpgRequested.Should().Equal(viewModel.Grid.SelectedRow!.Subject);
        viewModel.CommitGpgText.Should().Be("Loading data...", "not the result of another revision while verifying");

        verification.SetResult(new GpgInfo(CommitStatus.GoodSignature, "gpg: Good signature from \"Alice\"", TagStatus.OneGood, "gpg: Good signature (tag)"));
        Dispatcher.UIThread.RunJobs();
        viewModel.CommitGpgIcon.Should().Be("CommitSignatureOk");
        viewModel.CommitGpgText.Should().Contain("Good signature from");
        viewModel.TagGpgIcon.Should().Be("TagOk");
        viewModel.TagGpgText.Should().Be("gpg: Good signature (tag)");
        SaveScreenshot(window.CaptureRenderedFrame(), "browse-gpg");

        // Neither signed: "not signed" and no tag row.
        host.GpgResult = Task.FromResult<GpgInfo?>(null);
        viewModel.Grid.SelectedRow = viewModel.Grid.Rows.First(r => r != viewModel.Grid.SelectedRow && !r.Revision.IsArtificial);
        Dispatcher.UIThread.RunJobs();
        host.GpgRequested.Should().HaveCount(2);
        viewModel.CommitGpgText.Should().Be("Commit is not signed");
        viewModel.CommitGpgIcon.Should().BeNull();
        viewModel.TagGpgText.Should().BeNull("without a tag the tag row is hidden");

        // A tag that is not signed.
        host.GpgResult = Task.FromResult<GpgInfo?>(new GpgInfo(CommitStatus.NoSignature, "", TagStatus.TagNotSigned, null));
        viewModel.Grid.SelectedRow = viewModel.Grid.Rows.First(r => r != viewModel.Grid.SelectedRow && !r.Revision.IsArtificial);
        Dispatcher.UIThread.RunJobs();
        viewModel.TagGpgText.Should().Be("Tag is not signed");
        viewModel.TagGpgIcon.Should().BeNull();
        window.Close();
    });

    [Test]
    public Task The_console_tab_starts_the_shell_when_it_is_first_shown() => OnUiThreadAsync(() =>
    {
        (BrowseWindow window, BrowseViewModel viewModel, FakeBrowseHost host) = Show();
        viewModel.HasConsole.Should().BeTrue();
        host.Terminals.Should().BeEmpty("the terminal is created when its tab is first selected");

        viewModel.SelectedTab = BrowseTab.Console;
        FakeTerminal terminal = host.Terminals.Single();
        viewModel.ConsoleView.Should().BeSameAs(terminal);
        terminal.Calls.Should().Equal("start");

        // Back to the tab: the running shell is focused; an exited one is started again.
        viewModel.SelectedTab = BrowseTab.Diff;
        viewModel.SelectedTab = BrowseTab.Console;
        terminal.Calls.Should().Equal("start", "focus");
        terminal.IsShellRunning = false;
        viewModel.SelectedTab = BrowseTab.Diff;
        viewModel.SelectedTab = BrowseTab.Console;
        terminal.Calls.Should().Equal("start", "focus", "start");
        host.Terminals.Should().HaveCount(1);

        viewModel.Dispose();
        terminal.Calls[^1].Should().Be("dispose");
        window.Close();
    });

    [Test]
    public Task Another_repository_keeps_the_selected_tab() => OnUiThreadAsync(() =>
    {
        (BrowseWindow window, BrowseViewModel _, FakeBrowseHost _) = Show();
        window.Tabs.SelectedIndex = 2;
        Dispatcher.UIThread.RunJobs();

        // As SetGitModule: a new view model for the other repository; its file tree is loaded, as its tab is shown.
        (BrowseWindow other, BrowseViewModel viewModel, FakeBrowseHost host) = Show();
        other.Close();
        window.ShowViewModel(viewModel);
        Dispatcher.UIThread.RunJobs();
        viewModel.SelectedTab.Should().Be(BrowseTab.FileTree);
        window.Tabs.SelectedIndex.Should().Be(2);
        host.TreesRequested.Should().NotBeEmpty();
        window.Close();
    });

    [Test]
    public Task The_plugins_menu_lists_the_plugins_once_they_are_loaded() => OnUiThreadAsync(() =>
    {
        (BrowseWindow window, BrowseViewModel viewModel, FakeBrowseHost host) = Show();
        List<string> Headers() => [.. window.MainMenu.Items.Cast<MenuItem>().Where(m => m.IsVisible).Select(m => (string)m.Header!)];
        MenuItem Menu(string header) => window.MainMenu.Items.Cast<MenuItem>().Single(m => (string?)m.Header == header);

        // As pluginsLoadingToolStripMenuItem while the plugins load.
        MenuItem loading = Menu("_Plugins").Items.OfType<MenuItem>().First();
        loading.Header.Should().Be("Loading...");
        loading.IsEnabled.Should().BeFalse();

        host.LoadPlugins([new("Statistics", null, true, "statistics"), new("Plugin Manager", null, false, "manager"), new("Delete obsolete branches", null, true, "delete")], "GitHub");
        Dispatcher.UIThread.RunJobs();

        Headers().Should().Equal("_Start", "_Repository", "_Commands", "GitHub", "_Plugins", "_Tools", "_Help");
        List<object?> plugins = [.. Menu("_Plugins").Items.Select(i => i is MenuItem m ? m.Header : "-")];
        plugins.Should().Equal("Delete obsolete branches", "Statistics", "-", "Plugin Manager", "Plugins _settings...");
        MenuItem statistics = Menu("_Plugins").Items.OfType<MenuItem>().Single(m => (string?)m.Header == "Statistics");
        statistics.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
        host.PluginRuns.Should().Equal("statistics");

        MenuItem viewPullRequests = Menu("GitHub").Items.OfType<MenuItem>().Single(m => (string?)m.Header == "View _pull requests...");
        viewPullRequests.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
        host.HostCommands.Should().Equal(RepositoryHostCommand.ViewPullRequests);
        viewModel.Menus.Should().Contain(m => m.Header == "_Plugins");
        window.Close();
    });

    [Test]
    public Task The_navigate_and_view_menus_are_the_ones_of_the_grid() => OnUiThreadAsync(() =>
    {
        int built = 0;
        (BrowseWindow window, BrowseViewModel _, FakeBrowseHost _) = Show(navigate: () =>
        {
            built++;
            return [new MenuModelItem("Go to _parent commit", () => { })];
        });

        List<MenuItem> menus = [.. window.MainMenu.Items.Cast<MenuItem>()];
        menus.Where(m => m.IsVisible).Select(m => m.Header).Should().StartWith(["_Start", "_Repository", "_Navigate", "_Commands"], "View is hidden without items");
        MenuItem navigate = menus.Single(m => (string?)m.Header == "_Navigate");
        navigate.Items.OfType<MenuItem>().Select(m => m.Header).Should().Equal("Go to _parent commit");
        built.Should().Be(1);

        // Built again when the main menu closes, as the grid's settings may have changed.
        window.MainMenu.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(MenuBase.ClosedEvent));
        Dispatcher.UIThread.RunJobs();
        built.Should().Be(2);
        window.Close();
    });

    [Test]
    public Task The_toolbar_shows_the_status_the_working_directory_the_worktrees_and_the_shells() => OnUiThreadAsync(() =>
    {
        (BrowseWindow window, BrowseViewModel viewModel, FakeBrowseHost host) = Show();

        // As UpdateCommitButtonAndGetBrush: the number of changes with the image of the state.
        TextBlock commitText = window.FindControl<TextBlock>("commitButtonText")!;
        commitText.Text.Should().Be("Commit");
        host.RaiseStatus(new BrowseWorkingDirectoryStatus(3, "RepoStateDirty"));
        Dispatcher.UIThread.RunJobs();
        commitText.Text.Should().Be("Commit (3)");
        viewModel.CommitButtonIcon.Should().Be("RepoStateDirty");
        host.RaiseStatus(new BrowseWorkingDirectoryStatus(null, null));
        viewModel.CommitButtonText.Should().Be("Commit");

        // As WorkingDirectoryToolStripSplitButton.
        host.RecentRepositoriesMenu = [new("~/other", null) { Invoke = () => { } }];
        viewModel.WorkingDirectoryText.Should().Be("~/repo");
        viewModel.GetWorkingDirectoryItems().Select(i => i.Header).Should().Equal(
            "_Favorite repositories", "-", "~/other", "-", "_Open...", "_Close (go to Dashboard)", "-", "Co_nfigure this menu...");

        // As UpdateWorktreeToolStipVisibility: shown with more than one worktree.
        window.FindControl<SplitButton>("worktreesButton")!.IsVisible.Should().BeTrue();
        viewModel.WorktreeItems.Select(i => (i.Header, i.IsChecked)).Should().Equal(("repo", true), ("repo-feature", false));

        // As userShell_Click: the button runs the first shell.
        viewModel.Shells.Select(s => s.Name).Should().Equal("bash", "pwsh");
        window.FindControl<SplitButton>("userShellButton")!.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(SplitButton.ClickEvent));
        viewModel.ShellItems[1].Invoke!();
        host.ShellRuns.Should().Equal("bash", "pwsh");

        // As LoadUserMenu: a button for each script of the user menu bar.
        Button script = window.FindControl<StackPanel>("scriptsToolBar")!.Children.OfType<Button>().Single();
        script.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        host.ShellRuns[^1].Should().Be("script Deploy");
        SaveScreenshot(window.CaptureRenderedFrame(), "browse-toolbar");
        window.Close();
    });

    [Test]
    public Task The_hotkeys_run_the_commands_show_the_tabs_and_run_the_scripts() => OnUiThreadAsync(() =>
    {
        (BrowseWindow window, BrowseViewModel viewModel, FakeBrowseHost host) = Show();

        viewModel.ExecuteHotkeyCommand((int)BrowseHotkeyCommand.Commit).Should().BeTrue();
        viewModel.ExecuteHotkeyCommand((int)BrowseHotkeyCommand.QuickPush).Should().BeTrue();
        host.Runs.Select(r => r.Command).Should().Equal(BrowseCommand.Commit, BrowseCommand.QuickPush);

        // As FocusDiff and FocusNextTab / FocusPrevTab (around the shown tabs).
        viewModel.ExecuteHotkeyCommand((int)BrowseHotkeyCommand.FocusDiff);
        Dispatcher.UIThread.RunJobs();
        window.Tabs.SelectedIndex.Should().Be((int)BrowseTab.Diff);
        viewModel.ExecuteHotkeyCommand((int)BrowseHotkeyCommand.FocusPrevTab);
        viewModel.SelectedTab.Should().Be(BrowseTab.Commit);
        viewModel.ExecuteHotkeyCommand((int)BrowseHotkeyCommand.FocusPrevTab);
        viewModel.SelectedTab.Should().Be(BrowseTab.Console, "the last shown tab");

        BrowseFocusTarget? focused = null;
        viewModel.FocusRequested += (_, target) => focused = target;
        viewModel.ExecuteHotkeyCommand((int)BrowseHotkeyCommand.FocusFilter);
        focused.Should().Be(BrowseFocusTarget.Filter);

        // The hotkeys of the scripts.
        viewModel.ExecuteHotkeyCommand(9001).Should().BeTrue();
        viewModel.ExecuteHotkeyCommand(9002).Should().BeFalse();
        host.ShellRuns.Should().Equal("script 9001");
        window.Close();
    });

    private static (BrowseWindow Window, BrowseViewModel ViewModel, FakeBrowseHost Host) Show(Func<IReadOnlyList<MenuModelItem>>? navigate = null)
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
            host.ViewerHost,
            new FileStatusListStrings(),
            new FileStatusTreeOptions())
        {
            NavigateMenuProvider = navigate,
        };
        BrowseWindow window = new() { Width = 1100, Height = 760, DataContext = viewModel };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, viewModel, host);
    }

    internal sealed class FakeTerminal : IBrowseTerminal, IEmbeddedNativeView
    {
        public List<string> Calls { get; } = [];

        public IEmbeddedNativeView View => this;

        public bool IsShellRunning { get; set; }

        public void StartShell()
        {
            Calls.Add("start");
            IsShellRunning = true;
        }

        public void Focus() => Calls.Add("focus");

        public void Dispose() => Calls.Add("dispose");

        public nint Attach(nint parentWindow) => 0;

        public void Detach()
        {
        }
    }

    internal sealed class FakeBrowseHost : IBrowseHost, IBrowseFileTreeHost, IBrowseGpgHost, IBrowseConsoleHost, IBrowsePluginsHost, IBrowseToolbarHost, IBrowseStatusHost, IBrowseScriptsHost
    {
        public bool RunScriptOfHotkey(int commandCode)
        {
            if (commandCode != 9001)
            {
                return false;
            }

            ShellRuns.Add($"script {commandCode}");
            return true;
        }

        public event EventHandler<BrowseWorkingDirectoryStatus>? WorkingDirectoryStatusChanged;

        public List<string> ShellRuns { get; } = [];

        public string WorkingDirectoryCaption => "~/repo";

        public void RaiseStatus(BrowseWorkingDirectoryStatus status) => WorkingDirectoryStatusChanged?.Invoke(this, status);

        public Task<BrowseWorktreeMenu> GetWorktreeMenuAsync()
            => Task.FromResult(new BrowseWorktreeMenu(2, [new("repo", null) { IsChecked = true, Invoke = () => { } }, new("repo-feature", null) { IsChecked = false, Invoke = () => { } }]));

        public IReadOnlyList<BrowseShell> GetShells() => [new("bash", null, "bash"), new("pwsh", null, "pwsh")];

        public void RunShell(BrowseShell shell) => ShellRuns.Add(shell.Name);

        public IReadOnlyList<BrowseMenuItem> GetToolbarScripts() => [new("Deploy", null) { Invoke = () => ShellRuns.Add("script Deploy") }];

        public event EventHandler? PluginsChanged;

        public IReadOnlyList<BrowsePlugin>? Plugins { get; private set; }

        public string? RepositoryHostName { get; private set; }

        public List<object> PluginRuns { get; } = [];

        public List<RepositoryHostCommand> HostCommands { get; } = [];

        public void LoadPlugins(IReadOnlyList<BrowsePlugin> plugins, string? hostName)
        {
            Plugins = plugins;
            RepositoryHostName = hostName;
            PluginsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RunPlugin(BrowsePlugin plugin) => PluginRuns.Add(plugin.Plugin);

        public void OpenPluginSettings() => PluginRuns.Add("settings");

        public void RunRepositoryHostCommand(RepositoryHostCommand command) => HostCommands.Add(command);

        public List<string> GpgRequested { get; } = [];

        public Task<GpgInfo?> GpgResult { get; set; } = Task.FromResult<GpgInfo?>(null);

        public bool ShowGpgInformation => true;

        public Task<GpgInfo?> LoadGpgInfoAsync(GitRevision revision)
        {
            GpgRequested.Add(revision.Subject);
#pragma warning disable VSTHRD003 // The test completes the task.
            return GpgResult;
#pragma warning restore VSTHRD003
        }

        public List<FakeTerminal> Terminals { get; } = [];

        public bool IsConsoleAvailable => true;

        public IBrowseTerminal? CreateTerminal()
        {
            FakeTerminal terminal = new();
            Terminals.Add(terminal);
            return terminal;
        }

        public DiffViewModelTests.FakeViewerHost ViewerHost { get; } = new();

        public List<string> TreesRequested { get; } = [];

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

        public Task<FileStatusGroup> GetTreeFilesAsync(GitRevision revision, CancellationToken cancellationToken)
        {
            TreesRequested.Add(revision.Subject);
            GitItemStatus[] files = [.. new[] { "README.md", "src/a.cs", "src/b.cs" }.Select(name => new GitItemStatus(name) { IsTracked = true })];
            return Task.FromResult(new FileStatusGroup(null, revision, $"grep:  {revision.ObjectId.ToShortString()}", files, IconName: FileStatusIcons.GitGrepIconName));
        }

        public IReadOnlyList<BrowseMenuItem> GetRepositoriesMenu(bool favourites) => favourites ? FavouriteRepositoriesMenu : RecentRepositoriesMenu;

        public void RaiseRepositoryChanged() => RepositoryChanged?.Invoke(this, EventArgs.Empty);
    }
}
