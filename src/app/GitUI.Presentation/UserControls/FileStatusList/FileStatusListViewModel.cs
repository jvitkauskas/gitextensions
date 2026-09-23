using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Translations;
using GitUIPluginInterfaces;

namespace GitUI.Presentation.UserControls.FileStatusList;

/// <summary>Strings of the file status list; ids match <c>FileStatusList</c>.</summary>
public sealed class FileStatusListStrings : ViewStrings
{
    public FileStatusListStrings()
        : base("FileStatusList")
    {
        NoFiles = Add("NoFiles", "Text", "No changes");
        LoadingFiles = Add("LoadingFiles", "Text", "Loading data...");
        FilterWatermark = Add("cboFilterComboBox", "Watermark", "Filter files using a regular expression...");
        FilterToolTipTitle = Add("FilterToolTip", "ToolTipTitle", "RegEx");
        AsTreeToolTip = Add("btnAsTree", "ToolTipText", "Toggle flat list / tree");
        ByPathToolTip = Add("btnByPath", "ToolTipText", "Group by file path");
        ByExtensionToolTip = Add("btnByExtension", "ToolTipText", "Group by file type (extension)");
        ByStatusToolTip = Add("btnByStatus", "ToolTipText", "Group by diff status");
        ExpandAll = Add("_expandAll", "Text", "E&xpand all");
        CollapseAll = Add("_collapseAll", "Text", "C&ollapse all");
    }

    public TranslatedText NoFiles { get; }

    public TranslatedText LoadingFiles { get; }

    public TranslatedText FilterWatermark { get; }

    public TranslatedText FilterToolTipTitle { get; }

    public TranslatedText AsTreeToolTip { get; }

    public TranslatedText ByPathToolTip { get; }

    public TranslatedText ByExtensionToolTip { get; }

    public TranslatedText ByStatusToolTip { get; }

    public TranslatedText ExpandAll { get; }

    public TranslatedText CollapseAll { get; }
}

/// <summary>
///  View model of the file status list (port of the WinForms <c>FileStatusList</c>; docs/avalonia-port/PLAN.md, phase 5):
///  the files of one or more diffs as a tree, their selection and the filter.
/// </summary>
public sealed partial class FileStatusListViewModel : ObservableObject
{
    private readonly GitItemStatus _noItemStatus;
    private IReadOnlyList<FileStatusGroup> _groups = [];
    private Regex? _filterRegex;
    private bool _updatingSelection;

    /// <param name="fileNameOnlyFilter">Whether the filter matches only file names (<c>TruncatePathMethod.FileNameOnly</c>).</param>
    public FileStatusListViewModel(FileStatusListStrings strings, FileStatusTreeOptions? options = null, bool fileNameOnlyFilter = false)
    {
        Strings = strings;
        Options = options ?? new FileStatusTreeOptions();
        FileNameOnlyFilter = fileNameOnlyFilter;
        _noItemStatus = new GitItemStatus(name: $"- {strings.NoFiles.Text} -") { IsStatusOnly = true, ErrorMessage = string.Empty };
        SelectedNodes.CollectionChanged += (_, _) =>
        {
            if (!_updatingSelection)
            {
                OnSelectionChanged();
            }
        };
    }

    public FileStatusListStrings Strings { get; }

    public bool FileNameOnlyFilter { get; }

    /// <summary>How the files are sorted; changing it rebuilds the tree.</summary>
    [ObservableProperty]
    public partial FileStatusTreeOptions Options { get; set; }

    /// <summary>The root nodes.</summary>
    public ObservableCollection<FileStatusNode> Nodes { get; } = [];

    /// <summary>The selected nodes (the view keeps them in sync with its selection).</summary>
    public ObservableCollection<FileStatusNode> SelectedNodes { get; } = [];

    /// <summary>The selected files, in the order of the tree (as <c>FileStatusList.SelectedItems</c>).</summary>
    public IReadOnlyList<FileStatusEntry> SelectedEntries { get; private set; } = [];

    /// <summary>The selected file if exactly one is selected (as <c>FileStatusList.SelectedItem</c>).</summary>
    public FileStatusEntry? SelectedEntry => SelectedEntries.Count == 1 ? SelectedEntries[0] : null;

    /// <summary>All files shown (as <c>FileStatusList.AllItems</c>).</summary>
    public IEnumerable<FileStatusEntry> AllEntries => Nodes.SelectMany(n => n.DescendantsAndSelf()).Select(n => n.Entry).OfType<FileStatusEntry>().Where(e => e.Item != _noItemStatus);

    /// <summary>The regular expression the file names are filtered with.</summary>
    [ObservableProperty]
    public partial string Filter { get; set; } = "";

    /// <summary>The error of an invalid <see cref="Filter"/>, as its tooltip.</summary>
    [ObservableProperty]
    public partial string? FilterError { get; set; }

    public bool IsFilterActive => _filterRegex is not null;

    /// <summary>Whether "No changes" is shown instead of the list (as <c>SetFileStatusListVisibility</c>).</summary>
    [ObservableProperty]
    public partial bool ShowNoFiles { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>Whether the first file is selected when the files are set (as <c>SelectFirstItemOnSetItems</c>).</summary>
    public bool SelectFirstItemOnSetItems { get; init; } = true;

    /// <summary>Raised when the selected files change (as <c>SelectedIndexChanged</c>).</summary>
    public event EventHandler? SelectionChanged;

    /// <summary>Raised when the files are set (as <c>DataSourceChanged</c>).</summary>
    public event EventHandler? DataSourceChanged;

    public bool IsFlatList => Options.SortType.ToString().EndsWith("Flat");

    /// <summary>Shows "Loading data..." until the files are set.</summary>
    public void SetLoading()
    {
        IsLoading = true;
        ShowNoFiles = false;
    }

    /// <summary>Shows the files of the diffs (as <c>FileStatusList.SetDiffs</c>).</summary>
    public void SetGroups(IReadOnlyList<FileStatusGroup> groups)
    {
        _groups = groups;
        IsLoading = false;
        Update(updateCausedByFilter: false);
    }

    /// <summary>Shows the files of a diff between two revisions.</summary>
    public void SetDiff(GitRevision? firstRevision, GitRevision secondRevision, IReadOnlyList<GitItemStatus> items)
        => SetGroups([new FileStatusGroup(firstRevision, secondRevision, "", items)]);

    public void Clear() => SetGroups([]);

    /// <summary>Selects the files (as setting <c>SelectedItems</c>).</summary>
    public void Select(Func<FileStatusEntry, bool> predicate)
        => SetSelection([.. Nodes.SelectMany(n => n.DescendantsAndSelf()).Where(n => n.Entry is { } entry && predicate(entry))]);

    /// <summary>As <c>SelectFirstVisibleItem</c>: the first file, expanding its parents.</summary>
    public void SelectFirstVisibleItem()
    {
        FileStatusNode? first = Nodes.SelectMany(n => n.DescendantsAndSelf()).FirstOrDefault(n => n.Entry is not null && n.Entry.Item != _noItemStatus);
        SetSelection(first is null ? [] : [first]);
        for (FileStatusNode? parent = first?.Parent; parent is not null; parent = parent.Parent)
        {
            parent.IsExpanded = true;
        }
    }

    /// <summary>As <c>SelectNextItem</c> (without looping): the next or previous file.</summary>
    public void SelectNextItem(bool backwards)
    {
        List<FileStatusNode> files = [.. Nodes.SelectMany(n => n.DescendantsAndSelf()).Where(n => n.Entry is not null)];
        if (files.Count == 0)
        {
            return;
        }

        int index = SelectedNodes.Count == 0 ? -1 : files.IndexOf(SelectedNodes[^1]);
        int next = index < 0 ? 0 : Math.Clamp(index + (backwards ? -1 : 1), 0, files.Count - 1);
        SetSelection([files[next]]);
    }

    [RelayCommand]
    private void ToggleFlatList()
    {
        // As btnAsTree: switch between the flat and the tree variant of the sorting.
        string sortType = Options.SortType.ToString();
        DiffListSortType toggled = Enum.Parse<DiffListSortType>(sortType.EndsWith("Flat") ? sortType[..^"Flat".Length] : sortType + "Flat");
        Options = Options with { SortType = toggled };
    }

    [RelayCommand]
    private void SortBy(DiffListSortType sortType)
        => Options = Options with { SortType = IsFlatList ? Enum.Parse<DiffListSortType>(sortType + "Flat") : sortType };

    [RelayCommand]
    private void ExpandAll()
    {
        foreach (FileStatusNode node in Nodes)
        {
            node.ExpandAll();
        }
    }

    [RelayCommand]
    private void CollapseAll()
    {
        foreach (FileStatusNode node in Nodes.SelectMany(n => n.DescendantsAndSelf()))
        {
            node.IsExpanded = false;
        }
    }

    [RelayCommand]
    private void ClearFilter() => Filter = "";

    partial void OnOptionsChanged(FileStatusTreeOptions value)
    {
        OnPropertyChanged(nameof(IsFlatList));
        Update(updateCausedByFilter: true);
    }

    partial void OnFilterChanged(string value)
    {
        // As StoreFilter: an invalid expression keeps the previous filter and is reported.
        if (string.IsNullOrEmpty(value))
        {
            _filterRegex = null;
            FilterError = null;
        }
        else
        {
            try
            {
                _filterRegex = new Regex(value, RegexOptions.IgnoreCase);
                FilterError = null;
            }
            catch (ArgumentException ex)
            {
                FilterError = ex.Message;
                return;
            }
        }

        OnPropertyChanged(nameof(IsFilterActive));
        Update(updateCausedByFilter: true);
    }

    /// <summary>As <c>UpdateFileStatusListView</c>.</summary>
    private void Update(bool updateCausedByFilter)
    {
        HashSet<GitItemStatus>? previouslySelectedItems = updateCausedByFilter ? [.. SelectedEntries.Select(e => e.Item)] : null;

        (List<FileStatusNode> nodes, _, bool filesPresent) = FileStatusTreeBuilder.Build(_groups, Options, IsFilterMatch, _noItemStatus);
        ShowNoFiles = !filesPresent && _groups.Count <= 1;

        Nodes.Clear();
        foreach (FileStatusNode node in nodes)
        {
            Nodes.Add(node);
        }

        if (Nodes.Count == 1 && Nodes[0].Children.Count == 0)
        {
            SetSelection([Nodes[0]]);
        }
        else if (!updateCausedByFilter && SelectFirstItemOnSetItems)
        {
            SelectFirstVisibleItem();
        }
        else if (previouslySelectedItems?.Count is > 0)
        {
            Select(entry => previouslySelectedItems.Contains(entry.Item));
        }
        else
        {
            SetSelection([]);
        }

        DataSourceChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>As <c>FileStatusList.IsFilterMatch</c> (without the A/B diff status buttons).</summary>
    private bool IsFilterMatch(GitItemStatus item)
    {
        if (item.IsRangeDiff || _filterRegex is null)
        {
            return true;
        }

        string name = item.Name.TrimEnd('/');
        string? oldName = item.OldName;
        if (FileNameOnlyFilter)
        {
            name = Path.GetFileName(name);
            oldName = Path.GetFileName(oldName);
        }

        return _filterRegex.IsMatch(name) || (oldName is not null && _filterRegex.IsMatch(oldName));
    }

    private void SetSelection(IReadOnlyList<FileStatusNode> nodes)
    {
        _updatingSelection = true;
        try
        {
            SelectedNodes.Clear();
            foreach (FileStatusNode node in nodes)
            {
                SelectedNodes.Add(node);
            }
        }
        finally
        {
            _updatingSelection = false;
        }

        OnSelectionChanged();
    }

    private void OnSelectionChanged()
    {
        // As SelectedItems: a single selected folder or group selects its files.
        IEnumerable<FileStatusNode> selected = SelectedNodes.Count == 1 && SelectedNodes[0].Entry is null
            ? SelectedNodes[0].DescendantsAndSelf()
            : SelectedNodes;
        IReadOnlyList<FileStatusEntry> entries = [.. selected.Select(n => n.Entry).OfType<FileStatusEntry>().Where(e => e.Item != _noItemStatus)];
        if (entries.SequenceEqual(SelectedEntries))
        {
            return;
        }

        SelectedEntries = entries;
        OnPropertyChanged(nameof(SelectedEntries));
        OnPropertyChanged(nameof(SelectedEntry));
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
}
