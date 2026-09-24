using GitUI.Presentation.Translations;

namespace GitUI.Presentation.UserControls.RevisionGrid;

/// <summary>
///  Strings of the context menu of the revision grid; ids match <c>RevisionGridControl</c> and, for the navigate and view
///  menus, <c>RevisionGridMenuCommands</c> (category <c>RevisionGrid</c>).
/// </summary>
public sealed class RevisionGridMenuStrings : ViewStrings
{
    public RevisionGridMenuStrings()
        : base("RevisionGridControl")
    {
        AreYouSureRebase = Add("_areYouSureRebase", "Text", "Are you sure you want to rebase? This action will rewrite commit history.");
        BaseForCompareNotSelectedError = Add("_baseForCompareNotSelectedError", "Text", "Base commit for compare is not selected.");
        NoMergeBaseCommit = Add("_noMergeBaseCommit", "Text", "There is no common ancestor for the selected commits.");
        NoRevisionFoundError = Add("_noRevisionFoundError", "Text", "No revision found.");
        RebaseBranch = Add("_rebaseBranch", "Text", "Rebase branch.");
        RebaseBranchInteractive = Add("_rebaseBranchInteractive", "Text", "Rebase branch interactively.");
        RebaseConfirmTitle = Add("_rebaseConfirmTitle", "Text", "Rebase Confirmation");
        AmendCommit = Add("amendCommitToolStripMenuItem", "Text", "Create an &amend commit...");
        ApplyStash = Add("applyStashToolStripMenuItem", "Text", "Appl&y stash");
        ArchiveRevision = Add("archiveRevisionToolStripMenuItem", "Text", "Arch&ive this commit...");
        BisectSkipRevision = Add("bisectSkipRevisionToolStripMenuItem", "Text", "Skip revision");
        CheckoutBranch = Add("checkoutBranchToolStripMenuItem", "Text", "Chec&kout branch...");
        CheckoutRevision = Add("checkoutRevisionToolStripMenuItem", "Text", "Checkout &this commit...");
        CherryPickCommit = Add("cherryPickCommitToolStripMenuItem", "Text", "Cherr&y pick this commit...");
        Commit = Add("commitToolStripMenuItem", "Text", "&Commit");
        CompareSelectedCommits = Add("compareSelectedCommitsMenuItem", "Text", "Compare &selected commits");
        CompareToBase = Add("compareToBaseToolStripMenuItem", "Text", "Compare to &BASE");
        CompareToBranch = Add("compareToBranchToolStripMenuItem", "Text", "Compare &to branch...");
        CompareToWorkingDirectory = Add("compareToWorkingDirectoryMenuItem", "Text", "Compare to &working directory");
        Compare = Add("compareToolStripMenuItem", "Text", "Com&pare");
        CompareWithCurrentBranch = Add("compareWithCurrentBranchToolStripMenuItem", "Text", "Compare with &current branch");
        CopyToClipboard = Add("copyToClipboardToolStripMenuItem", "Text", "&Copy to clipboard");
        CreateNewBranch = Add("createNewBranchToolStripMenuItem", "Text", "Create new branch here (&x)...");
        CreateTag = Add("createTagToolStripMenuItem", "Text", "Create new ta&g here...");
        DeleteBranch = Add("deleteBranchToolStripMenuItem", "Text", "&Delete branch...");
        DeleteTag = Add("deleteTagToolStripMenuItem", "Text", "&Delete tag...");
        DropStash = Add("dropStashToolStripMenuItem", "Text", "&Drop stash...");
        EditCommit = Add("editCommitToolStripMenuItem", "Text", "&Edit commit");
        FixupCommit = Add("fixupCommitToolStripMenuItem", "Text", "Create a &fixup commit...");
        GetHelpOnHowToUseTheseFeatures = Add("getHelpOnHowToUseTheseFeaturesToolStripMenuItem", "Text", "Get &help on how to use these features");
        ManipulateCommit = Add("manipulateCommitToolStripMenuItem", "Text", "&Advanced");
        MarkRevisionAsBad = Add("markRevisionAsBadToolStripMenuItem", "Text", "Mark revision as bad");
        MarkRevisionAsGood = Add("markRevisionAsGoodToolStripMenuItem", "Text", "Mark revision as good");
        MergeBranch = Add("mergeBranchToolStripMenuItem", "Text", "&Merge into current branch...");
        Navigate = Add("navigateToolStripMenuItem", "Text", "&Navigate");
        OpenBuildReport = Add("openBuildReportToolStripMenuItem", "Text", "View &build report in a browser");
        OpenCommitsWithDiffTool = Add("openCommitsWithDiffToolMenuItem", "Text", "Open selected commits with &difftool");
        OpenPullRequestPage = Add("openPullRequestPageStripMenuItem", "Text", "Vie&w pull request in a browser");
        PopStash = Add("popStashToolStripMenuItem", "Text", "Pop &stash");
        RebaseInteractively = Add("rebaseInteractivelyToolStripMenuItem", "Text", "Selected commit &interactively...");
        RebaseOn = Add("rebaseOnToolStripMenuItem", "Text", "&Rebase current branch on");
        Rebase = Add("rebaseToolStripMenuItem", "Text", "&Selected commit");
        RebaseWithAdvOptions = Add("rebaseWithAdvOptionsToolStripMenuItem", "Text", "Selected commit with &advanced options...");
        RenameBranch = Add("renameBranchToolStripMenuItem", "Text", "R&ename branch...");
        ResetAnotherBranchToHere = Add("resetAnotherBranchToHereToolStripMenuItem", "Text", "Reset an&other branch to here...");
        ResetChanges = Add("resetChangesToolStripMenuItem", "Text", "&Reset changes");
        ResetCurrentBranchToHere = Add("resetCurrentBranchToHereToolStripMenuItem", "Text", "Reset c&urrent branch to here...");
        RevertCommit = Add("revertCommitToolStripMenuItem", "Text", "Re&vert this commit...");
        RewordCommit = Add("rewordCommitToolStripMenuItem", "Text", "&Reword commit");
        RunScript = Add("runScriptToolStripMenuItem", "Text", "Run &script");
        SelectAsBase = Add("selectAsBaseToolStripMenuItem", "Text", "Select &as BASE to compare");
        SquashCommit = Add("squashCommitToolStripMenuItem", "Text", "Create a &squash commit...");
        StopBisect = Add("stopBisectToolStripMenuItem", "Text", "Stop bisect");
        TsmiOtherActions = Add("tsmiOtherActions", "Text", "&Other actions");
        TsmiPushBranch = Add("tsmiPushBranch", "Text", "Pus&h branch...");
        TsmiSelectInLeftPanel = Add("tsmiSelectInLeftPanel", "Text", "Se&lect in left panel");
        View = Add("viewToolStripMenuItem", "Text", "View");
        AuthorDateSort = Add("AuthorDateSort", "Text", "&Sort commits by author date", category: "RevisionGrid");
        Branches = Add("BranchesToolStripMenuItem", "Text", "Branches", category: "RevisionGrid");
        Columns = Add("ColumnsToolStripMenuItem", "Text", "Columns", category: "RevisionGrid");
        Commits = Add("CommitsToolStripMenuItem", "Text", "Commits", category: "RevisionGrid");
        GotoChildCommit = Add("GotoChildCommit", "Text", "Go to c&hild commit", category: "RevisionGrid");
        GotoCommit = Add("GotoCommit", "Text", "Go to &commit...", category: "RevisionGrid");
        GotoCurrentRevision = Add("GotoCurrentRevision", "Text", "Go to c&urrent revision", category: "RevisionGrid");
        GotoFirstParentCommit = Add("GotoFirstParentCommit", "Text", "Go to f&irst parent commit", category: "RevisionGrid");
        GotoLastParentCommit = Add("GotoLastParentCommit", "Text", "Go to &last parent commit", category: "RevisionGrid");
        GotoMergeBaseCommit = Add("GotoMergeBaseCommit", "Text", "Go to common &ancestor (merge base)", category: "RevisionGrid");
        GotoMergeBaseCommitToolTip = Add("GotoMergeBaseCommit", "ToolTipText", "Selects the common ancestor commit (merge base), which is the most recent shared ancestor of the selected commits (or if only one commit is selected, between it and the checked out commit (HEAD))", category: "RevisionGrid");
        GotoParentCommit = Add("GotoParentCommit", "Text", "Go to &parent commit", category: "RevisionGrid");
        HighlightSelectedBranch = Add("HighlightSelectedBranch", "Text", "Highlight selected branch (until refresh)", category: "RevisionGrid");
        NavigateBackward = Add("NavigateBackward", "Text", "Navigate &backward", category: "RevisionGrid");
        NavigateForward = Add("NavigateForward", "Text", "Navigate &forward", category: "RevisionGrid");
        NextQuickSearch = Add("NextQuickSearch", "Text", "Quick search &next", category: "RevisionGrid");
        PrevQuickSearch = Add("PrevQuickSearch", "Text", "Quick search p&revious", category: "RevisionGrid");
        QuickSearch = Add("QuickSearch", "Text", "&Quick search", category: "RevisionGrid");
        QuickSearchToolTip = Add("QuickSearch", "ToolTipText", "Start typing in revision grid to start quick search.", category: "RevisionGrid");
        SaveAsDefault = Add("SaveAsDefault", "Text", "Save current view settings as default", category: "RevisionGrid");
        ShowAllBranches = Add("ShowAllBranches", "Text", "Show &all branches", category: "RevisionGrid");
        ShowArtificialCommits = Add("ShowArtificialCommits", "Text", "Show artificial commits", category: "RevisionGrid");
        ShowCurrentBranchOnly = Add("ShowCurrentBranchOnly", "Text", "Show &current branch only", category: "RevisionGrid");
        ShowFilteredBranches = Add("ShowFilteredBranches", "Text", "Show &filtered branches", category: "RevisionGrid");
        ShowReflogReferences = Add("ShowReflogReferences", "Text", "Show &reflog references", category: "RevisionGrid");
        ShowRemoteBranches = Add("ShowRemoteBranches", "Text", "Show remote &branches", category: "RevisionGrid");
        ShowSessionCheckpoints = Add("ShowSessionCheckpoints", "Text", "Show session checkpoints", category: "RevisionGrid");
        ShowStashes = Add("ShowStashes", "Text", "Show stashes", category: "RevisionGrid");
        ShowSuperprojectBranches = Add("ShowSuperprojectBranches", "Text", "Show sup&erproject branches", category: "RevisionGrid");
        ShowSuperprojectRemoteBranches = Add("ShowSuperprojectRemoteBranches", "Text", "Show superpro&ject remote branches", category: "RevisionGrid");
        ShowSuperprojectTags = Add("ShowSuperprojectTags", "Text", "Show su&perproject tags", category: "RevisionGrid");
        Sorting = Add("SortingToolStripMenuItem", "Text", "Sorting", category: "RevisionGrid");
        ToggleBetweenArtificialAndHeadCommits = Add("ToggleBetweenArtificialAndHeadCommits", "Text", "&Toggle between artificial and HEAD commits", category: "RevisionGrid");
        TopoOrder = Add("TopoOrder", "Text", "Arrange c&ommits by topo order (ancestor order)", category: "RevisionGrid");
        QuickSearchQuickHelp = Add("_quickSearchQuickHelp", "Text", "Start typing in revision grid to start quick search.", category: "RevisionGrid");
        DrawNonrelativesGray = Add("drawNonrelativesGrayToolStripMenuItem", "Text", "Draw non relatives gra&y", category: "RevisionGrid");
        Filter = Add("filterToolStripMenuItem", "Text", "Advanced filter...", category: "RevisionGrid");
        ShowAuthorAvatarColumn = Add("showAuthorAvatarColumnToolStripMenuItem", "Text", "Show aut&hor avatar column", category: "RevisionGrid");
        ShowAuthorDate = Add("showAuthorDateToolStripMenuItem", "Text", "Sho&w author date", category: "RevisionGrid");
        ShowAuthorNameColumn = Add("showAuthorNameColumnToolStripMenuItem", "Text", "Show a&uthor name column", category: "RevisionGrid");
        ShowBuildStatusIcon = Add("showBuildStatusIconToolStripMenuItem", "Text", "Show build status &icon", category: "RevisionGrid");
        ShowBuildStatusText = Add("showBuildStatusTextToolStripMenuItem", "Text", "Show build status te&xt", category: "RevisionGrid");
        ShowCommitMessageBody = Add("showCommitMessageBodyToolStripMenuItem", "Text", "Show commit message body", category: "RevisionGrid");
        ShowDateColumn = Add("showDateColumnToolStripMenuItem", "Text", "Show &date column", category: "RevisionGrid");
        ShowGitNotesColumn = Add("showGitNotesColumnToolStripMenuItem", "Text", "Show Git &notes column", category: "RevisionGrid");
        ShowGitNotes = Add("showGitNotesToolStripMenuItem", "Text", "Show git &notes", category: "RevisionGrid");
        ShowIdColumn = Add("showIdColumnToolStripMenuItem", "Text", "Show SHA&-1 column", category: "RevisionGrid");
        ShowRelativeDate = Add("showRelativeDateToolStripMenuItem", "Text", "Show relati&ve date", category: "RevisionGrid");
        ShowRevisionGraphColumn = Add("showRevisionGraphColumnToolStripMenuItem", "Text", "Show revision &graph column", category: "RevisionGrid");
        ShowTags = Add("showTagsToolStripMenuItem", "Text", "Show &tags", category: "RevisionGrid");
    }

    public TranslatedText AreYouSureRebase { get; }

    public TranslatedText BaseForCompareNotSelectedError { get; }

    public TranslatedText NoMergeBaseCommit { get; }

    public TranslatedText NoRevisionFoundError { get; }

    public TranslatedText RebaseBranch { get; }

    public TranslatedText RebaseBranchInteractive { get; }

    public TranslatedText RebaseConfirmTitle { get; }

    public TranslatedText AmendCommit { get; }

    public TranslatedText ApplyStash { get; }

    public TranslatedText ArchiveRevision { get; }

    public TranslatedText BisectSkipRevision { get; }

    public TranslatedText CheckoutBranch { get; }

    public TranslatedText CheckoutRevision { get; }

    public TranslatedText CherryPickCommit { get; }

    public TranslatedText Commit { get; }

    public TranslatedText CompareSelectedCommits { get; }

    public TranslatedText CompareToBase { get; }

    public TranslatedText CompareToBranch { get; }

    public TranslatedText CompareToWorkingDirectory { get; }

    public TranslatedText Compare { get; }

    public TranslatedText CompareWithCurrentBranch { get; }

    public TranslatedText CopyToClipboard { get; }

    public TranslatedText CreateNewBranch { get; }

    public TranslatedText CreateTag { get; }

    public TranslatedText DeleteBranch { get; }

    public TranslatedText DeleteTag { get; }

    public TranslatedText DropStash { get; }

    public TranslatedText EditCommit { get; }

    public TranslatedText FixupCommit { get; }

    public TranslatedText GetHelpOnHowToUseTheseFeatures { get; }

    public TranslatedText ManipulateCommit { get; }

    public TranslatedText MarkRevisionAsBad { get; }

    public TranslatedText MarkRevisionAsGood { get; }

    public TranslatedText MergeBranch { get; }

    public TranslatedText Navigate { get; }

    public TranslatedText OpenBuildReport { get; }

    public TranslatedText OpenCommitsWithDiffTool { get; }

    public TranslatedText OpenPullRequestPage { get; }

    public TranslatedText PopStash { get; }

    public TranslatedText RebaseInteractively { get; }

    public TranslatedText RebaseOn { get; }

    public TranslatedText Rebase { get; }

    public TranslatedText RebaseWithAdvOptions { get; }

    public TranslatedText RenameBranch { get; }

    public TranslatedText ResetAnotherBranchToHere { get; }

    public TranslatedText ResetChanges { get; }

    public TranslatedText ResetCurrentBranchToHere { get; }

    public TranslatedText RevertCommit { get; }

    public TranslatedText RewordCommit { get; }

    public TranslatedText RunScript { get; }

    public TranslatedText SelectAsBase { get; }

    public TranslatedText SquashCommit { get; }

    public TranslatedText StopBisect { get; }

    public TranslatedText TsmiOtherActions { get; }

    public TranslatedText TsmiPushBranch { get; }

    public TranslatedText TsmiSelectInLeftPanel { get; }

    public TranslatedText View { get; }

    public TranslatedText AuthorDateSort { get; }

    public TranslatedText Branches { get; }

    public TranslatedText Columns { get; }

    public TranslatedText Commits { get; }

    public TranslatedText GotoChildCommit { get; }

    public TranslatedText GotoCommit { get; }

    public TranslatedText GotoCurrentRevision { get; }

    public TranslatedText GotoFirstParentCommit { get; }

    public TranslatedText GotoLastParentCommit { get; }

    public TranslatedText GotoMergeBaseCommit { get; }

    public TranslatedText GotoMergeBaseCommitToolTip { get; }

    public TranslatedText GotoParentCommit { get; }

    public TranslatedText HighlightSelectedBranch { get; }

    public TranslatedText NavigateBackward { get; }

    public TranslatedText NavigateForward { get; }

    public TranslatedText NextQuickSearch { get; }

    public TranslatedText PrevQuickSearch { get; }

    public TranslatedText QuickSearch { get; }

    public TranslatedText QuickSearchToolTip { get; }

    public TranslatedText SaveAsDefault { get; }

    public TranslatedText ShowAllBranches { get; }

    public TranslatedText ShowArtificialCommits { get; }

    public TranslatedText ShowCurrentBranchOnly { get; }

    public TranslatedText ShowFilteredBranches { get; }

    public TranslatedText ShowReflogReferences { get; }

    public TranslatedText ShowRemoteBranches { get; }

    public TranslatedText ShowSessionCheckpoints { get; }

    public TranslatedText ShowStashes { get; }

    public TranslatedText ShowSuperprojectBranches { get; }

    public TranslatedText ShowSuperprojectRemoteBranches { get; }

    public TranslatedText ShowSuperprojectTags { get; }

    public TranslatedText Sorting { get; }

    public TranslatedText ToggleBetweenArtificialAndHeadCommits { get; }

    public TranslatedText TopoOrder { get; }

    public TranslatedText QuickSearchQuickHelp { get; }

    public TranslatedText DrawNonrelativesGray { get; }

    public TranslatedText Filter { get; }

    public TranslatedText ShowAuthorAvatarColumn { get; }

    public TranslatedText ShowAuthorDate { get; }

    public TranslatedText ShowAuthorNameColumn { get; }

    public TranslatedText ShowBuildStatusIcon { get; }

    public TranslatedText ShowBuildStatusText { get; }

    public TranslatedText ShowCommitMessageBody { get; }

    public TranslatedText ShowDateColumn { get; }

    public TranslatedText ShowGitNotesColumn { get; }

    public TranslatedText ShowGitNotes { get; }

    public TranslatedText ShowIdColumn { get; }

    public TranslatedText ShowRelativeDate { get; }

    public TranslatedText ShowRevisionGraphColumn { get; }

    public TranslatedText ShowTags { get; }
}
