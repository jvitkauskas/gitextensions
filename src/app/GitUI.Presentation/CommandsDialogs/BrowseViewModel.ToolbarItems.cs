using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the default pull action and the Toolbars menu; ids match <c>FormBrowse</c> and <c>FormBrowseMenus</c>.</summary>
public sealed class BrowseToolbarItemsStrings : ViewStrings
{
    public BrowseToolbarItemsStrings()
        : base("FormBrowse")
    {
        SetDefaultPullAction = Add("setDefaultPullButtonActionToolStripMenuItem", "Text", "Set &default Pull button action");
        PullFetch = Add("_pullFetch", "Text", "Fetch");
        PullFetchAll = Add("_pullFetchAll", "Text", "Fetch all");
        PullFetchPruneAll = Add("_pullFetchPruneAll", "Text", "Fetch and prune all");
        PullMerge = Add("_pullMerge", "Text", "Pull - merge");
        PullRebase = Add("_pullRebase", "Text", "Pull - rebase");
        PullOpenDialog = Add("_pullOpenDialog", "Text", "Open pull dialog");
        Toolbars = Add("toolbarsMenuItem", "Text", "Toolbars");
        StandardToolbar = Add("ToolStripMain", "Text", "Standard");
        FiltersToolbar = Add("ToolStripFilters", "Text", "Filters");
        ScriptsToolbar = Add("ToolStripScripts", "Text", "Scripts");
        CloseAllWindows = Add("_closeAll", "Text", "Close all windows");
    }

    public TranslatedText SetDefaultPullAction { get; }

    public TranslatedText PullFetch { get; }

    public TranslatedText PullFetchAll { get; }

    public TranslatedText PullFetchPruneAll { get; }

    public TranslatedText PullMerge { get; }

    public TranslatedText PullRebase { get; }

    public TranslatedText PullOpenDialog { get; }

    public TranslatedText Toolbars { get; }

    public TranslatedText StandardToolbar { get; }

    public TranslatedText FiltersToolbar { get; }

    public TranslatedText ScriptsToolbar { get; }

    /// <summary>The thumbnail button of the taskbar that closes all the windows.</summary>
    public TranslatedText CloseAllWindows { get; }
}

/// <summary>The settings of the pull button and of the items of the toolbars (<c>AppSettings.DefaultPullAction</c>, <c>formbrowse_toolbar_visibility_</c>).</summary>
public interface IBrowseToolbarItemsHost
{
    GitPullAction DefaultPullAction { get; set; }

    /// <summary>Whether the item (the name of its WinForms <c>ToolStripItem</c>) is shown.</summary>
    bool GetToolbarItemVisibility(string key, bool defaultValue);

    /// <summary>Saves whether the item is shown (as the default, nothing is saved).</summary>
    void SetToolbarItemVisibility(string key, bool visible, bool defaultValue);
}

/// <summary>
///  Whether each item of the main toolbar is shown, by the name of its WinForms <c>ToolStripItem</c> (bound as
///  <c>ToolbarItems[RefreshButton]</c>): the toolbar is shown and the item is not hidden in the Toolbars menu.
/// </summary>
public sealed class BrowseToolbarItemVisibility : INotifyPropertyChanged
{
    private readonly Dictionary<string, bool> _visible = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool this[string key] => IsToolbarShown && IsItemShown(key);

    /// <summary>Whether the Standard toolbar is shown (not saved, as <c>ToolStripMain.Visible</c>).</summary>
    public bool IsToolbarShown { get; private set; } = true;

    /// <summary>Reads the saved visibility of an item, the first time it is needed.</summary>
    internal Func<string, bool>? LoadVisibility { get; set; }

    public bool IsItemShown(string key)
    {
        if (!_visible.TryGetValue(key, out bool visible))
        {
            visible = LoadVisibility?.Invoke(key) ?? BrowseViewModel.IsToolbarItemVisibleByDefault(key);
            _visible[key] = visible;
        }

        return visible;
    }

    internal void SetItemShown(string key, bool visible)
    {
        _visible[key] = visible;
        Changed();
    }

    internal void SetToolbarShown(bool shown)
    {
        IsToolbarShown = shown;
        Changed();
    }

    // "Item": the indexer changed, as the bindings of ToolbarItems[key] expect.
    private void Changed() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item"));
}

/// <summary>The default pull action (<c>RefreshDefaultPullAction</c>), the fetch and pull shortcut buttons (<c>InsertFetchPullShortcuts</c>) and the Toolbars menu (<c>FormBrowseMenus.CreateToolbarsMenus</c>).</summary>
public sealed partial class BrowseViewModel
{
    /// <summary>The prefix of the names of the fetch and pull shortcut buttons (<c>FetchPullToolbarShortcutsPrefix</c>).</summary>
    public const string FetchPullShortcutsPrefix = "pull_shortcut_";

    private IBrowseToolbarItemsHost? _toolbarItemsHost;

    public BrowseToolbarItemsStrings ToolbarItemsStrings { get; } = ViewStrings.Load<BrowseToolbarItemsStrings>();

    public BrowseToolbarItemVisibility ToolbarItems { get; } = new();

    /// <summary>The action of a click on the pull button (<c>AppSettings.DefaultPullAction</c>).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PullButtonIcon), nameof(PullButtonToolTip), nameof(PullItems))]
    public partial GitPullAction DefaultPullAction { get; set; }

    /// <summary>Whether the Filters toolbar is shown (<c>ToolStripFilters.Visible</c>).</summary>
    [ObservableProperty]
    public partial bool ShowFiltersToolbar { get; set; } = true;

    /// <summary>Whether the Scripts toolbar is shown (<c>ToolStripScripts.Visible</c>).</summary>
    [ObservableProperty]
    public partial bool ShowScriptsToolbar { get; set; } = true;

    public string PullButtonIcon => GetPullAction(DefaultPullAction).Icon;

    public string PullButtonToolTip => DefaultPullAction switch
    {
        GitPullAction.Fetch => ToolbarItemsStrings.PullFetch.Text,
        GitPullAction.FetchAll => ToolbarItemsStrings.PullFetchAll.Text,
        GitPullAction.FetchPruneAll => ToolbarItemsStrings.PullFetchPruneAll.Text,
        GitPullAction.Merge => ToolbarItemsStrings.PullMerge.Text,
        GitPullAction.Rebase => ToolbarItemsStrings.PullRebase.Text,
        _ => ToolbarItemsStrings.PullOpenDialog.Text,
    };

    /// <summary>The fetch and pull shortcut buttons, before the pull button; hidden unless shown in the Toolbars menu.</summary>
    public IReadOnlyList<BrowseToolbarShortcut> FetchPullShortcuts =>
    [
        Shortcut("fetchToolStripMenuItem", Strings.Fetch, BrowseCommand.Fetch, "PullFetch"),
        Shortcut("fetchAllToolStripMenuItem", Strings.FetchAll, BrowseCommand.FetchAll, "PullFetchAll"),
        Shortcut("fetchPruneAllToolStripMenuItem", Strings.FetchPruneAll, BrowseCommand.FetchPruneAll, "PullFetchPruneAll"),
        Shortcut("mergeToolStripMenuItem", Strings.PullMerge, BrowseCommand.PullMerge, "PullMerge"),
        Shortcut("rebaseToolStripMenuItem1", Strings.PullRebase, BrowseCommand.PullRebase, "PullRebase"),
        Shortcut("pullToolStripMenuItem1", Strings.OpenPullDialog, BrowseCommand.OpenPullDialog, "Pull"),
    ];

    /// <summary>The Toolbars menu (View menu and the context menu of the toolbars).</summary>
    public BrowseMenuItem ToolbarsMenu => new(ToolbarItemsStrings.Toolbars.AccessKeyText, null, Children: GetToolbarsMenuItems());

    /// <summary>The items of the Toolbars menu: each toolbar with, in its submenu, the toolbar itself and its items.</summary>
    public IReadOnlyList<BrowseMenuItem> GetToolbarsMenuItems()
    {
        List<BrowseMenuItem> standard = [ToggleItem(ToolbarItemsStrings.StandardToolbar, ToolbarItems.IsToolbarShown, () => ToolbarItems.SetToolbarShown(!ToolbarItems.IsToolbarShown)), BrowseMenuItem.Separator];
        foreach ((string key, string header, object? icon) in GetStandardToolbarItems())
        {
            standard.Add(new BrowseMenuItem(header, null, icon)
            {
                IsChecked = ToolbarItems.IsItemShown(key),
                Invoke = () => SetToolbarItemShown(key, !ToolbarItems.IsItemShown(key)),
            });
        }

        List<BrowseMenuItem> toolbars = [new(ToolbarItemsStrings.StandardToolbar.AccessKeyText, null, Children: standard)];
        if (Filters is { } filters)
        {
            List<BrowseMenuItem> filterItems = [ToggleItem(ToolbarItemsStrings.FiltersToolbar, ShowFiltersToolbar, () => ShowFiltersToolbar = !ShowFiltersToolbar), BrowseMenuItem.Separator];
            foreach ((string key, string header, object? icon) in GetFilterToolbarItems(filters))
            {
                filterItems.Add(new BrowseMenuItem(header, null, icon)
                {
                    IsChecked = filters.ItemVisibility.IsItemShown(key),
                    Invoke = () => SetFilterToolbarItemShown(key, !filters.ItemVisibility.IsItemShown(key)),
                });
            }

            toolbars.Add(new(ToolbarItemsStrings.FiltersToolbar.AccessKeyText, null, Children: filterItems));
        }

        toolbars.Add(ToggleItem(ToolbarItemsStrings.ScriptsToolbar, ShowScriptsToolbar, () => ShowScriptsToolbar = !ShowScriptsToolbar));
        return toolbars;

        static BrowseMenuItem ToggleItem(TranslatedText text, bool isChecked, Action invoke)
            => new(text.AccessKeyText, null) { IsChecked = isChecked, Invoke = invoke };
    }

    /// <summary>As <c>IsVisibleByDefault</c>: all items but the fetch and pull shortcuts.</summary>
    internal static bool IsToolbarItemVisibleByDefault(string key) => !key.StartsWith(FetchPullShortcutsPrefix, StringComparison.Ordinal);

    /// <summary>Shows or hides an item (or group) of the Filters toolbar, and saves it.</summary>
    public void SetFilterToolbarItemShown(string key, bool visible)
    {
        Filters?.ItemVisibility.SetItemShown(key, visible);
        _toolbarItemsHost?.SetToolbarItemVisibility(GetFilterSettingKey(key), visible, defaultValue: true);
    }

    // The groups are saved by their group name (the Tag "ToolBar_group:..." of their WinForms items).
    private static string GetFilterSettingKey(string key) => key switch
    {
        "BranchFilter" => "ToolBar_group:Branch filter",
        "TextFilter" => "ToolBar_group:Text filter",
        _ => key,
    };

    // The items of ToolStripFilters with their tooltips as the headers.
    private static IEnumerable<(string Key, string Header, object? Icon)> GetFilterToolbarItems(UserControls.RevisionGrid.FilterToolBarViewModel filters)
    {
        yield return ("tsbtnAdvancedFilter", filters.Strings.AdvancedFilterToolTip.Text, "FunnelPencil");
        yield return ("tsbShowReflog", filters.Strings.ShowReflogToolTip.Text, "Book");
        yield return ("tssbtnShowBranches", filters.BranchesModeToolTip, filters.BranchesModeIcon);
        yield return ("BranchFilter", filters.Strings.BranchesToolTip.Text, null);
        yield return ("TextFilter", filters.Strings.FilterToolTip.Text, null);
        yield return ("tsmiShowOnlyFirstParent", filters.Strings.ShowOnlyFirstParent.Text, "ShowOnlyFirstParent");
    }

    /// <summary>Shows or hides an item of the main toolbar, and saves it.</summary>
    public void SetToolbarItemShown(string key, bool visible)
    {
        ToolbarItems.SetItemShown(key, visible);
        _toolbarItemsHost?.SetToolbarItemVisibility(key, visible, IsToolbarItemVisibleByDefault(key));
    }

    // As FillNextPullActionAsDefaultToolStripMenuItems: the pull actions, the selected one checked.
    private BrowseMenuItem CreateDefaultPullActionMenu()
    {
        GitPullAction[] actions = [GitPullAction.Merge, GitPullAction.Rebase, GitPullAction.Fetch, GitPullAction.FetchAll, GitPullAction.FetchPruneAll, GitPullAction.None];
        return new BrowseMenuItem(ToolbarItemsStrings.SetDefaultPullAction.AccessKeyText, null, Children:
            [.. actions.Select(action =>
            {
                (TranslatedText text, string icon) = GetPullAction(action);
                return new BrowseMenuItem(text.AccessKeyText, null, icon) { IsChecked = action == DefaultPullAction, Invoke = () => DefaultPullAction = action };
            })]);
    }

    private (TranslatedText Text, string Icon) GetPullAction(GitPullAction action) => action switch
    {
        GitPullAction.Fetch => (Strings.Fetch, "PullFetch"),
        GitPullAction.FetchAll => (Strings.FetchAll, "PullFetchAll"),
        GitPullAction.FetchPruneAll => (Strings.FetchPruneAll, "PullFetchPruneAll"),
        GitPullAction.Merge => (Strings.PullMerge, "PullMerge"),
        GitPullAction.Rebase => (Strings.PullRebase, "PullRebase"),
        _ => (Strings.OpenPullDialog, "Pull"),
    };

    // The items of ToolStripMain with their tooltips as the headers, in the order of the toolbar.
    private IEnumerable<(string Key, string Header, object? Icon)> GetStandardToolbarItems()
    {
        yield return ("RefreshButton", Strings.Refresh.Text, "ReloadRevisions");
        if (LeftPanel is not null)
        {
            yield return ("toggleLeftPanel", LeftPanel.Strings.ToggleLeftPanel.Text, "LayoutSidebarLeft");
        }

        yield return ("toggleSplitViewLayout", LayoutStrings.ToggleSplitViewLayout.Text, "LayoutFooter");
        yield return ("menuCommitInfoPosition", LayoutStrings.CommitInfoPosition.Text, CommitInfoPositionIcon);
        yield return ("toolStripButtonLevelUp", SubmodulesToolTip, SubmodulesIcon);
        yield return ("toolStripWorktrees", ToolbarStrings.WorktreesToolTip.Text, "WorkTree");
        yield return ("_NO_TRANSLATE_WorkingDir", ToolbarStrings.WorkingDirToolTip.Text, "RepoOpen");
        yield return ("branchSelect", Strings.BranchToolTip.Text, "BranchCheckout");
        foreach (BrowseToolbarShortcut shortcut in FetchPullShortcuts)
        {
            yield return (shortcut.Key, shortcut.ToolTip, shortcut.Icon);
        }

        yield return ("toolStripButtonPull", PullButtonToolTip, PullButtonIcon);
        yield return ("toolStripButtonPush", Strings.PushButton.Text, "Push");
        yield return ("toolStripButtonCommit", Strings.CommitButton.Text, CommitButtonIcon);
        yield return ("toolStripSplitStash", Strings.ManageStashesToolTip.Text, "Stash");
        yield return ("toolStripFileExplorer", Strings.FileExplorerToolTip.Text, "BrowseFileExplorer");
        yield return ("userShell", DefaultShell?.Name ?? Strings.GitBashToolTip.Text, DefaultShell?.Icon ?? "GitForWindows");
        yield return ("EditSettings", Strings.SettingsToolTip.Text, "Settings");
    }

    private BrowseToolbarShortcut Shortcut(string menuItemName, TranslatedText text, BrowseCommand command, string icon)
        => new(FetchPullShortcutsPrefix + menuItemName, text.PlainText, command, icon);

    private void InitializeToolbarItems()
    {
        _toolbarItemsHost = _host as IBrowseToolbarItemsHost;
        if (_toolbarItemsHost is null)
        {
            return;
        }

        DefaultPullAction = _toolbarItemsHost.DefaultPullAction;

        // Read when first shown: the left panel and the filters are set after the constructor.
        IBrowseToolbarItemsHost host = _toolbarItemsHost;
        ToolbarItems.LoadVisibility = key => host.GetToolbarItemVisibility(key, IsToolbarItemVisibleByDefault(key));
    }

    // As SetDefaultPullActionMenuItemClick: saved.
    partial void OnDefaultPullActionChanged(GitPullAction value) => _toolbarItemsHost?.DefaultPullAction = value;
}

/// <summary>A fetch or pull shortcut button (<c>CreateCorrespondingToolbarButton</c>): the text of its menu item as the tooltip.</summary>
public sealed record BrowseToolbarShortcut(string Key, string ToolTip, BrowseCommand Command, string Icon);
