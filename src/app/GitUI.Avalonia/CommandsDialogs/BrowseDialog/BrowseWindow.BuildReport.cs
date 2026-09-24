using Avalonia;
using Avalonia.Controls;

namespace GitUI.Avalonia.CommandsDialogs.BrowseDialog;

/// <summary>The build report tab of the main window (<c>BuildReportTabPageExtension</c>).</summary>
public partial class BrowseWindow
{
    public TabItem BuildReportTab => buildReportTab;

    // As the removal of the tab page by FillBuildReport: when the tab is hidden while shown, the first tab is shown.
    private void OnBuildReportTabPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == IsVisibleProperty && !buildReportTab.IsVisible && tabs.SelectedItem == buildReportTab)
        {
            tabs.SelectedIndex = 0;
        }
    }
}
