using System.Globalization;
using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using GitUI.Presentation.Editor;

namespace GitUI.Avalonia.Editor;

/// <summary>The backgrounds of the line numbers of the added, removed and header lines (and of the grep matches).</summary>
internal sealed record DiffMarginBrushes(IBrush? Added, IBrush? Removed, IBrush? Header);

/// <summary>
///  The line numbers of a diff in the old and the new file, in two columns (the WinForms <c>DiffViewerLineNumberControl</c>),
///  on the background of the kind of the line (as its <c>Paint</c>).
/// </summary>
internal sealed class DiffLineNumberMargin : AbstractMargin
{
    private const double Padding = 4;

    private IReadOnlyDictionary<int, DiffLine> _linesByNumber = new Dictionary<int, DiffLine>();
    private int _maxDigits = 1;
    private bool _showLeftColumn = true;

    /// <summary>The backgrounds of the kinds of lines.</summary>
    public DiffMarginBrushes Brushes { get; set; } = new(null, null, null);

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

    /// <summary>Whether the numbers of the old file are shown (as the <c>showLeftColumn</c> of <c>DisplayLineNum</c>).</summary>
    public bool ShowLeftColumn
    {
        get => _showLeftColumn;
        set
        {
            _showLeftColumn = value;
            InvalidateMeasure();
            InvalidateVisual();
        }
    }

    private int ColumnCount => _showLeftColumn ? 2 : 1;

    protected override Size MeasureOverride(Size availableSize)
        => new((ColumnCount * ColumnWidth) + ((ColumnCount + 1) * Padding), 0);

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
        double width = Bounds.Width;
        foreach (VisualLine visualLine in textView.VisualLines)
        {
            if (!_linesByNumber.TryGetValue(visualLine.FirstDocumentLine.LineNumber, out DiffLine? line))
            {
                continue;
            }

            double top = visualLine.VisualTop - textView.VerticalOffset;
            DrawBackground(line.Kind, top, visualLine.Height);
            double y = visualLine.GetTextLineVisualYPosition(visualLine.TextLines[0], VisualYPosition.TextTop) - textView.VerticalOffset;
            if (_showLeftColumn)
            {
                DrawNumber(line.LeftLineNumber, Padding + columnWidth);
            }

            DrawNumber(line.RightLineNumber, (ColumnCount * Padding) + (ColumnCount * columnWidth));

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

        return;

        // As DiffViewerLineNumberControl.Paint: the lines of a git word diff and of difftastic color the side they change.
        void DrawBackground(DiffLineKind kind, double top, double height)
        {
            switch (kind)
            {
                case DiffLineKind.MinusPlus or DiffLineKind.MinusLeft or DiffLineKind.PlusRight:
                    if (kind is not DiffLineKind.PlusRight && Brushes.Removed is { } removed)
                    {
                        context.FillRectangle(removed, new Rect(0, top, width / 2, height));
                    }

                    if (kind is not DiffLineKind.MinusLeft && Brushes.Added is { } added)
                    {
                        context.FillRectangle(added, new Rect(width / 2, top, width - (width / 2), height));
                    }

                    break;
                case DiffLineKind.Plus or DiffLineKind.Minus or DiffLineKind.Header or DiffLineKind.Grep:
                    IBrush? brush = kind switch
                    {
                        DiffLineKind.Plus => Brushes.Added,
                        DiffLineKind.Header => Brushes.Header,
                        _ => Brushes.Removed,
                    };
                    if (brush is not null)
                    {
                        context.FillRectangle(brush, new Rect(0, top, width, height));
                    }

                    break;
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
            GetValue(TextElement.ForegroundProperty) ?? global::Avalonia.Media.Brushes.Gray);
}
