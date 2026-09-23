using CommunityToolkit.Mvvm.ComponentModel;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Translations;
using GitUIPluginInterfaces;

namespace GitUI.Presentation.UserControls;

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
    }

    public TranslatedText BrokenRefs { get; }

    public TranslatedText DerivesFromNoTag { get; }

    public TranslatedText DerivesFromTag { get; }

    public TranslatedText PlusCommits { get; }

    public TranslatedText RepoFailure { get; }

    public TranslatedText LinksRelatedToRevision { get; }

    public TranslatedText CopyCommitInfo { get; }
}

/// <summary>A line of the commit header: its label and its value as XHTML (e.g. a link to the author's e-mail).</summary>
public sealed record CommitInfoHeaderLine(string Label, string ValueXhtml);

/// <summary>What the commit info shows at once (as <c>CommitInfoHeader.ShowCommitInfo</c> and the fixed commit message).</summary>
public sealed record CommitInfoContent(IReadOnlyList<CommitInfoHeaderLine> Header, string MessageXhtml);

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
            previous.Dispose();
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
