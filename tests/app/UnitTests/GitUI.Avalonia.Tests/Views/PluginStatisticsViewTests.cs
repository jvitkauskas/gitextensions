using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GitExtensions.Extensibility.Git;
using GitExtensions.Plugins.GitImpact;
using GitExtensions.Plugins.GitStatistics;
using GitUI.AvaloniaTests.ViewModels;
using NSubstitute;
using TeamCityIntegration;
using TeamCityIntegration.Settings;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the statistics, impact and TeamCity plugins (docs/avalonia-port/PLAN.md, phase 7).</summary>
[TestFixture]
public sealed class PluginStatisticsViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        GitStatisticsViewModel viewModel = CreateStatistics();
        GitStatisticsWindow statistics = Show(new GitStatisticsWindow { DataContext = viewModel });

        // The commits are counted when the window opens; the lines of code of the test repository are made up (no files are read).
        viewModel.ShowLinesOfCode(new LinesOfCodeCounts(1650, 400, 250, 180, 120, 2200, [new(".cs", 1200), new(".xml", 300), new(".js", 150)]));
        TabControl tabs = statistics.FindControl<TabControl>("tabs")!;
        string[] names = ["commits", "languages", "types", "test-code"];
        for (int i = 0; i < names.Length; i++)
        {
            tabs.SelectedIndex = i;
            Dispatcher.UIThread.RunJobs();
            SaveScreenshot(statistics.CaptureRenderedFrame(), $"plugin-statistics-{names[i]}-{theme}");
        }

        statistics.Close();

        ImpactViewModel impact = CreateImpact();
        ImpactWindow impactWindow = Show(new ImpactWindow { DataContext = impact });
        impact.SelectedAuthor = "Alice";
        Dispatcher.UIThread.RunJobs();
        SaveScreenshot(impactWindow.CaptureRenderedFrame(), $"plugin-impact-{theme}");
        impactWindow.Close();

        TeamCityBuildChooserViewModel chooser = new(CreateProjects(), LoadBuilds, "Web", "Web_Deploy");
        TeamCityBuildChooserWindow chooserWindow = Show(new TeamCityBuildChooserWindow { DataContext = chooser });
        SaveScreenshot(chooserWindow.CaptureRenderedFrame(), $"plugin-teamcity-build-chooser-{theme}");
        chooserWindow.Close();
    });

    [Test]
    public Task PieChart_shows_the_tooltip_of_the_slice_under_the_mouse() => OnUiThreadAsync(() =>
    {
        PieChartView pie = new() { Slices = [new PieSlice(3, "Three"), new PieSlice(1, "One")] };
        Window window = Show(new Window { Width = 300, Height = 300, Content = pie });

        // The first slice starts at 3 o'clock and goes clockwise to 9 o'clock (3/4 of the pie... here 270 degrees).
        Point center = new(pie.Bounds.Width / 2, pie.Bounds.Height / 2);
        pie.HitTestSlice(center + new Point(0, 20)).Should().Be(0, "below the center is in the first slice");
        pie.HitTestSlice(center + new Point(20, -20)).Should().Be(1, "above right of the center is the last quarter");
        pie.HitTestSlice(new Point(2, 2)).Should().Be(-1, "the corner is outside of the pie");
        SaveScreenshot(window.CaptureRenderedFrame(), "plugin-statistics-pie");
        window.Close();
    });

    [Test]
    public Task ImpactGraph_selects_the_author_under_the_mouse() => OnUiThreadAsync(() =>
    {
        ImpactViewModel viewModel = CreateImpact();
        ImpactWindow window = Show(new ImpactWindow { DataContext = viewModel });
        ImpactGraphView graph = window.GetVisualDescendants().OfType<ImpactGraphView>().Single();

        graph.Bounds.Width.Should().Be((3 * 110) - 50, "three weeks of blocks of 60 and transitions of 50");
        graph.HitTestAuthor(new Point(30, 5)).Should().Be("Bob", "Bob changed most lines in the first week, so his block is on top");
        graph.HitTestAuthor(new Point(30, graph.Bounds.Height - 2)).Should().BeNull("under the blocks");

        window.MouseMove(graph.TranslatePoint(new Point(30, 5), window)!.Value);
        Dispatcher.UIThread.RunJobs();

        viewModel.SelectedAuthor.Should().Be("Bob");
        window.FindControl<TextBlock>("authorText")!.Text.Should().Be("Bob (4 Commits, 1100 Changed Lines)", "the commits of all the weeks");
        window.Close();
    });

    [Test]
    public Task TeamCityBuildChooser_expands_the_project_of_the_selected_build() => OnUiThreadAsync(() =>
    {
        TeamCityBuildChooserViewModel viewModel = new(CreateProjects(), LoadBuilds, "Web", "Web_Deploy");
        TeamCityBuildChooserWindow window = Show(new TeamCityBuildChooserWindow { DataContext = viewModel });

        TreeViewItem[] items = [.. window.GetVisualDescendants().OfType<TreeViewItem>()];
        items.Select(i => ((TeamCityBuildNode)i.DataContext!).Text).Should().Contain(["Root", "Desktop", "Web", "Deploy (Web_Deploy)"]);
        window.FindControl<Button>("okButton")!.IsEffectivelyEnabled.Should().BeTrue();
        window.Close();
    });

    private static T Show<T>(T window)
        where T : Window
    {
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static GitStatisticsViewModel CreateStatistics()
    {
        IGitModule module = Substitute.For<IGitModule>();
        module.GetCommitsByContributor(Arg.Any<DateTime?>(), Arg.Any<DateTime?>()).Returns((10, new Dictionary<string, int> { ["Alice"] = 6, ["Bob"] = 3, ["Carol"] = 1 }));
        module.GetTree(Arg.Any<ObjectId>(), Arg.Any<bool>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([]);
        module.GetSubmodulesInfo().Returns([]);
        module.WorkingDir.Returns(Path.GetTempPath());
        return new GitStatisticsViewModel(new GitStatisticsStrings(), module, _ => module, "*.cs", false, "", new SynchronousBackgroundRunner());
    }

    private static ImpactViewModel CreateImpact()
    {
        ImpactViewModel viewModel = new(new ImpactStrings(), loader: null, new SynchronousBackgroundRunner());
        DateOnly week = new(2024, 1, 7);
        viewModel.AddCommits(
        [
            new ImpactLoader.Commit(week, "Alice", new ImpactLoader.DataPoint(1, 20, 2)),
            new ImpactLoader.Commit(week, "Bob", new ImpactLoader.DataPoint(1, 90, 10)),
            new ImpactLoader.Commit(week.AddDays(7), "Alice", new ImpactLoader.DataPoint(2, 300, 40)),
            new ImpactLoader.Commit(week.AddDays(7), "Carol", new ImpactLoader.DataPoint(1, 5, 0)),
            new ImpactLoader.Commit(week.AddDays(14), "Bob", new ImpactLoader.DataPoint(3, 800, 200)),
            new ImpactLoader.Commit(week.AddDays(14), "Alice", new ImpactLoader.DataPoint(1, 12, 0)),
        ]);
        return viewModel;
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

    private static IList<Build> LoadBuilds(string projectId)
        => projectId == "Web"
            ? [new Build { Id = "Web_Deploy", Name = "Deploy", ParentProject = "Web" }, new Build { Id = "Web_Build", Name = "Build", ParentProject = "Web" }]
            : [];
}
