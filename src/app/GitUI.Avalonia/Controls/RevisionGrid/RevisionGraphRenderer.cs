using Avalonia;
using Avalonia.Media;
using GitExtensions.Extensibility.Git;
using GitUI.UserControls.RevisionGrid.Graph;

namespace GitUI.Avalonia.Controls.RevisionGrid;

/// <summary>How non-relative revisions are drawn (the WinForms <c>RevisionGraphDrawStyle</c>).</summary>
public enum RevisionGraphDrawStyle
{
    Normal,
    DrawNonRelativesGray,
    HighlightSelected,
}

/// <summary>
///  Draws a row of the revision graph: the Avalonia port of the WinForms <c>GraphRenderer</c> and <c>SegmentRenderer</c>
///  (docs/avalonia-port/PLAN.md, phase 4). The lane geometry is duplicated from them, so keep it in sync (see the ledger);
///  only the drawing calls differ: device-independent units, <see cref="DrawingContext"/> and Avalonia brushes.
/// </summary>
public sealed class RevisionGraphRenderer
{
    public const double LaneLineWidth = 2;
    public const double LaneWidth = 16;
    public const double NodeDimension = 10;

    private const int _noLane = -10;
    private const int MaxLanes = RevisionGraph.MaxLanes;

    private readonly IReadOnlyList<IBrush> _laneBrushes;
    private readonly IBrush _nonRelativeBrush;
    private readonly IBrush _outlineBrush;

    /// <param name="laneBrushes">The brush of each lane color (<see cref="RevisionGraphLanePalette"/>).</param>
    /// <param name="nonRelativeBrush">The brush of revisions that are not relatives of the selected one.</param>
    /// <param name="outlineBrush">The brush of the outline of the checked out revision.</param>
    public RevisionGraphRenderer(IReadOnlyList<IBrush> laneBrushes, IBrush nonRelativeBrush, IBrush outlineBrush)
    {
        _laneBrushes = laneBrushes.Count > 0 ? laneBrushes : [Brushes.Cyan, Brushes.Magenta, Brushes.Yellow, Brushes.Lime];
        _nonRelativeBrush = nonRelativeBrush;
        _outlineBrush = outlineBrush;
    }

    /// <summary>The width of the graph of <paramref name="laneCount"/> lanes.</summary>
    public static double GetWidth(int laneCount) => Math.Min(laneCount, MaxLanes) * LaneWidth;

    /// <summary>As <c>GraphRenderer.DrawItem</c>, at the origin of <paramref name="context"/>.</summary>
    public void DrawRow(
        DrawingContext context,
        bool mergeGraphLanesHavingCommonParent,
        bool renderGraphWithDiagonals,
        int index,
        double rowHeight,
        Func<int, IRevisionGraphRow?> getSegmentsForRow,
        RevisionGraphDrawStyle revisionGraphDrawStyle,
        ObjectId headId,
        IReadOnlySet<ObjectId>? hoverHighlightedIds = null)
    {
        IRevisionGraphRow? currentRow = getSegmentsForRow(index);
        if (currentRow is null)
        {
            return;
        }

        IRevisionGraphRow? previousRow = getSegmentsForRow(index - 1);
        IRevisionGraphRow? nextRow = getSegmentsForRow(index + 1);

        SegmentPoints p = default;
        p.Center = new Point(0, rowHeight / 2);
        p.Start = new Point(0, p.Center.Y - rowHeight);
        p.End = new Point(0, p.Center.Y + rowHeight);

        LaneInfo? currentRowRevisionLaneInfo = null;

        foreach (RevisionGraphSegment revisionGraphSegment in currentRow.Segments.Reverse()
            .OrderBy(s => s.Child.IsRelative)
            .ThenBy(s => hoverHighlightedIds?.Contains(s.Child.Objectid) is true
                      || hoverHighlightedIds?.Contains(s.Parent.Objectid) is true))
        {
            bool skipSecondarySharedSegments = revisionGraphDrawStyle is not (RevisionGraphDrawStyle.DrawNonRelativesGray or RevisionGraphDrawStyle.HighlightSelected);
            SegmentLanes lanes = GetLanesInfo(revisionGraphSegment, previousRow, currentRow, nextRow, skipSecondarySharedSegments, mergeGraphLanesHavingCommonParent,
                setLaneInfo: li => currentRowRevisionLaneInfo = li);
            if (!lanes.DrawFromStart && !lanes.DrawToEnd)
            {
                continue;
            }

            p.Start = p.Start.WithX((int)((lanes.StartLane + 0.5) * LaneWidth));
            p.Center = p.Center.WithX((int)((lanes.CenterLane + 0.5) * LaneWidth));
            p.End = p.End.WithX((int)((lanes.EndLane + 0.5) * LaneWidth));

            bool? isHoverHighlighted = hoverHighlightedIds?.Contains(revisionGraphSegment.Child.Objectid);
            IBrush laneBrush = GetBrushForLaneInfo(revisionGraphSegment.LaneInfo, revisionGraphSegment.Child.IsRelative, revisionGraphDrawStyle, isHoverHighlighted);
            SegmentRenderer segmentRenderer = new(context, new Pen(laneBrush, LaneLineWidth), new Size(LaneWidth, rowHeight), renderGraphWithDiagonals);

            if (renderGraphWithDiagonals)
            {
                Lazy<DiagonalSegment> previousSegmentInfo = new(() =>
                {
                    SegmentLanes previousLanes = GetLanesInfo(revisionGraphSegment, getSegmentsForRow(index - 2), previousRow!, currentRow, skipSecondarySharedSegments, mergeGraphLanesHavingCommonParent);
                    return GetDiagonalSegmentInfo(previousLanes, mergeGraphLanesHavingCommonParent);
                });
                Lazy<DiagonalSegment> nextSegmentInfo = new(() =>
                {
                    SegmentLanes nextLanes = GetLanesInfo(revisionGraphSegment, currentRow, nextRow!, getSegmentsForRow(index + 2), skipSecondarySharedSegments, mergeGraphLanesHavingCommonParent);
                    return GetDiagonalSegmentInfo(nextLanes, mergeGraphLanesHavingCommonParent);
                });
                DiagonalSegment currentSegmentInfo = GetDiagonalSegmentInfo(lanes, mergeGraphLanesHavingCommonParent);

                DrawSegmentWithDiagonals(ref segmentRenderer, p, previousSegmentInfo, currentSegmentInfo, nextSegmentInfo);
            }
            else
            {
                DrawSegmentCurvy(ref segmentRenderer, p, lanes);
            }
        }

        if (currentRow.GetCurrentRevisionLane() < MaxLanes)
        {
            double centerX = (int)((currentRow.GetCurrentRevisionLane() + 0.5) * LaneWidth);
            Rect nodeRect = new(centerX - (NodeDimension / 2), p.Center.Y - (NodeDimension / 2), NodeDimension, NodeDimension);

            bool square = currentRow.Revision.GitRevision!.Refs.Count > 0;
            bool hasOutline = currentRow.Revision.GitRevision.ObjectId == headId;

            bool? isNodeHoverHighlighted = hoverHighlightedIds?.Contains(currentRow.Revision.Objectid);
            IBrush brush = GetBrushForLaneInfo(currentRowRevisionLaneInfo, currentRow.Revision.IsRelative, revisionGraphDrawStyle, isNodeHoverHighlighted);
            if (square)
            {
                context.DrawRectangle(brush, null, nodeRect);
            }
            else
            {
                context.DrawEllipse(brush, null, nodeRect.Center, NodeDimension / 2, NodeDimension / 2);
            }

            if (hasOutline)
            {
                nodeRect = nodeRect.Inflate(1);
                Pen pen = new(_outlineBrush, 2);
                if (square)
                {
                    context.DrawRectangle(null, pen, nodeRect);
                }
                else
                {
                    context.DrawEllipse(null, pen, nodeRect.Center, nodeRect.Width / 2, nodeRect.Height / 2);
                }
            }
        }
    }

    private static SegmentLanes GetLanesInfo(RevisionGraphSegment revisionGraphSegment,
        IRevisionGraphRow? previousRow,
        IRevisionGraphRow currentRow,
        IRevisionGraphRow? nextRow,
        bool skipSecondarySharedSegments,
        bool mergeGraphLanesHavingCommonParent,
        Action<LaneInfo?>? setLaneInfo = null)
    {
        Lane currentLane = currentRow.GetLaneForSegment(revisionGraphSegment);

        int startLane = _noLane;
        int centerLane = _noLane;
        int endLane = _noLane;

        // Avoid drawing the same curve twice (caused aliasing artifacts, particularly when in different colors)
        if (skipSecondarySharedSegments && currentLane.Sharing == LaneSharing.Entire)
        {
            return new SegmentLanes(startLane, centerLane, endLane, PrimaryEndLane: endLane, IsTheRevisionLane: false, DrawFromStart: false, DrawToEnd: false);
        }

        centerLane = currentLane.Index;
        bool isTheRevisionLane = true;
        if (revisionGraphSegment.Parent == currentRow.Revision)
        {
            // This lane ends here
            startLane = GetLaneForRow(previousRow, revisionGraphSegment);
            setLaneInfo?.Invoke(revisionGraphSegment.LaneInfo);
        }
        else if (revisionGraphSegment.Child == currentRow.Revision)
        {
            // This lane starts here
            endLane = GetLaneForRow(nextRow, revisionGraphSegment);
            setLaneInfo?.Invoke(revisionGraphSegment.LaneInfo);
        }
        else
        {
            // This lane crosses
            startLane = GetLaneForRow(previousRow, revisionGraphSegment);
            endLane = GetLaneForRow(nextRow, revisionGraphSegment);
            isTheRevisionLane = false;
        }

        int primaryEndLane = endLane;
        switch (currentLane.Sharing)
        {
            case LaneSharing.DifferentStart:
                if (mergeGraphLanesHavingCommonParent)
                {
                    if (skipSecondarySharedSegments)
                    {
                        endLane = _noLane;
                    }
                }
                else if (endLane != _noLane)
                {
                    throw new Exception($"{currentRow.Revision.Objectid.ToShortString()}: lane {centerLane} has DifferentStart but has EndLane {endLane} (StartLane {startLane})");
                }

                break;

            case LaneSharing.DifferentEnd:
                if (startLane != _noLane)
                {
                    throw new Exception($"{currentRow.Revision.Objectid.ToShortString()}: lane {centerLane} has DifferentEnd but has StartLane {startLane} (EndLane {endLane})");
                }

                break;
        }

        return new SegmentLanes(startLane, centerLane, endLane, primaryEndLane, isTheRevisionLane,
            DrawFromStart: startLane >= 0 && centerLane >= 0 && (startLane <= MaxLanes || centerLane <= MaxLanes),
            DrawToEnd: endLane >= 0 && centerLane >= 0 && (endLane <= MaxLanes || centerLane <= MaxLanes));
    }

    private static DiagonalSegment GetDiagonalSegmentInfo(SegmentLanes currentLanes, bool mergeGraphLanesHavingCommonParent)
    {
        bool drawFromStart = currentLanes.DrawFromStart;
        bool drawToEnd = currentLanes.DrawToEnd;
        bool isTheRevisionLane = currentLanes.IsTheRevisionLane;

        int startShift = currentLanes.CenterLane - currentLanes.StartLane;
        int endShift = currentLanes.EndLane - currentLanes.CenterLane;
        bool startIsDiagonal = Math.Abs(startShift) == 1;
        bool endIsDiagonal = Math.Abs(endShift) == 1;
        bool isBowOfDiagonals = startIsDiagonal && endIsDiagonal && -Math.Sign(startShift) == Math.Sign(endShift);
        int bowOffset = (int)LaneWidth / 6;
        int junctionBowOffset = mergeGraphLanesHavingCommonParent ? (int)LaneLineWidth : bowOffset;
        int horizontalOffset = isBowOfDiagonals ? -Math.Sign(startShift) * junctionBowOffset : 0;

        // Go perpendicularly through the center in order to avoid crossing independend nodes
        bool drawCenterToStartPerpendicularly = drawFromStart && (startShift == 0 || (!startIsDiagonal && !isTheRevisionLane));
        bool drawCenterToEndPerpendicularly = drawToEnd && (endShift == 0 || (!endIsDiagonal && !isTheRevisionLane));
        bool drawCenterPerpendicularly = isBowOfDiagonals;
        bool drawCenter = drawCenterPerpendicularly
            || !drawFromStart
            || !drawToEnd
            || (!drawCenterToStartPerpendicularly && !drawCenterToEndPerpendicularly);

        // handle non-straight junctions
        if (currentLanes.EndLane < 0 && currentLanes.PrimaryEndLane >= 0 && startShift != 0)
        {
            endShift = currentLanes.PrimaryEndLane - currentLanes.CenterLane;
            bool sameDirection = Math.Sign(endShift) == Math.Sign(startShift);
            if (startIsDiagonal)
            {
                int endDelta = Math.Abs(endShift);
                if (!sameDirection || endDelta > 1)
                {
                    drawCenterToEndPerpendicularly = true;
                    drawCenter = false;
                    horizontalOffset = -Math.Sign(startShift) * (endDelta != 1 || sameDirection ? (int)LaneLineWidth / 3 : bowOffset);
                }
            }
            else if (Math.Abs(endShift) == 1)
            {
                // multi-lane crossing continued by a diagonal
                drawCenterToStartPerpendicularly = false;
                if (!sameDirection)
                {
                    // bow
                    horizontalOffset = -Math.Sign(startShift) * (int)LaneLineWidth * 2 / 3;
                }
            }
            else
            {
                // multi-lane crossing continued by a straight or a multi-lane crossing
                drawCenterToStartPerpendicularly = false;
            }
        }

        return new DiagonalSegment(drawFromStart, drawToEnd,
            drawCenterToStartPerpendicularly, drawCenter, drawCenterPerpendicularly, drawCenterToEndPerpendicularly,
            isTheRevisionLane, horizontalOffset);
    }

    private static void DrawSegmentCurvy(ref SegmentRenderer segmentRenderer, SegmentPoints p, SegmentLanes lanes)
    {
        if (lanes.DrawFromStart)
        {
            segmentRenderer.DrawTo(p.Start);
        }

        segmentRenderer.DrawTo(p.Center);

        if (lanes.DrawToEnd)
        {
            segmentRenderer.DrawTo(p.End);
        }
    }

    private static void DrawSegmentWithDiagonals(ref SegmentRenderer segmentRenderer,
        SegmentPoints p,
        Lazy<DiagonalSegment> previousSegmentInfo,
        DiagonalSegment current,
        Lazy<DiagonalSegment> nextSegmentInfo)
    {
        double halfPerpendicularHeight = (int)segmentRenderer.RowHeight / 6;

        if (current.DrawFromStart)
        {
            DiagonalSegment previous = previousSegmentInfo.Value;
            double startX = p.Start.X + previous.HorizontalOffset;
            if (previous.DrawCenterToEndPerpendicularly)
            {
                segmentRenderer.DrawTo(new Point(startX, p.Start.Y + halfPerpendicularHeight));
            }
            else if (previous.DrawCenter)
            {
                segmentRenderer.DrawTo(new Point(startX, p.Start.Y), previous.DrawCenterPerpendicularly);
            }
            else
            {
                segmentRenderer.DrawTo(new Point(startX, p.Start.Y - halfPerpendicularHeight));
            }
        }

        double centerX = p.Center.X + current.HorizontalOffset;

        if (current.DrawCenterToStartPerpendicularly)
        {
            segmentRenderer.DrawTo(new Point(centerX, p.Center.Y - halfPerpendicularHeight));
        }

        if (current.DrawCenter)
        {
            segmentRenderer.DrawTo(new Point(centerX, p.Center.Y), current.DrawCenterPerpendicularly);
        }

        if (current.DrawCenterToEndPerpendicularly)
        {
            segmentRenderer.DrawTo(new Point(centerX, p.Center.Y + halfPerpendicularHeight));
        }

        if (current.DrawToEnd)
        {
            DiagonalSegment next = nextSegmentInfo.Value;
            double endX = p.End.X + next.HorizontalOffset;
            if (next.DrawCenterToStartPerpendicularly)
            {
                segmentRenderer.DrawTo(new Point(endX, p.End.Y - halfPerpendicularHeight));
            }
            else if (next.DrawCenter)
            {
                segmentRenderer.DrawTo(new Point(endX, p.End.Y), next.DrawCenterPerpendicularly);
            }
            else
            {
                segmentRenderer.DrawTo(new Point(endX, p.End.Y + halfPerpendicularHeight));
            }
        }
    }

    private IBrush GetBrushForLaneInfo(LaneInfo? laneInfo, bool isRelative, RevisionGraphDrawStyle revisionGraphDrawStyle, bool? isHoverHighlighted = null)
    {
        // As GraphRenderer.GetBrushForLaneInfo.
        if (laneInfo is not null && isHoverHighlighted is not false
            && (isHoverHighlighted is true || isRelative || revisionGraphDrawStyle is not (RevisionGraphDrawStyle.DrawNonRelativesGray or RevisionGraphDrawStyle.HighlightSelected)))
        {
            return _laneBrushes[laneInfo.Color % _laneBrushes.Count];
        }

        return _nonRelativeBrush;
    }

    private static int GetLaneForRow(IRevisionGraphRow? row, RevisionGraphSegment revisionGraphRevision)
    {
        if (row is not null)
        {
            int lane = row.GetLaneForSegment(revisionGraphRevision).Index;
            if (lane >= 0)
            {
                return lane;
            }
        }

        return _noLane;
    }

    private struct SegmentPoints
    {
        public Point Start;
        public Point Center;
        public Point End;
    }

    private readonly record struct SegmentLanes(int StartLane, int CenterLane, int EndLane, int PrimaryEndLane, bool IsTheRevisionLane, bool DrawFromStart, bool DrawToEnd);

    private readonly record struct DiagonalSegment(
        bool DrawFromStart,
        bool DrawToEnd,
        bool DrawCenterToStartPerpendicularly,
        bool DrawCenter,
        bool DrawCenterPerpendicularly,
        bool DrawCenterToEndPerpendicularly,
        bool IsTheRevisionLane,
        int HorizontalOffset);

    /// <summary>As the WinForms <c>SegmentRenderer</c>: draws a segment point by point with lines and Bezier curves.</summary>
    private struct SegmentRenderer(DrawingContext context, Pen pen, Size cellSize, bool renderGraphWithDiagonals)
    {
        private bool _fromPerpendicularly = true;
        private Point? _fromPoint = null;

        public readonly double RowHeight => cellSize.Height;

        public void DrawTo(Point toPoint, bool toPerpendicularly = true)
        {
            try
            {
                if (_fromPoint is null)
                {
                    return;
                }

                DrawTo(_fromPoint.Value, toPoint, _fromPerpendicularly, toPerpendicularly);
            }
            finally
            {
                _fromPoint = toPoint;
                _fromPerpendicularly = toPerpendicularly;
            }
        }

        private readonly void DrawTo(Point e0, Point e1, bool fromPerpendicularly, bool toPerpendicularly)
        {
            if (e0.X == e1.X)
            {
                DrawLine(e0, e1);
                return;
            }

            double height = e1.Y - e0.Y;
            double width = e1.X - e0.X;
            bool singleLane = Math.Abs(width) <= cellSize.Width;
            Vector cellShift = new(Math.Sign(width) * cellSize.Width, cellSize.Height);

            if (!fromPerpendicularly && !toPerpendicularly && singleLane)
            {
                // Direct line
                DrawLine(e0, e1);
                return;
            }

            // Control points for Bezier curve
            Point c0 = e0;
            Point c1 = e1;

            const double diagonalFractionCurve = 1d / 4d;
            const double perpendicularFraction = diagonalFractionCurve;
            double perpendicularOffset = perpendicularFraction * cellShift.Y;

            if (fromPerpendicularly && toPerpendicularly)
            {
                if (renderGraphWithDiagonals && singleLane)
                {
                    c0 = c0.WithY(c0.Y + perpendicularOffset);
                    c1 = c1.WithY(c1.Y - perpendicularOffset);

                    Point mid = new((e0.X + e1.X) / 2, (e0.Y + e1.Y) / 2);
                    Vector shift = diagonalFractionCurve * cellShift;
                    DrawBezier(e0, c0, mid - shift, mid);
                    DrawBezier(e1, c1, mid + shift, mid);
                    return;
                }

                double middleY = (e0.Y + e1.Y) / 2;
                c0 = c0.WithY(middleY);
                c1 = c1.WithY(middleY);
            }
            else if (singleLane)
            {
                // Is the end of a diagonal
                double diagonalFractionStraight = height < cellShift.Y ? 2d / 5d : 1d / 2d;

                if (fromPerpendicularly)
                {
                    MoveDrawDiagonallyFrom(ref e1, out _, -diagonalFractionStraight, cellShift);

                    // Prepare remaining curve
                    c1 = e1 - (diagonalFractionCurve * cellShift);
                    c0 = c0.WithY(c0.Y + perpendicularOffset);
                }
                else
                {
                    MoveDrawDiagonallyFrom(ref e0, out _, +diagonalFractionStraight, cellShift);

                    // Prepare remaining curve
                    c0 = e0 + (diagonalFractionCurve * cellShift);
                    c1 = c1.WithY(c1.Y - perpendicularOffset);
                }
            }
            else
            {
                // Is a multi-lane crossing
                const double diagonalFractionStraight = 1d / 6d;

                if (fromPerpendicularly)
                {
                    c0 = c0.WithY(c0.Y + perpendicularOffset);
                }
                else
                {
                    MoveDrawDiagonallyFrom(ref e0, out c0, +diagonalFractionStraight, cellShift);
                }

                if (toPerpendicularly)
                {
                    c1 = c1.WithY(c1.Y - perpendicularOffset);
                }
                else
                {
                    MoveDrawDiagonallyFrom(ref e1, out c1, -diagonalFractionStraight, cellShift);
                }
            }

            DrawBezier(e0, c0, c1, e1);
        }

        private readonly void DrawLine(Point from, Point to) => context.DrawLine(pen, from, to);

        private readonly void DrawBezier(Point e0, Point c0, Point c1, Point e1)
        {
            StreamGeometry geometry = new();
            using (StreamGeometryContext figure = geometry.Open())
            {
                figure.BeginFigure(e0, isFilled: false);
                figure.CubicBezierTo(c0, c1, e1);
                figure.EndFigure(isClosed: false);
            }

            context.DrawGeometry(null, pen, geometry);
        }

        private readonly void MoveDrawDiagonallyFrom(ref Point start, out Point bezierCenter, double fractionOfCell, Vector cellShift)
        {
            Vector shift = fractionOfCell * cellShift;
            Point end = start + shift;
            DrawLine(start, end);

            start = end;
            bezierCenter = end + shift;
        }
    }
}
