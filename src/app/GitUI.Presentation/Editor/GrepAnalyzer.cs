using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace GitUI.Presentation.Editor;

/// <summary>
///  Analyzes the output of <c>git grep</c> for a file: removes the line numbers and the kinds from the text, and makes the
///  matches, the function headers and the separators lines of the viewer. A port of the WinForms <c>GrepHighlightService</c>
///  (docs/avalonia-port/PLAN.md, phase 3); keep it in sync.
/// </summary>
public static partial class GrepAnalyzer
{
    private const string _grepResultKind_FunctionHeader = "=";
    private const string _grepResultKind_Match = ":";
    private const string _grepResultKind_Separator = "--";
    private const string _grepResultKind_Unknown = "";

    [GeneratedRegex(@"^(?<line>\d+)(?<kind>:|.)(?<text>.*)$", RegexOptions.ExplicitCapture)]
    private static partial Regex GrepLineRegex { get; }

    /// <summary>As <c>GrepHighlightService.SetText</c>.</summary>
    public static AnalyzedDiff Analyze(string text, IThemeColors colors)
    {
        StringBuilder sb = new(text.Length);
        List<ColoredSegment> segments = [];
        List<DiffLine> lines = [];
        bool skipNextSeparator = false;
        bool pendingSeparator = false;
        bool firstError = true;
        foreach (string line in text.Split('\n'))
        {
            if (line == _grepResultKind_Separator)
            {
                if (!skipNextSeparator && sb.Length > 0)
                {
                    pendingSeparator = true;
                }

                continue;
            }

            // Parse line no and if match (must not have colors)
            Match match = GrepLineRegex.Match(line);
            if (!match.Success || !int.TryParse(match.Groups["line"].ValueSpan, out int lineNo))
            {
                if (line.Length > 0 && firstError)
                {
                    firstError = false;
                    Trace.WriteLine($"Cannot parse lineNo for grep {(line.Length > 80 ? line[..80] : line)} ({sb.Length})");
                }

                // git-grep emits an empty line last, should not be displayed.
                // Other occurrences should not occur, just print them to debug (no lineno to not add extra line).
                sb.Append(line);
                pendingSeparator = false;
                continue;
            }

            string grepText = match.Groups["text"].Value;
            string kind = match.Groups["kind"].Success ? match.Groups["kind"].Value : _grepResultKind_Unknown;

            skipNextSeparator = kind == _grepResultKind_FunctionHeader;
            if (pendingSeparator && !skipNextSeparator)
            {
                lines.Add(GetDiffLine(DiffLine.NotApplicable, _grepResultKind_Separator));
                sb.Append('\n');
            }

            pendingSeparator = false;
            lines.Add(GetDiffLine(lineNo, kind));

            AnsiEscapeParser.Parse(grepText, sb, segments, colors);
            sb.Append('\n');
        }

        return new AnalyzedDiff(sb.ToString(), segments, lines);

        // As GrepHighlightService.GetDiffLineInfo: the line numbers are in the right column.
        DiffLine GetDiffLine(int lineno, string kind)
            => new(
                lines.Count + 1,
                DiffLine.NotApplicable,
                lineno,
                lineno == DiffLine.NotApplicable || kind == _grepResultKind_FunctionHeader
                    ? DiffLineKind.Header
                    : kind == _grepResultKind_Match
                        ? DiffLineKind.Grep
                        : DiffLineKind.Context);
    }
}

/// <summary>
///  Analyzes the output of <c>git range-diff</c>: the lines are numbered in the right column, the headers of the commits are
///  the changes. A port of the WinForms <c>RangeDiffHighlightService</c> (docs/avalonia-port/PLAN.md, phase 3); keep it in sync.
/// </summary>
public static partial class RangeDiffAnalyzer
{
    [GeneratedRegex(@"^(\u001b\[.*?m)?\s*(\d+|-):", RegexOptions.ExplicitCapture)]
    private static partial Regex RangeHeaderRegex { get; }

    /// <param name="text">The output of git range-diff, colored by git.</param>
    public static AnalyzedDiff Analyze(string text, IThemeColors colors)
    {
        (string parsedText, IReadOnlyList<ColoredSegment> segments) = AnsiEscapeParser.Parse(text, colors);
        List<DiffLine> lines = [];
        int bufferLine = 0;
        foreach (string line in parsedText.Split('\n'))
        {
            ++bufferLine;

            // Note that Git output occasionally corrupts context lines, so parse headers
            lines.Add(new DiffLine(bufferLine, DiffLine.NotApplicable, bufferLine, RangeHeaderRegex.IsMatch(line) ? DiffLineKind.Header : DiffLineKind.Context));
        }

        return new AnalyzedDiff(parsedText, segments, lines);
    }
}
