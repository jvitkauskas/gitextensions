using CommunityToolkit.Mvvm.ComponentModel;
using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Translations;
using GitUIPluginInterfaces;

namespace GitUI.Presentation.UserControls;

/// <summary>Strings of the menu of the avatar; ids match <c>AvatarControl</c>.</summary>
public sealed class AvatarStrings : ViewStrings
{
    public AvatarStrings()
        : base("AvatarControl")
    {
        ClearImageCache = Add("clearImagecacheToolStripMenuItem", "Text", "Clear image cache");
        AvatarProvider = Add("avatarProviderToolStripMenuItem", "Text", "Avatar provider");
        FallbackAvatarStyle = Add("fallbackAvatarStyleToolStripMenuItem", "Text", "Fallback generated avatar style");
        RegisterGravatar = Add("registerGravatarToolStripMenuItem", "Text", "Register at gravatar.com");
    }

    public TranslatedText ClearImageCache { get; }

    public TranslatedText AvatarProvider { get; }

    public TranslatedText FallbackAvatarStyle { get; }

    public TranslatedText RegisterGravatar { get; }
}

/// <summary>Strings of the commit info; ids match <c>CommitInfo</c>.</summary>
public sealed class CommitInfoStrings : ViewStrings
{
    public CommitInfoStrings()
        : base("CommitInfo")
    {
        BrokenRefs = Add("_brokenRefs", "Text", "The repository refs seem to be broken:");
        DerivesFromNoTag = Add("_derivesFromNoTag", "Text", "Derives from no tag");
        DerivesFromTag = Add("_derivesFromTag", "Text", "Derives from tag:");
        PlusCommits = Add("_plusCommits", "Text", "commits");
        RepoFailure = Add("_repoFailure", "Text", "Repository failure");
        LinksRelatedToRevision = Add("_trsLinksRelatedToRevision", "Text", "Related links:");
        CopyCommitInfo = Add("copyCommitInfoToolStripMenuItem", "Text", "&Copy commit info");
        CopyLink = Add("_copyLink", "Text", "Copy &link ({0})");
        AddNotes = Add("addNoteToolStripMenuItem", "Text", "Add &notes");
        ShowContainedInBranchesLocal = Add("showContainedInBranchesToolStripMenuItem", "Text", "Show local branches containing this commit");
        ShowContainedInBranchesRemote = Add("showContainedInBranchesRemoteToolStripMenuItem", "Text", "Show remote branches containing this commit");
        ShowContainedInBranchesRemoteIfNoLocal = Add("showContainedInBranchesRemoteIfNoLocalToolStripMenuItem", "Text", "Show remote branches only when no local branch contains this commit");
        ShowContainedInTags = Add("showContainedInTagsToolStripMenuItem", "Text", "Show tags containing this commit");
        ShowAnnotatedTagsMessages = Add("showMessagesOfAnnotatedTagsToolStripMenuItem", "Text", "Show messages of annotated tags");
        ShowTagThisCommitDerivesFrom = Add("showTagThisCommitDerivesFromMenuItem", "Text", "Show the most recent tag this commit derives from");
    }

    public TranslatedText BrokenRefs { get; }

    public TranslatedText DerivesFromNoTag { get; }

    public TranslatedText DerivesFromTag { get; }

    public TranslatedText PlusCommits { get; }

    public TranslatedText RepoFailure { get; }

    public TranslatedText LinksRelatedToRevision { get; }

    public TranslatedText CopyCommitInfo { get; }

    public TranslatedText CopyLink { get; }

    public TranslatedText AddNotes { get; }

    public TranslatedText ShowContainedInBranchesLocal { get; }

    public TranslatedText ShowContainedInBranchesRemote { get; }

    public TranslatedText ShowContainedInBranchesRemoteIfNoLocal { get; }

    public TranslatedText ShowContainedInTags { get; }

    public TranslatedText ShowAnnotatedTagsMessages { get; }

    public TranslatedText ShowTagThisCommitDerivesFrom { get; }
}

/// <summary>A line of the commit header: its label and its value as XHTML (e.g. a link to the author's e-mail).</summary>
public sealed record CommitInfoHeaderLine(string Label, string ValueXhtml);

/// <summary>What the commit info shows at once (as <c>CommitInfoHeader.ShowCommitInfo</c> and the fixed commit message).</summary>
public sealed record CommitInfoContent(IReadOnlyList<CommitInfoHeaderLine> Header, string MessageXhtml);

/// <summary>What the commit info shows below the message (the settings of its context menu).</summary>
public sealed record CommitInfoDisplayOptions(
    bool ShowContainedInBranchesLocal = true,
    bool ShowContainedInBranchesRemote = false,
    bool ShowContainedInBranchesRemoteIfNoLocal = false,
    bool ShowContainedInTags = true,
    bool ShowAnnotatedTagsMessages = true,
    bool ShowTagThisCommitDerivesFrom = true);

/// <summary>Renders the commit info with the WinForms renderers (<c>CommitDataHeaderRenderer</c>, <c>RefsFormatter</c>, ...).</summary>
public interface ICommitInfoHost
{
    /// <param name="showRevisionsAsLinks">Whether commit hashes are links (as <c>CommandClickedEvent is not null</c>).</param>
    CommitInfoContent Render(GitRevision revision, IReadOnlyList<ObjectId>? children, bool showRevisionsAsLinks);

    /// <summary>The commit message with its body and notes loaded (as <c>UpdateCommitMessageAsync</c>).</summary>
    Task<string> LoadMessageAsync(GitRevision revision, IReadOnlyList<ObjectId>? children, bool showRevisionsAsLinks, CancellationToken cancellationToken);

    /// <summary>
    ///  The annotated tags, related links, containing branches and tags and the tag the commit derives from, as XHTML
    ///  (as <c>StartAsyncDataLoad</c> and <c>UpdateRevisionInfo</c>).
    /// </summary>
    /// <param name="showAll">What is shown without a limit (<c>branches</c>, <c>tags</c>), after a "show all" link.</param>
    Task<string> LoadRevisionInfoAsync(GitRevision revision, bool showBranchesAsLinks, IReadOnlySet<string> showAll, CancellationToken cancellationToken);

    /// <summary>
    ///  Executes a link (as <c>LinkFactory.ExecuteLink</c>): opens a URL, or calls <paramref name="internalCommand"/> for an
    ///  internal link (<c>gitext://</c>) or <paramref name="showAll"/> for a "show all" link.
    /// </summary>
    void ExecuteLink(string uri, Action<string, string?>? internalCommand, Action<string?> showAll);

    /// <summary>The settings of the context menu (<c>AppSettings.CommitInfoShow*</c>); setting them saves them.</summary>
    CommitInfoDisplayOptions Options { get; set; }

    /// <summary>The strings of the commit info (its context menu).</summary>
    CommitInfoStrings Strings { get; }

    /// <summary>Whether the author's avatar is shown (<c>AppSettings.ShowAuthorAvatarInCommitInfo</c>).</summary>
    bool ShowAvatar { get; }

    /// <summary>The size of the avatar (<c>AppSettings.AuthorImageSizeInCommitInfo</c>).</summary>
    int AvatarSize { get; }

    /// <summary>The avatar of the author as an image file, or the default one (as <c>AvatarControl.UpdateAvatarAsync</c>).</summary>
    Task<byte[]?> GetAvatarAsync(string? email, string? name, CancellationToken cancellationToken);

    /// <summary>Whether the avatar has the menu of <c>AvatarControl</c> (the provider, the fallback style, the cache); none by default.</summary>
    bool HasAvatarMenu => false;

    /// <summary>The provider of the avatars (<c>AppSettings.AvatarProvider</c>).</summary>
    AvatarProvider AvatarProvider
    {
        get => default;
        set
        {
        }
    }

    /// <summary>The style of the generated avatars (<c>AppSettings.AvatarFallbackType</c>).</summary>
    AvatarFallbackType AvatarFallbackType
    {
        get => default;
        set
        {
        }
    }

    /// <summary>As <c>AvatarControl.ClearCache</c>: the provider of the settings, and its cache cleared.</summary>
    Task ClearAvatarCacheAsync() => Task.CompletedTask;

    /// <summary>"Register at gravatar.com": the site in the default browser.</summary>
    void OpenUrl(string url)
    {
    }

    /// <summary>The cache of the avatars was cleared, maybe by another window (<c>IAvatarCacheCleaner.CacheCleared</c>); raised on the UI thread.</summary>
    event EventHandler? AvatarsCleared
    {
        add { }
        remove { }
    }

    /// <summary>As <c>addNoteToolStripMenuItem_Click</c>: edits the notes of the commit (<c>GitModule.EditNotes</c>).</summary>
    void EditNotes(ObjectId objectId);

    /// <summary>The copied commit info from the texts of the header and the message (as <c>copyCommitInfoToolStripMenuItem_Click</c>).</summary>
    string GetCopyText(string header, string message);

    void CopyToClipboard(string text);
}

/// <summary>
///  View model of the commit info (port of the WinForms <c>CommitInfo</c> and <c>CommitInfoHeader</c>;
///  docs/avalonia-port/PLAN.md, phase 5): the header, the message and the related refs of a commit.
/// </summary>
public sealed partial class CommitInfoViewModel : ObservableObject
{
    private readonly ICommitInfoHost _host;
    private readonly HashSet<string> _showAll = [];
    private GitRevision? _revision;
    private IReadOnlyList<ObjectId>? _children;
    private CancellationTokenSource? _loading;

    public CommitInfoViewModel(ICommitInfoHost host)
    {
        _host = host;
    }

    /// <summary>Whether branches and tags are links (as <c>CommitInfo.ShowBranchesAsLinks</c>).</summary>
    public bool ShowBranchesAsLinks { get; init; }

    [ObservableProperty]
    public partial IReadOnlyList<CommitInfoHeaderLine> Header { get; private set; } = [];

    [ObservableProperty]
    public partial string Message { get; private set; } = "";

    [ObservableProperty]
    public partial string RevisionInfo { get; private set; } = "";

    [ObservableProperty]
    public partial bool HasRevision { get; private set; }

    /// <summary>The author's avatar as an image file, if shown (<c>CommitInfoHeader.LoadAuthorImage</c>).</summary>
    [ObservableProperty]
    public partial byte[]? Avatar { get; private set; }

    [ObservableProperty]
    public partial bool ShowAvatar { get; private set; }

    /// <summary>The size of the avatar (in device-independent pixels).</summary>
    public int AvatarSize => _host.AvatarSize;

    /// <summary>Whether the avatar has its menu (<c>AvatarControl</c>).</summary>
    public bool HasAvatarMenu => _host.HasAvatarMenu;

    public AvatarStrings AvatarStrings => field ??= ViewStrings.Load<AvatarStrings>();

    public AvatarProvider AvatarProvider => _host.AvatarProvider;

    public AvatarFallbackType AvatarFallbackType => _host.AvatarFallbackType;

    /// <summary>As the items of "Avatar provider": the provider saved, and the cache cleared.</summary>
    public Task SetAvatarProviderAsync(AvatarProvider provider)
    {
        _host.AvatarProvider = provider;
        return ClearAvatarCacheAsync();
    }

    /// <summary>As the items of "Fallback generated avatar style": the style saved, and the cache cleared.</summary>
    public Task SetAvatarFallbackTypeAsync(AvatarFallbackType fallbackType)
    {
        _host.AvatarFallbackType = fallbackType;
        return ClearAvatarCacheAsync();
    }

    /// <summary>As <c>AvatarControl.ClearCache</c>: the avatar loaded again once the cache is cleared.</summary>
    public async Task ClearAvatarCacheAsync()
    {
        await _host.ClearAvatarCacheAsync();
        if (_revision is { } revision)
        {
            await LoadAvatarAsync(revision);
        }
    }

    /// <summary>As <c>OnRegisterGravatarClick</c>.</summary>
    public void RegisterAtGravatar() => _host.OpenUrl("https://www.gravatar.com");

    /// <summary>
    ///  As <c>OnCacheCleared</c>: the avatar is loaded again when the cache is cleared elsewhere, while <paramref name="watch"/>
    ///  (the view is shown).
    /// </summary>
    public void WatchAvatarCache(bool watch)
    {
        _host.AvatarsCleared -= OnAvatarsCleared;
        if (watch)
        {
            _host.AvatarsCleared += OnAvatarsCleared;
        }
    }

    private void OnAvatarsCleared(object? sender, EventArgs e)
    {
        if (_revision is { } revision)
        {
            _ = LoadAvatarAsync(revision);
        }
    }

    /// <summary>The settings of the context menu, as its check boxes.</summary>
    public CommitInfoDisplayOptions Options => _host.Options;

    public CommitInfoStrings Strings => _host.Strings;

    /// <summary>Saves the settings of the context menu and reloads the commit (as their click handlers and <c>ReloadCommitInfo</c>).</summary>
    public void SetOptions(CommitInfoDisplayOptions options)
    {
        _host.Options = options;
        OnPropertyChanged(nameof(Options));
        SetRevision(_revision, _children);
    }

    /// <summary>As <c>copyCommitInfoToolStripMenuItem_Click</c>: the header and the message as text.</summary>
    public void CopyCommitInfo()
    {
        string header = string.Join("\n", Header.Select(line => $"{line.Label}\t{XhtmlText.ToPlainText(line.ValueXhtml)}"));
        _host.CopyToClipboard(_host.GetCopyText(header, XhtmlText.ToPlainText(Message)));
    }

    /// <summary>As <c>copyLinkToolStripMenuItem_Click</c>.</summary>
    public void CopyLink(string uri) => _host.CopyToClipboard(uri);

    /// <summary>As <c>addNoteToolStripMenuItem_Click</c>: the notes are edited, then the commit is reloaded with them.</summary>
    public void AddNotes()
    {
        if (_revision is null)
        {
            return;
        }

        _host.EditNotes(_revision.ObjectId);
        _revision.Body = null;
        _revision.Notes = null;
        SetRevision(_revision, _children);
    }

    /// <summary>
    ///  Raised for an internal link, with its command and data (as <c>CommitInfo.CommandClicked</c>); commit hashes are links
    ///  only if handled.
    /// </summary>
    public event EventHandler<(string Command, string? Data)>? CommandClicked;

    /// <summary>As <c>CommitInfo.SetRevisionWithChildren</c>.</summary>
    public void SetRevision(GitRevision? revision, IReadOnlyList<ObjectId>? children = null)
    {
        _revision = revision;
        _children = children;
        _showAll.Clear();
        HasRevision = revision is not null;
        if (revision is null)
        {
            Cancel();
            Header = [];
            Message = "";
            RevisionInfo = "";
            return;
        }

        CommitInfoContent content = _host.Render(revision, children, ShowRevisionsAsLinks);
        Header = content.Header;
        Message = content.MessageXhtml;
        RevisionInfo = "";
        _ = LoadAsync(revision, loadMessage: !revision.IsArtificial && !revision.IsAutostash);
        _ = LoadAvatarAsync(revision);
    }

    private CancellationTokenSource? _loadingAvatar;

    /// <summary>As <c>CommitInfoHeader.LoadAuthorImage</c> and <c>AvatarControl.UpdateAvatarAsync</c>.</summary>
    private async Task LoadAvatarAsync(GitRevision revision)
    {
        ShowAvatar = _host.ShowAvatar;
#pragma warning disable VSTHRD103 // CancelAsync may resume off the UI thread.
        _loadingAvatar?.Cancel();
#pragma warning restore VSTHRD103
        if (!ShowAvatar)
        {
            Avatar = null;
            return;
        }

        CancellationTokenSource loading = new();
        _loadingAvatar = loading;
        try
        {
            byte[]? avatar = await _host.GetAvatarAsync(revision.AuthorEmail ?? revision.CommitterEmail, revision.Author ?? revision.Committer, loading.Token);
            if (!loading.IsCancellationRequested)
            {
                Avatar = avatar;
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
            // As AvatarControl: no avatar if it cannot be loaded.
            if (!loading.IsCancellationRequested)
            {
                Avatar = null;
            }
        }
    }

    /// <summary>A click on a link (as <c>LinkClicked</c>).</summary>
    public void OnLinkClicked(string uri)
        => _host.ExecuteLink(
            uri,
            CommandClicked is null ? null : (command, data) => CommandClicked?.Invoke(this, (command, data)),
            ShowAll);

    private bool ShowRevisionsAsLinks => CommandClicked is not null;

    /// <summary>As <c>ShowAll</c>: the branches or tags without a limit.</summary>
    private void ShowAll(string? what)
    {
        if (what is null || _revision is null)
        {
            return;
        }

        _showAll.Add(what);
        _ = LoadAsync(_revision, loadMessage: false);
    }

    private void Cancel()
    {
        CancellationTokenSource? previous = _loading;
        _loading = null;
        if (previous is not null)
        {
            // Synchronously: awaiting CancelAsync could continue off the UI thread, and no long callbacks are registered.
#pragma warning disable VSTHRD103 // Call async methods when in an async method
            previous.Cancel();
#pragma warning restore VSTHRD103

            // Not disposed: the work in the background may still use its token (a disposed source throws ObjectDisposedException).
        }
    }

    /// <summary>As <c>ReloadCommitInfo</c>: no refs for artificial commits.</summary>
    private async Task LoadAsync(GitRevision revision, bool loadMessage)
    {
        Cancel();
        if (revision.IsArtificial || revision.IsAutostash)
        {
            return;
        }

        CancellationTokenSource loading = new();
        _loading = loading;
        try
        {
            if (loadMessage)
            {
                string message = await _host.LoadMessageAsync(revision, _children, ShowRevisionsAsLinks, loading.Token);
                if (loading.IsCancellationRequested)
                {
                    return;
                }

                Message = message;
            }

            string revisionInfo = await _host.LoadRevisionInfoAsync(revision, ShowBranchesAsLinks, _showAll, loading.Token);
            if (!loading.IsCancellationRequested)
            {
                RevisionInfo = revisionInfo;
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
