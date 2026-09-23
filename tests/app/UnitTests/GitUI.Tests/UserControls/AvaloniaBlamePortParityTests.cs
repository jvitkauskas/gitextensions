using System.Globalization;
using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI.Blame;
using GitUI.Presentation.UserControls.Blame;

namespace GitUITests.UserControls;

/// <summary>
///  Compares the Avalonia port of the blame texts (<see cref="BlameContentsBuilder"/>) with <see cref="BlameControl"/>, so
///  that an upstream change of the WinForms blame fails here until it is ported (docs/avalonia-port/ledger.md).
/// </summary>
[SetCulture("en-US")]
[SetUICulture("en-US")]
[Apartment(ApartmentState.STA)]
public sealed class AvaloniaBlamePortParityTests
{
    private static readonly GitBlameCommit First = new(
        ObjectId.Random(), "author1", "author1@mail.fake", new DateTime(2010, 3, 22, 12, 1, 2), "+0100",
        "committer1", "committer1@mail.fake", new DateTime(2010, 3, 22, 13, 1, 2), "+0100", "summary 1", "old/name.txt");

    private static readonly GitBlameCommit Second = new(
        ObjectId.Random(), "a much longer author name", "author2@mail.fake", DateTime.Now.AddMonths(-2), "+0100",
        "committer2", "committer2@mail.fake", DateTime.Now.AddMonths(-2), "+0100", "summary 2", "name.txt");

    private static readonly GitBlame Blame = new([
        new GitBlameLine(First, 1, 1, "line1"),
        new GitBlameLine(First, 2, 2, "line2"),
        new GitBlameLine(Second, 3, 3, "line3"),
        new GitBlameLine(First, 4, 3, "line4"),
        new GitBlameLine(Second, 5, 5, "\tline5 "),
    ]);

    [Test]
    public void Blame_contents_match_the_winforms_blame(
        [Values(false, true)] bool showAuthor,
        [Values(false, true)] bool showAuthorTime,
        [Values(false, true)] bool showOriginalFilePath,
        [Values(false, true)] bool displayAuthorFirst)
    {
        // The avatars are not compared: the WinForms blame would download them.
        BlameDisplayOptions options = new(
            ShowAuthor: showAuthor,
            ShowAuthorDate: true,
            ShowAuthorTime: showAuthorTime,
            ShowOriginalFilePath: showOriginalFilePath,
            DisplayAuthorFirst: displayAuthorFirst,
            ShowAuthorAvatar: false);
        (bool, bool, bool, bool, bool, bool) original = (AppSettings.BlameShowAuthor, AppSettings.BlameShowAuthorDate, AppSettings.BlameShowAuthorTime,
            AppSettings.BlameShowOriginalFilePath, AppSettings.BlameDisplayAuthorFirst, AppSettings.BlameShowAuthorAvatar);
        try
        {
            AppSettings.BlameShowAuthor = options.ShowAuthor;
            AppSettings.BlameShowAuthorDate = options.ShowAuthorDate;
            AppSettings.BlameShowAuthorTime = options.ShowAuthorTime;
            AppSettings.BlameShowOriginalFilePath = options.ShowOriginalFilePath;
            AppSettings.BlameDisplayAuthorFirst = options.DisplayAuthorFirst;
            AppSettings.BlameShowAuthorAvatar = options.ShowAuthorAvatar;
            using BlameControl control = new();
            control.GetTestAccessor().Blame = Blame;

            (string gutter, string body, _) = control.GetTestAccessor().BuildBlameContents("name.txt");

            BlameContents port = BlameContentsBuilder.Build(Blame, "name.txt", options, DateTime.Now, CultureInfo.CurrentCulture);
            port.Gutter.Should().Be(gutter);
            port.Body.Should().Be(body);
        }
        finally
        {
            (AppSettings.BlameShowAuthor, AppSettings.BlameShowAuthorDate, AppSettings.BlameShowAuthorTime,
                AppSettings.BlameShowOriginalFilePath, AppSettings.BlameDisplayAuthorFirst, AppSettings.BlameShowAuthorAvatar) = original;
        }
    }

    [Test]
    public void Age_buckets_match_the_winforms_blame()
    {
        using BlameControl control = new();
        DateTime now = DateTime.Now;
        IReadOnlyList<GitBlameLine> lines = [
            .. Blame.Lines,
            new GitBlameLine(new GitBlameCommit(ObjectId.Random(), "a", "a", now.AddYears(-1), "", "c", "c", now, "", "s", "f"), 6, 6, ""),
            new GitBlameLine(new GitBlameCommit(ObjectId.Random(), "a", "a", DateTime.MinValue, "", "c", "c", now, "", "s", "f"), 7, 7, ""),
        ];

        List<int> winForms = [.. control.GetTestAccessor().CalculateBlameGutterData(lines).Select(entry => entry.AgeBucketIndex)];

        BlameContentsBuilder.CalculateAgeBuckets(lines, now).Should().Equal(winForms);
        BlameContentsBuilder.GetArtificialOldBoundary(now).Date.Should().Be(control.GetTestAccessor().ArtificialOldBoundary.Date);
    }
}
