using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands;
using GitCommands.Git;
using GitCommands.Remotes;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the push dialog; ids match <c>FormPush</c>.</summary>
public sealed class PushStrings : ViewStrings
{
    public PushStrings()
        : base("FormPush")
    {
        Title = Add("_pushCaption", "Text", "Push");
        PushToCaption = Add("_pushToCaption", "Text", "Push to {0}");
        PushTo = Add("groupBox2", "Text", "Push to");
        PushToRemote = Add("PushToRemote", "Text", "&Remote");
        PushToRemoteToolTip = Add("PushToRemote", "toolTip1", "Remote repository to push to");
        PushToUrl = Add("PushToUrl", "Text", "U&rl");
        PushToUrlToolTip = Add("PushToUrl", "toolTip1", "Url to push to");
        ManageRemotes = Add("AddRemote", "Text", "&Manage remotes");
        Browse = Add("folderBrowserButton1", "Text", "Bro&wse...");
        BranchTab = Add("BranchTab", "Text", "Push branches");
        BranchTabToolTip = Add("BranchTab", "ToolTipText", "Push branches and commits to remote repository.");
        TagTab = Add("TagTab", "Text", "Push tags");
        TagTabToolTip = Add("TagTab", "ToolTipText", "Push tags to remote repository");
        MultipleBranchTab = Add("MultipleBranchTab", "Text", "Push multiple branches");
        BranchToPush = Add("labelFrom", "Text", "&Branch to push");
        To = Add("labelTo", "Text", "&to");
        ShowOptions = Add("ShowOptions", "Text", "Show options");
        RecursiveSubmodules = Add("label2", "Text", "Recursive &submodules");
        RecursiveSubmodulesNone = Add("RecursiveSubmodules", "Item0", "None");
        RecursiveSubmodulesCheck = Add("RecursiveSubmodules", "Item1", "Check");
        RecursiveSubmodulesOnDemand = Add("RecursiveSubmodules", "Item2", "On-demand");
        ReplaceTrackingReference = Add("ReplaceTrackingReference", "Text", "R&eplace tracking reference");
        ForceWithLease = Add("ckForceWithLease", "Text", "&Force with lease");
        ForceWithLeaseToolTip = Add("_forceWithLeaseTooltips", "Text", "Force with lease is a safer way to force push. It ensures you only overwrite work that you have seen in your local repository");
        ForcePushBranches = Add("ForcePushBranches", "Text", "F&orce push");
        CreatePullRequest = Add("_createPullRequestCB", "Text", "&Create pull request after push");
        TagToPush = Add("label1", "Text", "&Tag to push");
        ForcePushTags = Add("ForcePushTags", "Text", "&Force push");
        LocalColumn = Add("LocalColumn", "HeaderText", "Local Branch");
        RemoteColumn = Add("RemoteColumn", "HeaderText", "Remote Branch");
        AheadBehindColumn = Add("NewColumn", "HeaderText", "Ahead/Behind");
        PushColumn = Add("PushColumn", "HeaderText", "Push");
        ForceColumn = Add("ForceColumn", "HeaderText", "Force");
        DeleteColumn = Add("DeleteColumn", "HeaderText", "Delete Remote Branch");
        UnselectAll = Add("unselectAllToolStripMenuItem", "Text", "Unselect all");
        SelectTracked = Add("selectTrackedToolStripMenuItem", "Text", "Select tracked");
        SelectAll = Add("selectAllToolStripMenuItem", "Text", "Select all");
        LoadSshKey = Add("LoadSSHKey", "Text", "&Load SSH key");
        Pull = Add("Pull", "Text", "P&ull");
        BranchNewForRemote = Add("_branchNewForRemote", "Text", "The branch you are about to push seems to be a new branch for the remote.\nAre you sure you want to push this branch?");
        NoCurrentBranch = Add("_noCurrentBranch", "Text", "No branch is selected, cannot push.");
        SelectDestinationDirectory = Add("_selectDestinationDirectory", "Text", "Please select a destination directory");
        ErrorPushToRemoteCaption = Add("_errorPushToRemoteCaption", "Text", "Push to remote");
        ConfigureRemote = Add("_configureRemote", "Text", "Please configure a remote repository first.\nWould you like to do it now?");
        SelectTag = Add("_selectTag", "Text", "You need to select a tag to push or select \"Push all tags\".");
        UpdateTrackingReference = Add("_updateTrackingReference", "Text", "The branch {0} does not have a tracking reference. Do you want to add a tracking reference to {1}?");
        UseForceWithLeaseInstead = Add("_useForceWithLeaseInstead", "Text", "Force push may overwrite changes since your last fetch. Do you want to use the safer force with lease instead?");
        PullRepositoryMainMergeInstruction = Add("_pullRepositoryMainMergeInstruction", "Text", "Pull latest changes from remote repository");
        PullRepositoryMainForceInstruction = Add("_pullRepositoryMainForceInstruction", "Text", "Push rejected");
        PullRepositoryMergeInstruction = Add("_pullRepositoryMergeInstruction", "Text", "The push was rejected because the tip of your current branch is behind its remote counterpart. Merge the remote changes before pushing again.");
        PullRepositoryForceInstruction = Add("_pullRepositoryForceInstruction", "Text", "The push was rejected because the tip of your current branch is behind its remote counterpart");
        PullDefaultButton = Add("_pullDefaultButton", "Text", "&Pull with the default pull action ({0})");
        PullRebaseButton = Add("_pullRebaseButton", "Text", "Pull with &rebase");
        PullMergeButton = Add("_pullMergeButton", "Text", "Pull with &merge");
        PushForceButton = Add("_pushForceButton", "Text", "&Force push with lease");
        PullActionNone = Add("_pullActionNone", "Text", "none");
        PullActionFetch = Add("_pullActionFetch", "Text", "fetch");
        PullActionRebase = Add("_pullActionRebase", "Text", "rebase");
        PullActionMerge = Add("_pullActionMerge", "Text", "merge");
        PullRepositoryCaption = Add("_pullRepositoryCaption", "Text", "Push was rejected from \"{0}\"");
        Error = Add("_error", "Text", "Error", category: "TranslatedStrings");
    }

    public TranslatedText Error { get; }

    public TranslatedText Title { get; }

    public TranslatedText PushToCaption { get; }

    public TranslatedText PushTo { get; }

    public TranslatedText PushToRemote { get; }

    public TranslatedText PushToRemoteToolTip { get; }

    public TranslatedText PushToUrl { get; }

    public TranslatedText PushToUrlToolTip { get; }

    public TranslatedText ManageRemotes { get; }

    public TranslatedText Browse { get; }

    public TranslatedText BranchTab { get; }

    public TranslatedText BranchTabToolTip { get; }

    public TranslatedText TagTab { get; }

    public TranslatedText TagTabToolTip { get; }

    public TranslatedText MultipleBranchTab { get; }

    public TranslatedText BranchToPush { get; }

    public TranslatedText To { get; }

    public TranslatedText ShowOptions { get; }

    public TranslatedText RecursiveSubmodules { get; }

    public TranslatedText RecursiveSubmodulesNone { get; }

    public TranslatedText RecursiveSubmodulesCheck { get; }

    public TranslatedText RecursiveSubmodulesOnDemand { get; }

    public TranslatedText ReplaceTrackingReference { get; }

    public TranslatedText ForceWithLease { get; }

    public TranslatedText ForceWithLeaseToolTip { get; }

    public TranslatedText ForcePushBranches { get; }

    public TranslatedText CreatePullRequest { get; }

    public TranslatedText TagToPush { get; }

    public TranslatedText ForcePushTags { get; }

    public TranslatedText LocalColumn { get; }

    public TranslatedText RemoteColumn { get; }

    public TranslatedText AheadBehindColumn { get; }

    public TranslatedText PushColumn { get; }

    public TranslatedText ForceColumn { get; }

    public TranslatedText DeleteColumn { get; }

    public TranslatedText UnselectAll { get; }

    public TranslatedText SelectTracked { get; }

    public TranslatedText SelectAll { get; }

    public TranslatedText LoadSshKey { get; }

    public TranslatedText Pull { get; }

    public TranslatedText BranchNewForRemote { get; }

    public TranslatedText NoCurrentBranch { get; }

    public TranslatedText SelectDestinationDirectory { get; }

    public TranslatedText ErrorPushToRemoteCaption { get; }

    public TranslatedText ConfigureRemote { get; }

    public TranslatedText SelectTag { get; }

    public TranslatedText UpdateTrackingReference { get; }

    public TranslatedText UseForceWithLeaseInstead { get; }

    public TranslatedText PullRepositoryMainMergeInstruction { get; }

    public TranslatedText PullRepositoryMainForceInstruction { get; }

    public TranslatedText PullRepositoryMergeInstruction { get; }

    public TranslatedText PullRepositoryForceInstruction { get; }

    public TranslatedText PullDefaultButton { get; }

    public TranslatedText PullRebaseButton { get; }

    public TranslatedText PullMergeButton { get; }

    public TranslatedText PushForceButton { get; }

    public TranslatedText PullActionNone { get; }

    public TranslatedText PullActionFetch { get; }

    public TranslatedText PullActionRebase { get; }

    public TranslatedText PullActionMerge { get; }

    public TranslatedText PullRepositoryCaption { get; }
}

/// <summary>The tabs of the push dialog (<c>TabControlTagBranch</c>).</summary>
public enum PushTab
{
    Branch,
    Tag,
    MultipleBranches,
}

/// <summary>A row of the multiple branches (a row of <c>_branchTable</c>); pushing, forcing and deleting exclude each other.</summary>
public sealed partial class PushBranchRow : ObservableObject
{
    public PushBranchRow(string? localBranch, string remoteBranch, string aheadBehind)
    {
        LocalBranch = localBranch;
        RemoteBranch = remoteBranch;
        AheadBehind = aheadBehind;
    }

    /// <summary>The local branch, <see langword="null"/> for a branch that only exists at the remote.</summary>
    public string? LocalBranch { get; }

    [ObservableProperty]
    public partial string RemoteBranch { get; set; }

    public string AheadBehind { get; }

    /// <summary>As <c>BranchGrid_DataBindingComplete</c>: a remote-only branch can only be deleted.</summary>
    public bool CanPush => LocalBranch is not null;

    /// <summary>As <c>selectTrackedToolStripMenuItem_Click</c>: the branch exists locally and at the remote.</summary>
    public bool IsTracked => !string.IsNullOrEmpty(LocalBranch) && !string.IsNullOrEmpty(RemoteBranch);

    [ObservableProperty]
    public partial bool Push { get; set; }

    [ObservableProperty]
    public partial bool Force { get; set; }

    [ObservableProperty]
    public partial bool Delete { get; set; }

    // As BranchTable_ColumnChanged.
    partial void OnPushChanged(bool value)
    {
        if (value)
        {
            Force = false;
            Delete = false;
        }
    }

    partial void OnForceChanged(bool value)
    {
        if (value)
        {
            Push = false;
            Delete = false;
        }
    }

    partial void OnDeleteChanged(bool value)
    {
        if (value)
        {
            Push = false;
            Force = false;
        }
    }
}

/// <summary>What to push, as <c>PushChanges</c> builds its git command.</summary>
/// <param name="Remote">The remote pushed to (empty when pushing to a URL), for the PuTTY key of <c>FormRemoteProcess</c>.</param>
/// <param name="PushAllBranches">Whether "[ All ]" is selected (<c>Commands.PushAll</c>).</param>
/// <param name="PushActions">The pushes and deletes of the multiple branches tab (<c>Commands.PushMultiple</c>).</param>
/// <param name="IsCurrentBranch">Whether the current branch is pushed, which can be pulled when the push is rejected (<c>HandlePushOnExit</c>).</param>
/// <param name="IsCurrentBranchRemote">Whether the remote is the one of the current branch (<c>IsRebasingMergeCommit</c>).</param>
public sealed record PushRequest(
    PushTab Tab,
    string Destination,
    string Remote,
    bool PushToRemote,
    string LocalBranch,
    string RemoteBranch,
    bool PushAllBranches,
    ForcePushOptions ForcePush,
    bool Track,
    int RecursiveSubmodules,
    string Tag,
    bool PushAllTags,
    IReadOnlyList<GitPushAction> PushActions,
    bool CreatePullRequest,
    bool IsCurrentBranch,
    bool IsCurrentBranchRemote);

/// <summary>The repository, the settings and the dialogs of the push dialog.</summary>
public interface IPushHost
{
    string WorkingDirectory { get; }

    /// <summary>As <c>Module.GetSelectedBranch</c>.</summary>
    string CurrentBranch { get; }

    bool IsBareRepository { get; }

    /// <summary>The local and remote branches (<c>GetRefs(RefsFilter.Heads | RefsFilter.Remotes)</c>).</summary>
    IReadOnlyList<IGitRef> GetRefs();

    /// <summary>The names of the tags (<c>FillTagDropDown</c>).</summary>
    IReadOnlyList<string> GetTags();

    /// <summary>The remotes of the .git/config file (<c>LoadRemotes(false)</c>).</summary>
    IReadOnlyList<ConfigFileRemote> LoadRemotes();

    /// <summary>The remote of the branch (the <c>branch.&lt;name&gt;.remote</c> setting).</summary>
    string GetBranchRemote(string? branch);

    /// <summary>As <c>IConfigFileRemoteSettingsManager.GetDefaultPushRemote</c>.</summary>
    string? GetDefaultPushRemote(ConfigFileRemote remote, string branch);

    /// <summary>Whether <c>branch.autosetupmerge</c> is false.</summary>
    bool IsAutoSetupMergeDisabled { get; }

    /// <summary>Whether a pull request can be created after the push (a git hoster plugin or an Azure DevOps remote).</summary>
    bool CanCreatePullRequest { get; }

    /// <summary>As <c>AppSettings.RecursiveSubmodules</c> (the index of the combo box).</summary>
    int RecursiveSubmodules { get; set; }

    /// <summary>As <c>AppSettings.AlwaysShowAdvOpt</c>.</summary>
    bool AlwaysShowAdvancedOptions { get; }

    /// <summary>As <c>AppSettings.DontConfirmAddTrackingRef</c>.</summary>
    bool DontConfirmAddTrackingReference { get; }

    /// <summary>The URLs pushed to before (<c>RepositoryHistoryManager.Remotes</c>).</summary>
    IReadOnlyList<string> GetRecentUrls();

    /// <summary>As <c>StartRemotesDialog</c>; <see langword="true"/> if the remotes may have changed.</summary>
    bool ManageRemotes(string? selectedRemote);

    /// <summary>As <c>PullClick</c>.</summary>
    void Pull();

    /// <summary>As <c>StartPageant</c>: the PuTTY key of the remote, with plink.</summary>
    void StartPageant(string? remote);

    /// <summary>As the suppressible confirmation of a branch new for the remote (<c>AppSettings.DontConfirmPushNewBranch</c>).</summary>
    bool ConfirmNewBranchForRemote(string text, string caption);

    /// <summary>
    ///  As <c>LoadMultiBranchViewData</c>: the local branches and the branches of the remote (from the remote itself if set so),
    ///  <see langword="null"/> if they cannot be listed.
    /// </summary>
    IReadOnlyList<PushBranchRow>? LoadMultipleBranches(string remote);

    /// <summary>
    ///  As the end of <c>PushChanges</c>: runs the scripts and the push (offering to pull or force when it is rejected,
    ///  <c>HandlePushOnExit</c>), then creates the pull request if asked.
    /// </summary>
    /// <param name="errorOccurred">As <c>ErrorOccurred</c>: git failed.</param>
    /// <returns><see langword="true"/> if the push completed.</returns>
    bool Push(PushRequest request, out bool errorOccurred);
}

/// <summary>
///  View model of the push dialog (port of <c>FormPush</c>; docs/avalonia-port/PLAN.md, phase 5): a branch, the tags or
///  several branches to a remote or a URL.
/// </summary>
public sealed partial class PushViewModel : DialogViewModel
{
    /// <summary>As <c>HeadText</c>.</summary>
    public const string HeadText = "HEAD";

    /// <summary>As <c>AllRefs</c>: all the branches, or all the tags.</summary>
    public const string AllRefs = "[ All ]";

    private readonly IPushHost _host;
    private readonly IMessageBoxService _messageBoxes;
    private readonly IFileDialogService? _fileDialogs;
    private readonly string _currentBranchName;
    private readonly bool _initializing = true;
    private IReadOnlyList<IGitRef> _gitRefs;
    private ConfigFileRemote? _currentBranchRemote;

    public PushViewModel(PushStrings strings, IPushHost host, IMessageBoxService messageBoxes, string? branchName = null, bool forceWithLease = false, IFileDialogService? fileDialogs = null)
    {
        Strings = strings;
        _host = host;
        _messageBoxes = messageBoxes;
        _fileDialogs = fileDialogs;
        Title = string.Concat(strings.Title.Text, " (", host.WorkingDirectory, ")");
        RecursiveSubmoduleModes = [strings.RecursiveSubmodulesNone.Text, strings.RecursiveSubmodulesCheck.Text, strings.RecursiveSubmodulesOnDemand.Text];
        CanCreatePullRequest = host.CanCreatePullRequest;

        // As Init.
        _gitRefs = host.GetRefs();
        RecursiveSubmodules = host.RecursiveSubmodules;
        _currentBranchName = host.CurrentBranch;
        branchName ??= _currentBranchName;
        Branch = DetachedHeadParser.IsDetachedHead(branchName) ? HeadText : branchName;
        UpdateBranches();
        BindRemotes(selectedRemoteName: null);
        UpdateRemoteBranches();
        ShowOptions = host.AlwaysShowAdvancedOptions;
        ForceWithLease = forceWithLease;
        _initializing = false;
        OnRemoteChanged();
    }

    public PushStrings Strings { get; }

    public string Title { get; }

    public IReadOnlyList<string> RecursiveSubmoduleModes { get; }

    public ObservableCollection<ConfigFileRemote> Remotes { get; } = [];

    [ObservableProperty]
    public partial ConfigFileRemote? SelectedRemote { get; set; }

    /// <summary>The push goes to a remote (<c>PushToRemote</c>), otherwise to <see cref="PushDestination"/> (<c>PushToUrl</c>).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPushToUrl))]
    public partial bool IsPushToRemote { get; set; } = true;

    public bool IsPushToUrl
    {
        get => !IsPushToRemote;
        set => IsPushToRemote = !value;
    }

    /// <summary>The URL pushed to (<c>PushDestination</c>); shows the URL of the remote when pushing to a remote.</summary>
    [ObservableProperty]
    public partial string PushDestination { get; set; } = "";

    public ObservableCollection<string> RecentUrls { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedTabIndex))]
    public partial PushTab SelectedTab { get; set; }

    /// <summary>The index of <see cref="SelectedTab"/>, for the tab control.</summary>
    public int SelectedTabIndex
    {
        get => (int)SelectedTab;
        set => SelectedTab = (PushTab)value;
    }

    /// <summary>The local branch (<c>_NO_TRANSLATE_Branch</c>): <see cref="AllRefs"/>, <see cref="HeadText"/> or a branch.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRemoteBranchEnabled))]
    public partial string Branch { get; set; } = "";

    public ObservableCollection<string> Branches { get; } = [];

    /// <summary>The branch of the remote (<c>RemoteBranch</c>).</summary>
    [ObservableProperty]
    public partial string RemoteBranch { get; set; } = "";

    public ObservableCollection<string> RemoteBranches { get; } = [];

    /// <summary>As <c>_NO_TRANSLATE_Branch_SelectedIndexChanged</c>.</summary>
    public bool IsRemoteBranchEnabled => Branch != AllRefs;

    /// <summary>As <c>ShowOptions_LinkClicked</c>: the options of the branch tab are shown.</summary>
    [ObservableProperty]
    public partial bool ShowOptions { get; set; }

    [ObservableProperty]
    public partial int RecursiveSubmodules { get; set; }

    [ObservableProperty]
    public partial bool ReplaceTrackingReference { get; set; }

    [ObservableProperty]
    public partial bool ForceWithLease { get; set; }

    [ObservableProperty]
    public partial bool ForcePushBranches { get; set; }

    /// <summary>As the binding of <c>ForcePushTags</c> to <c>ckForceWithLease</c>: the tags are forced with the branches.</summary>
    public bool ForcePushTags
    {
        get => ForceWithLease;
        set => ForceWithLease = value;
    }

    [ObservableProperty]
    public partial bool CreatePullRequest { get; set; }

    public bool CanCreatePullRequest { get; }

    /// <summary>The tag to push (<c>TagComboBox</c>), or <see cref="AllRefs"/>.</summary>
    [ObservableProperty]
    public partial string Tag { get; set; } = "";

    public ObservableCollection<string> Tags { get; } = [];

    public ObservableCollection<PushBranchRow> MultipleBranches { get; } = [];

    /// <summary>As <c>EnableLoadSshButton</c>.</summary>
    public bool CanLoadSshKey => !string.IsNullOrWhiteSpace(SelectedRemote?.PuttySshKey);

    /// <summary>As <c>ErrorOccurred</c>: git failed in the last push.</summary>
    public bool ErrorOccurred { get; private set; }

    // As ForcePushBranchesCheckedChanged and ForceWithLeaseCheckedChanged.
    partial void OnForceWithLeaseChanged(bool value)
    {
        if (value)
        {
            ForcePushBranches = false;
        }

        OnPropertyChanged(nameof(ForcePushTags));
    }

    partial void OnForcePushBranchesChanged(bool value)
    {
        if (value)
        {
            ForceWithLease = false;
        }
    }

    partial void OnSelectedRemoteChanged(ConfigFileRemote? value)
    {
        OnPropertyChanged(nameof(CanLoadSshKey));
        if (!_initializing)
        {
            OnRemoteChanged();
        }
    }

    partial void OnBranchChanged(string value)
    {
        if (!_initializing)
        {
            OnBranchSelected();
        }
    }

    // As TabControlTagBranch_Selected.
    partial void OnSelectedTabChanged(PushTab value)
    {
        switch (value)
        {
            case PushTab.MultipleBranches:
                UpdateMultipleBranches();
                break;
            case PushTab.Tag:
                Tags.Clear();
                Tags.Add(AllRefs);
                foreach (string tag in _host.GetTags())
                {
                    Tags.Add(tag);
                }

                break;
            default:
                UpdateBranches();
                UpdateRemoteBranches();
                break;
        }
    }

    // As PushToUrlCheckedChanged.
    partial void OnIsPushToRemoteChanged(bool value)
    {
        if (!value)
        {
            string previousUrl = PushDestination;
            RecentUrls.Clear();
            foreach (string url in _host.GetRecentUrls())
            {
                RecentUrls.Add(url);
            }

            PushDestination = previousUrl;
            OnBranchSelected();
        }
        else
        {
            OnRemoteChanged();
        }
    }

    /// <summary>As <c>RemotesUpdated</c>.</summary>
    private void OnRemoteChanged()
    {
        if (SelectedRemote is not { } remote)
        {
            return;
        }

        if (SelectedTab == PushTab.MultipleBranches)
        {
            UpdateMultipleBranches();
        }

        // The URL of the remote.
        PushDestination = string.IsNullOrEmpty(remote.PushUrl) ? remote.Url ?? "" : remote.PushUrl;

        if (string.IsNullOrEmpty(Branch))
        {
            Branch = _currentBranchName;
        }

        OnBranchSelected();
    }

    /// <summary>As <c>BindRemotesDropDown</c>: the remote of the current branch, else origin, else the first one.</summary>
    private void BindRemotes(string? selectedRemoteName)
    {
        Remotes.Clear();
        foreach (ConfigFileRemote remote in _host.LoadRemotes())
        {
            Remotes.Add(remote);
        }

        if (string.IsNullOrEmpty(selectedRemoteName))
        {
            selectedRemoteName = _host.GetBranchRemote(_currentBranchName);
        }

        _currentBranchRemote = Remotes.FirstOrDefault(x => StringComparer.OrdinalIgnoreCase.Equals(x.Name, selectedRemoteName));
        SelectedRemote = _currentBranchRemote
            ?? Remotes.FirstOrDefault(x => StringComparer.OrdinalIgnoreCase.Equals(x.Name, "origin"))
            ?? Remotes.FirstOrDefault();
    }

    /// <summary>As <c>UpdateBranchDropDown</c>.</summary>
    private void UpdateBranches()
    {
        string branch = Branch;
        Branches.Clear();
        Branches.Add(AllRefs);
        Branches.Add(HeadText);
        foreach (IGitRef head in _gitRefs.Where(r => r.IsHead))
        {
            Branches.Add(head.Name);
        }

        Branch = branch;
    }

    /// <summary>As <c>UpdateRemoteBranchDropDown</c>.</summary>
    private void UpdateRemoteBranches()
    {
        string remoteBranch = RemoteBranch;
        RemoteBranches.Clear();
        if (!string.IsNullOrEmpty(Branch) && !DetachedHeadParser.IsDetachedHead(Branch) && Branch != HeadText)
        {
            RemoteBranches.Add(Branch);
        }

        if (SelectedRemote is not null)
        {
            foreach (string head in GetRemoteBranches(SelectedRemote.Name).Select(head => head.LocalName).Where(head => head != Branch))
            {
                RemoteBranches.Add(head);
            }
        }

        RemoteBranch = remoteBranch;
    }

    /// <summary>As <c>BranchSelectedValueChanged</c>: the remote branch the local branch is pushed to by default.</summary>
    private void OnBranchSelected()
    {
        if (Branch == AllRefs)
        {
            RemoteBranch = "";
            return;
        }

        if (Branch == HeadText)
        {
            return;
        }

        if (IsPushToRemote && SelectedRemote is { } remote && _gitRefs.FirstOrDefault(r => r.Name == Branch) is { } selectedBranchRef)
        {
            string? defaultRemote = _host.GetDefaultPushRemote(remote, selectedBranchRef.Name);
            if (!string.IsNullOrEmpty(defaultRemote))
            {
                RemoteBranch = defaultRemote;
                return;
            }

            if (selectedBranchRef.TrackingRemote.Equals(remote.Name, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(selectedBranchRef.MergeWith))
            {
                RemoteBranch = selectedBranchRef.MergeWith;
                return;
            }
        }

        string newRemoteBranchName = $"{SelectedRemote?.Prefix}{Branch}";
        if (!RemoteBranches.Contains(newRemoteBranchName))
        {
            RemoteBranches.Add(newRemoteBranchName);
        }

        RemoteBranch = newRemoteBranchName;
    }

    /// <summary>As <c>UpdateMultiBranchView</c>.</summary>
    private void UpdateMultipleBranches()
    {
        MultipleBranches.Clear();
        if (SelectedRemote?.Name is not { } remote)
        {
            return;
        }

        foreach (PushBranchRow row in _host.LoadMultipleBranches(remote) ?? [])
        {
            MultipleBranches.Add(row);
        }
    }

    private IEnumerable<IGitRef> GetRemoteBranches(string? remoteName) => _gitRefs.Where(r => r.IsRemote && r.Remote == remoteName);

    /// <summary>As <c>IsBranchKnownToRemote</c>.</summary>
    private bool IsBranchKnownToRemote(string? remote, string branch)
        => GetRemoteBranches(remote).Any(r => r.LocalName == branch)
            || _gitRefs.Any(r => r.IsHead && r.Name == branch && r.TrackingRemote == remote);

    /// <summary>As <c>GetForcePushOption</c>, for the tab (see the ledger: the lease is used for the branches, the tags are forced).</summary>
    public ForcePushOptions GetForcePushOption()
    {
        if (SelectedTab == PushTab.Tag)
        {
            return ForcePushTags ? ForcePushOptions.Force : ForcePushOptions.DoNotForce;
        }

        return ForcePushBranches ? ForcePushOptions.Force
            : ForceWithLease ? ForcePushOptions.ForceWithLease
            : ForcePushOptions.DoNotForce;
    }

    /// <summary>As <c>unselectAllToolStripMenuItem_Click</c>, <c>selectAllToolStripMenuItem_Click</c> and <c>selectTrackedToolStripMenuItem_Click</c>.</summary>
    public void SelectBranchesToPush(bool? tracked)
    {
        foreach (PushBranchRow row in MultipleBranches.Where(r => r.CanPush))
        {
            row.Push = tracked switch
            {
                null => true,
                true => row.IsTracked,
                false => false,
            };
        }
    }

    /// <summary>As <c>OpenRemotesDialogAndRefreshList</c>.</summary>
    [RelayCommand]
    private void ManageRemotes()
    {
        string? selectedRemoteName = SelectedRemote?.Name;
        if (_host.ManageRemotes(selectedRemoteName))
        {
            BindRemotes(selectedRemoteName);
        }
    }

    [RelayCommand]
    private void LoadSshKey() => _host.StartPageant(SelectedRemote?.Name);

    [RelayCommand]
    private void Pull() => _host.Pull();

    [RelayCommand]
    private void ShowAllOptions() => ShowOptions = true;

    /// <summary>As <c>folderBrowserButton1</c>: a folder to push to.</summary>
    [RelayCommand]
    private async Task BrowseAsync()
    {
        if (_fileDialogs is not null && await _fileDialogs.PickFolderAsync(PushDestination) is { } folder)
        {
            PushDestination = folder;
        }
    }

    /// <summary>As <c>PushClick</c>: the dialog closes after a push.</summary>
    [RelayCommand]
    private void Push()
    {
        if (PushChanges())
        {
            Close(accepted: true);
        }
    }

    /// <summary>As <c>PushChanges</c>.</summary>
    /// <returns><see langword="true"/> if the push completed.</returns>
    public bool PushChanges()
    {
        ErrorOccurred = false;
        if (IsPushToUrl && !Uri.IsWellFormedUriString(PushDestination, UriKind.Absolute))
        {
            _messageBoxes.ShowError(Strings.SelectDestinationDirectory.Text, Strings.Error.Text);
            return false;
        }

        if (!CheckIfRemoteExist() || SelectedRemote is not { } selectedRemote)
        {
            return false;
        }

        string? selectedRemoteName = selectedRemote.Name;
        if (SelectedTab == PushTab.Tag && string.IsNullOrEmpty(Tag))
        {
            _messageBoxes.ShowError(Strings.SelectTag.Text, Strings.Error.Text);
            return false;
        }

        if (SelectedTab == PushTab.Branch && Branch != AllRefs
            && (string.IsNullOrWhiteSpace(Branch)
                || Branch == DetachedHeadParser.DetachedBranch
                || string.IsNullOrWhiteSpace(RemoteBranch)
                || RemoteBranch == DetachedHeadParser.DetachedBranch))
        {
            _messageBoxes.ShowError(Strings.NoCurrentBranch.Text, Strings.Error.Text);
            return false;
        }

        // A branch new for the remote (as far as known without connecting) is pushed on confirmation only.
        if (SelectedTab == PushTab.Branch && IsPushToRemote && !_host.IsBareRepository
            && Branch != AllRefs
            && RemoteBranch != _host.GetDefaultPushRemote(selectedRemote, Branch)
            && !IsBranchKnownToRemote(selectedRemoteName, RemoteBranch)
            && !_host.ConfirmNewBranchForRemote(Strings.BranchNewForRemote.Text, Strings.Title.Text))
        {
            return false;
        }

        _host.RecursiveSubmodules = RecursiveSubmodules;

        string remote = "";
        string destination;
        if (IsPushToUrl)
        {
            destination = PushDestination;
        }
        else
        {
            destination = selectedRemoteName ?? "";
            remote = destination.Trim();
        }

        bool track = false;
        IReadOnlyList<GitPushAction> pushActions = [];
        string tag = "";
        bool pushAllTags = false;
        if (SelectedTab == PushTab.Branch)
        {
            track = ReplaceTrackingReference;
            if (!track && !string.IsNullOrWhiteSpace(RemoteBranch))
            {
                IGitRef? selectedLocalBranch = _gitRefs.FirstOrDefault(b => b.IsHead && b.Name == Branch);
                track = selectedLocalBranch is not null && string.IsNullOrEmpty(selectedLocalBranch.TrackingRemote)
                    && !Remotes.Any(x => Branch.StartsWith(x.Name!, StringComparison.OrdinalIgnoreCase));
                if (_host.IsAutoSetupMergeDisabled)
                {
                    track = false;
                }

                if (track && !_host.DontConfirmAddTrackingReference)
                {
                    bool? answer = _messageBoxes.ConfirmWithCancel(string.Format(Strings.UpdateTrackingReference.Text, selectedLocalBranch!.Name, RemoteBranch), Strings.Title.Text);
                    if (answer is null)
                    {
                        return false;
                    }

                    track = answer.Value;
                }
            }

            if (ForcePushBranches)
            {
                switch (_messageBoxes.ConfirmWithCancel(Strings.UseForceWithLeaseInstead.Text, "Question"))
                {
                    case true:
                        ForceWithLease = true;
                        break;
                    case null:
                        return false;
                }
            }
        }
        else if (SelectedTab == PushTab.Tag)
        {
            tag = Tag;
            if (tag == AllRefs)
            {
                tag = "";
                pushAllTags = true;
            }
        }
        else
        {
            List<GitPushAction> actions = [];
            foreach (PushBranchRow row in MultipleBranches)
            {
                string? remoteBranch = string.IsNullOrWhiteSpace(row.RemoteBranch) ? row.LocalBranch : row.RemoteBranch;
                if (string.IsNullOrWhiteSpace(remoteBranch))
                {
                    continue;
                }

                if (row.Push || row.Force)
                {
                    actions.Add(new GitPushAction(row.LocalBranch, remoteBranch, row.Force));
                }
                else if (row.Delete)
                {
                    actions.Add(GitPushAction.DeleteRemoteBranch(remoteBranch));
                }
            }

            pushActions = actions;
        }

        PushRequest request = new(
            SelectedTab,
            destination,
            remote,
            IsPushToRemote,
            Branch,
            RemoteBranch,
            PushAllBranches: Branch == AllRefs,
            GetForcePushOption(),
            track,
            RecursiveSubmodules,
            tag,
            pushAllTags,
            pushActions,
            CreatePullRequest && CanCreatePullRequest,
            IsCurrentBranch: IsPushToRemote && Branch != AllRefs && SelectedTab == PushTab.Branch && Branch == _currentBranchName,
            IsCurrentBranchRemote: selectedRemote == _currentBranchRemote);
        bool pushed = _host.Push(request, out bool errorOccurred);
        ErrorOccurred = errorOccurred;
        return pushed;
    }

    /// <summary>As <c>CheckIfRemoteExist</c>: without a remote, the remotes can be configured first.</summary>
    private bool CheckIfRemoteExist()
    {
        if (Remotes.Count > 0)
        {
            return true;
        }

        if (!_messageBoxes.Confirm(Strings.ConfigureRemote.Text, Strings.ErrorPushToRemoteCaption.Text))
        {
            return false;
        }

        ManageRemotes();
        return Remotes.Count > 0;
    }
}
