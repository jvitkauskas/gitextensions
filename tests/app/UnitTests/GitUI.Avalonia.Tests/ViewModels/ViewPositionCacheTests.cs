using GitUI.Presentation.Editor;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>The position kept when the viewer shows the same content again (<c>CurrentViewPositionCache</c>).</summary>
[TestFixture]
public sealed class ViewPositionCacheTests
{
    [Test]
    public void The_same_file_is_shown_again_at_the_line_of_the_old_file()
    {
        ViewPositionCache cache = new();
        cache.Restore("f.cs", 20, Lines(firstLeft: 10, firstLineInDiff: 5), visibleLineCount: 10).Should().BeNull("nothing was shown before");

        // The caret on the line 8 of the diff, the line 13 of the old file.
        cache.Capture(new ViewerPosition(CaretLine: 8, CaretColumn: 3, FirstVisibleLine: 1, CaretVisible: true), 20, Lines(10, 5));

        // With two more lines above (e.g. more context): the line 13 is the line 10 of the diff, centered.
        cache.Restore("f.cs", 22, Lines(10, 7), visibleLineCount: 10).Should().Be(new ViewerPosition(10, 3, 5, CaretVisible: true));
    }

    [Test]
    public void A_caret_out_of_view_keeps_the_line_at_the_top()
    {
        ViewPositionCache cache = new();
        cache.Restore("f.cs", 20, Lines(10, 5), visibleLineCount: 10);

        // From the top line of the view, the first line with line numbers below it (the headers have none).
        cache.Capture(new ViewerPosition(CaretLine: 1, CaretColumn: 1, FirstVisibleLine: 3, CaretVisible: false), 20, Lines(10, 5));

        cache.Restore("f.cs", 20, Lines(10, 4), visibleLineCount: 10).Should().Be(new ViewerPosition(4, 1, 4, CaretVisible: false));
    }

    [Test]
    public void Another_content_is_shown_from_its_start()
    {
        ViewPositionCache cache = new();
        cache.Restore("f.cs", 20, diffLines: null, visibleLineCount: 10);
        cache.Capture(new ViewerPosition(12, 1, 8, CaretVisible: true), 20, diffLines: null);

        cache.Restore("g.cs", 20, diffLines: null, visibleLineCount: 10).Should().BeNull();
        cache.Restore(null, 20, diffLines: null, visibleLineCount: 10).Should().BeNull("without identification, nothing is kept");
    }

    [Test]
    public void A_text_keeps_its_line_within_the_new_text()
    {
        ViewPositionCache cache = new();
        cache.Restore("f.txt", 40, diffLines: null, visibleLineCount: 10);
        cache.Capture(new ViewerPosition(30, 2, 25, CaretVisible: true), 40, diffLines: null);

        cache.Restore("f.txt", 20, diffLines: null, visibleLineCount: 10).Should().Be(new ViewerPosition(20, 2, 20, CaretVisible: true));
    }

    [Test]
    public void The_line_of_a_file_in_a_diff_is_the_first_one_from_it()
    {
        // As GetCaretOffset (go to line): the line of the new file, or the next one in the diff.
        IReadOnlyList<DiffLine> lines =
        [
            new(1, DiffLine.NotApplicable, DiffLine.NotApplicable, DiffLineKind.Header),
            new(2, 5, 5, DiffLineKind.Context),
            new(3, 6, DiffLine.NotApplicable, DiffLineKind.Minus),
            new(4, DiffLine.NotApplicable, 6, DiffLineKind.Plus),
            new(5, DiffLine.NotApplicable, DiffLine.NotApplicable, DiffLineKind.Header),
            new(6, 20, 20, DiffLineKind.Context),
        ];

        ViewPositionCache.GetLineInDiff(lines, 6, rightFile: true).Should().Be(4);
        ViewPositionCache.GetLineInDiff(lines, 6, rightFile: false).Should().Be(3);
        ViewPositionCache.GetLineInDiff(lines, 10, rightFile: true).Should().Be(6);
        ViewPositionCache.GetLineInDiff(lines, 99, rightFile: true).Should().Be(1);
    }

    private static IReadOnlyList<DiffLine> Lines(int firstLeft, int firstLineInDiff)
        => [.. Enumerable.Range(0, 12).Select(i => new DiffLine(firstLineInDiff + i, firstLeft + i, firstLeft + i, DiffLineKind.Context))];
}
