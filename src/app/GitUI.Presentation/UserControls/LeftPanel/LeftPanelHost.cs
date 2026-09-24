using GitCommands.Git;
using GitCommands.Submodules;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Services;
using GitUIPluginInterfaces;

namespace GitUI.Presentation.UserControls.LeftPanel;

/// <summary>The trees of the left panel, in their default order (the <c>RepoObjectsTree*Index</c> settings).</summary>
public enum LeftPanelTreeKind
{
    Branches,
    Remotes,
    Worktrees,
    Tags,
    Submodules,
    Stashes,
}

/// <summary>The hotkeys of the left panel; the codes match <c>HotkeyCommands.LeftPanel</c> (settings name "LeftPanel").</summary>
public enum LeftPanelHotkeyCommand
{
    Delete = 0,
    Rename = 1,
    Search = 2,
    MultiSelect = 3,
    MultiSelectWithChildren = 4,
}

/// <summary>The operations of the menus and the double clicks of the nodes, which the host runs (the dialogs of <c>UICommands</c>).</summary>
public enum LeftPanelAction
{
    // A local branch (LocalBranchNode)
    CheckoutBranch,
    MergeBranch,
    RebaseOnBranch,
    CreateBranchFromBranch,
    ResetToBranch,
    RenameBranch,
    DeleteBranch,

    // A remote branch (RemoteBranchNode)
    CheckoutRemoteBranch,
    MergeRemoteBranch,
    RebaseOnRemoteBranch,
    CreateBranchFromRemoteBranch,
    ResetToRemoteBranch,
    DeleteRemoteBranch,
    FetchRemoteBranch,
    FetchAndMergeRemoteBranch,
    FetchAndCheckoutRemoteBranch,
    FetchAndCreateBranchFromRemoteBranch,
    FetchAndRebaseOnRemoteBranch,

    // A tag (TagNode)
    CheckoutTag,
    MergeTag,
    RebaseOnTag,
    CreateBranchFromTag,
    ResetToTag,
    DeleteTag,

    // A folder of local branches (BranchPathNode)
    CreateBranchInFolder,
    DeleteAllBranchesInFolder,

    // The remotes (RemoteBranchTree) and a remote (RemoteRepoNode)
    ManageRemotes,
    FetchAllRemotes,
    FetchAndPruneAllRemotes,
    FetchRemote,
    FetchAndPruneRemote,
    OpenRemoteUrl,
    EnableRemote,
    EnableRemoteAndFetch,
    DisableRemote,

    // The stashes (StashTree) and a stash (StashNode)
    StashAll,
    StashStaged,
    ManageStashes,
    OpenStash,
    ApplyStash,
    PopStash,
    DropStash,

    // A submodule (SubmoduleNode)
    OpenSubmodule,
    OpenSubmoduleInNewInstance,
    ManageSubmodules,
    SynchronizeSubmodules,
    UpdateSubmodule,
    ResetSubmodule,
    StashSubmodule,
    CommitSubmodule,

    // The worktrees (WorktreeTree) and a worktree (WorktreeNode)
    CreateWorktree,
    PruneWorktrees,
    ManageWorktrees,
    OpenWorktree,
    DeleteWorktree,
    CopyWorktreePath,
    ShowWorktreeInFolder,
}

/// <summary>
///  The submodules of the repository, as <c>SubmoduleStatusProvider.StatusUpdated</c> reports them
///  (<c>SubmoduleInfoResult</c>), with the paths the tree is built from.
/// </summary>
/// <param name="TopProject">The top project (the root of the tree).</param>
/// <param name="AllSubmodules">All submodules of the top project, recursively.</param>
/// <param name="ModulePaths">The working directory of the current module and of its super projects.</param>
/// <param name="TopModuleWorkingDir">The working directory of the top project.</param>
/// <param name="CurrentSubmoduleStatus">The git status of the current module, if it is a submodule.</param>
/// <param name="StructureUpdated">Whether the structure changed (else only the status of the submodules).</param>
public sealed record LeftPanelSubmodules(
    SubmoduleInfo TopProject,
    IReadOnlyList<SubmoduleInfo> AllSubmodules,
    IReadOnlyList<string> ModulePaths,
    string TopModuleWorkingDir,
    IReadOnlyList<GitItemStatus>? CurrentSubmoduleStatus,
    bool StructureUpdated);

/// <summary>An item of the context menu of the left panel, a separator, or a submenu.</summary>
public sealed record LeftPanelMenuItem(
    string Header,
    Action? Execute = null,
    string? Icon = null,
    string? ToolTip = null,
    bool IsEnabled = true,
    bool? IsChecked = null,
    IReadOnlyList<LeftPanelMenuItem>? Children = null)
{
    public static LeftPanelMenuItem Separator { get; } = new("-");

    public bool IsSeparator => ReferenceEquals(this, Separator);
}

/// <summary>What the left panel needs from the application: git, the dialogs of <c>UICommands</c> and the grid's filter.</summary>
public interface ILeftPanelHost
{
    /// <summary>Whether the module is a git working directory (not in the dashboard).</summary>
    bool IsValidWorkingDir { get; }

    bool IsBareRepository { get; }

    /// <summary>The working directory of the module.</summary>
    string WorkingDir { get; }

    /// <summary>The configured hotkeys of the left panel (settings name "LeftPanel").</summary>
    IReadOnlyList<HotkeyBinding> Hotkeys { get; }

    /// <summary>Whether the grid shows only some branches (the branch filter, which "filter for selected" sets).</summary>
    bool IsBranchFilterActive { get; }

    /// <summary>All references, sorted as configured (<c>FilteredGitRefsProvider.GetRefs</c>); called in the background.</summary>
    IReadOnlyList<IGitRef> GetRefs();

    /// <summary>The checked out branch (<c>IRevisionGridInfo.GetCurrentBranch</c>); called in the background.</summary>
    string GetCurrentBranch();

    /// <summary>The ahead/behind counts of the branches (<c>IAheadBehindDataProvider.GetData</c>); called in the background.</summary>
    IReadOnlyDictionary<string, AheadBehindData>? GetAheadBehindData();

    /// <summary>The active remotes (<c>GetRemotesAsync</c>); called in the background.</summary>
    IReadOnlyList<Remote> GetRemotes();

    /// <summary>The inactive remotes (<c>GetDisabledRemotes</c>); called in the background.</summary>
    IReadOnlyList<Remote> GetDisabledRemotes();

    /// <summary>The stashes (if stashes are shown, <c>RevisionReader.GetStashes</c>); called in the background.</summary>
    IReadOnlyCollection<GitRevision> GetStashes();

    /// <summary>The worktrees (<c>GetWorktrees</c>); called in the background.</summary>
    IReadOnlyList<GitWorktree> GetWorktrees();

    bool DirectoryExists(string path);

    /// <summary>The branches (full names) merged into <paramref name="commit"/>, local and remote.</summary>
    Task<IReadOnlyCollection<string>> GetMergedBranchesAsync(string commit, CancellationToken cancellationToken);

    /// <summary>The tooltip of a submodule (its status, as <c>SubmoduleNode.SetStatusToolTipAsync</c>); called in the background.</summary>
    string GetSubmoduleToolTip(SubmoduleInfo info, IReadOnlyList<GitItemStatus>? gitStatus);

    /// <summary>Requests the submodules (<c>UpdateSubmodulesStructureAsync</c>); they come with <see cref="SubmodulesUpdated"/>.</summary>
    void UpdateSubmodules();

    /// <summary>Raised on the UI thread with the submodules, when requested and when their status changed.</summary>
    event EventHandler<LeftPanelSubmodules>? SubmodulesUpdated;

    /// <summary>Runs <paramref name="work"/> in the background; the result is awaited on the UI thread.</summary>
    Task<T> RunInBackgroundAsync<T>(Func<T> work, CancellationToken cancellationToken);

    /// <summary>Runs an operation of a node; returns whether it was done (e.g. not cancelled).</summary>
    bool Run(LeftPanelAction action, LeftPanelNode? node);

    /// <summary>Filters the grid to the space-separated references, or shows all branches (<see langword="null"/>).</summary>
    void FilterRevisionGrid(string? refs);

    void CopyToClipboard(string text);

    /// <summary>A node's revision is not in the grid (<c>MessageBoxes.RevisionFilteredInGrid</c>).</summary>
    void ShowRevisionNotInGrid(ObjectId objectId);

    /// <summary>A submodule's directory is missing (<c>MessageBoxes.SubmoduleDirectoryDoesNotExist</c>).</summary>
    void ShowSubmoduleDirectoryMissing(string directory, string submoduleName);
}

/// <summary>The settings of the left panel (the <c>AppSettings</c> of <c>RepoObjectsTree</c>).</summary>
public interface ILeftPanelSettings
{
    /// <summary>Whether the tree is shown (<c>RepoObjectsTreeShow*</c>).</summary>
    bool IsShown(LeftPanelTreeKind kind);

    void SetShown(LeftPanelTreeKind kind, bool shown);

    /// <summary>The position of the tree (<c>RepoObjectsTree*Index</c>).</summary>
    int GetIndex(LeftPanelTreeKind kind);

    void SetIndex(LeftPanelTreeKind kind, int index);

    GitRefsSortBy RefsSortBy { get; set; }

    GitRefsSortOrder RefsSortOrder { get; set; }

    /// <summary>The regexes of the branches listed first (<c>AppSettings.PrioritizedBranchNames</c>).</summary>
    string PrioritizedBranchNames { get; }

    /// <summary>The regexes of the remotes listed first (<c>AppSettings.PrioritizedRemoteNames</c>).</summary>
    string PrioritizedRemoteNames { get; }
}
