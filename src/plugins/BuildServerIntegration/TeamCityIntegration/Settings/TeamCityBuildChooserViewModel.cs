using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitUI.Presentation;

namespace TeamCityIntegration.Settings;

/// <summary>
///  A node of the tree of the build chooser: a project (with its subprojects and, once expanded, its builds), a build, or the
///  "Loading..." placeholder of a project without subprojects (as the nodes of <c>TeamCityBuildChooser</c>).
/// </summary>
public sealed partial class TeamCityBuildNode : ObservableObject
{
    private readonly Func<string, IList<Build>>? _getProjectBuilds;

    private TeamCityBuildNode(string text, Project? project, Build? build, Func<string, IList<Build>>? getProjectBuilds)
    {
        Text = text;
        Project = project;
        Build = build;
        _getProjectBuilds = getProjectBuilds;
    }

    public string Text { get; }

    public Project? Project { get; }

    public Build? Build { get; }

    public bool IsBuild => Build is not null;

    public ObservableCollection<TeamCityBuildNode> Children { get; } = [];

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>As <c>ConvertProjectInTreeNode</c>.</summary>
    internal static TeamCityBuildNode ForProject(Project project, Func<string, IList<Build>> getProjectBuilds)
    {
        TeamCityBuildNode node = new(project.Name ?? "", project, build: null, getProjectBuilds);
        foreach (TeamCityBuildNode child in (project.SubProjects ?? []).Select(p => ForProject(p, getProjectBuilds)).OrderBy(p => p.Project!.Name))
        {
            node.Children.Add(child);
        }

        if (node.Children.Count == 0)
        {
            node.Children.Add(new TeamCityBuildNode(TeamCityBuildChooserViewModel.Loading, project: null, build: null, getProjectBuilds: null));
        }

        return node;
    }

    /// <summary>As <c>treeViewTeamCityProjects_BeforeExpand</c>.</summary>
    partial void OnIsExpandedChanged(bool value)
    {
        if (value)
        {
            LoadProjectBuilds();
        }
    }

    /// <summary>As <c>LoadProjectBuilds</c>: the builds are read when the project is expanded the first time.</summary>
    private void LoadProjectBuilds()
    {
        if (Project is not { Builds: null, Id: string projectId } || _getProjectBuilds is null)
        {
            return;
        }

        Project.Builds = _getProjectBuilds(projectId);

        // Remove the "Loading..." node.
        if (Children.Count == 1 && Children[0] is { Project: null, Build: null })
        {
            Children.RemoveAt(0);
        }

        foreach (Build build in Project.Builds.OrderBy(b => b.Id))
        {
            Children.Add(new TeamCityBuildNode(build.DisplayName, project: null, build, getProjectBuilds: null));
        }
    }
}

/// <summary>View model of the Avalonia port of <c>TeamCityBuildChooser</c> (a form without translations: constant strings).</summary>
public sealed partial class TeamCityBuildChooserViewModel : DialogViewModel
{
    public const string Title = "Choose the TeamCity build...";
    public const string Ok = "OK";
    public const string Cancel = "Cancel";
    public const string Loading = "Loading...";

    /// <param name="rootProject">The tree of the projects (<c>GetProjectsTree</c>), if any.</param>
    /// <param name="getProjectBuilds">Reads the builds of a project (<c>GetProjectBuilds</c>).</param>
    /// <param name="projectName">The project of the settings, whose build is selected.</param>
    /// <param name="buildIdFilter">The build of the settings.</param>
    public TeamCityBuildChooserViewModel(Project? rootProject, Func<string, IList<Build>> getProjectBuilds, string projectName, string buildIdFilter)
    {
        TeamCityProjectName = projectName;
        TeamCityBuildIdFilter = buildIdFilter;

        if (rootProject is not null)
        {
            TeamCityBuildNode root = TeamCityBuildNode.ForProject(rootProject, getProjectBuilds);
            Nodes.Add(root);
            root.IsExpanded = true;
        }
    }

    public ObservableCollection<TeamCityBuildNode> Nodes { get; } = [];

    /// <summary>The project of the chosen build.</summary>
    public string TeamCityProjectName { get; private set; }

    /// <summary>The id of the chosen build.</summary>
    public string TeamCityBuildIdFilter { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SelectBuildCommand))]
    public partial TeamCityBuildNode? SelectedNode { get; set; }

    /// <summary>As <c>TeamCityBuildChooser_Load</c> (<c>ReselectPreviouslySelectedBuild</c>).</summary>
    public void ReselectPreviouslySelectedBuild()
    {
        if (FindProject(Nodes, TeamCityProjectName) is not { } project)
        {
            return;
        }

        project.IsExpanded = true;
        SelectedNode = project.Children.FirstOrDefault(node => node.Build?.Id == TeamCityBuildIdFilter) ?? project;
    }

    /// <summary>As <c>SelectBuild</c> (OK, or a double click on a build).</summary>
    [RelayCommand(CanExecute = nameof(IsBuildSelected))]
    private void SelectBuild()
    {
        if (SelectedNode?.Build is { ParentProject: string parentProject, Id: string id })
        {
            TeamCityProjectName = parentProject;
            TeamCityBuildIdFilter = id;
            Close(accepted: true);
        }
    }

    [RelayCommand]
    private void CancelDialog() => Close(accepted: false);

    private bool IsBuildSelected() => SelectedNode?.IsBuild == true;

    private static TeamCityBuildNode? FindProject(IEnumerable<TeamCityBuildNode> nodes, string projectId)
    {
        foreach (TeamCityBuildNode node in nodes)
        {
            if (node.Project?.Id == projectId)
            {
                return node;
            }

            if (FindProject(node.Children, projectId) is { } found)
            {
                // As selecting a node of a WinForms tree view, its ancestors are expanded.
                node.IsExpanded = true;
                return found;
            }
        }

        return null;
    }
}
