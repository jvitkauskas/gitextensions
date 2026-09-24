using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Plugins;
using GitUI.Presentation.Editor;
using GitUI.Presentation.Services;
using GitUI.Presentation.SpellChecker;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUIPluginInterfaces;

namespace GitUI.Presentation.CommandsDialogs.RepoHosting;

/// <summary>Strings of the pull requests dialog; ids match <c>ViewPullRequestsForm</c> and <c>TranslatedStrings</c>.</summary>
public sealed class ViewPullRequestsStrings : ViewStrings
{
    public ViewPullRequestsStrings()
        : base("ViewPullRequestsForm")
    {
        Title = Add("$this", "Text", "View Pull Requests");
        ChooseRepository = Add("_chooseRepo", "Text", "Choose repository:");
        HeadingColumn = Add("columnHeaderHeading", "Text", "Heading");
        ByColumn = Add("columnHeaderBy", "Text", "By");
        CreatedColumn = Add("columnHeaderCreated", "Text", "Created");
        BranchColumn = Add("columnHeaderBranch", "Text", "Will be fetched to branch");
        Fetch = Add("_fetchBtn", "Text", "Fetch to pr/ branch");
        AddRemoteAndFetch = Add("_addAndFetchBtn", "Text", "Add remote and fetch");
        ClosePullRequest = Add("_closePullRequestBtn", "Text", "Close pull request");
        DiffsTab = Add("tabPage1", "Text", "Diffs");
        CommentsTab = Add("tabPage2", "Text", "Comments");
        Refresh = Add("_refreshCommentsBtn", "Text", "Refresh");
        PostComment = Add("_postComment", "Text", "Post comment");
        FailedToFetchPullData = Add("_strFailedToFetchPullData", "Text", "Failed to fetch pull data!");
        FailedToPostDiscussionItem = Add("_strFailedToLoadDiscussionItem", "Text", "Failed to post discussion item!");
        FailedToClosePullRequest = Add("_strFailedToClosePullRequest", "Text", "Failed to close pull request!");
        FailedToLoadDiffData = Add("_strFailedToLoadDiffData", "Text", "Failed to load diff data!");
        CouldNotLoadDiscussion = Add("_strCouldNotLoadDiscussion", "Text", "Could not load discussion!");
        Loading = Add("_strLoading", "Text", " : LOADING : ");
        UnableUnderstandPatch = Add("_strUnableUnderstandPatch", "Text", "Error: Unable to understand patch");
        RemoteAlreadyExist = Add("_strRemoteAlreadyExist", "Text", "ERROR: Remote with name {0} already exists but it points to a different repository!\nDetails: Is {1} expected {2}");
        CouldNotAddRemote = Add("_strCouldNotAddRemote", "Text", "Could not add remote with name {0} and URL {1}");
        RemoteIgnore = Add("_strRemoteIgnore", "Text", "Remote ignored");
        Error = Add("_error", "Text", "Error", category: "TranslatedStrings");
        RemoteInError = Add("_remoteInError", "Text", "{0}\n\nRemote: {1}", category: "TranslatedStrings");
    }

    public TranslatedText Title { get; }

    public TranslatedText ChooseRepository { get; }

    public TranslatedText HeadingColumn { get; }

    public TranslatedText ByColumn { get; }

    public TranslatedText CreatedColumn { get; }

    public TranslatedText BranchColumn { get; }

    public TranslatedText Fetch { get; }

    public TranslatedText AddRemoteAndFetch { get; }

    public TranslatedText ClosePullRequest { get; }

    public TranslatedText DiffsTab { get; }

    public TranslatedText CommentsTab { get; }

    public TranslatedText Refresh { get; }

    public TranslatedText PostComment { get; }

    public TranslatedText FailedToFetchPullData { get; }

    public TranslatedText FailedToPostDiscussionItem { get; }

    public TranslatedText FailedToClosePullRequest { get; }

    public TranslatedText FailedToLoadDiffData { get; }

    public TranslatedText CouldNotLoadDiscussion { get; }

    public TranslatedText Loading { get; }

    public TranslatedText UnableUnderstandPatch { get; }

    public TranslatedText RemoteAlreadyExist { get; }

    public TranslatedText CouldNotAddRemote { get; }

    public TranslatedText RemoteIgnore { get; }

    public TranslatedText Error { get; }

    public TranslatedText RemoteInError { get; }
}

/// <summary>Operations of the pull requests dialog that need the repository and the application.</summary>
public interface IViewPullRequestsHost
{
    /// <summary>The remote of the current branch (<c>GetCurrentRemote</c>); empty for a local branch.</summary>
    string GetCurrentRemote();

    /// <summary>As <c>SelectHostedRepositoryForCurrentRemote</c>: HTTPS if the fetch URL of the remote uses HTTP, else SSH.</summary>
    GitProtocol GetCloneProtocol(string currentRemote);

    /// <summary>Runs git in the progress dialog (<c>FormProcess.ShowDialog</c>); returns whether it succeeded.</summary>
    bool RunGit(string arguments);

    /// <summary>Adds a remote (<c>GitModule.AddRemote</c>); returns the error, if any.</summary>
    string AddRemote(string name, string url);

    /// <summary>Stops the notifications of repository changes (<c>RepoChangedNotifier.Lock</c>) until <see cref="UnlockRepoChanged"/>.</summary>
    void LockRepoChanged();

    void UnlockRepoChanged();

    void NotifyRepoChanged();
}

/// <summary>A row of the pull request list; a row without pull request is the "loading" placeholder.</summary>
public sealed record PullRequestRow(IPullRequestInformation? PullRequest, string Id, string Title, string Owner = "", string Created = "", string FetchBranch = "");

/// <summary>An entry of the discussion of a pull request (as <c>DiscussionHtmlCreator</c> shows it).</summary>
/// <param name="CommitSha">The commit of a commit entry (<c>ICommitDiscussionEntry</c>); <see langword="null"/> for a comment.</param>
public sealed record DiscussionEntryItem(string Author, string Created, string Body, string? CommitSha)
{
    public bool IsCommit => CommitSha is not null;

    /// <summary>As the heading of the entry: "Commit: " and the hash.</summary>
    public string CommitText => $"Commit:  {CommitSha}";
}

/// <summary>View model of the pull requests dialog (port of <c>ViewPullRequestsForm</c>).</summary>
public sealed partial class ViewPullRequestsViewModel : DialogViewModel
{
    private readonly IRepositoryHostPlugin _gitHoster;
    private readonly IViewPullRequestsHost _host;
    private readonly IBackgroundRunner _backgroundRunner;
    private readonly IMessageBoxService _messageBoxes;
    private readonly Dictionary<string, string> _diffCache = [];
    private IReadOnlyList<IHostedRemote> _hostedRemotes = [];
    private GitProtocol _cloneGitProtocol;
    private bool _isFirstLoad;
    private IPullRequestInformation? _currentPullRequest;
    private IPullRequestDiscussion? _discussion;

    public ViewPullRequestsViewModel(
        ViewPullRequestsStrings strings,
        IRepositoryHostPlugin gitHoster,
        IViewPullRequestsHost host,
        IFileViewerHost fileViewerHost,
        FileStatusListStrings fileStatusListStrings,
        FileStatusTreeOptions fileStatusTreeOptions,
        IBackgroundRunner backgroundRunner,
        IMessageBoxService messageBoxes,
        ISpellCheckHost? spellCheckHost = null)
    {
        Strings = strings;
        _gitHoster = gitHoster;
        _host = host;
        _backgroundRunner = backgroundRunner;
        _messageBoxes = messageBoxes;
        Files = new FileStatusListViewModel(fileStatusListStrings, fileStatusTreeOptions);
        Viewer = new FileViewerViewModel(fileViewerHost);

        // As FileViewer_TopScrollReached and FileViewer_BottomScrollReached.
        Viewer.ScrollOnThrough(() => Files);
        Files.SelectionChanged += (_, _) => ShowSelectedFile();
        SpellCheck = spellCheckHost is null ? null : new SpellCheckViewModel(ViewStrings.Load<SpellCheckStrings>(), spellCheckHost);
    }

    public ViewPullRequestsStrings Strings { get; }

    public FileStatusListViewModel Files { get; }

    public FileViewerViewModel Viewer { get; }

    /// <summary>The comment to post (<c>_postCommentText</c>, an <c>EditNetSpell</c>).</summary>
    public TextEditorViewModel Comment { get; } = new() { ShowLineNumbers = false };

    public SpellCheckViewModel? SpellCheck { get; }

    public ObservableCollection<HostedRemoteItem> HostedRemotes { get; } = [];

    public ObservableCollection<PullRequestRow> PullRequests { get; } = [];

    [ObservableProperty]
    public partial HostedRemoteItem? SelectedHostedRemote { get; set; }

    [ObservableProperty]
    public partial bool CanSelectHostedRemote { get; private set; } = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(FetchCommand), nameof(AddRemoteAndFetchCommand), nameof(ClosePullRequestCommand), nameof(RefreshDiscussionCommand), nameof(PostCommentCommand))]
    public partial PullRequestRow? SelectedPullRequest { get; set; }

    /// <summary>The discussion of the selected pull request.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<DiscussionEntryItem> Discussion { get; private set; } = [];

    /// <summary>Whether the remotes are being loaded (the WinForms form was masked).</summary>
    [ObservableProperty]
    public partial bool IsLoading { get; private set; }

    private bool HasPullRequest => SelectedPullRequest?.PullRequest is not null;

    /// <summary>As <c>ViewPullRequestsForm_Load</c>: the hosted repositories are read in the background.</summary>
    public async Task LoadAsync()
    {
        _isFirstLoad = true;
        IsLoading = true;

        (IHostedRemote[] hostedRemotes, List<(IHostedRemote Remote, Exception Error)> errors) = await _backgroundRunner.RunAsync(() =>
        {
            IHostedRemote[] remotes = [.. _gitHoster.GetHostedRemotesForModule()];
            List<(IHostedRemote, Exception)> failures = [];
            foreach (IHostedRemote hostedRemote in remotes)
            {
                try
                {
                    // Read now, in the background.
                    hostedRemote.GetHostedRepository();
                }
                catch (Exception ex)
                {
                    failures.Add((hostedRemote, ex));
                }
            }

            return (remotes, failures);
        });

        foreach ((IHostedRemote remote, Exception error) in errors)
        {
            _messageBoxes.ShowError(string.Format(Strings.RemoteInError.Text, error.Message, remote.DisplayData), Strings.RemoteIgnore.Text);
        }

        _hostedRemotes = hostedRemotes;
        HostedRemotes.Clear();
        foreach (IHostedRemote remote in hostedRemotes)
        {
            HostedRemotes.Add(new HostedRemoteItem(remote));
        }

        SelectHostedRepositoryForCurrentRemote();
        IsLoading = false;
    }

    partial void OnSelectedHostedRemoteChanged(HostedRemoteItem? value) => _ = LoadPullRequestsAsync();

    partial void OnSelectedPullRequestChanged(PullRequestRow? value) => ShowPullRequest(value?.PullRequest);

    /// <summary>As <c>SelectHostedRepositoryForCurrentRemote</c>.</summary>
    private void SelectHostedRepositoryForCurrentRemote()
    {
        string currentRemote = _host.GetCurrentRemote();
        _cloneGitProtocol = _host.GetCloneProtocol(currentRemote);
        SelectedHostedRemote = HostedRemotes.FirstOrDefault(item => string.Equals(item.Remote.Name, currentRemote, StringComparison.OrdinalIgnoreCase))
            ?? HostedRemotes.FirstOrDefault();
    }

    /// <summary>As <c>_selectedOwner_SelectedIndexChanged</c>.</summary>
    private async Task LoadPullRequestsAsync()
    {
        PullRequests.Clear();
        IHostedRepository? hostedRepository;
        try
        {
            hostedRepository = SelectedHostedRemote?.Remote.GetHostedRepository();
        }
        catch (Exception)
        {
            // If this remote fails to load, the next one is selected.
            SelectNextHostedRepositoryIfFirstLoad();
            return;
        }

        if (hostedRepository is null)
        {
            SelectNextHostedRepositoryIfFirstLoad();
            return;
        }

        CanSelectHostedRemote = false;

        // As ResetAllAndShowLoadingPullRequests.
        ShowPullRequest(null);
        PullRequests.Clear();
        PullRequests.Add(new PullRequestRow(null, "", Strings.Loading.Text));

        try
        {
            IReadOnlyList<IPullRequestInformation> pullRequests = await _backgroundRunner.RunAsync(hostedRepository.GetPullRequests);
            SetPullRequestsData(pullRequests);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _messageBoxes.ShowError(Strings.FailedToFetchPullData.Text + Environment.NewLine + ex.Message, Strings.Error.Text);
        }

        CanSelectHostedRemote = true;

        void SelectNextHostedRepositoryIfFirstLoad()
        {
            if (_isFirstLoad)
            {
                SelectNextHostedRepository();
            }
        }
    }

    /// <summary>As <c>SetPullRequestsData</c> and <c>LoadListView</c>.</summary>
    private void SetPullRequestsData(IReadOnlyList<IPullRequestInformation> infos)
    {
        if (_isFirstLoad)
        {
            if (infos.Count == 0 && _hostedRemotes.Count > 0)
            {
                SelectNextHostedRepository();
                return;
            }

            _isFirstLoad = false;
        }

        PullRequests.Clear();
        foreach (IPullRequestInformation info in infos)
        {
            PullRequests.Add(new PullRequestRow(info, info.Id, info.Title, info.Owner, info.Created.ToString(), info.FetchBranch));
        }

        SelectedPullRequest = PullRequests.FirstOrDefault();
    }

    /// <summary>As <c>SelectNextHostedRepository</c>.</summary>
    private void SelectNextHostedRepository()
    {
        int index = SelectedHostedRemote is null ? -1 : HostedRemotes.IndexOf(SelectedHostedRemote);
        if (index + 1 < HostedRemotes.Count)
        {
            SelectedHostedRemote = HostedRemotes[index + 1];
        }
    }

    /// <summary>As <c>_pullRequestsList_SelectedIndexChanged</c>: the diff and the discussion of the pull request.</summary>
    private void ShowPullRequest(IPullRequestInformation? pullRequest)
    {
        IPullRequestInformation? previous = _currentPullRequest;
        _currentPullRequest = pullRequest;
        if (pullRequest is null)
        {
            _discussion = null;
            Discussion = [];
            Viewer.Show(FileViewContent.Empty);
            Files.Clear();
            return;
        }

        if (previous?.Equals(pullRequest) is true)
        {
            return;
        }

        pullRequest.HeadRepo.CloneProtocol = _cloneGitProtocol;
        _discussion = null;
        Discussion = [];
        Viewer.Show(FileViewContent.Empty);
        Files.Clear();

        _ = LoadDiffPatchAsync(pullRequest);
        _ = LoadDiscussionAsync(pullRequest, reload: false);
    }

    /// <summary>As <c>LoadDiscussion</c>.</summary>
    private async Task LoadDiscussionAsync(IPullRequestInformation pullRequest, bool reload)
    {
        try
        {
            IPullRequestDiscussion discussion = await _backgroundRunner.RunAsync(() =>
            {
                IPullRequestDiscussion loaded = reload && _discussion is { } current ? current : pullRequest.GetDiscussion();
                if (reload)
                {
                    loaded.ForceReload();
                }

                return loaded;
            });

            if (_currentPullRequest == pullRequest)
            {
                _discussion = discussion;
                Discussion = ToItems(discussion.Entries);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _messageBoxes.ShowError(Strings.CouldNotLoadDiscussion.Text + Environment.NewLine + ex.Message, Strings.Error.Text);
            Discussion = [];
        }
    }

    /// <summary>As <c>DiscussionHtmlCreator.CreateFor</c>: the texts of the entries.</summary>
    private static IReadOnlyList<DiscussionEntryItem> ToItems(IEnumerable<IDiscussionEntry>? entries)
        => [.. (entries ?? []).Select(entry => new DiscussionEntryItem(
            entry.Author ?? "[UNKNOWN]",
            entry.Created.ToString(),
            (entry.Body ?? "[UNKNOWN]").Replace("\r", ""),
            entry is ICommitDiscussionEntry commit ? commit.Sha ?? "[UNKNOWN]" : null))];

    /// <summary>As <c>LoadDiffPatch</c>.</summary>
    private async Task LoadDiffPatchAsync(IPullRequestInformation pullRequest)
    {
        try
        {
            string content = await pullRequest.GetDiffDataAsync();
            if (_currentPullRequest == pullRequest)
            {
                SplitAndLoadDiff(content, pullRequest.BaseSha, pullRequest.HeadSha);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _messageBoxes.ShowError(Strings.FailedToLoadDiffData.Text + Environment.NewLine + ex.Message, Strings.Error.Text);
        }
    }

    [GeneratedRegex(@"(?:\n|^)diff --git ", RegexOptions.ExplicitCapture)]
    private static partial Regex DiffCommandRegex { get; }

    [GeneratedRegex(@"^a/([^\n]+) b/(?<name>[^\n]+)\s*(?<value>.*)$", RegexOptions.Singleline | RegexOptions.ExplicitCapture)]
    private static partial Regex FilePartRegex { get; }

    /// <summary>As <c>SplitAndLoadDiff</c>: the patch split into its files.</summary>
    public void SplitAndLoadDiff(string diffData, string baseSha, string secondSha)
    {
        _diffCache.Clear();

        List<string> fileParts = [.. DiffCommandRegex.Split(diffData).Where(el => el?.Trim().Length is > 10)];
        List<GitItemStatus> statuses = [];

        // The base is the commit merged into (e.g. "master"); the commits of the pull request may not exist in the local repository.
        GitRevision? firstRevision = ObjectId.TryParse(baseSha, out ObjectId firstId) ? new GitRevision(firstId) : null;
        GitRevision? secondRevision = ObjectId.TryParse(secondSha, out ObjectId secondId) ? new GitRevision(secondId) : null;
        if (secondRevision is null)
        {
            _messageBoxes.ShowError(Strings.UnableUnderstandPatch.Text, Strings.Error.Text);
            return;
        }

        foreach (string part in fileParts)
        {
            Match match = FilePartRegex.Match(part);
            if (!match.Success)
            {
                _messageBoxes.ShowError(Strings.UnableUnderstandPatch.Text, Strings.Error.Text);
                return;
            }

            GitItemStatus status = new(name: match.Groups["name"].Value.Trim())
            {
                IsChanged = true,
                IsNew = false,
                IsDeleted = false,
                IsTracked = true,
                Staged = StagedStatus.None
            };

            statuses.Add(status);
            _diffCache[status.Name] = match.Groups["value"].Value;
        }

        Files.SetDiff(firstRevision, secondRevision, statuses);
    }

    /// <summary>As <c>_fileStatusList_SelectedIndexChanged</c>: the part of the patch of the file.</summary>
    private void ShowSelectedFile()
    {
        if (Files.SelectedEntry?.Item is not { } item || !_diffCache.TryGetValue(item.Name, out string? data))
        {
            return;
        }

        Viewer.Show(item.IsSubmodule
            ? new FileViewContent(FileViewKind.Text, data, item.Name)
            : new FileViewContent(FileViewKind.Diff, data, item.Name));
    }

    /// <summary>As <c>_fetchBtn_Click</c>.</summary>
    [RelayCommand(CanExecute = nameof(HasPullRequest))]
    private void Fetch()
    {
        if (_currentPullRequest is not { } pullRequest)
        {
            return;
        }

        string arguments = string.Format("fetch --no-tags --progress {0} {1}:{2}", pullRequest.HeadRepo.CloneUrl, pullRequest.HeadRef, pullRequest.FetchBranch);
        if (!_host.RunGit(arguments))
        {
            return;
        }

        _host.NotifyRepoChanged();
        Close(accepted: true);
    }

    /// <summary>As <c>_addAsRemoteAndFetch_Click</c>.</summary>
    [RelayCommand(CanExecute = nameof(HasPullRequest))]
    private void AddRemoteAndFetch()
    {
        if (_currentPullRequest is not { } pullRequest)
        {
            return;
        }

        _host.LockRepoChanged();
        try
        {
            string remoteName = pullRequest.Owner;
            string remoteUrl = pullRequest.HeadRepo.CloneUrl;
            string remoteRef = pullRequest.HeadRef;

            if (_hostedRemotes.FirstOrDefault(remote => remote.Name == remoteName) is { } existingRemote)
            {
                IHostedRepository hostedRepository = existingRemote.GetHostedRepository();
                hostedRepository.CloneProtocol = _cloneGitProtocol;
                if (hostedRepository.CloneUrl != remoteUrl)
                {
                    _messageBoxes.ShowError(string.Format(Strings.RemoteAlreadyExist.Text, remoteName, hostedRepository.CloneUrl, remoteUrl), Strings.Error.Text);
                    return;
                }
            }
            else
            {
                string error = _host.AddRemote(remoteName, remoteUrl);
                if (!string.IsNullOrEmpty(error))
                {
                    _messageBoxes.ShowError(error, string.Format(Strings.CouldNotAddRemote.Text, remoteName, remoteUrl));
                    return;
                }

                _host.NotifyRepoChanged();
            }

            if (!_host.RunGit(string.Format("fetch --no-tags --progress {0} {1}:{0}/{1}", remoteName, remoteRef)))
            {
                return;
            }

            _host.NotifyRepoChanged();

            if (_host.RunGit(string.Format("checkout {0}/{1}", remoteName, remoteRef)))
            {
                _host.NotifyRepoChanged();
            }
        }
        finally
        {
            _host.UnlockRepoChanged();
        }

        Close(accepted: true);
    }

    /// <summary>As <c>_closePullRequestBtn_Click</c>.</summary>
    [RelayCommand(CanExecute = nameof(HasPullRequest))]
    private Task ClosePullRequestAsync()
    {
        if (_currentPullRequest is not { } pullRequest)
        {
            return Task.CompletedTask;
        }

        try
        {
            pullRequest.Close();
            return LoadPullRequestsAsync();
        }
        catch (Exception ex)
        {
            _messageBoxes.ShowError(Strings.FailedToClosePullRequest.Text + Environment.NewLine + ex.Message, Strings.Error.Text);
            return Task.CompletedTask;
        }
    }

    /// <summary>The refresh button of the comments, which has no handler in the WinForms form: loads the discussion again.</summary>
    [RelayCommand(CanExecute = nameof(HasPullRequest))]
    private Task RefreshDiscussionAsync()
        => _currentPullRequest is { } pullRequest ? LoadDiscussionAsync(pullRequest, reload: true) : Task.CompletedTask;

    /// <summary>
    ///  The post button of the comments, which has no handler in the WinForms form: posts the comment
    ///  (<c>IPullRequestDiscussion.Post</c>, with its translated error), then loads the discussion again.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasPullRequest))]
    private async Task PostCommentAsync()
    {
        string text = Comment.Text.Trim();
        if (_currentPullRequest is not { } pullRequest || _discussion is not { } discussion || text.Length == 0)
        {
            return;
        }

        try
        {
            await _backgroundRunner.RunAsync(() =>
            {
                discussion.Post(text);
                return true;
            });
            Comment.Text = "";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _messageBoxes.ShowError(Strings.FailedToPostDiscussionItem.Text + Environment.NewLine + ex.Message, Strings.Error.Text);
            return;
        }

        await LoadDiscussionAsync(pullRequest, reload: true);
    }
}
