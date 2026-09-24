using System.Globalization;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaBrushes = Avalonia.Media.Brushes;
using AvaloniaColor = Avalonia.Media.Color;
using AvaloniaCursor = Avalonia.Input.Cursor;
using AvaloniaFontFamily = Avalonia.Media.FontFamily;
using AvaloniaPen = Avalonia.Media.Pen;
using AvaloniaPoint = Avalonia.Point;
using AvaloniaSize = Avalonia.Size;

namespace GitExtensions.Plugins.GitImpact;

/// <summary>
///  The impact graph: the port of the drawing of <c>ImpactControl</c> (<c>UpdatePathsAndLabels</c>, <c>OnPaint</c>,
///  <c>TrySetAuthorByScreenPosition</c>) with the Avalonia <see cref="DrawingContext"/>. Each week is a column of blocks, one
///  per author, joined to the author's blocks of the neighbouring weeks by curves. The control is as wide as the graph; it is
///  scrolled horizontally by its <c>ScrollViewer</c>.
/// </summary>
public sealed class ImpactGraphView : Avalonia.Controls.Control
{
    public static readonly StyledProperty<ImpactSnapshot?> SnapshotProperty =
        AvaloniaProperty.Register<ImpactGraphView, ImpactSnapshot?>(nameof(Snapshot));

    public static readonly StyledProperty<string> SelectedAuthorProperty =
        AvaloniaProperty.Register<ImpactGraphView, string>(nameof(SelectedAuthor), "", defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    private const double BlockWidth = 60;
    private const double BlockHalfWidth = BlockWidth / 2;
    private const double TransitionWidth = 50;
    private const double TransitionHalfWidth = TransitionWidth / 2;

    // The sizes of the Arial fonts of ImpactControl (10 and 8 points).
    private const double LinesFontSize = 10 * 96 / 72.0;
    private const double WeekFontSize = 8 * 96 / 72.0;

    private static readonly Typeface _typeface = new(new AvaloniaFontFamily("Arial"));

    private GraphLayout? _layout;

    static ImpactGraphView()
    {
        AffectsRender<ImpactGraphView>(SelectedAuthorProperty);
        AffectsMeasure<ImpactGraphView>(SnapshotProperty);
    }

    public ImpactSnapshot? Snapshot
    {
        get => GetValue(SnapshotProperty);
        set => SetValue(SnapshotProperty, value);
    }

    public string SelectedAuthor
    {
        get => GetValue(SelectedAuthorProperty);
        set => SetValue(SelectedAuthorProperty, value);
    }

    /// <summary>The color of an author, as <c>ImpactControl</c> picks it (from the hash of the name), darkened on dark themes.</summary>
    public static AvaloniaColor GetAuthorColor(string author, bool isDark)
    {
        uint argb = unchecked((uint)(author.GetHashCode() | 0xFF000000));
        AvaloniaColor color = AvaloniaColor.FromUInt32(argb);

        // As AdaptBackColor, keep the blocks darker than the white labels on dark themes.
        return isDark ? AvaloniaColor.FromRgb((byte)(color.R * 0.6), (byte)(color.G * 0.6), (byte)(color.B * 0.6)) : color;
    }

    /// <summary>As <c>TrySetAuthorByScreenPosition</c>: the author whose area is at <paramref name="point"/>, the topmost first.</summary>
    public string? HitTestAuthor(AvaloniaPoint point)
    {
        if (_layout is not { } layout)
        {
            return null;
        }

        for (int i = layout.AuthorStack.Count - 1; i >= 0; i--)
        {
            string author = layout.AuthorStack[i];
            if (layout.Paths.TryGetValue(author, out Geometry? path) && path.FillContains(point))
            {
                return author;
            }
        }

        return null;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SnapshotProperty || change.Property == BoundsProperty)
        {
            _layout = null;
            InvalidateVisual();
        }

        if (change.Property == SnapshotProperty)
        {
            // As OnPaint: the wait cursor until there are results.
            Cursor = Snapshot is { Weeks.Count: > 0 } ? AvaloniaCursor.Default : new AvaloniaCursor(StandardCursorType.Wait);
        }
    }

    /// <summary>As <c>GetGraphWidth</c>.</summary>
    protected override AvaloniaSize MeasureOverride(AvaloniaSize availableSize)
    {
        int weeks = Snapshot?.Weeks.Count ?? 0;
        return new AvaloniaSize(Math.Max(0, (weeks * (BlockWidth + TransitionWidth)) - TransitionWidth), 0);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        // As OnPaint: the window background (also making the control hit-testable).
        context.FillRectangle(AvaloniaBrushes.Transparent, new Rect(Bounds.Size));

        if (Snapshot is not { Weeks.Count: > 0 } || Bounds.Height <= 0)
        {
            return;
        }

        GraphLayout layout = _layout ??= CreateLayout(Snapshot, Bounds.Height);
        bool isDark = ActualThemeVariant == ThemeVariant.Dark;

        // The paths in the order of the author stack, the selected author on top.
        foreach (string author in layout.AuthorStack)
        {
            if (author != SelectedAuthor)
            {
                DrawAuthorContribution(author);
            }
        }

        DrawAuthorContribution(SelectedAuthor);
        if (layout.Paths.TryGetValue(SelectedAuthor, out Geometry? selectedPath))
        {
            // SystemColors.WindowText.
            context.DrawGeometry(null, new AvaloniaPen(isDark ? AvaloniaBrushes.White : AvaloniaBrushes.Black, 2), selectedPath);
        }

        foreach (string author in layout.AuthorStack)
        {
            if (layout.LineLabels.TryGetValue(author, out List<(AvaloniaPoint Center, string Text)>? labels))
            {
                foreach ((AvaloniaPoint center, string text) in labels)
                {
                    FormattedText formatted = CreateText(text, LinesFontSize, AvaloniaBrushes.White);
                    context.DrawText(formatted, new AvaloniaPoint(center.X - (formatted.Width / 2), center.Y - (formatted.Height / 2)));
                }
            }
        }

        foreach ((AvaloniaPoint point, string date) in layout.WeekLabels)
        {
            FormattedText formatted = CreateText(date, WeekFontSize, AvaloniaBrushes.Gray);
            context.DrawText(formatted, new AvaloniaPoint(point.X - (formatted.Width / 2), point.Y + (formatted.Height / 2)));
        }

        void DrawAuthorContribution(string author)
        {
            if (layout.Paths.TryGetValue(author, out Geometry? path))
            {
                context.DrawGeometry(new SolidColorBrush(GetAuthorColor(author, isDark)), null, path);
            }
        }
    }

    private static FormattedText CreateText(string text, double size, IBrush brush)
        => new(text, CultureInfo.CurrentCulture, Avalonia.Media.FlowDirection.LeftToRight, _typeface, size, brush);

    /// <summary>As <c>UpdatePathsAndLabels</c>.</summary>
    private static GraphLayout CreateLayout(ImpactSnapshot snapshot, double height)
    {
        double maxHeight = 0;
        double x = 0;
        Dictionary<string, List<(Rect Rect, int ChangeCount)>> authorPoints = [];
        List<(AvaloniaPoint Point, string Date)> weekLabels = [];

        foreach (ImpactWeek week in snapshot.Weeks)
        {
            double y = 0;
            foreach (ImpactBlock block in week.Blocks)
            {
                int changedLines = Math.Max(1, block.Data.ChangedLines);
                double blockHeight = Math.Max(1, (int)Math.Round(Math.Pow(Math.Log(changedLines), 1.5) * 4));
                if (!authorPoints.TryGetValue(block.Author, out List<(Rect, int)>? rects))
                {
                    authorPoints.Add(block.Author, rects = []);
                }

                rects.Add((new Rect(x, y, BlockWidth, blockHeight), block.Data.ChangedLines));
                y += blockHeight + 2;
            }

            maxHeight = Math.Max(maxHeight, y);
            weekLabels.Add((new AvaloniaPoint(x + BlockHalfWidth, y), week.Week.ToShortDateString()));
            x += BlockWidth + TransitionWidth;
        }

        double heightFactor = maxHeight > 0 ? 0.9 * height / maxHeight : 1.0;
        for (int i = 0; i < weekLabels.Count; i++)
        {
            (AvaloniaPoint point, string date) = weekLabels[i];
            weekLabels[i] = (new AvaloniaPoint(point.X, point.Y * heightFactor), date);
        }

        Dictionary<string, Geometry> paths = [];
        Dictionary<string, List<(AvaloniaPoint Center, string Text)>> lineLabels = [];
        foreach ((string author, List<(Rect Rect, int ChangeCount)> points) in authorPoints)
        {
            List<Rect> rects = new(points.Count);
            List<(AvaloniaPoint, string)> labels = [];
            foreach ((Rect unscaled, int changeCount) in points)
            {
                Rect rect = new(unscaled.Left, (int)(unscaled.Top * heightFactor), unscaled.Width, Math.Max(1, (int)(unscaled.Height * heightFactor)));
                rects.Add(rect);
                if (rect.Height > LinesFontSize * 1.5)
                {
                    labels.Add((new AvaloniaPoint(rect.Left + BlockHalfWidth, rect.Top + (rect.Height / 2)), changeCount.ToString()));
                }
            }

            lineLabels.Add(author, labels);
            paths.Add(author, CreatePath(rects));
        }

        return new GraphLayout(snapshot.AuthorStack, paths, lineLabels, weekLabels);
    }

    /// <summary>The outline of the blocks of an author: the tops left to right, then the bottoms right to left.</summary>
    private static StreamGeometry CreatePath(List<Rect> rects)
    {
        StreamGeometry geometry = new();
        using StreamGeometryContext context = geometry.Open();

        Rect first = rects[0];
        context.BeginFigure(first.BottomLeft, isFilled: true);
        context.LineTo(first.TopLeft);
        for (int i = 0; i < rects.Count; i++)
        {
            Rect rect = rects[i];
            context.LineTo(rect.TopRight);
            if (i < rects.Count - 1)
            {
                Rect next = rects[i + 1];
                context.CubicBezierTo(
                    new AvaloniaPoint(rect.Right + TransitionHalfWidth, rect.Top),
                    new AvaloniaPoint(rect.Right + TransitionHalfWidth, next.Top),
                    next.TopLeft);
            }
        }

        Rect last = rects[^1];
        context.LineTo(last.BottomRight);
        for (int i = rects.Count - 1; i >= 0; i--)
        {
            Rect rect = rects[i];
            context.LineTo(rect.BottomLeft);
            if (i > 0)
            {
                Rect previous = rects[i - 1];
                context.CubicBezierTo(
                    new AvaloniaPoint(rect.Left - TransitionHalfWidth, rect.Bottom),
                    new AvaloniaPoint(rect.Left - TransitionHalfWidth, previous.Bottom),
                    previous.BottomRight);
            }
        }

        context.EndFigure(isClosed: true);
        return geometry;
    }

    private sealed record GraphLayout(
        IReadOnlyList<string> AuthorStack,
        Dictionary<string, Geometry> Paths,
        Dictionary<string, List<(AvaloniaPoint Center, string Text)>> LineLabels,
        List<(AvaloniaPoint Point, string Date)> WeekLabels);
}
