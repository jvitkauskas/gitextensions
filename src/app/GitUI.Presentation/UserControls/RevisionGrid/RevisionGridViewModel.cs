using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using GitExtensions.Extensibility;
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
}

/// <summary>A reference shown before the subject (as the WinForms revision grid's ref "capsules").</summary>
/// <param name="Name">The name as shown, e.g. <c>main</c>, <c>origin/main</c> or <c>v1.0</c>.</param>
/// <param name="Kind">The kind of the reference.</param>
/// <param name="IsCurrentBranch">Whether it is the checked out branch (shown in bold).</param>
public sealed record RevisionRefItem(string Name, RevisionRefKind Kind, bool IsCurrentBranch);

/// <summary>How the revisions are shown (the <c>AppSettings</c> of the WinForms columns).</summary>
/// <param name="RelativeDate">Whether dates are relative (<c>AppSettings.RelativeDate</c>).</param>
/// <param name="ShowAuthorDate">Whether the author date rather than the commit date is shown (<c>AppSettings.ShowAuthorDate</c>).</param>
public sealed record RevisionGridDisplayOptions(bool RelativeDate, bool ShowAuthorDate);

/// <summary>A row of the revision grid: a revision and its row in the <see cref="RevisionGraph"/>.</summary>
public sealed class RevisionGridRow
{
    public RevisionGridRow(int index, GitRevision revision, RevisionGridDisplayOptions options, string? currentBranch)
    {
        Index = index;
        Revision = revision;
        ShortId = revision.ObjectId.ToShortString();
        Date = FormatDate(options.ShowAuthorDate ? revision.AuthorDate : revision.CommitDate, options.RelativeDate);
        Refs = [.. revision.Refs
            .OrderBy(r => r.IsTag ? 2 : r.IsRemote ? 1 : 0)
            .Select(r => new RevisionRefItem(
                r.Name,
                r.IsHead ? RevisionRefKind.Branch : r.IsRemote ? RevisionRefKind.RemoteBranch : r.IsTag ? RevisionRefKind.Tag : RevisionRefKind.Other,
                r.IsHead && r.Name == currentBranch))];
    }

    public int Index { get; }

    public GitRevision Revision { get; }

    public ObjectId ObjectId => Revision.ObjectId;

    public string Subject => Revision.Subject;

    public string AuthorName => Revision.Author ?? "";

    public string Date { get; }

    public string ShortId { get; }

    public IReadOnlyList<RevisionRefItem> Refs { get; }

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

    /// <summary>Runs <paramref name="work"/> in the background, then <paramref name="then"/> on the UI thread.</summary>
    void RunInBackground(Action work, Action then);
}

/// <summary>
///  View model of the Avalonia revision grid (port of <c>RevisionGridControl</c>; docs/avalonia-port/PLAN.md, phase 4):
///  the revisions in graph order, with the <see cref="RevisionGraph"/> layout computed ahead of the visible rows.
/// </summary>
public sealed partial class RevisionGridViewModel : ObservableObject, IDisposable
{
    private readonly IRevisionGridHost _host;
    private readonly RevisionGridDisplayOptions _options;
    private CancellationTokenSource? _loadCancellation;
    private ObjectId? _toBeSelected;
    private bool _isCaching;
    private int _cacheRequestedTo = -1;

    public RevisionGridViewModel(IRevisionGridHost host, RevisionGridDisplayOptions options)
    {
        _host = host;
        _options = options;
    }

    /// <summary>The layout of the graph, which the graph column draws from.</summary>
    public RevisionGraph Graph { get; } = new();

    public RevisionGridRowList Rows { get; } = [];

    [ObservableProperty]
    public partial RevisionGridRow? SelectedRow { get; set; }

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
        _loadCancellation?.Cancel();
        _loadCancellation = new CancellationTokenSource();
        CancellationToken cancellationToken = _loadCancellation.Token;

        _toBeSelected = toBeSelected;
        Graph.Clear();
        Rows.Clear();
        CachedGraphRowCount = 0;
        _cacheRequestedTo = -1;
        IsLoading = true;

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
                rows.Add(new RevisionGridRow(index, revision, _options, currentBranch));
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

    public void Dispose()
    {
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
    }
}
