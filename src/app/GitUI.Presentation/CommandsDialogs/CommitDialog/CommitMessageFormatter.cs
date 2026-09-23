namespace GitUI.Presentation.CommandsDialogs.CommitDialog;

/// <summary>The formatting settings of the commit message (the <c>CommitValidation*</c> settings).</summary>
public sealed record CommitMessageFormatOptions(
    int MaxFirstLineLength = 0,
    int MaxLineLength = 0,
    bool SecondLineMustBeEmpty = false,
    bool AutoWrap = false,
    bool IndentAfterFirstLine = false);

/// <summary>A replacement in the message: <see cref="Length"/> characters at <see cref="Offset"/> become <see cref="Text"/>.</summary>
public sealed record TextEdit(int Offset, int Length, string Text);

/// <summary>
///  The formatting of the commit message as it is typed (port of <c>FormCommit.FormatAllText</c> and <c>WordWrapper</c>): the
///  second line kept empty, and the lines of the body wrapped at their limit.
/// </summary>
public static class CommitMessageFormatter
{
    /// <summary>The limit of the body when none is set (as <c>WordWrapCommitMessageBody</c>).</summary>
    public const int DefaultBodyLineLimit = 72;

    /// <summary>
    ///  The edits that format the message, the first one first (apply them from the last): as <c>FormatLine</c>, an empty line
    ///  (and a bullet, if set) before a text on the second line, and the wrapped lines of the body.
    /// </summary>
    public static IReadOnlyList<TextEdit> GetEdits(string text, CommitMessageFormatOptions options)
    {
        List<TextEdit> edits = [];
        IReadOnlyList<(int Offset, string Line)> lines = SplitLines(text);
        if (options.SecondLineMustBeEmpty && lines.Count > 1 && lines[1].Line.Length > 0)
        {
            // As EditNetSpell.EnsureEmptyLine: the text moves to the third line.
            edits.Add(new TextEdit(lines[1].Offset, 0, Environment.NewLine + (options.IndentAfterFirstLine ? " - " : "")));
        }

        if (options.AutoWrap && options.MaxLineLength > 0)
        {
            int firstBodyLine = options.SecondLineMustBeEmpty ? 2 : 1;
            for (int index = firstBodyLine; index < lines.Count; index++)
            {
                if (WrapLineIfNecessary(lines[index].Line, options.MaxLineLength) is { } wrapped)
                {
                    edits.Add(new TextEdit(lines[index].Offset, lines[index].Line.Length, wrapped));
                }
            }
        }

        return edits;
    }

    /// <summary>As <c>WordWrapCommitMessageBody</c>: the lines after the subject wrapped at the limit.</summary>
    public static IReadOnlyList<TextEdit> GetBodyWrapEdits(string text, int maxLineLength)
    {
        int limit = maxLineLength > 0 ? maxLineLength : DefaultBodyLineLimit;
        IReadOnlyList<(int Offset, string Line)> lines = SplitLines(text);
        List<TextEdit> edits = [];
        for (int index = 1; index < lines.Count; index++)
        {
            if (WrapLineIfNecessary(lines[index].Line, limit) is { } wrapped)
            {
                edits.Add(new TextEdit(lines[index].Offset, lines[index].Line.Length, wrapped));
            }
        }

        return edits;
    }

    /// <summary>Applies the edits (as the view does), e.g. for tests.</summary>
    public static string Apply(string text, IReadOnlyList<TextEdit> edits)
    {
        foreach (TextEdit edit in edits.OrderByDescending(e => e.Offset))
        {
            text = text.Remove(edit.Offset, edit.Length).Insert(edit.Offset, edit.Text);
        }

        return text;
    }

    /// <summary>As <c>WordWrapCommitMessageLineIfNecessary</c>: the wrapped line, or <see langword="null"/> if unchanged.</summary>
    private static string? WrapLineIfNecessary(string line, int lineLimit)
    {
        if (line.Length <= lineLimit)
        {
            return null;
        }

        string wrapped = WrapSingleLine(line, lineLimit);
        return wrapped == line ? null : wrapped;
    }

    /// <summary>Port of <c>WordWrapper.WrapSingleLine</c>: the words of the line on lines shorter than the limit.</summary>
    public static string WrapSingleLine(string text, int lineLimit)
    {
        List<string> lines = [];
        List<string> words = [];
        int wordsLength = 0;
        foreach (string word in text.Split())
        {
            // As WrapperState.CanAddWord: a word always fits on an empty line.
            if (words.Count > 0 && wordsLength + words.Count + word.Length >= lineLimit)
            {
                lines.Add(string.Join(" ", words));
                words.Clear();
                wordsLength = 0;
            }

            words.Add(word);
            wordsLength += word.Length;
        }

        if (words.Count > 0)
        {
            lines.Add(string.Join(" ", words));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static IReadOnlyList<(int Offset, string Line)> SplitLines(string text)
    {
        List<(int Offset, string Line)> lines = [];
        int start = 0;
        while (true)
        {
            int end = text.IndexOf('\n', start);
            if (end < 0)
            {
                lines.Add((start, text[start..]));
                return lines;
            }

            int lineEnd = end > start && text[end - 1] == '\r' ? end - 1 : end;
            lines.Add((start, text[start..lineEnd]));
            start = end + 1;
        }
    }
}
