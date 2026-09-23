using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>The strings of the list of parents of a merge commit, shared by cherry-pick and revert.</summary>
public interface IParentCommitStrings
{
    TranslatedText SelectParent { get; }

    TranslatedText NumberColumn { get; }

    TranslatedText MessageColumn { get; }

    TranslatedText AuthorColumn { get; }

    TranslatedText DateColumn { get; }

    TranslatedText NoParentSelected { get; }
}

/// <summary>Strings of the cherry-pick dialog; ids match <c>FormCherryPick</c>.</summary>
public sealed class CherryPickStrings : ViewStrings, IParentCommitStrings
{
    public CherryPickStrings()
        : base("FormCherryPick")
    {
        Title = Add("$this", "Text", "Cherry pick commit");
        CherryPickThisCommit = Add("lblBranchInfo", "Text", "Cherry pick this commit:");
        ChooseAnotherRevision = Add("lblAnotherRev", "Text", "C&hoose another revision:");
        SelectParent = Add("lblParents", "Text", "This commit is a merge, select &parent:");
        NumberColumn = Add("columnHeader1", "Text", "No.");
        MessageColumn = Add("columnHeader2", "Text", "Message");
        AuthorColumn = Add("columnHeader3", "Text", "Author");
        DateColumn = Add("columnHeader4", "Text", "Date");
        AutoCommit = Add("cbxAutoCommit", "Text", "&Automatically create a commit");
        AddReference = Add("cbxAddReference", "Text", "A&dd commit reference to commit message");
        CherryPick = Add("btnPick", "Text", "&Cherry pick");
        Abort = Add("btnAbort", "Text", "A&bort");
        NoParentSelected = Add("_noneParentSelectedText", "Text", "None parent is selected!");
    }

    public TranslatedText Title { get; }

    public TranslatedText CherryPickThisCommit { get; }

    public TranslatedText ChooseAnotherRevision { get; }

    public TranslatedText SelectParent { get; }

    public TranslatedText NumberColumn { get; }

    public TranslatedText MessageColumn { get; }

    public TranslatedText AuthorColumn { get; }

    public TranslatedText DateColumn { get; }

    public TranslatedText AutoCommit { get; }

    public TranslatedText AddReference { get; }

    public TranslatedText CherryPick { get; }

    public TranslatedText Abort { get; }

    public TranslatedText NoParentSelected { get; }
}

/// <summary>Strings of the revert dialog; ids match <c>FormRevertCommit</c>.</summary>
public sealed class RevertCommitStrings : ViewStrings, IParentCommitStrings
{
    public RevertCommitStrings()
        : base("FormRevertCommit")
    {
        Title = Add("$this", "Text", "Revert commit");
        RevertThisCommit = Add("BranchInfo", "Text", "Revert this commit:");
        SelectParent = Add("ParentsLabel", "Text", "This commit is a merge, select &parent:");
        NumberColumn = Add("columnHeader1", "Text", "No.");
        MessageColumn = Add("columnHeader2", "Text", "Message");
        AuthorColumn = Add("columnHeader3", "Text", "Author");
        DateColumn = Add("columnHeader4", "Text", "Date");
        AutoCommit = Add("AutoCommit", "Text", "&Automatically create a commit");
        Revert = Add("Revert", "Text", "&Revert this commit");
        Abort = Add("btnAbort", "Text", "A&bort");
        NoParentSelected = Add("_noneParentSelectedText", "Text", "None parent is selected!");
    }

    public TranslatedText Title { get; }

    public TranslatedText RevertThisCommit { get; }

    public TranslatedText SelectParent { get; }

    public TranslatedText NumberColumn { get; }

    public TranslatedText MessageColumn { get; }

    public TranslatedText AuthorColumn { get; }

    public TranslatedText DateColumn { get; }

    public TranslatedText AutoCommit { get; }

    public TranslatedText Revert { get; }

    public TranslatedText Abort { get; }

    public TranslatedText NoParentSelected { get; }
}

/// <summary>Strings of the reset current branch dialog; ids match <c>FormResetCurrentBranch</c>.</summary>
public sealed class ResetCurrentBranchStrings : ViewStrings
{
    public ResetCurrentBranchStrings()
        : base("FormResetCurrentBranch")
    {
        Title = Add("$this", "Text", "Reset current branch");
        BranchInfo = Add("_branchInfo", "Text", "Reset branch '{0}' to revision:");
        Soft = Add("Soft", "Text", "&Soft: leave working directory and index untouched");
        Mixed = Add("Mixed", "Text", "Mi&xed: leave working directory untouched, reset index");
        Keep = Add("Keep", "Text", "&Keep: update working directory to the commit \r\n(abort if there are local changes), reset index");
        Merge = Add("Merge", "Text", "&Merge: update working directory to the commit and keep local changes \r\n(abort if there are conflicts), reset index");
        Hard = Add("Hard", "Text", "&Hard: reset working directory and index\r\n(discard ALL local changes, even uncommitted changes)");
        Ok = Add("Ok", "Text", "OK");
        Cancel = Add("Cancel", "Text", "Cancel");
        ResetHardWarning = Add("_resetHardWarning", "Text", "You are about to discard ALL local changes, are you sure?");
        ResetCaption = Add("_resetCaption", "Text", "Reset branch");
    }

    public TranslatedText Title { get; }

    public TranslatedText BranchInfo { get; }

    /// <summary>The caption of the reset type group, which the WinForms form does not translate.</summary>
    public string ResetType => "Reset type";

    public TranslatedText Soft { get; }

    public TranslatedText Mixed { get; }

    public TranslatedText Keep { get; }

    public TranslatedText Merge { get; }

    public TranslatedText Hard { get; }

    public TranslatedText Ok { get; }

    public TranslatedText Cancel { get; }

    public TranslatedText ResetHardWarning { get; }

    public TranslatedText ResetCaption { get; }
}

/// <summary>Strings of the reset another branch dialog; ids match <c>FormResetAnotherBranch</c>.</summary>
public sealed class ResetAnotherBranchStrings : ViewStrings
{
    public ResetAnotherBranchStrings()
        : base("FormResetAnotherBranch")
    {
        Title = Add("$this", "Text", "Reset branch");
        Warning = Add(
            "lblResetBranchWarning",
            "Text",
            "You can only reset a branch safely if there is a direct path from it to selected revision.\r\nForcing a branch to reset if it has not been merged might leave some commits unreachable.");
        ResetLocalBranch = Add("BranchInfo", "Text", "Reset local &branch:");
        CheckoutAfterReset = Add("cbxCheckoutBranch", "Text", "Chec&kout branch after reset");
        ForceReset = Add("ForceReset", "Text", "&Force reset for a non-fast-forward reset");
        Ok = Add("Ok", "Text", "OK");
        Cancel = Add("Cancel", "Text", "Cancel");
        LocalRefInvalid = Add("_localRefInvalid", "Text", "The entered value '{0}' is not the name of an existing local branch.");
    }

    public TranslatedText Title { get; }

    public TranslatedText Warning { get; }

    public TranslatedText ResetLocalBranch { get; }

    public TranslatedText CheckoutAfterReset { get; }

    public TranslatedText ForceReset { get; }

    public TranslatedText Ok { get; }

    public TranslatedText Cancel { get; }

    public TranslatedText LocalRefInvalid { get; }
}

/// <summary>Strings of the archive dialog; ids match <c>FormArchive</c>.</summary>
public sealed class ArchiveStrings : ViewStrings
{
    public ArchiveStrings()
        : base("FormArchive")
    {
        Title = Add("$this", "Text", "Archive");
        RevisionToArchive = Add("label1", "Text", "This revision will be archived:");
        ChooseAnotherRevision = Add("label2", "Text", "Choose another\r\nrevision:");
        FilterFiles = Add("groupBox2", "Text", "Filter files");
        PathFilter = Add("checkBoxPathFilter", "Text", "Archive specific paths only");
        PathFilterHint = Add("label4", "Text", "separate each new path by new line");
        RevisionFilter = Add("checkboxRevisionFilter", "Text", "Take the files that have changed from the revision above to this one and archive only those");
        ChooseDiffRevision = Add("lblChooseDiffRevision", "Text", "Choose revision to \r\ncompare with first:");
        ArchiveFormat = Add("groupBox1", "Text", "Archive format");
        SaveAs = Add("buttonArchiveRevision", "Text", "Save as...");
        SaveFileDialogCaption = Add("_saveFileDialogCaption", "Text", "Save archive as");
        SaveFileDialogFilterZip = Add("_saveFileDialogFilterZip", "Text", "Zip file (*.zip)");
        SaveFileDialogFilterTar = Add("_saveFileDialogFilterTar", "Text", "Tar file (*.tar)");
        NoRevisionSelected = Add("_noRevisionSelected", "Text", "You need to choose a target revision.");
    }

    public TranslatedText Title { get; }

    public TranslatedText RevisionToArchive { get; }

    public TranslatedText ChooseAnotherRevision { get; }

    public TranslatedText FilterFiles { get; }

    public TranslatedText PathFilter { get; }

    public TranslatedText PathFilterHint { get; }

    public TranslatedText RevisionFilter { get; }

    public TranslatedText ChooseDiffRevision { get; }

    public TranslatedText ArchiveFormat { get; }

    public TranslatedText SaveAs { get; }

    public TranslatedText SaveFileDialogCaption { get; }

    public TranslatedText SaveFileDialogFilterZip { get; }

    public TranslatedText SaveFileDialogFilterTar { get; }

    public TranslatedText NoRevisionSelected { get; }
}
