using System.Globalization;
using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using GitExtensions.Extensibility.Git;

namespace GitUI.Avalonia.Controls.Blame;

/// <summary>
///  The author gutter of the blame (the author viewer of <c>BlameControl</c> with its <c>BlameAuthorMargin</c>): the age of
///  each line as a colored bar, and the author line where the commit changes.
/// </summary>
internal sealed class BlameMargin : AbstractMargin
{
    private const double AgeBarWidth = 4;
    private const double Padding = 6;
    private const double TextGap = 4;
    private const double MaxGutterWidth = 480;

    private IReadOnlyList<string?> _authorLines = [];
    private IReadOnlyList<int> _ageBuckets = [];
    private double _textWidth;

    /// <summary>The colors of the age buckets, oldest first.</summary>
    public IReadOnlyList<IBrush> AgeBrushes { get; set; } = [];

    /// <summary>The background of the lines of the highlighted commit.</summary>
    public IBrush? HighlightBrush { get; set; }

    public IBrush SeparatorBrush { get; set; } = Brushes.Gray;

    public IReadOnlyList<GitBlameLine> Lines { get; private set; } = [];

    public GitBlameCommit? HighlightedCommit
    {
        get;
        set
        {
            field = value;
            InvalidateVisual();
        }
    }

    public void Update(IReadOnlyList<GitBlameLine> lines, IReadOnlyList<string?> authorLines, IReadOnlyList<int> ageBuckets)
    {
        Lines = lines;
        _authorLines = authorLines;
        _ageBuckets = ageBuckets;
        _textWidth = authorLines.Count == 0 ? 0 : authorLines.Max(line => line is null ? 0 : Format(line).WidthIncludingTrailingWhitespace);
        InvalidateMeasure();
        InvalidateVisual();
    }

    private double BarWidth => _ageBuckets.Count > 0 ? AgeBarWidth + Padding : 0;

    protected override Size MeasureOverride(Size availableSize)
        => new(_authorLines.Count == 0 ? 0 : Math.Min(MaxGutterWidth, BarWidth + _textWidth + (2 * Padding) + TextGap), 0);

    protected override void OnTextViewChanged(TextView? oldTextView, TextView? newTextView)
    {
        oldTextView?.VisualLinesChanged -= OnVisualLinesChanged;
        newTextView?.VisualLinesChanged += OnVisualLinesChanged;
        base.OnTextViewChanged(oldTextView, newTextView);
    }

    private void OnVisualLinesChanged(object? sender, EventArgs e) => InvalidateVisual();

    public override void Render(DrawingContext context)
    {
        Rect bounds = new(Bounds.Size);
        context.FillRectangle(Brushes.Transparent, bounds);
        if (_authorLines.Count > 0)
        {
            // The separator of the author lines and the text (the splitter of the WinForms viewers).
            context.DrawLine(new Pen(SeparatorBrush), new Point(bounds.Width - TextGap - 0.5, 0), new Point(bounds.Width - TextGap - 0.5, bounds.Height));
        }

        if (TextView is not { VisualLinesValid: true } textView)
        {
            return;
        }

        using DrawingContext.PushedState clip = context.PushClip(bounds);
        foreach (VisualLine visualLine in textView.VisualLines)
        {
            int index = visualLine.FirstDocumentLine.LineNumber - 1;
            if (index >= _authorLines.Count)
            {
                continue;
            }

            double top = visualLine.VisualTop - textView.VerticalOffset;
            if (HighlightBrush is not null && HighlightedCommit is not null && index < Lines.Count && ReferenceEquals(Lines[index].Commit, HighlightedCommit))
            {
                context.FillRectangle(HighlightBrush, new Rect(0, top, bounds.Width, visualLine.Height));
            }

            if (index < _ageBuckets.Count && _ageBuckets[index] < AgeBrushes.Count)
            {
                context.FillRectangle(AgeBrushes[_ageBuckets[index]], new Rect(0, top, AgeBarWidth, visualLine.Height));
            }

            if (_authorLines[index] is { } authorLine)
            {
                double y = visualLine.GetTextLineVisualYPosition(visualLine.TextLines[0], VisualYPosition.TextTop) - textView.VerticalOffset;
                context.DrawText(Format(authorLine), new Point(BarWidth + Padding, y));
            }
        }
    }

    /// <summary>The line (1-based) at the vertical position, or 0.</summary>
    public int GetLineAt(double y)
    {
        if (TextView is not { VisualLinesValid: true } textView)
        {
            return 0;
        }

        VisualLine? visualLine = textView.GetVisualLineFromVisualTop(y + textView.VerticalOffset);
        return visualLine?.FirstDocumentLine.LineNumber ?? 0;
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

/// <summary>Highlights the lines of the commit under the mouse in the file (as <c>BlameControl.HighlightLinesForCommit</c>).</summary>
internal sealed class BlameHighlightRenderer : IBackgroundRenderer
{
    public IBrush? Brush { get; set; }

    public IReadOnlyList<GitBlameLine> Lines { get; set; } = [];

    public GitBlameCommit? HighlightedCommit { get; set; }

    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (Brush is null || HighlightedCommit is null)
        {
            return;
        }

        foreach (VisualLine visualLine in textView.VisualLines)
        {
            int index = visualLine.FirstDocumentLine.LineNumber - 1;
            if (index < Lines.Count && ReferenceEquals(Lines[index].Commit, HighlightedCommit))
            {
                double top = visualLine.VisualTop - textView.VerticalOffset;
                drawingContext.FillRectangle(Brush, new Rect(0, top, textView.Bounds.Width, visualLine.Height));
            }
        }
    }
}
