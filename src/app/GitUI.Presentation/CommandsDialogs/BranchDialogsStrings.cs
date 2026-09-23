using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the delete branch dialog; ids match <c>FormDeleteBranch</c>.</summary>
public sealed class DeleteBranchStrings : ViewStrings
{
    public DeleteBranchStrings()
        : base("FormDeleteBranch")
    {
        Title = Add("$this", "Text", "Delete branch");
        SelectBranches = Add("labelSelectBranches", "Text", "Select &branches");
        Delete = Add("Delete", "Text", "&Delete");
        DeleteBranchCaption = Add("_deleteBranchCaption", "Text", "Delete Branches");
        CannotDeleteCurrentBranch = Add("_cannotDeleteCurrentBranchMessage", "Text", "Cannot delete the branch “{0}” which you are currently on.");
        DeleteBranchConfirmTitle = Add("_deleteBranchConfirmTitle", "Text", "Delete Confirmation");
        DeleteBranchQuestion = Add("_deleteBranchQuestion", "Text", "The selected branch(es) have not been merged into HEAD.\r\nProceed?");
        UseReflogHint = Add("_useReflogHint", "Text", "Did you know you can use reflog to restore deleted branches?");
        BranchUsedByWorktreeQuestion = Add(
            "_branchUsedByWorktreeQuestion",
            "Text",
            "The following branches are checked out in worktrees and cannot be deleted directly:\n\n{0}\n\nDo you want to delete the worktrees and branches together?");
        CannotDeleteBranchInMainWorktree = Add("_cannotDeleteBranchInMainWorktree", "Text", "The branch “{0}” cannot be deleted because it is checked out in the main worktree at:\n{1}");
    }

    public TranslatedText Title { get; }

    public TranslatedText SelectBranches { get; }

    public TranslatedText Delete { get; }

    public TranslatedText DeleteBranchCaption { get; }

    public TranslatedText CannotDeleteCurrentBranch { get; }

    public TranslatedText DeleteBranchConfirmTitle { get; }

    public TranslatedText DeleteBranchQuestion { get; }

    public TranslatedText UseReflogHint { get; }

    public TranslatedText BranchUsedByWorktreeQuestion { get; }

    public TranslatedText CannotDeleteBranchInMainWorktree { get; }
}

/// <summary>Strings of the delete remote branch dialog; ids match <c>FormDeleteRemoteBranch</c>.</summary>
public sealed class DeleteRemoteBranchStrings : ViewStrings
{
    public DeleteRemoteBranchStrings()
        : base("FormDeleteRemoteBranch")
    {
        Title = Add("$this", "Text", "Delete branch");
        SelectBranches = Add("labelSelectBranches", "Text", "Select &branches");
        DeleteRemote = Add("DeleteRemote", "Text", "Delete branch(es) from &remote repository");
        DeleteLocalTrackingBranch = Add("DeleteLocalTrackingBranch", "Text", "Delete &local tracking branch (if available)");
        Delete = Add("Delete", "Text", "&Delete");
        DeleteRemoteBranchesCaption = Add("_deleteRemoteBranchesCaption", "Text", "Delete remote branches");
        ConfirmDeleteUnmerged = Add(
            "_confirmDeleteUnmergedRemoteBranchMessage",
            "Text",
            "At least one remote branch is unmerged. Are you sure you want to delete it?" + Environment.NewLine + "Deleting a branch can cause commits to be deleted too!");
        ToDeleteCandidates = Add("_toDeleteCandidates", "Text", "Local tracking branche(s) candidate to deletion:");
        AndMore = Add("_andMore", "Text", "and {0} more...");
    }

    public TranslatedText Title { get; }

    public TranslatedText SelectBranches { get; }

    public TranslatedText DeleteRemote { get; }

    public TranslatedText DeleteLocalTrackingBranch { get; }

    public TranslatedText Delete { get; }

    public TranslatedText DeleteRemoteBranchesCaption { get; }

    public TranslatedText ConfirmDeleteUnmerged { get; }

    public TranslatedText ToDeleteCandidates { get; }

    public TranslatedText AndMore { get; }
}

/// <summary>Strings of the merge branch dialog; ids match <c>FormMergeBranch</c>.</summary>
public sealed class MergeBranchStrings : ViewStrings
{
    public MergeBranchStrings()
        : base("FormMergeBranch")
    {
        Title = Add("$this", "Text", "Merge branches");
        MergeBranch = Add("label2", "Text", "Merge branch");
        IntoCurrentBranch = Add("Currentbranch", "Text", "Into current branch");
        MergeGroup = Add("groupBox1", "Text", "Merge");
        FastForward = Add("fastForward", "Text", "Keep a single branch line if possible (fast forward)");
        NoFastForward = Add("noFastForward", "Text", "Always create a new merge commit");
        NoCommit = Add("noCommit", "Text", "Do not commit");
        ShowAdvanced = Add("advanced", "Text", "Show advanced options");
        Squash = Add("squash", "Text", "Squash commits");
        AllowUnrelatedHistories = Add("allowUnrelatedHistories", "Text", "Allow unrelated histories");
        NonDefaultMergeStrategy = Add("NonDefaultMergeStrategy", "Text", "Use non-default merge strategy");
        StrategyHelp = Add("strategyHelp", "Text", "Help");
        AddLogMessages = Add("addLogMessages", "Text", "Add log messages");
        AddMergeMessage = Add("addMergeMessage", "Text", "Specify merge message");
        Merge = Add("Ok", "Text", "&Merge");
        HoverShowImageText = Add("_formMergeBranchHoverShowImageLabelText", "Text", "Hover to see scenario when fast forward is possible.");
    }

    public TranslatedText Title { get; }

    public TranslatedText MergeBranch { get; }

    public TranslatedText IntoCurrentBranch { get; }

    public TranslatedText MergeGroup { get; }

    public TranslatedText FastForward { get; }

    public TranslatedText NoFastForward { get; }

    public TranslatedText NoCommit { get; }

    public TranslatedText ShowAdvanced { get; }

    public TranslatedText Squash { get; }

    public TranslatedText AllowUnrelatedHistories { get; }

    public TranslatedText NonDefaultMergeStrategy { get; }

    public TranslatedText StrategyHelp { get; }

    public TranslatedText AddLogMessages { get; }

    public TranslatedText AddMergeMessage { get; }

    public TranslatedText Merge { get; }

    public TranslatedText HoverShowImageText { get; }
}
