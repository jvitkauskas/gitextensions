using GitUI.Presentation.Editor;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>The line and column of the file at the caret (<c>CurrentFileLine</c>, <c>CurrentFileColumn</c>), which scripts get.</summary>
[TestFixture]
public sealed class TextEditorFilePositionTests
{
    [Test]
    public void A_text_has_the_line_and_column_of_the_caret()
    {
        TextEditorViewModel editor = new();
        editor.Load("first\nsecond");

        editor.CaretOffset = 0;
        editor.GetCurrentFilePosition().Should().Be((1, 0));
        editor.CaretOffset = 9;
        editor.GetCurrentFilePosition().Should().Be((2, 3));
    }

    [Test]
    public void A_diff_has_the_line_of_the_new_file_else_of_the_old_one()
    {
        TextEditorViewModel editor = new();
        string patch = FileViewerContextMenuTests.Patch;
        editor.LoadDiff(patch);

        editor.CaretOffset = patch.IndexOf("+c", StringComparison.Ordinal) + 1;
        editor.GetCurrentFilePosition().Should().Be((2, 1));
        editor.CaretOffset = patch.IndexOf("-b", StringComparison.Ordinal);
        editor.GetCurrentFilePosition().Should().Be((2, 0), "a removed line has the line of the old file");
        editor.CaretOffset = 0;
        editor.GetCurrentFilePosition().Should().BeNull("the header has no line of the file");
    }
}
