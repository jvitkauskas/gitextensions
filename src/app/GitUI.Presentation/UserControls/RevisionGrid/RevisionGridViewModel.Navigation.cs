using CommunityToolkit.Mvvm.ComponentModel;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Services;
using GitUI.UserControls.RevisionGrid.Graph;
using GitUIPluginInterfaces;

namespace GitUI.Presentation.UserControls.RevisionGrid;

/// <summary>
///  The navigation of the revision grid (<c>NavigationHistory</c>, <c>ParentChildNavigationHistory</c>, the go-to
///  commands of <c>RevisionGridControl</c>), its context menu and the visibility of its columns.
/// </summary>
public sealed partial class RevisionGridViewModel
{
    // History of the selected revisions (browse history); the top is the current one.
    private readonly Stack<ObjectId> _previous = new();

    // The revisions navigated back from; the top is the one to show when navigating forward.
    private readonly Stack<ObjectId> _next = new();

    // As ParentChildNavigationHistory: going to a parent then back to the child returns to the same child.
    private readonly Stack<ObjectId> _childHistory = new();
    private readonly Stack<ObjectId> _parentHistory = new();
    private bool _navigating;

    /// <summary>The items of the context menu of the selected revisions, built when the menu opens.</summary>
    public Func<IReadOnlyList<MenuModelItem>>? ContextMenuProvider { get; set; }

    /// <summary>As <c>ShowRevisionGridGraphColumn</c>.</summary>
    [ObservableProperty]
    public partial bool ShowGraphColumn { get; set; } = true;

    /// <summary>As <c>ShowAuthorNameColumn</c>.</summary>
    [ObservableProperty]
    public partial bool ShowAuthorColumn { get; set; } = true;

    /// <summary>As <c>ShowDateColumn</c>.</summary>
    [ObservableProperty]
    public partial bool ShowDateColumn { get; set; } = true;

    /// <summary>As <c>ShowObjectIdColumn</c>.</summary>
    [ObservableProperty]
    public partial bool ShowIdColumn { get; set; } = true;

    public bool CanNavigateBackward => _previous.Count > 1;

    public bool CanNavigateForward => _next.Count > 0;

    /// <summary>As <c>NavigateBackward</c>.</summary>
    public void NavigateBackward()
    {
        if (CanNavigateBackward)
        {
            _next.Push(_previous.Pop());
            NavigateTo(_previous.Peek());
        }
    }

    /// <summary>As <c>NavigateForward</c>.</summary>
    public void NavigateForward()
    {
        if (CanNavigateForward)
        {
            ObjectId next = _next.Pop();
            _previous.Push(next);
            NavigateTo(next);
        }
    }

    /// <summary>As <c>goToParentToolStripMenuItem_Click</c>: back to the parent navigated from, else the first parent.</summary>
    public void GoToParent()
    {
        if (SelectedRow?.Revision is not { } revision)
        {
            return;
        }

        if (_parentHistory.Count > 0)
        {
            NavigateParentChild(revision.ObjectId, _parentHistory.Pop(), toChild: false);
        }
        else if (revision.HasParent)
        {
            NavigateParentChild(revision.ObjectId, revision.FirstParentId, toChild: false);
        }
    }

    /// <summary>As <c>goToFirstParentToolStripMenuItem_Click</c>.</summary>
    public void GoToFirstParent()
    {
        if (SelectedRow?.Revision is { HasParent: true } revision)
        {
            SelectRevision(revision.FirstParentId);
        }
    }

    /// <summary>As <c>goToLastParentToolStripMenuItem_Click</c>.</summary>
    public void GoToLastParent()
    {
        if (SelectedRow?.Revision is { HasParent: true } revision)
        {
            SelectRevision(revision.ParentIds![^1]);
        }
    }

    /// <summary>As <c>goToChildToolStripMenuItem_Click</c>: back to the child navigated from, else the first child.</summary>
    public void GoToChild()
    {
        if (SelectedRow?.Revision is not { } revision)
        {
            return;
        }

        if (_childHistory.Count > 0)
        {
            NavigateParentChild(revision.ObjectId, _childHistory.Pop(), toChild: true);
        }
        else if (GetChildren(revision.ObjectId).FirstOrDefault() is { } child)
        {
            NavigateParentChild(revision.ObjectId, child, toChild: true);
        }
    }

    /// <summary>
    ///  As <c>SelectNextForkPointAsDiffBase</c>: the first ancestor of the (first) selected revision with a branch or another
    ///  child is selected first, as the base of the diff, with the other selected revisions.
    /// </summary>
    public void SelectNextForkPointAsDiffBase()
    {
        IReadOnlyList<GitRevision> revisions = GetSelectedRevisionsLatestSelectedFirst();
        if (revisions.Count == 0)
        {
            return;
        }

        GitRevision? revision = revisions[^1];
        while (revision is { IsArtificial: true })
        {
            revision = GetRevision(revision.FirstParentId);
        }

        if (revision is null)
        {
            return;
        }

        do
        {
            if (!revision.HasParent || GetRevision(revision.FirstParentId) is not { } parent)
            {
                break;
            }

            revision = parent;
        }
        while (!revision.Refs.Any(r => r.IsHead || r.IsRemote) && GetChildren(revision.ObjectId).Count == 1);

        SelectRevisions([revision.ObjectId, .. revisions.Take(Math.Max(1, revisions.Count - 1)).Select(r => r.ObjectId)]);
    }

    /// <summary>Raised to select several rows in this order (the first one first), which the view selects.</summary>
    public event EventHandler<IReadOnlyList<RevisionGridRow>>? RowsSelectionRequested;

    /// <summary>
    ///  Selects the listed revisions in this order (as <c>SetSelectedRevision</c> with <c>toggleSelection</c> for the next
    ///  ones); the last one only without <see cref="MultiSelect"/>.
    /// </summary>
    public void SelectRevisions(IReadOnlyList<ObjectId> objectIds)
    {
        List<RevisionGridRow> rows = [];
        foreach (ObjectId objectId in objectIds.Distinct())
        {
            if (Graph.TryGetRowIndex(objectId, out int index) && index < Rows.Count)
            {
                rows.Add(Rows[index]);
            }
        }

        if (rows.Count == 0)
        {
            return;
        }

        if (!MultiSelect || rows.Count == 1)
        {
            SelectedRow = rows[^1];
            return;
        }

        RowsSelectionRequested?.Invoke(this, rows);
    }

    /// <summary>The children of a revision in the grid (<c>GetRevisionChildren</c>), the newest first.</summary>
    public IReadOnlyList<ObjectId> GetChildren(ObjectId objectId)
        => Graph.TryGetNode(objectId, out RevisionGraphRevision? node)
            ? [.. node.Children.Select(child => child.Objectid)]
            : [];

    partial void OnSelectedRowChanged(RevisionGridRow? value)
    {
        UpdateAuthorHighlight();
        if (_navigating || value is null)
        {
            return;
        }

        // As ParentChildNavigationHistory.RevisionsSelectionChanged: another selection forgets the parent/child history.
        _childHistory.Clear();
        _parentHistory.Clear();

        // As NavigationHistory.Push.
        if (_previous.Count == 0 || _previous.Peek() != value.ObjectId)
        {
            _previous.Push(value.ObjectId);
            _next.Clear();
        }
    }

    private void NavigateParentChild(ObjectId current, ObjectId to, bool toChild)
    {
        (toChild ? _parentHistory : _childHistory).Push(current);
        Stack<ObjectId> child = new(_childHistory.Reverse());
        Stack<ObjectId> parent = new(_parentHistory.Reverse());
        SelectRevision(to);

        // The selection change clears the parent/child history; restore it.
        _childHistory.Clear();
        _parentHistory.Clear();
        foreach (ObjectId id in child.Reverse())
        {
            _childHistory.Push(id);
        }

        foreach (ObjectId id in parent.Reverse())
        {
            _parentHistory.Push(id);
        }
    }

    private void NavigateTo(ObjectId objectId)
    {
        _navigating = true;
        try
        {
            SelectRevision(objectId);
        }
        finally
        {
            _navigating = false;
        }
    }
}
