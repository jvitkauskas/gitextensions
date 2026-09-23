using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Threading;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the file history.</summary>
[TestFixture]
public sealed class FileHistoryViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        (FileHistoryViewModel viewModel, _, _) = FileHistoryViewModelTests.Create();
        FileHistoryWindow window = new() { DataContext = viewModel };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        window.Title.Should().Be("File History - src/file.cs - C:\\repo");
        window.Tabs.SelectedItem.Should().BeOfType<TabItem>().Which.Name.Should().Be("diffTab");
        viewModel.Grid.SelectedRow!.Revision.Should().BeSameAs(FileHistoryViewModelTests.Changed);
        SaveScreenshot(window.CaptureRenderedFrame(), $"file-history-{theme}");
        window.Close();
    });

    [Test]
    public Task The_tabs_select_what_is_shown() => OnUiThreadAsync(() =>
    {
        (FileHistoryViewModel viewModel, _, DiffViewModelTests.FakeViewerHost viewer) = FileHistoryViewModelTests.Create();
        FileHistoryWindow window = new() { DataContext = viewModel };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        window.Tabs.SelectedIndex = 2;
        viewModel.SelectedTab.Should().Be(FileHistoryTab.View);
        viewer.Requested[^1].Should().Be("src/file.cs@c3c3c3c3");

        viewModel.SelectedTab = FileHistoryTab.Blame;
        window.Tabs.SelectedIndex.Should().Be(3);
        Dispatcher.UIThread.RunJobs();
        window.Blame.Editor.Text.Should().StartWith("namespace Sample;");
        window.Close();
    });

    [Test]
    public Task The_grid_menu_follows_the_selection() => OnUiThreadAsync(() =>
    {
        (FileHistoryViewModel viewModel, FileHistoryViewModelTests.FakeHost host, _) = FileHistoryViewModelTests.Create();
        FileHistoryWindow window = new() { DataContext = viewModel };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        window.RevisionGrid.RaiseEvent(new ContextRequestedEventArgs { RoutedEvent = Control.ContextRequestedEvent });
        Dispatcher.UIThread.RunJobs();

        MenuItem copy = window.GridMenu.Items.OfType<MenuItem>().First();
        copy.Header.Should().Be("Copy to clipboard");
        copy.IsEnabled.Should().BeTrue();
        MenuItem copyHash = copy.Items.OfType<MenuItem>().Single();
        copyHash.Header.Should().Be("Commit _hash");
        copyHash.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
        host.Actions.Should().Equal($"copy {FileHistoryViewModelTests.Changed.Guid}");
        window.GridMenu.Close();
        window.Close();
    });
}
