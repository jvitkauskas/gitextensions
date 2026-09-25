using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the layout buttons of the main toolbar; ids match <c>FormBrowse</c>.</summary>
public sealed class BrowseLayoutStrings : ViewStrings
{
    public BrowseLayoutStrings()
        : base("FormBrowse")
    {
        ToggleSplitViewLayout = Add("toggleSplitViewLayout", "ToolTipText", "Toggle split view layout");
        CommitInfoPosition = Add("menuCommitInfoPosition", "ToolTipText", "Commit info position");
        CommitInfoBelow = Add("commitInfoBelowMenuItem", "Text", "Commit info &below graph");
        CommitInfoLeftward = Add("commitInfoLeftwardMenuItem", "Text", "Commit info &left of graph");
        CommitInfoRightward = Add("commitInfoRightwardMenuItem", "Text", "Commit info &right of graph");
    }

    public TranslatedText ToggleSplitViewLayout { get; }

    public TranslatedText CommitInfoPosition { get; }

    public TranslatedText CommitInfoBelow { get; }

    public TranslatedText CommitInfoLeftward { get; }

    public TranslatedText CommitInfoRightward { get; }
}

/// <summary>
///  A saved splitter of a window, as <c>SplitterManager</c> saves a <c>SplitContainer</c>: the distance of the splitter and
///  the size of the container in pixels at <paramref name="Dpi"/>, and whether its first panel is collapsed.
/// </summary>
public sealed record SplitterPosition(int Distance, int Size, int Dpi, bool Panel1Collapsed = false);

/// <summary>The layout settings of the main window (<c>AppSettings.ShowSplitViewLayout</c>, <c>AppSettings.CommitInfoPosition</c>).</summary>
public interface IBrowseLayoutHost
{
    bool ShowSplitViewLayout { get; set; }

    CommitInfoPosition CommitInfoPosition { get; set; }

    /// <summary>The saved splitter (<c>SplitterManager.RestoreSplitters</c>, <c>FormBrowse.{name}_Distance</c>, ...), if any.</summary>
    SplitterPosition? GetSplitter(string name) => null;

    /// <summary>Saves the splitter (<c>SplitterManager.SaveSplitters</c>, when the window closes).</summary>
    void SaveSplitter(string name, SplitterPosition position)
    {
    }
}

/// <summary>The layout of the main window: the tabs below the grid, and where the commit info is (<c>LayoutRevisionInfo</c>).</summary>
public sealed partial class BrowseViewModel
{
    private IBrowseLayoutHost? _layoutHost;

    public BrowseLayoutStrings LayoutStrings { get; } = ViewStrings.Load<BrowseLayoutStrings>();

    /// <summary>Whether the tabs are shown below the grid (<c>toggleSplitViewLayout</c>).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowTabs))]
    public partial bool ShowSplitViewLayout { get; set; } = true;

    /// <summary>Where the commit info is: its tab, or beside the grid (<c>menuCommitInfoPosition</c>).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CommitInfoPositionIcon), nameof(IsCommitInfoInTab))]
    public partial CommitInfoPosition CommitInfoPosition { get; set; }

    /// <summary>Whether the tabs below the grid are shown: a repository, with the split view layout.</summary>
    public bool ShowTabs => !IsDashboard && ShowSplitViewLayout;

    public bool IsCommitInfoInTab => CommitInfoPosition == CommitInfoPosition.BelowList;

    /// <summary>As <c>RefreshLayoutToggleButtonStates</c>: the image of the selected position.</summary>
    public string CommitInfoPositionIcon => CommitInfoPosition switch
    {
        CommitInfoPosition.LeftwardFromList => "LayoutSidebarTopLeft",
        CommitInfoPosition.RightwardFromList => "LayoutSidebarTopRight",
        _ => "LayoutFooterTab",
    };

    /// <summary>The items of the commit info position button.</summary>
    public IReadOnlyList<BrowseMenuItem> CommitInfoPositionItems =>
    [
        PositionItem(LayoutStrings.CommitInfoBelow, CommitInfoPosition.BelowList, "LayoutFooterTab"),
        PositionItem(LayoutStrings.CommitInfoLeftward, CommitInfoPosition.LeftwardFromList, "LayoutSidebarTopLeft"),
        PositionItem(LayoutStrings.CommitInfoRightward, CommitInfoPosition.RightwardFromList, "LayoutSidebarTopRight"),
    ];

    [RelayCommand]
    private void ToggleSplitViewLayout() => ShowSplitViewLayout = !ShowSplitViewLayout;

    /// <summary>The saved splitter of the window (the <c>SplitterManager</c> of <c>FormBrowse</c>), if any.</summary>
    public SplitterPosition? GetSplitter(string name) => _layoutHost?.GetSplitter(name);

    /// <summary>Saves a splitter of the window (when it closes).</summary>
    public void SaveSplitter(string name, SplitterPosition position) => _layoutHost?.SaveSplitter(name, position);

    private BrowseMenuItem PositionItem(TranslatedText text, CommitInfoPosition position, string icon)
        => new(text.AccessKeyText, null, icon) { Invoke = () => CommitInfoPosition = position };

    private void InitializeLayout()
    {
        _layoutHost = _host as IBrowseLayoutHost;
        if (_layoutHost is null)
        {
            return;
        }

        ShowSplitViewLayout = _layoutHost.ShowSplitViewLayout;
        CommitInfoPosition = _layoutHost.CommitInfoPosition;
    }

    // As RefreshSplitViewLayout: saved.
    partial void OnShowSplitViewLayoutChanged(bool value) => _layoutHost?.ShowSplitViewLayout = value;

    // As SetCommitInfoPosition: saved; beside the grid, the commit info has no tab any more.
    partial void OnCommitInfoPositionChanged(CommitInfoPosition value)
    {
        _layoutHost?.CommitInfoPosition = value;
        if (value != CommitInfoPosition.BelowList && SelectedTab == BrowseTab.Commit)
        {
            SelectedTab = BrowseTab.Diff;
        }
    }
}
