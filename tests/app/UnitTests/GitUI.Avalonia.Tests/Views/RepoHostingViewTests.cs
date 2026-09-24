using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GitExtensions.Extensibility.Plugins;
using GitUI.Avalonia.CommandsDialogs.RepoHosting;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs.RepoHosting;
using NSubstitute;
using static GitUI.AvaloniaTests.ViewModels.DiffViewModelTests;
using static GitUI.AvaloniaTests.ViewModels.ProcessViewModelTests;
using static GitUI.AvaloniaTests.ViewModels.RepoHostingViewModelTests;
using static GitUI.AvaloniaTests.ViewModels.SmallDialogViewModelTests;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the repository hosting dialogs (docs/avalonia-port/PLAN.md, phase 7).</summary>
[TestFixture]
public sealed class RepoHostingViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        ForkAndCloneWindow forkWindow = Show(new ForkAndCloneWindow { DataContext = CreateForkAndClone() });
        forkWindow.FindControl<DataGrid>("myRepositories")!.SelectedIndex = 1;
        Dispatcher.UIThread.RunJobs();
        SaveScreenshot(forkWindow.CaptureRenderedFrame(), $"repohosting-fork-and-clone-{theme}");
        forkWindow.FindControl<TabControl>("tabs")!.SelectedIndex = 1;
        Dispatcher.UIThread.RunJobs();
        SaveScreenshot(forkWindow.CaptureRenderedFrame(), $"repohosting-fork-and-clone-search-{theme}");
        forkWindow.Close();

        CreatePullRequestWindow createWindow = Show(new CreatePullRequestWindow { DataContext = CreateCreatePullRequest() });
        SaveScreenshot(createWindow.CaptureRenderedFrame(), $"repohosting-create-pull-request-{theme}");
        createWindow.Close();

        ViewPullRequestsWindow viewWindow = Show(new ViewPullRequestsWindow { DataContext = CreateViewPullRequests() });
        SaveScreenshot(viewWindow.CaptureRenderedFrame(), $"repohosting-view-pull-requests-{theme}");
        viewWindow.FindControl<TabControl>("tabs")!.SelectedIndex = 1;
        Dispatcher.UIThread.RunJobs();
        SaveScreenshot(viewWindow.CaptureRenderedFrame(), $"repohosting-view-pull-requests-comments-{theme}");
        viewWindow.Close();
    });

    [Test]
    public Task ForkAndClone_lists_the_repositories_with_their_headers() => OnUiThreadAsync(() =>
    {
        ForkAndCloneViewModel viewModel = CreateForkAndClone();
        ForkAndCloneWindow window = Show(new ForkAndCloneWindow { DataContext = viewModel });

        DataGrid grid = window.FindControl<DataGrid>("myRepositories")!;
        grid.Columns.Select(c => c.Header).Should().Equal("Name", "Is fork", "# Forks", "Private");
        grid.GetVisualDescendants().OfType<DataGridRow>().Should().HaveCount(2);
        window.FindControl<Button>("cloneButton")!.IsEffectivelyEnabled.Should().BeFalse("no repository is selected");

        grid.SelectedIndex = 0;
        Dispatcher.UIThread.RunJobs();

        window.FindControl<TextBox>("createDirectory")!.Text.Should().Be("a-tool");
        window.FindControl<Button>("cloneButton")!.IsEffectivelyEnabled.Should().BeTrue();
        window.Close();
    });

    [Test]
    public Task ViewPullRequests_shows_the_discussion() => OnUiThreadAsync(() =>
    {
        ViewPullRequestsWindow window = Show(new ViewPullRequestsWindow { DataContext = CreateViewPullRequests() });
        window.FindControl<TabControl>("tabs")!.SelectedIndex = 1;
        Dispatcher.UIThread.RunJobs();

        DataGrid grid = window.FindControl<DataGrid>("pullRequests")!;
        grid.Columns.Select(c => c.Header).Should().Equal("#", "Heading", "By", "Created", "Will be fetched to branch");
        window.FindControl<ItemsControl>("discussion")!.GetVisualDescendants().OfType<SelectableTextBlock>().Select(t => t.Text)
            .Should().Equal("Please review", "Fix typo");
        window.Close();
    });

    private static T Show<T>(T window)
        where T : Window
    {
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static ForkAndCloneViewModel CreateForkAndClone()
    {
        IHostedRepository[] repositories = [Repository("gitextensions", isFork: true, parentOwner: "gitextensions-org"), Repository("a-tool")];
        IRepositoryHostPlugin hoster = Substitute.For<IRepositoryHostPlugin>();
        hoster.Name.Returns("GitHub");
        hoster.GetMyRepos().Returns(repositories);
        hoster.SearchForRepository(Arg.Any<string>()).Returns([]);
        return new ForkAndCloneViewModel(
            new ForkAndCloneStrings(), hoster, new ForkAndCloneHost { DefaultDestination = @"C:\repos" }, new SynchronousBackgroundRunner(), new FakeMessageBoxes(), new FakeFileDialogs());
    }

    private static CreatePullRequestViewModel CreateCreatePullRequest()
    {
        IHostedRepository mine = Repository("repo");
        mine.GetBranches().Returns([]);
        IHostedRepository upstream = Repository("repo", owner: "upstream");
        upstream.GetBranches().Returns([]);
        IHostedRemote[] remotes = [Remote("origin", isOwnedByMe: true, mine), Remote("upstream", isOwnedByMe: false, upstream)];
        IRepositoryHostPlugin hoster = Substitute.For<IRepositoryHostPlugin>();
        hoster.GetHostedRemotesForModule().Returns(remotes);
        CreatePullRequestViewModel viewModel = new(
            new CreatePullRequestStrings(), hoster, null, new CreatePullRequestHost { Template = "## Description\n\n## Tests\n" }, new SynchronousBackgroundRunner(), new FakeMessageBoxes());
        viewModel.PullRequestTitle = "Add the Avalonia port of the pull request dialogs";
        return viewModel;
    }

    private static ViewPullRequestsViewModel CreateViewPullRequests()
    {
        (IRepositoryHostPlugin hoster, _, _) = ViewPullRequestsHoster();
        return RepoHostingViewModelTests.CreateViewPullRequests(hoster, new ViewPullRequestsHost { CurrentRemote = "upstream" }, new FakeViewerHost(), new FakeMessageBoxes());
    }
}
