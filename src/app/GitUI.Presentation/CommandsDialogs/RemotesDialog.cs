using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands.Git;
using GitCommands.Remotes;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the remotes dialog; ids match <c>FormRemotes</c> and its <c>FolderBrowserButton</c>.</summary>
public sealed class RemotesStrings : ViewStrings
{
    public RemotesStrings()
        : base("FormRemotes")
    {
        Title = Add("$this", "Text", "Remote repositories");
        RemotesTab = Add("tabPage1", "Text", "Remote repositories");
        PullBehaviorTab = Add("tabPage2", "Text", "Default pull behavior (fetch & merge)");
        NewRemoteHeader = Add("_gbMgtPanelHeaderNew", "Text", "Create New Remote");
        EditRemoteHeader = Add("_gbMgtPanelHeaderEdit", "Text", "Edit Remote Details");
        NewTooltip = Add("_btnNewTooltip", "Text", "Add a new remote");
        DeleteTooltip = Add("_btnDeleteTooltip", "Text", "Delete the selected remote");
        ActivateTooltip = Add("_btnToggleStateTooltip_Activate", "Text", "Activate the selected remote");
        DeactivateTooltip = Add("_btnToggleStateTooltip_Deactivate", "Text", "Deactivate the selected remote.\nInactive remote is completely invisible to git.");
        ActiveGroup = Add("_lvgEnabledHeader", "Text", "Active");
        InactiveGroup = Add("_lvgDisabledHeader", "Text", "Inactive");
        Details = Add("pnlMgtDetails", "Text", "Details");
        Name = Add("label1", "Text", "&Name");
        UrlAsFetchPush = Add("_labelUrlAsFetchPush", "Text", "&Url");
        UrlAsFetch = Add("_labelUrlAsFetch", "Text", "Fetch &Url");
        Prefix = Add("lblRemotePrefix", "Text", "Prefix");
        Color = Add("lblRemoteColor", "Text", "Color");
        SetColor = Add("btnRemoteColor", "Text", "Set &color");
        DefaultColor = Add("btnRemoteColorReset", "Text", "Default color");
        SeparatePushUrl = Add("checkBoxSepPushUrl", "Text", "Sep&arate Push Url");
        PushUrl = Add("labelPushUrl", "Text", "&Push Url");
        BrowseUrl = Add("buttonBrowse", "Text", "&Browse...", category: "FolderBrowserButton");
        BrowsePushUrl = Add("folderBrowserButtonPushUrl", "Text", "Bro&wse...");
        PuttySsh = Add("lblMgtPuttyPanelHeader", "Text", "PuTTY SSH");
        PrivateKeyFile = Add("label3", "Text", "Private &key file");
        SshBrowse = Add("SshBrowse", "Text", "Brows&e...");
        LoadSshKey = Add("LoadSSHKey", "Text", "&Load SSH key");
        TestConnection = Add("TestConnection", "Text", "&Test connection");
        Save = Add("Save", "Text", "&Save changes");
        GitMessage = Add("_gitMessage", "Text", "Message");
        QuestionDeleteRemote = Add("_questionDeleteRemote", "Text", "Are you sure you want to delete this remote?");
        QuestionDeleteRemoteCaption = Add("_questionDeleteRemoteCaption", "Text", "Delete");
        EnabledRemoteAlreadyExists = Add("_enabledRemoteAlreadyExists", "Text", "An active remote named \"{0}\" already exists.");
        DisabledRemoteAlreadyExists = Add("_disabledRemoteAlreadyExists", "Text", "An inactive remote named \"{0}\" already exists.");
        SshKeyOpenFilter = Add("_sshKeyOpenFilter", "Text", "Private key (*.ppk)");
        SshKeyOpenCaption = Add("_sshKeyOpenCaption", "Text", "Select ssh key file");
        LocalBranchNameColumn = Add("BranchName", "HeaderText", "Local branch name");
        RemoteRepositoryColumn = Add("RemoteCombo", "HeaderText", "Remote repository");
        MergeWithColumn = Add("MergeWith", "HeaderText", "Default merge with");
        LocalBranchName = Add("label4", "Text", "&Local branch name");
        RemoteRepository = Add("label5", "Text", "&Remote repository");
        DefaultMergeWith = Add("label6", "Text", "&Default merge with");
        SaveDefaultPushPull = Add("SaveDefaultPushPull", "Text", "&Save changes");
    }

    public TranslatedText Title { get; }

    public TranslatedText RemotesTab { get; }

    public TranslatedText PullBehaviorTab { get; }

    public TranslatedText NewRemoteHeader { get; }

    public TranslatedText EditRemoteHeader { get; }

    public TranslatedText NewTooltip { get; }

    public TranslatedText DeleteTooltip { get; }

    public TranslatedText ActivateTooltip { get; }

    public TranslatedText DeactivateTooltip { get; }

    public TranslatedText ActiveGroup { get; }

    public TranslatedText InactiveGroup { get; }

    public TranslatedText Details { get; }

    public TranslatedText Name { get; }

    public TranslatedText UrlAsFetchPush { get; }

    public TranslatedText UrlAsFetch { get; }

    public TranslatedText Prefix { get; }

    public TranslatedText Color { get; }

    public TranslatedText SetColor { get; }

    public TranslatedText DefaultColor { get; }

    public TranslatedText SeparatePushUrl { get; }

    public TranslatedText PushUrl { get; }

    public TranslatedText BrowseUrl { get; }

    public TranslatedText BrowsePushUrl { get; }

    public TranslatedText PuttySsh { get; }

    public TranslatedText PrivateKeyFile { get; }

    public TranslatedText SshBrowse { get; }

    public TranslatedText LoadSshKey { get; }

    public TranslatedText TestConnection { get; }

    public TranslatedText Save { get; }

    public TranslatedText GitMessage { get; }

    public TranslatedText QuestionDeleteRemote { get; }

    public TranslatedText QuestionDeleteRemoteCaption { get; }

    public TranslatedText EnabledRemoteAlreadyExists { get; }

    public TranslatedText DisabledRemoteAlreadyExists { get; }

    public TranslatedText SshKeyOpenFilter { get; }

    public TranslatedText SshKeyOpenCaption { get; }

    public TranslatedText LocalBranchNameColumn { get; }

    public TranslatedText RemoteRepositoryColumn { get; }

    public TranslatedText MergeWithColumn { get; }

    public TranslatedText LocalBranchName { get; }

    public TranslatedText RemoteRepository { get; }

    public TranslatedText DefaultMergeWith { get; }

    public TranslatedText SaveDefaultPushPull { get; }
}

/// <summary>A remote as listed, with the group it starts (the equivalent of the <c>ListViewGroup</c>s).</summary>
/// <param name="Remote">The remote.</param>
/// <param name="GroupHeader">The header of the group (active or inactive remotes) this remote starts, if it does.</param>
public sealed record RemoteListItem(ConfigFileRemote Remote, string? GroupHeader)
{
    public string Name => Remote.Name ?? "";

    public bool IsDisabled => Remote.Disabled;
}

/// <summary>Operations of the remotes dialog that need the host (repository history, other dialogs, PuTTY).</summary>
public interface IRemotesHost
{
    /// <summary>The recently used remote URLs.</summary>
    IReadOnlyList<string> LoadUrlHistory();

    /// <summary>Replaces the old URLs by the new ones in the remote URL history (as <c>FormRemotesController.RemoteUpdate</c>).</summary>
    void UpdateUrlHistory(string? oldUrl, string newUrl, string? oldPushUrl, string? newPushUrl);

    /// <summary>After a remote was saved: reloads the remote colors, fetches the remote and notifies the repository changed.</summary>
    void OnRemoteSaved(string remoteName);

    /// <summary>Shows a color picker; returns the chosen color as HTML (e.g. <c>#FF0000</c>), or <see langword="null"/> if cancelled.</summary>
    string? PickColor(string? currentColor);

    /// <summary>Picks a PuTTY private key file.</summary>
    string? BrowseSshKey(string filter, string title);

    /// <summary>Starts Pageant with the key file, if configured.</summary>
    void LoadSshKey(string keyFile);

    /// <summary>Connects to the URL with plink, which asks to accept an unknown host key.</summary>
    void TestConnection(string url);

    /// <summary>The local branches (whose tracking remote and merge branch <see cref="IGitRef"/> writes to the git config).</summary>
    IReadOnlyList<IGitRef> LoadHeads();

    /// <summary>The remote branches of <paramref name="remoteName"/> (as <c>FormRemotes.DefaultMergeWithComboDropDown</c>).</summary>
    IReadOnlyList<string> GetMergeWithCandidates(string remoteName);
}

/// <summary>View model of the remotes dialog (port of <c>FormRemotes</c>).</summary>
public sealed partial class RemotesViewModel : DialogViewModel
{
    private const string NameToReplace = "TO_REPLACE";

    private readonly IConfigFileRemoteSettingsManager _remotesManager;
    private readonly IRemotesHost _host;
    private readonly IMessageBoxService _messageBoxes;
    private readonly IFileDialogService _fileDialogs;
    private readonly IGitBranchNameNormaliser _branchNameNormaliser;
    private readonly GitBranchNameOptions _prefixOptions;
    private readonly IReadOnlyList<string> _genericRemoteNames;
    private IReadOnlyList<ConfigFileRemote> _remotes = [];
    private IReadOnlyList<string> _urlHistory = [];
    private bool _loadingHead;

    /// <param name="prefixOptions">How the prefix is normalised (it may end with a slash).</param>
    /// <param name="isPuttyEnabled">Whether git uses PuTTY for SSH (<c>GitSshHelpers.IsPlink</c>).</param>
    /// <param name="showAdvancedOptions">Whether the color and the prefix are shown (<c>AppSettings.AlwaysShowAdvOpt</c>).</param>
    /// <param name="customGenericRemoteNames">Remote names that are not used to suggest URLs (<c>AppSettings.CustomGenericRemoteNames</c>).</param>
    public RemotesViewModel(
        RemotesStrings strings,
        IConfigFileRemoteSettingsManager remotesManager,
        IGitBranchNameNormaliser branchNameNormaliser,
        GitBranchNameOptions prefixOptions,
        bool isPuttyEnabled,
        bool showAdvancedOptions,
        IEnumerable<string> customGenericRemoteNames,
        IRemotesHost host,
        IMessageBoxService messageBoxes,
        IFileDialogService fileDialogs)
    {
        Strings = strings;
        _remotesManager = remotesManager;
        _branchNameNormaliser = branchNameNormaliser;
        _prefixOptions = prefixOptions;
        IsPuttyEnabled = isPuttyEnabled;
        ShowAdvancedOptions = showAdvancedOptions;
        _genericRemoteNames = ["origin", "upstream", "fork", "remote", "internal", .. customGenericRemoteNames];
        _host = host;
        _messageBoxes = messageBoxes;
        _fileDialogs = fileDialogs;
    }

    public RemotesStrings Strings { get; }

    public bool IsPuttyEnabled { get; }

    public bool ShowAdvancedOptions { get; }

    public ObservableCollection<RemoteListItem> Remotes { get; } = [];

    /// <summary>The URL history offered for the URL and the push URL, with suggestions for the entered remote.</summary>
    public ObservableCollection<string> UrlSuggestions { get; } = [];

    public ObservableCollection<string> PushUrlSuggestions { get; } = [];

    [ObservableProperty]
    public partial int SelectedTabIndex { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditing), nameof(ManagementHeader), nameof(IsManagementEnabled), nameof(ToggleStateTooltip), nameof(IsSelectedDisabled))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand), nameof(ToggleStateCommand))]
    public partial RemoteListItem? SelectedRemote { get; set; }

    /// <summary>Whether an existing remote is edited, rather than a new one created.</summary>
    public bool IsEditing => SelectedRemote is not null;

    public string ManagementHeader => IsEditing ? Strings.EditRemoteHeader.Text : Strings.NewRemoteHeader.Text;

    /// <summary>An inactive remote cannot be edited until it is activated.</summary>
    public bool IsManagementEnabled => SelectedRemote?.IsDisabled != true;

    public bool IsSelectedDisabled => SelectedRemote?.IsDisabled == true;

    public string ToggleStateTooltip => (IsSelectedDisabled ? Strings.ActivateTooltip.Text : Strings.DeactivateTooltip.Text).Trim();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string RemoteName { get; set; } = "";

    [ObservableProperty]
    public partial string Url { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UrlLabel))]
    public partial bool IsSeparatePushUrl { get; set; }

    public string UrlLabel => IsSeparatePushUrl ? Strings.UrlAsFetch.AccessKeyText : Strings.UrlAsFetchPush.AccessKeyText;

    [ObservableProperty]
    public partial string PushUrl { get; set; } = "";

    [ObservableProperty]
    public partial string PuttySshKey { get; set; } = "";

    [ObservableProperty]
    public partial string Prefix { get; set; } = "";

    /// <summary>The color of the remote's branches as HTML (<c>#RRGGBB</c>), or <see langword="null"/> for the default.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasColor))]
    public partial string? Color { get; set; }

    public bool HasColor => Color is not null;

    /// <summary>Loads the remotes and preselects a remote, or a local branch on the pull behavior tab (as <c>FormRemotes.Initialize</c>).</summary>
    public void Initialize(string? preselectRemote = null, string? preselectLocal = null)
    {
        _remotes = [.. _remotesManager.LoadRemotes(loadDisabled: true)];
        _urlHistory = _host.LoadUrlHistory();
        ReplaceAll(UrlSuggestions, _urlHistory);
        ReplaceAll(PushUrlSuggestions, _urlHistory);
        BindRemotes(preselectRemote);

        if (preselectLocal is not null && _remotes.Count != 0)
        {
            SelectedTabIndex = 1;
        }

        InitializePullBehavior(preselectLocal);
    }

    private void BindRemotes(string? preselectRemote)
    {
        List<RemoteListItem> items = [];
        AddGroup(_remotes.Where(r => !r.Disabled), Strings.ActiveGroup.Text);
        AddGroup(_remotes.Where(r => r.Disabled), Strings.InactiveGroup.Text);
        ReplaceAll(Remotes, items);

        // Preselect the remote, or else the first one (an active one, if any).
        SelectedRemote = Remotes.FirstOrDefault(r => r.Name == preselectRemote) ?? Remotes.FirstOrDefault();
        if (SelectedRemote is null)
        {
            ClearDetails();
        }

        void AddGroup(IEnumerable<ConfigFileRemote> remotes, string header)
            => items.AddRange(remotes.Select((remote, index) => new RemoteListItem(remote, index == 0 ? header : null)));
    }

    partial void OnSelectedRemoteChanged(RemoteListItem? value)
    {
        // As FormRemotes.Remotes_SelectedIndexChanged.
        ClearDetails();
        if (value?.Remote is not { } remote)
        {
            return;
        }

        RemoteName = remote.Name ?? "";
        Url = remote.Url ?? "";
        PushUrl = remote.PushUrl ?? "";
        IsSeparatePushUrl = !string.IsNullOrEmpty(remote.PushUrl);
        PuttySshKey = remote.PuttySshKey ?? "";
        Prefix = remote.Prefix ?? "";
        Color = string.IsNullOrWhiteSpace(remote.Color) ? null : remote.Color;
    }

    private void ClearDetails()
    {
        RemoteName = "";
        Url = "";
        PushUrl = "";
        IsSeparatePushUrl = false;
        PuttySshKey = "";
        Prefix = "";
        Color = null;
    }

    /// <summary>When the remote name is entered without one, suggests the owner of a hosted URL (as <c>FormRemotes.RemoteName_Enter</c>).</summary>
    public void SuggestNameFromUrl()
    {
        if (!string.IsNullOrEmpty(RemoteName) || string.IsNullOrEmpty(Url))
        {
            return;
        }

        if (new GitHostingRemoteParser().TryExtractGitHostingDataFromRemoteUrl(Url, out _, out string? owner, out _))
        {
            RemoteName = owner;
        }
    }

    /// <summary>
    ///  Adds URLs derived from the other remotes' URLs for the entered remote name to the URL suggestions
    ///  (as <c>FormRemotes.FillWithSomeGeneratedRemoteUrls</c>).
    /// </summary>
    /// <param name="push">Whether for the push URL rather than the URL.</param>
    public void SuggestUrls(bool push)
    {
        string remoteName = RemoteName;
        bool fillEmptyUrl = true;
        if (string.IsNullOrWhiteSpace(remoteName) || _genericRemoteNames.Contains(remoteName))
        {
            remoteName = NameToReplace;
            fillEmptyUrl = false;
        }

        HashSet<string> candidates = [];
        GitHostingRemoteParser parser = new();
        foreach (ConfigFileRemote remote in _remotes)
        {
            string? url = push ? remote.PushUrl : remote.Url;
            if (string.IsNullOrEmpty(url))
            {
                continue;
            }

            if (remote.Name is { } name && url.Contains(name))
            {
                candidates.Add(url.Replace($"{name}/", $"{remoteName}/"));
            }

            if (remote.Url is not null && parser.TryExtractGitHostingDataFromRemoteUrl(remote.Url, out _, out string? owner, out _))
            {
                candidates.Add(url.Replace($"{owner}/", $"{remoteName}/"));
            }
        }

        string current = push ? PushUrl : Url;
        List<string> proposed = [.. _urlHistory];
        bool added = false;
        foreach (string url in candidates.Where(url => !proposed.Contains(url)))
        {
            added = true;
            proposed.Insert(0, url);
        }

        if (!added)
        {
            return;
        }

        if (!string.IsNullOrEmpty(current))
        {
            proposed.Insert(0, current);
        }

        ReplaceAll(push ? PushUrlSuggestions : UrlSuggestions, proposed.Distinct());

        // Suggest the URL only for a specific remote name with a single candidate.
        if (string.IsNullOrEmpty(current) && fillEmptyUrl && candidates.Count == 1)
        {
            if (push)
            {
                PushUrl = candidates.First();
            }
            else
            {
                Url = candidates.First();
            }
        }
    }

    /// <summary>Normalises the prefix, which may end with a slash (as <c>txtRemotePrefix.Leave</c>).</summary>
    public void NormalisePrefix()
        => Prefix = _branchNameNormaliser.Normalise(Prefix, _prefixOptions);

    [RelayCommand]
    private void New()
    {
        SelectedRemote = null;
        ClearDetails();
    }

    private bool IsRemoteSelected() => SelectedRemote is not null;

    [RelayCommand(CanExecute = nameof(IsRemoteSelected))]
    private void Delete()
    {
        if (SelectedRemote is not { } selected
            || !_messageBoxes.Confirm(Strings.QuestionDeleteRemote.Text, Strings.QuestionDeleteRemoteCaption.Text, defaultNo: true))
        {
            return;
        }

        string output = _remotesManager.RemoveRemote(selected.Remote);
        if (!string.IsNullOrEmpty(output))
        {
            _messageBoxes.ShowInformation(output, Strings.GitMessage.Text);
        }

        // The URL stays in the history, so that the repository can still be cloned quickly.
        Initialize();
    }

    [RelayCommand(CanExecute = nameof(IsRemoteSelected))]
    private void ToggleState()
    {
        if (SelectedRemote?.Remote is not { Name: { } name } remote)
        {
            return;
        }

        remote.Disabled = !remote.Disabled;
        _remotesManager.ToggleRemoteState(name, remote.Disabled);
        Initialize(name);
    }

    private bool CanSave() => RemoteName.Trim().Length > 0;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        // As FormRemotes.SaveClick.
        string remoteName = RemoteName.Trim();
        string remoteUrl = Url.Trim();
        string remotePushUrl = PushUrl.Trim();
        ConfigFileRemote? selected = SelectedRemote?.Remote;
        try
        {
            if ((string.IsNullOrEmpty(remotePushUrl) && IsSeparatePushUrl)
                || (!string.IsNullOrEmpty(remotePushUrl) && remotePushUrl.Equals(remoteUrl, StringComparison.OrdinalIgnoreCase)))
            {
                IsSeparatePushUrl = false;
            }

            if (selected is null && !ValidateRemoteDoesNotExist(remoteName))
            {
                return;
            }

            ConfigFileRemoteSaveResult result = _remotesManager.SaveRemote(
                selected,
                remoteName,
                remoteUrl,
                IsSeparatePushUrl ? remotePushUrl : null,
                PuttySshKey,
                Color,
                Prefix);
            if (!string.IsNullOrEmpty(result.UserMessage))
            {
                _messageBoxes.ShowError(result.UserMessage, Strings.GitMessage.Text);
                return;
            }

            _host.UpdateUrlHistory(selected?.Url, remoteUrl, selected?.PushUrl, IsSeparatePushUrl ? remotePushUrl : null);
            _host.OnRemoteSaved(remoteName);
        }
        finally
        {
            Initialize(remoteName);
        }
    }

    private bool ValidateRemoteDoesNotExist(string remoteName)
    {
        if (_remotesManager.EnabledRemoteExists(remoteName))
        {
            _messageBoxes.ShowError(string.Format(Strings.EnabledRemoteAlreadyExists.Text, remoteName), Strings.GitMessage.Text);
            return false;
        }

        if (_remotesManager.DisabledRemoteExists(remoteName))
        {
            _messageBoxes.ShowError(string.Format(Strings.DisabledRemoteAlreadyExists.Text, remoteName), Strings.GitMessage.Text);
            return false;
        }

        return true;
    }

    [RelayCommand]
    private void PickColor()
    {
        if (_host.PickColor(Color) is { } color)
        {
            Color = color;
        }
    }

    [RelayCommand]
    private void ResetColor() => Color = null;

    [RelayCommand]
    private async Task BrowseUrlAsync()
    {
        if (await _fileDialogs.PickFolderAsync() is { } folder)
        {
            Url = folder;
        }
    }

    [RelayCommand]
    private async Task BrowsePushUrlAsync()
    {
        if (await _fileDialogs.PickFolderAsync() is { } folder)
        {
            PushUrl = folder;
        }
    }

    [RelayCommand]
    private void BrowseSshKey()
    {
        if (_host.BrowseSshKey($"{Strings.SshKeyOpenFilter.Text}|*.ppk", Strings.SshKeyOpenCaption.Text) is { } keyFile)
        {
            PuttySshKey = keyFile;
        }
    }

    [RelayCommand]
    private void LoadSshKey() => _host.LoadSshKey(PuttySshKey);

    [RelayCommand]
    private void TestConnection() => _host.TestConnection(Url);

    // The default pull behavior tab.

    public ObservableCollection<IGitRef> Heads { get; } = [];

    /// <summary>The remotes a branch can track; the first (empty) one for none.</summary>
    public ObservableCollection<string> TrackingRemoteNames { get; } = [];

    public ObservableCollection<string> MergeWithCandidates { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LocalBranchName), nameof(IsHeadSelected))]
    public partial IGitRef? SelectedHead { get; set; }

    public string LocalBranchName => SelectedHead?.Name ?? "";

    public bool IsHeadSelected => SelectedHead is not null;

    [ObservableProperty]
    public partial string? SelectedTrackingRemote { get; set; }

    [ObservableProperty]
    public partial string MergeWith { get; set; } = "";

    private void InitializePullBehavior(string? preselectLocal)
    {
        ReplaceAll(TrackingRemoteNames, ["", .. _remotes.Select(r => r.Name ?? "")]);
        ReplaceAll(Heads, _host.LoadHeads().OrderBy(r => r.LocalName));
        SelectedHead = Heads.FirstOrDefault(h => h.LocalName == preselectLocal) ?? Heads.FirstOrDefault();
    }

    partial void OnSelectedHeadChanged(IGitRef? value)
    {
        // As FormRemotes.RemoteBranchesSelectionChanged.
        _loadingHead = true;
        try
        {
            SelectedTrackingRemote = value is null
                ? null
                : TrackingRemoteNames.FirstOrDefault(name => name.Length > 0 && StringComparer.OrdinalIgnoreCase.Equals(name, value.TrackingRemote)) ?? "";
            MergeWith = value?.MergeWith ?? "";
            ReplaceAll(MergeWithCandidates, [""]);
        }
        finally
        {
            _loadingHead = false;
        }
    }

    /// <summary>Writes the tracking remote of the selected branch to the git config (as <c>RemoteRepositoryComboValidated</c>).</summary>
    partial void OnSelectedTrackingRemoteChanged(string? value)
    {
        if (!_loadingHead && SelectedHead is { } head)
        {
            head.TrackingRemote = value ?? "";
        }
    }

    /// <summary>Writes the merge branch of the selected branch to the git config (as <c>DefaultMergeWithComboValidated</c>).</summary>
    partial void OnMergeWithChanged(string value)
    {
        if (!_loadingHead && SelectedHead is { } head)
        {
            head.MergeWith = value;
        }
    }

    /// <summary>Offers the branches of the selected tracking remote to merge with (when the list is opened).</summary>
    public void LoadMergeWithCandidates()
    {
        List<string> candidates = [""];
        string remote = SelectedTrackingRemote?.Trim() ?? "";
        if (SelectedHead is { } head && !string.IsNullOrEmpty(head.TrackingRemote) && remote.Length > 0)
        {
            candidates.AddRange(_host.GetMergeWithCandidates(remote));
        }

        ReplaceAll(MergeWithCandidates, candidates);
    }

    /// <summary>Shows the saved tracking settings (they are written as edited, as in <c>FormRemotes</c>).</summary>
    [RelayCommand]
    private void SaveDefaultPushPull() => Initialize(SelectedRemote?.Name, SelectedHead?.LocalName);

    private static void ReplaceAll<T>(ObservableCollection<T> list, IEnumerable<T> items)
    {
        list.Clear();
        foreach (T item in items)
        {
            list.Add(item);
        }
    }
}
