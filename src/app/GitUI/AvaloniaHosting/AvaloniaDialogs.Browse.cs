using GitCommands;
using GitCommands.Git;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.CommandsDialogs.BrowseDialog;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.CommandsDialogs.BrowseDialog;
using GitUI.CommandsDialogs.WorktreeDialog;
using GitUI.HelperDialogs;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUI.Shells;
using GitUI.UserControls.RevisionGrid;
using GitUIPluginInterfaces;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  Routing of the main window (docs/avalonia-port/PLAN.md, phase 7). The port is unfinished, so it is used only when named
///  in <c>GE_AVALONIA</c> (e.g. <c>GE_AVALONIA=all,FormBrowse</c>).
/// </summary>
internal static partial class AvaloniaDialogs
{
    private static ApplicationContext? _browseContext;
    private static int _openBrowseWindows;

    /// <summary>
    ///  The Avalonia port of <see cref="GitUICommands.StartBrowseDialog"/>: the main window, shown modeless; without a message
    ///  loop yet (the start of the application), the loop runs until the last main window closes, as <c>Application.Run</c>.
    /// </summary>
    public static bool TryShowBrowse(IGitUICommands commands, BrowseArguments args)
    {
        if (!AvaloniaUi.IsExplicitlyEnabledFor(nameof(FormBrowse)))
        {
            return false;
        }

        AvaloniaUi.EnsureInitialized(GetOptions);
        ShowBrowseWindow(commands, args);
        if (!Application.MessageLoop)
        {
            using ApplicationContext context = new();
            _browseContext = context;
            Application.Run(context);
            _browseContext = null;
        }

        return true;
    }

    private static BrowseWindow ShowBrowseWindow(IGitUICommands commands, BrowseArguments args)
    {
        BrowseWindow window = new()
        {
            PositionName = nameof(FormBrowse),
            PositionStore = WindowPositionStore.Instance,
            SelectedId = args.SelectedId.IsZero ? null : args.SelectedId,
        };
        BrowseHost host = new(commands, window);
        RevisionGridHost gridHost = new(
            commands,
            currentCheckout => new FilterInfo().GetRevisionFilter(new Lazy<ObjectId>(() => currentCheckout)),
            showArtificial: true);
        RevisionGridViewModel grid = new(gridHost, GetDisplayOptions())
        {
            MultiSelect = true,
        };
        ApplyColumns(grid);
        BrowseViewModel? browseViewModel = null;
        RevisionGridMenuBuilder gridMenu = new((GitUICommands)commands, () => new NativeWindowOwner(window), grid, () => browseViewModel?.RefreshRevisions());
        grid.ContextMenuProvider = gridMenu.Build;
        BrowseViewModel viewModel = new(
            ViewStrings.Load<BrowseStrings>(),
            host,
            grid,
            new CommitInfoHost(commands),
            new FileViewerHost(commands),
            ViewStrings.Load<FileStatusListStrings>(),
            GetFileStatusTreeOptions());
        browseViewModel = viewModel;
        UseFileStatusListMenu(viewModel.Files, commands, window);
        if (viewModel.FileTree is { } fileTree)
        {
            // Not UseFileStatusListMenu: the sorting of the file tree is not the one of the diff lists.
            fileTree.MenuHost = new FileStatusListMenuHost(commands, window);
        }

        window.DataContext = viewModel;

        // As FormBrowse (IBrowseRepo): the scripts and the plugins see the selection of the grid.
        commands.BrowseRepo = new BrowseRepoAdapter(commands, grid, window);

        _openBrowseWindows++;
        window.Closed += (_, _) =>
        {
            host.Dispose();
            grid.Dispose();
            if (commands.BrowseRepo is BrowseRepoAdapter adapter && adapter.Window == window)
            {
                commands.BrowseRepo = null;
            }

            if (--_openBrowseWindows == 0)
            {
                _browseContext?.ExitThread();
            }
        };
        AvaloniaDialogHost.Show(window, ownerHandle: 0);
        return window;
    }

    /// <summary>The dialogs and the git operations of the menus of <c>FormBrowse</c>, for the Avalonia main window.</summary>
    private sealed partial class BrowseHost : IBrowseHost, IDisposable
    {
        private readonly IGitUICommands _commands;
        private readonly BrowseWindow _window;

        public BrowseHost(IGitUICommands commands, BrowseWindow window)
        {
            _commands = commands;
            _window = window;
            commands.PostRepositoryChanged += OnPostRepositoryChanged;
        }

        public event EventHandler? RepositoryChanged;

        private IGitModule Module => _commands.Module;

        private NativeWindowOwner Owner => new(_window);

        public void Dispose() => _commands.PostRepositoryChanged -= OnPostRepositoryChanged;

        // As FormBrowse.SetTitle.
        public string GetTitle()
        {
            IAppTitleGenerator titleGenerator = _commands.GetRequiredService<IAppTitleGenerator>();
            bool isValid = Module.IsValidGitWorkingDir();
            return titleGenerator.Generate(Module.WorkingDir, isValid, isValid ? Module.GetSelectedBranch() : null, TranslatedStrings.NoBranch);
        }

        public string GetCurrentBranch() => Module.IsValidGitWorkingDir() ? Module.GetSelectedBranch() : TranslatedStrings.NoBranch;

        public Task<IReadOnlyList<FileStatusGroup>> GetDiffsAsync(IReadOnlyList<GitRevision> revisions, CancellationToken cancellationToken)
            => CalculateDiffsAsync(_commands, revisions, Module.GetCurrentCheckout(), allowMultiDiff: true, cancellationToken);

        public void Run(BrowseCommand command, BrowseSelection selection) => AvaloniaUi.RunInHostContext(() =>
        {
            GitRevision? latest = selection.LatestSelectedFirst.Count > 0 ? selection.LatestSelectedFirst[0] : null;
            NativeWindowOwner owner = Owner;
            switch (command)
            {
                // Start (StartToolStripMenuItem): another repository opens in a new window.
                case BrowseCommand.Open:
                    if (FormOpenDirectory.OpenModule(owner, _commands.GetRequiredService<IGitExecutorProvider>(), Module) is { } module)
                    {
                        OpenModule(module);
                    }

                    break;
                case BrowseCommand.Clone:
                    _commands.StartCloneDialog(owner, string.Empty, false, (_, e) => OpenModule(e.GitModule));
                    break;
                case BrowseCommand.Init:
                    _commands.StartInitializeDialog(owner, gitModuleChanged: (_, e) => OpenModule(e.GitModule));
                    break;

                // Repository
                case BrowseCommand.FileExplorer:
                    OsShellUtil.OpenWithFileExplorer(Module.WorkingDir);
                    break;
                case BrowseCommand.Remotes:
                    _commands.StartRemotesDialog(owner);
                    break;
                case BrowseCommand.ManageSubmodules:
                    _commands.StartSubmodulesDialog(owner);
                    break;
                case BrowseCommand.UpdateAllSubmodules:
                    _commands.StartUpdateSubmodulesDialog(owner);
                    break;
                case BrowseCommand.SynchronizeAllSubmodules:
                    _commands.StartSyncSubmodulesDialog(owner);
                    break;
                case BrowseCommand.ManageWorktrees:
                    if (!TryShowManageWorktree(owner, _commands, out bool refresh))
                    {
                        using FormManageWorktree form = new((GitUICommands)_commands);
                        form.ShowDialog(owner);
                        refresh = form.ShouldRefreshRevisionGrid;
                    }

                    if (refresh)
                    {
                        RepositoryChanged?.Invoke(this, EventArgs.Empty);
                    }

                    break;
                case BrowseCommand.EditGitIgnore:
                    _commands.StartEditGitIgnoreDialog(owner, false);
                    break;
                case BrowseCommand.EditGitInfoExclude:
                    _commands.StartEditGitIgnoreDialog(owner, true);
                    break;
                case BrowseCommand.EditGitAttributes:
                    _commands.StartEditGitAttributesDialog(owner);
                    break;
                case BrowseCommand.EditMailMap:
                    _commands.StartMailMapDialog(owner);
                    break;
                case BrowseCommand.CompressGitDatabase:
                    FormProcess.ReadDialog(owner, _commands, arguments: "gc", Module.WorkingDir, input: null, useDialogSettings: true);
                    break;
                case BrowseCommand.RecoverLostObjects:
                    _commands.StartVerifyDatabaseDialog(owner);
                    break;
                case BrowseCommand.DeleteIndexLock:
                    try
                    {
                        Module.UnlockIndex(true);
                    }
                    catch (FileDeleteException ex)
                    {
                        MessageBoxes.ShowError(owner, ex.Message, ViewStrings.Load<BrowseStrings>().DeleteIndexLock.PlainText);
                    }

                    break;
                case BrowseCommand.EditLocalGitConfig:
                    _commands.StartFileEditorDialog(Module.ResolveGitInternalPath("config"), true);
                    break;
                case BrowseCommand.RepoSettings:
                    _commands.StartRepoSettingsDialog(owner);
                    break;
                case BrowseCommand.SparseWorkingCopy:
                    _commands.StartSparseWorkingCopyDialog(owner);
                    break;

                // Commands
                case BrowseCommand.Commit:
                    _commands.StartCommitDialog(owner);
                    break;
                case BrowseCommand.PullDefault:
                    // As ToolStripButtonPullClick: the default action silently, unless it is to open the dialog.
                    if (AppSettings.DefaultPullAction != GitPullAction.None)
                    {
                        _commands.StartPullDialogAndPullImmediately(owner, pullAction: AppSettings.DefaultPullAction);
                    }
                    else
                    {
                        _commands.StartPullDialog(owner, pullAction: AppSettings.FormPullAction);
                    }

                    break;
                case BrowseCommand.Pull:
                case BrowseCommand.OpenPullDialog:
                    _commands.StartPullDialog(owner, pullAction: AppSettings.FormPullAction);
                    break;
                case BrowseCommand.PullMerge:
                    _commands.StartPullDialogAndPullImmediately(owner, pullAction: GitPullAction.Merge);
                    break;
                case BrowseCommand.PullRebase:
                    _commands.StartPullDialogAndPullImmediately(owner, pullAction: GitPullAction.Rebase);
                    break;
                case BrowseCommand.Fetch:
                    _commands.StartPullDialogAndPullImmediately(owner, pullAction: GitPullAction.Fetch);
                    break;
                case BrowseCommand.FetchAll:
                    _commands.StartPullDialogAndPullImmediately(owner, pullAction: GitPullAction.FetchAll);
                    break;
                case BrowseCommand.FetchPruneAll:
                    _commands.StartPullDialogAndPullImmediately(owner, pullAction: GitPullAction.FetchPruneAll);
                    break;
                case BrowseCommand.Push:
                    _commands.StartPushDialog(owner, pushOnShow: false);
                    break;
                case BrowseCommand.ManageStashes:
                    _commands.StartStashDialog(owner);
                    break;
                case BrowseCommand.StashChanges:
                    _commands.StashSave(owner, AppSettings.IncludeUntrackedFilesInManualStash);
                    break;
                case BrowseCommand.StashStaged:
                    _commands.StashStaged(owner);
                    break;
                case BrowseCommand.StashPop:
                    _commands.StashPop(owner);
                    break;
                case BrowseCommand.CreateStash:
                    _commands.StartStashDialog(owner, false);
                    break;
                case BrowseCommand.ResetChanges:
                    _commands.StartResetChangesDialog(owner, Module.GetWorkTreeFiles(), onlyWorkTree: false);
                    break;
                case BrowseCommand.CleanWorkingDirectory:
                    _commands.StartCleanupRepositoryDialog(owner);
                    break;
                case BrowseCommand.CreateBranch:
                    _commands.StartCreateBranchDialog(owner, latest?.ObjectId ?? default);
                    break;
                case BrowseCommand.DeleteBranch:
                    _commands.StartDeleteBranchDialog(owner, string.Empty);
                    break;
                case BrowseCommand.CheckoutBranch:
                    _commands.StartCheckoutBranch(owner);
                    break;
                case BrowseCommand.MergeBranches:
                    _commands.StartMergeBranchDialog(owner, null);
                    break;
                case BrowseCommand.Rebase:
                    Rebase(owner, selection.LatestSelectedFirst);
                    break;
                case BrowseCommand.SolveMergeConflicts:
                    _commands.StartResolveConflictsDialog(owner);
                    break;
                case BrowseCommand.CreateTag:
                    _commands.StartCreateTagDialog(owner, latest);
                    break;
                case BrowseCommand.DeleteTag:
                    _commands.StartDeleteTagDialog(owner, null);
                    break;
                case BrowseCommand.CherryPick:
                    _commands.StartCherryPickDialog(owner, selection.Descending);
                    break;
                case BrowseCommand.Archive:
                    // As ArchiveToolStripMenuItemClick.
                    if (selection.LatestSelectedFirst.Count is < 1 or > 2)
                    {
                        MessageBoxes.SelectOnlyOneOrTwoRevisions(owner);
                        break;
                    }

                    _commands.StartArchiveDialog(owner, selection.LatestSelectedFirst[0], selection.LatestSelectedFirst.Count == 2 ? selection.LatestSelectedFirst[1] : null);
                    break;
                case BrowseCommand.CheckoutRevision:
                    _commands.StartCheckoutRevisionDialog(owner);
                    break;
                case BrowseCommand.Bisect:
                    if (!TryShowBisect(owner, _commands, new SelectionGridInfo(Module, selection)))
                    {
                        MessageBoxes.ShowError(owner, "The bisect dialog needs the WinForms revision grid.", "Bisect");
                    }

                    break;
                case BrowseCommand.FormatPatch:
                    _commands.StartFormatPatchDialog(owner);
                    break;
                case BrowseCommand.ApplyPatch:
                    _commands.StartApplyPatchDialog(owner);
                    break;
                case BrowseCommand.ViewPatch:
                    _commands.StartViewPatchDialog(owner);
                    break;
                case BrowseCommand.Reflog:
                    if (!TryShowReflog(owner, _commands))
                    {
                        using FormReflog form = new(_commands);
                        form.ShowDialog(owner);
                    }

                    break;

                // Tools (ToolsToolStripMenuItem)
                case BrowseCommand.GitBash:
                    StartGitBash(owner);
                    break;
                case BrowseCommand.GitGui:
                    Module.RunGui();
                    break;
                case BrowseCommand.GitK:
                    Module.RunGitK();
                    break;
                case BrowseCommand.GitCommandLog:
                    FormGitCommandLog.ShowOrActivate(owner);
                    break;
                case BrowseCommand.Settings:
                    _commands.StartSettingsDialog(owner);
                    break;

                // Help (HelpToolStripMenuItem)
                case BrowseCommand.UserManual:
                    OsShellUtil.OpenUrlInDefaultBrowser(AppSettings.DocumentationBaseUrl);
                    break;
                case BrowseCommand.Changelog:
                    if (!TryShowChangeLog(owner))
                    {
                        using FormChangeLog form = new();
                        form.ShowDialog(owner);
                    }

                    break;
                case BrowseCommand.Translate:
                    OsShellUtil.OpenUrlInDefaultBrowser(@"https://github.com/gitextensions/gitextensions/wiki/Translations");
                    break;
                case BrowseCommand.Donate:
                    if (!TryShowDonate(owner))
                    {
                        using FormDonate form = new();
                        form.ShowDialog(owner);
                    }

                    break;
                case BrowseCommand.ReportAnIssue:
                    OsShellUtil.OpenUrlInDefaultBrowser(@"https://github.com/gitextensions/gitextensions/issues");
                    break;
                case BrowseCommand.CheckForUpdates:
                    if (!TrySearchForUpdatesAndShow(owner, alwaysShow: true))
                    {
                        new FormUpdates(AppSettings.AppVersion).SearchForUpdatesAndShow(owner, true);
                    }

                    break;
                case BrowseCommand.About:
                    if (!TryShowAbout(owner))
                    {
                        using FormAbout form = new();
                        form.ShowDialog(owner);
                    }

                    break;
            }

            // As the refresh after the dialogs of FormBrowse (e.g. RefreshRevisions, UpdateStashCount).
            RepositoryChanged?.Invoke(this, EventArgs.Empty);
        });

        // As RebaseToolStripMenuItemClick.
        private void Rebase(IWin32Window owner, IReadOnlyList<GitRevision> revisions)
        {
            if (revisions.Count == 0 || revisions[0].IsArtificial)
            {
                return;
            }

            string onto = revisions[0].ObjectId.ToString();
            if (revisions.Count == 2)
            {
                // The commits from the first selected commit (excluded) to HEAD, onto the second one.
                _commands.StartRebaseDialog(owner, revisions[1].ObjectId.ToShortString(), Module.GetSelectedBranch(), onto, interactive: false, startRebaseImmediately: false);
            }
            else
            {
                _commands.StartRebaseDialog(owner, onto);
            }
        }

        // As gitBashToolStripMenuItem_Click.
        private void StartGitBash(IWin32Window owner)
        {
            IShellDescriptor? shell = _commands.GetRequiredService<IShellProvider>().GetShell(BashShell.ShellName);
            if (shell?.ExecutablePath is null)
            {
                return;
            }

            try
            {
                new Executable(shell.ExecutablePath, Module.WorkingDir).Start(createWindow: true, throwOnErrorExit: false);
            }
            catch (Exception exception)
            {
                MessageBoxes.FailedToRunShell(owner, shell.Name, exception);
            }
        }

        // As GitModuleChanged of the start menu: another repository opens in a new main window, this one closes.
        private void OpenModule(IGitModule module)
        {
            ShowBrowseWindow(_commands.WithGitModule(module), new BrowseArguments());
            _window.Close();
        }

        private void OnPostRepositoryChanged(object? sender, GitUIEventArgs e) => RepositoryChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>The revision grid for the dialogs needing the WinForms one (<c>IRevisionGridInfo</c>), over the selection.</summary>
    private sealed class SelectionGridInfo(IGitModule module, BrowseSelection selection) : IRevisionGridInfo
    {
        public ObjectId CurrentCheckout => module.GetCurrentCheckout();

        public GitRevision GetRevision(ObjectId objectId)
            => selection.LatestSelectedFirst.FirstOrDefault(r => r.ObjectId == objectId) ?? module.GetRevision(objectId) ?? new GitRevision(objectId);

        public GitRevision? GetActualRevision(ObjectId objectId) => GetRevision(objectId);

        public GitRevision GetActualRevision(GitRevision revision) => revision;

        public IReadOnlyList<GitRevision> GetSelectedRevisions() => selection.LatestSelectedFirst;

        public string DescribeRevision(GitRevision revision, int maxLength = 0)
        {
            string description = $"{revision.ObjectId.ToShortString()}: {revision.Subject}";
            return maxLength > 0 && description.Length > maxLength ? description[..maxLength] : description;
        }

        public string GetCurrentBranch() => module.GetSelectedBranch();
    }

    /// <summary>The main window for the scripts and the plugins (<c>IBrowseRepo</c> of <c>FormBrowse</c>).</summary>
    private sealed class BrowseRepoAdapter(IGitUICommands commands, RevisionGridViewModel grid, BrowseWindow window) : IBrowseRepo
    {
        public BrowseWindow Window => window;

        public GitRevision? GetLatestSelectedRevision() => grid.GetSelectedRevisionsLatestSelectedFirst().FirstOrDefault();

        public IReadOnlyList<GitRevision> GetSelectedRevisions() => grid.GetSelectedRevisionsLatestSelectedFirst();

        public System.Drawing.Point GetQuickItemSelectorLocation()
        {
            global::Avalonia.PixelPoint position = window.Position;
            return new System.Drawing.Point(position.X + 100, position.Y + 100);
        }

        // As FormBrowse.GoToRef: the revision of the reference is selected.
        public void GoToRef(string refName, bool showNoRevisionMsg, bool toggleSelection = false)
        {
            ObjectId objectId = commands.Module.RevParse(refName);
            if (!objectId.IsZero && !grid.SelectRevision(objectId) && showNoRevisionMsg)
            {
                AvaloniaUi.RunInHostContext(() => MessageBoxes.RevisionFilteredInGrid(new NativeWindowOwner(window), objectId));
            }
        }

        // As FormBrowse.SetWorkingDir: another repository opens in the main window.
        public void SetWorkingDir(string? path, ObjectId selectedId = default, ObjectId firstId = default)
        {
            ShowBrowseWindow(commands.WithWorkingDirectory(path), new BrowseArguments { SelectedId = selectedId, FirstId = firstId });
            window.Close();
        }
    }
}
