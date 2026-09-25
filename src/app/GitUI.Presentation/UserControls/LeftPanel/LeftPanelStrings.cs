using GitUI.Presentation.Translations;

namespace GitUI.Presentation.UserControls.LeftPanel;

/// <summary>
///  Strings of the left panel; ids match <c>RepoObjectsTree</c>, the tree names of <c>TranslatedStrings</c> and the toggle of
///  <c>FormBrowse</c>.
/// </summary>
public sealed class LeftPanelStrings : ViewStrings
{
    private const string TranslatedStringsCategory = "TranslatedStrings";

    public LeftPanelStrings()
        : base("RepoObjectsTree")
    {
        Search = Add("_searchTooltip", "Text", "Search");
        SortBy = Add("_sortByContextMenuItem", "Text", "&Sort by");
        SortOrder = Add("_sortOrderContextMenuItem", "Text", "&Sort order");
        CopyToClipboard = Add("copyContextMenuItem", "Text", "&Copy to clipboard");
        FilterForSelected = Add("filterForSelectedRefsMenuItem", "Text", "&Filter for selected");
        FilterForSelectedToolTip = Add("filterForSelectedRefsMenuItem", "ToolTipText", "Filter the revision grid to show selected (underlined) refs (branches and tags) only."
            + "\nHold CTRL while clicking to de/select multiple and include descendant tree nodes by additionally holding SHIFT."
            + "\nReset the filter via View > Show all branches.");

        // The remotes and a remote
        ManageRemotesFromRoot = Add("mnuBtnManageRemotesFromRootNode", "Text", "&Manage...");
        ManageRemotesFromRootToolTip = Add("mnuBtnManageRemotesFromRootNode", "ToolTipText", "Manage remotes");
        FetchAllRemotes = Add("mnuBtnFetchAllRemotes", "Text", "Fetch all remotes");
        FetchAndPruneAllRemotes = Add("mnuBtnPruneAllRemotes", "Text", "Fetch and prune all remotes");
        ManageRemotes = Add("mnubtnManageRemotes", "Text", "&Manage...");
        ManageRemotesToolTip = Add("mnubtnManageRemotes", "ToolTipText", "Manage remotes");
        EnableRemote = Add("mnubtnEnableRemote", "Text", "&Activate");
        EnableRemoteAndFetch = Add("mnubtnEnableRemoteAndFetch", "Text", "A&ctivate and fetch");
        DisableRemote = Add("mnubtnDisableRemote", "Text", "&Deactivate");
        FetchRemote = Add("mnubtnFetchAllBranchesFromARemote", "Text", "&Fetch");
        FetchAndPruneRemote = Add("mnuBtnPruneAllBranchesFromARemote", "Text", "Fetch and &prune");
        OpenRemoteUrl = Add("mnuBtnOpenRemoteUrlInBrowser", "Text", "Open remote Url");
        OpenRemoteUrlToolTip = Add("mnuBtnOpenRemoteUrlInBrowser", "ToolTipText", "Redirects you to the actual repository page");

        // A submodule
        OpenSubmodule = Add("mnubtnOpenSubmodule", "Text", "&Open");
        OpenSubmoduleToolTip = Add("mnubtnOpenSubmodule", "ToolTipText", "Open selected submodule");
        OpenSubmoduleInNewInstance = Add("mnubtnOpenGESubmodule", "Text", "O&pen");
        OpenSubmoduleInNewInstanceToolTip = Add("mnubtnOpenGESubmodule", "ToolTipText", "Open selected submodule in a new instance");
        ManageSubmodules = Add("mnubtnManageSubmodules", "Text", "&Manage...");
        ManageSubmodulesToolTip = Add("mnubtnManageSubmodules", "ToolTipText", "Manage submodules");
        UpdateSubmodule = Add("mnubtnUpdateSubmodule", "Text", "&Update");
        UpdateSubmoduleToolTip = Add("mnubtnUpdateSubmodule", "ToolTipText", "Update selected submodule recursively");
        SynchronizeSubmodules = Add("mnubtnSynchronizeSubmodules", "Text", "Synchronize");
        SynchronizeSubmodulesToolTip = Add("mnubtnSynchronizeSubmodules", "ToolTipText", "Synchronize selected submodule recursively");
        ResetSubmodule = Add("mnubtnResetSubmodule", "Text", "&Reset");
        ResetSubmoduleToolTip = Add("mnubtnResetSubmodule", "ToolTipText", "Reset selected submodule");
        StashSubmodule = Add("mnubtnStashSubmodule", "Text", "&Stash");
        StashSubmoduleToolTip = Add("mnubtnStashSubmodule", "ToolTipText", "Stash changes in selected submodule");
        CommitSubmodule = Add("mnubtnCommitSubmodule", "Text", "&Commit");
        CommitSubmoduleToolTip = Add("mnubtnCommitSubmodule", "ToolTipText", "Commit changes in selected submodule");

        // A remote branch
        FetchAndCheckout = Add("mnubtnRemoteBranchFetchAndCheckout", "Text", "&Fetch && Checkout");
        FetchAndCheckoutToolTip = Add("mnubtnRemoteBranchFetchAndCheckout", "ToolTipText", "Fetch then checkout this remote branch");
        FetchAndMerge = Add("mnubtnPullFromRemoteBranch", "Text", "Fetch && Merge (&Pull)");
        FetchAndMergeToolTip = Add("mnubtnPullFromRemoteBranch", "ToolTipText", "Fetch then merge this remote branch into current branch");
        FetchAndRebase = Add("mnubtnFetchRebase", "Text", "Fetch && Re&base");
        FetchAndRebaseToolTip = Add("mnubtnFetchRebase", "ToolTipText", "Fetch then rebase current branch on this remote branch");
        FetchAndCreateBranch = Add("mnubtnFetchCreateBranch", "Text", "Fetc&h && Create Branch");
        FetchAndCreateBranchToolTip = Add("mnubtnFetchCreateBranch", "ToolTipText", "Fetch then create a local branch from the remote branch");
        FetchBranch = Add("mnubtnFetchOneBranch", "Text", "Fe&tch");
        FetchBranchToolTip = Add("mnubtnFetchOneBranch", "ToolTipText", "Fetch this remote branch");

        // A folder of branches
        CreateBranchInFolder = Add("mnubtnCreateBranch", "Text", "Create Branch...");
        CreateBranchInFolderToolTip = Add("mnubtnCreateBranch", "ToolTipText", "Create a local branch");
        DeleteAllBranches = Add("mnubtnDeleteAllBranches", "Text", "Delete All");
        DeleteAllBranchesToolTip = Add("mnubtnDeleteAllBranches", "ToolTipText", "Delete all child branches, which must all be fully merged in its upstream branch or in HEAD");

        // The stashes and a stash
        StashAll = Add("mnubtnStashAllFromRootNode", "Text", "&Stash");
        StashStaged = Add("mnubtnStashStagedFromRootNode", "Text", "S&tash staged");
        ManageStashes = Add("mnubtnManageStashFromRootNode", "Text", "&Manage stashes...");
        OpenStash = Add("mnubtnOpenStash", "Text", "&Open stash");
        OpenStashToolTip = Add("mnubtnOpenStash", "ToolTipText", "Open this stash");
        ApplyStash = Add("mnubtnApplyStash", "Text", "&Apply stash");
        ApplyStashToolTip = Add("mnubtnApplyStash", "ToolTipText", "Apply this stash");
        PopStash = Add("mnubtnPopStash", "Text", "&Pop stash");
        PopStashToolTip = Add("mnubtnPopStash", "ToolTipText", "Pop this stash");
        DropStash = Add("mnubtnDropStash", "Text", "&Drop stash...");
        DropStashToolTip = Add("mnubtnDropStash", "ToolTipText", "Drop this stash");

        // The worktrees and a worktree
        CreateWorktree = Add("mnubtnCreateWorktreeFromRootNode", "Text", "&Create worktree...");
        PruneWorktrees = Add("mnubtnPruneWorktreesFromRootNode", "Text", "&Prune worktrees");
        ManageWorktrees = Add("mnubtnManageWorktreesFromRootNode", "Text", "&Manage worktrees...");
        OpenWorktree = Add("mnubtnOpenWorktree", "Text", "&Open worktree");
        OpenWorktreeToolTip = Add("mnubtnOpenWorktree", "ToolTipText", "Open this worktree");
        DeleteWorktree = Add("mnubtnDeleteWorktree", "Text", "&Delete worktree...");
        DeleteWorktreeToolTip = Add("mnubtnDeleteWorktree", "ToolTipText", "Delete this worktree");
        CopyWorktreePath = Add("mnubtnCopyWorktreePath", "Text", "Copy &path");
        CopyWorktreePathToolTip = Add("mnubtnCopyWorktreePath", "ToolTipText", "Copy the worktree path to clipboard");
        ShowWorktreeInFolder = Add("mnubtnShowWorktreeInFolder", "Text", "Show &in folder");
        ShowWorktreeInFolderToolTip = Add("mnubtnShowWorktreeInFolder", "ToolTipText", "Show the worktree in File Explorer");

        // Expand, collapse, order
        Collapse = Add("mnubtnCollapse", "Text", "Collapse");
        CollapseToolTip = Add("mnubtnCollapse", "ToolTipText", "Collapse all subnodes");
        Expand = Add("mnubtnExpand", "Text", "Expand");
        ExpandToolTip = Add("mnubtnExpand", "ToolTipText", "Expand all subnodes");
        MoveUp = Add("mnubtnMoveUp", "Text", "Move Up");
        MoveUpToolTip = Add("mnubtnMoveUp", "ToolTipText", "Move node up");
        MoveDown = Add("mnubtnMoveDown", "Text", "Move Down");
        MoveDownToolTip = Add("mnubtnMoveDown", "ToolTipText", "Move node down");

        // The toolbar
        CollapseAllToolTip = Add("tsbCollapseAll", "ToolTipText", "Collapse all subnodes");
        ShowBranchesToolTip = Add("tsbShowBranches", "ToolTipText", "Branches");
        ShowRemotesToolTip = Add("tsbShowRemotes", "ToolTipText", "Remotes");
        ShowTagsToolTip = Add("tsbShowTags", "ToolTipText", "Tags");
        ShowSubmodulesToolTip = Add("tsbShowSubmodules", "ToolTipText", "Submodules");
        ShowStashesToolTip = Add("tsbShowStashes", "ToolTipText", "Stashes");
        ShowWorktreesToolTip = Add("tsbShowWorktrees", "ToolTipText", "Worktrees");

        // The git reference menus (MenuItemsGenerator and its strings)
        Merge = Add("Merge", "Text", "&Merge into current branch...", category: "MenuItemsStrings");
        CreateBranch = Add("CreateBranch", "Text", "Create &branch...", category: "MenuItemsStrings");
        Reset = Add("Reset", "Text", "Re&set current branch to here...", category: "MenuItemsStrings");
        Rename = Add("Rename", "Text", "R&ename branch...", category: "MenuItemsStrings");
        BranchCheckout = Add("Checkout", "Text", "Chec&kout branch...", category: "BranchMenuItemsStrings");
        BranchRebase = Add("Rebase", "Text", "&Rebase current branch on this branch...", category: "BranchMenuItemsStrings");
        BranchDelete = Add("Delete", "Text", "&Delete branch...", category: "BranchMenuItemsStrings");
        BranchCheckoutToolTip = Add("CheckoutTooltip", "Text", "Checkout this branch", category: "BranchMenuItemsStrings");
        BranchMergeToolTip = Add("MergeTooltip", "Text", "Merge this branch into current branch", category: "BranchMenuItemsStrings");
        BranchCreateToolTip = Add("CreateTooltip", "Text", "Create a local branch from this branch", category: "BranchMenuItemsStrings");
        BranchRebaseToolTip = Add("RebaseTooltip", "Text", "Rebase current branch on this branch", category: "BranchMenuItemsStrings");
        BranchResetToolTip = Add("ResetTooltip", "Text", "Reset current branch to here", category: "BranchMenuItemsStrings");
        BranchRenameToolTip = Add("RenameTooltip", "Text", "Rename this branch", category: "BranchMenuItemsStrings");
        LocalBranchDeleteToolTip = Add("DeleteTooltip", "Text", "Delete the branch, which must be fully merged in its upstream branch or in HEAD", category: "LocalBranchMenuItemsStrings");
        RemoteBranchCheckout = Add("Checkout", "Text", "Chec&kout remote branch...", category: "RemoteBranchMenuItemsStrings");
        RemoteBranchRebase = Add("Rebase", "Text", "&Rebase current branch on this remote branch...", category: "RemoteBranchMenuItemsStrings");
        RemoteBranchDelete = Add("Delete", "Text", "&Delete remote branch...", category: "RemoteBranchMenuItemsStrings");
        RemoteBranchDeleteToolTip = Add("DeleteTooltip", "Text", "Delete the branch from the remote", category: "RemoteBranchMenuItemsStrings");
        TagCheckout = Add("Checkout", "Text", "Chec&kout tag revision...", category: "TagMenuItemsStrings");
        TagRebase = Add("Rebase", "Text", "&Rebase current branch on this tag revision...", category: "TagMenuItemsStrings");
        TagDelete = Add("Delete", "Text", "&Delete tag...", category: "TagMenuItemsStrings");
        TagCheckoutToolTip = Add("CheckoutTooltip", "Text", "Checkout this tag revision", category: "TagMenuItemsStrings");
        TagCreateToolTip = Add("CreateTooltip", "Text", "Create a local branch from this tag", category: "TagMenuItemsStrings");
        TagMergeToolTip = Add("MergeTooltip", "Text", "Merge this tag into current branch", category: "TagMenuItemsStrings");
        TagRebaseToolTip = Add("RebaseTooltip", "Text", "Rebase current branch on this tag", category: "TagMenuItemsStrings");
        TagResetToolTip = Add("ResetTooltip", "Text", "Reset current branch to here", category: "TagMenuItemsStrings");
        TagDeleteToolTip = Add("DeleteTooltip", "Text", "Delete this tag", category: "TagMenuItemsStrings");

        // TranslatedStrings
        Branches = Add("_branchesText", "Text", "Branches", category: TranslatedStringsCategory);
        Remotes = Add("_remotesText", "Text", "Remotes", category: TranslatedStringsCategory);
        Worktrees = Add("_worktreesText", "Text", "Worktrees", category: TranslatedStringsCategory);
        Tags = Add("_tagsText", "Text", "Tags", category: TranslatedStringsCategory);
        Submodules = Add("_submodulesText", "Text", "Submodules", category: TranslatedStringsCategory);
        Stashes = Add("_stashesText", "Text", "Stashes", category: TranslatedStringsCategory);
        Inactive = Add("_rotInactive", "Text", "[ Inactive ]", category: TranslatedStringsCategory);
        InvisibleCommit = Add("_invisibleCommitText", "Text", "'{0}' is not currently visible", category: TranslatedStringsCategory);
        ContainedInCurrentCommit = Add("_containedInCurrentCommitText", "Text", "'{0}' is contained in the currently selected commit", category: TranslatedStringsCategory);

        // FormBrowse
        RunScript = Add("runScriptToolStripMenuItem", "Text", "Run script");
        ToggleLeftPanel = Add("toggleLeftPanel", "ToolTipText", "Toggle left panel", category: "FormBrowse");
        ShowAllBranches = Add("tsmiShowBranchesAll", "ToolTipText", "Show all branches", category: "FormBrowse");
    }

    public TranslatedText Search { get; }

    public TranslatedText SortBy { get; }

    public TranslatedText SortOrder { get; }

    public TranslatedText CopyToClipboard { get; }

    public TranslatedText FilterForSelected { get; }

    public TranslatedText FilterForSelectedToolTip { get; }

    public TranslatedText ManageRemotesFromRoot { get; }

    public TranslatedText ManageRemotesFromRootToolTip { get; }

    public TranslatedText FetchAllRemotes { get; }

    public TranslatedText FetchAndPruneAllRemotes { get; }

    public TranslatedText ManageRemotes { get; }

    public TranslatedText ManageRemotesToolTip { get; }

    public TranslatedText EnableRemote { get; }

    public TranslatedText EnableRemoteAndFetch { get; }

    public TranslatedText DisableRemote { get; }

    public TranslatedText FetchRemote { get; }

    public TranslatedText FetchAndPruneRemote { get; }

    public TranslatedText OpenRemoteUrl { get; }

    public TranslatedText OpenRemoteUrlToolTip { get; }

    public TranslatedText OpenSubmodule { get; }

    public TranslatedText OpenSubmoduleToolTip { get; }

    public TranslatedText OpenSubmoduleInNewInstance { get; }

    public TranslatedText OpenSubmoduleInNewInstanceToolTip { get; }

    public TranslatedText ManageSubmodules { get; }

    public TranslatedText ManageSubmodulesToolTip { get; }

    public TranslatedText UpdateSubmodule { get; }

    public TranslatedText UpdateSubmoduleToolTip { get; }

    public TranslatedText SynchronizeSubmodules { get; }

    public TranslatedText SynchronizeSubmodulesToolTip { get; }

    public TranslatedText ResetSubmodule { get; }

    public TranslatedText ResetSubmoduleToolTip { get; }

    public TranslatedText StashSubmodule { get; }

    public TranslatedText StashSubmoduleToolTip { get; }

    public TranslatedText CommitSubmodule { get; }

    public TranslatedText CommitSubmoduleToolTip { get; }

    public TranslatedText FetchAndCheckout { get; }

    public TranslatedText FetchAndCheckoutToolTip { get; }

    public TranslatedText FetchAndMerge { get; }

    public TranslatedText FetchAndMergeToolTip { get; }

    public TranslatedText FetchAndRebase { get; }

    public TranslatedText FetchAndRebaseToolTip { get; }

    public TranslatedText FetchAndCreateBranch { get; }

    public TranslatedText FetchAndCreateBranchToolTip { get; }

    public TranslatedText FetchBranch { get; }

    public TranslatedText FetchBranchToolTip { get; }

    public TranslatedText CreateBranchInFolder { get; }

    public TranslatedText CreateBranchInFolderToolTip { get; }

    public TranslatedText DeleteAllBranches { get; }

    public TranslatedText DeleteAllBranchesToolTip { get; }

    public TranslatedText StashAll { get; }

    public TranslatedText StashStaged { get; }

    public TranslatedText ManageStashes { get; }

    public TranslatedText OpenStash { get; }

    public TranslatedText OpenStashToolTip { get; }

    public TranslatedText ApplyStash { get; }

    public TranslatedText ApplyStashToolTip { get; }

    public TranslatedText PopStash { get; }

    public TranslatedText PopStashToolTip { get; }

    public TranslatedText DropStash { get; }

    public TranslatedText DropStashToolTip { get; }

    public TranslatedText CreateWorktree { get; }

    public TranslatedText PruneWorktrees { get; }

    public TranslatedText ManageWorktrees { get; }

    public TranslatedText OpenWorktree { get; }

    public TranslatedText OpenWorktreeToolTip { get; }

    public TranslatedText DeleteWorktree { get; }

    public TranslatedText DeleteWorktreeToolTip { get; }

    public TranslatedText CopyWorktreePath { get; }

    public TranslatedText CopyWorktreePathToolTip { get; }

    public TranslatedText ShowWorktreeInFolder { get; }

    public TranslatedText ShowWorktreeInFolderToolTip { get; }

    public TranslatedText Collapse { get; }

    public TranslatedText CollapseToolTip { get; }

    public TranslatedText Expand { get; }

    public TranslatedText ExpandToolTip { get; }

    public TranslatedText MoveUp { get; }

    public TranslatedText MoveUpToolTip { get; }

    public TranslatedText MoveDown { get; }

    public TranslatedText MoveDownToolTip { get; }

    public TranslatedText CollapseAllToolTip { get; }

    public TranslatedText ShowBranchesToolTip { get; }

    public TranslatedText ShowRemotesToolTip { get; }

    public TranslatedText ShowTagsToolTip { get; }

    public TranslatedText ShowSubmodulesToolTip { get; }

    public TranslatedText ShowStashesToolTip { get; }

    public TranslatedText ShowWorktreesToolTip { get; }

    public TranslatedText Merge { get; }

    public TranslatedText CreateBranch { get; }

    public TranslatedText Reset { get; }

    public TranslatedText Rename { get; }

    public TranslatedText BranchCheckout { get; }

    public TranslatedText BranchRebase { get; }

    public TranslatedText BranchDelete { get; }

    public TranslatedText BranchCheckoutToolTip { get; }

    public TranslatedText BranchMergeToolTip { get; }

    public TranslatedText BranchCreateToolTip { get; }

    public TranslatedText BranchRebaseToolTip { get; }

    public TranslatedText BranchResetToolTip { get; }

    public TranslatedText BranchRenameToolTip { get; }

    public TranslatedText LocalBranchDeleteToolTip { get; }

    public TranslatedText RemoteBranchCheckout { get; }

    public TranslatedText RemoteBranchRebase { get; }

    public TranslatedText RemoteBranchDelete { get; }

    public TranslatedText RemoteBranchDeleteToolTip { get; }

    public TranslatedText TagCheckout { get; }

    public TranslatedText TagRebase { get; }

    public TranslatedText TagDelete { get; }

    public TranslatedText TagCheckoutToolTip { get; }

    public TranslatedText TagCreateToolTip { get; }

    public TranslatedText TagMergeToolTip { get; }

    public TranslatedText TagRebaseToolTip { get; }

    public TranslatedText TagResetToolTip { get; }

    public TranslatedText TagDeleteToolTip { get; }

    public TranslatedText Branches { get; }

    public TranslatedText Remotes { get; }

    public TranslatedText Worktrees { get; }

    public TranslatedText Tags { get; }

    public TranslatedText Submodules { get; }

    public TranslatedText Stashes { get; }

    public TranslatedText Inactive { get; }

    public TranslatedText InvisibleCommit { get; }

    public TranslatedText ContainedInCurrentCommit { get; }

    public TranslatedText RunScript { get; }

    public TranslatedText ToggleLeftPanel { get; }

    public TranslatedText ShowAllBranches { get; }
}
