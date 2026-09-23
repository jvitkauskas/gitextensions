using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Rendering;
using GitUI.Presentation.Editor;

namespace GitUI.Avalonia.Editor;

/// <summary>
///  Colors the added, removed and header lines of a diff, as the WinForms <c>DiffHighlightService</c> does for a patch
///  without git's colors (<c>HighlightAddedAndDeletedLines</c>).
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
