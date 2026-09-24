using System.Text.RegularExpressions;
using GitExtUtils.GitUI.Theming;

namespace GitUI.Presentation.Editor;

/// <summary>The kind of a line of a diff (the WinForms <c>DiffLineType</c>, with the same names).</summary>
public enum DiffLineKind
{
    /// <summary>Before the first hunk: <c>diff --git</c>, <c>index</c>, <c>---</c> / <c>+++</c> lines.</summary>
    FileHeader,

    /// <summary>A hunk header (<c>@@ ... @@</c>) or a line inserted by git (<c>\ No newline at end of file</c>).</summary>
    Header,

    Plus,
    Minus,
    Context,

    /// <summary>A removed line of a git word diff or of difftastic (only in the old file).</summary>
    MinusLeft,

    /// <summary>An added line of a git word diff or of difftastic (only in the new file).</summary>
    PlusRight,

    /// <summary>A changed line of a git word diff or of difftastic, with removed and added words.</summary>
    MinusPlus,

    /// <summary>A match of git grep.</summary>
    Grep,
}

/// <summary>A line of a diff and its line numbers in the old (left) and new (right) file.</summary>
/// <param name="LineNumInDiff">The line in the diff, 1-based.</param>
/// <param name="LeftLineNumber">The line in the old file, or <see cref="DiffLine.NotApplicable"/>.</param>
/// <param name="RightLineNumber">The line in the new file, or <see cref="DiffLine.NotApplicable"/>.</param>
public sealed record DiffLine(int LineNumInDiff, int LeftLineNumber, int RightLineNumber, DiffLineKind Kind)
{
    public const int NotApplicable = -1;

    /// <summary>Whether git's colors show a removed or added line as moved (they are not matched for in-line differences).</summary>
    public bool IsMovedLine { get; init; }
}

/// <summary>The colors of git's output of a diff, as <see cref="AnsiEscapeParser"/> parsed them.</summary>
/// <param name="Reverse">Whether git colors the background (<c>AppSettings.ReverseGitColoring</c>), else the text.</param>
public sealed record GitColoring(IReadOnlyList<ColoredSegment> Segments, IThemeColors Colors, bool Reverse);

/// <summary>
///  Analyzes the lines of a unified (or combined) diff, or of git's word diff: their kind and line numbers. A port of the WinForms
///  <c>DiffLineNumAnalyzer</c> (docs/avalonia-port/PLAN.md, phase 3); keep it in sync.
/// </summary>
public static partial class DiffLinesAnalyzer
{
    [GeneratedRegex(@"\-(?<leftStart>\d{1,})\,{0,}(?<leftCount>\d{0,})\s\+(?<rightStart>\d{1,})\,{0,}(?<rightCount>\d{0,})", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture)]
    private static partial Regex HunkHeaderRegex { get; }

    /// <summary>Whether the diff is combined (a merge commit, <c>diff --cc</c>), whose lines have one prefix per parent.</summary>
    public static bool IsCombinedDiff(string text)
        => text.StartsWith("diff --cc", StringComparison.Ordinal) || text.StartsWith("diff --combined", StringComparison.Ordinal);

    public static IReadOnlyList<DiffLine> Analyze(string text, GitColoring? gitColoring = null) => Analyze(text, IsCombinedDiff(text), gitColoring);

    /// <param name="gitColoring">The colors of git's output, to detect moved lines (and the lines of a git word diff).</param>
    /// <param name="isGitWordDiff">
    ///  Whether the text is git's <c>--word-diff=color</c> output (<c>DiffDisplayAppearance.GitWordDiff</c>), whose lines have no
    ///  prefixes: git's colors tell the removed, added and changed lines (as <c>isGitWordDiff</c> of <c>DiffLineNumAnalyzer</c>).
    /// </param>
    public static IReadOnlyList<DiffLine> Analyze(string text, bool isCombinedDiff, GitColoring? gitColoring = null, bool isGitWordDiff = false)
    {
        // As PatchHighlightService: the git word diff needs git's colors.
        isGitWordDiff &= gitColoring is not null;
        List<DiffLine> result = [];
        int leftLineNum = DiffLine.NotApplicable;
        int rightLineNum = DiffLine.NotApplicable;
        bool isHeaderLineLocated = false;
        string[] lines = text.Split('\n');
        int lastLine = lines.Length - 1;
        int textOffset = 0;
        for (int i = 0; i <= lastLine; textOffset += lines[i].Length + 1, i++)
        {
            string rawLine = lines[i];
            string line = rawLine.TrimEnd('\r');
            if (i == lastLine && line.Length == 0)
            {
                break;
            }

            int lineNumInDiff = i + 1;
            if (line.StartsWith("@@", StringComparison.Ordinal))
            {
                result.Add(new DiffLine(lineNumInDiff, DiffLine.NotApplicable, DiffLine.NotApplicable, DiffLineKind.Header));
                Match lineNumbers = HunkHeaderRegex.Match(line);
                if (lineNumbers.Success)
                {
                    leftLineNum = int.Parse(lineNumbers.Groups["leftStart"].ValueSpan);
                    rightLineNum = int.Parse(lineNumbers.Groups["rightStart"].ValueSpan);
                }

                isHeaderLineLocated = true;
            }
            else if (!isHeaderLineLocated)
            {
                result.Add(new DiffLine(lineNumInDiff, DiffLine.NotApplicable, DiffLine.NotApplicable, DiffLineKind.FileHeader));
            }
            else if (isCombinedDiff)
            {
                if (IsMinusLineInCombinedDiff(line))
                {
                    // The left line is from two documents, so undefined.
                    result.Add(new DiffLine(lineNumInDiff, DiffLine.NotApplicable, DiffLine.NotApplicable, DiffLineKind.Minus));
                }
                else
                {
                    result.Add(new DiffLine(lineNumInDiff, DiffLine.NotApplicable, rightLineNum, IsPlusLineInCombinedDiff(line) ? DiffLineKind.Plus : DiffLineKind.Context));
                    rightLineNum++;
                }
            }
            else if (!isGitWordDiff ? line.StartsWith('-') : IsGitWordMatch(DiffLineKind.MinusLeft, rawLine, textOffset, line.Length, gitColoring!))
            {
                DiffLineKind kind = isGitWordDiff ? DiffLineKind.MinusLeft : DiffLineKind.Minus;
                result.Add(new DiffLine(lineNumInDiff, leftLineNum, DiffLine.NotApplicable, kind)
                {
                    IsMovedLine = IsMovedLine(text, gitColoring, textOffset, line.Length, kind),
                });
                leftLineNum++;
            }
            else if (!isGitWordDiff ? line.StartsWith('+') : IsGitWordMatch(DiffLineKind.PlusRight, rawLine, textOffset, line.Length, gitColoring!))
            {
                DiffLineKind kind = isGitWordDiff ? DiffLineKind.PlusRight : DiffLineKind.Plus;
                result.Add(new DiffLine(lineNumInDiff, DiffLine.NotApplicable, rightLineNum, kind)
                {
                    IsMovedLine = IsMovedLine(text, gitColoring, textOffset, line.Length, kind),
                });
                rightLineNum++;
            }
            else if (isGitWordDiff && GetLineSegments(gitColoring!, textOffset, line.Length).Count > 0)
            {
                // As DiffLineNumAnalyzer: a line of a git word diff with removed and added words.
                result.Add(new DiffLine(lineNumInDiff, leftLineNum, rightLineNum, DiffLineKind.MinusPlus));
                leftLineNum++;
                rightLineNum++;
            }
            else if (!isGitWordDiff && line.StartsWith('\\'))
            {
                // git-diff has inserted this line (the only known one is "\ No newline at end of file"), present it as a header.
                result.Add(new DiffLine(lineNumInDiff, DiffLine.NotApplicable, DiffLine.NotApplicable, DiffLineKind.Header));
            }
            else
            {
                result.Add(new DiffLine(lineNumInDiff, leftLineNum, rightLineNum, DiffLineKind.Context));
                leftLineNum++;
                rightLineNum++;
            }
        }

        return result;
    }

    /// <summary>
    ///  As <c>DiffLineNumAnalyzer.IsMovedLine</c>: git colors moved lines in other than red / green. However, git may mark
    ///  trailing whitespace (diff.colormovedws is ignored).
    /// </summary>
    private static bool IsMovedLine(string text, GitColoring? gitColoring, int textOffset, int lineLength, DiffLineKind kind)
    {
        if (gitColoring is null)
        {
            return false;
        }

        List<ColoredSegment> segments = GetLineSegments(gitColoring, textOffset, lineLength);
        return segments.Count > 0
            && !ColorMatch(segments[0], kind, gitColoring)
            && (segments.Count <= 1 || !ColorMatch(segments[^1], kind, gitColoring) || text.AsSpan()[segments[^1].Offset..(segments[^1].Offset + segments[^1].Length - 1)].IsWhiteSpace())
            && (segments.Count <= 2 || !segments[1..^1].All(segment => ColorMatch(segment, kind, gitColoring)));
    }

    /// <summary>
    ///  As <c>DiffLineNumAnalyzer.IsGitWordMatch</c>: heuristics (or wild guessing) whether a line of a git word diff is only
    ///  removed (or only added), else it is <see cref="DiffLineKind.MinusPlus"/>. If the marker covers the line this should be
    ///  true. Git output is impossible to parse, some guesses are done. Whitespace only lines are still incorrect (no marker at
    ///  all in git), as well as some other situations.
    /// </summary>
    /// <param name="line">The line, with its carriage return if any (as <c>DiffLineNumAnalyzer</c>).</param>
    private static bool IsGitWordMatch(DiffLineKind kind, string line, int textOffset, int textLength, GitColoring gitColoring)
    {
        List<ColoredSegment> segments = GetLineSegments(gitColoring, textOffset, textLength);
        int firstNonWhiteSpace = line.Length - line.AsSpan().TrimStart().Length;

        return segments.Count == 1

            // start may be indented, if a new block of changes starts with white spaces
            && (segments[0].Offset <= textOffset || (firstNonWhiteSpace > 0 && segments[0].Offset <= textOffset + firstNonWhiteSpace))

            // Compensate length->ending and remove the trailing newline chars (no check for \r\n vs \n)
            && (segments[0].Offset + segments[0].Length - 1 >= textOffset + textLength - 3)

            // Assume the user has not overridden colors
            && ColorMatch(segments[0], kind, gitColoring);
    }

    /// <summary>The colored segments of a line (as the markers of a line in <c>DiffLineNumAnalyzer.Analyze</c>).</summary>
    private static List<ColoredSegment> GetLineSegments(GitColoring gitColoring, int textOffset, int lineLength)
    {
        // The segments are ordered and do not overlap.
        IReadOnlyList<ColoredSegment> all = gitColoring.Segments;
        int low = 0;
        int high = all.Count;
        while (low < high)
        {
            int middle = (low + high) / 2;
            if (all[middle].Offset + all[middle].Length - 1 < textOffset)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        List<ColoredSegment> segments = [];
        for (int i = low; i < all.Count && all[i].Offset < textOffset + lineLength; i++)
        {
            segments.Add(all[i]);
        }

        return segments;
    }

    /// <summary>As <c>DiffLineNumAnalyzer.MarkerColorMatch</c>: the expected color for a line kind, for heuristics.</summary>
    private static bool ColorMatch(ColoredSegment segment, DiffLineKind kind, GitColoring gitColoring)
    {
        IThemeColors colors = gitColoring.Colors;
        return kind is DiffLineKind.Minus or DiffLineKind.MinusLeft
            ? (gitColoring.Reverse
                ? segment.BackColor == colors.GetColor(AppColor.AnsiTerminalRedBackNormal)
                : segment.ForeColor == colors.GetColor(AppColor.AnsiTerminalRedForeNormal))
            : (gitColoring.Reverse
                ? segment.BackColor == colors.GetColor(AppColor.AnsiTerminalGreenBackNormal)
                : segment.ForeColor == colors.GetColor(AppColor.AnsiTerminalGreenForeNormal));
    }

    // As DiffLineNumAnalyzer: a combined diff has one prefix column per parent.
    private static bool IsMinusLineInCombinedDiff(string line) => line.StartsWith("--", StringComparison.Ordinal) || line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith(" -", StringComparison.Ordinal);

    private static bool IsPlusLineInCombinedDiff(string line) => line.StartsWith("++", StringComparison.Ordinal) || line.StartsWith("+ ", StringComparison.Ordinal) || line.StartsWith(" +", StringComparison.Ordinal);
}
