using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using GitUI.Presentation.Editor;
using DrawingColor = System.Drawing.Color;

namespace GitUI.Avalonia.Editor;

/// <summary>The colors of the in-line differences: a dimmed background, or a dimmed text on the editor background.</summary>
internal sealed record InlineDiffBrushes(IBrush? AddedBack, IBrush? RemovedBack, IBrush? AddedFore, IBrush? RemovedFore);

/// <summary>
///  Colors the text of a diff: git's colors (<see cref="GitColoring"/>), then the identical parts of matching removed and
///  added lines (as the WinForms <c>DiffHighlightService</c>, whose in-line markers override git's).
/// </summary>
internal sealed class DiffColorizer : DocumentColorizingTransformer
{
    private readonly Dictionary<DrawingColor, IBrush> _brushes = [];
    private ColoredSegment[] _segments = [];
    private InlineDiffMarker[] _dimmed = [];
    private InlineDiffBrushes _inlineBrushes = new(null, null, null, null);

    public void Update(GitColoring? gitColoring, IReadOnlyList<InlineDiffMarker> markers, InlineDiffBrushes inlineBrushes)
    {
        _segments = [.. gitColoring?.Segments ?? []];
        _dimmed = [.. markers.Where(m => m.Length > 0 && m.Kind is InlineDiffMarkerKind.DimmedAdded or InlineDiffMarkerKind.DimmedRemoved).OrderBy(m => m.Offset)];
        _inlineBrushes = inlineBrushes;
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        int lineStart = line.Offset;
        int lineEnd = line.EndOffset;
        if (lineEnd <= lineStart)
        {
            return;
        }

        for (int i = FindFirstEndingAfter(_segments, lineStart, s => s.Offset + s.Length); i < _segments.Length && _segments[i].Offset < lineEnd; i++)
        {
            ColoredSegment segment = _segments[i];
            IBrush back = GetBrush(segment.BackColor);
            IBrush? fore = segment.ForeColor is { } foreColor ? GetBrush(foreColor) : null;
            Colorize(segment.Offset, segment.Offset + segment.Length, back, fore);
        }

        for (int i = FindFirstEndingAfter(_dimmed, lineStart, m => m.Offset + m.Length); i < _dimmed.Length && _dimmed[i].Offset < lineEnd; i++)
        {
            InlineDiffMarker marker = _dimmed[i];
            bool isAdded = marker.Kind == InlineDiffMarkerKind.DimmedAdded;
            Colorize(
                marker.Offset,
                marker.Offset + marker.Length,
                isAdded ? _inlineBrushes.AddedBack : _inlineBrushes.RemovedBack,
                isAdded ? _inlineBrushes.AddedFore : _inlineBrushes.RemovedFore);
        }

        return;

        void Colorize(int start, int end, IBrush? back, IBrush? fore)
        {
            start = Math.Max(start, lineStart);
            end = Math.Min(end, lineEnd);
            if (start >= end)
            {
                return;
            }

            ChangeLinePart(start, end, element =>
            {
                if (back is not null)
                {
                    element.BackgroundBrush = back;
                }

                if (fore is not null)
                {
                    element.TextRunProperties.SetForegroundBrush(fore);
                }
            });
        }
    }

    private IBrush GetBrush(DrawingColor color)
    {
        if (!_brushes.TryGetValue(color, out IBrush? brush))
        {
            brush = new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
            _brushes[color] = brush;
        }

        return brush;
    }

    /// <summary>The index of the first item (ordered, not overlapping) that ends after the offset.</summary>
    private static int FindFirstEndingAfter<T>(T[] items, int offset, Func<T, int> getEnd)
    {
        int low = 0;
        int high = items.Length;
        while (low < high)
        {
            int middle = (low + high) / 2;
            if (getEnd(items[middle]) <= offset)
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
}
