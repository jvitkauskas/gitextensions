using CommunityToolkit.Mvvm.ComponentModel;
using GitUI.Presentation;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitExtensions.Plugins.GitImpact;

/// <summary>Strings of the Avalonia port of <see cref="FormImpact"/>; ids match the form.</summary>
public sealed class ImpactStrings : ViewStrings
{
    public ImpactStrings()
        : base(nameof(FormImpact))
    {
        Title = Add("$this", "Text", "Impact");
        AuthorCommits = Add("_authorCommits", "Text", "{0} ({1} Commits, {2} Changed Lines)");
        IncludingSubmodules = Add("cbIncludingSubmodules", "Text", "Including submodules");
    }

    public TranslatedText Title { get; }

    public TranslatedText AuthorCommits { get; }

    public TranslatedText IncludingSubmodules { get; }
}

/// <summary>The commits of one author in one week.</summary>
public sealed record ImpactBlock(string Author, ImpactLoader.DataPoint Data);

/// <summary>The authors of one week, the most changed lines first (the drawing order of <c>UpdatePathsAndLabels</c>).</summary>
public sealed record ImpactWeek(DateOnly Week, IReadOnlyList<ImpactBlock> Blocks);

/// <summary>
///  The data of the impact graph at a point of the loading: the weeks, and the authors in their drawing order
///  (<c>_authorStack</c>: the first author of the repository on top).
/// </summary>
public sealed record ImpactSnapshot(IReadOnlyList<ImpactWeek> Weeks, IReadOnlyList<string> AuthorStack)
{
    public static ImpactSnapshot Empty { get; } = new([], []);
}

/// <summary>
///  View model of the Avalonia port of <see cref="FormImpact"/>: the commits of <see cref="ImpactLoader"/> summed up per week
///  and author, as <c>ImpactControl.OnImpactUpdate</c> does; the view (<c>ImpactGraphView</c>) draws them.
/// </summary>
public sealed partial class ImpactViewModel : DialogViewModel, IDisposable
{
    private readonly ImpactLoader? _loader;
    private readonly IBackgroundRunner _backgroundRunner;
    private readonly Lock _dataLock = new();

    // <Author, <Commits, Added Lines, Deleted Lines>>
    private readonly Dictionary<string, ImpactLoader.DataPoint> _authors = [];

    // <First weekday of commit date, <Author, <Commits, Added Lines, Deleted Lines>>>
    private SortedDictionary<DateOnly, Dictionary<string, ImpactLoader.DataPoint>> _impact = [];

    // List of authors that determines the drawing order
    private readonly List<string> _authorStack = [];

    /// <param name="loader">Loads the commits (<see langword="null"/> in tests, which add them with <see cref="AddCommits"/>).</param>
    public ImpactViewModel(ImpactStrings strings, ImpactLoader? loader, IBackgroundRunner backgroundRunner)
    {
        Strings = strings;
        _loader = loader;
        _backgroundRunner = backgroundRunner;
        if (loader is not null)
        {
            loader.CommitLoaded += AddCommits;
        }
    }

    public ImpactStrings Strings { get; }

    /// <summary>The data to draw; replaced when commits are loaded.</summary>
    [ObservableProperty]
    public partial ImpactSnapshot Snapshot { get; private set; } = ImpactSnapshot.Empty;

    /// <summary>The author under the mouse (<c>ImpactControl.SelectedAuthor</c>), or the empty string.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedAuthor), nameof(SelectedAuthorText))]
    public partial string SelectedAuthor { get; set; } = "";

    public bool HasSelectedAuthor => !string.IsNullOrEmpty(SelectedAuthor);

    /// <summary>As <c>FormImpact.UpdateAuthorInfo</c>: the author with their commits and changed lines.</summary>
    public string SelectedAuthorText
    {
        get
        {
            if (!HasSelectedAuthor)
            {
                return "";
            }

            ImpactLoader.DataPoint data = GetAuthorInfo(SelectedAuthor);
            return string.Format(Strings.AuthorCommits.Text, SelectedAuthor, data.Commits, data.ChangedLines);
        }
    }

    [ObservableProperty]
    public partial bool ShowSubmodules { get; set; }

    /// <summary>As the constructor of <c>FormImpact</c> (<c>Impact.UpdateData</c>): starts loading.</summary>
    public void Start() => UpdateData();

    /// <summary>As <c>ImpactControl.GetAuthorInfo</c>.</summary>
    public ImpactLoader.DataPoint GetAuthorInfo(string author)
    {
        lock (_dataLock)
        {
            return _authors.TryGetValue(author, out ImpactLoader.DataPoint info) ? info : new ImpactLoader.DataPoint(0, 0, 0);
        }
    }

    /// <summary>As <c>ImpactControl.OnImpactUpdate</c>; called on the loading threads.</summary>
    public void AddCommits(IList<ImpactLoader.Commit> commits)
    {
        lock (_dataLock)
        {
            foreach (ImpactLoader.Commit commit in commits)
            {
                if (!_impact.TryGetValue(commit.Week, out Dictionary<string, ImpactLoader.DataPoint>? weekData))
                {
                    _impact.Add(commit.Week, weekData = []);
                }

                weekData[commit.Author] = weekData.TryGetValue(commit.Author, out ImpactLoader.DataPoint authorWeekData)
                    ? authorWeekData + commit.Data
                    : commit.Data;

                _authors[commit.Author] = _authors.TryGetValue(commit.Author, out ImpactLoader.DataPoint authorData)
                    ? authorData + commit.Data
                    : commit.Data;

                if (!_authorStack.Contains(commit.Author))
                {
                    // Added to the front (drawn first).
                    _authorStack.Insert(0, commit.Author);
                }
            }

            // Add authors to intermediate weeks where they didn't create commits
            ImpactLoader.AddIntermediateEmptyWeeks(ref _impact, _authors.Keys);
        }

        _backgroundRunner.Post(UpdateSnapshot);
    }

    public void Dispose()
    {
        // As FormImpact.OnFormClosed.
        if (_loader is not null)
        {
            _loader.CommitLoaded -= AddCommits;
            _loader.Stop();
            _loader.Dispose();
        }
    }

    /// <summary>As <c>cbShowSubmodules_CheckedChanged</c> and <c>ImpactControl.ShowSubmodules</c>.</summary>
    partial void OnShowSubmodulesChanged(bool value)
    {
        SelectedAuthor = "";
        _loader?.Stop();
        Clear();
        UpdateData();
    }

    private void UpdateData()
    {
        if (_loader is not null)
        {
            _loader.ShowSubmodules = ShowSubmodules;
            _loader.Execute();
        }
    }

    private void Clear()
    {
        lock (_dataLock)
        {
            _authors.Clear();
            _impact.Clear();
            _authorStack.Clear();
        }

        Snapshot = ImpactSnapshot.Empty;
    }

    private void UpdateSnapshot()
    {
        lock (_dataLock)
        {
            Snapshot = new ImpactSnapshot(
                [.. _impact.Select(week => new ImpactWeek(
                    week.Key,
                    [.. week.Value.OrderByDescending(entry => entry.Value.ChangedLines).Select(entry => new ImpactBlock(entry.Key, entry.Value))]))],
                [.. _authorStack]);
        }

        OnPropertyChanged(nameof(SelectedAuthorText));
    }
}
