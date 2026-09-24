using Avalonia.Threading;
using GitUI.Avalonia.Hosting;

namespace TeamCityIntegration.Settings;

/// <summary>Avalonia port of <c>TeamCityBuildChooser</c>; behaviour lives in <c>TeamCityBuildChooserViewModel</c>.</summary>
public partial class TeamCityBuildChooserWindow : DialogWindow
{
    public TeamCityBuildChooserWindow()
    {
        InitializeComponent();

        // As treeViewTeamCityProjects_MouseDoubleClick.
        projectsTree.DoubleTapped += (_, _) =>
        {
            if (DataContext is TeamCityBuildChooserViewModel viewModel && viewModel.SelectBuildCommand.CanExecute(null))
            {
                viewModel.SelectBuildCommand.Execute(null);
            }
        };

        // As TeamCityBuildChooser_Load.
        Opened += (_, _) => Dispatcher.UIThread.Post(() =>
        {
            (DataContext as TeamCityBuildChooserViewModel)?.ReselectPreviouslySelectedBuild();
            projectsTree.Focus();
        });
    }
}
