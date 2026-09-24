using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.BuildServerIntegration;
using GitExtensions.Extensibility.Git;
using GitUI.UserControls.RevisionGrid.Graph;
using GitUIPluginInterfaces;
using ResourceManager;

namespace GitUI.Presentation.UserControls.RevisionGrid;

/// <summary>The kind of a reference shown in the message column, which chooses its color.</summary>
public enum RevisionRefKind
{
    Branch,
    RemoteBranch,
    Tag,
    Other,

    /// <summary>The label of a stash (its reflog selector) or of the autostash (as <c>MessageColumnProvider</c>).</summary>
    Stash,
}

/// <summary>A reference shown before the subject (as the WinForms revision grid's ref "capsules").</summary>
/// <param name="Name">The name as shown, e.g. <c>main</c>, <c>origin/main</c> or <c>v1.0</c>.</param>
/// <param name="Kind">The kind of the reference.</param>
/// <param name="IsCurrentBranch">Whether it is the checked out branch (shown in bold).</param>
public sealed record RevisionRefItem(string Name, RevisionRefKind Kind, bool IsCurrentBranch)
{
    /// <summary>The reference (none for a stash label), for its menu and the hover highlighting of its ancestry.</summary>
    public IGitRef? GitRef { get; init; }

    // Compared as shown, not by the reference object.
    public bool Equals(RevisionRefItem? other)
        => other is not null && Name == other.Name && Kind == other.Kind && IsCurrentBranch == other.IsCurrentBranch;

    public override int GetHashCode() => HashCode.Combine(Name, Kind, IsCurrentBranch);
}

/// <summary>How the revisions are shown (the <c>AppSettings</c> of the WinForms columns).</summary>
/// <param name="RelativeDate">Whether dates are relative (<c>AppSettings.RelativeDate</c>).</param>
/// <param name="ShowAuthorDate">Whether the author date rather than the commit date is shown (<c>AppSettings.ShowAuthorDate</c>).</param>
/// <param name="QuickSearchLabel">The text before the quick search string (<c>TranslatedStrings.SearchingFor</c>).</param>
/// <param name="QuickSearchTimeout">How long the quick search string is kept after typing (<c>AppSettings.RevisionGridQuickSearchTimeout</c>).</param>
/// <param name="ShowRemoteBranches">As <c>AppSettings.ShowRemoteBranches</c>: the remote branches are shown as references.</param>
/// <param name="ShowTags">As <c>AppSettings.ShowTags</c>: the tags are shown as references.</param>
public sealed record RevisionGridDisplayOptions(bool RelativeDate, bool ShowAuthorDate, string QuickSearchLabel = "Searching for: ", int QuickSearchTimeout = 4000, bool ShowRemoteBranches = true, bool ShowTags = true);

/// <summary>A row of the revision grid: a revision and its row in the <see cref="RevisionGraph"/>.</summary>
public sealed partial class RevisionGridRow : ObservableObject
{
    public RevisionGridRow(int index, GitRevision revision, RevisionGridDisplayOptions options, string? currentBranch)
    {
        Index = index;
        Revision = revision;
        ShortId = revision.IsArtificial ? "" : revision.ObjectId.ToShortString();
        Date = FormatDate(options.ShowAuthorDate ? revision.AuthorDate : revision.CommitDate, options.RelativeDate);
        List<RevisionRefItem> refs = [.. revision.Refs
            .Where(r => (options.ShowRemoteBranches || !r.IsRemote) && (options.ShowTags || !r.IsTag))
            .OrderBy(r => r.IsTag ? 2 : r.IsRemote ? 1 : 0)
            .Select(r => new RevisionRefItem(
                r.Name,
                r.IsHead ? RevisionRefKind.Branch : r.IsRemote ? RevisionRefKind.RemoteBranch : r.IsTag ? RevisionRefKind.Tag : RevisionRefKind.Other,
                r.IsHead && r.Name == currentBranch)
            {
                GitRef = r,
            })];

        // As MessageColumnProvider.OnCellPainting: after the references, the label of a stash (its reflog selector without
        // "refs/"), or the autostash with its subject as label (and no message).
        if (revision.IsAutostash)
        {
            refs.Add(new RevisionRefItem(revision.Subject, RevisionRefKind.Stash, IsCurrentBranch: false));
        }
        else if (revision.ReflogSelector is { Length: > 5 } reflogSelector)
        {
            refs.Add(new RevisionRefItem(reflogSelector[5..], RevisionRefKind.Stash, IsCurrentBranch: false));
        }

        Refs = refs;

        // As NotesColumnProvider: the first line of the notes, all of them as tooltip.
        Notes = revision.Notes?.IndexOf('\n') is int eolIndex and >= 0 ? revision.Notes[..eolIndex] : revision.Notes ?? "";
        UpdateBuildStatus();
        revision.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(GitRevision.BuildStatus))
            {
                UpdateBuildStatus();
            }
        };
    }

    public int Index { get; }

    public GitRevision Revision { get; }

    public ObjectId ObjectId => Revision.ObjectId;

    /// <summary>The subject; none for the autostash, whose subject is its label.</summary>
    public string Subject => Revision.IsAutostash ? "" : Revision.Subject;

    public string AuthorName => Revision.Author ?? "";

    public string Date { get; }

    public string ShortId { get; }

    public IReadOnlyList<RevisionRefItem> Refs { get; }

    /// <summary>The first line of the git notes (the notes column).</summary>
    public string Notes { get; }

    /// <summary>The git notes, as the tooltip of the notes column; none without notes.</summary>
    public string? NotesToolTip => string.IsNullOrEmpty(Revision.Notes) ? null : Revision.Notes;

    /// <summary>The tooltip of the author and the avatar (the author and the committer); none for the artificial commits.</summary>
    public string? AuthorToolTip => AuthorToolTipProvider?.Invoke(this);

    internal Func<RevisionGridRow, string?>? AuthorToolTipProvider { get; init; }

    /// <summary>The author's avatar (PNG data), once loaded for the avatar column.</summary>
    [ObservableProperty]
    public partial byte[]? Avatar { get; set; }

    /// <summary>Whether the author is the one of the selected revision (in bold, as <c>AuthorRevisionHighlighting</c>).</summary>
    [ObservableProperty]
    public partial bool IsAuthorHighlighted { get; set; }

    /// <summary>The build status (<c>GitRevision.BuildStatus</c>) of the build server integration, if any.</summary>
    [ObservableProperty]
    public partial BuildInfo? BuildStatus { get; private set; }

    /// <summary>The symbol of the build status (as <c>BuildInfo.StatusSymbol</c>); empty without one.</summary>
    public string BuildStatusSymbol => BuildStatus?.StatusSymbol ?? "";

    /// <summary>The description of the build status (the text of <c>BuildStatusColumnProvider</c>).</summary>
    public string BuildStatusDescription => BuildStatus?.Description ?? "";

    /// <summary>As <c>BuildStatusColumnProvider.TryGetToolTip</c>.</summary>
    public string? BuildStatusToolTip => BuildStatus is { } status ? status.Tooltip ?? status.Description : null;

    /// <summary>Whether the build status has a report to open (the hand cursor of the WinForms column).</summary>
    public bool HasBuildReport => !string.IsNullOrWhiteSpace(BuildStatus?.Url);

    private void UpdateBuildStatus()
    {
        BuildStatus = Revision.BuildStatus;
        OnPropertyChanged(nameof(BuildStatusSymbol));
        OnPropertyChanged(nameof(BuildStatusDescription));
        OnPropertyChanged(nameof(BuildStatusToolTip));
        OnPropertyChanged(nameof(HasBuildReport));
    }

    /// <summary>As <c>DateColumnProvider.FormatDate</c>.</summary>
    public static string FormatDate(DateTime date, bool relative)
    {
        if (date == DateTime.MinValue || date == DateTime.MaxValue)
        {
            return "";
        }

        return relative ? LocalizationHelpers.GetRelativeDateString(DateTime.Now, date, displayWeeks: false) : date.ToString("G");
    }
}

/// <summary>The rows of the grid; added in batches with a single reset notification, as revisions arrive in batches.</summary>
public sealed class RevisionGridRowList : ObservableCollection<RevisionGridRow>
{
    public void AddRange(IEnumerable<RevisionGridRow> rows)
    {
        bool added = false;
        foreach (RevisionGridRow row in rows)
        {
            Items.Add(row);
            added = true;
        }

        if (added)
        {
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}

/// <summary>Operations of the revision grid that need the host (git, threads).</summary>
public interface IRevisionGridHost
{
    /// <summary>The checked out branch, whose reference is shown in bold (empty if detached).</summary>
    string CurrentBranch { get; }

    /// <summary>
    ///  Reads the revisions in the background and adds them to <paramref name="graph"/> (with their references),
    ///  reporting on the UI thread after each batch and when done (with the error, if reading failed).
    /// </summary>
    void LoadRevisions(RevisionGraph graph, Action reportBatch, Action<Exception?> completed, CancellationToken cancellationToken);

    /// <summary>Whether the revision matches the quick search criteria (<c>IGitRevisionTester.Matches</c>).</summary>
    bool MatchesQuickSearch(GitRevision revision, string criteria);

    /// <summary>Runs <paramref name="work"/> in the background, then <paramref name="then"/> on the UI thread.</summary>
    void RunInBackground(Action work, Action then);

    /// <summary>
    ///  The email of the user (<c>user.email</c>), whose revisions are highlighted when none is selected
    ///  (<c>AuthorRevisionHighlighting</c>).
    /// </summary>
    string UserEmail => "";

    /// <summary>The avatar of an author (PNG data) for the avatar column (<c>IAvatarProvider.GetAvatarAsync</c>).</summary>
    Task<byte[]?> GetAvatarAsync(string email, string? name, int size) => Task.FromResult<byte[]?>(null);

    /// <summary>The tooltip of the author and avatar columns (<c>AuthorNameColumnProvider.GetAuthorAndCommiterToolTip</c>).</summary>
    string GetAuthorToolTip(GitRevision revision) => $"{revision.Author} <{revision.AuthorEmail}>";

    /// <summary>
    ///  The revisions to highlight in the graph when hovering the label of <paramref name="gitRef"/> in row
    ///  <paramref name="rowIndex"/> (<c>HoverHighlightCalculator</c>, debounced); none to clear. Cancelled (by a later call,
    ///  or if the highlight did not change) with an <see cref="OperationCanceledException"/>.
    /// </summary>
    Task<IReadOnlySet<ObjectId>?> GetHoverHighlightAsync(RevisionGraph graph, IGitRef? gitRef, int rowIndex, int firstVisibleRow, int visibleRowCount)
        => Task.FromResult<IReadOnlySet<ObjectId>?>(null);

    /// <summary>Opens a URL in the browser (the build report of a build status).</summary>
    void OpenUrl(string url)
    {
    }
}

/// <summary>
///  View model of the Avalonia revision grid (port of <c>RevisionGridControl</c>; docs/avalonia-port/PLAN.md, phase 4):
///  the revisions in graph order, with the <see cref="RevisionGraph"/> layout computed ahead of the visible rows.
/// </summary>
public sealed partial class RevisionGridViewModel : ObservableObject, IDisposable
{
    private readonly IRevisionGridHost _host;
    private RevisionGridDisplayOptions _options;
    private CancellationTokenSource? _loadCancellation;
    private ObjectId? _toBeSelected;
    private bool _isCaching;
    private int _cacheRequestedTo = -1;
    private bool _disposed;

    public RevisionGridViewModel(IRevisionGridHost host, RevisionGridDisplayOptions options)
    {
        _host = host;
        _options = options;
    }

    /// <summary>How the revisions are shown; setting it loads them again (as the view settings of the grid).</summary>
    public RevisionGridDisplayOptions DisplayOptions
    {
        get => _options;
        set
        {
            _options = value;
            Load(SelectedRow?.ObjectId);
        }
    }

    /// <summary>The layout of the graph, which the graph column draws from.</summary>
    public RevisionGraph Graph { get; } = new();

    public RevisionGridRowList Rows { get; } = [];

    [ObservableProperty]
    public partial RevisionGridRow? SelectedRow { get; set; }

    /// <summary>Whether several revisions can be selected (<c>RevisionGridControl.MultiSelect</c>).</summary>
    public bool MultiSelect { get; init; }

    /// <summary>The selected rows, in the order selected; the view reports them.</summary>
    public IReadOnlyList<RevisionGridRow> SelectedRows { get; private set; } = [];

    /// <summary>Raised when <see cref="SelectedRows"/> changed.</summary>
    public event EventHandler? SelectionChanged;

    /// <summary>Sets the selected rows (from the view).</summary>
    public void SetSelectedRows(IEnumerable<RevisionGridRow> rows)
    {
        SelectedRows = [.. rows];
        UpdateAuthorHighlight();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    ///  The selected revisions, newest first or (<paramref name="descending"/>) oldest first, as
    ///  <c>RevisionGridControl.GetSelectedRevisions</c> sorts them by row.
    /// </summary>
    public IReadOnlyList<GitRevision> GetSelectedRevisions(bool descending)
    {
        IEnumerable<RevisionGridRow> rows = SelectedRows.Count > 0 ? SelectedRows : SelectedRow is { } row ? [row] : [];
        return [.. (descending ? rows.OrderByDescending(r => r.Index) : rows.OrderBy(r => r.Index)).Select(r => r.Revision)];
    }

    /// <summary>
    ///  The selected revisions, the latest selected first, as <c>RevisionGridControl.GetSelectedRevisions()</c> without a
    ///  direction returns the selected rows of the WinForms grid.
    /// </summary>
    public IReadOnlyList<GitRevision> GetSelectedRevisionsLatestSelectedFirst()
    {
        IEnumerable<RevisionGridRow> rows = SelectedRows.Count > 0 ? SelectedRows.Reverse() : SelectedRow is { } row ? [row] : [];
        return [.. rows.Select(r => r.Revision)];
    }

    /// <summary>The revision if it is listed (<c>RevisionGridControl.GetRevision</c>).</summary>
    public GitRevision? GetRevision(ObjectId objectId)
        => Graph.TryGetNode(objectId, out RevisionGraphRevision? node) ? node.GitRevision : null;

    [ObservableProperty]
    public partial bool IsLoading { get; private set; }

    /// <summary>The number of rows whose graph is laid out; later rows are drawn once they are.</summary>
    [ObservableProperty]
    public partial int CachedGraphRowCount { get; private set; }

    /// <summary>Raised when revisions were loaded, after the preselected revision (if any) was selected.</summary>
    public event EventHandler? Loaded;

    /// <summary>(Re)loads the revisions, selecting <paramref name="toBeSelected"/> once it is loaded.</summary>
    public void Load(ObjectId? toBeSelected = null)
    {
        // E.g. the repository changed after the window of the grid closed.
        if (_disposed)
        {
            return;
        }

        _loadCancellation?.Cancel();
        _loadCancellation = new CancellationTokenSource();
        CancellationToken cancellationToken = _loadCancellation.Token;

        _toBeSelected = toBeSelected;
        Graph.Clear();
        Rows.Clear();
        CachedGraphRowCount = 0;
        _cacheRequestedTo = -1;
        IsLoading = true;

        // As PerformRefreshRevisions: the highlighted branch (until refresh) and the hover highlight are reset.
        IsBranchHighlighted = false;
        HoverHighlightedIds = null;

        _host.LoadRevisions(
            Graph,
            reportBatch: () =>
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    AddNewRows();
                }
            },
            completed: _ =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                Graph.LoadingCompleted();

                // Revisions inserted before the rows already shown (the artificial commits when HEAD is filtered out)
                // move the rows: they are created again.
                if (Rows.Where((row, index) => Graph.GetNodeForRow(index)?.GitRevision != row.Revision).Any())
                {
                    RevisionGridRow? selected = SelectedRow;
                    Rows.Clear();
                    AddNewRows();
                    SelectedRow = selected is null ? null : Rows.FirstOrDefault(row => row.ObjectId == selected.ObjectId);
                }

                AddNewRows();
                IsLoading = false;
                if (_toBeSelected is { } id)
                {
                    SelectRevision(id);
                }

                Loaded?.Invoke(this, EventArgs.Empty);
            },
            cancellationToken);
    }

    private void AddNewRows()
    {
        int count = Graph.Count;
        List<RevisionGridRow> rows = [];
        string currentBranch = _host.CurrentBranch;
        for (int index = Rows.Count; index < count; index++)
        {
            if (Graph.GetNodeForRow(index)?.GitRevision is { } revision)
            {
                rows.Add(new RevisionGridRow(index, revision, _options, currentBranch) { IsAuthorHighlighted = IsAuthorHighlightedFor(revision), AuthorToolTipProvider = GetAuthorToolTip });
            }
        }

        RevisionGridRow? selected = SelectedRow;
        Rows.AddRange(rows);
        SelectedRow = selected;

        if (_toBeSelected is { } id && SelectedRow is null && Graph.TryGetRowIndex(id, out int rowIndex) && rowIndex < Rows.Count)
        {
            SelectedRow = Rows[rowIndex];
        }

        // Lay out the first page right away.
        EnsureGraphCached(Math.Max(_cacheRequestedTo, 50));
    }

    /// <summary>Selects the revision; returns <see langword="false"/> if it is not listed.</summary>
    public bool SelectRevision(ObjectId objectId)
    {
        if (Graph.TryGetRowIndex(objectId, out int index) && index < Rows.Count)
        {
            SelectedRow = Rows[index];
            return true;
        }

        return false;
    }

    /// <summary>
    ///  Lays out the graph to <paramref name="lastVisibleIndex"/> and a page beyond, in the background
    ///  (as <c>RevisionDataGridView.UpdateVisibleRowRangeInternalAsync</c>).
    /// </summary>
    public void EnsureGraphCached(int lastVisibleIndex)
    {
        _cacheRequestedTo = Math.Max(_cacheRequestedTo, lastVisibleIndex);
        if (_isCaching || Graph.Count == 0 || CachedGraphRowCount > Math.Min(_cacheRequestedTo, Graph.Count - 1))
        {
            return;
        }

        _isCaching = true;
        int cacheTo = _cacheRequestedTo * 2;
        _host.RunInBackground(
            () => Graph.CacheTo(currentRowIndex: Graph.GetCachedCount(), lastToCacheRowIndex: cacheTo),
            () =>
            {
                _isCaching = false;
                int cached = Graph.GetCachedCount();
                if (cached != CachedGraphRowCount)
                {
                    CachedGraphRowCount = cached;
                    EnsureGraphCached(_cacheRequestedTo);
                }
            });
    }

    // Quick search: typing in the grid selects the next revision matching the typed text (as QuickSearchProvider).

    private string _lastQuickSearch = "";

    /// <summary>The typed quick search string; empty when not searching.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(QuickSearchLabel))]
    public partial string QuickSearchText { get; private set; } = "";

    [ObservableProperty]
    public partial bool IsQuickSearchVisible { get; private set; }

    /// <summary>Whether a revision matches the quick search string (the label is red otherwise).</summary>
    [ObservableProperty]
    public partial bool IsQuickSearchMatched { get; private set; } = true;

    public string QuickSearchLabel => _options.QuickSearchLabel + QuickSearchText;

    /// <summary>How long the quick search string is kept after typing, in milliseconds.</summary>
    public int QuickSearchTimeout => _options.QuickSearchTimeout;

    /// <summary>Raised when the quick search string changed, so that the view restarts its timeout.</summary>
    public event EventHandler? QuickSearchRestarted;

    /// <summary>Adds typed characters (lowercase, as <c>QuickSearchProvider.OnKeyPress</c>) to the quick search string.</summary>
    public void QuickSearchType(string text) => UpdateQuickSearch(QuickSearchText + text.ToLowerInvariant());

    /// <summary>Adds pasted text to the quick search string.</summary>
    public void QuickSearchPaste(string text) => UpdateQuickSearch(QuickSearchText + text);

    /// <summary>Removes the last character; returns <see langword="false"/> (and ends the search) if nothing would remain.</summary>
    public bool QuickSearchBackspace()
    {
        if (QuickSearchText.Length > 1)
        {
            UpdateQuickSearch(QuickSearchText[..^1]);
            return true;
        }

        HideQuickSearch();
        return false;
    }

    public void HideQuickSearch()
    {
        QuickSearchText = "";
        IsQuickSearchVisible = false;
    }

    /// <summary>Selects the next (or previous) revision matching the last quick search string.</summary>
    public void QuickSearchNext(bool down)
    {
        int currentIndex = SelectedRow?.Index ?? -1;
        int nextIndex = currentIndex < 0 ? 0 : down ? currentIndex + 1 : currentIndex - 1;
        QuickSearchText = _lastQuickSearch;
        FindNextMatch(nextIndex, QuickSearchText, reverse: !down);
        IsQuickSearchVisible = true;
        QuickSearchRestarted?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateQuickSearch(string text)
    {
        QuickSearchText = text;
        FindNextMatch(Math.Max(SelectedRow?.Index ?? 0, 0), text, reverse: false);
        _lastQuickSearch = text;
        IsQuickSearchVisible = true;
        QuickSearchRestarted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>As <c>QuickSearchProvider.FindNextMatch</c>: searches from the start index, wrapping around.</summary>
    private void FindNextMatch(int startIndex, string criteria, bool reverse)
    {
        int count = Rows.Count;
        if (count == 0)
        {
            return;
        }

        if (startIndex < 0 || startIndex >= count)
        {
            startIndex = reverse ? count - 1 : 0;
        }

        for (int step = 0; step < count; step++)
        {
            int index = reverse ? (startIndex - step + count) % count : (startIndex + step) % count;
            if (_host.MatchesQuickSearch(Rows[index].Revision, criteria))
            {
                IsQuickSearchMatched = true;
                SelectedRow = Rows[index];
                return;
            }
        }

        IsQuickSearchMatched = false;
    }

    public void Dispose()
    {
        // Not disposed: the revision reader in the background may still use its token.
        _disposed = true;
        _loadCancellation?.Cancel();
    }
}
