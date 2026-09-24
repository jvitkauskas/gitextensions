using Avalonia.Controls;
using GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

namespace GitUI.Avalonia.CommandsDialogs.SettingsDialog.Pages;

public partial class GitConfigSettingsPageView : UserControl
{
    public GitConfigSettingsPageView()
    {
        InitializeComponent();

        // As txtDiffMergeToolPath_LostFocus: the paths of the tools are converted to posix paths.
        mergeToolPath.LostFocus += (_, _) => (DataContext as GitConfigSettingsPageViewModel)?.NormalizeMergeToolPath();
        diffToolPath.LostFocus += (_, _) => (DataContext as GitConfigSettingsPageViewModel)?.NormalizeDiffToolPath();
    }
}
