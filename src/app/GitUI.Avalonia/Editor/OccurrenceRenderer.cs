using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace GitUI.Avalonia.Editor;

/// <summary>
///  The occurrences of the selected text, on the color <c>AppColor.HighlightAllOccurences</c> (as the markers of
///  <c>FileViewerInternal.SelectionManagerSelectionChanged</c>), which next / previous occurrence go to.
/// </summary>
internal sealed class OccurrenceRenderer : IBackgroundRenderer
{
    /// <summary>The offsets of the occurrences, in order.</summary>
    public IReadOnlyList<int> Offsets { get; private set; } = [];

    /// <summary>The length of the selected text that is searched for.</summary>
    public int Length { get; private set; }

    public IBrush? Brush { get; set; }

    public KnownLayer Layer => KnownLayer.Selection;

    /// <summary>
    ///  As <c>GetTextMarkersMatchingWord</c>: every occurrence of <paramref name="word"/>, ignoring the case (also overlapping
    ///  ones); none for a blank selection.
    /// </summary>
    public static IReadOnlyList<int> FindOccurrences(string text, string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return [];
        }

        List<int> offsets = [];
        int index = -1;
        while (index < text.Length - 1 && (index = text.IndexOf(word, index + 1, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            offsets.Add(index);
        }

        return offsets;
    }

    /// <returns>Whether the occurrences changed.</returns>
    public bool Update(string text, string word)
    {
        IReadOnlyList<int> offsets = FindOccurrences(text, word);
        if (offsets.Count == 0 && Offsets.Count == 0)
        {
            return false;
        }

        Offsets = offsets;
        Length = word.Length;
        return true;
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (Offsets.Count == 0 || Brush is null || textView.VisualLines.Count == 0)
        {
            return;
        }

        int viewStart = textView.VisualLines[0].FirstDocumentLine.Offset;
        int viewEnd = textView.VisualLines[^1].LastDocumentLine.EndOffset;
        int documentLength = textView.Document.TextLength;
        foreach (int offset in Offsets)
        {
            if (offset + Length < viewStart)
            {
                continue;
            }

            if (offset > viewEnd)
            {
                break;
            }

            if (offset + Length > documentLength)
            {
                break;
            }

            TextSegment segment = new() { StartOffset = offset, Length = Length };
            foreach (Rect rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
            {
                drawingContext.FillRectangle(Brush, rect);
            }
        }
    }
}
