using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Services;
using static GitUI.AvaloniaTests.ViewModels.ResolveConflictsViewModelTests;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the merge conflicts dialog.</summary>
[TestFixture]
public sealed class ResolveConflictsViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        ResolveConflictsWindow window = Show(CreateHost());
        SaveScreenshot(window.CaptureRenderedFrame(), $"resolve-conflicts-{theme}");
        window.Close();

        FakeResolveConflictsHost rebase = CreateHost();
        rebase.IsRebasing = true;
        window = Show(rebase);
        window.FindControl<DataGrid>("conflictsGrid")!.SelectAll();
        Dispatcher.UIThread.RunJobs();
        SaveScreenshot(window.CaptureRenderedFrame(), $"resolve-conflicts-rebase-{theme}");
        window.Close();
    });

    [Test]
    public Task Shows_the_conflicts_and_the_sides_of_the_selected_file() => OnUiThreadAsync(() =>
    {
        ResolveConflictsWindow window = Show(CreateHost());
        ResolveConflictsViewModel viewModel = (ResolveConflictsViewModel)window.DataContext!;
        DataGrid grid = window.FindControl<DataGrid>("conflictsGrid")!;

        grid.Columns[0].Header.Should().Be("Filename");
        grid.SelectedItem.Should().BeSameAs(viewModel.Conflicts[0]);
        window.FindControl<TextBlock>("localLabel")!.Text.Should().Be("Local/current (ours)");
        window.FindControl<SelectableTextBlock>("localFileName")!.Text.Should().Be("README.md");
        window.FindControl<SelectableTextBlock>("baseFileName")!.Text.Should().Be("README.md");
        window.FindControl<Button>("openMergeToolButton")!.IsEnabled.Should().BeTrue();
        window.FindControl<Button>("mergeButton")!.IsDefault.Should().BeTrue();
        window.FindControl<ProgressBar>("progressBar")!.IsVisible.Should().BeFalse();

        ContextMenu menu = window.FindControl<ContextMenu>("conflictsContextMenu")!;
        menu.Open(grid);
        Dispatcher.UIThread.RunJobs();
        grid.SelectedItem = viewModel.Conflicts[2];
        Dispatcher.UIThread.RunJobs();
        viewModel.SelectedConflicts.Should().Equal(viewModel.Conflicts[2]);
        window.FindControl<SelectableTextBlock>("localFileName")!.Text.Should().Be("deleted");
        window.FindControl<MenuItem>("openLocalWithMenuItem")!.IsEnabled.Should().BeFalse("there is no local side");
        window.FindControl<MenuItem>("saveLocalAsMenuItem")!.IsEnabled.Should().BeFalse();
        window.FindControl<MenuItem>("openRemoteWithMenuItem")!.IsEnabled.Should().BeTrue();

        grid.SelectAll();
        Dispatcher.UIThread.RunJobs();
        viewModel.SelectedConflicts.Should().HaveCount(3);
        window.FindControl<Button>("openMergeToolButton")!.IsEnabled.Should().BeFalse("the merge tool opens one file");
        window.FindControl<MenuItem>("openMergeToolMenuItem")!.IsEnabled.Should().BeFalse();
        window.FindControl<MenuItem>("customMergeToolMenuItem")!.IsEnabled.Should().BeFalse();
        window.FindControl<MenuItem>("fileHistoryMenuItem")!.IsEnabled.Should().BeFalse();
        window.FindControl<MenuItem>("chooseLocalMenuItem")!.IsEnabled.Should().BeTrue("the sides can be chosen for several files");
        menu.Close();
        window.Close();
    });

    [Test]
    public Task The_custom_merge_tools_are_listed_under_the_menu_item() => OnUiThreadAsync(() =>
    {
        FakeResolveConflictsHost host = CreateHost();
        host.CustomMergeTools = ["kdiff3", "meld", "vscode"];
        ResolveConflictsWindow window = Show(host);

        ContextMenu menu = window.FindControl<ContextMenu>("conflictsContextMenu")!;
        menu.Open(window.FindControl<DataGrid>("conflictsGrid"));
        Dispatcher.UIThread.RunJobs();
        MenuItem customMergeTool = window.FindControl<MenuItem>("customMergeToolMenuItem")!;
        customMergeTool.Header.Should().Be("Open in _mergetool");
        customMergeTool.Items.Cast<MenuItem>().Select(item => item.Header).Should().Equal("kdiff3", "meld", "vscode");
        customMergeTool.Items.Cast<MenuItem>().Select(item => item.FontWeight).Should().Equal(FontWeight.Bold, FontWeight.Normal, FontWeight.Normal);

        ((MenuItem)customMergeTool.Items[1]!).Command!.Execute("meld");
        Dispatcher.UIThread.RunJobs();
        host.Calls.Should().Contain("mergetool README.md meld");
        menu.Close();
        window.Close();
    });

    [Test]
    public Task Keys_run_the_merge_and_choose_commands() => OnUiThreadAsync(() =>
    {
        FakeResolveConflictsHost host = CreateHost();
        host.ExitCode = 1;
        ResolveConflictsWindow window = Show(host, window => window.Hotkeys =
        [
            new HotkeyBinding((int)ResolveConflictsHotkeyCommand.ChooseRemote, 0x52 /* R */),
            new HotkeyBinding((int)ResolveConflictsHotkeyCommand.Rescan, 0x74 /* F5 */),
        ]);
        DataGrid grid = window.FindControl<DataGrid>("conflictsGrid")!;
        window.Activate();
        grid.Focus();

        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        host.Calls.Should().ContainInOrder("checkout README.md", "run kdiff3-full:\"base/README.md\" \"local/README.md\" \"remote/README.md\" -o \"README.md\"");

        host.Calls.Clear();
        window.KeyPressQwerty(PhysicalKey.Digit1, TestKeys.Command);
        Dispatcher.UIThread.RunJobs();
        host.Calls.Should().StartWith("choose README.md Local");

        host.Calls.Clear();
        window.KeyPressQwerty(PhysicalKey.R, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        host.Calls.Should().StartWith("choose docs/new-page.md Remote", "the configured hotkey chooses the remote side of the selected file");
        window.Close();
    });

    private static FakeResolveConflictsHost CreateHost() => new()
    {
        Conflicts =
        [
            Conflict("README.md"),
            Conflict("src/app/removed.txt", hasLocal: false),
            Conflict("docs/new-page.md", hasBase: false),
        ],
        ExitCode = 0,
    };

    private static ResolveConflictsWindow Show(FakeResolveConflictsHost host, Action<ResolveConflictsWindow>? configure = null)
    {
        ResolveConflictsWindow window = new() { DataContext = Create(host) };
        configure?.Invoke(window);
        window.Show();

        // Runs the InitializeView posted by the Opened handler, then the bindings.
        Dispatcher.UIThread.RunJobs();
        Dispatcher.UIThread.RunJobs();
        return window;
    }
}
