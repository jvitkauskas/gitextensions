namespace GitUI.Presentation.CommandsDialogs;

/// <summary>The commands of the "Browse" hotkeys; the values are the ones of <c>FormBrowse.Command</c>, which the settings store.</summary>
public enum BrowseHotkeyCommand
{
    GitBash = 0,
    GitGui = 1,
    GitGitK = 2,
    FocusRevisionGrid = 3,
    FocusCommitInfo = 4,
    FocusDiff = 5,
    FocusFileTree = 6,
    Commit = 7,
    CheckoutBranch = 10,
    QuickFetch = 11,
    QuickPull = 12,
    QuickPush = 13,
    CloseRepository = 15,
    Stash = 16,
    StashPop = 17,
    FocusFilter = 18,
    OpenSettings = 20,
    ToggleLeftPanel = 21,
    FocusLeftPanel = 25,
    FocusGpgInfo = 26,
    GoToSuperproject = 27,
    GoToSubmodule = 28,
    FocusGitConsole = 29,
    FocusBuildServerStatus = 30,
    FocusNextTab = 31,
    FocusOutputHistory = 47,
    FocusPrevTab = 32,
    GoToChild = 37,
    GoToParent = 38,
    PullOrFetch = 39,
    Push = 40,
    CreateBranch = 41,
    MergeBranches = 42,
    CreateTag = 43,
    Rebase = 44,
    OpenRepo = 45,
    StashStaged = 46,
    QuickPullOrFetch = 48,
    ManageWorkTrees = 49,
}

/// <summary>What gets the focus for a hotkey (<c>FocusLeftPanel</c>, <c>RevisionGrid.Focus</c>, <c>ToolStripFilters.SetFocus</c>).</summary>
public enum BrowseFocusTarget
{
    LeftPanel,
    RevisionGrid,
    Filter,
    CommitInfo,
    OutputHistory,
}

/// <summary>What the script hotkeys need from the application.</summary>
public interface IBrowseScriptsHost
{
    /// <summary>
    ///  As <c>GitModuleForm.ExecuteCommand</c> for a script: runs the script of the hotkey command, if it is one.
    /// </summary>
    /// <returns><see langword="true"/> if a script has the command.</returns>
    bool RunScriptOfHotkey(int commandCode);
}

/// <summary>The hotkeys of the main window (<c>FormBrowse.ExecuteCommand</c>).</summary>
public sealed partial class BrowseViewModel
{
    /// <summary>Raised when a hotkey moves the focus to a part of the window the view model has no view model for.</summary>
    public event EventHandler<BrowseFocusTarget>? FocusRequested;

    public override bool ExecuteHotkeyCommand(int commandCode)
    {
        if (IsDashboard)
        {
            // As the dashboard: only the commands that don't need a repository.
            return commandCode switch
            {
                (int)BrowseHotkeyCommand.OpenRepo => RunAndHandle(BrowseCommand.Open),
                (int)BrowseHotkeyCommand.OpenSettings => RunAndHandle(BrowseCommand.Settings),
                _ => false,
            };
        }

        switch ((BrowseHotkeyCommand)commandCode)
        {
            case BrowseHotkeyCommand.GitBash:
                if (HasShells)
                {
                    RunDefaultShell();
                    return true;
                }

                return RunAndHandle(BrowseCommand.GitBash);
            case BrowseHotkeyCommand.GitGui: return RunAndHandle(BrowseCommand.GitGui);
            case BrowseHotkeyCommand.GitGitK: return RunAndHandle(BrowseCommand.GitK);
            case BrowseHotkeyCommand.FocusLeftPanel: return Focus(BrowseFocusTarget.LeftPanel);
            case BrowseHotkeyCommand.FocusRevisionGrid: return Focus(BrowseFocusTarget.RevisionGrid);
            case BrowseHotkeyCommand.FocusFilter: return Focus(BrowseFocusTarget.Filter);
            case BrowseHotkeyCommand.FocusCommitInfo: return IsCommitInfoInTab ? SelectTab(BrowseTab.Commit) : Focus(BrowseFocusTarget.CommitInfo);
            case BrowseHotkeyCommand.FocusDiff: return SelectTab(BrowseTab.Diff);
            case BrowseHotkeyCommand.FocusFileTree: return FileTree is not null && SelectTab(BrowseTab.FileTree);
            case BrowseHotkeyCommand.FocusGpgInfo: return HasGpgInfo && SelectTab(BrowseTab.Gpg);
            case BrowseHotkeyCommand.FocusGitConsole: return HasConsole && SelectTab(BrowseTab.Console);
            case BrowseHotkeyCommand.FocusBuildServerStatus: return HasBuildReport && SelectTab(BrowseTab.BuildReport);
            case BrowseHotkeyCommand.FocusOutputHistory: return ShowOrToggleOutputHistory();
            case BrowseHotkeyCommand.FocusNextTab: return SelectTab(NextTab(forward: true));
            case BrowseHotkeyCommand.FocusPrevTab: return SelectTab(NextTab(forward: false));
            case BrowseHotkeyCommand.ToggleLeftPanel:
                if (LeftPanel is { } leftPanel)
                {
                    leftPanel.IsVisible = !leftPanel.IsVisible;
                    return true;
                }

                return false;
            case BrowseHotkeyCommand.GoToSuperproject:
                GoUpOrShowSubmodules();
                return true;
            case BrowseHotkeyCommand.GoToSubmodule:
                SubmodulesMenuRequested?.Invoke(this, EventArgs.Empty);
                return true;
            case BrowseHotkeyCommand.OpenRepo: return RunAndHandle(BrowseCommand.Open);
            case BrowseHotkeyCommand.Commit: return RunAndHandle(BrowseCommand.Commit);
            case BrowseHotkeyCommand.CheckoutBranch: return RunAndHandle(BrowseCommand.CheckoutBranch);
            case BrowseHotkeyCommand.QuickFetch: return RunAndHandle(BrowseCommand.Fetch);
            case BrowseHotkeyCommand.QuickPull: return RunAndHandle(BrowseCommand.PullMerge);
            case BrowseHotkeyCommand.QuickPullOrFetch: return RunAndHandle(BrowseCommand.PullDefault);
            case BrowseHotkeyCommand.QuickPush: return RunAndHandle(BrowseCommand.QuickPush);
            case BrowseHotkeyCommand.CloseRepository: return RunAndHandle(BrowseCommand.CloseRepository);
            case BrowseHotkeyCommand.Stash: return RunAndHandle(BrowseCommand.StashChanges);
            case BrowseHotkeyCommand.StashStaged: return RunAndHandle(BrowseCommand.StashStaged);
            case BrowseHotkeyCommand.StashPop: return RunAndHandle(BrowseCommand.StashPop);
            case BrowseHotkeyCommand.OpenSettings: return RunAndHandle(BrowseCommand.Settings);
            case BrowseHotkeyCommand.GoToChild:
                Grid.GoToChild();
                return true;
            case BrowseHotkeyCommand.GoToParent:
                Grid.GoToParent();
                return true;
            case BrowseHotkeyCommand.PullOrFetch: return RunAndHandle(BrowseCommand.Pull);
            case BrowseHotkeyCommand.Push: return RunAndHandle(BrowseCommand.Push);
            case BrowseHotkeyCommand.CreateBranch: return RunAndHandle(BrowseCommand.CreateBranch);
            case BrowseHotkeyCommand.MergeBranches: return RunAndHandle(BrowseCommand.MergeBranches);
            case BrowseHotkeyCommand.CreateTag: return RunAndHandle(BrowseCommand.CreateTag);
            case BrowseHotkeyCommand.Rebase: return RunAndHandle(BrowseCommand.Rebase);
            case BrowseHotkeyCommand.ManageWorkTrees: return RunAndHandle(BrowseCommand.ManageWorktrees);
        }

        // As the base class: the scripts with a hotkey.
        return _host is IBrowseScriptsHost scriptsHost && scriptsHost.RunScriptOfHotkey(commandCode);

        bool RunAndHandle(BrowseCommand command)
        {
            RunCommand.Execute(command);
            return true;
        }

        bool Focus(BrowseFocusTarget target)
        {
            FocusRequested?.Invoke(this, target);
            return true;
        }

        bool SelectTab(BrowseTab tab)
        {
            SelectedTab = tab;
            return true;
        }
    }

    // As FocusNextTab: the next shown tab, around the end.
    private BrowseTab NextTab(bool forward)
    {
        List<BrowseTab> shown = [.. Enum.GetValues<BrowseTab>().Where(IsTabShown)];
        int index = shown.IndexOf(SelectedTab);
        return shown[((index < 0 ? 0 : index) + (forward ? 1 : shown.Count - 1)) % shown.Count];
    }

    private bool IsTabShown(BrowseTab tab) => tab switch
    {
        BrowseTab.Commit => IsCommitInfoInTab,
        BrowseTab.FileTree => FileTree is not null,
        BrowseTab.Gpg => HasGpgInfo,
        BrowseTab.Console => HasConsole,
        BrowseTab.OutputHistory => HasOutputHistory && IsOutputHistoryTab,
        BrowseTab.BuildReport => HasBuildReport,
        _ => true,
    };
}
