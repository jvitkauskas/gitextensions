using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.CommandsDialogs.CommitDialog;
using GitUI.Avalonia.Editor;
using GitUI.Avalonia.Hosting;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs.CommitDialog;
using GitUI.Presentation.Editor;
using GitUI.Presentation.Services;
using GitUI.Presentation.UserControls.FileStatusList;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless tests of the context menu and the hotkeys of the file viewer.</summary>
[TestFixture]
public sealed class FileViewerContextMenuViewTests : HeadlessTest
{
    [Test]
    public Task The_menu_has_the_items_of_the_diff() => OnUiThreadAsync(() =>
    {
        DiffViewModelTests.FakeViewerHost host = new() { Diff = FileViewerContextMenuTests.Patch, SupportsLinePatching = true };
        (DialogWindow window, FileViewerView view, FileViewerViewModel viewModel) = Create(host);
        _ = viewModel.ShowChangesAsync(FileViewerContextMenuTests.Entry(StagedStatus.WorkTree));
        Dispatcher.UIThread.RunJobs();

        view.FillContextMenu().Select(i => i is MenuItem item ? item.Header : "-").Should().Equal(
            "Stage selected line(s)", "Reset selected line(s)", "_Copy", "Copy _patch", "Copy _new version", "Copy _old version", "-",
            "_Increase the number of lines of context", "_Decrease the number of lines of context", "Show _entire file", "S_how nonprinting characters",
            "Show synta_x highlighting", "Ignore whitespace changes at end of _line", "Ignore changes in _amount of whitespace", "Ignore all _whitespace changes",
            "Diff appea_rance", "-", "_Treat all files as text", "_Find...", "_Go to line");

        viewModel.Show(new FileViewContent(FileViewKind.Text, "text"));
        view.FillContextMenu().Select(i => i is MenuItem item ? item.Header : "-").Should().Equal(
            "_Copy", "-", "S_how nonprinting characters", "-", "_Find...", "_Go to line");
        window.Close();
    });

    [Test]
    public Task The_hotkeys_of_the_focused_viewer_come_first() => OnUiThreadAsync(() =>
    {
        DiffViewModelTests.FakeViewerHost viewerHost = new()
        {
            Diff = FileViewerContextMenuTests.Patch,
            SupportsLinePatching = true,
            Hotkeys =
            [
                new HotkeyBinding((int)FileViewerHotkeyCommand.StageLines, 0x53 /* S */),
                new HotkeyBinding((int)FileViewerHotkeyCommand.Find, 0x46 /* F */ | HotkeyBinding.Control),
                new HotkeyBinding((int)FileViewerHotkeyCommand.GoToLine, 0x47 /* G */ | HotkeyBinding.Control),
            ],
        };
        CommitViewModel viewModel = new(
            new CommitStrings(), new CommitViewModelTests.FakeHost(), viewerHost, new FileStatusListStrings(), new FileStatusTreeOptions());
        CommitWindow window = new()
        {
            DataContext = viewModel,
            Hotkeys = [new HotkeyBinding((int)CommitHotkeyCommand.ToggleSelectionFilter, 0x46 /* F */ | HotkeyBinding.Control)],
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.Activate();
        _ = viewModel.Diff.ShowChangesAsync(FileViewerContextMenuTests.Entry(StagedStatus.WorkTree));
        Dispatcher.UIThread.RunJobs();
        FileViewerView view = window.DiffViewer;
        view.AskLineNumber = (_, max) => max;
        view.TextView.Editor.TextArea.Focus();
        view.TextView.Editor.Select(FileViewerContextMenuTests.Patch.IndexOf("-b", StringComparison.Ordinal), 2);

        window.KeyPressQwerty(PhysicalKey.S, RawInputModifiers.None);
        viewerHost.Patches.Should().Equal($"Stage WorkTree {FileViewerContextMenuTests.Patch.IndexOf("-b", StringComparison.Ordinal)}+2");

        window.KeyPressQwerty(PhysicalKey.F, RawInputModifiers.Control);
        Dispatcher.UIThread.RunJobs();
        view.TextView.Search.IsOpened.Should().BeTrue("Ctrl+F finds in the focused diff");
        viewModel.IsSelectionFilterVisible.Should().BeFalse("the hotkey of the dialog does not apply");
        view.TextView.Search.Close();

        view.TextView.Editor.TextArea.Focus();
        window.KeyPressQwerty(PhysicalKey.G, RawInputModifiers.Control);
        view.TextView.Editor.TextArea.Caret.Line.Should().Be(view.TextView.Editor.Document.LineCount);

        window.UnstagedFiles.Tree.ContainerFromIndex(0)!.Focus();
        window.DiffViewer.IsKeyboardFocusWithin.Should().BeFalse();
        window.KeyPressQwerty(PhysicalKey.F, RawInputModifiers.Control);
        Dispatcher.UIThread.RunJobs();
        viewModel.IsSelectionFilterVisible.Should().BeTrue("outside the diff, the hotkey of the dialog applies");
        window.Close();
    });

    private static (DialogWindow Window, FileViewerView View, FileViewerViewModel ViewModel) Create(DiffViewModelTests.FakeViewerHost host)
    {
        FileViewerViewModel viewModel = new(host);
        FileViewerView view = new() { DataContext = viewModel };
        DialogWindow window = new() { Content = view, Width = 700, Height = 300 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, view, viewModel);
    }
}
