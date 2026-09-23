using System.Globalization;
using System.Text;
using GitCommands;
using GitExtensions.Extensibility.Git;

namespace GitUI.Presentation.UserControls.Blame;

/// <summary>What the blame gutter shows (the <c>Blame*</c> settings, as <c>BlameControl.BuildBlameContents</c> reads them).</summary>
public sealed record BlameDisplayOptions(
    bool ShowAuthor = true,
    bool ShowAuthorDate = true,
    bool ShowAuthorTime = true,
    bool ShowOriginalFilePath = true,
    bool DisplayAuthorFirst = false,
    bool ShowLineNumbers = false,
    bool ShowAuthorAvatar = true);

/// <summary>
///  The texts of a blame: the author lines of the gutter, the file and the age bucket of each line
///  (<see cref="AgeBuckets"/> is empty unless the avatars are shown, as <c>BlameControl.CalculateBlameGutterData</c>).
/// </summary>
public sealed record BlameContents(string Gutter, string Body, IReadOnlyList<int> AgeBuckets);

/// <summary>Port of <c>BlameControl.BuildBlameContents</c>, <c>BuildAuthorLine</c> and <c>CalculateBlameGutterData</c>.</summary>
public static class BlameContentsBuilder
{
    /// <summary>The number of age buckets (the colors of <c>BlameControl.GetAgeBucketGradientColors</c>).</summary>
    public const int AgeBucketCount = 7;

    /// <summary>The oldest date of the age buckets, unless older lines exist (as <c>BlameControl.ArtificialOldBoundary</c>).</summary>
    public static DateTime GetArtificialOldBoundary(DateTime now) => now.AddYears(-3);

    public static BlameContents Build(GitBlame blame, string? filename, BlameDisplayOptions options, DateTime now, CultureInfo culture)
    {
        if (blame.Lines.Count == 0)
        {
            return new BlameContents("", "", []);
        }

        StringBuilder body = new(capacity: 4096);
        GitBlameCommit? lastCommit = null;
        IReadOnlyList<int> ageBuckets = options.ShowAuthorAvatar ? CalculateAgeBuckets(blame.Lines, now) : [];
        string dateTimeFormat = options.ShowAuthorTime
            ? culture.DateTimeFormat.ShortDatePattern + " " + culture.DateTimeFormat.ShortTimePattern
            : culture.DateTimeFormat.ShortDatePattern;

        // As BlameControl: the lines are padded with spaces (which the WinForms viewer highlights).
        filename = filename?.ToPosixPath();
        int filePathLengthEstimate = blame.Lines.Where(l => filename != l.Commit.FileName)
                                                .Select(l => l.Commit.FileName.Length)
                                                .DefaultIfEmpty(0)
                                                .Max();
        int lineLengthEstimate = 25 + blame.Lines.Max(l => l.Commit.Author?.Length ?? 0) + filePathLengthEstimate;
        int lineLength = Math.Max(80, lineLengthEstimate);
        StringBuilder lineBuilder = new(lineLength + 2);
        StringBuilder gutter = new(capacity: lineBuilder.Capacity * blame.Lines.Count);
        string emptyLine = new(' ', lineLength);
        Dictionary<ObjectId, string> authorLineCache = [];
        foreach (GitBlameLine line in blame.Lines)
        {
            if (line.Commit == lastCommit)
            {
                gutter.AppendLine(emptyLine);
            }
            else
            {
                if (!authorLineCache.TryGetValue(line.Commit.ObjectId, out string? authorLine))
                {
                    authorLine = BuildAuthorLine(line, lineBuilder, lineLength, dateTimeFormat, filename, options.ShowAuthor, options.ShowAuthorDate, options.ShowOriginalFilePath, options.DisplayAuthorFirst, culture);
                    authorLineCache.Add(line.Commit.ObjectId, authorLine);
                    lineBuilder.Clear();
                }

                gutter.Append(authorLine);
            }

            body.AppendLine(line.Text);
            lastCommit = line.Commit;
        }

        return new BlameContents(gutter.ToString(), body.ToString(), ageBuckets);
    }

    public static string BuildAuthorLine(GitBlameLine line, StringBuilder authorLineBuilder, int lineLength, string dateTimeFormat,
        string? filename, bool showAuthor, bool showAuthorDate, bool showOriginalFilePath, bool displayAuthorFirst, CultureInfo culture)
    {
        if (showAuthor && displayAuthorFirst)
        {
            authorLineBuilder.Append(line.Commit.Author);
            if (showAuthorDate)
            {
                authorLineBuilder.Append(" - ");
            }
        }

        if (showAuthorDate)
        {
            authorLineBuilder.Append(line.Commit.AuthorTime.ToString(dateTimeFormat, culture));
        }

        if (showAuthor && !displayAuthorFirst)
        {
            if (showAuthorDate)
            {
                authorLineBuilder.Append(" - ");
            }

            authorLineBuilder.Append(line.Commit.Author);
        }

        if (showOriginalFilePath && filename != line.Commit.FileName)
        {
            authorLineBuilder.Append(" - ");
            authorLineBuilder.Append(line.Commit.FileName);
        }

        authorLineBuilder.Append(' ', Math.Max(0, lineLength - authorLineBuilder.Length)).AppendLine();

        return authorLineBuilder.ToString();
    }

    /// <summary>The age bucket of each line, from the oldest (0) to the most recent.</summary>
    public static IReadOnlyList<int> CalculateAgeBuckets(IReadOnlyList<GitBlameLine> blameLines, DateTime now)
    {
        long mostRecentDate = now.Ticks;
        DateTime artificialOldBoundary = GetArtificialOldBoundary(now);
        long lessRecentDate = Math.Min(artificialOldBoundary.Ticks,
                                      blameLines.Select(l => l.Commit.AuthorTime)
                                                .Where(d => d != DateTime.MinValue)
                                                .DefaultIfEmpty(artificialOldBoundary)
                                                .Min()
                                                .Ticks);
        long intervalSize = (mostRecentDate - lessRecentDate + 1) / AgeBucketCount;
        List<int> buckets = new(blameLines.Count);
        foreach (GitBlameLine blame in blameLines)
        {
            long relativeTicks = Math.Max(0, blame.Commit.AuthorTime.Ticks - lessRecentDate);
            buckets.Add(Math.Min((int)(relativeTicks / intervalSize), AgeBucketCount - 1));
        }

        return buckets;
    }
}
