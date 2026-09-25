using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Threading;
using GitUI.Avalonia.Editor;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.Editor;

namespace GitUI.AvaloniaTests.Views;

/// <summary>
///  Headless view tests of the search of the text editors: AvaloniaEdit's search panel with the behaviour of
///  <c>FindAndReplaceForm</c>.
/// </summary>
[TestFixture]
public sealed class EditorSearchViewTests : HeadlessTest
{
    private const string Text = "first line\nsecond value line\nthird value\n";

    [Test]
    public Task Ctrl_F_searches_the_word_at_the_caret_or_the_selection_on_one_line() => OnUiThreadAsync(() =>
    {
        (Window window, TextEditorView view) = Show(readOnly: false);
        view.Editor.CaretOffset = Text.IndexOf("cond", StringComparison.Ordinal);

        window.KeyPressQwerty(PhysicalKey.F, TestKeys.Command);
        Dispatcher.UIThread.RunJobs();

        view.Search.IsOpened.Should().BeTrue();
        view.Search.IsReplaceMode.Should().BeFalse();
        view.Search.SearchPattern.Should().Be("second");

        view.Search.Close();
        view.Editor.Select(Text.IndexOf("value line", StringComparison.Ordinal), "value line".Length);
        view.OpenSearch(replace: false);
        view.Search.SearchPattern.Should().Be("value line");

        view.Search.Close();
        view.Editor.Select(0, Text.IndexOf("value", StringComparison.Ordinal));
        view.OpenSearch(replace: false);
        view.Search.SearchPattern.Should().Be("value line", "a selection of several lines keeps the previous search");
        window.Close();
    });

    [Test]
    public Task F3_finds_the_next_and_previous_match_once_the_panel_is_closed() => OnUiThreadAsync(() =>
    {
        (Window window, TextEditorView view) = Show(readOnly: true);
        view.Search.SearchPattern = "VALUE";
        view.Search.Open();
        view.Search.Close();
        view.Editor.Select(0, 0);
        view.Editor.TextArea.Focus();
        int second = Text.IndexOf("value", StringComparison.Ordinal);
        int third = Text.LastIndexOf("value", StringComparison.Ordinal);

        window.KeyPressQwerty(PhysicalKey.F3, RawInputModifiers.None);
        view.Search.IsOpened.Should().BeFalse("the match is selected without the panel");
        (view.Editor.SelectionStart, view.Editor.SelectionLength).Should().Be((second, 5));

        window.KeyPressQwerty(PhysicalKey.F3, RawInputModifiers.None);
        view.Editor.SelectionStart.Should().Be(third);

        window.KeyPressQwerty(PhysicalKey.F3, RawInputModifiers.None);
        view.Editor.SelectionStart.Should().Be(second, "the search loops around");

        window.KeyPressQwerty(PhysicalKey.F3, RawInputModifiers.Shift);
        view.Editor.SelectionStart.Should().Be(third, "Shift+F3 finds the previous match, looping around");

        view.Search.MatchCase = true;
        view.FindNext(backward: false).Should().BeFalse("nothing matches the case");
        view.Search.IsOpened.Should().BeTrue("the panel shows that nothing was found");
        window.Close();
    });

    [Test]
    public Task F3_without_a_search_opens_the_panel() => OnUiThreadAsync(() =>
    {
        (Window window, TextEditorView view) = Show(readOnly: true);

        view.FindNext(backward: false).Should().BeFalse();

        view.Search.IsOpened.Should().BeTrue();
        window.Close();
    });

    [Test]
    public Task Replace_only_in_editable_texts() => OnUiThreadAsync(() =>
    {
        (Window window, TextEditorView view) = Show(readOnly: true);
        view.Editor.CaretOffset = Text.IndexOf("value", StringComparison.Ordinal);
        view.Editor.TextArea.Focus();

        PressReplace(window);
        Dispatcher.UIThread.RunJobs();
        view.Search.IsReplaceMode.Should().BeFalse("a read-only text cannot be replaced");
        view.OpenSearch(replace: true);
        view.Search.IsReplaceMode.Should().BeFalse();
        window.Close();

        (window, view) = Show(readOnly: false);
        view.Editor.CaretOffset = Text.IndexOf("value", StringComparison.Ordinal);
        view.Editor.TextArea.Focus();
        PressReplace(window);
        Dispatcher.UIThread.RunJobs();
        view.Search.IsReplaceMode.Should().BeTrue();
        view.Search.SearchPattern.Should().Be("value");
        view.Search.ReplacePattern = "number";
        view.Search.ReplaceAll();

        ((TextEditorViewModel)view.DataContext!).Text.Should().Be("first line\nsecond number line\nthird number\n");
        window.Close();
    });

    [Test]
    public Task The_file_viewer_hotkeys_find_also_with_the_panel_closed() => OnUiThreadAsync(() =>
    {
        DiffViewModelTests.FakeViewerHost host = new();
        FileViewerViewModel viewModel = new(host);
        FileViewerView view = new() { DataContext = viewModel };
        Window window = new() { Content = view, Width = 500, Height = 300 };
        window.Show();
        viewModel.Show(new FileViewContent(FileViewKind.Text, Text, "notes.txt"));
        Dispatcher.UIThread.RunJobs();
        AvaloniaEdit.TextEditor editor = view.TextView.Editor;
        editor.CaretOffset = Text.IndexOf("value", StringComparison.Ordinal) + 1;

        view.ExecuteHotkeyCommand(FileViewerHotkeyCommand.Find).Should().BeTrue();
        Dispatcher.UIThread.RunJobs();
        view.TextView.Search.SearchPattern.Should().Be("value");
        view.TextView.Search.Close();
        editor.Select(0, 0);

        view.ExecuteHotkeyCommand(FileViewerHotkeyCommand.FindNextOrOpenWithDifftool).Should().BeTrue();
        editor.SelectionStart.Should().Be(Text.IndexOf("value", StringComparison.Ordinal));
        view.ExecuteHotkeyCommand(FileViewerHotkeyCommand.FindPrevious).Should().BeTrue();
        editor.SelectionStart.Should().Be(Text.LastIndexOf("value", StringComparison.Ordinal));
        view.ExecuteHotkeyCommand(FileViewerHotkeyCommand.Replace).Should().BeFalse("the viewer is read-only");
        window.Close();
    });

    [Test]
    public Task The_search_panel_shows_the_texts_of_FindAndReplaceForm() => OnUiThreadAsync(() =>
    {
        (Window window, _) = Show(readOnly: false);

        AvaloniaEdit.SR.SearchMatchCaseText.Should().Be("Match case");
        AvaloniaEdit.SR.SearchMatchWholeWordsText.Should().Be("Match whole word");
        AvaloniaEdit.SR.SearchFindNextText.Should().Be("Find next (F3)");
        AvaloniaEdit.SR.SearchNoMatchesFoundText.Should().Be("Text not found");
        AvaloniaEdit.SR.SearchToggleReplace.Should().Be("Find & replace");
        AvaloniaEdit.SR.SearchUseRegexText.Should().Be("Use regular expressions", "without a translation, AvaloniaEdit's text is kept");
        window.Close();
    });

    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        (Window window, TextEditorView view) = Show(readOnly: false);
        view.Editor.CaretOffset = Text.IndexOf("value", StringComparison.Ordinal);
        view.OpenSearch(replace: true);
        Dispatcher.UIThread.RunJobs();

        SaveScreenshot(window.CaptureRenderedFrame(), $"editor-search-{theme}");
        window.Close();
    });

    private static (Window Window, TextEditorView View) Show(bool readOnly)
    {
        TextEditorViewModel viewModel = new() { IsReadOnly = readOnly };
        viewModel.Load(Text);
        TextEditorView view = new() { DataContext = viewModel };
        Window window = new() { Content = view, Width = 560, Height = 220 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        view.Editor.TextArea.Focus();
        return (window, view);
    }

    /// <summary>Ctrl+H, Cmd+Option+F on macOS (where Cmd+H hides the application).</summary>
    private static void PressReplace(Window window)
    {
        if (OperatingSystem.IsMacOS())
        {
            window.KeyPressQwerty(PhysicalKey.F, TestKeys.Command | RawInputModifiers.Alt);
        }
        else
        {
            window.KeyPressQwerty(PhysicalKey.H, TestKeys.Command);
        }
    }
}
