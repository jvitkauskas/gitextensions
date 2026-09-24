namespace GitUI.Presentation.Editor;

/// <summary>Where a text is shown: the caret and the first visible line (1-based), and whether the caret is in view.</summary>
public readonly record struct ViewerPosition(int CaretLine, int CaretColumn, int FirstVisibleLine, bool CaretVisible);

/// <summary>
///  The position of the viewer kept when the same content is shown again, e.g. with other options or in another revision (a
///  port of <c>FileViewerInternal.CurrentViewPositionCache</c>): in a diff, the line of the file at the caret (or at the top),
///  else the line of the text.
/// </summary>
public sealed class ViewPositionCache
{
    private string? _currentIdentification;
    private string? _capturedIdentification;
    private ViewerPosition _position;
    private DiffLine? _activeLine;

    /// <summary>As <c>Capture</c>: remembers the position of the content shown until now.</summary>
    /// <param name="lineCount">The number of lines of the content shown.</param>
    /// <param name="diffLines">The lines of the diff shown, <see langword="null"/> for a text (whose line is kept).</param>
    public void Capture(ViewerPosition position, int lineCount, IReadOnlyList<DiffLine>? diffLines)
    {
        if (lineCount <= 1 || string.IsNullOrEmpty(_currentIdentification))
        {
            return;
        }

        _capturedIdentification = _currentIdentification;
        _position = position;
        _activeLine = null;
        if (diffLines is null)
        {
            return;
        }

        Dictionary<int, DiffLine> lines = [];
        foreach (DiffLine line in diffLines)
        {
            lines.TryAdd(line.LineNumInDiff, line);
        }

        // A line with line numbers, from the caret (or the top) down, else up.
        int initialLine = position.CaretVisible ? position.CaretLine : position.FirstVisibleLine;
        for (int line = initialLine; line <= lineCount && _activeLine is null; line++)
        {
            _activeLine = GetNumberedLine(line);
        }

        for (int line = initialLine - 1; line >= 1 && _activeLine is null; line--)
        {
            _activeLine = GetNumberedLine(line);
        }

        DiffLine? GetNumberedLine(int line)
            => lines.TryGetValue(line, out DiffLine? diffLine)
                && (diffLine.LeftLineNumber != DiffLine.NotApplicable || diffLine.RightLineNumber != DiffLine.NotApplicable)
                    ? diffLine
                    : null;
    }

    /// <summary>
    ///  As <c>Restore</c>: the position to show the new content at if it is the same content as captured (e.g. the same file),
    ///  else <see langword="null"/>.
    /// </summary>
    /// <param name="contentIdentification">What the new content is (the file name), <see langword="null"/> for no position to keep.</param>
    /// <param name="visibleLineCount">The number of lines the viewer shows, to center the caret.</param>
    public ViewerPosition? Restore(string? contentIdentification, int lineCount, IReadOnlyList<DiffLine>? diffLines, int visibleLineCount)
    {
        _currentIdentification = contentIdentification;
        if (lineCount <= 1 || string.IsNullOrEmpty(contentIdentification) || contentIdentification != _capturedIdentification)
        {
            return null;
        }

        if (_activeLine is null || diffLines is null)
        {
            return _position with
            {
                CaretLine = Math.Clamp(_position.CaretLine, 1, lineCount),
                FirstVisibleLine = Math.Clamp(_position.FirstVisibleLine, 1, lineCount),
            };
        }

        // The left line number first, as the base revision does not change.
        int caretLine = _activeLine.LeftLineNumber != DiffLine.NotApplicable
            ? GetLineInDiff(diffLines, _activeLine.LeftLineNumber, rightFile: false)
            : GetLineInDiff(diffLines, _activeLine.RightLineNumber, rightFile: true);

        // As CenterViewOn (with its threshold of 5 lines), or the line at the top.
        int firstVisibleLine = _position.CaretVisible ? Math.Max(1, caretLine - (visibleLineCount / 2)) : caretLine;
        return _position with { CaretLine = caretLine, FirstVisibleLine = firstVisibleLine };
    }

    /// <summary>
    ///  As <c>GetCaretOffset</c>: the line of the diff (1-based) of the line of the old (or new) file, or the one after it; the first
    ///  line if there is none.
    /// </summary>
    public static int GetLineInDiff(IReadOnlyList<DiffLine> diffLines, int lineNumber, bool rightFile)
    {
        foreach (DiffLine line in diffLines.OrderBy(line => line.LineNumInDiff))
        {
            int fileLine = rightFile ? line.RightLineNumber : line.LeftLineNumber;
            if (fileLine != DiffLine.NotApplicable && fileLine >= lineNumber)
            {
                return line.LineNumInDiff;
            }
        }

        return 1;
    }
}
