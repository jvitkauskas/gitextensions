using GitExtensions.Extensibility.Git;
using GitUI.Presentation.HelperDialogs;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUIPluginInterfaces;
using static GitUI.AvaloniaTests.ViewModels.FileStatusListViewModelTests;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the commit diff dialog (port of <c>FormCommitDiff</c>).</summary>
[TestFixture]
public sealed class CommitDiffViewModelTests
{
    internal static readonly GitRevision Revision = new(ObjectId.Parse("c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3"))
    {
        Subject = "Fix the bug",
        Author = "Alice",
        AuthorUnixTime = new DateTimeOffset(2026, 9, 21, 10, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
        ParentIds = [First.ObjectId],
    };

    [Test]
    public async Task Shows_the_commit_its_files_and_the_first_diff()
    {
        FakeHost host = new();
        DiffViewModelTests.FakeViewerHost viewer = new();
        CommitDiffViewModel viewModel = Create(host, viewer);
        viewModel.Title.Should().Be("Diff");

        await viewModel.InitializeAsync();

        viewModel.Title.Should().Be($"Diff - c3c3c3c3 - {Revision.AuthorDate} - Alice - C:\\repo");
        viewModel.CommitInfo.HasRevision.Should().BeTrue();
        viewModel.Files.AllEntries.Should().HaveCount(4);
        host.Diffs.Should().Equal(Revision.ObjectId);
        await viewer.Shown;
        viewModel.Viewer.Editor.Text.Should().Be("diff of docs/readme.md");
    }

    [Test]
    public async Task Selects_the_requested_file()
    {
        CommitDiffViewModel viewModel = Create(new FakeHost(), new DiffViewModelTests.FakeViewerHost(), fileToSelect: "src/Program.cs");

        await viewModel.InitializeAsync();

        viewModel.Files.SelectedEntry!.Item.Name.Should().Be("src/Program.cs");
    }

    [Test]
    public async Task An_unreadable_commit_shows_nothing()
    {
        CommitDiffViewModel viewModel = Create(new FakeHost { Missing = true }, new DiffViewModelTests.FakeViewerHost());

        await viewModel.InitializeAsync();

        viewModel.CommitInfo.HasRevision.Should().BeFalse();
        viewModel.Files.Nodes.Should().BeEmpty();
    }

    internal static CommitDiffViewModel Create(FakeHost host, DiffViewModelTests.FakeViewerHost viewer, string? fileToSelect = null)
        => new(new CommitDiffStrings(), host, viewer, new Views.CommitInfoViewTests.FakeHost(), new FileStatusListStrings(), new FileStatusTreeOptions(), Revision.ObjectId, fileToSelect);

    internal sealed class FakeHost : ICommitDiffHost
    {
        public bool Missing { get; init; }

        public List<ObjectId> Diffs { get; } = [];

        public string WorkingDirectory => @"C:\repo";

        public GitRevision? GetRevision(ObjectId objectId) => Missing ? null : Revision;

        public Task<IReadOnlyList<FileStatusGroup>> GetDiffsAsync(GitRevision revision, CancellationToken cancellationToken)
        {
            Diffs.Add(revision.ObjectId);
            return Task.FromResult<IReadOnlyList<FileStatusGroup>>([new FileStatusGroup(First, revision, "", CreateStatuses())]);
        }
    }
}
