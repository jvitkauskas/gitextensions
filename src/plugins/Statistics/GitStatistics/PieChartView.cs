using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaColor = Avalonia.Media.Color;
using AvaloniaColors = Avalonia.Media.Colors;
using AvaloniaPen = Avalonia.Media.Pen;
using AvaloniaPoint = Avalonia.Point;
using AvaloniaSize = Avalonia.Size;

namespace GitExtensions.Plugins.GitStatistics;

/// <summary>
///  A tilted pie chart with a rim, drawn with the Avalonia <see cref="DrawingContext"/>: the port of <c>PieChartControl</c> and
///  <c>PieChart3D</c> as <c>FormGitStatistics</c> sets them up (<c>SetPieStyle</c>: the decent colors, edges darker than
///  the surface, a slice height of 20 %). The slice under the mouse shows its tooltip.
/// </summary>
/// <remarks>Deliberate difference: no gradual shadow, and the slices are not lifted when hovered.</remarks>
public sealed class PieChartView : Avalonia.Controls.Control
{
    public static readonly StyledProperty<IReadOnlyList<PieSlice>?> SlicesProperty =
        AvaloniaProperty.Register<PieChartView, IReadOnlyList<PieSlice>?>(nameof(Slices));

    /// <summary>The margin around the chart (<c>SetLeftMargin(10)</c> and so on).</summary>
    private const double ChartMargin = 10;

    /// <summary>The height of the ellipse relative to its width (the tilt of the pie).</summary>
    private const double EllipseRatio = 0.55;

    /// <summary>The height of the rim relative to the width (<c>SetSliceRelativeHeight(0.20f)</c> of the tilted height).</summary>
    private const double RimRatio = 0.20 * EllipseRatio;

    /// <summary><c>FormGitStatistics.DecentColors</c>.</summary>
    private static readonly AvaloniaColor[] _decentColors =
    [
        AvaloniaColors.Red,
        AvaloniaColors.Yellow,
        AvaloniaColors.DodgerBlue,
        AvaloniaColors.LightGreen,
        AvaloniaColors.Coral,
        AvaloniaColors.Goldenrod,
        AvaloniaColors.YellowGreen,
        AvaloniaColors.MediumPurple,
        AvaloniaColors.LightGray,
        AvaloniaColors.Brown,
        AvaloniaColors.Pink,
        AvaloniaColors.DarkBlue,
        AvaloniaColors.Purple,
    ];

    static PieChartView()
    {
        AffectsRender<PieChartView>(SlicesProperty);
    }

    public IReadOnlyList<PieSlice>? Slices
    {
        get => GetValue(SlicesProperty);
        set => SetValue(SlicesProperty, value);
    }

    /// <summary>The index of the slice at <paramref name="point"/> (on the top of the pie), or -1.</summary>
    public int HitTestSlice(AvaloniaPoint point)
    {
        if (GetLayout() is not { } layout)
        {
            return -1;
        }

        double dx = (point.X - layout.Center.X) / layout.RadiusX;
        double dy = (point.Y - layout.Center.Y) / layout.RadiusY;
        if ((dx * dx) + (dy * dy) > 1)
        {
            return -1;
        }

        double angle = Math.Atan2(dy, dx);
        if (angle < 0)
        {
            angle += 2 * Math.PI;
        }

        for (int i = 0; i < layout.Angles.Count; i++)
        {
            (double start, double end) = layout.Angles[i];
            if (angle >= start && angle < end)
            {
                return i;
            }
        }

        return -1;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        // A transparent background makes the whole control hit-testable (for the tooltips).
        context.FillRectangle(Avalonia.Media.Brushes.Transparent, new Rect(Bounds.Size));

        if (GetLayout() is not { } layout)
        {
            return;
        }

        bool isDark = ActualThemeVariant == ThemeVariant.Dark;

        // The rim first: only its front half (angles 0 to pi, y growing downwards) is visible.
        for (int i = 0; i < layout.Angles.Count; i++)
        {
            (double start, double end) = layout.Angles[i];
            double visibleStart = Math.Max(start, 0);
            double visibleEnd = Math.Min(end, Math.PI);
            if (visibleEnd <= visibleStart)
            {
                continue;
            }

            AvaloniaColor color = GetColor(i, isDark);
            context.DrawGeometry(new SolidColorBrush(Darken(color, 0.7)), new AvaloniaPen(new SolidColorBrush(Darken(color, 0.5)), 1), CreateRim(layout, visibleStart, visibleEnd));
        }

        // Then the top of the slices.
        for (int i = 0; i < layout.Angles.Count; i++)
        {
            (double start, double end) = layout.Angles[i];
            AvaloniaColor color = GetColor(i, isDark);
            SolidColorBrush fill = new(color);
            AvaloniaPen edge = new(new SolidColorBrush(Darken(color, 0.6)), 1);
            if (end - start >= (2 * Math.PI) - 1e-9)
            {
                context.DrawEllipse(fill, edge, layout.Center, layout.RadiusX, layout.RadiusY);
            }
            else
            {
                context.DrawGeometry(fill, edge, CreateSlice(layout, start, end));
            }
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        int slice = HitTestSlice(e.GetPosition(this));
        Avalonia.Controls.ToolTip.SetTip(this, slice >= 0 ? Slices![slice].ToolTip : null);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        Avalonia.Controls.ToolTip.SetTip(this, null);
    }

    protected override AvaloniaSize MeasureOverride(AvaloniaSize availableSize) => new(0, 0);

    /// <summary>As <c>SetPieStyle</c>: the pie is as wide as the smaller side of the control allows.</summary>
    private PieLayout? GetLayout()
    {
        IReadOnlyList<PieSlice>? slices = Slices;
        decimal total = slices?.Sum(slice => Math.Max(0, slice.Value)) ?? 0;
        if (slices is null || total <= 0)
        {
            return null;
        }

        double width = Math.Min(Bounds.Width, Bounds.Height / (EllipseRatio + RimRatio)) - (2 * ChartMargin);
        if (width <= 0)
        {
            return null;
        }

        double height = width * (EllipseRatio + RimRatio);
        double radiusX = width / 2;
        double radiusY = width * EllipseRatio / 2;
        AvaloniaPoint center = new(Bounds.Width / 2, ((Bounds.Height - height) / 2) + radiusY);

        List<(double Start, double End)> angles = new(slices.Count);
        double angle = 0;
        foreach (PieSlice slice in slices)
        {
            double sweep = (double)(Math.Max(0, slice.Value) / total) * 2 * Math.PI;
            angles.Add((angle, angle + sweep));
            angle += sweep;
        }

        return new PieLayout(center, radiusX, radiusY, width * RimRatio, angles);
    }

    private static AvaloniaPoint PointAt(PieLayout layout, double angle, double offsetY = 0)
        => new(layout.Center.X + (layout.RadiusX * Math.Cos(angle)), layout.Center.Y + (layout.RadiusY * Math.Sin(angle)) + offsetY);

    private static StreamGeometry CreateSlice(PieLayout layout, double start, double end)
    {
        StreamGeometry geometry = new();
        using StreamGeometryContext context = geometry.Open();
        context.BeginFigure(layout.Center, isFilled: true);
        context.LineTo(PointAt(layout, start));
        context.ArcTo(PointAt(layout, end), new AvaloniaSize(layout.RadiusX, layout.RadiusY), 0, isLargeArc: end - start > Math.PI, SweepDirection.Clockwise);
        context.EndFigure(isClosed: true);
        return geometry;
    }

    private static StreamGeometry CreateRim(PieLayout layout, double start, double end)
    {
        AvaloniaSize radius = new(layout.RadiusX, layout.RadiusY);
        StreamGeometry geometry = new();
        using StreamGeometryContext context = geometry.Open();
        context.BeginFigure(PointAt(layout, start), isFilled: true);
        context.ArcTo(PointAt(layout, end), radius, 0, isLargeArc: false, SweepDirection.Clockwise);
        context.LineTo(PointAt(layout, end, layout.RimHeight));
        context.ArcTo(PointAt(layout, start, layout.RimHeight), radius, 0, isLargeArc: false, SweepDirection.CounterClockwise);
        context.EndFigure(isClosed: true);
        return geometry;
    }

    /// <summary>The color of a slice, darkened on dark themes (as <c>AdaptBackColor</c> keeps the contrast with the background).</summary>
    private static AvaloniaColor GetColor(int index, bool isDark)
    {
        AvaloniaColor color = _decentColors[index % _decentColors.Length];
        return isDark ? Darken(color, 0.7) : color;
    }

    private static AvaloniaColor Darken(AvaloniaColor color, double factor)
        => AvaloniaColor.FromArgb(color.A, (byte)(color.R * factor), (byte)(color.G * factor), (byte)(color.B * factor));

    private sealed record PieLayout(AvaloniaPoint Center, double RadiusX, double RadiusY, double RimHeight, IReadOnlyList<(double Start, double End)> Angles);
}
