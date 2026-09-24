using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using AvaloniaEdit.Rendering;
using GitUI.Avalonia.Editor;
using GitUI.Presentation.Editor;

namespace GitUI.AvaloniaTests.Views;

/// <summary>The syntax highlighting of the text editor by the TextMate grammars (<see cref="TextMateColorizer"/>).</summary>
[TestFixture]
public sealed class SyntaxHighlightingViewTests : HeadlessTest
{
    [Test]
    public Task A_file_is_highlighted_by_the_grammar_of_its_extension() => OnUiThreadAsync(() =>
    {
        TextEditorViewModel viewModel = new();
        viewModel.Load("// A comment\npublic class Sample\n{\n}\n", "Sample.cs");
        (Window window, TextEditorView view) = Show(viewModel);

        IBrush? comment = ForegroundAt(view, line: 1, text: "comment");
        IBrush? keyword = ForegroundAt(view, line: 2, text: "class");
        IBrush? name = ForegroundAt(view, line: 2, text: "Sample");
        comment.Should().NotBeNull();
        keyword.Should().NotBeNull();
        ColorOf(comment).Should().NotBe(ColorOf(keyword));
        ColorOf(name).Should().NotBe(ColorOf(keyword));
        view.Editor.SyntaxHighlighting.Should().BeNull("TextMate highlights the file instead of AvaloniaEdit");
        SaveScreenshot(window.CaptureRenderedFrame(), "syntax-highlighting-file");
        window.Close();
    });

    [Test]
    public Task A_diff_is_highlighted_as_its_old_and_new_files() => OnUiThreadAsync(() =>
    {
        const string diff = "@@ -1,3 +1,3 @@\n int a = 1;\n-/* the old start of a comment\n+int b = 2;\n int c = 3;\n";
        TextEditorViewModel viewModel = new();
        viewModel.LoadDiff(diff, new DiffLoadOptions(HighlightingFileName: "Sample.cs"));
        (Window window, TextEditorView view) = Show(viewModel);

        IBrush? keywordOfContext = ForegroundAt(view, line: 2, text: "int");
        IBrush? comment = ForegroundAt(view, line: 3, text: "comment");
        IBrush? keywordOfAdded = ForegroundAt(view, line: 4, text: "int");
        IBrush? keywordAfter = ForegroundAt(view, line: 5, text: "int");

        ColorOf(keywordOfContext).Should().NotBe(ColorOf(comment));
        ColorOf(keywordOfAdded).Should().Be(ColorOf(keywordOfContext), "the comment opened in the removed line is in the old file only");
        ColorOf(keywordAfter).Should().Be(ColorOf(keywordOfContext), "a context line is shown as in the new file");
        window.Close();
    });

    [Test]
    public Task A_file_without_grammar_is_not_highlighted_by_TextMate() => OnUiThreadAsync(() =>
    {
        TextEditorViewModel viewModel = new();
        viewModel.Load("plain text\n", "notes.unknownextension");
        (Window window, TextEditorView view) = Show(viewModel);

        view.Editor.TextArea.TextView.LineTransformers.OfType<TextMateColorizer>().Should().BeEmpty();
        window.Close();
    });

    [Test]
    public Task The_dark_theme_uses_the_dark_colors_of_the_grammar() => OnUiThreadAsync(() =>
    {
        TextEditorViewModel viewModel = new();
        viewModel.Load("public class Sample\n{\n}\n", "Sample.cs");
        (Window lightWindow, TextEditorView light) = Show(viewModel);
        Color? lightKeyword = ColorOf(ForegroundAt(light, line: 1, text: "class"));
        lightWindow.Close();

        UseTheme(ThemeVariant.Dark);
        TextEditorViewModel darkViewModel = new();
        darkViewModel.Load("public class Sample\n{\n}\n", "Sample.cs");
        (Window darkWindow, TextEditorView dark) = Show(darkViewModel);
        ColorOf(ForegroundAt(dark, line: 1, text: "class")).Should().NotBe(lightKeyword);
        SaveScreenshot(darkWindow.CaptureRenderedFrame(), "syntax-highlighting-file-dark");
        darkWindow.Close();
    });

    private static (Window Window, TextEditorView View) Show(TextEditorViewModel viewModel)
    {
        TextEditorView view = new() { DataContext = viewModel };
        Window window = new() { Content = view, Width = 560, Height = 220 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, view);
    }

    /// <summary>The foreground of the visual element of line <paramref name="line"/> that shows <paramref name="text"/>.</summary>
    private static IBrush? ForegroundAt(TextEditorView view, int line, string text)
    {
        TextView textView = view.Editor.TextArea.TextView;
        VisualLine visualLine = textView.GetOrConstructVisualLine(view.Editor.Document.GetLineByNumber(line));
        int column = view.Editor.Document.GetText(view.Editor.Document.GetLineByNumber(line)).IndexOf(text, StringComparison.Ordinal);
        column.Should().BeGreaterThanOrEqualTo(0);
        VisualLineElement element = visualLine.Elements.First(e => e.RelativeTextOffset <= column && column < e.RelativeTextOffset + e.DocumentLength);
        return element.TextRunProperties.ForegroundBrush;
    }

    private static Color? ColorOf(IBrush? brush) => (brush as ISolidColorBrush)?.Color;
}
