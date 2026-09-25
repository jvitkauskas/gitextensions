using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitExtensions.Extensibility.Plugins;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs.RepoHosting;

/// <summary>Strings of the fork and clone dialog; ids match <c>ForkAndCloneForm</c>, its <c>FolderBrowserButton</c> and <c>TranslatedStrings</c>.</summary>
public sealed class ForkAndCloneStrings : ViewStrings
{
    public ForkAndCloneStrings()
        : base("ForkAndCloneForm")
    {
        Title = Add("$this", "Text", "Remote repository fork and clone");
        MyRepositoriesTab = Add("myReposPage", "Text", "My repositories");
        SearchRepositoriesTab = Add("searchReposPage", "Text", "Search for repositories");
        NameColumn = Add("columnHeaderMyReposName", "Text", "Name");
        IsForkColumn = Add("columnHeaderMyReposIsFork", "Text", "Is fork");
        ForksColumn = Add("columnHeaderMyReposForks", "Text", "# Forks");
        IsPrivateColumn = Add("columnHeaderMyReposIsPrivate", "Text", "Private");
        SearchNameColumn = Add("columnHeaderSearchName", "Text", "Name");
        SearchOwnerColumn = Add("columnHeaderSearchOwner", "Text", "Owner");
        SearchIsForkColumn = Add("columnHeaderSearchIsFork", "Text", "Is fork");
        SearchForksColumn = Add("columnHeaderSearchForks", "Text", "# Forks");
        HelpText = Add("helpTextLbl", "Text", "If you want to fork a repository owned by somebody else, go to the Search for repositories tab.");
        Search = Add("searchBtn", "Text", "Search");
        Or = Add("orLbl", "Text", "or");
        GetFromUser = Add("getFromUserBtn", "Text", "Get from user");
        Fork = Add("forkBtn", "Text", "Fork!");
        Description = Add("descriptionLbl", "Text", "Description:");
        OpenHomepage = Add("openGitupPageBtn", "Text", "Open github page");
        CloneGroup = Add("cloneSetupGB", "Text", "Clone");
        DestinationFolder = Add("label1", "Text", "Destination folder:");
        CreateDirectory = Add("createDirectoryLbl", "Text", "Create directory:");
        AddUpstreamRemoteAs = Add("label3", "Text", "Add upstream remote as:");
        Protocol = Add("ProtocolLabel", "Text", "Protocol:");
        Depth = Add("depthLabel", "Text", "Limit Depth:");
        Clone = Add("cloneBtn", "Text", "Clone");
        Loading = Add("_strLoading", "Text", " : LOADING : ");
        FailedToGetRepos = Add("_strFailedToGetRepos", "Text", "Failed to get repositories. This most likely means you didn't configure {0}, please do so via the menu \"Plugins/{0}\".");
        WillCloneWithPushAccess = Add("_strWillCloneWithPushAccess", "Text", "Will clone {0} into {1}.\nYou will have push access. {2}");
        WillCloneInfo = Add("_strWillCloneInfo", "Text", "Will clone {0} into {1}.\nYou can not push unless you are a collaborator. {2}");
        WillBeAddedAsARemote = Add("_strWillBeAddedAsARemote", "Text", "\"{0}\" will be added as a remote.");
        CouldNotAddRemote = Add("_strCouldNotAddRemote", "Text", "Could not add remote");
        NoHomepageDefined = Add("_strNoHomepageDefined", "Text", "No homepage defined");
        FailedToFork = Add("_strFailedToFork", "Text", "Failed to fork:");
        SearchFailed = Add("_strSearchFailed", "Text", "Search failed!");
        UserNotFound = Add("_strUserNotFound", "Text", "User not found!");
        CouldNotFetchReposOfUser = Add("_strCouldNotFetchReposOfUser", "Text", "Could not fetch repositories of user!");
        Searching = Add("_strSearching", "Text", " : SEARCHING : ");
        SelectOneItem = Add("_strSelectOneItem", "Text", "You must select exactly one item");
        CloneFolderCanNotBeEmpty = Add("_strCloneFolderCanNotBeEmpty", "Text", "Clone folder can not be empty");
        Browse = Add("buttonBrowse", "Text", "&Browse...", category: "FolderBrowserButton");
        Close = Add("_closeText", "Text", "Close", category: "TranslatedStrings");
        Yes = Add("_yes", "Text", "Yes", category: "TranslatedStrings");
        No = Add("_no", "Text", "No", category: "TranslatedStrings");
        Error = Add("_error", "Text", "Error", category: "TranslatedStrings");
    }

    public TranslatedText Title { get; }

    public TranslatedText MyRepositoriesTab { get; }

    public TranslatedText SearchRepositoriesTab { get; }

    public TranslatedText NameColumn { get; }

    public TranslatedText IsForkColumn { get; }

    public TranslatedText ForksColumn { get; }

    public TranslatedText IsPrivateColumn { get; }

    public TranslatedText SearchNameColumn { get; }

    public TranslatedText SearchOwnerColumn { get; }

    public TranslatedText SearchIsForkColumn { get; }

    public TranslatedText SearchForksColumn { get; }

    public TranslatedText HelpText { get; }

    public TranslatedText Search { get; }

    public TranslatedText Or { get; }

    public TranslatedText GetFromUser { get; }

    public TranslatedText Fork { get; }

    public TranslatedText Description { get; }

    public TranslatedText OpenHomepage { get; }

    public TranslatedText CloneGroup { get; }

    public TranslatedText DestinationFolder { get; }

    public TranslatedText CreateDirectory { get; }

    public TranslatedText AddUpstreamRemoteAs { get; }

    public TranslatedText Protocol { get; }

    public TranslatedText Depth { get; }

    public TranslatedText Clone { get; }

    public TranslatedText Loading { get; }

    public TranslatedText FailedToGetRepos { get; }

    public TranslatedText WillCloneWithPushAccess { get; }

    public TranslatedText WillCloneInfo { get; }

    public TranslatedText WillBeAddedAsARemote { get; }

    public TranslatedText CouldNotAddRemote { get; }

    public TranslatedText NoHomepageDefined { get; }

    public TranslatedText FailedToFork { get; }

    public TranslatedText SearchFailed { get; }

    public TranslatedText UserNotFound { get; }

    public TranslatedText CouldNotFetchReposOfUser { get; }

    public TranslatedText Searching { get; }

    public TranslatedText SelectOneItem { get; }

    public TranslatedText CloneFolderCanNotBeEmpty { get; }

    public TranslatedText Browse { get; }

    public TranslatedText Close { get; }

    public TranslatedText Yes { get; }

    public TranslatedText No { get; }

    public TranslatedText Error { get; }
}

/// <summary>A row of the repository lists; a row without repository is the "loading" or "searching" placeholder.</summary>
public sealed record HostedRepositoryRow(IHostedRepository? Repository, string Name, string Owner = "", string IsFork = "", string Forks = "", string IsPrivate = "");

/// <summary>Operations of the fork and clone dialog that need the host (git and the application).</summary>
public interface IForkAndCloneHost
{
    /// <summary>As <c>ForkAndCloneForm.Init</c>: the default clone destination, else the parent of the last repository.</summary>
    string? GetDefaultDestination();

    /// <summary>Clones <paramref name="cloneUrl"/> into <paramref name="targetDirectory"/> in the progress dialog; returns whether it succeeded.</summary>
    bool Clone(string cloneUrl, string targetDirectory, int? depth);

    /// <summary>Adds a remote to the cloned repository; returns the error, if any.</summary>
    string AddRemote(string repositoryDirectory, string name, string url);

    /// <summary>Opens the cloned repository (<c>gitModuleChanged</c>).</summary>
    void OpenRepository(string repositoryDirectory);

    void OpenUrl(string url);
}

/// <summary>View model of the fork and clone dialog (port of <c>ForkAndCloneForm</c>).</summary>
public sealed partial class ForkAndCloneViewModel : DialogViewModel
{
    private const string UpstreamRemoteName = "upstream";

    private readonly IRepositoryHostPlugin _gitHoster;
    private readonly IForkAndCloneHost _host;
    private readonly IBackgroundRunner _backgroundRunner;
    private readonly IMessageBoxService _messageBoxes;
    private readonly IFileDialogService _fileDialogs;
    private bool _updatingCloneInfo;

    public ForkAndCloneViewModel(
        ForkAndCloneStrings strings,
        IRepositoryHostPlugin gitHoster,
        IForkAndCloneHost host,
        IBackgroundRunner backgroundRunner,
        IMessageBoxService messageBoxes,
        IFileDialogService fileDialogs)
    {
        Strings = strings;
        _gitHoster = gitHoster;
        _host = host;
        _backgroundRunner = backgroundRunner;
        _messageBoxes = messageBoxes;
        _fileDialogs = fileDialogs;
        Title = $"{gitHoster.Name}: {strings.Title.PlainText}";
        HelpText = strings.HelpText.Text;
    }

    public ForkAndCloneStrings Strings { get; }

    public string Title { get; }

    public ObservableCollection<HostedRepositoryRow> MyRepositories { get; } = [];

    public ObservableCollection<HostedRepositoryRow> SearchResults { get; } = [];

    public ObservableCollection<string> UpstreamRemoteNames { get; } = [];

    /// <summary>0 for "My repositories", 1 for "Search for repositories".</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSearchTab))]
    public partial int SelectedTabIndex { get; set; }

    public bool IsSearchTab => SelectedTabIndex == 1;

    [ObservableProperty]
    public partial HostedRepositoryRow? SelectedMyRepository { get; set; }

    [ObservableProperty]
    public partial HostedRepositoryRow? SelectedSearchResult { get; set; }

    [ObservableProperty]
    public partial string HelpText { get; private set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand), nameof(GetFromUserCommand))]
    public partial bool CanSearch { get; private set; } = true;

    [ObservableProperty]
    public partial string SelectedDescription { get; private set; } = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ForkCommand))]
    public partial bool CanFork { get; private set; }

    [ObservableProperty]
    public partial string Destination { get; set; } = "";

    [ObservableProperty]
    public partial string CreateDirectory { get; set; } = "";

    [ObservableProperty]
    public partial string AddUpstreamRemote { get; set; } = "";

    [ObservableProperty]
    public partial bool CanChooseUpstreamRemote { get; private set; }

    [ObservableProperty]
    public partial IReadOnlyList<GitProtocol> Protocols { get; private set; } = [];

    [ObservableProperty]
    public partial GitProtocol? SelectedProtocol { get; set; }

    [ObservableProperty]
    public partial bool ShowProtocols { get; private set; }

    [ObservableProperty]
    public partial decimal? Depth { get; set; } = 0;

    [ObservableProperty]
    public partial string CloneInfoText { get; private set; } = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloneCommand))]
    public partial bool CanClone { get; private set; }

    /// <summary>The repository selected in the list of the current tab (<c>CurrentySelectedGitRepo</c>).</summary>
    public IHostedRepository? CurrentRepository => (IsSearchTab ? SelectedSearchResult : SelectedMyRepository)?.Repository;

    /// <summary>As <c>ForkAndCloneForm_Load</c> (<c>Init</c>).</summary>
    public void Load()
    {
        if (_host.GetDefaultDestination() is { } destination)
        {
            Destination = destination;
        }

        UpdateCloneInfo();
        _ = UpdateMyRepositoriesAsync();
    }

    partial void OnSelectedTabIndexChanged(int value) => UpdateCloneInfo();

    partial void OnSelectedMyRepositoryChanged(HostedRepositoryRow? value) => UpdateCloneInfo();

    /// <summary>As <c>_searchResultsLV_SelectedIndexChanged</c>.</summary>
    partial void OnSelectedSearchResultChanged(HostedRepositoryRow? value)
    {
        UpdateCloneInfo();
        CanFork = value?.Repository is not null;
        if (value?.Repository is { } repository)
        {
            SelectedDescription = repository.Description;
        }
    }

    partial void OnDestinationChanged(string value) => UpdateCloneInfo(updateCreateDirectory: false, updateProtocols: false);

    partial void OnCreateDirectoryChanged(string value) => UpdateCloneInfo(updateCreateDirectory: false, updateProtocols: false);

    partial void OnAddUpstreamRemoteChanged(string value) => UpdateCloneInfo(updateCreateDirectory: false, updateProtocols: false);

    /// <summary>As <c>ProtocolSelectionChanged</c>.</summary>
    partial void OnSelectedProtocolChanged(GitProtocol? value)
    {
        if (!_updatingCloneInfo && CurrentRepository is { } repository && value is { } protocol)
        {
            repository.CloneProtocol = protocol;
            SetCloneInfoText(repository);
        }
    }

    /// <summary>As <c>UpdateMyRepos</c>.</summary>
    private async Task UpdateMyRepositoriesAsync()
    {
        MyRepositories.Clear();
        MyRepositories.Add(new HostedRepositoryRow(null, Strings.Loading.Text));

        try
        {
            IReadOnlyList<IHostedRepository> repositories = await _backgroundRunner.RunAsync(() => _gitHoster.GetMyRepos());
            MyRepositories.Clear();
            foreach (IHostedRepository repository in repositories.OrderBy(r => r.Name))
            {
                MyRepositories.Add(new HostedRepositoryRow(
                    repository,
                    repository.Name,
                    IsFork: repository.IsAFork ? Strings.Yes.Text : Strings.No.Text,
                    Forks: repository.Forks.ToString(),
                    IsPrivate: repository.IsPrivate ? Strings.Yes.Text : Strings.No.Text));
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            MyRepositories.Clear();
            HelpText = string.Format(Strings.FailedToGetRepos.Text, _gitHoster.Name) + "\r\n\r\nException: " + ex.Message + "\r\n\r\n" + HelpText;
        }
    }

    /// <summary>As <c>_searchBtn_Click</c>.</summary>
    [RelayCommand(CanExecute = nameof(CanSearch))]
    private Task SearchAsync()
        => SearchAsync(search => _gitHoster.SearchForRepository(search), ex => _messageBoxes.ShowError(Strings.SearchFailed.Text + Environment.NewLine + ex.Message, Strings.Error.Text));

    /// <summary>As <c>_getFromUserBtn_Click</c>.</summary>
    [RelayCommand(CanExecute = nameof(CanSearch))]
    private Task GetFromUserAsync()
        => SearchAsync(
            search => _gitHoster.GetRepositoriesOfUser(search.Trim()),
            ex => _messageBoxes.ShowError(
                ex.Message.Contains("404") ? Strings.UserNotFound.Text : Strings.CouldNotFetchReposOfUser.Text + Environment.NewLine + ex.Message,
                Strings.Error.Text));

    private async Task SearchAsync(Func<string, IReadOnlyList<IHostedRepository>> search, Action<Exception> reportError)
    {
        string text = SearchText;
        if (text.Trim().Length == 0)
        {
            return;
        }

        // As PrepareSearch.
        SearchResults.Clear();
        SelectedSearchResult = null;
        CanSearch = false;
        SearchResults.Add(new HostedRepositoryRow(null, Strings.Searching.Text));

        try
        {
            IReadOnlyList<IHostedRepository> repositories = await _backgroundRunner.RunAsync(() => search(text));

            // As HandleSearchResult.
            SearchResults.Clear();
            foreach (IHostedRepository repository in repositories.OrderBy(r => r.Name))
            {
                SearchResults.Add(new HostedRepositoryRow(
                    repository,
                    repository.Name,
                    Owner: repository.Owner ?? "",
                    IsFork: repository.IsAFork ? Strings.Yes.Text : Strings.No.Text,
                    Forks: repository.Forks.ToString()));
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            reportError(ex);
        }

        CanSearch = true;
    }

    /// <summary>As <c>_forkBtn_Click</c>.</summary>
    [RelayCommand(CanExecute = nameof(CanFork))]
    private Task ForkAsync()
    {
        if (SelectedSearchResult?.Repository is not { } repository)
        {
            _messageBoxes.ShowError(Strings.SelectOneItem.Text, Strings.Error.Text);
            return Task.CompletedTask;
        }

        try
        {
            repository.Fork();
        }
        catch (Exception ex)
        {
            _messageBoxes.ShowError(Strings.FailedToFork.Text + Environment.NewLine + ex.Message, Strings.Error.Text);
        }

        SelectedTabIndex = 0;
        return UpdateMyRepositoriesAsync();
    }

    /// <summary>As <c>_browseForCloneToDirbtn_Click</c>.</summary>
    [RelayCommand]
    private async Task BrowseAsync()
    {
        string initialDirectory = Destination.Length > 0 ? Destination
            : OperatingSystem.IsWindows() ? "C:\\" : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (await _fileDialogs.PickFolderAsync(initialDirectory) is { } folder)
        {
            Destination = folder;
        }
    }

    /// <summary>As <c>_openGitupPageBtn_Click</c>.</summary>
    [RelayCommand]
    private void OpenHomepage()
    {
        if (CurrentRepository is not { } repository)
        {
            return;
        }

        string homepage = repository.Homepage;
        if (string.IsNullOrEmpty(homepage) || (!homepage.StartsWith("http://") && !homepage.StartsWith("https://")))
        {
            _messageBoxes.ShowError(Strings.NoHomepageDefined.Text, Strings.Error.Text);
        }
        else
        {
            _host.OpenUrl(homepage);
        }
    }

    /// <summary>As <c>_cloneBtn_Click</c> (<c>Clone</c>).</summary>
    [RelayCommand(CanExecute = nameof(CanClone))]
    private void Clone()
    {
        if (CurrentRepository is not { } repository || GetTargetDirectory(validate: true) is not { } targetDirectory)
        {
            return;
        }

        int? depth = Depth is > 0 ? (int)Depth.Value : null;
        if (!_host.Clone(repository.CloneUrl, targetDirectory, depth))
        {
            return;
        }

        string upstreamRemote = AddUpstreamRemote.Trim();
        if (upstreamRemote.Length > 0 && !string.IsNullOrEmpty(repository.ParentUrl))
        {
            string error = _host.AddRemote(targetDirectory, upstreamRemote, repository.ParentUrl);
            if (!string.IsNullOrEmpty(error))
            {
                _messageBoxes.ShowError(error, Strings.CouldNotAddRemote.Text);
            }
        }

        _host.OpenRepository(targetDirectory);
        Close(accepted: true);
    }

    [RelayCommand]
    private void CloseDialog() => Close(accepted: true);

    /// <summary>As <c>UpdateCloneInfo</c>.</summary>
    private void UpdateCloneInfo(bool updateCreateDirectory = true, bool updateProtocols = true)
    {
        if (_updatingCloneInfo)
        {
            return;
        }

        _updatingCloneInfo = true;
        try
        {
            if (CurrentRepository is { } repository)
            {
                bool multipleProtocols = repository.SupportedCloneProtocols.Any();
                if (multipleProtocols && updateProtocols)
                {
                    GitProtocol currentSelection = SelectedProtocol ?? repository.SupportedCloneProtocols[0];
                    Protocols = repository.SupportedCloneProtocols;
                    if (repository.SupportedCloneProtocols.Contains(currentSelection))
                    {
                        repository.CloneProtocol = currentSelection;
                    }

                    SelectedProtocol = repository.CloneProtocol;
                }

                ShowProtocols = multipleProtocols;

                if (updateCreateDirectory)
                {
                    CreateDirectory = repository.Name;
                    AddUpstreamRemote = "";
                    UpstreamRemoteNames.Clear();
                    if (repository.ParentOwner is { } parentOwner)
                    {
                        UpstreamRemoteNames.Add(parentOwner);
                        UpstreamRemoteNames.Add(UpstreamRemoteName);
                        AddUpstreamRemote = parentOwner;
                    }

                    CanChooseUpstreamRemote = repository.ParentOwner is not null;
                }

                CanClone = true;
                SetCloneInfoText(repository);
            }
            else
            {
                ShowProtocols = false;
                CanClone = false;
                CloneInfoText = "";
                CreateDirectory = "";
            }
        }
        finally
        {
            _updatingCloneInfo = false;
        }
    }

    /// <summary>As <c>SetCloneInfoText</c>.</summary>
    private void SetCloneInfoText(IHostedRepository repository)
    {
        string moreInfo = !string.IsNullOrEmpty(AddUpstreamRemote) ? string.Format(Strings.WillBeAddedAsARemote.Text, AddUpstreamRemote.Trim()) : "";
        string format = IsSearchTab ? Strings.WillCloneInfo.Text : Strings.WillCloneWithPushAccess.Text;
        CloneInfoText = string.Format(format, repository.CloneUrl, GetTargetDirectory(validate: false), moreInfo);
    }

    /// <summary>As <c>GetTargetDir</c>; only cloning reports an empty destination (the WinForms info text did too).</summary>
    private string? GetTargetDirectory(bool validate)
    {
        string targetDirectory = Destination.Trim();
        if (targetDirectory.Length == 0)
        {
            if (validate)
            {
                _messageBoxes.ShowError(Strings.CloneFolderCanNotBeEmpty.Text, Strings.Error.Text);
            }

            return null;
        }

        return Path.Combine(targetDirectory, CreateDirectory);
    }
}
