using System.Text.RegularExpressions;

namespace GitUI.Presentation.Editor;

/// <summary>The kind of a line of a diff (the WinForms <c>DiffLineType</c> of a patch without git's colors).</summary>
public enum DiffLineKind
{
    /// <summary>Before the first hunk: <c>diff --git</c>, <c>index</c>, <c>---</c> / <c>+++</c> lines.</summary>
    FileHeader,

    /// <summary>A hunk header (<c>@@ ... @@</c>) or a line inserted by git (<c>\ No newline at end of file</c>).</summary>
    Header,

    Plus,
    Minus,
    Context,
}

/// <summary>A line of a diff and its line numbers in the old (left) and new (right) file.</summary>
/// <param name="LineNumInDiff">The line in the diff, 1-based.</param>
/// <param name="LeftLineNumber">The line in the old file, or <see cref="DiffLine.NotApplicable"/>.</param>
/// <param name="RightLineNumber">The line in the new file, or <see cref="DiffLine.NotApplicable"/>.</param>
public sealed record DiffLine(int LineNumInDiff, int LeftLineNumber, int RightLineNumber, DiffLineKind Kind)
{
    public const int NotApplicable = -1;
}

/// <summary>
///  Analyzes the lines of a unified (or combined) diff: their kind and line numbers. A port of the WinForms
///  <c>DiffLineNumAnalyzer</c> for patches without git's colors (docs/avalonia-port/PLAN.md, phase 3); keep it in sync.
/// </summary>
public static partial class DiffLinesAnalyzer
{
    [GeneratedRegex(@"\-(?<leftStart>\d{1,})\,{0,}(?<leftCount>\d{0,})\s\+(?<rightStart>\d{1,})\,{0,}(?<rightCount>\d{0,})", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture)]
    private static partial Regex HunkHeaderRegex { get; }

    /// <summary>Whether the diff is combined (a merge commit, <c>diff --cc</c>), whose lines have one prefix per parent.</summary>
    public static bool IsCombinedDiff(string text)
        => text.StartsWith("diff --cc", StringComparison.Ordinal) || text.StartsWith("diff --combined", StringComparison.Ordinal);

    public static IReadOnlyList<DiffLine> Analyze(string text) => Analyze(text, IsCombinedDiff(text));

    public static IReadOnlyList<DiffLine> Analyze(string text, bool isCombinedDiff)
    {
        List<DiffLine> result = [];
        int leftLineNum = DiffLine.NotApplicable;
        int rightLineNum = DiffLine.NotApplicable;
        bool isHeaderLineLocated = false;
        string[] lines = text.Split('\n');
        int lastLine = lines.Length - 1;
        for (int i = 0; i <= lastLine; i++)
        {
            string line = lines[i].TrimEnd('\r');
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
            else if (line.StartsWith('-'))
            {
                result.Add(new DiffLine(lineNumInDiff, leftLineNum, DiffLine.NotApplicable, DiffLineKind.Minus));
                leftLineNum++;
            }
            else if (line.StartsWith('+'))
            {
                result.Add(new DiffLine(lineNumInDiff, DiffLine.NotApplicable, rightLineNum, DiffLineKind.Plus));
                rightLineNum++;
            }
            else if (line.StartsWith('\\'))
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

    // As DiffLineNumAnalyzer: a combined diff has one prefix column per parent.
    private static bool IsMinusLineInCombinedDiff(string line) => line.StartsWith("--", StringComparison.Ordinal) || line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith(" -", StringComparison.Ordinal);

    private static bool IsPlusLineInCombinedDiff(string line) => line.StartsWith("++", StringComparison.Ordinal) || line.StartsWith("+ ", StringComparison.Ordinal) || line.StartsWith(" +", StringComparison.Ordinal);
}
