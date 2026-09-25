using System.Runtime.CompilerServices;
using GitCommands;
using GitCommands.Config;
using GitCommands.Git;
using GitCommands.Remotes;
using GitCommands.Submodules;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.CommandsDialogs.BrowseDialog;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.Hotkey;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls.LeftPanel;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUI.ScriptsEngine;
using GitUI.UserControls.RevisionGrid;
using GitUIPluginInterfaces;
using Microsoft.VisualStudio.Threading;
using ResourceManager;

namespace GitUI.AvaloniaHosting;

/// <summary>The left panel of the Avalonia main window (the port of <c>RepoObjectsTree</c>; docs/avalonia-port/PLAN.md, phase 7).</summary>
internal static partial class AvaloniaDialogs
{
    // The left panel of each main window, disposed when another repository is shown in the window or when it closes.
    private static readonly ConditionalWeakTable<BrowseWindow, LeftPanelAttachment> _leftPanels = new();

    /// <summary>
    ///  Attaches the left panel (<c>RepoObjectsTree</c>) to the main window, beside <paramref name="grid"/>, for the repository of
    ///  <paramref name="commands"/>: sets <see cref="BrowseViewModel.LeftPanel"/>. Called for each repository shown in the window;
    ///  the panel of the previous one is disposed.
    /// </summary>
    /// <param name="setBranchFilter">
    ///  Filters the grid to the space-separated references ("filter for selected"), or shows all branches again with
    ///  <see langword="null"/> (as <c>FilterToolBar.SetBranchFilter</c>, which <c>RepoObjectsTree.Initialize</c> gets).
    /// </param>
    /// <param name="isBranchFilterActive">Whether the grid shows only some branches; if given, the menu can show all branches again.</param>
    /// <returns>The panel, e.g. for <c>SelectInLeftPanel</c> of the grid's menu (<see cref="LeftPanelViewModel.SelectInLeftPanel"/>).</returns>
    internal static LeftPanelViewModel AttachLeftPanel(
        IGitUICommands commands,
        BrowseWindow window,
        BrowseViewModel viewModel,
        RevisionGridViewModel grid,
        Action<string?> setBranchFilter,
        Func<bool>? isBranchFilterActive = null)
    {
        LeftPanelHost host = new(commands, window, grid, setBranchFilter, isBranchFilterActive, runScript: code => viewModel.RunScript(code));
        LeftPanelViewModel leftPanel = new(ViewStrings.Load<LeftPanelStrings>(), host, new LeftPanelSettings(), grid);
        if (_leftPanels.TryGetValue(window, out LeftPanelAttachment? previous))
        {
            previous.Dispose();
        }
        else
        {
            window.Closed += (_, _) =>
            {
                if (_leftPanels.TryGetValue(window, out LeftPanelAttachment? attachment))
                {
                    attachment.Dispose();
                    _leftPanels.Remove(window);
                }
            };
        }

        _leftPanels.AddOrUpdate(window, new LeftPanelAttachment(leftPanel, host));
        viewModel.LeftPanel = leftPanel;
        return leftPanel;
    }

    /// <summary>
    ///  <see cref="AttachLeftPanel(IGitUICommands, BrowseWindow, BrowseViewModel, RevisionGridViewModel, Action{string?}, Func{bool}?)"/>
    ///  for a grid filtered by <paramref name="filter"/>: its branch filter is set and the grid loaded again.
    /// </summary>
    internal static LeftPanelViewModel AttachLeftPanel(IGitUICommands commands, BrowseWindow window, BrowseViewModel viewModel, RevisionGridViewModel grid, FilterInfo filter)
        => AttachLeftPanel(
            commands,
            window,
            viewModel,
            grid,
            setBranchFilter: refs =>
            {
                filter.SetBranchFilter(refs ?? "");
                grid.Load(grid.SelectedRow?.ObjectId);
            },
            isBranchFilterActive: () => filter.IsShowFilteredBranchesChecked && !string.IsNullOrWhiteSpace(filter.BranchFilter));

    private sealed class LeftPanelAttachment(LeftPanelViewModel leftPanel, LeftPanelHost host) : IDisposable
    {
        public void Dispose()
        {
            leftPanel.Dispose();
            host.Dispose();
        }
    }

    /// <summary>The settings of the left panel in <see cref="AppSettings"/> (as <c>RepoObjectsTree.SettingsContextMenu</c>).</summary>
    private sealed class LeftPanelSettings : ILeftPanelSettings
    {
        public GitRefsSortBy RefsSortBy
        {
            get => AppSettings.RefsSortBy;
            set => AppSettings.RefsSortBy = value;
        }

        public GitRefsSortOrder RefsSortOrder
        {
            get => AppSettings.RefsSortOrder;
            set => AppSettings.RefsSortOrder = value;
        }

        public string PrioritizedBranchNames => AppSettings.PrioritizedBranchNames;

        public string PrioritizedRemoteNames => AppSettings.PrioritizedRemoteNames;

        public bool IsShown(LeftPanelTreeKind kind) => kind switch
        {
            LeftPanelTreeKind.Branches => AppSettings.RepoObjectsTreeShowBranches,
            LeftPanelTreeKind.Remotes => AppSettings.RepoObjectsTreeShowRemotes,
            LeftPanelTreeKind.Worktrees => AppSettings.RepoObjectsTreeShowWorktrees,
            LeftPanelTreeKind.Tags => AppSettings.RepoObjectsTreeShowTags,
            LeftPanelTreeKind.Submodules => AppSettings.RepoObjectsTreeShowSubmodules,
            LeftPanelTreeKind.Stashes => AppSettings.RepoObjectsTreeShowStashes,
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

        public void SetShown(LeftPanelTreeKind kind, bool shown)
        {
            switch (kind)
            {
                case LeftPanelTreeKind.Branches: AppSettings.RepoObjectsTreeShowBranches = shown; break;
                case LeftPanelTreeKind.Remotes: AppSettings.RepoObjectsTreeShowRemotes = shown; break;
                case LeftPanelTreeKind.Worktrees: AppSettings.RepoObjectsTreeShowWorktrees = shown; break;
                case LeftPanelTreeKind.Tags: AppSettings.RepoObjectsTreeShowTags = shown; break;
                case LeftPanelTreeKind.Submodules: AppSettings.RepoObjectsTreeShowSubmodules = shown; break;
                case LeftPanelTreeKind.Stashes: AppSettings.RepoObjectsTreeShowStashes = shown; break;
            }
        }

        public int GetIndex(LeftPanelTreeKind kind) => kind switch
        {
            LeftPanelTreeKind.Branches => AppSettings.RepoObjectsTreeBranchesIndex,
            LeftPanelTreeKind.Remotes => AppSettings.RepoObjectsTreeRemotesIndex,
            LeftPanelTreeKind.Worktrees => AppSettings.RepoObjectsTreeWorktreesIndex,
            LeftPanelTreeKind.Tags => AppSettings.RepoObjectsTreeTagsIndex,
            LeftPanelTreeKind.Submodules => AppSettings.RepoObjectsTreeSubmodulesIndex,
            LeftPanelTreeKind.Stashes => AppSettings.RepoObjectsTreeStashesIndex,
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

        public void SetIndex(LeftPanelTreeKind kind, int index)
        {
            switch (kind)
            {
                case LeftPanelTreeKind.Branches: AppSettings.RepoObjectsTreeBranchesIndex = index; break;
                case LeftPanelTreeKind.Remotes: AppSettings.RepoObjectsTreeRemotesIndex = index; break;
                case LeftPanelTreeKind.Worktrees: AppSettings.RepoObjectsTreeWorktreesIndex = index; break;
                case LeftPanelTreeKind.Tags: AppSettings.RepoObjectsTreeTagsIndex = index; break;
                case LeftPanelTreeKind.Submodules: AppSettings.RepoObjectsTreeSubmodulesIndex = index; break;
                case LeftPanelTreeKind.Stashes: AppSettings.RepoObjectsTreeStashesIndex = index; break;
            }
        }
    }

    /// <summary>
    ///  Git and the dialogs of the left panel: the data of the trees, and the operations of the nodes as the WinForms nodes and
    ///  <c>RepoObjectsTree.ContextActions</c> run them.
    /// </summary>
    private sealed class LeftPanelHost : ILeftPanelHost, IDisposable
    {
        private readonly IGitUICommands _commands;
        private readonly BrowseWindow _window;
        private readonly RevisionGridViewModel _grid;
        private readonly Action<string?> _setBranchFilter;
        private readonly Func<bool>? _isBranchFilterActive;
        private readonly Func<int, bool> _runScript;
        private readonly ISubmoduleStatusProvider _submoduleStatusProvider;
        private readonly AheadBehindDataProvider _aheadBehindDataProvider;
        private IReadOnlyList<HotkeyBinding>? _hotkeys;
        private bool _disposed;

        public LeftPanelHost(IGitUICommands commands, BrowseWindow window, RevisionGridViewModel grid, Action<string?> setBranchFilter, Func<bool>? isBranchFilterActive, Func<int, bool> runScript)
        {
            _runScript = runScript;
            _commands = commands;
            _window = window;
            _grid = grid;
            _setBranchFilter = setBranchFilter;
            _isBranchFilterActive = isBranchFilterActive;
            _aheadBehindDataProvider = new AheadBehindDataProvider(() => commands.Module.GitExecutable);
            _submoduleStatusProvider = commands.GetRequiredService<ISubmoduleStatusProvider>();
            _submoduleStatusProvider.StatusUpdated += OnSubmoduleStatusUpdated;
        }

        public event EventHandler<LeftPanelSubmodules>? SubmodulesUpdated;

        private IGitModule Module => _commands.Module;

        private NativeWindowOwner Owner => new(_window);

        // As AddUserScripts: the enabled scripts, those of the grid's menu in the menu itself.
        public IReadOnlyList<LeftPanelScript> GetScripts()
            => [.. _commands.GetRequiredService<IScriptsManager>().GetScripts()
                .Where(script => script.Enabled)
                .Select(script => new LeftPanelScript(script.HotkeyCommandIdentifier, script.Name ?? "", script.AddToRevisionGridContextMenu, script.GetIcon()))];

        // As the scriptInvoker of AddUserScripts (ExecuteCommand with the options of the main window).
        public void RunScript(int scriptId) => _runScript(scriptId);

        public bool IsValidWorkingDir => Module.IsValidGitWorkingDir();

        public bool IsBareRepository => Module.IsBareRepository();

        public string WorkingDir => Module.WorkingDir;

        public IReadOnlyList<HotkeyBinding> Hotkeys => _hotkeys ??= LoadHotkeys(_commands, GitUI.Hotkey.HotkeyCommands.LeftPanelSettingsName);

        public bool IsBranchFilterActive => _isBranchFilterActive?.Invoke() is true;

        public void Dispose()
        {
            _disposed = true;
            _submoduleStatusProvider.StatusUpdated -= OnSubmoduleStatusUpdated;
        }

        public IReadOnlyList<IGitRef> GetRefs()
        {
            // Once per refresh of the panel: the ahead/behind counts are read again too.
            _aheadBehindDataProvider.ResetCache();
            return Module.GetRefs(RefsFilter.NoFilter);
        }

        public string GetCurrentBranch() => Module.GetSelectedBranch();

        public IReadOnlyDictionary<string, AheadBehindData>? GetAheadBehindData() => _aheadBehindDataProvider.GetData();

        public IReadOnlyList<Remote> GetRemotes() => ThreadHelper.JoinableTaskFactory.Run(Module.GetRemotesAsync);

        public IReadOnlyList<Remote> GetDisabledRemotes() => new ConfigFileRemoteSettingsManager(() => Module).GetDisabledRemotes();

        // As toggleLeftPanel_Click: the stashes, including their reflog selectors.
        public IReadOnlyCollection<GitRevision> GetStashes()
            => !AppSettings.ShowStashes
                ? []
                : new RevisionReader(new GitModule(_commands.GetRequiredService<IGitExecutorProvider>(), Module.WorkingDir)).GetStashes(CancellationToken.None);

        public IReadOnlyList<GitWorktree> GetWorktrees() => Module.GetWorktrees();

        public bool DirectoryExists(string path) => Directory.Exists(path);

        public async Task<IReadOnlyCollection<string>> GetMergedBranchesAsync(string commit, CancellationToken cancellationToken)
        {
            await TaskScheduler.Default;
            return await Module.GetMergedBranchesAsync(includeRemote: true, fullRefname: true, commit, cancellationToken);
        }

        // As SubmoduleNode.SetStatusToolTipAsync.
        public string GetSubmoduleToolTip(SubmoduleInfo info, IReadOnlyList<GitItemStatus>? gitStatus)
        {
            IGitExecutorProvider executorProvider = _commands.GetRequiredService<IGitExecutorProvider>();
            if (info.Detailed?.RawStatus is { } rawStatus)
            {
                return SubmoduleResources.GetSubmoduleStatusText(new GitModule(executorProvider, info.Path), rawStatus, moduleIsParent: false, limitOutput: true);
            }

            if (gitStatus is not null)
            {
                ArtificialCommitChangeCount changeCount = new();
                changeCount.Update(gitStatus);
                return changeCount.GetSummary();
            }

            return SubmoduleResources.GetSubmoduleText(new GitModule(executorProvider, "."), info.Path, hash: "");
        }

        // As FormBrowse.UpdateSubmodulesStructure.
        public void UpdateSubmodules()
        {
            string workingDir = Module.WorkingDir;
            bool updateStatus = AppSettings.ShowSubmoduleStatus;
            ThreadHelper.FileAndForget(async () =>
            {
                try
                {
                    await _submoduleStatusProvider.UpdateSubmodulesStructureAsync(workingDir, TranslatedStrings.NoBranch, updateStatus);
                }
                catch (GitConfigurationException ex)
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    AvaloniaUi.RunInHostContext(() => MessageBoxes.ShowGitConfigurationExceptionMessage(Owner, ex));
                }
            });
        }

        public async Task<T> RunInBackgroundAsync<T>(Func<T> work, CancellationToken cancellationToken)
        {
            await TaskScheduler.Default;
            cancellationToken.ThrowIfCancellationRequested();
            return work();
        }

        public void FilterRevisionGrid(string? refs) => _setBranchFilter(refs);

        public void CopyToClipboard(string text) => ClipboardUtil.TrySetText(text);

        public void ShowRevisionNotInGrid(ObjectId objectId) => AvaloniaUi.RunInHostContext(() => MessageBoxes.RevisionFilteredInGrid(Owner, objectId));

        public void ShowSubmoduleDirectoryMissing(string directory, string submoduleName)
            => ThreadHelper.FileAndForget(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                AvaloniaUi.RunInHostContext(() => MessageBoxes.SubmoduleDirectoryDoesNotExist(Owner, directory, submoduleName));
            });

        public bool Run(LeftPanelAction action, LeftPanelNode? node) => AvaloniaUi.RunInHostContext(() => RunInHost(action, node));

        private bool RunInHost(LeftPanelAction action, LeftPanelNode? node)
        {
            NativeWindowOwner owner = Owner;
            string fullPath = (node as LeftPanelRevisionNode)?.FullPath ?? "";
            switch (action)
            {
                // Local branches (LocalBranchNode, BranchPathNode)
                case LeftPanelAction.CheckoutBranch:
                    return MessageBoxes.ConfirmBranchCheckout(owner, fullPath) && _commands.StartCheckoutBranch(owner, branch: fullPath, remote: false);
                case LeftPanelAction.CreateBranchFromBranch:
                case LeftPanelAction.CreateBranchFromRemoteBranch:
                    return _commands.StartCreateBranchDialog(owner, branch: fullPath);
                case LeftPanelAction.MergeBranch:
                case LeftPanelAction.MergeRemoteBranch:
                case LeftPanelAction.MergeTag:
                    return _commands.StartMergeBranchDialog(owner, fullPath);
                case LeftPanelAction.RebaseOnBranch:
                case LeftPanelAction.RebaseOnRemoteBranch:
                case LeftPanelAction.RebaseOnTag:
                    return _commands.StartRebaseDialog(owner, onto: fullPath);
                case LeftPanelAction.ResetToBranch:
                case LeftPanelAction.ResetToRemoteBranch:
                case LeftPanelAction.ResetToTag:
                    return _commands.StartResetCurrentBranchDialog(owner, branch: fullPath);
                case LeftPanelAction.RenameBranch:
                    return _commands.StartRenameDialog(owner, branch: fullPath);
                case LeftPanelAction.DeleteBranch:
                    return _commands.StartDeleteBranchDialog(owner, branch: fullPath);
                case LeftPanelAction.CreateBranchInFolder:
                    return _commands.StartCreateBranchDialog(owner, objectId: default, newBranchNamePrefix: fullPath + LeftPanelRevisionNode.PathSeparator);
                case LeftPanelAction.DeleteAllBranchesInFolder when node is BranchPathNode folder:
                    return _commands.StartDeleteBranchDialog(owner, folder.Branches);

                // Remote branches (RemoteBranchNode)
                case LeftPanelAction.CheckoutRemoteBranch:
                    return CheckoutRemoteBranch(owner, fullPath);
                case LeftPanelAction.DeleteRemoteBranch when node is RemoteBranchNode remoteBranch:
                    return _commands.StartDeleteRemoteBranchDialog(owner, $"{remoteBranch.RemoteName}/{remoteBranch.BranchName}");
                case LeftPanelAction.FetchRemoteBranch when node is RemoteBranchNode remoteBranch:
                    return FetchRemoteBranch(owner, remoteBranch);
                case LeftPanelAction.FetchAndMergeRemoteBranch when node is RemoteBranchNode remoteBranch:
                    return FetchRemoteBranch(owner, remoteBranch) && _commands.StartMergeBranchDialog(owner, fullPath);
                case LeftPanelAction.FetchAndCheckoutRemoteBranch when node is RemoteBranchNode remoteBranch:
                    return FetchRemoteBranch(owner, remoteBranch) && CheckoutRemoteBranch(owner, fullPath);
                case LeftPanelAction.FetchAndCreateBranchFromRemoteBranch when node is RemoteBranchNode remoteBranch:
                    return FetchRemoteBranch(owner, remoteBranch) && _commands.StartCreateBranchDialog(owner, branch: fullPath);
                case LeftPanelAction.FetchAndRebaseOnRemoteBranch when node is RemoteBranchNode remoteBranch:
                    return FetchRemoteBranch(owner, remoteBranch) && _commands.StartRebaseDialog(owner, onto: fullPath);

                // Tags (TagNode)
                case LeftPanelAction.CheckoutTag:
                    return _commands.StartCheckoutRevisionDialog(owner, fullPath);
                case LeftPanelAction.CreateBranchFromTag when node is TagNode tag:
                    return _commands.StartCreateBranchDialog(owner, tag.ObjectId);
                case LeftPanelAction.DeleteTag:
                    return _commands.StartDeleteTagDialog(owner, fullPath);

                // Remotes (RemoteBranchTree, RemoteRepoNode)
                case LeftPanelAction.ManageRemotes:
                    return _commands.StartRemotesDialog(owner, (node as RemoteRepoNode)?.FullPath);
                case LeftPanelAction.FetchAllRemotes:
                    return Pull(owner, GitPullAction.FetchAll);
                case LeftPanelAction.FetchAndPruneAllRemotes:
                    return Pull(owner, GitPullAction.FetchPruneAll);
                case LeftPanelAction.FetchRemote:
                    return Pull(owner, GitPullAction.Fetch, remote: fullPath);
                case LeftPanelAction.FetchAndPruneRemote:
                    return Pull(owner, GitPullAction.FetchPruneAll, remote: fullPath);
                case LeftPanelAction.OpenRemoteUrl when node is RemoteRepoNode { IsRemoteUrlUsingHttp: true } remote:
                    OsShellUtil.OpenUrlInDefaultBrowser(remote.Remote.FetchUrl);
                    return true;
                case LeftPanelAction.EnableRemote when node is RemoteRepoNode remote:
                    new ConfigFileRemoteSettingsManager(() => Module).ToggleRemoteState(remote.Name, disabled: false);
                    _commands.RepoChangedNotifier.Notify();
                    return true;
                case LeftPanelAction.EnableRemoteAndFetch when node is RemoteRepoNode remote:
                    // The fetch notifies the change.
                    new ConfigFileRemoteSettingsManager(() => Module).ToggleRemoteState(remote.Name, disabled: false);
                    return Pull(owner, GitPullAction.Fetch, remote: remote.FullPath);
                case LeftPanelAction.DisableRemote when node is RemoteRepoNode remote:
                    new ConfigFileRemoteSettingsManager(() => Module).ToggleRemoteState(remote.Name, disabled: true);
                    _commands.RepoChangedNotifier.Notify();
                    return true;

                // Stashes (StashTree, StashNode)
                case LeftPanelAction.StashAll:
                    return _commands.StashSave(owner, AppSettings.IncludeUntrackedFilesInManualStash);
                case LeftPanelAction.StashStaged:
                    return _commands.StashStaged(owner);
                case LeftPanelAction.ManageStashes:
                    return _commands.StartStashDialog(owner, manageStashes: true);
                case LeftPanelAction.OpenStash when node is StashNode stash:
                    return _commands.StartStashDialog(owner, manageStashes: true, stash.ReflogSelector);
                case LeftPanelAction.ApplyStash when node is StashNode stash:
                    return _commands.StashApply(owner, stash.ReflogSelector);
                case LeftPanelAction.PopStash when node is StashNode stash:
                    return _commands.StashPop(owner, stash.ReflogSelector);
                case LeftPanelAction.DropStash when node is StashNode stash:
                    return DropStash(owner, stash.ReflogSelector);

                // Submodules (SubmoduleNode, SubmoduleTree)
                case LeftPanelAction.OpenSubmodule when node is SubmoduleNode submodule:
                    return OpenSubmodule(submodule);
                case LeftPanelAction.OpenSubmoduleInNewInstance when node is SubmoduleNode submodule:
                    return LaunchGitExtensions(submodule);
                case LeftPanelAction.ManageSubmodules:
                    _commands.StartSubmodulesDialog(owner);
                    UpdateSubmodules();
                    return true;
                case LeftPanelAction.SynchronizeSubmodules:
                    _commands.StartSyncSubmodulesDialog(owner);
                    UpdateSubmodules();
                    return true;
                case LeftPanelAction.UpdateSubmodule when node is SubmoduleNode submodule:
                    return _commands.StartUpdateSubmoduleDialog(owner, submodule.LocalPath, submodule.SuperPath);
                case LeftPanelAction.ResetSubmodule when node is SubmoduleNode submodule:
                    return ResetSubmodule(owner, submodule);
                case LeftPanelAction.StashSubmodule when node is SubmoduleNode submodule:
                    return _commands.WithWorkingDirectory(submodule.Info.Path).StashSave(owner, AppSettings.IncludeUntrackedFilesInManualStash);
                case LeftPanelAction.CommitSubmodule when node is SubmoduleNode submodule:
                    return _commands.WithWorkingDirectory(submodule.Info.Path.EnsureTrailingPathSeparator()).StartCommitDialog(owner);

                // Worktrees (WorktreeTree, WorktreeNode)
                case LeftPanelAction.CreateWorktree:
                    return _commands.WorktreeCreate(owner, (node as WorktreeTree)?.MainWorktreePath ?? Module.WorkingDir);
                case LeftPanelAction.PruneWorktrees:
                    if (_commands.StartCommandLineProcessDialog(owner, command: null, "worktree prune"))
                    {
                        _commands.RepoChangedNotifier.Notify();
                        return true;
                    }

                    return false;
                case LeftPanelAction.ManageWorktrees:
                    return ManageWorktrees(owner);
                case LeftPanelAction.OpenWorktree when node is WorktreeNode { Worktree.IsDeleted: false } worktree:
                    return SwitchWorktree(owner, worktree.Worktree.Path);
                case LeftPanelAction.DeleteWorktree when node is WorktreeNode worktree:
                    return _commands.WorktreeDelete(owner, worktree.Worktree.Path);
                case LeftPanelAction.CopyWorktreePath when node is WorktreeNode worktree:
                    ClipboardUtil.TrySetText(worktree.Worktree.Path);
                    return true;
                case LeftPanelAction.ShowWorktreeInFolder when node is WorktreeNode worktree:
                    OsShellUtil.OpenWithFileExplorer(worktree.Worktree.Path);
                    return true;
                default:
                    return false;
            }
        }

        private bool CheckoutRemoteBranch(IWin32Window owner, string branch)
            => MessageBoxes.ConfirmBranchCheckout(owner, branch) && _commands.StartCheckoutRemoteBranch(owner, branch);

        private bool FetchRemoteBranch(IWin32Window owner, RemoteBranchNode branch)
            => Pull(owner, GitPullAction.Fetch, remote: branch.RemoteName, remoteBranch: branch.BranchName);

        private bool Pull(IWin32Window owner, GitPullAction pullAction, string? remote = null, string? remoteBranch = null)
        {
            _commands.StartPullDialogAndPullImmediately(out bool pullCompleted, owner, remoteBranch: remoteBranch, remote: remote, pullAction: pullAction);
            return pullCompleted;
        }

        // As StashNode.DropStash.
        private bool DropStash(IWin32Window owner, string stash)
        {
            if (!AppSettings.DontConfirmStashDrop)
            {
                TaskDialogPage page = new()
                {
                    Text = TranslatedStrings.AreYouSure,
                    Caption = TranslatedStrings.StashDropConfirmTitle,
                    Heading = TranslatedStrings.CannotBeUndone,
                    Buttons = { TaskDialogButton.Yes, TaskDialogButton.No },
                    Icon = TaskDialogIcon.Information,
                    Verification = new TaskDialogVerificationCheckBox { Text = TranslatedStrings.DontShowAgain },
                    SizeToContent = true,
                };
                TaskDialogButton result = TaskDialog.ShowDialog(owner, page);
                if (page.Verification.Checked)
                {
                    AppSettings.DontConfirmStashDrop = true;
                }

                if (result != TaskDialogButton.Yes)
                {
                    return false;
                }
            }

            return _commands.StashDrop(owner, stash);
        }

        // As SubmoduleNode.Open: the submodule opens in the main window (here a new one), with its diff to the expected commit.
        private bool OpenSubmodule(SubmoduleNode submodule)
        {
            SubmoduleInfo info = submodule.Info;
            if (!Directory.Exists(info.Path))
            {
                MessageBoxes.SubmoduleDirectoryDoesNotExist(Owner, info.Path, info.Text);
                return false;
            }

            if (info.Detailed?.RawStatus is { } rawStatus)
            {
                OpenRepository(info.Path, ObjectId.WorkTreeId, rawStatus.OldCommit);
            }
            else
            {
                OpenRepository(info.Path);
            }

            return true;
        }

        // As SubmoduleNode.LaunchGitExtensions: a new instance, for the current module with the selection of the grid.
        private bool LaunchGitExtensions(SubmoduleNode submodule)
        {
            SubmoduleInfo info = submodule.Info;
            if (!Directory.Exists(info.Path))
            {
                MessageBoxes.SubmoduleDirectoryDoesNotExist(Owner, info.Path, info.Text);
                return false;
            }

            ObjectId selected;
            ObjectId first;
            if (submodule.IsCurrent)
            {
                IReadOnlyList<GitRevision> revisions = _grid.GetSelectedRevisionsLatestSelectedFirst();
                selected = revisions.Count > 0 ? revisions[0].ObjectId : default;
                first = revisions.Count > 1 ? revisions[^1].ObjectId : default;
            }
            else
            {
                selected = ObjectId.WorkTreeId;
                first = info.Detailed?.RawStatus?.OldCommit ?? default;
            }

            GitUICommands.LaunchBrowse(workingDir: info.Path.EnsureTrailingPathSeparator(), selected, first);
            return true;
        }

        // As SubmoduleTree.ResetSubmodule.
        private bool ResetSubmodule(IWin32Window owner, SubmoduleNode submodule)
        {
            ResetChangesAction resetType = AvaloniaHosting.AvaloniaDialogs.ShowResetChanges(owner, true, true);
            if (resetType == ResetChangesAction.Cancel)
            {
                return false;
            }

            GitModule module = new(_commands.GetRequiredService<IGitExecutorProvider>(), submodule.Info.Path);
            return module.ResetAllChanges(clean: resetType == ResetChangesAction.ResetAndDelete);
        }

        // As WorktreeTree.ManageWorktrees.
        private bool ManageWorktrees(IWin32Window owner)
        {
            TryShowManageWorktree(owner, _commands, out bool shouldRefreshRevisionGrid);

            if (shouldRefreshRevisionGrid)
            {
                _commands.RepoChangedNotifier.Notify();
            }

            return shouldRefreshRevisionGrid;
        }

        // As GitUICommands.WorktreeSwitch, whose FormBrowse is here the Avalonia main window.
        private bool SwitchWorktree(IWin32Window owner, string worktreePath)
        {
            if (!MessageBoxes.ConfirmSuppressible(owner, string.Format(TranslatedStrings.SwitchWorktreeConfirmation, worktreePath), TranslatedStrings.SwitchWorktreeCaption, AppSettings.DontConfirmSwitchWorktree))
            {
                return false;
            }

            if (!Directory.Exists(worktreePath))
            {
                return false;
            }

            OpenRepository(Path.GetFullPath(worktreePath));
            return true;
        }

        // As BrowseRepo.SetWorkingDir: as for another repository of the start menu, it opens in a new main window, this one closes.
        // As FormBrowse.SetWorkingDir: the repository is shown in this window (BrowseSession, through IBrowseRepo).
        private void OpenRepository(string workingDir, ObjectId selectedId = default, ObjectId firstId = default)
        {
            if (_commands.BrowseRepo is { } browseRepo)
            {
                browseRepo.SetWorkingDir(workingDir, selectedId, firstId);
                return;
            }

            ShowBrowseWindow(_commands.WithWorkingDirectory(workingDir), new BrowseArguments { SelectedId = selectedId, FirstId = firstId });
            _window.Close();
        }

        // Raised in the background: the submodules are passed on the UI thread.
        private void OnSubmoduleStatusUpdated(object? sender, SubmoduleStatusEventArgs e)
        {
            SubmoduleInfoResult info = e.Info;
            if (_disposed || info.TopProject is not { } topProject || info.Module is not { } module)
            {
                return;
            }

            List<string> modulePaths = [];
            for (IGitModule? parent = module; parent is not null; parent = parent.SuperprojectModule)
            {
                modulePaths.Add(parent.WorkingDir);
            }

            LeftPanelSubmodules submodules = new(topProject, [.. info.AllSubmodules], modulePaths, module.GetTopModule().WorkingDir, info.CurrentSubmoduleStatus, e.StructureUpdated);
            ThreadHelper.FileAndForget(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(e.Token);
                if (!_disposed)
                {
                    SubmodulesUpdated?.Invoke(this, submodules);
                }
            });
        }
    }
}
