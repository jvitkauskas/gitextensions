using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Editor;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;
using GitUIPluginInterfaces;

namespace GitUI.Presentation.UserControls.Blame;

/// <summary>Strings of the blame; ids match <c>BlameControl</c>.</summary>
public sealed class BlameStrings : ViewStrings
{
    public BlameStrings()
        : base("BlameControl")
    {
        BlameActualPreviousRevision = Add("_blameActualPreviousRevision", "Text", "&Blame previous revision");
        BlameVisiblePreviousRevision = Add("_blameVisiblePreviousRevision", "Text", "&Blame previous visible revision");
        BlameRevision = Add("blameRevisionToolStripMenuItem", "Text", "Blame &this revision");
        ShowChanges = Add("showChangesToolStripMenuItem", "Text", "&Show changes");
        CopyToClipboard = Add("copyToClipboardToolStripMenuItem", "Text", "&Copy to clipboard");
        CommitHash = Add("commitHashToolStripMenuItem", "Text", "Commit &hash");
        CommitMessage = Add("commitMessageToolStripMenuItem", "Text", "Commit &message");
        AllCommitInfo = Add("allCommitInfoToolStripMenuItem", "Text", "&All commit info");
    }

    public TranslatedText BlameActualPreviousRevision { get; }

    public TranslatedText BlameVisiblePreviousRevision { get; }

    public TranslatedText BlameRevision { get; }

    public TranslatedText ShowChanges { get; }

    public TranslatedText CopyToClipboard { get; }

    public TranslatedText CommitHash { get; }

    public TranslatedText CommitMessage { get; }

    public TranslatedText AllCommitInfo { get; }
}

/// <summary>Operations of the blame that need the host (git, dialogs, the clipboard).</summary>
public interface IBlameHost
{
    /// <summary>The display settings, read at each load.</summary>
    BlameDisplayOptions Options { get; }

    /// <summary>The blame of the file in the revision (<c>GitModule.Blame</c>); throws if git fails.</summary>
    Task<GitBlame> GetBlameAsync(string fileName, ObjectId objectId, CancellationToken cancellationToken);

    /// <summary>The revision of the commit info, if no grid lists it (<c>GitModule.GetRevision</c>).</summary>
    GitRevision? GetRevision(ObjectId objectId);

    /// <summary>The tooltip of a line: the commit with its summary (<c>GitBlameCommit.ToString(BuildSummary)</c>).</summary>
    string Describe(GitBlameCommit commit);

    /// <summary>As <c>GitBlameParser.GetOriginalLineInPreviousCommit</c>.</summary>
    int GetOriginalLineInPreviousCommit(GitRevision revision, string fileName, int line);

    /// <summary>Shows the changes of the commit (<c>FormCommitDiff</c>).</summary>
    void ShowCommitDiff(ObjectId objectId);

    /// <summary>Tells that the commit is not listed in the grid (<c>MessageBoxes.RevisionFilteredInGrid</c>).</summary>
    void ShowRevisionFiltered(ObjectId objectId);

    void CopyToClipboard(string text);

    /// <summary>
    ///  The items of the repository host plugin for a line (plugin API v2, <c>IBlameContextMenuProvider</c>), added at the end
    ///  of the context menu (as <c>ConfigureContextMenu</c> for the WinForms blame); none by default.
    /// </summary>
    /// <param name="fileName">The blamed file.</param>
    /// <param name="lineIndex">The 0-based index of the line of the menu.</param>
    /// <param name="blameId">The blamed revision.</param>
    IReadOnlyList<MenuModelItem> GetRepositoryHostMenuItems(string fileName, int lineIndex, ObjectId blameId) => [];

    /// <summary>
    ///  The avatar (PNG) of an author, or the placeholder (as <c>BlameControl</c> with <c>BlameShowAuthorAvatar</c>: the avatar
    ///  provider, <c>Images.User80</c> without an email); none by default.
    /// </summary>
    Task<byte[]?> GetAvatarAsync(string email, string? name, int size, CancellationToken cancellationToken) => Task.FromResult<byte[]?>(null);
}

/// <summary>The revision grid that shows the blamed revision (<c>IRevisionGridInfo</c> and <c>IRevisionGridFileUpdate</c>).</summary>
public interface IBlameRevisionGrid
{
    /// <summary>The revision if the grid lists it.</summary>
    GitRevision? GetRevision(ObjectId objectId);

    /// <summary>The revision with its real parents (a filtered grid rewrites them).</summary>
    GitRevision GetActualRevision(GitRevision revision);

    /// <summary>The revision with its real parents, from the grid or git.</summary>
    GitRevision? GetActualRevision(ObjectId objectId);

    /// <summary>Selects the revision (whose file is <paramref name="fileName"/>); <see langword="false"/> if it is not listed.</summary>
    bool SelectFileInRevision(ObjectId objectId, string fileName);
}

/// <summary>What the context menu of a blame line enables (as <c>BlameControl.contextMenu_Opened</c>).</summary>
public sealed record BlameMenuState(bool CanBlameRevision, bool CanBlamePreviousRevision, bool PreviousIsActual);

/// <summary>
///  View model of the blame (port of <c>BlameControl</c>; docs/avalonia-port/PLAN.md, phase 5): the commit info of the
///  selected line, the file with an author gutter, and the lines of the commit under the mouse highlighted.
/// </summary>
public sealed partial class BlameViewModel : ObservableObject
{
    private readonly IBlameHost _host;
    private CancellationTokenSource? _loading;
    private GitBlameLine? _lastBlameLine;
    private GitBlameLine? _clickedBlameLine;
    private (ObjectId Id, string FileName)? _loaded;

    public BlameViewModel(BlameStrings strings, IBlameHost host, ICommitInfoHost commitInfoHost)
    {
        Strings = strings;
        _host = host;
        CommitInfo = new CommitInfoViewModel(commitInfoHost);
        File.IsReadOnly = true;
    }

    public BlameStrings Strings { get; }

    public CommitInfoViewModel CommitInfo { get; }

    /// <summary>Whether the commit info is shown (<c>BlameControl.HideCommitInfo</c> hides it).</summary>
    public bool ShowCommitInfo { get; init; } = true;

    /// <summary>The revision grid of the window, if any; its revisions can be blamed.</summary>
    public IBlameRevisionGrid? RevisionGrid { get; set; }

    /// <summary>The blamed file.</summary>
    public TextEditorViewModel File { get; } = new();

    /// <summary>The blame, whose lines match the lines of <see cref="File"/>.</summary>
    [ObservableProperty]
    public partial GitBlame? Blame { get; private set; }

    /// <summary>The author line of each line: <see langword="null"/> where the previous line has the same commit.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<string?> AuthorLines { get; private set; } = [];

    /// <summary>The age bucket of each line (empty unless the avatars are shown).</summary>
    [ObservableProperty]
    public partial IReadOnlyList<int> AgeBuckets { get; private set; } = [];

    /// <summary>
    ///  The avatar (PNG) of the author at the first line of each commit, loaded after the blame (empty unless the avatars are
    ///  shown); the same array for the lines of an author.
    /// </summary>
    [ObservableProperty]
    public partial IReadOnlyList<byte[]?> Avatars { get; private set; } = [];

    /// <summary>The commit whose lines are highlighted (the one under the mouse).</summary>
    [ObservableProperty]
    public partial GitBlameCommit? HighlightedCommit { get; private set; }

    [ObservableProperty]
    public partial bool IsLoading { get; private set; }

    /// <summary>The blamed revision.</summary>
    public ObjectId BlameId { get; private set; }

    public string? FileName { get; private set; }

    /// <summary>As <c>BlameControl.LoadBlameAsync</c>: blames the file in the revision, unless already shown.</summary>
    /// <param name="initialLine">The line to show (1-based), unless a line was clicked to blame a revision.</param>
    public async Task LoadAsync(GitRevision revision, IReadOnlyList<ObjectId>? children, string fileName, int? initialLine = null, bool force = false)
    {
        ObjectId objectId = revision.ObjectId;
        if (!force && _loaded == (objectId, fileName))
        {
            if (initialLine is int lineToShow && !IsLoading)
            {
                File.Load(File.Text, fileName, lineToShow);
            }

            return;
        }

        int line = _clickedBlameLine?.OriginLineNumber ?? initialLine ?? (fileName == FileName ? CurrentLine : 1);
        _loaded = (objectId, fileName);
        FileName = fileName;

#pragma warning disable VSTHRD103 // CancelAsync may resume off the UI thread.
        _loading?.Cancel();
#pragma warning restore VSTHRD103
        _loading = new CancellationTokenSource();
        CancellationToken cancellationToken = _loading.Token;
        IsLoading = true;
        Blame = null;
        AuthorLines = [];
        AgeBuckets = [];
        Avatars = [];
        HighlightedCommit = null;
        File.Load("", fileName);

        GitBlame blame;
        try
        {
            blame = await _host.GetBlameAsync(fileName, objectId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                File.Load(ex.Message, fileName);
                IsLoading = false;
            }

            return;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        BlameDisplayOptions options = _host.Options;
        BlameContents contents = BlameContentsBuilder.Build(blame, fileName, options, DateTime.Now, CultureInfo.CurrentCulture);
        List<string?> authorLines = new(blame.Lines.Count);
        GitBlameCommit? lastCommit = null;
        foreach ((GitBlameLine blameLine, string gutterLine) in blame.Lines.Zip(contents.Gutter.Split(Environment.NewLine)))
        {
            authorLines.Add(blameLine.Commit == lastCommit ? null : gutterLine.TrimEnd());
            lastCommit = blameLine.Commit;
        }

        File.ShowLineNumbers = options.ShowLineNumbers;
        Blame = blame;
        AuthorLines = authorLines;
        AgeBuckets = contents.AgeBuckets;
        BlameId = objectId;
        _lastBlameLine = null;
        File.Load(contents.Body, fileName, Math.Min(line, blame.Lines.Count));
        _clickedBlameLine = null;
        if (ShowCommitInfo)
        {
            CommitInfo.SetRevision(revision, children);
        }

        IsLoading = false;
        if (options.ShowAuthorAvatar)
        {
            await LoadAvatarsAsync(blame, cancellationToken);
        }
    }

    // The size of the avatars requested, scaled down to the height of a line in the gutter.
    private const int AvatarSize = 32;

    // As BlameControl.ProcessBlame: the avatar of the author of the first line of each commit, one request per email.
    private async Task LoadAvatarsAsync(GitBlame blame, CancellationToken cancellationToken)
    {
        Dictionary<string, Task<byte[]?>> byEmail = [];
        Task<byte[]?>?[] requests = new Task<byte[]?>?[blame.Lines.Count];
        GitBlameCommit? lastCommit = null;
        for (int i = 0; i < blame.Lines.Count; i++)
        {
            GitBlameCommit commit = blame.Lines[i].Commit;
            if (commit != lastCommit)
            {
                string email = commit.AuthorMail?.Trim('<', '>') ?? "";
                if (!byEmail.TryGetValue(email, out Task<byte[]?>? request))
                {
                    request = GetAvatarAsync(email, commit.Author, cancellationToken);
                    byEmail[email] = request;
                }

                requests[i] = request;
            }

            lastCommit = commit;
        }

        await Task.WhenAll(byEmail.Values);
        if (!cancellationToken.IsCancellationRequested)
        {
#pragma warning disable VSTHRD103 // The tasks are complete.
            Avatars = [.. requests.Select(request => request?.Result)];
#pragma warning restore VSTHRD103
        }
    }

    private async Task<byte[]?> GetAvatarAsync(string email, string? name, CancellationToken cancellationToken)
    {
        try
        {
            return await _host.GetAvatarAsync(email, name, AvatarSize, cancellationToken);
        }
        catch (Exception)
        {
            // No avatar, as the WinForms blame when the provider fails.
            return null;
        }
    }

    /// <summary>The line of the caret (1-based), which the view reports.</summary>
    public int CurrentLine { get; private set; } = 1;

    /// <summary>The caret moved to <paramref name="line"/> (1-based): shows the commit of the line (as <c>SelectedLineChanged</c>).</summary>
    public void SelectLine(int line)
    {
        CurrentLine = line;
        if (Blame is null || line < 1 || line > Blame.Lines.Count)
        {
            return;
        }

        GitBlameLine newBlameLine = Blame.Lines[line - 1];
        if (ReferenceEquals(_lastBlameLine?.Commit, newBlameLine.Commit))
        {
            return;
        }

        _lastBlameLine = newBlameLine;
        if (ShowCommitInfo)
        {
            ObjectId objectId = newBlameLine.Commit.ObjectId;
            CommitInfo.SetRevision(RevisionGrid is null ? _host.GetRevision(objectId) : RevisionGrid.GetActualRevision(objectId));
        }
    }

    /// <summary>The mouse is over <paramref name="line"/> (1-based, or 0 if none): highlights the lines of its commit.</summary>
    public void HoverLine(int line) => HighlightedCommit = GetCommit(line);

    /// <summary>The tooltip of the gutter at <paramref name="line"/> (1-based).</summary>
    public string? GetToolTip(int line) => GetCommit(line) is { } commit ? _host.Describe(commit) : null;

    /// <summary>The commit of <paramref name="line"/> (1-based).</summary>
    public GitBlameCommit? GetCommit(int line)
        => Blame is { } blame && line >= 1 && line <= blame.Lines.Count ? blame.Lines[line - 1].Commit : null;

    /// <summary>The items of the repository host plugin for the 1-based <paramref name="line"/>, at the end of the context menu.</summary>
    public IReadOnlyList<MenuModelItem> GetRepositoryHostMenuItems(int line)
        => _loaded is { } loaded && line > 0 ? _host.GetRepositoryHostMenuItems(loaded.FileName, line - 1, loaded.Id) : [];

    /// <summary>As <c>contextMenu_Opened</c>: what the menu enables for the line under the mouse.</summary>
    public BlameMenuState GetMenuState(int line)
    {
        if (!TryGetRevision(GetCommit(line), out GitRevision? revision, out _))
        {
            return new BlameMenuState(CanBlameRevision: false, CanBlamePreviousRevision: false, PreviousIsActual: true);
        }

        // The parent of the actual revision, as the grid may rewrite them.
        if (RevisionHasParent(RevisionGrid!.GetActualRevision(revision)))
        {
            return new BlameMenuState(CanBlameRevision: true, CanBlamePreviousRevision: true, PreviousIsActual: true);
        }

        return new BlameMenuState(CanBlameRevision: true, CanBlamePreviousRevision: RevisionHasParent(revision), PreviousIsActual: false);

        bool RevisionHasParent(GitRevision? revision)
            => revision?.HasParent is true && RevisionGrid!.GetRevision(revision.FirstParentId) is not null;
    }

    /// <summary>A double click on the gutter blames the revision of the selected line (as <c>ActiveTextAreaControlDoubleClick</c>).</summary>
    public void BlameSelectedLineRevision()
    {
        if (_lastBlameLine is not null && TryGetRevision(_lastBlameLine.Commit, out _, out string? fileName))
        {
            BlameRevision(_lastBlameLine.Commit.ObjectId, fileName, _lastBlameLine);
        }
    }

    /// <summary>As <c>blameRevisionToolStripMenuItem_Click</c>.</summary>
    public void BlameRevisionOf(int line)
    {
        if (TryGetRevision(GetCommit(line), out GitRevision? revision, out string? fileName))
        {
            BlameRevision(revision.ObjectId, fileName, _lastBlameLine ?? Blame!.Lines[line - 1]);
        }
    }

    /// <summary>As <c>blamePreviousRevisionToolStripMenuItem_Click</c>.</summary>
    public void BlamePreviousRevisionOf(int line)
    {
        if (!TryGetRevision(GetCommit(line), out GitRevision? revision, out string? fileName))
        {
            return;
        }

        if (GetMenuState(line).PreviousIsActual)
        {
            revision = RevisionGrid!.GetActualRevision(revision);
        }

        // The origin line of the selected commit is the final line of the previous blame.
        GitBlameLine lastBlameLine = _lastBlameLine ?? Blame!.Lines[line - 1];
        int finalLineNumberOfPreviousBlame = lastBlameLine.OriginLineNumber;
        int originalLineNumberOfPreviousBlame = _host.GetOriginalLineInPreviousCommit(revision, fileName, finalLineNumberOfPreviousBlame);
        GitBlameLine blameLine = new(lastBlameLine.Commit, finalLineNumberOfPreviousBlame, originalLineNumberOfPreviousBlame, "Dummy Git blame line used only to store the good 'originLineNumber' value to display and select it");
        BlameRevision(revision.FirstParentId, fileName, blameLine);
    }

    /// <summary>As <c>showChangesToolStripMenuItem_Click</c>.</summary>
    public void ShowChangesOf(int line)
    {
        if (GetCommit(line) is { } commit)
        {
            _host.ShowCommitDiff(commit.ObjectId);
        }
    }

    public void CopyCommitHash(int line) => CopyToClipboard(line, c => c.ObjectId.ToString());

    public void CopyCommitMessage(int line) => CopyToClipboard(line, c => c.Summary);

    public void CopyAllCommitInfo(int line) => CopyToClipboard(line, c => c.ToString());

    private void CopyToClipboard(int line, Func<GitBlameCommit, string> formatter)
    {
        if (GetCommit(line) is { } commit)
        {
            _host.CopyToClipboard(formatter(commit));
        }
    }

    private bool TryGetRevision(GitBlameCommit? blameCommit, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out GitRevision? revision, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? fileName)
    {
        revision = blameCommit is null ? null : RevisionGrid?.GetRevision(blameCommit.ObjectId);
        fileName = blameCommit?.FileName;
        return revision is not null && fileName is not null;
    }

    /// <summary>As <c>BlameControl.BlameRevision</c>: selects the revision in the grid, or shows its changes.</summary>
    private void BlameRevision(ObjectId commitId, string fileName, GitBlameLine blameLine)
    {
        _clickedBlameLine = blameLine;
        if (RevisionGrid is not null)
        {
            if (!RevisionGrid.SelectFileInRevision(commitId, fileName))
            {
                _host.ShowRevisionFiltered(commitId);
            }

            return;
        }

        _host.ShowCommitDiff(commitId);
    }
}
