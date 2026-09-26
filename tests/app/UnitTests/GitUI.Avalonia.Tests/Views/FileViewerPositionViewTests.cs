using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.Editor;
using GitUI.Avalonia.Hosting;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.Editor;
using GitUI.Presentation.Services;

namespace GitUI.AvaloniaTests.Views;

/// <summary>
///  Headless tests of where the file viewer shows a text: the first change, the position kept for the same file, the
///  occurrences of the selection and the continuous scroll.
/// </summary>
[TestFixture]
public sealed class FileViewerPositionViewTests : HeadlessTest
{
    [TestCase(11)]
    [TestCase(12.5)]
    [TestCase(13.3)]
    public Task A_diff_is_shown_at_its_first_change_below_its_context(double fontSize) => OnUiThreadAsync(() =>
    {
        DiffViewModelTests.FakeViewerHost host = new() { Content = new FileViewContent(FileViewKind.Diff, LongDiff(contextBefore: 40), FileName: "f.cs") };
        (DialogWindow window, FileViewerView view, FileViewerViewModel viewModel) = Create(host);
        view.TextView.Editor.FontSize = fontSize;

        _ = viewModel.ShowChangesAsync(FileViewerContextMenuTests.Entry(StagedStatus.WorkTree));
        Dispatcher.UIThread.RunJobs();

        // As GoToFirstChange: the header and the lines of context above.
        view.TextView.Editor.TextArea.Caret.Line.Should().Be(45);
        view.TextView.FirstVisibleLine.Should().Be(45 - host.Settings.NumberOfContextLines - 1);
        window.Close();
    });

    [Test]
    public Task The_same_file_shown_again_keeps_the_line_of_the_caret() => OnUiThreadAsync(() =>
    {
        DiffViewModelTests.FakeViewerHost host = new() { Content = new FileViewContent(FileViewKind.Diff, LongDiff(contextBefore: 40), FileName: "f.cs") };
        (DialogWindow window, FileViewerView view, FileViewerViewModel viewModel) = Create(host);
        _ = viewModel.ShowChangesAsync(FileViewerContextMenuTests.Entry(StagedStatus.WorkTree));
        Dispatcher.UIThread.RunJobs();
        AvaloniaEdit.TextEditor editor = view.TextView.Editor;
        view.TextView.ScrollToFirstVisibleLine(55);
        editor.TextArea.Caret.Line = 60;
        Dispatcher.UIThread.RunJobs();

        // Shown again (e.g. with more context) with a line more in the header: the same line of the file.
        host.Content = new FileViewContent(FileViewKind.Diff, LongDiff(contextBefore: 40, indexLine: true), FileName: "f.cs");
        viewModel.IncreaseContextLinesCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();

        editor.TextArea.Caret.Line.Should().Be(61);
        view.TextView.FirstVisibleLine.Should().BeLessThanOrEqualTo(61);
        (view.TextView.FirstVisibleLine + view.TextView.VisibleLineCount).Should().BeGreaterThan(61);

        // Another file: its first change.
        host.Content = new FileViewContent(FileViewKind.Diff, LongDiff(contextBefore: 40), FileName: "g.cs");
        _ = viewModel.ShowChangesAsync(FileViewerContextMenuTests.Entry(StagedStatus.WorkTree));
        Dispatcher.UIThread.RunJobs();
        editor.TextArea.Caret.Line.Should().Be(45);
        window.Close();
    });

    [Test]
    public Task The_occurrences_of_the_selection_are_highlighted_and_gone_to_by_the_hotkeys() => OnUiThreadAsync(() =>
    {
        DiffViewModelTests.FakeViewerHost host = new()
        {
            Hotkeys =
            [
                new HotkeyBinding((int)FileViewerHotkeyCommand.NextOccurrence, 0x27 /* Right */ | HotkeyBinding.Alt),
                new HotkeyBinding((int)FileViewerHotkeyCommand.PreviousOccurrence, 0x25 /* Left */ | HotkeyBinding.Alt),
            ],
        };
        (DialogWindow window, FileViewerView view, FileViewerViewModel viewModel) = Create(host);
        viewModel.Show(new FileViewContent(FileViewKind.Text, "foo bar\nFoo baz\nfoo"));
        Dispatcher.UIThread.RunJobs();
        AvaloniaEdit.TextEditor editor = view.TextView.Editor;

        // As GetTextMarkersMatchingWord: ignoring the case.
        editor.Select(0, 3);
        view.TextView.Occurrences.Should().Equal(0, 8, 16);

        editor.TextArea.Focus();
        window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.Alt);
        editor.SelectionStart.Should().Be(8);
        window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.Alt);
        editor.SelectionStart.Should().Be(16);
        window.KeyPressQwerty(PhysicalKey.ArrowLeft, RawInputModifiers.Alt);
        editor.SelectionStart.Should().Be(8);
        editor.SelectedText.Should().Be("Foo", "the occurrence is selected");
        view.TextView.Occurrences.Should().Equal(0, 8, 16);

        editor.Select(3, 1);
        view.TextView.Occurrences.Should().BeEmpty("blanks are not searched for");
        window.Close();
    });

    [Test]
    public Task The_wheel_at_the_end_of_the_text_scrolls_on_into_the_next_file() => OnUiThreadAsync(() =>
    {
        DiffViewModelTests.FakeViewerHost host = new() { Settings = new FileViewerSettings(AutomaticContinuousScroll: true) };
        (DialogWindow window, FileViewerView view, FileViewerViewModel viewModel) = Create(host);
        List<string> reached = [];
        viewModel.BottomScrollReached += (_, _) => reached.Add("bottom");
        viewModel.TopScrollReached += (_, _) => reached.Add("top");
        viewModel.Show(new FileViewContent(FileViewKind.Text, "short\ntext"));
        Dispatcher.UIThread.RunJobs();
        Point center = view.TextView.TranslatePoint(new Point(100, 20), window)!.Value;

        window.MouseWheel(center, new Vector(0, -1));
        reached.Should().Equal("bottom");

        // The next file shown at its end, as after scrolling up into it (ScrollToBottom).
        viewModel.Editor.PendingScroll = TextScrollRequest.Bottom;
        viewModel.Show(new FileViewContent(FileViewKind.Diff, LongDiff(contextBefore: 80), FileName: "f.cs"));
        Dispatcher.UIThread.RunJobs();
        AvaloniaEdit.TextEditor editor = view.TextView.Editor;
        editor.VerticalOffset.Should().BeGreaterThan(0);
        (editor.VerticalOffset + editor.ViewportHeight).Should().BeApproximately(editor.TextArea.TextView.DocumentHeight, 1, "the last line at the bottom");
        window.Close();
    });

    [Test]
    public Task Alt_held_while_the_wheel_turns_scrolls_on() => OnUiThreadAsync(() =>
    {
        DiffViewModelTests.FakeViewerHost host = new();
        (DialogWindow window, FileViewerView view, FileViewerViewModel viewModel) = Create(host);
        List<string> reached = [];
        viewModel.BottomScrollReached += (_, _) => reached.Add("bottom");
        viewModel.Show(new FileViewContent(FileViewKind.Text, "short\ntext"));
        Dispatcher.UIThread.RunJobs();
        Point center = view.TextView.TranslatePoint(new Point(100, 20), window)!.Value;

        window.MouseWheel(center, new Vector(0, -1));
        reached.Should().BeEmpty("the continuous scroll is not automatic");

        // As Windows reports the wheel: without Alt among the modifiers, which the key tells.
        window.KeyPress(Key.LeftAlt, RawInputModifiers.Alt, PhysicalKey.AltLeft, keySymbol: null);
        window.MouseWheel(center, new Vector(0, -1));
        window.KeyRelease(Key.LeftAlt, RawInputModifiers.None, PhysicalKey.AltLeft, keySymbol: null);
        reached.Should().Equal("bottom");
        window.Close();
    });

    [Test]
    public Task Go_to_line_in_a_diff_goes_to_the_line_of_the_new_file() => OnUiThreadAsync(() =>
    {
        DiffViewModelTests.FakeViewerHost host = new() { Content = new FileViewContent(FileViewKind.Diff, LongDiff(contextBefore: 40), FileName: "f.cs") };
        (DialogWindow window, FileViewerView view, FileViewerViewModel viewModel) = Create(host);
        _ = viewModel.ShowChangesAsync(FileViewerContextMenuTests.Entry(StagedStatus.WorkTree));
        Dispatcher.UIThread.RunJobs();
        int? max = null;
        view.AskLineNumber = (_, maxLineNumber) =>
        {
            max = maxLineNumber;
            return 42;
        };

        view.ExecuteHotkeyCommand(FileViewerHotkeyCommand.GoToLine);

        // The new file has 40 lines of context, the new line and 20 more; its line 42 is the first after the change.
        max.Should().Be(61);
        view.TextView.Editor.TextArea.Caret.Line.Should().Be(47);
        window.Close();
    });

    /// <summary>A diff of one changed line after <paramref name="contextBefore"/> lines, and 20 lines after it.</summary>
    private static string LongDiff(int contextBefore, bool indexLine = false)
    {
        StringBuilder diff = new("diff --git a/f.cs b/f.cs\n");
        if (indexLine)
        {
            diff.Append("index 1111111..2222222 100644\n");
        }

        diff.Append("--- a/f.cs\n+++ b/f.cs\n");
        diff.Append($"@@ -1,{contextBefore + 21} +1,{contextBefore + 21} @@\n");
        for (int i = 1; i <= contextBefore; i++)
        {
            diff.Append($" line {i}\n");
        }

        diff.Append("-old\n+new\n");
        for (int i = 1; i <= 20; i++)
        {
            diff.Append($" tail {i}\n");
        }

        return diff.ToString();
    }

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
