using Avalonia;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using GitUI.Presentation.Editor;

namespace GitUI.Avalonia.Editor;

/// <summary>
///  Colors the added, removed and header lines of a diff without git's colors, as the WinForms <c>DiffHighlightService</c>
///  does (<c>HighlightAddedAndDeletedLines</c>).
/// </summary>
internal sealed class DiffBackgroundRenderer(IBrush? addedBrush, IBrush? removedBrush, IBrush? headerBrush) : IBackgroundRenderer
{
    private IReadOnlyDictionary<int, DiffLine> _linesByNumber = new Dictionary<int, DiffLine>();

    public KnownLayer Layer => KnownLayer.Background;

    public IReadOnlyList<DiffLine> Lines
    {
        set => _linesByNumber = value.ToDictionary(line => line.LineNumInDiff);
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        foreach (VisualLine visualLine in textView.VisualLines)
        {
            if (!_linesByNumber.TryGetValue(visualLine.FirstDocumentLine.LineNumber, out DiffLine? line)
                || GetBrush(line.Kind) is not { } brush)
            {
                continue;
            }

            double top = visualLine.VisualTop - textView.VerticalOffset;
            drawingContext.FillRectangle(brush, new Rect(0, top, textView.Bounds.Width, visualLine.Height));
        }
    }

    private IBrush? GetBrush(DiffLineKind kind) => kind switch
    {
        DiffLineKind.Plus => addedBrush,
        DiffLineKind.Minus => removedBrush,
        DiffLineKind.Header => headerBrush,
        _ => null,
    };
}

/// <summary>
///  Draws the anchors of insertions and deletions (as ICSharpCode's <c>InterChar</c> markers of <c>DiffHighlightService</c>):
///  a bar between two characters, above the colors of the text.
/// </summary>
internal sealed class DiffAnchorRenderer(IBrush? addedBrush, IBrush? removedBrush) : IBackgroundRenderer
{
    private const double AnchorWidth = 2;

    private InlineDiffMarker[] _anchors = [];

    // Above the backgrounds of the text elements (drawn by the text view itself), below the text.
    public KnownLayer Layer => KnownLayer.Selection;

    public IReadOnlyList<InlineDiffMarker> Markers
    {
        set => _anchors = [.. value.Where(m => m.Kind is InlineDiffMarkerKind.InsertionAnchor or InlineDiffMarkerKind.DeletionAnchor).OrderBy(m => m.Offset)];
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        TextDocument? document = textView.Document;
        if (document is null || _anchors.Length == 0 || textView.VisualLines.Count == 0)
        {
            return;
        }

        int visibleStart = textView.VisualLines[0].FirstDocumentLine.Offset;
        int visibleEnd = textView.VisualLines[^1].LastDocumentLine.EndOffset;
        foreach (InlineDiffMarker anchor in _anchors)
        {
            if (anchor.Offset < visibleStart || anchor.Offset > visibleEnd || anchor.Offset > document.TextLength)
            {
                continue;
            }

            IBrush? brush = anchor.Kind == InlineDiffMarkerKind.InsertionAnchor ? addedBrush : removedBrush;
            if (brush is null)
            {
                continue;
            }

            TextViewPosition position = new(document.GetLocation(anchor.Offset));
            Point top = textView.GetVisualPosition(position, VisualYPosition.LineTop) - textView.ScrollOffset;
            Point bottom = textView.GetVisualPosition(position, VisualYPosition.LineBottom) - textView.ScrollOffset;
            drawingContext.FillRectangle(brush, new Rect(top.X - (AnchorWidth / 2), top.Y, AnchorWidth, bottom.Y - top.Y));
        }
    }
}
