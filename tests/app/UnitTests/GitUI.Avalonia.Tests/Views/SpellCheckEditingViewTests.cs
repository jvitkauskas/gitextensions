using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;
using GitUI.Avalonia.Editor;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.SpellChecker;
using static GitUI.AvaloniaTests.ViewModels.SpellCheckViewModelTests;

namespace GitUI.AvaloniaTests.Views;

/// <summary>The editing of the commit message editor as <c>EditNetSpell</c>: paste, Shift+Enter, the double click, the list keys.</summary>
[TestFixture]
public sealed class SpellCheckEditingViewTests : HeadlessTest
{
    [Test]
    public Task Up_on_the_first_word_of_the_list_goes_to_the_last_and_Down_on_the_last_to_the_first() => OnUiThreadAsync(() =>
    {
        FakeSpellCheckHost host = new();
        host.Words.SetResult(["FileStatusList", "FileViewer"]);
        (Window window, TextEditor editor, SpellCheckController controller) = Create(host);
        editor.Text = "Fix Fi";
        editor.CaretOffset = editor.Text.Length;
        window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.Control);
        controller.CompletionWindow!.CompletionList.SelectedItem!.Text.Should().Be("FileStatusList");

        window.KeyPressQwerty(PhysicalKey.ArrowUp, RawInputModifiers.None);
        controller.CompletionWindow!.CompletionList.SelectedItem!.Text.Should().Be("FileViewer");
        window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
        controller.CompletionWindow!.CompletionList.SelectedItem!.Text.Should().Be("FileStatusList");
        window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
        controller.CompletionWindow!.CompletionList.SelectedItem!.Text.Should().Be("FileViewer", "within the list, the keys are the list's");
        window.Close();
    });

    [Test]
    public Task A_double_click_selects_the_word_with_its_leading_dot() => OnUiThreadAsync(() =>
    {
        (Window window, TextEditor editor, _) = Create(new FakeSpellCheckHost());
        editor.Text = "Ignore .git_dir files";
        Dispatcher.UIThread.RunJobs();
        TextView textView = editor.TextArea.TextView;
        textView.EnsureVisualLines();
        Point inWord = textView.GetVisualPosition(new TextViewPosition(1, 11), VisualYPosition.LineMiddle) - textView.ScrollOffset;
        Point point = textView.TranslatePoint(inWord, window)!.Value;

        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        // As WordAtCursorExtractor (AvaloniaEdit would select "git_dir" only).
        editor.SelectedText.Should().Be(".git_dir");
        window.Close();
    });

    [Test]
    public Task Pasted_vertical_tabs_are_line_feeds_and_Shift_Enter_is_a_new_line() => OnUiThreadAsync(async () =>
    {
        (Window window, TextEditor editor, _) = Create(new FakeSpellCheckHost());
        editor.Text = "Subject";
        editor.CaretOffset = editor.Text.Length;
        await window.Clipboard!.SetTextAsync("\vbody line\vmore");

        editor.Paste();
        for (int i = 0; i < 50 && !editor.Text.Contains("more"); i++)
        {
            await Task.Delay(10);
            Dispatcher.UIThread.RunJobs();
        }

        editor.Text.Should().Be("Subject\nbody line\nmore");

        // As AddNewLine (a RichTextBox would add a vertical tab).
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.Shift);
        editor.Text.Should().Be("Subject\nbody line\nmore\n");
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
        window.Activate();
        editor.TextArea.Focus();
        return (window, editor, controller);
    }
}
