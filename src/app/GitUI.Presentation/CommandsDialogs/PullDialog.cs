using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands.Config;
using GitCommands.Git;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the pull dialog; ids match <c>FormPull</c> and its <c>FolderBrowserButton</c>.</summary>
public sealed class PullStrings : ViewStrings
{
    public PullStrings()
        : base("FormPull")
    {
        GroupPullFrom = Add("GroupPullFrom", "Text", "Pull from");
        PullFromRemote = Add("PullFromRemote", "Text", "&Remote");
        PullFromRemoteToolTip = Add("PullFromRemote", "Tooltip", "Remote repository to pull from");
        AddRemote = Add("AddRemote", "Text", "Mana&ge remotes");
        PullFromUrl = Add("PullFromUrl", "Text", "&URL");
        PullFromUrlToolTip = Add("PullFromUrl", "Tooltip", "Url to pull from");
        Browse = Add("buttonBrowse", "Text", "&Browse...", category: "FolderBrowserButton");
        GroupBranch = Add("GroupBranch", "Text", "Branch");
        LocalBranch = Add("lblLocalBranch", "Text", "&Local branch");
        LocalBranchToolTip = Add("lblLocalBranch", "Tooltip", "Local branch to create or reset to the remote branch selected.");
        RemoteBranch = Add("lblRemoteBranch", "Text", "Rem&ote branch");
        RemoteBranchToolTip = Add("lblRemoteBranch", "Tooltip", "Remote branch to pull. Leave empty to pull all branches.");
        GroupMergeOptions = Add("GroupMergeOptions", "Text", "Merge options");
        Merge = Add("Merge", "Text", "&Merge remote branch into current branch");
        Rebase = Add("Rebase", "Text", "R&ebase current branch on top of remote branch, creates linear history (use with caution)");
        Fetch = Add("Fetch", "Text", "Do not merge, only &fetch remote changes");
        GroupTagOptions = Add("GroupTagOptions", "Text", "Tag options");
        ReachableTags = Add("ReachableTags", "Text", "Follow &tagopt, if not specified, fetch tags reachable from remote HEAD");
        NoTags = Add("NoTags", "Text", "Fetch &no tag");
        AllTags = Add("AllTags", "Text", "Fetch &all tags");
        Unshallow = Add("Unshallow", "Text", "Do&wnload full history");
        Prune = Add("Prune", "Text", "&Prune remote branches");
        PruneToolTip = Add(
            "Prune",
            "Tooltip",
            "Removes remote tracking branches that no longer exist on the remote (e.g. if someone else deleted them).\n\nActual command line (if checked): --prune --force\n");
        PruneTags = Add("PruneTags", "Text", "Prune remote branches an&d tags");
        PruneTagsToolTip = Add("PruneTags", "Tooltip", "Before fetching, remove any local tags that no longer exist on the remote if --prune is enabled.");
        Mergetool = Add("Mergetool", "Text", "&Solve conflicts");
        Stash = Add("Stash", "Text", "Stash &changes");
        AutoStash = Add("AutoStash", "Text", "Auto stas&h");

        AreYouSureYouWantToRebaseMerge = Add("_areYouSureYouWantToRebaseMerge", "Text", "The current commit is a merge.\nAre you sure you want to rebase this merge?");
        AreYouSureYouWantToRebaseMergeCaption = Add("_areYouSureYouWantToRebaseMergeCaption", "Text", "Rebase merge commit?");
        AllMergeConflictSolvedQuestion = Add("_allMergeConflictSolvedQuestion", "Text", "Are all merge conflicts solved? Do you want to commit?");
        AllMergeConflictSolvedQuestionCaption = Add("_allMergeConflictSolvedQuestionCaption", "Text", "Conflicts solved");
        ApplyStashedItemsAgain = Add("_applyStashedItemsAgain", "Text", "Apply stashed items to working directory again?");
        ApplyStashedItemsAgainCaption = Add("_applyStashedItemsAgainCaption", "Text", "Auto stash");
        FetchAllBranchesCanOnlyWithFetch = Add(
            "_fetchAllBranchesCanOnlyWithFetch",
            "Text",
            "You can only fetch all remote branches (*) without merge or rebase.\nIf you want to fetch all remote branches, choose fetch.\nIf you want to fetch and merge a branch, choose a specific branch.");
        SelectRemoteRepository = Add("_selectRemoteRepository", "Text", "Please select a remote repository");
        SelectSourceDirectory = Add("_selectSourceDirectory", "Text", "Please select a source directory");
        QuestionInitSubmodules = Add(
            "_questionInitSubmodules",
            "Text",
            "The pulled has submodules configured.\nDo you want to initialize the submodules?\nThis will initialize and update all submodules recursive.");
        QuestionInitSubmodulesCaption = Add("_questionInitSubmodulesCaption", "Text", "Submodules");
        NotOnBranch = Add("_notOnBranch", "Text", "You cannot \"pull\" when git head detached.\n\nDo you want to continue?");
        NoRemoteBranch = Add("_noRemoteBranch", "Text", "You didn't specify a remote branch");
        NoRemoteBranchMainInstruction = Add(
            "_noRemoteBranchMainInstruction",
            "Text",
            "You asked to pull from the remote '{0}',\nbut did not specify a remote branch.\nBecause this is not the default configured remote for your local branch,\nyou must specify a remote branch.");
        NoRemoteBranchForFetchMainInstruction = Add(
            "_noRemoteBranchForFetchMainInstruction",
            "Text",
            "You asked to fetch from the remote '{0}',\nbut did not specify a remote branch.\nBecause this is not the current branch, you must specify a remote branch.");
        NoRemoteBranchButton = Add("_noRemoteBranchButton", "Text", "Pull from {0}");
        NoRemoteBranchForFetchButton = Add("_noRemoteBranchForFetchButton", "Text", "Fetch from {0}");
        NoRemoteBranchCaption = Add("_noRemoteBranchCaption", "Text", "Remote branch not specified");
        PruneBranchesCaption = Add("_pruneBranchesCaption", "Text", "Pull was rejected");
        PruneBranchesMainInstruction = Add("_pruneBranchesMainInstruction", "Text", "Remote branch no longer exist");
        PruneBranchesBranch = Add("_pruneBranchesBranch", "Text", "Do you want to delete all stale remote-tracking branches?");
        PruneFromCaption = Add("_pruneFromCaption", "Text", "Prune remote branches from {0}");
        HoverShowImageLabelText = Add("_hoverShowImageLabelText", "Text", "Hover to see scenario when fast forward is possible.");
        FormTitlePull = Add("_formTitlePull", "Text", "Pull ({0})");
        FormTitleFetch = Add("_formTitleFetch", "Text", "Fetch ({0})");
        ButtonPull = Add("_buttonPull", "Text", "&Pull");
        ButtonFetch = Add("_buttonFetch", "Text", "&Fetch");
        PullFetchPruneAllConfirmation = Add(
            "_pullFetchPruneAllConfirmation",
            "Text",
            "Warning! The fetch with prune will remove all the remote-tracking references which no longer exist on remotes. Do you want to proceed?");
    }

    public TranslatedText GroupPullFrom { get; }

    public TranslatedText PullFromRemote { get; }

    public TranslatedText PullFromRemoteToolTip { get; }

    public TranslatedText AddRemote { get; }

    public TranslatedText PullFromUrl { get; }

    public TranslatedText PullFromUrlToolTip { get; }

    public TranslatedText Browse { get; }

    public TranslatedText GroupBranch { get; }

    public TranslatedText LocalBranch { get; }

    public TranslatedText LocalBranchToolTip { get; }

    public TranslatedText RemoteBranch { get; }

    public TranslatedText RemoteBranchToolTip { get; }

    public TranslatedText GroupMergeOptions { get; }

    public TranslatedText Merge { get; }

    public TranslatedText Rebase { get; }

    public TranslatedText Fetch { get; }

    public TranslatedText GroupTagOptions { get; }

    public TranslatedText ReachableTags { get; }

    public TranslatedText NoTags { get; }

    public TranslatedText AllTags { get; }

    public TranslatedText Unshallow { get; }

    public TranslatedText Prune { get; }

    public TranslatedText PruneToolTip { get; }

    public TranslatedText PruneTags { get; }

    public TranslatedText PruneTagsToolTip { get; }

    public TranslatedText Mergetool { get; }

    public TranslatedText Stash { get; }

    public TranslatedText AutoStash { get; }

    public TranslatedText AreYouSureYouWantToRebaseMerge { get; }

    public TranslatedText AreYouSureYouWantToRebaseMergeCaption { get; }

    public TranslatedText AllMergeConflictSolvedQuestion { get; }

    public TranslatedText AllMergeConflictSolvedQuestionCaption { get; }

    public TranslatedText ApplyStashedItemsAgain { get; }

    public TranslatedText ApplyStashedItemsAgainCaption { get; }

    public TranslatedText FetchAllBranchesCanOnlyWithFetch { get; }

    public TranslatedText SelectRemoteRepository { get; }

    public TranslatedText SelectSourceDirectory { get; }

    public TranslatedText QuestionInitSubmodules { get; }

    public TranslatedText QuestionInitSubmodulesCaption { get; }

    public TranslatedText NotOnBranch { get; }

    public TranslatedText NoRemoteBranch { get; }

    public TranslatedText NoRemoteBranchMainInstruction { get; }

    public TranslatedText NoRemoteBranchForFetchMainInstruction { get; }

    public TranslatedText NoRemoteBranchButton { get; }

    public TranslatedText NoRemoteBranchForFetchButton { get; }

    public TranslatedText NoRemoteBranchCaption { get; }

    public TranslatedText PruneBranchesCaption { get; }

    public TranslatedText PruneBranchesMainInstruction { get; }

    public TranslatedText PruneBranchesBranch { get; }

    public TranslatedText PruneFromCaption { get; }

    public TranslatedText HoverShowImageLabelText { get; }

    public TranslatedText FormTitlePull { get; }

    public TranslatedText FormTitleFetch { get; }

    public TranslatedText ButtonPull { get; }

    public TranslatedText ButtonFetch { get; }

    public TranslatedText PullFetchPruneAllConfirmation { get; }
}

/// <summary>How a pull ended (the dialog results of <c>FormPull.PullChanges</c>).</summary>
public enum PullOutcome
{
    /// <summary>Git ran, successfully or not (<c>DialogResult.OK</c>).</summary>
    Pulled,

    /// <summary>Not pulled, the dialog stays open or is shown (<c>DialogResult.No</c>).</summary>
    NotPulled,

    /// <summary>Cancelled by the user (<c>DialogResult.Cancel</c>).</summary>
    Cancelled,
}

/// <summary>The merge options of the pull dialog (the <c>Merge</c>, <c>Rebase</c> and <c>Fetch</c> radio buttons).</summary>
public enum PullMergeAction
{
    Merge,
    Rebase,
    Fetch,
}

/// <summary>The tag options of the pull dialog (the <c>ReachableTags</c>, <c>NoTags</c> and <c>AllTags</c> radio buttons).</summary>
public enum PullTagsOption
{
    Reachable,
    None,
    All,
}

/// <summary>The answer to the question of <c>FormPull.PullChanges</c> on a detached HEAD.</summary>
public enum DetachedHeadPullChoice
{
    Cancel,
    CheckoutBranch,
    Continue,
}

/// <summary>A branch offered as the remote branch (<c>IGitRef.Name</c> and <c>IGitRef.LocalName</c>).</summary>
public sealed record PullRef(string Name, string LocalName);

/// <summary>The arguments of the git command of <c>FormPull.CreateFormProcess</c>.</summary>
/// <param name="Fetch">Whether to fetch only (<c>FetchCmd</c>), otherwise pull (<c>PullCmd</c>).</param>
/// <param name="Source">The remote, the URL or <c>--all</c>.</param>
/// <param name="FetchTags"><see langword="true"/> to fetch all tags, <see langword="false"/> no tag, <see langword="null"/> to follow tagopt.</param>
/// <param name="IsPullAll">Whether all remotes are pulled (the process then has no <c>Remote</c>).</param>
/// <param name="PruneRemote">The remote to offer pruning when git reports a removed ref (<c>HandlePullOnExit</c>), if pulling from a remote.</param>
public sealed record PullCommand(
    bool Fetch,
    string Source,
    string? LocalBranch,
    string? RemoteBranch,
    bool Rebase,
    bool? FetchTags,
    bool Unshallow,
    bool Prune,
    bool PruneTags,
    bool IsPullAll,
    string? PruneRemote);

/// <summary>How the git process of the pull ended.</summary>
/// <param name="Aborted">Whether the user aborted it (<c>DialogResult.Abort</c>).</param>
public readonly record struct PullProcessResult(bool Aborted, bool ErrorOccurred);

/// <summary>How the pull dialog starts (the constructor arguments and settings of <c>FormPull</c>).</summary>
/// <param name="SelectedBranch">The current branch (<c>Module.GetSelectedBranch()</c>).</param>
/// <param name="DefaultPullAction">The action used for <see cref="GitPullAction.None"/> (<c>AppSettings.DefaultPullAction</c>).</param>
/// <param name="AutoStash"><c>AppSettings.AutoStash</c>.</param>
/// <param name="IsShallow">Whether the repository is shallow, which offers to download the full history.</param>
/// <param name="WorkingDirDisplayPath">The working directory as shown in the title.</param>
/// <param name="ErrorCaption">The caption of the error messages (<c>TranslatedStrings.Error</c>).</param>
public sealed record PullOptions(
    string SelectedBranch,
    string? DefaultRemoteBranch,
    string? DefaultRemote,
    GitPullAction PullAction,
    GitPullAction DefaultPullAction,
    bool AutoStash,
    bool IsShallow,
    string WorkingDirDisplayPath,
    string ErrorCaption);

/// <summary>Operations of the pull dialog that need the host (git, settings, other dialogs, event scripts).</summary>
public interface IPullHost
{
    /// <summary>The names of the configured remotes (<c>ConfigFileRemoteSettingsManager.LoadRemotes(false)</c>).</summary>
    IReadOnlyList<string> LoadRemotes();

    /// <summary>A git config value of the repository (<c>Module.GetSetting</c>).</summary>
    string GetSetting(string name);

    /// <summary>The remote branches, or the local branches (<c>Module.GetRefs</c>).</summary>
    IReadOnlyList<PullRef> GetRefs(bool remotes);

    /// <summary>The recent URLs (<c>RepositoryHistoryManager.Remotes</c>).</summary>
    IReadOnlyList<string> LoadUrlHistory();

    /// <summary>Adds the source to the recent URLs if it is a local directory.</summary>
    void AddLocalSourceToHistory(string path);

    /// <summary>Starts Pageant with the keys of the remotes, if PuTTY is used (<c>LoadPuttyKey</c>).</summary>
    void LoadPuttyKeys(IReadOnlyList<string> remotes);

    Task<string?> PickFolderAsync(string? startDirectory);

    bool IsDetachedHead();

    /// <summary><c>Module.ExistsMergeCommit</c>.</summary>
    bool ExistsMergeCommit(string remoteBranch, string branch);

    /// <summary>The output of <c>git name-rev --name-only</c>.</summary>
    string GetNameRev(string name);

    /// <summary>Remembers the merge option and the auto stash (<c>UpdateSettingsDuringPull</c>).</summary>
    void SaveSettings(GitPullAction pullAction, bool autoStash);

    /// <summary>Asks whether to rebase a merge commit (yes, no, <see langword="null"/> for cancel).</summary>
    bool? ConfirmRebaseMergeCommit(string text, string caption);

    /// <summary>Asks what to do when pulling on a detached HEAD.</summary>
    DetachedHeadPullChoice AskPullOnDetachedHead(string text);

    bool StartCheckoutBranch();

    /// <summary>Asks whether to use the local branch as the remote branch; <see langword="true"/> if the command link is chosen.</summary>
    bool ConfirmUseLocalBranch(string caption, string heading, string text, string commandLinkText, bool showDontShowAgain);

    /// <summary>Asks whether to fetch and prune all (<c>AppSettings.DontConfirmFetchAndPruneAll</c>).</summary>
    bool ConfirmFetchAndPruneAll(string text, string caption);

    /// <summary>Runs the event scripts before the fetch, and before the pull unless fetching only; <see langword="false"/> to cancel.</summary>
    bool RunBeforeScripts(bool fetchOnly);

    /// <summary>Runs the event scripts after the fetch, and after the pull unless fetching only.</summary>
    void RunAfterScripts(bool fetchOnly);

    /// <summary>Whether the working directory (of a non-bare repository) has changes to stash.</summary>
    bool HasChangesToStash();

    void StashSave();

    /// <summary>Runs git in the remote process dialog.</summary>
    PullProcessResult RunPull(PullCommand command);

    /// <summary>Whether the repository has a <c>.gitmodules</c> file.</summary>
    bool HasSubmodules();

    /// <summary>Whether all submodules are valid working directories.</summary>
    bool AreSubmodulesInitialized();

    /// <summary>Whether to update the submodules after the pull without asking (<see langword="null"/> to ask).</summary>
    bool? UpdateSubmodulesWithoutAsking { get; }

    void StartUpdateSubmodulesDialog();

    void UpdateSubmodules();

    bool IsInTheMiddleOfRebase();

    bool IsInTheMiddleOfAction();

    bool StartContinueRebaseDialog();

    bool HandleMergeConflicts();

    /// <summary>Whether to apply the auto stash again (<c>AppSettings.AutoPopStashAfterPull</c>, asked if not set).</summary>
    bool ConfirmApplyStash();

    void StashPop();

    void StartRemotesDialog(string? remote);

    void StartStashDialog();

    Task RunMergeToolAsync();

    void StartCommitDialog();
}

/// <summary>View model of the pull dialog (port of <c>FormPull</c>).</summary>
public sealed partial class PullViewModel : DialogViewModel
{
    /// <summary>The entry of the remotes that pulls from all of them.</summary>
    public const string AllRemotes = "[ All ]";

    private readonly PullOptions _options;
    private readonly IPullHost _host;
    private readonly IMessageBoxService _messageBoxes;
    private readonly string _branch;
    private List<string>? _heads;
    private bool _internalUpdate;
    private PullMergeAction? _action;
    private PullTagsOption _tags = PullTagsOption.Reachable;

    public PullViewModel(PullStrings strings, PullOptions options, HelpImageViewModel helpImage, IPullHost host, IMessageBoxService messageBoxes)
    {
        Strings = strings;
        _options = options;
        _host = host;
        _messageBoxes = messageBoxes;
        HelpImage = helpImage;
        HelpImage.HoverNotice = strings.HoverShowImageLabelText.Text;

        // As the FormPull constructor.
        _branch = options.SelectedBranch;
        BindRemotesDropDown(options.DefaultRemote);

        GitPullAction pullAction = options.PullAction == GitPullAction.None ? options.DefaultPullAction : options.PullAction;
        switch (pullAction)
        {
            case GitPullAction.None:
                IsMerge = true;
                break;
            case GitPullAction.Merge:
                IsMerge = true;
                IsPruneEnabled = false;
                IsPruneTagsEnabled = false;
                break;
            case GitPullAction.Rebase:
                IsRebase = true;
                IsPruneEnabled = false;
                IsPruneTagsEnabled = false;
                break;
            case GitPullAction.Fetch:
                IsFetch = true;
                IsPruneEnabled = true;
                IsPruneTagsEnabled = true;
                break;
            case GitPullAction.FetchAll:
                IsFetch = true;
                Remote = AllRemotes;
                break;
            case GitPullAction.FetchPruneAll:
                IsFetch = true;
                Prune = true;
                PruneTags = false;
                Remote = string.IsNullOrEmpty(options.DefaultRemote) ? AllRemotes : options.DefaultRemote;
                break;
        }

        IsLocalBranchEnabled = IsFetch;
        AutoStash = options.AutoStash;

        if (!string.IsNullOrEmpty(options.DefaultRemoteBranch))
        {
            RemoteBranch = options.DefaultRemoteBranch;
        }

        ShowUnshallow = options.IsShallow;
        UpdateFormTitleAndButton();
    }

    public PullStrings Strings { get; }

    public HelpImageViewModel HelpImage { get; }

    /// <summary>Whether the pull failed (<c>FormPull.ErrorOccurred</c>).</summary>
    public bool ErrorOccurred { get; private set; }

    [ObservableProperty]
    public partial string Title { get; private set; } = "";

    [ObservableProperty]
    public partial string PullButtonText { get; private set; } = "";

    /// <summary>Whether to pull from a remote (<c>PullFromRemote</c>), otherwise from a URL (<c>PullFromUrl</c>).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPullFromUrl))]
    public partial bool IsPullFromRemote { get; set; } = true;

    public bool IsPullFromUrl
    {
        get => !IsPullFromRemote;
        set => IsPullFromRemote = !value;
    }

    /// <summary>The remotes, <see cref="AllRemotes"/> first.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<string> Remotes { get; private set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPullAll))]
    public partial string Remote { get; set; } = "";

    /// <summary>The URL (<c>comboBoxPullSource</c>), also showing the URL of the selected remote.</summary>
    [ObservableProperty]
    public partial string PullSource { get; set; } = "";

    [ObservableProperty]
    public partial IReadOnlyList<string> UrlHistory { get; private set; } = [];

    [ObservableProperty]
    public partial string LocalBranch { get; set; } = "";

    [ObservableProperty]
    public partial bool IsLocalBranchEnabled { get; private set; }

    [ObservableProperty]
    public partial string RemoteBranch { get; set; } = "";

    /// <summary>The remote branches, listed when the list drops down.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<string> RemoteBranches { get; private set; } = [];

    /// <summary>The selected merge option; <see langword="null"/> only while constructing.</summary>
    public PullMergeAction? Action => _action;

    public bool IsMerge
    {
        get => _action == PullMergeAction.Merge;
        set => SetAction(value, PullMergeAction.Merge);
    }

    public bool IsRebase
    {
        get => _action == PullMergeAction.Rebase;
        set => SetAction(value, PullMergeAction.Rebase);
    }

    public bool IsFetch
    {
        get => _action == PullMergeAction.Fetch;
        set => SetAction(value, PullMergeAction.Fetch);
    }

    /// <summary>Whether merge and rebase can be chosen (not when pulling from all remotes).</summary>
    [ObservableProperty]
    public partial bool CanMergeOrRebase { get; private set; } = true;

    public bool IsReachableTags
    {
        get => _tags == PullTagsOption.Reachable;
        set => SetTags(value, PullTagsOption.Reachable);
    }

    public bool IsNoTags
    {
        get => _tags == PullTagsOption.None;
        set => SetTags(value, PullTagsOption.None);
    }

    public bool IsAllTags
    {
        get => _tags == PullTagsOption.All;
        set => SetTags(value, PullTagsOption.All);
    }

    [ObservableProperty]
    public partial bool IsAllTagsEnabled { get; private set; } = true;

    /// <summary>Whether the repository is shallow (<c>Unshallow.Visible</c>).</summary>
    public bool ShowUnshallow { get; }

    [ObservableProperty]
    public partial bool Unshallow { get; set; }

    [ObservableProperty]
    public partial bool Prune { get; set; }

    [ObservableProperty]
    public partial bool IsPruneEnabled { get; private set; } = true;

    [ObservableProperty]
    public partial bool PruneTags { get; set; }

    [ObservableProperty]
    public partial bool IsPruneTagsEnabled { get; private set; }

    [ObservableProperty]
    public partial bool AutoStash { get; set; }

    /// <summary>Whether the merge tool runs (<c>FormBusyScope</c>).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    public partial bool IsBusy { get; private set; }

    public bool IsIdle => !IsBusy;

    /// <summary>As <c>FormPull.IsPullAll</c>.</summary>
    public bool IsPullAll => Remote.Equals(AllRemotes, StringComparison.InvariantCultureIgnoreCase);

    private void SetAction(bool isChecked, PullMergeAction action)
    {
        if (!isChecked || _action == action)
        {
            return;
        }

        _action = action;
        OnPropertyChanged(nameof(Action));
        OnPropertyChanged(nameof(IsMerge));
        OnPropertyChanged(nameof(IsRebase));
        OnPropertyChanged(nameof(IsFetch));

        switch (action)
        {
            case PullMergeAction.Merge:
                MergeCheckedChanged();
                break;
            case PullMergeAction.Rebase:
                RebaseCheckedChanged();
                break;
            case PullMergeAction.Fetch:
                FetchCheckedChanged();
                break;
        }
    }

    private void SetTags(bool isChecked, PullTagsOption tags)
    {
        if (!isChecked || _tags == tags)
        {
            return;
        }

        _tags = tags;
        OnPropertyChanged(nameof(IsReachableTags));
        OnPropertyChanged(nameof(IsNoTags));
        OnPropertyChanged(nameof(IsAllTags));
    }

    /// <summary>As <c>FormPull.MergeCheckedChanged</c>.</summary>
    private void MergeCheckedChanged()
    {
        IsLocalBranchEnabled = false;
        LocalBranch = _branch;
        HelpImage.IsOnHoverShowImage2 = true;
        IsAllTagsEnabled = false;
        IsPruneEnabled = false;
        IsPruneTagsEnabled = false;

        UpdateFormTitleAndButton();
        if (IsAllTags)
        {
            IsReachableTags = true;
        }
    }

    /// <summary>As <c>FormPull.RebaseCheckedChanged</c>.</summary>
    private void RebaseCheckedChanged()
    {
        IsLocalBranchEnabled = false;
        LocalBranch = _branch;
        HelpImage.IsOnHoverShowImage2 = false;
        IsAllTagsEnabled = false;
        IsPruneEnabled = false;
        IsPruneTagsEnabled = false;

        UpdateFormTitleAndButton();
        if (IsAllTags)
        {
            IsReachableTags = true;
        }
    }

    /// <summary>As <c>FormPull.FetchCheckedChanged</c>.</summary>
    private void FetchCheckedChanged()
    {
        IsLocalBranchEnabled = true;
        LocalBranch = "";
        HelpImage.IsOnHoverShowImage2 = false;
        IsAllTagsEnabled = true;
        IsPruneEnabled = true;
        IsPruneTagsEnabled = true;

        UpdateFormTitleAndButton();
    }

    /// <summary>As <c>FormPull.UpdateFormTitleAndButton</c>.</summary>
    private void UpdateFormTitleAndButton()
    {
        string format = IsFetch ? Strings.FormTitleFetch.Text : Strings.FormTitlePull.Text;
        Title = string.Format(format, _options.WorkingDirDisplayPath);
        PullButtonText = IsFetch ? Strings.ButtonFetch.AccessKeyText : Strings.ButtonPull.AccessKeyText;
    }

    /// <summary>As <c>FormPull.BindRemotesDropDown</c>.</summary>
    private void BindRemotesDropDown(string? selectedRemoteName)
    {
        // refresh registered git remotes
        IReadOnlyList<string> remotes = _host.LoadRemotes();
        Remotes = [AllRemotes, .. remotes.Where(remote => remote != AllRemotes)];

        if (string.IsNullOrEmpty(selectedRemoteName))
        {
            selectedRemoteName = _host.GetSetting(string.Format(SettingKeyString.BranchRemote, _branch));
        }

        string? currentBranchRemote = remotes.FirstOrDefault(x => StringComparer.OrdinalIgnoreCase.Equals(x, selectedRemoteName));

        // If the default remote of the branch is not found (it is usually set on the "default pull behavior" tab of the
        // remotes dialog), pick the first remote.
        Remote = currentBranchRemote ?? (remotes.Count > 0 ? remotes[0] : AllRemotes);
    }

    partial void OnRemoteChanged(string value)
    {
        // As FormPull.Remotes_TextChanged.
        if (!_internalUpdate)
        {
            RemotesValidating();
        }
    }

    /// <summary>As <c>FormPull.RemotesValidating</c>.</summary>
    private void RemotesValidating()
    {
        ResetRemoteHeads();

        // update the text box of the Remote Url combobox to show the URL of selected remote
        PullSource = _host.GetSetting(string.Format(SettingKeyString.RemoteUrl, Remote));

        // update merge options radio buttons
        CanMergeOrRebase = !IsPullAll;
        if (IsPullAll)
        {
            IsFetch = true;
        }
    }

    // As FormPull.PullSourceValidating (on every change, not when leaving the box).
    partial void OnPullSourceChanged(string value) => ResetRemoteHeads();

    partial void OnIsPullFromRemoteChanged(bool value)
    {
        ResetRemoteHeads();
        if (value)
        {
            // As FormPull.PullFromRemoteCheckedChanged.
            CanMergeOrRebase = !IsPullAll;
            return;
        }

        // As FormPull.PullFromUrlCheckedChanged.
        CanMergeOrRebase = true;
        string prevUrl = PullSource;
        UrlHistory = _host.LoadUrlHistory();
        PullSource = prevUrl;
    }

    partial void OnPruneChanged(bool value)
    {
        // As FormPull.Prune_CheckedChanged.
        PruneTags = Prune && PruneTags;
    }

    partial void OnPruneTagsChanged(bool value)
    {
        // As FormPull.PruneTags_CheckedChanged.
        Prune = Prune || PruneTags;
        if (PruneTags)
        {
            IsAllTags = true;
        }
    }

    private void ResetRemoteHeads()
    {
        RemoteBranches = [];
        _heads = null;
    }

    /// <summary>As <c>FormPull.localBranch_Leave</c>: called when the local branch box loses the focus.</summary>
    public void OnLocalBranchLeave()
    {
        if (_branch != LocalBranch.Trim() && string.IsNullOrWhiteSpace(RemoteBranch))
        {
            RemoteBranch = LocalBranch;
        }
    }

    /// <summary>As <c>FormPull.BranchesDropDown</c>: lists the remote branches when the list drops down.</summary>
    [RelayCommand]
    private void LoadRemoteBranches()
    {
        LoadPuttyKey();

        if (_heads is null)
        {
            if (IsPullFromUrl)
            {
                _heads = [.. _host.GetRefs(remotes: false).Select(head => head.LocalName)];
            }
            else
            {
                // The quick way to get the remote branches: only the heads already known to the repository, not those
                // that are new on the server (which the remotes dialog can update).
                _heads = [];
                foreach (PullRef head in _host.GetRefs(remotes: true))
                {
                    if (!head.Name.StartsWith(Remote, StringComparison.CurrentCultureIgnoreCase))
                    {
                        continue;
                    }

                    _heads.Insert(0, head.LocalName);
                }
            }

            // GitRef.NoHead, inserted once (FormPull inserts it on every drop down).
            _heads.Insert(0, "");
        }

        string remoteBranch = RemoteBranch;
        RemoteBranches = [.. _heads];
        RemoteBranch = remoteBranch;
    }

    /// <summary>As <c>FormPull.LoadPuttyKey</c>.</summary>
    private void LoadPuttyKey()
    {
        List<string> remotes = [];
        if (IsPullFromUrl)
        {
            // No remote.
        }
        else if (IsPullAll)
        {
            remotes.AddRange(Remotes.Where(remote => !string.IsNullOrWhiteSpace(remote) && remote != AllRemotes));
        }
        else if (!string.IsNullOrWhiteSpace(Remote))
        {
            remotes.Add(Remote);
        }

        _host.LoadPuttyKeys(remotes);
    }

    /// <summary>As <c>FormPull.AddRemoteClick</c>.</summary>
    [RelayCommand]
    private void ManageRemotes()
    {
        _host.StartRemotesDialog(IsPullAll ? null : Remote);

        _internalUpdate = true;
        try
        {
            BindRemotesDropDown(Remote);
        }
        finally
        {
            _internalUpdate = false;
        }
    }

    /// <summary>As the <c>FolderBrowserButton</c> of the URL.</summary>
    [RelayCommand]
    private async Task BrowseSourceAsync()
    {
        string? folder = await _host.PickFolderAsync(PullSource);
        if (!string.IsNullOrEmpty(folder))
        {
            PullSource = folder;
        }
    }

    /// <summary>As <c>FormPull.StashClick</c>.</summary>
    [RelayCommand]
    private void Stash() => _host.StartStashDialog();

    /// <summary>As <c>FormPull.MergetoolClick</c>.</summary>
    [RelayCommand]
    private async Task SolveConflictsAsync()
    {
        IsBusy = true;
        try
        {
            await _host.RunMergeToolAsync();
        }
        finally
        {
            IsBusy = false;
        }

        if (_messageBoxes.ConfirmQuestion(Strings.AllMergeConflictSolvedQuestion.Text, Strings.AllMergeConflictSolvedQuestionCaption.Text))
        {
            _host.StartCommitDialog();
        }
    }

    /// <summary>As <c>FormPull.PullClick</c>: the dialog stays open if nothing was pulled.</summary>
    [RelayCommand]
    private void Pull()
    {
        PullOutcome outcome = PullChanges();
        if (outcome != PullOutcome.NotPulled)
        {
            Close(accepted: outcome == PullOutcome.Pulled);
        }
    }

    /// <summary>
    ///  As <c>FormPull.PullAndShowDialogWhenFailed</c> before it shows the dialog: pulls without the dialog, after
    ///  confirming a fetch and prune of all remotes. The dialog is to be shown if the result is <see cref="PullOutcome.NotPulled"/>.
    /// </summary>
    public PullOutcome PullWithoutDialog(string? remote, GitPullAction pullAction)
    {
        // Special case for "Fetch and prune" and "Fetch and prune all" to make sure user confirms the action.
        if (pullAction == GitPullAction.FetchPruneAll)
        {
            string messageBoxTitle = string.Format(Strings.PruneFromCaption.Text, string.IsNullOrEmpty(remote) ? AllRemotes : remote);
            if (!_host.ConfirmFetchAndPruneAll(Strings.PullFetchPruneAllConfirmation.Text, messageBoxTitle))
            {
                return PullOutcome.Cancelled;
            }
        }

        return PullChanges();
    }

    /// <summary>As <c>FormPull.PullChanges</c>.</summary>
    public PullOutcome PullChanges()
    {
        if (!ShouldPullChanges())
        {
            return PullOutcome.NotPulled;
        }

        UpdateSettingsDuringPull();

        PullOutcome? rebaseAnswer = ShouldRebaseMergeCommit();
        if (rebaseAnswer is not null)
        {
            return rebaseAnswer.Value;
        }

        if (!IsFetch && string.IsNullOrWhiteSpace(RemoteBranch) && _host.IsDetachedHead())
        {
            switch (_host.AskPullOnDetachedHead(Strings.NotOnBranch.Text))
            {
                case DetachedHeadPullChoice.Cancel:
                    return PullOutcome.Cancelled;
                case DetachedHeadPullChoice.CheckoutBranch when !_host.StartCheckoutBranch():
                    return PullOutcome.Cancelled;
            }
        }

        if (IsPullFromUrl)
        {
            _host.AddLocalSourceToHistory(PullSource);
        }

        string source = CalculateSource();

        if (!CalculateLocalBranch(source, out string? curLocalBranch, out string? curRemoteBranch))
        {
            return PullOutcome.NotPulled;
        }

        if (!_host.RunBeforeScripts(fetchOnly: IsFetch))
        {
            return PullOutcome.NotPulled;
        }

        bool stashed = CalculateStashedValue();

        bool isPullAll = IsPullAll;
        PullProcessResult result = _host.RunPull(new PullCommand(
            IsFetch,
            source,
            curLocalBranch,
            curRemoteBranch,
            IsRebase,
            IsAllTags ? true : IsNoTags ? false : null,
            Unshallow,
            Prune,
            PruneTags,
            isPullAll,
            IsPullFromRemote && !string.IsNullOrEmpty(Remote) ? Remote : null));
        ErrorOccurred = result.ErrorOccurred;

        bool executeScripts = false;
        try
        {
            executeScripts = !result.Aborted && !ErrorOccurred;

            if (!result.Aborted && !IsFetch)
            {
                if (!ErrorOccurred)
                {
                    if (!InitModules())
                    {
                        _host.UpdateSubmodules();
                    }
                }
                else
                {
                    executeScripts |= CheckMergeConflictsOnError();
                }
            }
        }
        finally
        {
            if (stashed)
            {
                PopStash();
            }

            if (executeScripts)
            {
                _host.RunAfterScripts(fetchOnly: IsFetch);
            }
        }

        return PullOutcome.Pulled;

        bool ShouldPullChanges()
        {
            if (IsPullFromUrl && string.IsNullOrEmpty(PullSource))
            {
                _messageBoxes.ShowError(Strings.SelectSourceDirectory.Text, _options.ErrorCaption);
                return false;
            }

            if (IsPullFromRemote && string.IsNullOrEmpty(Remote) && !IsPullAll)
            {
                _messageBoxes.ShowError(Strings.SelectRemoteRepository.Text, _options.ErrorCaption);
                return false;
            }

            if (!IsFetch && RemoteBranch == "*")
            {
                _messageBoxes.ShowError(Strings.FetchAllBranchesCanOnlyWithFetch.Text, _options.ErrorCaption);
                return false;
            }

            return true;
        }

        string CalculateSource()
        {
            if (IsPullFromUrl)
            {
                return PullSource;
            }

            LoadPuttyKey();
            return IsPullAll ? "--all" : Remote;
        }

        bool InitModules()
        {
            if (!_host.HasSubmodules())
            {
                return false;
            }

            if (!_host.AreSubmodulesInitialized())
            {
                // If the "Update submodules on checkout" option is `true`, initialize and update all submodules. If it's
                // `false` don't initialize/update the submodules. If it's indeterminate, ask the user what they'd like to do.
                if (_host.UpdateSubmodulesWithoutAsking ?? _messageBoxes.ConfirmQuestion(Strings.QuestionInitSubmodules.Text, Strings.QuestionInitSubmodulesCaption.Text))
                {
                    _host.StartUpdateSubmodulesDialog();
                }

                return true;
            }

            return false;
        }

        bool CheckMergeConflictsOnError()
        {
            // Rebase failed -> special 'rebase' merge conflict
            if (IsRebase && _host.IsInTheMiddleOfRebase())
            {
                return _host.StartContinueRebaseDialog();
            }
            else if (_host.IsInTheMiddleOfAction())
            {
                return _host.HandleMergeConflicts();
            }

            return false;
        }

        void PopStash()
        {
            if (ErrorOccurred || _host.IsInTheMiddleOfAction())
            {
                return;
            }

            if (_host.ConfirmApplyStash())
            {
                _host.StashPop();
            }
        }
    }

    /// <summary>As <c>FormPull.UpdateSettingsDuringPull</c>.</summary>
    private void UpdateSettingsDuringPull()
    {
        GitPullAction pullAction = IsMerge ? GitPullAction.Merge
            : IsRebase ? GitPullAction.Rebase
            : IsFetch ? GitPullAction.Fetch
            : GitPullAction.Default;
        _host.SaveSettings(pullAction, AutoStash);
    }

    /// <summary>As <c>FormPull.ShouldRebaseMergeCommit</c>: <see langword="null"/> to go on, the outcome otherwise.</summary>
    private PullOutcome? ShouldRebaseMergeCommit()
    {
        // ask only if exists commit not pushed to remote yet
        if (IsRebase && IsPullFromRemote && MergeCommitExists())
        {
            return _host.ConfirmRebaseMergeCommit(Strings.AreYouSureYouWantToRebaseMerge.Text, Strings.AreYouSureYouWantToRebaseMergeCaption.Text) switch
            {
                true => null,
                false => PullOutcome.NotPulled,
                null => PullOutcome.Cancelled,
            };
        }

        return null;
    }

    /// <summary>As <c>FormPull.CalculateStashedValue</c>.</summary>
    private bool CalculateStashedValue()
    {
        if (!IsFetch && AutoStash && _host.HasChangesToStash())
        {
            _host.StashSave();
            return true;
        }

        return false;
    }

    /// <summary>As <c>FormPull.CalculateLocalBranch</c>.</summary>
    private bool CalculateLocalBranch(string remote, out string? curLocalBranch, out string? curRemoteBranch)
    {
        if (IsPullAll)
        {
            curLocalBranch = null;
            curRemoteBranch = null;
            return true;
        }

        curRemoteBranch = RemoteBranch;

        if (DetachedHeadParser.IsDetachedHead(_branch))
        {
            curLocalBranch = null;
            return true;
        }

        string localBranch = LocalBranch;
        Lazy<string> currentBranchRemote = new(() => _host.GetSetting(string.Format(SettingKeyString.BranchRemote, localBranch)));

        if (_branch == localBranch)
        {
            if (remote == currentBranchRemote.Value || string.IsNullOrEmpty(currentBranchRemote.Value))
            {
                curLocalBranch = string.IsNullOrEmpty(RemoteBranch) ? null : _branch;
            }
            else
            {
                curLocalBranch = localBranch;
            }
        }
        else
        {
            curLocalBranch = localBranch;
        }

        if (string.IsNullOrEmpty(RemoteBranch) && !string.IsNullOrEmpty(curLocalBranch)
            && remote != currentBranchRemote.Value && !IsFetch)
        {
            if (_host.ConfirmUseLocalBranch(
                Strings.NoRemoteBranchCaption.Text,
                Strings.NoRemoteBranch.Text,
                string.Format(Strings.NoRemoteBranchMainInstruction.Text, remote),
                string.Format(Strings.NoRemoteBranchButton.Text, remote + "/" + curLocalBranch),
                showDontShowAgain: true))
            {
                curRemoteBranch = curLocalBranch;
                return true;
            }

            return false;
        }

        if (string.IsNullOrEmpty(RemoteBranch) && !string.IsNullOrEmpty(curLocalBranch) && IsFetch)
        {
            // if local branch eq to current branch and remote branch is not specified
            // then run fetch with no refspec
            if (_branch == curLocalBranch)
            {
                curLocalBranch = null;
                return true;
            }

            if (!_host.ConfirmUseLocalBranch(
                Strings.NoRemoteBranchCaption.Text,
                Strings.NoRemoteBranch.Text,
                string.Format(Strings.NoRemoteBranchForFetchMainInstruction.Text, remote),
                string.Format(Strings.NoRemoteBranchForFetchButton.Text, remote + "/" + curLocalBranch),
                showDontShowAgain: false))
            {
                return false;
            }

            curRemoteBranch = curLocalBranch;
        }

        return true;
    }

    /// <summary>As <c>FormPull.MergeCommitExists</c>.</summary>
    private bool MergeCommitExists() => _host.ExistsMergeCommit(CalculateRemoteBranchName(), _branch);

    /// <summary>As <c>FormPull.CalculateRemoteBranchName</c>.</summary>
    private string CalculateRemoteBranchName()
    {
        string remoteBranchName = CalculateRemoteBranchNameBasedOnBranchesText();
        return string.IsNullOrEmpty(remoteBranchName) ? remoteBranchName : Remote + "/" + remoteBranchName;
    }

    /// <summary>As <c>FormPull.CalculateRemoteBranchNameBasedOnBranchesText</c>.</summary>
    private string CalculateRemoteBranchNameBasedOnBranchesText()
    {
        if (!string.IsNullOrEmpty(RemoteBranch))
        {
            return RemoteBranch;
        }

        string remoteBranchName = _host.GetSetting(string.Format(SettingKeyString.BranchMerge, _branch));
        if (!string.IsNullOrEmpty(remoteBranchName))
        {
            remoteBranchName = _host.GetNameRev(remoteBranchName);
        }

        return remoteBranchName;
    }
}
