using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Threading;
using GitUI.Avalonia.CommandsDialogs.CommitDialog;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs.CommitDialog;

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
