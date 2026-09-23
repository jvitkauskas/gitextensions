using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace GitUI.Avalonia.CommandsDialogs.CommitDialog;

/// <summary>
///  Colors the text of the commit message beyond the line limits (as <c>FormCommit.FormatAllText</c>): the first line beyond
///  its limit, and the lines of the body (after the empty second line, if required) beyond theirs.
/// </summary>
internal sealed class CommitMessageColorizer(int maxFirstLineLength, int maxLineLength, bool secondLineMustBeEmpty, IBrush brush) : DocumentColorizingTransformer
{
    protected override void ColorizeLine(DocumentLine line)
    {
        int limit = line.LineNumber == 1 ? maxFirstLineLength
            : line.LineNumber >= (secondLineMustBeEmpty ? 3 : 2) ? maxLineLength
            : 0;
        if (limit <= 0 || line.Length <= limit)
        {
            return;
        }

        ChangeLinePart(line.Offset + limit, line.EndOffset, element => element.TextRunProperties.SetForegroundBrush(brush));
    }
}
