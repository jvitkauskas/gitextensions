using System.Text;
using System.Text.RegularExpressions;
using Color = System.Drawing.Color;

namespace GitUI.Presentation.Editor;

/// <summary>A diff analyzed for the viewer: the text without the escape sequences, git's colors and the lines.</summary>
/// <param name="VerticalRulerColumn">The column of the vertical ruler (0 to hide it), for difftastic the start of the right side.</param>
public sealed record AnalyzedDiff(string Text, IReadOnlyList<ColoredSegment> Segments, IReadOnlyList<DiffLine> Lines, int VerticalRulerColumn = 0);

/// <summary>
///  Analyzes the side-by-side output of difftastic (<c>git difftool --tool=difftastic</c>): removes its line numbers from the
///  text and colors, and guesses the kind of the lines from the colors of the line numbers. A port of the WinForms
///  <c>DifftasticHighlightService</c> (docs/avalonia-port/PLAN.md, phase 3); keep it in sync.
/// </summary>
public static partial class DifftasticAnalyzer
{
    [GeneratedRegex(@"^(\s*(?<matchStart>(?<lineNo>\d+)|(\.+)) )", RegexOptions.ExplicitCapture)]
    private static partial Regex LineNoRegex { get; }

    /// <param name="column">The width of the output (<c>DFT_WIDTH</c>).</param>
    /// <param name="reverseGitColoring">Whether the colors are shown on the background (<c>AppSettings.ReverseGitColoring</c>).</param>
    public static AnalyzedDiff Analyze(string text, int column, IThemeColors colors, bool reverseGitColoring)
    {
        // Hide VRulerPos by default
        int rightColumnStart = 0;
        StringBuilder sb = new(text.Length);
        List<ColoredSegment> segments = [];
        List<DiffLine> lines = [];
        int halfColumn = column / 2;
        bool nextIsHeader = true;

        foreach (string rawLine in text.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                // Empty rawLine before header
                nextIsHeader = true;
                continue;
            }

            (string parsedLine, IReadOnlyList<ColoredSegment> parsedSegments) = AnsiEscapeParser.Parse(rawLine, colors, themeColors: reverseGitColoring);
            StringBuilder lineBuilder = new(parsedLine);
            List<Marker> textMarkers = [.. parsedSegments.Select(s => new Marker(s))];

            int leftLineNo = DiffLine.NotApplicable;
            int rightLineNo = DiffLine.NotApplicable;

            if (nextIsHeader)
            {
                nextIsHeader = false;
                AddInfo(leftLineNo, rightLineNo, DiffLineKind.Header, textMarkers, lineBuilder);
                continue;
            }

            DiffLineKind lineType = DiffLineKind.Context;

            Match matchLeft = LineNoRegex.Match(lineBuilder.ToString());
            if (!matchLeft.Success)
            {
                // This could also be a side-by-diff with zero context where ... are not printed
                AddInfo(leftLineNo, rightLineNo, lineType, textMarkers, lineBuilder);
                continue;
            }

            int leftLen;
            if (matchLeft.Groups["matchStart"].Index >= halfColumn)
            {
                // No left line number (occurs if zero context), ignore (unexpected) markers
                leftLen = 0;
                if (rightColumnStart > 0)
                {
                    // Keep a consistent start for the second column
                    leftLen = halfColumn - rightColumnStart;
                }
            }
            else
            {
                leftLen = matchLeft.Length;
                if (matchLeft.Groups["lineNo"].Success && int.TryParse(matchLeft.Groups["lineNo"].ValueSpan, out int lineNo))
                {
                    leftLineNo = lineNo;
                }

                if (textMarkers.Count > 0 && textMarkers[0].Offset < leftLen)
                {
                    // Use lineno coloring to guess if this is added or removed.
                    Color c = textMarkers[0].GetColor(reverseGitColoring);
                    if (!IsUnchanged(c))
                    {
                        // Use mostly red/green to detect removed/added.
                        if (c.R > c.G)
                        {
                            lineType = DiffLineKind.MinusLeft;
                        }
                        else
                        {
                            lineType = DiffLineKind.PlusRight;
                            rightLineNo = leftLineNo;
                            leftLineNo = DiffLine.NotApplicable;
                        }
                    }
                }
            }

            // Trim left lineno from text, marker
            if (leftLen > 0)
            {
                lineBuilder = lineBuilder.Remove(0, leftLen);
                foreach (Marker tm in textMarkers)
                {
                    RemoveLineNoPart(tm, 0, leftLen);
                }
            }

            // Where to try parse for next line number, if both-sides is displayed
            int rightStartOffset = halfColumn - leftLen;
            Match matchRight;
            if (lineBuilder.Length > rightStartOffset
                && LineNoRegex.Match(lineBuilder.ToString()[rightStartOffset..]) is Match match
                && match.Success)
            {
                matchRight = match;
                if (rightColumnStart == 0)
                {
                    // Keep a consistent start for the second column
                    rightColumnStart = rightStartOffset;
                }
            }
            else
            {
                // Lineno assumed in start of line
                rightStartOffset = 0;
                matchRight = LineNoRegex.Match(lineBuilder.ToString());
            }

            if (!matchRight.Success)
            {
                AddInfo(leftLineNo, rightLineNo, lineType, textMarkers, lineBuilder);
                continue;
            }

            if (matchRight.Groups["lineNo"].Success && int.TryParse(matchRight.Groups["lineNo"].ValueSpan, out int rightNo))
            {
                rightLineNo = rightNo;
            }

            // Remove right line no from text, markers
            int rightLen = matchRight.Length;
            lineBuilder = lineBuilder.Remove(rightStartOffset, rightLen);
            int columnGap = 0;

            if (rightStartOffset > 0)
            {
                if (rightColumnStart == 0)
                {
                    rightColumnStart = rightStartOffset;
                }

                // Add spaces so both-sides markers are aligned
                columnGap = rightColumnStart - rightStartOffset;
                if (columnGap > 0)
                {
                    lineBuilder = lineBuilder.Insert(rightStartOffset, new string(' ', columnGap));
                }
            }

            bool first = true;
            foreach (Marker tm in textMarkers)
            {
                if (tm.EndOffset < rightStartOffset)
                {
                    continue;
                }

                if (first)
                {
                    first = false;

                    // Use lineno coloring to guess if this is added or removed.
                    // If not unchanged this right lineno is assumed to be added (and is likely green).
                    Color c = tm.GetColor(reverseGitColoring);
                    if (!IsUnchanged(c))
                    {
                        // Merge line type with existing left line type
                        lineType = lineType == DiffLineKind.Context ? DiffLineKind.PlusRight : DiffLineKind.MinusPlus;
                    }
                }

                RemoveLineNoPart(tm, rightStartOffset, rightLen);
                if (tm.Offset >= rightStartOffset)
                {
                    tm.Offset += columnGap;
                }
            }

            AddInfo(leftLineNo, rightLineNo, lineType, textMarkers, lineBuilder);
        }

        return new AnalyzedDiff(sb.ToString(), segments, lines, rightColumnStart);

        void AddInfo(int leftLineNo, int rightLineNo, DiffLineKind lineType, List<Marker> textMarkers, StringBuilder lineBuilder)
        {
            lines.Add(new DiffLine(lines.Count + 1, leftLineNo, rightLineNo, lineType));
            foreach (Marker tm in textMarkers)
            {
                if (tm.Length > 0)
                {
                    segments.Add(new ColoredSegment(tm.Offset + sb.Length, tm.Length, tm.BackColor, tm.ForeColor));
                }
            }

            sb.Append(lineBuilder);
            sb.Append('\n');
        }
    }

    // Use lineno coloring to guess if this is added or removed.
    // Assume the theme in Diffstatic sets gray for unchanged.
    private static bool IsUnchanged(Color c)
        => c.R == c.G;

    private static void RemoveLineNoPart(Marker tm, int offset, int length)
    {
        if (tm.Offset + tm.Length <= offset)
        {
            // All is before the gap
            return;
        }

        if (tm.Offset >= offset + length)
        {
            // All is after the gap
            tm.Offset -= length;
            return;
        }

        if (tm.Offset <= offset && offset + length <= tm.Offset + tm.Length)
        {
            // Gap is covered
            tm.Length -= length;
            return;
        }

        if (tm.Offset > offset)
        {
            // Remove the start in the gap
            tm.Length -= tm.Offset - offset;
            tm.Offset = offset;
            return;
        }

        // the end part of the gap
        tm.Length -= tm.Offset + tm.Length - offset;
    }

    /// <summary>A colored segment being moved (the <c>TextMarker</c> of the WinForms service).</summary>
    private sealed class Marker(ColoredSegment segment)
    {
        public int Offset { get; set; } = segment.Offset;

        public int Length { get; set; } = segment.Length;

        public Color BackColor { get; } = segment.BackColor;

        public Color? ForeColor { get; } = segment.ForeColor;

        /// <summary>As <c>TextMarker.EndOffset</c>.</summary>
        public int EndOffset => Offset + Length - 1;

        /// <summary>The color of the WinForms marker: the background, or else the text (<c>Color.Empty</c> if not set).</summary>
        public Color GetColor(bool reverseGitColoring) => reverseGitColoring ? BackColor : ForeColor ?? Color.Empty;
    }
}
