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

        // As SplitterManager.SplitterData with the settings of FormBrowse: the keys of the WinForms splitters.
        public SplitterPosition? GetSplitter(string name)
        {
            string key = $"FormBrowse.{name}";
            int? distance = AppSettings.GetInt($"{key}_Distance");
            int? size = AppSettings.GetInt($"{key}_Size");
            bool? collapsed = AppSettings.GetBool($"{key}_Panel1Collapsed");
            if (distance is null && collapsed is null)
            {
                return null;
            }

            return new SplitterPosition(distance ?? 0, size ?? 0, AppSettings.GetInt($"{key}_Dpi") ?? 96, collapsed ?? false);
        }

        public void SaveSplitter(string name, SplitterPosition position)
        {
            string key = $"FormBrowse.{name}";
            AppSettings.SetInt($"{key}_Dpi", position.Dpi);
            AppSettings.SetInt($"{key}_Size", position.Size);
            AppSettings.SetInt($"{key}_Distance", position.Distance);
            AppSettings.SetBool($"{key}_Panel1Collapsed", position.Panel1Collapsed);
        }
    }
}
