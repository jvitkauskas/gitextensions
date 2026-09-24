using GitExtensions.Extensibility.Git;
using GitExtensions.Plugins.GitImpact;
using GitExtensions.Plugins.GitStatistics;
using NSubstitute;
using TeamCityIntegration;
using TeamCityIntegration.Settings;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the Avalonia ports of the statistics, impact and TeamCity plugins (docs/avalonia-port/PLAN.md, phase 7).</summary>
[TestFixture]
public sealed class PluginStatisticsViewModelTests
{
    private string _directory = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"GitStatisticsVm-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(_directory, "src"));
        Directory.CreateDirectory(Path.Combine(_directory, "tests"));
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_directory, recursive: true);

    [Test]
    public async Task GitStatistics_counts_the_commits_and_the_lines_of_code()
    {
        File.WriteAllText(Path.Combine(_directory, "src", "Program.cs"), "// A comment\n\nclass Program\n{\n    static void Main() { }\n}\n");
        File.WriteAllText(Path.Combine(_directory, "tests", "ProgramTests.cs"), "class ProgramTests\n{\n}\n");
        File.WriteAllText(Path.Combine(_directory, "readme.md"), "Not code\n");
        IGitModule module = CreateModule("src/Program.cs", "tests/ProgramTests.cs", "readme.md");
        module.GetCommitsByContributor(Arg.Any<DateTime?>(), Arg.Any<DateTime?>()).Returns((3, new Dictionary<string, int> { ["Alice"] = 2, ["Bob"] = 1 }));
        GitStatisticsStrings strings = new();
        GitStatisticsViewModel viewModel = new(strings, module, _ => throw new InvalidOperationException("no submodules"), "*.cs", countSubmodules: false, @"\bin", new SynchronousBackgroundRunner());

        viewModel.TotalCommitsText.Should().Be("Total commits");
        viewModel.CommitStatisticsText.Should().Be("Loading..");
        await viewModel.LoadAsync();

        viewModel.TotalCommitsText.Should().Be("3 Commits");
        viewModel.CommitStatisticsText.Should().Be($"2 Alice{Environment.NewLine}1 Bob{Environment.NewLine}");
        viewModel.CommitSlices.Should().Equal(new PieSlice(2, "2 Commits by Alice"), new PieSlice(1, "1 Commits by Bob"));

        viewModel.TotalLinesOfCodeText.Should().Be("7 Lines of code");
        viewModel.TotalLinesOfCode2Text.Should().Be("7 Lines of code");
        viewModel.TotalLinesOfTestCodeText.Should().Be("3 Lines of test code");
        viewModel.ExtensionSlices.Should().ContainSingle().Which.Value.Should().Be(7);
        viewModel.TestSlices.Select(slice => slice.Value).Should().Equal(3m, 4m);
        viewModel.TestCodeText.Should().Be(
            string.Format(strings.LinesOfTestCodeP.Text, 3, 3 / 7.0) + Environment.NewLine + string.Format(strings.LinesOfProductionCodeP.Text, 4, 4 / 7.0));
        viewModel.TypeSlices.Select(slice => slice.Value).Should().HaveCount(4);
        viewModel.LinesOfCodePerTypeText.Should().Be(string.Join(Environment.NewLine, viewModel.TypeSlices.Select(slice => slice.ToolTip)));
    }

    [Test]
    public async Task GitStatistics_counts_the_submodules_when_asked()
    {
        File.WriteAllText(Path.Combine(_directory, "src", "A.cs"), "class A\n{\n}\n");
        Directory.CreateDirectory(Path.Combine(_directory, "sub"));
        File.WriteAllText(Path.Combine(_directory, "sub", "B.cs"), "class B\n{\n}\n");
        IGitModule module = CreateModule("src/A.cs");
        IGitSubmoduleInfo submodule = Substitute.For<IGitSubmoduleInfo>();
        submodule.LocalPath.Returns("sub");
        module.GetSubmodulesInfo().Returns([submodule]);
        module.GetCommitsByContributor(Arg.Any<DateTime?>(), Arg.Any<DateTime?>()).Returns((0, new Dictionary<string, int>()));
        List<string> opened = [];
        GitStatisticsViewModel viewModel = new(new GitStatisticsStrings(), module, path =>
        {
            opened.Add(path);
            IObjectGitItem item = Item("B.cs");
            IGitModule sub = Substitute.For<IGitModule>();
            sub.WorkingDir.Returns(path);
            sub.GetTree(Arg.Any<ObjectId>(), Arg.Any<bool>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([item]);
            return sub;
        }, "*.cs", countSubmodules: true, @"\bin", new SynchronousBackgroundRunner());

        await viewModel.LoadAsync();

        opened.Should().Equal(Path.Combine(_directory, "sub"));
        viewModel.TotalLinesOfCodeText.Should().Be("6 Lines of code");
    }

    [Test]
    public void Impact_sums_the_commits_per_week_and_author()
    {
        ImpactViewModel viewModel = new(new ImpactStrings(), loader: null, new SynchronousBackgroundRunner());
        DateOnly week1 = new(2024, 1, 7);
        DateOnly week2 = new(2024, 1, 14);
        DateOnly week3 = new(2024, 1, 21);

        viewModel.AddCommits(
        [
            new ImpactLoader.Commit(week1, "Alice", new ImpactLoader.DataPoint(1, 10, 2)),
            new ImpactLoader.Commit(week1, "Bob", new ImpactLoader.DataPoint(1, 100, 0)),
            new ImpactLoader.Commit(week1, "Alice", new ImpactLoader.DataPoint(1, 5, 5)),
            new ImpactLoader.Commit(week3, "Alice", new ImpactLoader.DataPoint(1, 1, 1)),
            new ImpactLoader.Commit(week2, "Bob", new ImpactLoader.DataPoint(1, 3, 0)),
        ]);

        ImpactSnapshot snapshot = viewModel.Snapshot;
        snapshot.AuthorStack.Should().Equal("Bob", "Alice");
        snapshot.Weeks.Select(w => w.Week).Should().Equal(week1, week2, week3);
        snapshot.Weeks[0].Blocks.Select(b => (b.Author, b.Data.ChangedLines)).Should().Equal(("Bob", 100), ("Alice", 22));

        // AddIntermediateEmptyWeeks: Alice has an empty block in the week between her commits.
        snapshot.Weeks[1].Blocks.Select(b => (b.Author, b.Data.Commits)).Should().Equal(("Bob", 1), ("Alice", 0));
        snapshot.Weeks[2].Blocks.Select(b => b.Author).Should().Equal("Alice");

        viewModel.HasSelectedAuthor.Should().BeFalse();
        viewModel.SelectedAuthor = "Alice";
        viewModel.SelectedAuthorText.Should().Be("Alice (3 Commits, 24 Changed Lines)");
        viewModel.GetAuthorInfo("Nobody").Commits.Should().Be(0);
    }

    [Test]
    public void Impact_starts_again_with_the_submodules()
    {
        ImpactViewModel viewModel = new(new ImpactStrings(), loader: null, new SynchronousBackgroundRunner());
        viewModel.AddCommits([new ImpactLoader.Commit(new DateOnly(2024, 1, 7), "Alice", new ImpactLoader.DataPoint(1, 1, 0))]);
        viewModel.SelectedAuthor = "Alice";

        viewModel.ShowSubmodules = true;

        viewModel.Snapshot.Weeks.Should().BeEmpty();
        viewModel.SelectedAuthor.Should().BeEmpty();
        viewModel.HasSelectedAuthor.Should().BeFalse();
    }

    [Test]
    public void TeamCityBuildChooser_selects_the_build_of_the_settings()
    {
        List<string> loaded = [];
        TeamCityBuildChooserViewModel viewModel = new(CreateProjects(), projectId => LoadBuilds(projectId, loaded), projectName: "Web", buildIdFilter: "Web_Deploy");

        viewModel.Nodes.Should().ContainSingle().Which.Text.Should().Be("Root");
        loaded.Should().Equal("_Root");
        viewModel.Nodes[0].Children.Select(n => n.Text).Should().Equal("Desktop", "Web");
        viewModel.Nodes[0].Children[0].Children.Select(n => n.Text).Should().Equal(TeamCityBuildChooserViewModel.Loading);

        viewModel.ReselectPreviouslySelectedBuild();

        loaded.Should().Equal("_Root", "Web");
        viewModel.SelectedNode!.Text.Should().Be("Deploy (Web_Deploy)");
        viewModel.Nodes[0].Children[1].Children.Select(n => n.Text).Should().Equal("Build (Web_Build)", "Deploy (Web_Deploy)");
        viewModel.SelectBuildCommand.CanExecute(null).Should().BeTrue();
    }

    [Test]
    public void TeamCityBuildChooser_loads_the_builds_of_an_expanded_project_once_and_chooses_a_build()
    {
        List<string> loaded = [];
        TeamCityBuildChooserViewModel viewModel = new(CreateProjects(), projectId => LoadBuilds(projectId, loaded), projectName: "", buildIdFilter: "");
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;
        TeamCityBuildNode desktop = viewModel.Nodes[0].Children[0];

        desktop.IsExpanded = true;
        desktop.IsExpanded = false;
        desktop.IsExpanded = true;

        loaded.Should().Equal("_Root", "Desktop");
        desktop.Children.Select(n => (n.Text, n.IsBuild)).Should().Equal(("Package (Desktop_Package)", true));
        viewModel.SelectedNode = desktop;
        viewModel.SelectBuildCommand.CanExecute(null).Should().BeFalse("a project is not a build");

        viewModel.SelectedNode = desktop.Children[0];
        viewModel.SelectBuildCommand.Execute(null);

        closed.Should().BeTrue();
        viewModel.TeamCityProjectName.Should().Be("Desktop");
        viewModel.TeamCityBuildIdFilter.Should().Be("Desktop_Package");
    }

    private IGitModule CreateModule(params string[] files)
    {
        IGitModule module = Substitute.For<IGitModule>();
        module.WorkingDir.Returns(_directory);
        IObjectGitItem[] items = [.. files.Select(Item)];
        module.GetTree(Arg.Any<ObjectId>(), Arg.Any<bool>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(items);
        module.GetSubmodulesInfo().Returns([]);
        return module;
    }

    private static IObjectGitItem Item(string name)
    {
        IObjectGitItem item = Substitute.For<IObjectGitItem>();
        item.Name.Returns(name);
        return item;
    }

    private static Project CreateProjects()
        => new()
        {
            Id = "_Root",
            Name = "Root",
            SubProjects =
            [
                new Project { Id = "Web", Name = "Web", ParentProject = "_Root", SubProjects = [] },
                new Project { Id = "Desktop", Name = "Desktop", ParentProject = "_Root", SubProjects = [] },
            ],
        };

    private static IList<Build> LoadBuilds(string projectId, List<string> loaded)
    {
        loaded.Add(projectId);
        return projectId switch
        {
            "Web" => [new Build { Id = "Web_Deploy", Name = "Deploy", ParentProject = "Web" }, new Build { Id = "Web_Build", Name = "Build", ParentProject = "Web" }],
            "Desktop" => [new Build { Id = "Desktop_Package", Name = "Package", ParentProject = "Desktop" }],
            _ => [],
        };
    }
}
