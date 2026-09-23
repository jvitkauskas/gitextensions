using Avalonia;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using GitUI.Presentation.Editor;

namespace GitUI.Avalonia.Editor;

/// <summary>The brushes of a diff without git's colors.</summary>
internal sealed record DiffBrushes(
    IBrush? Added,
    IBrush? Removed,
    IBrush? Header,
    IBrush? DimmedAdded,
    IBrush? DimmedRemoved,
    IBrush? AddedAnchor,
    IBrush? RemovedAnchor);

/// <summary>
///  Colors the added, removed and header lines of a diff and dims the identical parts of matching removed and added lines,
///  as the WinForms <c>DiffHighlightService</c> does for a patch without git's colors (<c>HighlightAddedAndDeletedLines</c>
///  and <c>AddInlineDifferenceMarkers</c>).
/// </summary>
internal sealed class DiffBackgroundRenderer(DiffBrushes brushes) : IBackgroundRenderer
{
    private const double AnchorWidth = 2;

    private IReadOnlyDictionary<int, DiffLine> _linesByNumber = new Dictionary<int, DiffLine>();
    private InlineDiffMarker[] _markers = [];

    public KnownLayer Layer => KnownLayer.Background;

    public IReadOnlyList<DiffLine> Lines
    {
        set => _linesByNumber = value.ToDictionary(line => line.LineNumInDiff);
    }

    public IReadOnlyList<InlineDiffMarker> Markers
    {
        set => _markers = [.. value.OrderBy(marker => marker.Offset)];
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (textView.VisualLines.Count == 0)
        {
            return;
        }

        foreach (VisualLine visualLine in textView.VisualLines)
        {
            if (!_linesByNumber.TryGetValue(visualLine.FirstDocumentLine.LineNumber, out DiffLine? line)
                || GetLineBrush(line.Kind) is not { } brush)
            {
                continue;
            }

            double top = visualLine.VisualTop - textView.VerticalOffset;
            drawingContext.FillRectangle(brush, new Rect(0, top, textView.Bounds.Width, visualLine.Height));
        }

        DrawMarkers(textView, drawingContext);
    }

    private void DrawMarkers(TextView textView, DrawingContext drawingContext)
    {
        TextDocument? document = textView.Document;
        if (document is null || _markers.Length == 0)
        {
            return;
        }

        int visibleStart = textView.VisualLines[0].FirstDocumentLine.Offset;
        int visibleEnd = textView.VisualLines[^1].LastDocumentLine.EndOffset;
        for (int i = FindFirstMarkerAtOrAfter(visibleStart); i < _markers.Length && _markers[i].Offset <= visibleEnd; i++)
        {
            InlineDiffMarker marker = _markers[i];
            if (marker.Offset + marker.Length > document.TextLength || GetMarkerBrush(marker.Kind) is not { } brush)
            {
                continue;
            }

            if (marker.Length == 0)
            {
                // As an ICSharpCode InterChar marker: a bar between two characters.
                TextViewPosition position = new(document.GetLocation(marker.Offset));
                Point lineTop = textView.GetVisualPosition(position, VisualYPosition.LineTop) - textView.ScrollOffset;
                Point lineBottom = textView.GetVisualPosition(position, VisualYPosition.LineBottom) - textView.ScrollOffset;
                drawingContext.FillRectangle(brush, new Rect(lineTop.X - (AnchorWidth / 2), lineTop.Y, AnchorWidth, lineBottom.Y - lineTop.Y));
                continue;
            }

            TextSegment segment = new() { StartOffset = marker.Offset, Length = marker.Length };
            foreach (Rect rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
            {
                drawingContext.FillRectangle(brush, rect);
            }
        }
    }

    private int FindFirstMarkerAtOrAfter(int offset)
    {
        int low = 0;
        int high = _markers.Length;
        while (low < high)
        {
            int middle = (low + high) / 2;
            if (_markers[middle].Offset < offset)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }

    private IBrush? GetLineBrush(DiffLineKind kind) => kind switch
    {
        DiffLineKind.Plus => brushes.Added,
        DiffLineKind.Minus => brushes.Removed,
        DiffLineKind.Header => brushes.Header,
        _ => null,
    };

    private IBrush? GetMarkerBrush(InlineDiffMarkerKind kind) => kind switch
    {
        InlineDiffMarkerKind.DimmedAdded => brushes.DimmedAdded,
        InlineDiffMarkerKind.DimmedRemoved => brushes.DimmedRemoved,
        InlineDiffMarkerKind.InsertionAnchor => brushes.AddedAnchor,
        InlineDiffMarkerKind.DeletionAnchor => brushes.RemovedAnchor,
        _ => null,
    };
}
