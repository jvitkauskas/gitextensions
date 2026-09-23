using System.Globalization;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.UserControls.Blame;
using GitUIPluginInterfaces;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the blame (port of <c>BlameControl</c> and <c>FormBlame</c>).</summary>
[TestFixture]
[SetCulture("en-US")]
public sealed class BlameViewModelTests
{
    internal static readonly GitBlameCommit Old = new(
        ObjectId.Parse("a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1"), "Alice", "<alice@example.org>", new DateTime(2020, 3, 22, 12, 1, 2), "+0000",
        "Alice", "<alice@example.org>", new DateTime(2020, 3, 22, 12, 1, 2), "+0000", "Add the file", "src/old.cs");

    internal static readonly GitBlameCommit Recent = new(
        ObjectId.Parse("b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2"), "Bob", "<bob@example.org>", DateTime.Now.AddDays(-1), "+0000",
        "Bob", "<bob@example.org>", DateTime.Now.AddDays(-1), "+0000", "Change the file", "src/file.cs");

    internal static readonly GitRevision Revision = new(Recent.ObjectId) { Subject = "Change the file", Author = "Bob", ParentIds = [Old.ObjectId] };

    internal static GitBlame CreateBlame() => new([
        new GitBlameLine(Old, 1, 1, "namespace Sample;"),
        new GitBlameLine(Old, 2, 2, ""),
        new GitBlameLine(Recent, 3, 3, "public class Program"),
        new GitBlameLine(Recent, 4, 4, "{"),
        new GitBlameLine(Old, 5, 3, "}"),
    ]);

    [Test]
    public void Contents_are_built_as_the_winforms_blame()
    {
        GitBlame blame = CreateBlame();
        DateTime now = new(2026, 9, 23);

        BlameContents contents = BlameContentsBuilder.Build(blame, "src/file.cs", new BlameDisplayOptions(ShowAuthorTime: false), now, CultureInfo.GetCultureInfo("en-US"));

        string[] gutter = contents.Gutter.Split(Environment.NewLine);
        gutter[0].TrimEnd().Should().Be("3/22/2020 - Alice - src/old.cs");
        gutter[1].Trim().Should().BeEmpty();
        gutter[2].TrimEnd().Should().StartWith(Recent.AuthorTime.ToString("M/d/yyyy", CultureInfo.InvariantCulture) + " - Bob");
        gutter[4].TrimEnd().Should().Be("3/22/2020 - Alice - src/old.cs", "the commit changes");
        gutter[0].Length.Should().Be(80, "the lines are padded as in WinForms");
        contents.Body.Should().Be(string.Join(Environment.NewLine, blame.Lines.Select(l => l.Text)) + Environment.NewLine);
        contents.AgeBuckets.Should().Equal(0, 0, 6, 6, 0);
    }

    [Test]
    public void Contents_follow_the_display_options()
    {
        GitBlameLine line = CreateBlame().Lines[0];
        System.Text.StringBuilder builder = new();

        BlameContentsBuilder.BuildAuthorLine(line, builder, 40, "d", "src/file.cs", showAuthor: true, showAuthorDate: false, showOriginalFilePath: false, displayAuthorFirst: true, CultureInfo.InvariantCulture)
            .TrimEnd().Should().Be("Alice");
        BlameContentsBuilder.Build(CreateBlame(), "src/file.cs", new BlameDisplayOptions(ShowAuthorAvatar: false), DateTime.Now, CultureInfo.InvariantCulture)
            .AgeBuckets.Should().BeEmpty("the age is shown with the avatars");
    }

    [Test]
    public async Task Loads_the_blame_and_shows_the_commit_of_the_selected_line()
    {
        FakeHost host = new();
        BlameViewModel viewModel = Create(host);

        await viewModel.LoadAsync(Revision, children: null, "src/file.cs", initialLine: 3);

        viewModel.File.Text.Should().Be(string.Join(Environment.NewLine, CreateBlame().Lines.Select(l => l.Text)) + Environment.NewLine);
        viewModel.File.LineToShow.Should().Be(3);
        viewModel.File.IsReadOnly.Should().BeTrue();
        viewModel.AuthorLines.Select(l => l is null).Should().Equal(false, true, false, true, false);
        viewModel.AuthorLines[0].Should().EndWith("Alice - src/old.cs", "trimmed");
        viewModel.CommitInfo.HasRevision.Should().BeTrue();
        host.Blamed.Should().Equal(("src/file.cs", Recent.ObjectId));

        viewModel.SelectLine(1);
        host.Revisions.Should().Equal(Old.ObjectId);
        viewModel.SelectLine(2);
        host.Revisions.Should().HaveCount(1, "the same commit");
    }

    [Test]
    public async Task Is_not_reloaded_unless_something_changed()
    {
        FakeHost host = new();
        BlameViewModel viewModel = Create(host);

        await viewModel.LoadAsync(Revision, children: null, "src/file.cs");
        await viewModel.LoadAsync(Revision, children: null, "src/file.cs");
        host.Blamed.Should().HaveCount(1);

        await viewModel.LoadAsync(Revision, children: null, "src/file.cs", force: true);
        host.Blamed.Should().HaveCount(2);
    }

    [Test]
    public async Task A_failing_blame_shows_the_error()
    {
        BlameViewModel viewModel = Create(new FakeHost { Failure = new InvalidOperationException("fatal: no such path") });

        await viewModel.LoadAsync(Revision, children: null, "src/file.cs");

        viewModel.File.Text.Should().Be("fatal: no such path");
        viewModel.Blame.Should().BeNull();
        viewModel.IsLoading.Should().BeFalse();
    }

    [Test]
    public async Task Hovering_highlights_the_commit_and_describes_it()
    {
        BlameViewModel viewModel = Create(new FakeHost());
        await viewModel.LoadAsync(Revision, children: null, "src/file.cs");

        viewModel.HoverLine(4);
        viewModel.HighlightedCommit.Should().BeSameAs(Recent);
        viewModel.GetToolTip(4).Should().Be("Bob: Change the file");
        viewModel.HoverLine(0);
        viewModel.HighlightedCommit.Should().BeNull();
    }

    [Test]
    public async Task Without_a_grid_revisions_cannot_be_blamed_but_their_changes_shown()
    {
        FakeHost host = new();
        BlameViewModel viewModel = Create(host);
        await viewModel.LoadAsync(Revision, children: null, "src/file.cs");

        viewModel.GetMenuState(1).Should().Be(new BlameMenuState(CanBlameRevision: false, CanBlamePreviousRevision: false, PreviousIsActual: true));
        viewModel.ShowChangesOf(1);
        viewModel.CopyCommitHash(3);
        viewModel.CopyCommitMessage(1);

        host.CommitDiffs.Should().Equal(Old.ObjectId);
        host.Copied.Should().Equal(Recent.ObjectId.ToString(), "Add the file");
    }

    [Test]
    public async Task With_a_grid_blaming_a_revision_selects_it()
    {
        FakeHost host = new();
        FakeGrid grid = new() { Listed = { Revision, new GitRevision(Old.ObjectId) } };
        BlameViewModel viewModel = Create(host);
        viewModel.RevisionGrid = grid;
        await viewModel.LoadAsync(Revision, children: null, "src/file.cs");

        viewModel.GetMenuState(3).Should().Be(new BlameMenuState(CanBlameRevision: true, CanBlamePreviousRevision: true, PreviousIsActual: true));
        viewModel.GetMenuState(1).Should().Be(new BlameMenuState(CanBlameRevision: true, CanBlamePreviousRevision: false, PreviousIsActual: false), "the first commit has no parent");

        viewModel.SelectLine(3);
        viewModel.BlameSelectedLineRevision();
        grid.Selected.Should().Equal((Recent.ObjectId, "src/file.cs"));

        viewModel.BlamePreviousRevisionOf(3);
        grid.Selected[^1].Should().Be((Old.ObjectId, "src/file.cs"));
        host.OriginalLines.Should().Equal(3);

        grid.Listed.Clear();
        grid.Listed.Add(Revision);
        grid.SelectFails = true;
        viewModel.BlameRevisionOf(3);
        host.Filtered.Should().Equal(Recent.ObjectId);
    }

    [Test]
    public async Task The_dialog_blames_its_file()
    {
        FakeHost host = new();
        BlameDialogViewModel viewModel = new(new BlameStrings(), host, new Views.CommitInfoViewTests.FakeHost(), "src/file.cs", Revision, initialLine: 2);

        await viewModel.InitializeAsync();

        viewModel.Title.Should().Be("Blame (src/file.cs)");
        viewModel.Blame.File.LineToShow.Should().Be(2);
    }

    internal static BlameViewModel Create(FakeHost host) => new(new BlameStrings(), host, new Views.CommitInfoViewTests.FakeHost());

    internal sealed class FakeHost : IBlameHost
    {
        public Exception? Failure { get; init; }

        public List<(string FileName, ObjectId ObjectId)> Blamed { get; } = [];

        public List<ObjectId> Revisions { get; } = [];

        public List<ObjectId> CommitDiffs { get; } = [];

        public List<ObjectId> Filtered { get; } = [];

        public List<string> Copied { get; } = [];

        public List<int> OriginalLines { get; } = [];

        public BlameDisplayOptions Options { get; init; } = new(ShowAuthorTime: false);

        public Task<GitBlame> GetBlameAsync(string fileName, ObjectId objectId, CancellationToken cancellationToken)
        {
            Blamed.Add((fileName, objectId));
            return Failure is null ? Task.FromResult(CreateBlame()) : Task.FromException<GitBlame>(Failure);
        }

        public GitRevision? GetRevision(ObjectId objectId)
        {
            Revisions.Add(objectId);
            return new GitRevision(objectId) { Subject = "subject" };
        }

        public string Describe(GitBlameCommit commit) => $"{commit.Author}: {commit.Summary}";

        public int GetOriginalLineInPreviousCommit(GitRevision revision, string fileName, int line)
        {
            OriginalLines.Add(line);
            return line;
        }

        public void ShowCommitDiff(ObjectId objectId) => CommitDiffs.Add(objectId);

        public void ShowRevisionFiltered(ObjectId objectId) => Filtered.Add(objectId);

        public void CopyToClipboard(string text) => Copied.Add(text);
    }

    private sealed class FakeGrid : IBlameRevisionGrid
    {
        public List<GitRevision> Listed { get; } = [];

        public List<(ObjectId ObjectId, string FileName)> Selected { get; } = [];

        public bool SelectFails { get; set; }

        public GitRevision? GetRevision(ObjectId objectId) => Listed.FirstOrDefault(r => r.ObjectId == objectId);

        public GitRevision GetActualRevision(GitRevision revision) => revision;

        public GitRevision? GetActualRevision(ObjectId objectId) => GetRevision(objectId);

        public bool SelectFileInRevision(ObjectId objectId, string fileName)
        {
            Selected.Add((objectId, fileName));
            return !SelectFails;
        }
    }
}
