using GitCommands;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.AvaloniaHosting;

/// <summary>The layout settings of the Avalonia main window (<c>RefreshSplitViewLayout</c>, <c>SetCommitInfoPosition</c>).</summary>
internal static partial class AvaloniaDialogs
{
    private sealed partial class BrowseHost : IBrowseLayoutHost
    {
        public bool ShowSplitViewLayout
        {
            get => AppSettings.ShowSplitViewLayout;
            set => AppSettings.ShowSplitViewLayout = value;
        }

        public CommitInfoPosition CommitInfoPosition
        {
            get => AppSettings.CommitInfoPosition;
            set => AppSettings.CommitInfoPosition = value;
        }
    }
}
