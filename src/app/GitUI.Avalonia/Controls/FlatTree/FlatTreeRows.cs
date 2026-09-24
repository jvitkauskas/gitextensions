using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;

namespace GitUI.Avalonia.Controls.FlatTree;

/// <summary>How <see cref="FlatTreeRows"/> reads a tree of nodes: their children and whether they are expanded.</summary>
/// <param name="GetChildren">The children of a node (an <see cref="INotifyCollectionChanged"/> collection is followed).</param>
/// <param name="ChildrenPropertyName">The property of a node that raises a change when its children are replaced.</param>
/// <param name="IsExpandedPropertyName">The property of a node that raises a change when it is expanded or collapsed.</param>
/// <param name="IsExpanded">Whether a node is expanded.</param>
/// <param name="SetExpanded">Expands or collapses a node.</param>
public sealed record FlatTreeAdapter(
    Func<object, IEnumerable> GetChildren,
    string ChildrenPropertyName,
    string IsExpandedPropertyName,
    Func<object, bool> IsExpanded,
    Action<object, bool> SetExpanded);

/// <summary>A visible node of a tree shown as a flat list (<see cref="FlatTreeRows"/>): the node, its depth and its expander.</summary>
public sealed class FlatTreeRow : INotifyPropertyChanged
{
    private readonly FlatTreeAdapter _adapter;
    private bool _hasChildren;

    internal FlatTreeRow(object node, int level, FlatTreeAdapter adapter)
    {
        Node = node;
        Level = level;
        _adapter = adapter;
        _hasChildren = HasAnyChild();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The node of the tree.</summary>
    public object Node { get; }

    /// <summary>The depth of the node, 0 for a root.</summary>
    public int Level { get; }

    /// <summary>The indentation of the node, 16 pixels for each level.</summary>
    public Thickness Indent => new(Level * 16, 0, 0, 0);

    /// <summary>Whether the node has children (and an expander).</summary>
    public bool HasChildren
    {
        get => _hasChildren;
        internal set
        {
            if (_hasChildren != value)
            {
                _hasChildren = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasChildren)));
            }
        }
    }

    /// <summary>Whether the node is expanded (the expander of the row expands or collapses the node).</summary>
    public bool IsExpanded
    {
        get => _adapter.IsExpanded(Node);
        set => _adapter.SetExpanded(Node, value);
    }

    internal bool HasAnyChild() => _adapter.GetChildren(Node).GetEnumerator() is var e && e.MoveNext();

    internal void RaiseIsExpandedChanged() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
}

/// <summary>
///  The visible nodes of a tree as a flat list (<see cref="Rows"/>), which a virtualizing list shows with an indentation and an
///  expander for each row: a tree of thousands of nodes (the files of a large commit, the branches and tags of a large
///  repository) only creates the controls of the rows on screen, where a <c>TreeView</c> creates those of all the expanded
///  nodes. The rows follow the nodes as they are expanded or collapsed and as their children change.
/// </summary>
public sealed class FlatTreeRows : IDisposable
{
    private readonly IEnumerable _roots;
    private readonly FlatTreeAdapter _adapter;
    private readonly Dictionary<object, FlatTreeRow> _rowByNode = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<object, INotifyCollectionChanged> _childrenByNode = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<object, object> _nodeByChildren = new(ReferenceEqualityComparer.Instance);

    public FlatTreeRows(IEnumerable roots, FlatTreeAdapter adapter)
    {
        _roots = roots;
        _adapter = adapter;
        if (roots is INotifyCollectionChanged observableRoots)
        {
            observableRoots.CollectionChanged += OnRootsChanged;
        }

        Rebuild();
    }

    /// <summary>The visible nodes, in the order of the tree.</summary>
    public ObservableCollection<FlatTreeRow> Rows { get; } = [];

    /// <summary>The row of <paramref name="node"/>, if it is visible.</summary>
    public FlatTreeRow? RowOf(object node) => _rowByNode.GetValueOrDefault(node);

    /// <summary>The parent row of <paramref name="row"/>, if it is not a root.</summary>
    public FlatTreeRow? ParentOf(FlatTreeRow row)
    {
        for (int i = Rows.IndexOf(row) - 1; i >= 0; i--)
        {
            if (Rows[i].Level < row.Level)
            {
                return Rows[i];
            }
        }

        return null;
    }

    public void Dispose()
    {
        if (_roots is INotifyCollectionChanged observableRoots)
        {
            observableRoots.CollectionChanged -= OnRootsChanged;
        }

        foreach (FlatTreeRow row in Rows)
        {
            Unfollow(row.Node);
        }

        Rows.Clear();
        _rowByNode.Clear();
    }

    private void Rebuild()
    {
        foreach (FlatTreeRow row in Rows)
        {
            Unfollow(row.Node);
        }

        _rowByNode.Clear();
        List<FlatTreeRow> rows = [];
        foreach (object root in _roots)
        {
            AddVisible(root, level: 0, rows);
        }

        // One reset rather than an event for each row.
        Rows.Clear();
        foreach (FlatTreeRow row in rows)
        {
            Rows.Add(row);
        }
    }

    /// <summary>Adds the row of <paramref name="node"/> and those of its visible descendants to <paramref name="rows"/>.</summary>
    private void AddVisible(object node, int level, List<FlatTreeRow> rows)
    {
        FlatTreeRow row = new(node, level, _adapter);
        rows.Add(row);
        _rowByNode[node] = row;
        Follow(node);
        if (_adapter.IsExpanded(node))
        {
            foreach (object child in _adapter.GetChildren(node))
            {
                AddVisible(child, level + 1, rows);
            }
        }
    }

    private void Follow(object node)
    {
        if (node is INotifyPropertyChanged observableNode)
        {
            observableNode.PropertyChanged += OnNodePropertyChanged;
        }

        FollowChildren(node);
    }

    private void FollowChildren(object node)
    {
        if (_adapter.GetChildren(node) is INotifyCollectionChanged children)
        {
            children.CollectionChanged += OnChildrenChanged;
            _childrenByNode[node] = children;
            _nodeByChildren[children] = node;
        }
    }

    private void Unfollow(object node)
    {
        if (node is INotifyPropertyChanged observableNode)
        {
            observableNode.PropertyChanged -= OnNodePropertyChanged;
        }

        UnfollowChildren(node);
    }

    private void UnfollowChildren(object node)
    {
        if (_childrenByNode.Remove(node, out INotifyCollectionChanged? children))
        {
            _nodeByChildren.Remove(children);
            children.CollectionChanged -= OnChildrenChanged;
        }
    }

    private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is null || !_rowByNode.TryGetValue(sender, out FlatTreeRow? row))
        {
            return;
        }

        if (e.PropertyName == _adapter.IsExpandedPropertyName)
        {
            row.RaiseIsExpandedChanged();
            RefreshDescendants(row);
        }
        else if (e.PropertyName == _adapter.ChildrenPropertyName)
        {
            UnfollowChildren(sender);
            FollowChildren(sender);
            RefreshDescendants(row);
        }
    }

    private void OnChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (sender is not null && _nodeByChildren.TryGetValue(sender, out object? node) && _rowByNode.TryGetValue(node, out FlatTreeRow? row))
        {
            RefreshDescendants(row);
        }
    }

    private void OnRootsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewItems is { } newItems:
                // Before the row of the root that follows the new ones, or at the end.
                int index = e.NewStartingIndex + newItems.Count < RootCount()
                    ? Rows.IndexOf(_rowByNode[RootAt(e.NewStartingIndex + newItems.Count)])
                    : Rows.Count;
                List<FlatTreeRow> rows = [];
                foreach (object root in newItems)
                {
                    AddVisible(root, level: 0, rows);
                }

                InsertRows(index, rows);
                break;

            case NotifyCollectionChangedAction.Remove when e.OldItems is { } oldItems:
                foreach (object root in oldItems)
                {
                    if (_rowByNode.TryGetValue(root, out FlatTreeRow? rootRow))
                    {
                        int start = Rows.IndexOf(rootRow);
                        RemoveRows(start, 1 + CountDescendantRows(start));
                    }
                }

                break;

            default:
                Rebuild();
                break;
        }
    }

    /// <summary>Shows the visible descendants of <paramref name="row"/> again (after it was expanded or collapsed, or its children changed).</summary>
    private void RefreshDescendants(FlatTreeRow row)
    {
        row.HasChildren = row.HasAnyChild();
        int index = Rows.IndexOf(row);
        RemoveRows(index + 1, CountDescendantRows(index));
        if (!_adapter.IsExpanded(row.Node))
        {
            return;
        }

        List<FlatTreeRow> rows = [];
        foreach (object child in _adapter.GetChildren(row.Node))
        {
            AddVisible(child, row.Level + 1, rows);
        }

        InsertRows(index + 1, rows);
    }

    private int CountDescendantRows(int index)
    {
        int level = Rows[index].Level;
        int count = 0;
        while (index + 1 + count < Rows.Count && Rows[index + 1 + count].Level > level)
        {
            count++;
        }

        return count;
    }

    private void RemoveRows(int start, int count)
    {
        for (int i = start + count - 1; i >= start; i--)
        {
            // Unmapped first: the list reports the deselection of a removed row while it is removed.
            FlatTreeRow row = Rows[i];
            if (_rowByNode.TryGetValue(row.Node, out FlatTreeRow? current) && ReferenceEquals(current, row))
            {
                _rowByNode.Remove(row.Node);
                Unfollow(row.Node);
            }

            Rows.RemoveAt(i);
        }
    }

    private void InsertRows(int index, List<FlatTreeRow> rows)
    {
        for (int i = 0; i < rows.Count; i++)
        {
            Rows.Insert(index + i, rows[i]);
        }
    }

    private int RootCount() => _roots is ICollection collection ? collection.Count : _roots.Cast<object>().Count();

    private object RootAt(int index) => _roots is IList list ? list[index]! : _roots.Cast<object>().ElementAt(index);
}
