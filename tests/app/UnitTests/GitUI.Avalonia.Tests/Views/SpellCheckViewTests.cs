using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Threading;
using AvaloniaEdit;
using GitUI.Avalonia.CommandsDialogs.CommitDialog;
using GitUI.Avalonia.Editor;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs.CommitDialog;
using GitUI.Presentation.SpellChecker;
using GitUI.Presentation.UserControls.FileStatusList;
using static GitUI.AvaloniaTests.ViewModels.SpellCheckViewModelTests;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless tests of the spell checking and auto-completion of an editor (port of <c>EditNetSpell</c>).</summary>
[TestFixture]
public sealed class SpellCheckViewTests : HeadlessTest
{
    [Test]
    public Task Render_the_marks([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        (Window window, TextEditor editor, SpellCheckController controller) = Create(new FakeSpellCheckHost());
        editor.Text = "Fix teh bug in the subject line that is much longer than fifty characters\n\nThe body.";
        controller.CheckSpelling();
        Dispatcher.UIThread.RunJobs();

        controller.Marks.Mistakes.Should().Equal(new TextSpan(4, 3));
        controller.Marks.IllFormedLines.Should().Equal(new TextSpan(50, 23));
        SaveScreenshot(window.CaptureRenderedFrame(), $"spell-check-{theme}");
        window.Close();
    });

    [Test]
    public Task The_context_menu_has_the_suggestions_and_the_options() => OnUiThreadAsync(() =>
    {
        FakeSpellCheckHost host = new();
        (Window window, TextEditor editor, SpellCheckController controller) = Create(host);
        editor.Text = "Fix teh bug";
        editor.CaretOffset = 5;

        IReadOnlyList<object> items = controller.FillContextMenu();

        items.Select(i => i is MenuItem item ? item.Header : "-").Should().Equal(
            "the", "ten", "Add to dictionary", "Ignore word", "Remove word", "-",
            "Cut", "Copy", "Paste", "Delete", "Select all", "-",
            "Dictionary", "-",
            "Mark ill formed lines", "Provide auto completion");
        MenuItem dictionaries = items.OfType<MenuItem>().Single(i => (string?)i.Header == "Dictionary");
        dictionaries.Items.OfType<MenuItem>().Select(i => (i.Header, i.IsChecked)).Should().Equal(("None", false), ("de-DE", false), ("en-US", true));

        Click(items.OfType<MenuItem>().First());
        editor.Text.Should().Be("Fix the bug");

        editor.Text = "Fix teh bug";
        editor.CaretOffset = 5;
        Click(controller.FillContextMenu().OfType<MenuItem>().Single(i => (string?)i.Header == "Remove word"));
        editor.Text.Should().Be("Fix bug");

        editor.CaretOffset = 1;
        controller.FillContextMenu().OfType<MenuItem>().First().Header.Should().Be("Cut", "a word spelled right has no suggestions");
        window.Close();
    });

    [Test]
    public Task Ctrl_Space_completes_the_word_or_shows_the_list() => OnUiThreadAsync(() =>
    {
        FakeSpellCheckHost host = new();
        host.Words.SetResult(["FileStatusList", "FileViewer"]);
        (Window window, TextEditor editor, SpellCheckController controller) = Create(host);
        controller.ViewModel.IsAutoCompleteLoaded.Should().BeTrue("the words are loaded at once");
        window.Activate();
        editor.TextArea.Focus();

        editor.Text = "Fix FSL";
        editor.CaretOffset = editor.Text.Length;
        window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.Control);
        editor.Text.Should().Be("Fix FileStatusList");

        editor.Text = "Fix Fi";
        editor.CaretOffset = editor.Text.Length;
        window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.Control);
        controller.CompletionWindow.Should().NotBeNull();
        controller.CompletionWindow!.CompletionList.CompletionData.Select(d => d.Text).Should().Equal("FileStatusList", "FileViewer");

        window.KeyTextInput(" ");
        Dispatcher.UIThread.RunJobs();
        controller.CompletionWindow.Should().BeNull("a separator closes the list");
        window.Close();
    });

    [Test]
    public Task The_commit_message_menu_has_the_word_wrap_before_the_first_separator() => OnUiThreadAsync(() =>
    {
        CommitViewModel viewModel = new(
            new CommitStrings(), new CommitViewModelTests.FakeHost { StoredMessage = "" }, new DiffViewModelTests.FakeViewerHost(),
            new FileStatusListStrings(), new FileStatusTreeOptions(), spellCheckHost: new FakeSpellCheckHost());
        CommitWindow window = new() { DataContext = viewModel };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        window.SpellCheck.Should().NotBeNull();
        IReadOnlyList<object> items = window.SpellCheck!.FillContextMenu();
        items.Select(i => i is MenuItem item ? item.Header : "-").Take(7).Should().Equal(
            "Cut", "Copy", "Paste", "Delete", "Select all", "_Word wrap (except subject line)", "-");
        window.Close();
    });

    private static (Window Window, TextEditor Editor, SpellCheckController Controller) Create(FakeSpellCheckHost host)
    {
        SpellCheckViewModel viewModel = new(new SpellCheckStrings(), host);
        viewModel.LoadAutoCompleteWords();
        TextEditor editor = new();
        SpellCheckController controller = new(editor, viewModel);
        Window window = new() { Content = editor, Width = 600, Height = 150 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, editor, controller);
    }

    private static void Click(MenuItem item)
    {
        item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Dispatcher.UIThread.RunJobs();
    }
}
