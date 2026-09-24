using GitExtensions.Extensibility;
using GitUI.AvaloniaHosting;

namespace TeamCityIntegration.Settings;

/// <summary>Shows the Avalonia port of <see cref="TeamCityBuildChooser"/> (docs/avalonia-port/PLAN.md, phase 7).</summary>
internal static class TeamCityBuildChooserDialog
{
    /// <summary>
    ///  Returns <see langword="false"/> when the port is disabled, in which case the caller shows the WinForms form; otherwise
    ///  <paramref name="chosen"/> is the chosen build, if any. As the constructor of the WinForms form, throws if the projects
    ///  cannot be read from the server.
    /// </summary>
    public static bool TryShow(IWin32Window owner, string serverUrl, string projectName, string buildIdFilter, out (string ProjectName, string BuildIdFilter)? chosen)
        => TryShow(owner.ToWindowOwner(), serverUrl, projectName, buildIdFilter, out chosen);

    /// <summary>As the other overload, owned by a window of plugin API v2 (<see cref="WindowOwner"/>).</summary>
    public static bool TryShow(WindowOwner owner, string serverUrl, string projectName, string buildIdFilter, out (string ProjectName, string BuildIdFilter)? chosen)
    {
        chosen = null;
        using TeamCityAdapter adapter = new();
        adapter.InitializeHttpClient(serverUrl);
        Project? rootProject = adapter.GetProjectsTree();

        TeamCityBuildChooserViewModel? viewModel = null;
        bool accepted = AvaloniaPluginDialogs.ShowDialog(
            () =>
            {
                viewModel = new TeamCityBuildChooserViewModel(rootProject, projectId => adapter.GetProjectBuilds(projectId), projectName, buildIdFilter);
                return new TeamCityBuildChooserWindow { DataContext = viewModel };
            },
            owner);

        if (accepted && viewModel is not null)
        {
            chosen = (viewModel.TeamCityProjectName, viewModel.TeamCityBuildIdFilter);
        }

        return true;
    }
}
