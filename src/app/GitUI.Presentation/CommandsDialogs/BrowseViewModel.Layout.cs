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

/// <summary>The layout settings of the main window (<c>AppSettings.ShowSplitViewLayout</c>, <c>AppSettings.CommitInfoPosition</c>).</summary>
public interface IBrowseLayoutHost
{
    bool ShowSplitViewLayout { get; set; }

    CommitInfoPosition CommitInfoPosition { get; set; }
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
