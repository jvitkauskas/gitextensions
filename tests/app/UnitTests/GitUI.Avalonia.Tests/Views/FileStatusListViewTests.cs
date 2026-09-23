using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GitUI.Avalonia.Controls.FileStatusList;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.UserControls.FileStatusList;
using static GitUI.AvaloniaTests.ViewModels.FileStatusListViewModelTests;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the file status list.</summary>
[TestFixture]
public sealed class FileStatusListViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        (Window window, _, FileStatusListViewModel viewModel) = Show();
        SaveScreenshot(window.CaptureRenderedFrame(), $"file-status-list-{theme}");

        viewModel.SetGroups(
        [
            new FileStatusGroup(First, Second, "First parent", CreateStatuses()),
            new FileStatusGroup(null, Second, "Second parent", []),
        ]);
        viewModel.Filter = @"\.cs$";
        Dispatcher.UIThread.RunJobs();
        SaveScreenshot(window.CaptureRenderedFrame(), $"file-status-list-groups-{theme}");
        window.Close();
    });

    [Test]
    public Task Tree_selection_follows_the_view_model_both_ways() => OnUiThreadAsync(() =>
    {
        (Window window, FileStatusListView view, FileStatusListViewModel viewModel) = Show();

        view.Tree.SelectedItems.Cast<FileStatusNode>().Select(n => n.Entry!.Item.Name).Should().Equal("docs/readme.md");
        view.Tree.GetVisualDescendants().OfType<TreeViewItem>().Should().Contain(item => item.IsExpanded, "the folders are expanded");

        FileStatusNode program = viewModel.Nodes[1].Children[1];
        view.Tree.SelectedItems.Clear();
        view.Tree.SelectedItems.Add(program);
        Dispatcher.UIThread.RunJobs();
        viewModel.SelectedEntry!.Item.Name.Should().Be("src/Program.cs");

        viewModel.SelectNextItem(backwards: false);
        Dispatcher.UIThread.RunJobs();
        view.Tree.SelectedItem.Should().BeSameAs(viewModel.Nodes[2]);
        viewModel.SelectedEntry.Should().BeSameAs(viewModel.Nodes[2].Entry, "the tree keeps the selection of the view model");
        viewModel.Select(entry => entry.Item.Name == "src/Program.cs");
        Dispatcher.UIThread.RunJobs();
        viewModel.SelectedEntry!.Item.Name.Should().Be("src/Program.cs");
        view.Tree.SelectedItems.Cast<FileStatusNode>().Select(n => n.Entry!.Item.Name).Should().Equal("src/Program.cs");
        window.Close();
    });

    [Test]
    public Task Context_menu_shows_the_items_of_the_state_and_runs_their_commands() => OnUiThreadAsync(() =>
    {
        (Window window, FileStatusListView view, FileStatusListViewModel viewModel) = Show();
        FileStatusListMenuTests.FakeMenuHost host = new()
        {
            State = new FileStatusMenuState { ShowSaveAs = true, CanCopyPaths = true, ResetToParentText = "First: A a1a1a1a1" },
        };
        viewModel.MenuHost = host;

        // As a right click or the menu key: the menu opens with the items for the selection.
        view.Tree.RaiseEvent(new ContextRequestedEventArgs { RoutedEvent = Control.ContextRequestedEvent });
        Dispatcher.UIThread.RunJobs();

        MenuItem Item(string name) => view.Menu.Items.OfType<MenuItem>().SelectMany(i => i.Items.OfType<MenuItem>().Prepend(i)).Single(i => i.Name == name);
        Item("saveAsMenuItem").IsVisible.Should().BeTrue();
        Item("openWorkingDirectoryFileMenuItem").IsVisible.Should().BeFalse();
        Item("resetFileToMenuItem").IsEnabled.Should().BeTrue();
        Item("resetFileToParentMenuItem").Header.Should().Be("First: A a1a1a1a1");
        Item("resetFileToSelectedMenuItem").IsVisible.Should().BeFalse();

        MenuItem copy = Item("copyFullPathsNativeMenuItem");
        copy.Command!.Execute(copy.CommandParameter);
        MenuItem blame = Item("blameMenuItem");
        blame.Command!.Execute(blame.CommandParameter);
        host.Log.Should().Contain(["copy FullNative: docs/readme.md folder: ", "history blame: docs/readme.md"]);
        view.Menu.Close();
        window.Close();
    });

    private static (Window Window, FileStatusListView View, FileStatusListViewModel ViewModel) Show()
    {
        FileStatusListViewModel viewModel = Create();
        viewModel.SetDiff(First, Second, CreateStatuses());
        FileStatusListView view = new() { DataContext = viewModel };
        Window window = new() { Content = view, Width = 320, Height = 260 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, view, viewModel);
    }
}
