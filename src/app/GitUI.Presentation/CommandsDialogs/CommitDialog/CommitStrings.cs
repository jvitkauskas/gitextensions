using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs.CommitDialog;

/// <summary>Strings of the commit dialog; ids match <c>FormCommit</c>.</summary>
public sealed class CommitStrings : ViewStrings
{
    public CommitStrings()
        : base("FormCommit")
    {
        Title = Add("$this", "Text", "Commit");
        FormTitle = Add("_formTitle", "Text", "Commit to {0} ({1})");
        Amend = Add("Amend", "Text", "&Amend commit");
        Commit = Add("Commit", "Text", "&Commit");
        ResetAuthor = Add("ResetAuthor", "Text", "R&eset author");
        ResetSoft = Add("ResetSoft", "Text", "Reset so&ft");
        ResetSoftToolTip = Add("ResetSoft", "fileTooltip", "Perform a soft reset to the previous commit; leaves working directory and index untouched");
        ShowOnlyMyMessages = Add("ShowOnlyMyMessagesToolStripMenuItem", "Text", "Show only my messages");
        GenerateListOfChangesInSubmodules = Add("generateListOfChangesInSubmodulesChangesToolStripMenuItem", "Text", "Generate a list of changes in submodules");
        SolveMergeConflicts = Add("SolveMergeconflicts", "Text", "There are unresolved merge conflicts\n");
        StageInSuperproject = Add("StageInSuperproject", "Text", "Stage &in Superproject");
        StageInSuperprojectToolTip = Add("StageInSuperproject", "fileTooltip", "Stage current submodule in superproject after commit");
        StashStaged = Add("StashStaged", "Text", "Stas&h staged changes");
        AddSelectionToCommitMessage = Add("_addSelectionToCommitMessage", "Text", "Add selection to commit message");
        AmendCommit = Add("_amendCommit", "Text", "You are about to rewrite history.\nOnly use Amend if the commit has not been published yet!\n\nDo you want to continue?");
        AmendCommitCaption = Add("_amendCommitCaption", "Text", "Amend commit");
        AmendResetSoft = Add("_amendResetSoft", "Text", "You are about to rewrite history by Soft Reset to the previous commit.\nOnly use Amend / Reset if the commit has not been published yet!\n\nDo you want to continue?");
        CommitAndForcePush = Add("_commitAndForcePush", "Text", "Commit && force &push");
        CommitAndPush = Add("_commitAndPush", "Text", "Commit && &push");
        CommitAuthorInfo = Add("_commitAuthorInfo", "Text", "Author");
        CommitCommitterInfo = Add("_commitCommitterInfo", "Text", "Committer");
        CommitCommitterToolTip = Add("_commitCommitterToolTip", "Text", "Click to change committer information.");
        CommitMessageDisabled = Add("_commitMessageDisabled", "Text", "Commit Message is requested during commit");
        CommitMsgFirstLineInvalid = Add("_commitMsgFirstLineInvalid", "Text", "First line of commit message contains too many characters.\nDo you want to continue?");
        CommitMsgLineInvalid = Add("_commitMsgLineInvalid", "Text", "The following line of commit message contains too many characters:\n\n{0}\n\nDo you want to continue?");
        CommitMsgRegExNotMatched = Add("_commitMsgRegExNotMatched", "Text", "Commit message does not match RegEx.\nDo you want to continue?");
        CommitMsgSecondLineNotEmpty = Add("_commitMsgSecondLineNotEmpty", "Text", "Second line of commit message is not empty.\nDo you want to continue?");
        CommitValidationCaption = Add("_commitValidationCaption", "Text", "Commit validation");
        EnterCommitMessage = Add("_enterCommitMessage", "Text", "Please enter commit message");
        EnterCommitMessageCaption = Add("_enterCommitMessageCaption", "Text", "Commit message");
        EnterCommitMessageHint = Add("_enterCommitMessageHint", "Text", "Enter commit message");
        MergeConflicts = Add("_mergeConflicts", "Text", "There are unresolved merge conflicts, solve merge conflicts before committing.");
        MergeConflictsCaption = Add("_mergeConflictsCaption", "Text", "Merge conflicts");
        ModifyCommitMessageButtonToolTip = Add("_modifyCommitMessageButtonToolTip", "Text", "If you change the first line of the commit message, git will treat this commit as an ordinary commit,\ni.e. it may no longer be a fixup or an autosquash commit.");
        NoFilesStagedAndConfirmAnEmptyMergeCommit = Add("_noFilesStagedAndConfirmAnEmptyMergeCommit", "Text", "There are no files staged for this commit.\nAre you sure you want to commit?");
        NoFilesStagedCommitAllFilteredUnstagedOption = Add("_noFilesStagedCommitAllFilteredUnstagedOption", "Text", "Stage and commit the unstaged files that match your filter");
        NoFilesStagedCommitAllUnstagedOption = Add("_noFilesStagedCommitAllUnstagedOption", "Text", "Stage and commit all unstaged files");
        NoFilesStagedCommitCaption = Add("_noFilesStagedCommitCaption", "Text", "Confirm commit");
        NoFilesStagedCommitInstructions = Add("_noFilesStagedCommitInstructions", "Text", "There aren't any changes in the staging area.\nHow do you want to proceed?");
        NoFilesStagedMakeEmptyCommitOption = Add("_noFilesStagedMakeEmptyCommitOption", "Text", "Make an empty commit");
        NoStagedChanges = Add("_noStagedChanges", "Text", "There are no staged changes");
        NoUnstagedChanges = Add("_noUnstagedChanges", "Text", "There are no unstaged changes");
        NotOnBranch = Add("_notOnBranch", "Text", "This commit will be unreferenced when switching to another branch and can be lost.\n\nDo you want to continue?");
        StageAll = Add("_stageAll", "Text", "Stage all");
        StageDetails = Add("_stageDetails", "Text", "Stage Details");
        StageFiles = Add("_stageFiles", "Text", "Stage {0} files");
        StageFiltered = Add("_stageFiltered", "Text", "Stage filtered");
        SelectionFilter = Add("toolStripLabel1", "Text", "Selection Filter");
        SelectionFilterToolTip = Add("_selectionFilterToolTip", "Text", "Enter a regular expression to select unstaged files.");
        SelectionFilterErrorToolTip = Add("_selectionFilterErrorToolTip", "Text", "Error {0}");
        StatusBarBranchWithoutRemote = Add("_statusBarBranchWithoutRemote", "Text", "(remote not configured)");
        UnstageAll = Add("_unstageAll", "Text", "Unstage all");
        UnstageFiltered = Add("_unstageFiltered", "Text", "Unstage filtered");
        UntrackedRemote = Add("_untrackedRemote", "Text", "(untracked)");
        ResetAllChanges = Add("btnResetAllChanges", "Text", "&Reset all changes");
        ResetUnstagedChanges = Add("btnResetUnstagedChanges", "Text", "Reset u&nstaged changes");
        CloseDialogAfterAllFilesCommitted = Add("closeDialogAfterAllFilesCommittedToolStripMenuItem", "Text", "Close dialog when all changes are committed");
        CloseDialogAfterEachCommit = Add("closeDialogAfterEachCommitToolStripMenuItem", "Text", "Close dialog after each commit");
        CommitAuthorStatusToolTip = Add("commitAuthorStatus", "ToolTipText", "Click to change author information.");
        CursorColumn = Add("commitCursorColumnLabel", "Text", "Col");
        CursorLine = Add("commitCursorLineLabel", "Text", "Ln");
        CommitMessageMenu = Add("commitMessageToolStripMenuItem", "Text", "Commit &message");
        StagedCountLabel = Add("commitStagedCountLabel", "Text", "Staged");
        CreateBranch = Add("createBranchToolStripButton", "Text", "Create &branch");
        CreateBranchToolTip = Add("createBranchToolStripButton", "ToolTipText", "Create branch");
        ModifyCommitMessage = Add("modifyCommitMessageButton", "Text", "Modify the commit m&essage");
        NoVerify = Add("noVerifyToolStripMenuItem", "Text", "No verify");
        RefreshDialogOnFormFocus = Add("refreshDialogOnFormFocusToolStripMenuItem", "Text", "Refresh dialog on form focus");
        SignOff = Add("signOffToolStripMenuItem", "Text", "Sign-off commit");
        AuthorLabel = Add("toolAuthorLabelItem", "Text", "Author: (Format: \"name <mail>\")");
        Stage = Add("toolStageItem", "Text", "&Stage");
        Unstage = Add("toolUnstageItem", "Text", "&Unstage");
        Options = Add("tsmiOptions", "Text", "&Options");
        SelectStagedOnEnterMessage = Add("tsmiSelectStagedOnEnterMessage", "Text", "Select staged on entering message editor");
        TemplateNotFoundCaption = Add("_templateNotFoundCaption", "Text", "Template Error");
        TemplateNotFound = Add("_templateNotFound", "Text", $"Template not found: {{0}}.{Environment.NewLine}{Environment.NewLine}You can set your template:{Environment.NewLine}\t$ git config commit.template ./.git_commit_msg.txt{Environment.NewLine}You can unset the template:{Environment.NewLine}\t$ git config --unset commit.template");
        TemplateLoadErrorCaption = Add("_templateLoadErrorCaption", "Text", "Template could not be loaded");
        CommitTemplates = Add("commitTemplatesToolStripMenuItem", "Text", "Commit &templates");
        CommitTemplatesToolTip = Add("commitTemplatesToolStripMenuItem", "ToolTipText", "Commit templates");
        CommitMessageSettings = Add("_commitMessageSettings", "Text", "&Edit commit message templates and settings...");
        ConventionalCommit = Add("_conventionalCommit", "Text", "Conven&tional Commits");
        ConventionalCommitDocumentation = Add("_conventionalCommitDocumentation", "Text", "Documentation...");
        WordWrapCommitMessageBody = Add("_wordWrapCommitMessageBody", "Text", "&Word wrap (except subject line)");
    }

    public TranslatedText Title { get; }

    public TranslatedText FormTitle { get; }

    public TranslatedText Amend { get; }

    public TranslatedText Commit { get; }

    public TranslatedText ResetAuthor { get; }

    public TranslatedText ResetSoft { get; }

    public TranslatedText ResetSoftToolTip { get; }

    public TranslatedText ShowOnlyMyMessages { get; }

    public TranslatedText GenerateListOfChangesInSubmodules { get; }

    public TranslatedText SolveMergeConflicts { get; }

    public TranslatedText StageInSuperproject { get; }

    public TranslatedText StageInSuperprojectToolTip { get; }

    public TranslatedText StashStaged { get; }

    public TranslatedText AddSelectionToCommitMessage { get; }

    public TranslatedText AmendCommit { get; }

    public TranslatedText AmendCommitCaption { get; }

    public TranslatedText AmendResetSoft { get; }

    public TranslatedText CommitAndForcePush { get; }

    public TranslatedText CommitAndPush { get; }

    public TranslatedText CommitAuthorInfo { get; }

    public TranslatedText CommitCommitterInfo { get; }

    public TranslatedText CommitCommitterToolTip { get; }

    public TranslatedText CommitMessageDisabled { get; }

    public TranslatedText CommitMsgFirstLineInvalid { get; }

    public TranslatedText CommitMsgLineInvalid { get; }

    public TranslatedText CommitMsgRegExNotMatched { get; }

    public TranslatedText CommitMsgSecondLineNotEmpty { get; }

    public TranslatedText CommitValidationCaption { get; }

    public TranslatedText EnterCommitMessage { get; }

    public TranslatedText EnterCommitMessageCaption { get; }

    public TranslatedText EnterCommitMessageHint { get; }

    public TranslatedText MergeConflicts { get; }

    public TranslatedText MergeConflictsCaption { get; }

    public TranslatedText ModifyCommitMessageButtonToolTip { get; }

    public TranslatedText NoFilesStagedAndConfirmAnEmptyMergeCommit { get; }

    public TranslatedText NoFilesStagedCommitAllFilteredUnstagedOption { get; }

    public TranslatedText NoFilesStagedCommitAllUnstagedOption { get; }

    public TranslatedText NoFilesStagedCommitCaption { get; }

    public TranslatedText NoFilesStagedCommitInstructions { get; }

    public TranslatedText NoFilesStagedMakeEmptyCommitOption { get; }

    public TranslatedText NoStagedChanges { get; }

    public TranslatedText NoUnstagedChanges { get; }

    public TranslatedText NotOnBranch { get; }

    public TranslatedText StageAll { get; }

    public TranslatedText StageDetails { get; }

    public TranslatedText StageFiles { get; }

    public TranslatedText StageFiltered { get; }

    public TranslatedText SelectionFilter { get; }

    public TranslatedText SelectionFilterToolTip { get; }

    public TranslatedText SelectionFilterErrorToolTip { get; }

    public TranslatedText StatusBarBranchWithoutRemote { get; }

    public TranslatedText UnstageAll { get; }

    public TranslatedText UnstageFiltered { get; }

    public TranslatedText UntrackedRemote { get; }

    public TranslatedText ResetAllChanges { get; }

    public TranslatedText ResetUnstagedChanges { get; }

    public TranslatedText CloseDialogAfterAllFilesCommitted { get; }

    public TranslatedText CloseDialogAfterEachCommit { get; }

    public TranslatedText CommitAuthorStatusToolTip { get; }

    public TranslatedText CursorColumn { get; }

    public TranslatedText CursorLine { get; }

    public TranslatedText CommitMessageMenu { get; }

    public TranslatedText StagedCountLabel { get; }

    public TranslatedText CreateBranch { get; }

    public TranslatedText CreateBranchToolTip { get; }

    public TranslatedText ModifyCommitMessage { get; }

    public TranslatedText NoVerify { get; }

    public TranslatedText RefreshDialogOnFormFocus { get; }

    public TranslatedText SignOff { get; }

    public TranslatedText AuthorLabel { get; }

    public TranslatedText Stage { get; }

    public TranslatedText Unstage { get; }

    public TranslatedText Options { get; }

    public TranslatedText SelectStagedOnEnterMessage { get; }

    public TranslatedText TemplateNotFoundCaption { get; }

    public TranslatedText TemplateNotFound { get; }

    public TranslatedText TemplateLoadErrorCaption { get; }

    public TranslatedText CommitTemplates { get; }

    public TranslatedText CommitTemplatesToolTip { get; }

    public TranslatedText CommitMessageSettings { get; }

    public TranslatedText ConventionalCommit { get; }

    public TranslatedText ConventionalCommitDocumentation { get; }

    public TranslatedText WordWrapCommitMessageBody { get; }
}
