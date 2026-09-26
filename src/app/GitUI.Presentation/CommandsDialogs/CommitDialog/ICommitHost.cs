using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUIPluginInterfaces;

namespace GitUI.Presentation.CommandsDialogs.CommitDialog;

/// <summary>The kind of commit being prepared (the WinForms <c>CommitKind</c>).</summary>
public enum CommitDialogKind
{
    Normal,
    Fixup,
    Squash,
    Amend,
}

/// <summary>How to proceed without staged files (the choices of <c>ConfirmAndStageAllUnstaged</c>).</summary>
public enum NoStagedFilesChoice
{
    Cancel,
    StageAllAndCommit,
    EmptyCommit,
}

/// <summary>The settings of the commit dialog (the <c>AppSettings</c> it reads).</summary>
public sealed record CommitDialogOptions
{
    public bool UseFormCommitMessage { get; init; } = true;

    public int MaxFirstLineLength { get; init; }

    public int MaxLineLength { get; init; }

    public bool SecondLineMustBeEmpty { get; init; }

    public bool AutoWrap { get; init; }

    public bool IndentAfterFirstLine { get; init; }

    public string ValidationRegex { get; init; } = "";

    public bool DontConfirmCommitIfNoBranch { get; init; }

    public bool CommitAndPushForcedWhenAmend { get; init; }

    public bool ShowCommitAndPush { get; init; } = true;

    public bool ShowResetAllChanges { get; init; } = true;

    public bool ShowResetWorkTreeChanges { get; init; } = true;

    public bool SupportStashStaged { get; init; } = true;

    /// <summary>As <c>AppSettings.CommitDialogSelectionFilter</c>: the selection filter is shown.</summary>
    public bool ShowSelectionFilter { get; init; }

    public bool DontConfirmAmend { get; init; }

    public int NumberOfPreviousMessages { get; init; } = 6;
}

/// <summary>The settings the commit dialog changes (its options menu), saved when changed.</summary>
public interface ICommitDialogSettings
{
    bool CloseDialogAfterEachCommit { get; set; }

    bool CloseDialogAfterAllFilesCommitted { get; set; }

    bool RefreshDialogOnFormFocus { get; set; }

    bool SelectStagedOnEnterMessage { get; set; }

    bool ShowOnlyMyMessages { get; set; }

    bool StageInSuperproject { get; set; }
}

/// <summary>What <c>git commit</c> is run with (the arguments of <c>Commands.Commit</c>).</summary>
public sealed record CommitRequest(
    string Message,
    bool Amend,
    bool SignOff,
    string Author,
    bool NoVerify,
    bool AllowEmpty,
    bool ResetAuthor,
    bool UsingCommitTemplate,
    bool? GpgSign = null,
    string GpgKeyId = "");

/// <summary>The current branch and where it is pushed to (the status bar of <c>FormCommit</c>).</summary>
public sealed record CommitBranchInfo(string Branch, string? PushTo);

/// <summary>Operations of the commit dialog that need the host (git, dialogs, scripts).</summary>
public interface ICommitHost
{
    CommitDialogOptions Options { get; }

    ICommitDialogSettings Settings { get; }

    /// <summary>The working directory as shown in the title (<c>PathUtil.GetDisplayPath</c>).</summary>
    string WorkingDirectory { get; }

    /// <summary>The text of the push button (<c>TranslatedStrings.ButtonPush</c>).</summary>
    string PushText { get; }

    bool IsBareRepository { get; }

    /// <summary>Whether the repository is a submodule (whose superproject can stage it after the commit).</summary>
    bool HasSuperproject { get; }

    /// <summary>Whether a merge is being committed (<c>MERGE_HEAD</c> exists).</summary>
    bool IsMergeCommit { get; }

    bool InTheMiddleOfConflictedMerge();

    /// <summary>Whether HEAD has a parent to reset to (<c>RevParse("HEAD~1")</c>).</summary>
    bool CanResetSoft();

    /// <summary>The changed files of the working directory and the index (<c>GetAllChangedFilesWithSubmodulesStatus</c>).</summary>
    /// <param name="options">The files the settings of the unstaged list show (as the items of <c>Unstaged</c> in <c>ComputeUnstagedFiles</c>).</param>
    Task<IReadOnlyList<GitItemStatus>> GetAllChangedFilesAsync(FileStatusFileOptions options, CancellationToken cancellationToken);

    /// <summary>The files of the index (<c>GetIndexFilesWithSubmodulesStatus</c>).</summary>
    IReadOnlyList<GitItemStatus> GetIndexFiles();

    /// <summary>HEAD (if any), the index and the working directory as revisions (<c>GetHeadRevisions</c>).</summary>
    (GitRevision? Head, GitRevision Index, GitRevision WorkTree) GetHeadRevisions();

    /// <summary>Updates the status of the submodules (<c>GetSubmoduleCurrentStatus</c>).</summary>
    void UpdateSubmoduleStatus(IReadOnlyList<GitItemStatus> items);

    Task<CommitBranchInfo> GetBranchInfoAsync();

    /// <summary>The committer (and author) as the status bar shows them (<c>UpdateAuthorInfo</c>).</summary>
    Task<string> GetCommitterAsync(string author);

    /// <summary>Stages the files; <see langword="false"/> if git reported errors (which the host shows).</summary>
    bool StageFiles(IReadOnlyList<GitItemStatus> files);

    /// <summary>Unstages the files (<c>BatchUnstageFiles</c>); returns whether the changes must be rescanned.</summary>
    bool UnstageFiles(IReadOnlyList<GitItemStatus> files);

    /// <summary>Unstages everything (<c>git reset --mixed</c>).</summary>
    void UnstageAll();

    /// <summary>The stored commit message and amend state (<c>GetMergeOrCommitMessageAsync</c>, <c>GetAmendStateAsync</c>).</summary>
    Task<(string Message, bool Amend)> LoadCommitMessageAsync();

    /// <summary>The message of the commit template, if configured; errors are shown (<c>AssignCommitMessageFromTemplate</c>).</summary>
    string? LoadCommitTemplate();

    /// <summary>Stores the message when the dialog closes (<c>SetMergeOrCommitMessageAsync</c>, <c>SetAmendStateAsync</c>).</summary>
    Task SaveCommitMessageAsync(string message, bool amend);

    /// <summary>The previous commit messages, the last one first (as <c>CommitMessageToolStripMenuItemDropDownOpening</c>).</summary>
    IReadOnlyList<string> GetPreviousMessages(bool onlyMine);

    /// <summary>The message of HEAD, which an amend without message starts with.</summary>
    string? GetHeadMessage();

    /// <summary>The templates of the plugins (<c>RegisteredTemplates</c>) and of the settings (<c>LoadFromSettings</c>).</summary>
    (IReadOnlyList<CommitTemplateItem> Registered, IReadOnlyList<CommitTemplateItem> FromSettings) GetCommitTemplates();

    /// <summary>The icon of a template (the <c>Icon</c> of a template of a plugin) as PNG, if it has one.</summary>
    byte[]? GetTemplateIcon(CommitTemplateItem template);

    /// <summary>
    ///  As <c>generateListOfChangesInSubmodulesChangesToolStripMenuItem_Click</c>: a message with the commits of the staged
    ///  submodules, <see langword="null"/> if none changed (or the configuration of the submodules is invalid, which is shown).
    /// </summary>
    string? GetListOfChangesInSubmodules(IReadOnlyList<GitItemStatus> stagedFiles);

    /// <summary>The checked out branch, which the regular expressions of templates match (<c>GetSelectedBranch</c>).</summary>
    string GetCurrentBranch();

    /// <summary>As the settings item of the templates menu (<c>FormCommitTemplateSettings</c>).</summary>
    void EditCommitTemplateSettings();

    void OpenUrl(string url);

    bool ConfirmAmend();

    bool ConfirmEmptyMergeCommit();

    NoStagedFilesChoice AskWithoutStagedFiles(bool filterActive, bool hasUnstagedFiles);

    void ShowMergeConflicts();

    void ShowEnterCommitMessage();

    /// <summary>A validation question (Yes/No) of the commit message.</summary>
    bool ConfirmValidation(string text);

    /// <summary>
    ///  As the question on a detached HEAD: continue, check out a branch or create one (which the host does);
    ///  <see langword="false"/> to cancel.
    /// </summary>
    bool ConfirmDetachedHeadCommit(ObjectId? editedCommit);

    /// <summary>Writes the message, runs the scripts and <c>git commit</c>; <see langword="false"/> if it failed.</summary>
    bool Commit(CommitRequest request);

    /// <summary>Pushes the branch (<c>StartPushDialog</c> on show); returns whether the push completed.</summary>
    bool Push(bool forced);

    /// <summary>Stages this submodule in its superproject.</summary>
    void StageInSuperprojectNow();

    bool ConfirmResetSoft();

    void ResetSoft();

    bool ResolveConflicts();

    void ResetChanges(IReadOnlyList<GitItemStatus> unstagedFiles, bool onlyWorkTree);

    void StashStaged();

    bool CreateBranch();

    void EditCommitterSettings();

    /// <summary>As the click of <c>remoteNameLabel</c>: the remotes dialog, for the pull settings of <paramref name="branch"/>.</summary>
    void EditRemotes(string branch)
    {
    }

    void NotifyRepositoryChanged();

    void ShowError(string message);
}
