using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using GitUI.Avalonia.CommandsDialogs.BrowseDialog;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs.BrowseDialog;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the git command log (port of <c>FormGitCommandLog</c>).</summary>
[TestFixture]
public sealed class GitCommandLogViewTests : HeadlessTest
{
    [Test]
    public Task Lists_the_commands_and_shows_the_selected_one() => OnUiThreadAsync(() =>
    {
        (GitCommandLogWindow window, GitCommandLogViewModel viewModel, GitCommandLogViewModelTests.FakeHost host) = Show();
        ListBox logItems = window.FindControl<ListBox>("logItems")!;
        TextBox logOutput = window.FindControl<TextBox>("logOutput")!;

        logItems.ItemCount.Should().Be(3);
        logItems.SelectedIndex.Should().Be(2, "the last command is selected");
        logOutput.Text.Should().Be("details of diff --stat");

        logItems.SelectedIndex = 0;
        Dispatcher.UIThread.RunJobs();
        logOutput.Text.Should().Be("details of status");

        host.Add("fetch");
        Dispatcher.UIThread.RunJobs();
        logItems.ItemCount.Should().Be(4);
        logItems.SelectedIndex.Should().Be(0, "the selection is kept");

        logOutput.TextWrapping.Should().Be(TextWrapping.NoWrap);
        window.FindControl<CheckBox>("wordWrapCheckBox")!.IsChecked = true;
        Dispatcher.UIThread.RunJobs();
        logOutput.TextWrapping.Should().Be(TextWrapping.Wrap);

        window.Topmost.Should().BeFalse();
        window.FindControl<CheckBox>("alwaysOnTopCheckBox")!.IsChecked = true;
        Dispatcher.UIThread.RunJobs();
        window.Topmost.Should().BeTrue();

        window.Activate();
        logItems.ContainerFromIndex(0)!.Focus();
        window.KeyPressQwerty(PhysicalKey.C, RawInputModifiers.Control);
        host.Actions.Should().Equal("copy \"git.exe\" status");

        window.Close();
        host.Subscribers.Should().Be(0, "the log is no longer followed once closed");
    });

    [Test]
    public Task The_context_menus_have_the_commands_of_the_WinForms_menus() => OnUiThreadAsync(() =>
    {
        (GitCommandLogWindow window, _, GitCommandLogViewModelTests.FakeHost host) = Show();
        ListBox logItems = window.FindControl<ListBox>("logItems")!;
        ContextMenu logMenu = logItems.ContextMenu!;

        logMenu.Open(logItems);
        Dispatcher.UIThread.RunJobs();
        logMenu.Items.OfType<MenuItem>().Select(i => i.Header).Should().Equal("_Save to file", "_Copy full command line", "C_lear");
        logMenu.Items.OfType<MenuItem>().Last().Command!.Execute(null);
        logMenu.Close();
        host.Actions.Should().Equal("clear log");

        ListBox cacheItems = window.FindControl<ListBox>("cacheItems")!;
        cacheItems.ContextMenu!.Open(cacheItems);
        Dispatcher.UIThread.RunJobs();
        cacheItems.ContextMenu.Items.OfType<MenuItem>().Select(i => i.Header).Should().Equal("C_lear");
        cacheItems.ContextMenu.Close();
        window.Close();
    });

    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        (GitCommandLogWindow window, GitCommandLogViewModel viewModel, _) = Show();

        SaveScreenshot(window.CaptureRenderedFrame(), $"git-command-log-{theme}");

        viewModel.SelectedTabIndex = GitCommandLogViewModel.CommandCacheTabIndex;
        Dispatcher.UIThread.RunJobs();
        window.FindControl<ListBox>("cacheItems")!.ItemCount.Should().Be(1);
        window.FindControl<TextBox>("cacheOutput")!.Text.Should().StartWith("-c core.quotepath=false ls-files\n");
        SaveScreenshot(window.CaptureRenderedFrame(), $"git-command-log-cache-{theme}");
        window.Close();
    });

    private static (GitCommandLogWindow Window, GitCommandLogViewModel ViewModel, GitCommandLogViewModelTests.FakeHost Host) Show()
    {
        GitCommandLogViewModelTests.FakeHost host = new();
        host.Add("status");
        host.Add("log -1");
        host.Add("diff --stat");
        host.Cache["-c core.quotepath=false ls-files"] = ("a.txt\nb c.txt\n", "");
        GitCommandLogViewModel viewModel = new(new GitCommandLogStrings(), host);
        GitCommandLogWindow window = new() { DataContext = viewModel };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, viewModel, host);
    }
}
