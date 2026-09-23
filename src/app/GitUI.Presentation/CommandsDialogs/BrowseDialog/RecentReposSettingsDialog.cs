using System.Collections;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands;
using GitCommands.UserRepositoryHistory;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs.BrowseDialog;

/// <summary>Strings of the recent repositories settings; ids match <c>FormRecentReposSettings</c>.</summary>
public sealed class RecentReposSettingsStrings : ViewStrings
{
    public RecentReposSettingsStrings()
        : base("FormRecentReposSettings")
    {
        Title = Add("$this", "Text", "Recent repositories settings");
        TopRepositories = Add("TopLabel", "Text", "Top repositories");
        RecentRepositories = Add("label1", "Text", "Recent repositories");
        MaxTopRepositories = Add("maxRecentRepositories", "Text", "Maximum number of top repositories");
        HistorySize = Add("lblRecentRepositoriesHistorySize", "Text", "Maximum number of recent repositories");
        HideTopRepositoriesFromRecentList = Add("hideTopRepositoriesFromRecentList", "Text", "Hide top repositories from recent repositories list");
        SortTopRepos = Add("sortTopRepos", "Text", "Sort top repositories alphabetically");
        SortRecentRepos = Add("sortRecentRepos", "Text", "Sort recent repositories alphabetically");
        ComboMinWidth = Add("comboMinWidthLabel", "Text", "Combobox minimum width (0 = Autosize)");
        ComboMinWidthNote = Add("comboMinWidthNote", "Text", "NB: The width of the columns helps to visualise how the repository name will be shown in the combobox.");
        ShorteningStrategy = Add("shorteningGB", "Text", "Shortening strategy");
        DontShorten = Add("dontShortenRB", "Text", "Do not shorten  ");
        MostSignificantDir = Add("mostSigDirRB", "Text", "The most significant directory ");
        MiddleDots = Add("middleDotRB", "Text", "Replace middle part with dots ");
        AnchorToTop = Add("anchorToTopReposToolStripMenuItem", "Text", "Anchor to top repositories");
        AnchorToRecent = Add("anchorToRecentReposToolStripMenuItem", "Text", "Anchor to recent repositories");
        RemoveAnchor = Add("removeAnchorToolStripMenuItem", "Text", "Remove anchor");
        RemoveFromRecent = Add("removeRecentToolStripMenuItem", "Text", "Remove from recent repositories");
        Ok = Add("Ok", "Text", "OK");
        Cancel = Add("Abort", "Text", "Cancel");
    }

    public TranslatedText Title { get; }

    public TranslatedText TopRepositories { get; }

    public TranslatedText RecentRepositories { get; }

    public TranslatedText MaxTopRepositories { get; }

    public TranslatedText HistorySize { get; }

    public TranslatedText HideTopRepositoriesFromRecentList { get; }

    public TranslatedText SortTopRepos { get; }

    public TranslatedText SortRecentRepos { get; }

    public TranslatedText ComboMinWidth { get; }

    public TranslatedText ComboMinWidthNote { get; }

    public TranslatedText ShorteningStrategy { get; }

    public TranslatedText DontShorten { get; }

    public TranslatedText MostSignificantDir { get; }

    public TranslatedText MiddleDots { get; }

    public TranslatedText AnchorToTop { get; }

    public TranslatedText AnchorToRecent { get; }

    public TranslatedText RemoveAnchor { get; }

    public TranslatedText RemoveFromRecent { get; }

    public TranslatedText Ok { get; }

    public TranslatedText Cancel { get; }
}

/// <summary>The recent repositories settings the dialog edits (<c>AppSettings</c>).</summary>
public sealed record RecentReposOptions(
    ShorteningRecentRepoPathStrategy ShorteningStrategy,
    bool HideTopRepositoriesFromRecentList,
    bool SortTopRepos,
    bool SortRecentRepos,
    int MaxTopRepositories,
    int ComboMinWidth,
    int RecentRepositoriesHistorySize);

/// <summary>A repository as listed.</summary>
/// <param name="Path">The repository path, shown as tooltip.</param>
/// <param name="Caption">The (shortened) name as the repository combobox shows it.</param>
/// <param name="Anchor">Where the repository is anchored.</param>
/// <param name="IsAnchoredInList">Whether it is anchored in the list it is shown in (shown in bold).</param>
/// <param name="Exists">Whether the directory exists (shown in red otherwise).</param>
public sealed record RecentRepoItem(string Path, string Caption, Repository.RepositoryAnchor Anchor, bool IsAnchoredInList, bool Exists);

/// <summary>Operations of the recent repositories settings that need the host (the repository history).</summary>
public interface IRecentReposSettingsHost
{
    /// <summary>Splits the (edited) repository history into the top and the recent repositories (<c>RecentRepoSplitter</c>).</summary>
    (IReadOnlyList<RecentRepoItem> Top, IReadOnlyList<RecentRepoItem> Recent) Split(RecentReposOptions options);

    /// <summary>Anchors the repositories in the edited history, which is only saved by <see cref="Save"/>.</summary>
    void SetAnchor(IEnumerable<string> paths, Repository.RepositoryAnchor anchor);

    /// <summary>Removes the repositories from the recent repositories (right away, as <c>FormRecentReposSettings</c>).</summary>
    void RemoveFromRecent(IEnumerable<string> paths);

    /// <summary>Saves the settings and the edited history.</summary>
    void Save(RecentReposOptions options);
}

/// <summary>View model of the recent repositories settings (port of <c>FormRecentReposSettings</c>).</summary>
public sealed partial class RecentReposSettingsViewModel : DialogViewModel
{
    /// <summary>The narrowest combobox width other than autosize (0).</summary>
    public const int MinComboWidthAllowed = 30;

    private readonly IRecentReposSettingsHost _host;
    private bool _initialized;

    public RecentReposSettingsViewModel(RecentReposSettingsStrings strings, RecentReposOptions options, IRecentReposSettingsHost host)
    {
        Strings = strings;
        _host = host;

        ShorteningStrategy = options.ShorteningStrategy;
        HideTopRepositoriesFromRecentList = options.HideTopRepositoriesFromRecentList;
        SortTopRepos = options.SortTopRepos;
        SortRecentRepos = options.SortRecentRepos;
        MaxTopRepositories = Math.Clamp(options.MaxTopRepositories, 0, 1_000_000);
        ComboMinWidth = Math.Clamp(options.ComboMinWidth, 0, 800);
        RecentRepositoriesHistorySize = Math.Clamp(options.RecentRepositoriesHistorySize, 10, 999);

        _initialized = true;
        Refresh();
    }

    public RecentReposSettingsStrings Strings { get; }

    public ObservableCollection<RecentRepoItem> TopRepos { get; } = [];

    public ObservableCollection<RecentRepoItem> RecentRepos { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDontShorten), nameof(IsMostSignificantDir), nameof(IsMiddleDots))]
    public partial ShorteningRecentRepoPathStrategy ShorteningStrategy { get; set; }

    public bool IsDontShorten
    {
        get => ShorteningStrategy == ShorteningRecentRepoPathStrategy.None;
        set => SetStrategy(value, ShorteningRecentRepoPathStrategy.None);
    }

    public bool IsMostSignificantDir
    {
        get => ShorteningStrategy == ShorteningRecentRepoPathStrategy.MostSignDir;
        set => SetStrategy(value, ShorteningRecentRepoPathStrategy.MostSignDir);
    }

    public bool IsMiddleDots
    {
        get => ShorteningStrategy == ShorteningRecentRepoPathStrategy.MiddleDots;
        set => SetStrategy(value, ShorteningRecentRepoPathStrategy.MiddleDots);
    }

    [ObservableProperty]
    public partial bool HideTopRepositoriesFromRecentList { get; set; }

    [ObservableProperty]
    public partial bool SortTopRepos { get; set; }

    [ObservableProperty]
    public partial bool SortRecentRepos { get; set; }

    [ObservableProperty]
    public partial int MaxTopRepositories { get; set; }

    /// <summary>The combobox width; 0 (autosize) or at least <see cref="MinComboWidthAllowed"/>.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CaptionWidth))]
    public partial int ComboMinWidth { get; set; }

    /// <summary>The width of the listed captions, which previews the combobox (<see cref="double.NaN"/> to autosize).</summary>
    public double CaptionWidth => ComboMinWidth == 0 ? double.NaN : ComboMinWidth;

    [ObservableProperty]
    public partial int RecentRepositoriesHistorySize { get; set; }

    private void SetStrategy(bool isChecked, ShorteningRecentRepoPathStrategy strategy)
    {
        if (isChecked)
        {
            ShorteningStrategy = strategy;
        }
    }

    partial void OnShorteningStrategyChanged(ShorteningRecentRepoPathStrategy value) => Refresh();

    partial void OnHideTopRepositoriesFromRecentListChanged(bool value) => Refresh();

    partial void OnSortTopReposChanged(bool value) => Refresh();

    partial void OnSortRecentReposChanged(bool value) => Refresh();

    partial void OnMaxTopRepositoriesChanged(int value) => Refresh();

    /// <summary>As <c>FormRecentReposSettings.comboMinWidthEdit_ValueChanged</c>: below the minimum snaps to 0 going down, to the minimum going up.</summary>
    partial void OnComboMinWidthChanged(int oldValue, int newValue)
    {
        if (newValue is > 0 and < MinComboWidthAllowed)
        {
            ComboMinWidth = newValue < oldValue ? 0 : MinComboWidthAllowed;
        }
    }

    private RecentReposOptions Options => new(
        ShorteningStrategy,
        HideTopRepositoriesFromRecentList,
        SortTopRepos,
        SortRecentRepos,
        MaxTopRepositories,
        ComboMinWidth,
        RecentRepositoriesHistorySize);

    private void Refresh()
    {
        if (!_initialized)
        {
            return;
        }

        (IReadOnlyList<RecentRepoItem> top, IReadOnlyList<RecentRepoItem> recent) = _host.Split(Options);
        Replace(TopRepos, top);
        Replace(RecentRepos, recent);

        static void Replace(ObservableCollection<RecentRepoItem> list, IReadOnlyList<RecentRepoItem> items)
        {
            list.Clear();
            foreach (RecentRepoItem item in items)
            {
                list.Add(item);
            }
        }
    }

    private static IReadOnlyList<RecentRepoItem> Items(object? selection)
        => selection is IEnumerable items ? [.. items.OfType<RecentRepoItem>()] : [];

    private static bool CanAnchorToTop(object? selection) => Items(selection).Any(r => r.Anchor != Repository.RepositoryAnchor.AnchoredInTop);

    private static bool CanAnchorToRecent(object? selection) => Items(selection).Any(r => r.Anchor != Repository.RepositoryAnchor.AnchoredInRecent);

    private static bool CanRemoveAnchor(object? selection) => Items(selection).Any(r => r.Anchor != Repository.RepositoryAnchor.None);

    private static bool HasSelection(object? selection) => Items(selection).Count > 0;

    /// <param name="selection">The selected <see cref="RecentRepoItem"/>s.</param>
    [RelayCommand(CanExecute = nameof(CanAnchorToTop))]
    private void AnchorToTop(object? selection) => Anchor(selection, Repository.RepositoryAnchor.AnchoredInTop);

    /// <param name="selection">The selected <see cref="RecentRepoItem"/>s.</param>
    [RelayCommand(CanExecute = nameof(CanAnchorToRecent))]
    private void AnchorToRecent(object? selection) => Anchor(selection, Repository.RepositoryAnchor.AnchoredInRecent);

    /// <param name="selection">The selected <see cref="RecentRepoItem"/>s.</param>
    [RelayCommand(CanExecute = nameof(CanRemoveAnchor))]
    private void RemoveAnchor(object? selection) => Anchor(selection, Repository.RepositoryAnchor.None);

    /// <param name="selection">The selected <see cref="RecentRepoItem"/>s.</param>
    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RemoveFromRecent(object? selection)
    {
        IReadOnlyList<RecentRepoItem> items = Items(selection);
        if (items.Count > 0)
        {
            _host.RemoveFromRecent([.. items.Select(r => r.Path)]);
            Refresh();
        }
    }

    private void Anchor(object? selection, Repository.RepositoryAnchor anchor)
    {
        IReadOnlyList<RecentRepoItem> items = Items(selection);
        if (items.Count > 0)
        {
            _host.SetAnchor([.. items.Select(r => r.Path)], anchor);
            Refresh();
        }
    }

    [RelayCommand]
    private void Ok()
    {
        _host.Save(Options);
        Close(accepted: true);
    }

    [RelayCommand]
    private void Cancel() => Close(accepted: false);
}
