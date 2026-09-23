using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the revision filter dialog; ids match <c>FormRevisionFilter</c>.</summary>
public sealed class RevisionFilterStrings : ViewStrings
{
    public RevisionFilterStrings()
        : base("FormRevisionFilter")
    {
        Title = Add("$this", "Text", "Filter");
        Since = Add("_since", "Text", "&Since");
        Until = Add("_until", "Text", "&Until");
        Author = Add("_author", "Text", "&Author");
        Committer = Add("_committer", "Text", "&Committer");
        Message = Add("_message", "Text", "&Message");
        DiffContent = Add("_diffContent", "Text", "&Diff contains");
        DiffContentToolTip = Add("_diffContentToolTip", "Text", "SLOW");
        IgnoreCase = Add("IgnoreCase", "Text", "&Ignore case");
        Limit = Add("_limit", "Text", "&Limit");
        PathFilter = Add("_pathFilter", "Text", "&Path filter");
        Branches = Add("_branches", "Text", "&Branches");
        CurrentBranchOnly = Add("CurrentBranchOnlyCheck", "Text", "Show current branch &only");
        Reflog = Add("ReflogCheck", "Text", "Show &reflog");
        OnlyFirstParent = Add("OnlyFirstParentCheck", "Text", "Show only &first parent");
        HideMergeCommits = Add("HideMergeCommitsCheck", "Text", "Hide merge commi&ts");
        SimplifyByDecoration = Add("SimplifyByDecorationCheck", "Text", "Simplify b&y decoration");
        FullHistory = Add("FullHistoryCheck", "Text", "Full &history");
        SimplifyMerges = Add("SimplifyMergesCheck", "Text", "Simplify mer&ges");
        Ok = Add("Ok", "Text", "OK");
    }

    public TranslatedText Title { get; }

    public TranslatedText Since { get; }

    public TranslatedText Until { get; }

    public TranslatedText Author { get; }

    public TranslatedText Committer { get; }

    public TranslatedText Message { get; }

    public TranslatedText DiffContent { get; }

    public TranslatedText DiffContentToolTip { get; }

    public TranslatedText IgnoreCase { get; }

    public TranslatedText Limit { get; }

    public TranslatedText PathFilter { get; }

    public TranslatedText Branches { get; }

    public TranslatedText CurrentBranchOnly { get; }

    public TranslatedText Reflog { get; }

    public TranslatedText OnlyFirstParent { get; }

    public TranslatedText HideMergeCommits { get; }

    public TranslatedText SimplifyByDecoration { get; }

    public TranslatedText FullHistory { get; }

    public TranslatedText SimplifyMerges { get; }

    public TranslatedText Ok { get; }
}

/// <summary>The revision filter as edited in the dialog (the fields of <c>FilterInfo</c> that <c>FormRevisionFilter</c> edits).</summary>
public sealed record RevisionFilterValues(
    bool ByDateFrom,
    DateTime DateFrom,
    bool ByDateTo,
    DateTime DateTo,
    bool ByAuthor,
    string Author,
    bool ByCommitter,
    string Committer,
    bool ByMessage,
    string Message,
    bool ByDiffContent,
    string DiffContent,
    bool IgnoreCase,
    bool ByCommitsLimit,
    int CommitsLimit,
    bool ByPathFilter,
    string PathFilter,
    bool ByBranchFilter,
    string BranchFilter,
    bool ShowCurrentBranchOnly,
    bool ShowReflogReferences,
    bool ShowOnlyFirstParent,
    bool HideMergeCommits,
    bool ShowSimplifyByDecoration,
    bool ShowFullHistory,
    bool ShowSimplifyMerges);

/// <summary>View model of the revision filter dialog (port of <c>FormRevisionFilter</c>).</summary>
public sealed partial class RevisionFilterViewModel : DialogViewModel
{
    private readonly int _commitsLimitDefault;

    /// <param name="commitsLimitDefault">The commits limit shown when the limit is turned off.</param>
    public RevisionFilterViewModel(RevisionFilterStrings strings, RevisionFilterValues filter, int commitsLimitDefault)
    {
        Strings = strings;
        _commitsLimitDefault = commitsLimitDefault;

        ByDateFrom = filter.ByDateFrom;
        DateFrom = filter.DateFrom == DateTime.MinValue ? DateTime.Today : filter.DateFrom;
        ByDateTo = filter.ByDateTo;
        DateTo = filter.DateTo == DateTime.MinValue ? DateTime.Today : filter.DateTo;
        ByAuthor = filter.ByAuthor;
        Author = filter.Author;
        ByCommitter = filter.ByCommitter;
        Committer = filter.Committer;
        ByMessage = filter.ByMessage;
        Message = filter.Message;
        ByDiffContent = filter.ByDiffContent;
        DiffContent = filter.DiffContent;
        IgnoreCase = filter.IgnoreCase;
        ByCommitsLimit = filter.ByCommitsLimit;
        CommitsLimit = filter.CommitsLimit;
        ByPathFilter = filter.ByPathFilter;
        PathFilter = filter.PathFilter;
        ByBranchFilter = filter.ByBranchFilter;
        BranchFilter = filter.BranchFilter;
        ShowCurrentBranchOnly = filter.ShowCurrentBranchOnly;
        ShowReflogReferences = filter.ShowReflogReferences;
        ShowOnlyFirstParent = filter.ShowOnlyFirstParent;
        HideMergeCommits = filter.HideMergeCommits;
        ShowSimplifyByDecoration = filter.ShowSimplifyByDecoration;
        ShowFullHistory = filter.ShowFullHistory;
        ShowSimplifyMerges = filter.ShowSimplifyMerges;
    }

    public RevisionFilterStrings Strings { get; }

    /// <summary>The accepted filter, once accepted.</summary>
    public RevisionFilterValues? Result { get; private set; }

    [ObservableProperty]
    public partial bool ByDateFrom { get; set; }

    [ObservableProperty]
    public partial DateTime? DateFrom { get; set; }

    [ObservableProperty]
    public partial bool ByDateTo { get; set; }

    [ObservableProperty]
    public partial DateTime? DateTo { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanIgnoreCase))]
    public partial bool ByAuthor { get; set; }

    [ObservableProperty]
    public partial string Author { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanIgnoreCase))]
    public partial bool ByCommitter { get; set; }

    [ObservableProperty]
    public partial string Committer { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanIgnoreCase))]
    public partial bool ByMessage { get; set; }

    [ObservableProperty]
    public partial string Message { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanIgnoreCase))]
    public partial bool ByDiffContent { get; set; }

    [ObservableProperty]
    public partial string DiffContent { get; set; } = "";

    [ObservableProperty]
    public partial bool IgnoreCase { get; set; }

    /// <summary>Ignoring the case applies to the text filters only.</summary>
    public bool CanIgnoreCase => ByAuthor || ByCommitter || ByMessage || ByDiffContent;

    [ObservableProperty]
    public partial bool ByCommitsLimit { get; set; }

    [ObservableProperty]
    public partial int CommitsLimit { get; set; }

    [ObservableProperty]
    public partial bool ByPathFilter { get; set; }

    [ObservableProperty]
    public partial string PathFilter { get; set; } = "";

    [ObservableProperty]
    public partial bool ByBranchFilter { get; set; }

    [ObservableProperty]
    public partial string BranchFilter { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanFilterBranches))]
    public partial bool ShowCurrentBranchOnly { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanShowCurrentBranchOnly), nameof(CanFilterBranches))]
    public partial bool ShowReflogReferences { get; set; }

    public bool CanShowCurrentBranchOnly => !ShowReflogReferences;

    public bool CanFilterBranches => !ShowCurrentBranchOnly && !ShowReflogReferences;

    [ObservableProperty]
    public partial bool ShowOnlyFirstParent { get; set; }

    [ObservableProperty]
    public partial bool HideMergeCommits { get; set; }

    [ObservableProperty]
    public partial bool ShowSimplifyByDecoration { get; set; }

    [ObservableProperty]
    public partial bool ShowFullHistory { get; set; }

    /// <summary>Simplifying the merges has no effect without the full history.</summary>
    [ObservableProperty]
    public partial bool ShowSimplifyMerges { get; set; }

    partial void OnByCommitsLimitChanged(bool value)
    {
        // As FormRevisionFilter: turning the limit off shows the default limit.
        if (!value)
        {
            CommitsLimit = _commitsLimitDefault;
        }
    }

    [RelayCommand]
    private void Ok()
    {
        // Note: as in FormRevisionFilter, there is no validation of e.g. branch filters.
        Result = new RevisionFilterValues(
            ByDateFrom,
            DateFrom ?? DateTime.Today,
            ByDateTo,
            DateTo ?? DateTime.Today,
            ByAuthor,
            Author.Trim(),
            ByCommitter,
            Committer.Trim(),
            ByMessage,
            Message.Trim(),
            ByDiffContent,
            DiffContent.Trim(),
            IgnoreCase,
            ByCommitsLimit,
            CommitsLimit,
            ByPathFilter,
            PathFilter,
            ByBranchFilter,
            BranchFilter,
            ShowCurrentBranchOnly,
            ShowReflogReferences,
            ShowOnlyFirstParent,
            HideMergeCommits,
            ShowSimplifyByDecoration,
            ShowFullHistory,
            ShowSimplifyMerges);
        Close(accepted: true);
    }
}
