using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitExtensions.Extensibility;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.UserControls.RevisionGrid;

/// <summary>Strings of the filter toolbar of the main window; the ids match <c>FilterToolBar</c> (translated with <c>FormBrowse</c>).</summary>
public sealed class FilterToolBarStrings : ViewStrings
{
    public FilterToolBarStrings()
        : base("FormBrowse")
    {
        Branches = Add("toolStripLabel1", "Text", "&Branches:");
        BranchesToolTip = Add("toolStripLabel1", "ToolTipText", "Branch filter");
        AdvancedFilterToolTip = Add("tsbtnAdvancedFilter", "ToolTipText", "Advanced filter");
        BranchType = Add("tsddbtnBranchFilter", "Text", "Branch type");
        FilterType = Add("tsddbtnRevisionFilter", "Text", "Filter type");
        Filter = Add("tslblRevisionFilter", "Text", "&Filter:");
        FilterToolTip = Add("tslblRevisionFilter", "ToolTipText", "Text filter");
        AdvancedFilter = Add("tsmiAdvancedFilter", "Text", "&Advanced filter");
        Author = Add("tsmiAuthorFilter", "Text", "&Author");
        Local = Add("tsmiBranchLocal", "Text", "&Local");
        Remote = Add("tsmiBranchRemote", "Text", "&Remote");
        Tag = Add("tsmiBranchTag", "Text", "&Tag");
        CommitMessage = Add("tsmiCommitFilter", "Text", "Commit &message");
        Committer = Add("tsmiCommitterFilter", "Text", "&Committer");
        DiffContains = Add("tsmiDiffContainsFilter", "Text", "&Diff contains (SLOW)");
        ResetAllFilters = Add("tsmiResetAllFilters", "Text", "&Reset revision filters");
        ResetPathFilters = Add("tsmiResetPathFilters", "Text", "Reset &path filter");
        AllBranches = Add("tsmiShowBranchesAll", "Text", "&All branches");
        AllBranchesToolTip = Add("tsmiShowBranchesAll", "ToolTipText", "Show all branches");
        CurrentBranch = Add("tsmiShowBranchesCurrent", "Text", "&Current branch only");
        CurrentBranchToolTip = Add("tsmiShowBranchesCurrent", "ToolTipText", "Show current branch only");
        FilteredBranches = Add("tsmiShowBranchesFiltered", "Text", "&Filtered branches");
        FilteredBranchesToolTip = Add("tsmiShowBranchesFiltered", "ToolTipText", "Show filtered branches");
        NoResultsFound = Add("_noResultsFound", "Text", "<No results found>", category: "TranslatedStrings");
        ShowOnlyFirstParent = Add("_showOnlyFirstParent", "Text", "Show only first parent", category: "TranslatedStrings");
        ShowReflogToolTip = Add("_showReflogTooltip", "Text", "Show all reflog references", category: "TranslatedStrings");
        NonexistingGitRevision = Add("_nonexistingGitRevision", "Text", "Git revision does not exist", category: "TranslatedStrings");
        IgnoringReference = Add("_ignoringReference", "Text", "\"{0}\" is not a Git revision and will be ignored.", category: "TranslatedStrings");
    }

    public TranslatedText Branches { get; }

    public TranslatedText BranchesToolTip { get; }

    public TranslatedText AdvancedFilterToolTip { get; }

    public TranslatedText BranchType { get; }

    public TranslatedText FilterType { get; }

    public TranslatedText Filter { get; }

    public TranslatedText FilterToolTip { get; }

    public TranslatedText AdvancedFilter { get; }

    public TranslatedText Author { get; }

    public TranslatedText Local { get; }

    public TranslatedText Remote { get; }

    public TranslatedText Tag { get; }

    public TranslatedText CommitMessage { get; }

    public TranslatedText Committer { get; }

    public TranslatedText DiffContains { get; }

    public TranslatedText ResetAllFilters { get; }

    public TranslatedText ResetPathFilters { get; }

    public TranslatedText AllBranches { get; }

    public TranslatedText AllBranchesToolTip { get; }

    public TranslatedText CurrentBranch { get; }

    public TranslatedText CurrentBranchToolTip { get; }

    public TranslatedText FilteredBranches { get; }

    public TranslatedText FilteredBranchesToolTip { get; }

    public TranslatedText NoResultsFound { get; }

    public TranslatedText ShowOnlyFirstParent { get; }

    public TranslatedText ShowReflogToolTip { get; }

    public TranslatedText NonexistingGitRevision { get; }

    public TranslatedText IgnoringReference { get; }
}

/// <summary>The filter of the revision grid, as <c>FilterChangedEventArgs</c> reports it.</summary>
public sealed record RevisionGridFilterState(
    bool ShowAllBranches = true,
    bool ShowCurrentBranchOnly = false,
    bool ShowFilteredBranches = false,
    bool ShowOnlyFirstParent = false,
    bool ShowReflogReferences = false,
    bool HasFilter = false,
    string PathFilter = "",
    string BranchFilter = "",
    string MessageFilter = "",
    string CommitterFilter = "",
    string AuthorFilter = "",
    string DiffContentFilter = "",
    string FilterSummary = "");

/// <summary>The text filter of the toolbar (the WinForms <c>RevisionFilter</c>).</summary>
public sealed record RevisionTextFilter(string Text, bool ByMessage, bool ByCommitter, bool ByAuthor, bool ByDiffContent);

/// <summary>The filter of the revision grid (the WinForms <c>IRevisionGridFilter</c>), and what the toolbar needs of the repository.</summary>
public interface IRevisionGridFilterHost
{
    /// <summary>Raised when the revisions are loaded with a filter (as <c>FilterChanged</c>).</summary>
    event EventHandler? FilterChanged;

    RevisionGridFilterState State { get; }

    void SetAndApplyBranchFilter(string filter);

    void SetAndApplyRevisionFilter(RevisionTextFilter filter);

    void SetAndApplyPathFilter(string filter);

    void ResetAllFiltersAndRefresh();

    void ShowAllBranches();

    void ShowCurrentBranchOnly();

    void ShowFilteredBranches();

    void ToggleShowOnlyFirstParent();

    void ToggleShowReflogReferences();

    void ShowRevisionFilterDialog();

    /// <summary>Whether the working directory is a repository (else the toolbar is disabled).</summary>
    bool IsValidWorkingDir { get; }

    /// <summary>The names of the refs of the kinds (all refs if none), in the background (as <c>UpdateBranchFilterItems</c>).</summary>
    Task<IReadOnlyList<string>> GetRefNamesAsync(bool local, bool tags, bool remotes);

    /// <summary>The local names of all the refs (<c>IGitRef.LocalName</c>).</summary>
    IReadOnlyList<string> GetRefLocalNames();

    /// <summary>Whether git knows the revision (<c>RevParse</c>).</summary>
    bool RevisionExists(string revision);

    /// <summary>Warns that a reference of the branch filter does not exist.</summary>
    void ShowWarning(string heading, string caption);

    /// <summary>The previous text filters (<c>AppSettings.RevisionFilterDropdowns</c>); setting saves them.</summary>
    IReadOnlyList<string> RevisionFilterHistory { get; set; }
}

/// <summary>Port of <c>FilterToolBar</c>: the branch and text filters of the revision grid of the main window.</summary>
public sealed partial class FilterToolBarViewModel : ObservableObject
{
    private const int _maxFilterItems = 30;
    private readonly IRevisionGridFilterHost _host;
    private IReadOnlyList<string> _refNames = [];
    private bool _isApplyingFilter;
    private bool _updatingFromFilter;

    public FilterToolBarViewModel(FilterToolBarStrings strings, IRevisionGridFilterHost host)
    {
        Strings = strings;
        _host = host;

        // As the constructor: the saved filters and examples of the git options.
        foreach (string filter in host.RevisionFilterHistory.Union(
        [
            @"--invert-grep --grep=""EXCLUDE_COMMIT_MESSAGE_REGEX_PATTERN""",
            @"--perl-regexp --author=""^(?!.*EXCLUDE_AUTHOR_REGEX_PATTERN)""",
            @"--exclude=refs/remotes/EXCLUDE_REMOTE_REGEX_PATTERN",
        ]))
        {
            RevisionFilterItems.Add(filter);
        }

        host.FilterChanged += (_, _) => OnFilterChanged();
        OnFilterChanged();
    }

    public FilterToolBarStrings Strings { get; }

    /// <summary>The branches to show, separated by whitespace (<c>tscboBranchFilter</c>).</summary>
    [ObservableProperty]
    public partial string BranchFilter { get; set; } = "";

    /// <summary>The refs matching the branch filter (the items of <c>tscboBranchFilter</c>).</summary>
    public ObservableCollection<string> BranchItems { get; } = [];

    [ObservableProperty]
    public partial bool IncludeLocal { get; set; } = true;

    [ObservableProperty]
    public partial bool IncludeRemote { get; set; }

    [ObservableProperty]
    public partial bool IncludeTags { get; set; }

    /// <summary>The text filter (<c>tstxtRevisionFilter</c>).</summary>
    [ObservableProperty]
    public partial string RevisionFilter { get; set; } = "";

    /// <summary>The previous text filters (the items of <c>tstxtRevisionFilter</c>).</summary>
    public ObservableCollection<string> RevisionFilterItems { get; } = [];

    [ObservableProperty]
    public partial bool ByMessage { get; set; } = true;

    [ObservableProperty]
    public partial bool ByCommitter { get; set; }

    [ObservableProperty]
    public partial bool ByAuthor { get; set; }

    [ObservableProperty]
    public partial bool ByDiffContent { get; set; }

    [ObservableProperty]
    public partial RevisionGridFilterState State { get; private set; } = new();

    /// <summary>The text of the branches button (<c>tssbtnShowBranches</c>), the selected kind of branches.</summary>
    public string BranchesModeText
        => (State.ShowCurrentBranchOnly ? Strings.CurrentBranch : State.ShowFilteredBranches ? Strings.FilteredBranches : Strings.AllBranches).PlainText;

    public string BranchesModeToolTip
        => (State.ShowCurrentBranchOnly ? Strings.CurrentBranchToolTip : State.ShowFilteredBranches ? Strings.FilteredBranchesToolTip : Strings.AllBranchesToolTip).Text;

    /// <summary>The image of the branches button: all branches, or a filter.</summary>
    public string BranchesModeIcon => State.ShowAllBranches && !State.ShowFilteredBranches && !State.ShowCurrentBranchOnly ? "BranchLocal" : "BranchFilter";

    /// <summary>The image of the advanced filter button (<c>tsbtnAdvancedFilter</c>).</summary>
    public string AdvancedFilterIcon => State.HasFilter ? "FunnelExclamation" : "FunnelPencil";

    /// <summary>The tooltip of the advanced filter button: the summary of the filter, else its name.</summary>
    public string AdvancedFilterToolTip => string.IsNullOrEmpty(State.FilterSummary) ? Strings.AdvancedFilterToolTip.Text : State.FilterSummary;

    public bool CanResetPathFilter => !string.IsNullOrEmpty(State.PathFilter);

    public bool CanResetAllFilters => State.HasFilter;

    public bool IsEnabled => _host.IsValidWorkingDir;

    /// <summary>As <c>tsbtnAdvancedFilter_ButtonClick</c>: the dialog without a filter; else the menu of the button opens.</summary>
    public bool OpensAdvancedFilterMenu => CanResetAllFilters;

    /// <summary>As <c>ApplyCustomBranchFilter</c> (Enter in the branch filter): the references that do not exist are ignored.</summary>
    [RelayCommand]
    public void ApplyBranchFilter() => ApplyCustomBranchFilter(checkBranch: true);

    /// <summary>As <c>SetBranchFilter</c> (e.g. from the left panel): the branches are not checked.</summary>
    public void SetBranchFilter(string? filter)
    {
        BranchFilter = filter ?? "";
        ApplyCustomBranchFilter(checkBranch: false);
    }

    /// <summary>As <c>ApplyRevisionFilter</c> (Enter in the text filter).</summary>
    [RelayCommand]
    public void ApplyRevisionFilter()
    {
        if (_isApplyingFilter)
        {
            return;
        }

        _isApplyingFilter = true;
        try
        {
            _host.SetAndApplyRevisionFilter(new RevisionTextFilter(RevisionFilter.Trim(), ByMessage, ByCommitter, ByAuthor, ByDiffContent));
        }
        finally
        {
            _isApplyingFilter = false;
        }
    }

    /// <summary>As <c>SetRevisionFilter</c>.</summary>
    public void SetRevisionFilter(string? filter)
    {
        if (string.IsNullOrEmpty(RevisionFilter) && string.IsNullOrEmpty(filter))
        {
            return;
        }

        RevisionFilter = filter ?? "";
        ApplyRevisionFilter();
    }

    /// <summary>As <c>UpdateBranchFilterItems</c> (when the branch filter drops down): the refs containing the text.</summary>
    public async Task UpdateBranchItemsAsync()
    {
        if (!_host.IsValidWorkingDir)
        {
            return;
        }

        // As BranchesFilter: without a kind, all the refs are listed.
        _refNames = await _host.GetRefNamesAsync(IncludeLocal, IncludeTags, IncludeRemote);
        string filter = BranchFilter == Strings.NoResultsFound.Text ? "" : BranchFilter;
        string[] matches = [.. _refNames.Where(name => name.Contains(filter, StringComparison.InvariantCultureIgnoreCase))];
        BranchItems.Clear();
        foreach (string match in matches.Length == 0 ? [Strings.NoResultsFound.Text] : matches)
        {
            BranchItems.Add(match);
        }
    }

    [RelayCommand]
    private void ShowAllBranches() => _host.ShowAllBranches();

    [RelayCommand]
    private void ShowCurrentBranchOnly() => _host.ShowCurrentBranchOnly();

    [RelayCommand]
    private void ShowFilteredBranches() => _host.ShowFilteredBranches();

    [RelayCommand]
    private void ToggleShowOnlyFirstParent() => _host.ToggleShowOnlyFirstParent();

    [RelayCommand]
    private void ToggleShowReflog() => _host.ToggleShowReflogReferences();

    [RelayCommand]
    private void ShowAdvancedFilter() => _host.ShowRevisionFilterDialog();

    [RelayCommand]
    private void ResetPathFilter() => _host.SetAndApplyPathFilter("");

    [RelayCommand]
    private void ResetAllFilters() => _host.ResetAllFiltersAndRefresh();

    // As revisionFilterBox_CheckedChanged: another kind of text filter applies the text again.
    partial void OnByMessageChanged(bool value) => ReapplyRevisionFilter();

    partial void OnByCommitterChanged(bool value) => ReapplyRevisionFilter();

    partial void OnByAuthorChanged(bool value) => ReapplyRevisionFilter();

    partial void OnByDiffContentChanged(bool value) => ReapplyRevisionFilter();

    partial void OnStateChanged(RevisionGridFilterState value)
    {
        OnPropertyChanged(nameof(BranchesModeText));
        OnPropertyChanged(nameof(BranchesModeToolTip));
        OnPropertyChanged(nameof(BranchesModeIcon));
        OnPropertyChanged(nameof(AdvancedFilterIcon));
        OnPropertyChanged(nameof(AdvancedFilterToolTip));
        OnPropertyChanged(nameof(CanResetPathFilter));
        OnPropertyChanged(nameof(CanResetAllFilters));
        OnPropertyChanged(nameof(OpensAdvancedFilterMenu));
        OnPropertyChanged(nameof(IsEnabled));
    }

    private void ReapplyRevisionFilter()
    {
        if (!_updatingFromFilter && !string.IsNullOrWhiteSpace(RevisionFilter))
        {
            ApplyRevisionFilter();
        }
    }

    private void ApplyCustomBranchFilter(bool checkBranch)
    {
        if (_isApplyingFilter)
        {
            return;
        }

        _isApplyingFilter = true;
        try
        {
            string filter = BranchFilter == Strings.NoResultsFound.Text ? "" : BranchFilter;
            if (checkBranch && !string.IsNullOrWhiteSpace(filter))
            {
                List<string> newFilter = [];
                IReadOnlyList<string> refs = _host.GetRefLocalNames();

                // Split at whitespace; Git revisions do not allow spaces.
                foreach (string branch in filter.Split((char[]?)null, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                {
                    bool keep = branch.StartsWith("--")
                        || refs.Contains(branch)
                        || branch.Contains("..")
                        || branch.IndexOfAny(Delimiters.WildcardBranchSearchValues) >= 0
                        || _host.RevisionExists(branch.StartsWith('^') ? branch[1..] : branch);
                    if (!keep)
                    {
                        _host.ShowWarning(string.Format(Strings.IgnoringReference.Text, branch), Strings.NonexistingGitRevision.Text);
                        continue;
                    }

                    newFilter.Add(branch);
                }

                filter = string.Join(" ", newFilter);
            }

            _host.SetAndApplyBranchFilter(filter);
        }
        finally
        {
            _isApplyingFilter = false;
        }
    }

    // As revisionGridFilter_FilterChanged.
    private void OnFilterChanged()
    {
        RevisionGridFilterState state = _host.State;
        _updatingFromFilter = true;
        try
        {
            State = state;
            if (state.ShowFilteredBranches)
            {
                // Keep the value if other filter
                BranchFilter = state.BranchFilter;
            }

            List<(string Filter, Action<bool> Check)> revisionFilters =
            [
                (state.MessageFilter, value => ByMessage = value),
                (state.CommitterFilter, value => ByCommitter = value),
                (state.AuthorFilter, value => ByAuthor = value),
                (state.DiffContentFilter, value => ByDiffContent = value),
            ];

            // Without a text filter, the text is cleared and the kinds kept.
            string text = "";
            if (revisionFilters.Any(item => !string.IsNullOrWhiteSpace(item.Filter)))
            {
                foreach ((string filter, Action<bool> check) in revisionFilters)
                {
                    // Check the first kind that has a filter, and the following ones with the same filter.
                    bool matches = !string.IsNullOrWhiteSpace(filter) && (string.IsNullOrWhiteSpace(text) || filter == text);
                    if (matches)
                    {
                        text = filter;
                    }

                    check(matches);
                }
            }

            RevisionFilter = text;

            // Added to the previous filters and the settings, unless it is the last one.
            string trimmed = text.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed) && (RevisionFilterItems.Count == 0 || RevisionFilterItems[0] != trimmed))
            {
                RevisionFilterItems.Remove(trimmed);
                RevisionFilterItems.Insert(0, trimmed);
                RevisionFilter = trimmed;
                _host.RevisionFilterHistory = [.. RevisionFilterItems.Take(_maxFilterItems)];
            }
        }
        finally
        {
            _updatingFromFilter = false;
        }
    }
}
