using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommonTestUtils;
using GitUI.Avalonia.CommandsDialogs.BrowseDialog;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs.BrowseDialog;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUI.Presentation.UserControls.RevisionGrid;
using static GitUI.AvaloniaTests.ViewModels.DashboardViewModelTests;
using static GitUI.AvaloniaTests.Views.BrowseViewTests;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless tests of the dashboard of the main window (the port of <c>Dashboard</c> and <c>UserRepositoriesList</c>).</summary>
[TestFixture]
public sealed class DashboardViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        (BrowseWindow window, BrowseViewModel viewModel, _, _) = Show(new FakeDashboardHost { GitHosters = ["GitHub"] });

        // As ShowDashboard: the dashboard replaces the toolbar, the grid and the tabs, and the Dashboard menu the repository ones.
        viewModel.IsDashboard.Should().BeTrue();
        window.Dashboard.IsVisible.Should().BeTrue();
        window.Tabs.IsVisible.Should().BeFalse();
        window.RevisionGrid.IsEffectivelyVisible.Should().BeFalse();
        window.MainMenu.Items.Cast<MenuItem>().Select(m => m.Header).Should().Equal("_Start", "_Dashboard", "_Tools", "_Help");
        window.Dashboard.GetTileLists().Select(l => l.ItemCount).Should().Equal(3, 1, 2);
        viewModel.Dashboard!.Repositories.First().BranchName.Should().Be("master");
        window.Dashboard.TileWidth.Should().BeInRange(120, 250, "the tiles fit the longest caption, the icon and 50 more");

        SaveScreenshot(window.CaptureRenderedFrame(), $"dashboard-{theme}");
        window.Close();
    });

    [Test]
    public Task The_menus_of_a_tile_and_of_a_group_are_filled_when_they_open() => OnUiThreadAsync(() =>
    {
        FakeDashboardHost dashboardHost = new();
        (BrowseWindow window, BrowseViewModel viewModel, _, _) = Show(dashboardHost);
        DashboardView dashboard = window.Dashboard;
        ListBox recent = dashboard.GetTileLists().First();
        Control tile = recent.ContainerFromIndex(0)!;

        dashboard.ShowRepositoryMenu(viewModel.Dashboard!.Groups[0].Items[0], tile);
        Dispatcher.UIThread.RunJobs();
        List<MenuItem> items = [.. dashboard.RepositoryMenu.Items.OfType<MenuItem>()];
        items.Select(i => i.Header).Should().Equal("Show in folder", "Categories", "Remove project from the list", "Remove missing projects from the list");
        items[1].Items.OfType<MenuItem>().Select(i => (i.Header, i.IsEnabled)).Should().Equal(("(none)", false), ("Personal", true), ("Work", true), ("Add new...", true));
        items[0].RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        dashboardHost.ShownInFolder.Should().Equal(TestPaths.Native(@"C:\src\gitextensions"));
        dashboard.RepositoryMenu.Close();

        Button actions = dashboard.GetVisualDescendants().OfType<Button>().First(b => b.Classes.Contains("groupActions"));
        actions.Content.Should().Be("Actions");
        actions.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        dashboard.GroupMenu.Items.OfType<MenuItem>().Select(i => i.Header).Should().Equal("Clear all recent repositories");
        dashboard.GroupMenu.Close();
        window.Close();
    });

    [Test]
    public Task The_search_filters_the_tiles_and_Enter_opens_the_first_one() => OnUiThreadAsync(() =>
    {
        FakeDashboardHost dashboardHost = new();
        (BrowseWindow window, _, _, _) = Show(dashboardHost);
        DashboardView dashboard = window.Dashboard;

        dashboard.SearchBox.Focus();
        dashboard.SearchBox.Text = "work";
        Dispatcher.UIThread.RunJobs();
        dashboard.GetTileLists().Select(l => l.ItemCount).Should().Equal(2);

        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        dashboardHost.Opened.Should().Equal(TestPaths.Native(@"C:\work\api"));
        window.Close();
    });

    [Test]
    public Task The_arrow_keys_move_between_the_groups() => OnUiThreadAsync(() =>
    {
        (BrowseWindow window, _, _, _) = Show(new FakeDashboardHost());
        ListBox[] lists = [.. window.Dashboard.GetTileLists()];
        lists.Select(l => l.ItemCount).Should().Equal(3, 1, 2);
        lists[0].SelectedIndex = 2;
        lists[0].ContainerFromIndex(2)!.Focus();
        Dispatcher.UIThread.RunJobs();

        // Right on the last tile of a group: the first of the next group; Down on its last row: the next group.
        window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        lists[1].SelectedIndex.Should().Be(0);
        lists[0].SelectedIndex.Should().Be(-1, "one tile is selected in all the groups");
        window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        lists[2].SelectedIndex.Should().Be(0);

        // Up on the first row: the previous group; Left on its first tile: the last tile of the previous group.
        window.KeyPressQwerty(PhysicalKey.ArrowUp, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        lists[1].SelectedIndex.Should().Be(0);
        window.KeyPressQwerty(PhysicalKey.ArrowLeft, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        lists[0].SelectedIndex.Should().Be(2);
        window.Close();
    });

    [Test]
    public Task A_click_on_a_tile_opens_its_repository() => OnUiThreadAsync(() =>
    {
        FakeDashboardHost dashboardHost = new();
        (BrowseWindow window, _, _, _) = Show(dashboardHost);
        Control tile = window.Dashboard.GetTileLists().Last().ContainerFromIndex(1)!;

        Point point = tile.TranslatePoint(new Point(tile.Bounds.Width / 2, tile.Bounds.Height / 2), window)!.Value;
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        dashboardHost.Opened.Should().Equal(TestPaths.Native(@"C:\work\web"));
        window.Close();
    });

    [Test]
    public Task The_Start_menu_reads_the_recent_and_favourite_repositories_when_it_opens() => OnUiThreadAsync(() =>
    {
        string? opened = null;
        (BrowseWindow window, BrowseViewModel viewModel, _, FakeBrowseHost host) = Show(new FakeDashboardHost());
        host.RecentRepositoriesMenu = [new BrowseMenuItem("_1: gitextensions", null, "Pin") { Invoke = () => opened = "gitextensions", Shortcut = "master", ToolTip = @"C:\src\gitextensions" }];
        host.FavouriteRepositoriesMenu = [new BrowseMenuItem("Work", null, Children: [new BrowseMenuItem("_1: api", null) { Invoke = () => opened = "api" }])];

        MenuItem start = window.MainMenu.Items.Cast<MenuItem>().First();
        start.Items.OfType<MenuItem>().Select(m => m.Header).Should().StartWith(["_Create new repository...", "_Open...", "_Favorite repositories", "_Recent repositories"]);
        start.IsSubMenuOpen = true;
        Dispatcher.UIThread.RunJobs();

        MenuItem recent = start.Items.OfType<MenuItem>().Single(m => (string?)m.Header == "_Recent repositories");
        List<Control> recentItems = [.. recent.Items.OfType<Control>()];
        recentItems.Should().HaveCount(3);
        recentItems[1].Should().BeOfType<Separator>();
        ((MenuItem)recentItems[2]).Header.Should().Be("Clear list");
        ToolTip.GetTip(recentItems[0]).Should().Be(@"C:\src\gitextensions");
        recentItems[0].RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        opened.Should().Be("gitextensions");

        ((MenuItem)recentItems[2]).Command!.Execute(((MenuItem)recentItems[2]).CommandParameter);
        host.Runs[^1].Command.Should().Be(BrowseCommand.ClearRecentRepositories);

        MenuItem favourites = start.Items.OfType<MenuItem>().Single(m => (string?)m.Header == "_Favorite repositories");
        MenuItem work = favourites.Items.OfType<MenuItem>().Single();
        work.Header.Should().Be("Work");
        work.Items.OfType<MenuItem>().Single().RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        opened.Should().Be("api");

        start.IsSubMenuOpen = false;
        viewModel.RunCommand.Execute(BrowseCommand.RefreshDashboard);
        window.Close();
    });

    [Test]
    public Task The_links_of_the_git_hosters_are_added_once_the_plugins_are_loaded() => OnUiThreadAsync(() =>
    {
        (BrowseWindow window, BrowseViewModel viewModel, FakeDashboardHost dashboardHost, FakeBrowseHost host) = Show(new FakeDashboardHost());
        int links = viewModel.Dashboard!.StartLinks.Count;

        // As FormBrowse after InitializeGitHostersOnly: the dashboard shows the "Clone fork" link of the hoster.
        dashboardHost.GitHosters = ["GitHub"];
        host.LoadPlugins([], "GitHub");
        Dispatcher.UIThread.RunJobs();
        viewModel.Dashboard.StartLinks.Should().HaveCount(links + 1);
        viewModel.Dashboard.StartLinks[^1].Text.Should().Contain("GitHub");
        window.Close();
    });

    private static (BrowseWindow Window, BrowseViewModel ViewModel, FakeDashboardHost DashboardHost, FakeBrowseHost Host) Show(FakeDashboardHost dashboardHost)
    {
        FakeBrowseHost host = new();
        RevisionGridViewModel grid = new(new RevisionGridViewTests.FakeRevisionGridHost([]), new RevisionGridDisplayOptions(RelativeDate: true, ShowAuthorDate: false));
        DashboardViewModel dashboard = new(new DashboardStrings(), new UserRepositoriesListStrings(), dashboardHost);
        BrowseViewModel viewModel = new(
            new BrowseStrings(),
            host,
            grid,
            new CommitInfoViewTests.FakeHost(),
            new DiffViewModelTests.FakeViewerHost(),
            new FileStatusListStrings(),
            new FileStatusTreeOptions(),
            dashboard);
        BrowseWindow window = new() { Width = 1100, Height = 700, DataContext = viewModel };
        window.Show();
        window.Activate();
        Dispatcher.UIThread.RunJobs();
        dashboard.LoadingStatuses.IsCompleted.Should().BeTrue("the fake host answers at once");
        Dispatcher.UIThread.RunJobs();
        return (window, viewModel, dashboardHost, host);
    }
}
