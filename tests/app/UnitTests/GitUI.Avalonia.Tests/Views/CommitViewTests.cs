using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Threading;
using GitUI.Avalonia.CommandsDialogs.CommitDialog;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs.CommitDialog;
using GitUI.Presentation.Editor;
using GitUI.Presentation.Services;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the commit dialog.</summary>
[TestFixture]
public sealed class CommitViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        (CommitViewModel viewModel, _) = CommitViewModelTests.Create(new CommitViewModelTests.FakeHost { Options = new CommitDialogOptions { MaxFirstLineLength = 20 } });
        CommitWindow window = new() { DataContext = viewModel };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        viewModel.Message.Text = "A first line that is too long\n\nThe body.";
        Dispatcher.UIThread.RunJobs();

        window.Title.Should().Be("Commit to main (C:\\repo)");
        window.Watermark.IsVisible.Should().BeFalse();
        SaveScreenshot(window.CaptureRenderedFrame(), $"commit-{theme}");
        window.Close();
    });

    [Test]
    public Task Ctrl_Enter_commits_and_the_watermark_shows_without_message() => OnUiThreadAsync(() =>
    {
        CommitViewModelTests.FakeHost host = new() { StoredMessage = "" };
        (CommitViewModel viewModel, _) = CommitViewModelTests.Create(host);
        CommitWindow window = new() { DataContext = viewModel };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        window.Watermark.IsVisible.Should().BeTrue();
        window.Watermark.Text.Should().Be("Enter commit message");

        window.Activate();
        window.MessageEditor.Editor.TextArea.Focus();
        viewModel.Message.Text = "Fix the bug";
        Dispatcher.UIThread.RunJobs();
        window.Watermark.IsVisible.Should().BeFalse();
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.Control);
        Dispatcher.UIThread.RunJobs();

        host.Commits.Should().ContainSingle().Which.Message.Should().Be("Fix the bug");
        window.Close();
    });

    [Test]
    public Task Double_clicking_an_unstaged_file_stages_it() => OnUiThreadAsync(() =>
    {
        CommitViewModelTests.FakeHost host = new();
        (CommitViewModel viewModel, _) = CommitViewModelTests.Create(host);
        CommitWindow window = new() { DataContext = viewModel };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        viewModel.Unstaged.ActivateSelection();

        host.Staged.Should().Equal("a.txt");
        window.Close();
    });

    [Test]
    public Task The_templates_menu_lists_the_templates_and_conventional_commits() => OnUiThreadAsync(() =>
    {
        CommitViewModelTests.FakeHost host = new() { Templates = ([new GitCommands.CommitTemplateItem("Plugin", "From a plugin", icon: null, isRegex: false)], []) };
        (CommitViewModel viewModel, _) = CommitViewModelTests.Create(host);
        CommitWindow window = new() { DataContext = viewModel };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        IReadOnlyList<object> items = window.OpenCommitTemplatesMenu();

        items.OfType<MenuItem>().Select(i => i.Header).Should().Equal("Plugin", "Conven_tional Commits", "_Edit commit message templates and settings...");
        MenuItem conventional = items.OfType<MenuItem>().ElementAt(1);
        conventional.Items.OfType<MenuItem>().Select(i => i.Header).Should().StartWith(["build", "chore", "ci"]).And.Contain("[skip ci]");
        items.OfType<MenuItem>().First().RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
        viewModel.Message.Text.Should().Be("From a plugin");
        window.Close();
    });

    [Test]
    public Task Hotkeys_move_the_focus_and_add_the_selection_of_the_diff_to_the_message() => OnUiThreadAsync(() =>
    {
        (CommitViewModel viewModel, _) = CommitViewModelTests.Create(new CommitViewModelTests.FakeHost { StoredMessage = "" });
        CommitWindow window = new()
        {
            DataContext = viewModel,
            Hotkeys =
            [
                new HotkeyBinding((int)CommitHotkeyCommand.FocusSelectedDiff, 0x32 /* D2 */ | HotkeyBinding.Control),
                new HotkeyBinding((int)CommitHotkeyCommand.FocusStagedFiles, 0x33 /* D3 */ | HotkeyBinding.Control),
                new HotkeyBinding((int)CommitHotkeyCommand.FocusCommitMessage, 0x34 /* D4 */ | HotkeyBinding.Control),
                new HotkeyBinding((int)CommitHotkeyCommand.AddSelectionToCommitMessage, 0x43 /* C */),
                new HotkeyBinding((int)CommitHotkeyCommand.SelectNext, 0x4E /* N */ | HotkeyBinding.Control),
            ],
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.Activate();

        window.KeyPressQwerty(PhysicalKey.Digit4, RawInputModifiers.Control);
        window.MessageEditor.IsKeyboardFocusWithin.Should().BeTrue();
        window.KeyPressQwerty(PhysicalKey.N, RawInputModifiers.Control);
        Dispatcher.UIThread.RunJobs();
        viewModel.Staged.SelectedEntry!.Item.Name.Should().Be("c.txt", "from the message, the next staged file is selected");
        window.KeyPressQwerty(PhysicalKey.C, RawInputModifiers.None);
        window.MessageEditor.Editor.Text.Should().BeEmpty("without the focus in the diff, C is not a hotkey");

        viewModel.Diff.Show(new FileViewContent(FileViewKind.Diff, "@@ -1 +1 @@\n+added line\n"));
        Dispatcher.UIThread.RunJobs();
        window.KeyPressQwerty(PhysicalKey.Digit2, RawInputModifiers.Control);
        window.DiffViewer.IsKeyboardFocusWithin.Should().BeTrue();
        AvaloniaEdit.TextEditor diffEditor = window.DiffViewer.TextView.Editor;
        diffEditor.Select(diffEditor.Text.IndexOf("added", StringComparison.Ordinal), "added line".Length);
        window.KeyPressQwerty(PhysicalKey.C, RawInputModifiers.None);
        window.MessageEditor.Editor.Text.Should().Be("added line\n");
        window.MessageEditor.Editor.CaretOffset.Should().Be("added line\n".Length);

        window.KeyPressQwerty(PhysicalKey.Digit3, RawInputModifiers.Control);
        window.StagedFiles.IsKeyboardFocusWithin.Should().BeTrue();
        window.Close();
    });

    [Test]
    public Task The_conventional_commit_hotkey_opens_the_menu_and_inserts_the_scope() => OnUiThreadAsync(() =>
    {
        (CommitViewModel viewModel, _) = CommitViewModelTests.Create(new CommitViewModelTests.FakeHost { StoredMessage = "" });
        CommitWindow window = new()
        {
            DataContext = viewModel,
            Hotkeys = [new HotkeyBinding((int)CommitHotkeyCommand.ConventionalCommit_PrefixMessageWithScope, 0x54 /* T */ | HotkeyBinding.Control | HotkeyBinding.Shift)],
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.Activate();

        window.KeyPressQwerty(PhysicalKey.T, RawInputModifiers.Control | RawInputModifiers.Shift);
        Dispatcher.UIThread.RunJobs();

        MenuItem conventional = window.CommitTemplatesMenuItems.OfType<MenuItem>().Single(i => i.Items.Count > 0);
        conventional.IsSubMenuOpen.Should().BeTrue();
        MenuItem feat = conventional.Items.OfType<MenuItem>().Single(i => (string?)i.Header == ConventionalCommits.Feat);
        feat.IsSelected.Should().BeTrue("it is focused once its popup is shown");
        feat.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
        viewModel.Message.Text.Should().Be("feat(): ");
        window.Close();
    });

    [Test]
    public Task Typing_on_the_second_line_keeps_it_empty() => OnUiThreadAsync(() =>
    {
        (CommitViewModel viewModel, _) = CommitViewModelTests.Create(new CommitViewModelTests.FakeHost { StoredMessage = "", Options = new CommitDialogOptions { SecondLineMustBeEmpty = true } });
        CommitWindow window = new() { DataContext = viewModel };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.Activate();
        AvaloniaEdit.TextEditor editor = window.MessageEditor.Editor;
        editor.TextArea.Focus();

        window.KeyTextInput("Subject");
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        window.KeyTextInput("b");
        Dispatcher.UIThread.RunJobs();

        editor.Text.Should().Be($"Subject{Environment.NewLine}{Environment.NewLine}b");
        editor.CaretOffset.Should().Be(editor.Text.Length, "the caret stays after the typed text");
        viewModel.Message.Text.Should().Be(editor.Text);

        editor.Document.UndoStack.Undo();
        editor.Text.Should().Be($"Subject{Environment.NewLine}b", "the formatting is its own undo step");
        window.Close();
    });
}
