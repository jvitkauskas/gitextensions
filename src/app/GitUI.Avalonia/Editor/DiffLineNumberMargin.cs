using System.Globalization;
using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using GitUI.Presentation.Editor;

namespace GitUI.Avalonia.Editor;

/// <summary>
///  The line numbers of a diff in the old and the new file, in two columns (the WinForms <c>DiffViewerLineNumberControl</c>).
/// </summary>
internal sealed class DiffLineNumberMargin : AbstractMargin
{
    private const double Padding = 4;

    private IReadOnlyDictionary<int, DiffLine> _linesByNumber = new Dictionary<int, DiffLine>();
    private int _maxDigits = 1;

    public IReadOnlyList<DiffLine> Lines
    {
        set
        {
            _linesByNumber = value.ToDictionary(line => line.LineNumInDiff);
            int maxLineNumber = value.Count == 0 ? 0 : value.Max(line => Math.Max(line.LeftLineNumber, line.RightLineNumber));
            _maxDigits = Math.Max(1, maxLineNumber.ToString(CultureInfo.InvariantCulture).Length);
            InvalidateMeasure();
            InvalidateVisual();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
        => new((2 * ColumnWidth) + (3 * Padding), 0);

    private double ColumnWidth => _maxDigits * Format("0").Width;

    protected override void OnTextViewChanged(TextView? oldTextView, TextView? newTextView)
    {
        oldTextView?.VisualLinesChanged -= OnVisualLinesChanged;
        newTextView?.VisualLinesChanged += OnVisualLinesChanged;
        base.OnTextViewChanged(oldTextView, newTextView);
    }

    private void OnVisualLinesChanged(object? sender, EventArgs e) => InvalidateVisual();

    public override void Render(DrawingContext context)
    {
        if (TextView is not { VisualLinesValid: true } textView)
        {
            return;
        }

        double columnWidth = ColumnWidth;
        foreach (VisualLine visualLine in textView.VisualLines)
        {
            if (!_linesByNumber.TryGetValue(visualLine.FirstDocumentLine.LineNumber, out DiffLine? line))
            {
                continue;
            }

            double y = visualLine.GetTextLineVisualYPosition(visualLine.TextLines[0], VisualYPosition.TextTop) - textView.VerticalOffset;
            DrawNumber(line.LeftLineNumber, Padding + columnWidth);
            DrawNumber(line.RightLineNumber, (2 * Padding) + (2 * columnWidth));

            void DrawNumber(int number, double right)
            {
                if (number == DiffLine.NotApplicable)
                {
                    return;
                }

                FormattedText text = Format(number.ToString(CultureInfo.CurrentCulture));
                context.DrawText(text, new Point(right - text.Width, y));
            }
        }
    }

    private FormattedText Format(string text)
        => new(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(GetValue(TextElement.FontFamilyProperty)),
            GetValue(TextElement.FontSizeProperty),
            GetValue(TextElement.ForegroundProperty) ?? Brushes.Gray);
}
