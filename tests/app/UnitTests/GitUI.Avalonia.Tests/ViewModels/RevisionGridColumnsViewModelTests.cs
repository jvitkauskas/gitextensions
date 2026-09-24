using System.ComponentModel;
using GitExtensions.Extensibility.BuildServerIntegration;
using GitExtensions.Extensibility.Git;
using GitUI.AvaloniaTests.Views;
using GitUI.Presentation.Services;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUI.UserControls.RevisionGrid.Graph;
using GitUIPluginInterfaces;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>
///  View model tests of the revision grid beyond its first version: stash rows, notes, avatars, build statuses, the author and
///  branch highlighting, the hover highlight and the hotkeys (phase 4).
/// </summary>
[TestFixture]
public sealed class RevisionGridColumnsViewModelTests
{
    private static readonly RevisionGridDisplayOptions _options = new(RelativeDate: false, ShowAuthorDate: false);

    [Test]
    public void A_stash_has_its_reflog_selector_as_label_and_the_autostash_its_subject()
    {
        GitRevision stash = new(ObjectId.Random()) { Subject = "WIP on master: 1234567 Initial commit", ReflogSelector = "refs/stash@{1}" };
        GitRevision autostash = new(ObjectId.Random()) { Subject = "Autostash", IsAutostash = true };

        RevisionGridRow stashRow = new(0, stash, _options, "main");
        stashRow.Refs.Should().Equal(new RevisionRefItem("stash@{1}", RevisionRefKind.Stash, IsCurrentBranch: false));
        stashRow.Refs[0].GitRef.Should().BeNull("a stash label has no reference");
        stashRow.Subject.Should().Be(stash.Subject);

        RevisionGridRow autostashRow = new(1, autostash, _options, "main");
        autostashRow.Refs.Should().Equal(new RevisionRefItem("Autostash", RevisionRefKind.Stash, IsCurrentBranch: false));
        autostashRow.Subject.Should().BeEmpty("the subject of the autostash is its label");
    }

    [Test]
    public void The_notes_column_shows_the_first_line_and_the_notes_as_tooltip()
    {
        RevisionGridRow row = new(0, new GitRevision(ObjectId.Random()) { Subject = "s", Notes = "Reviewed-by: Bob\nTested" }, _options, null);
        row.Notes.Should().Be("Reviewed-by: Bob");
        row.NotesToolTip.Should().Be("Reviewed-by: Bob\nTested");

        RevisionGridRow noNotes = new(0, new GitRevision(ObjectId.Random()) { Subject = "s" }, _options, null);
        noNotes.Notes.Should().BeEmpty();
        noNotes.NotesToolTip.Should().BeNull();
    }

    [Test]
    public void A_row_shows_the_build_status_reported_later()
    {
        GitRevision revision = new(ObjectId.Random()) { Subject = "s" };
        RevisionGridRow row = new(0, revision, _options, null);
        List<string?> changed = [];
        row.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        row.HasBuildReport.Should().BeFalse();
        row.BuildStatusSymbol.Should().BeEmpty();

        revision.BuildStatus = new BuildInfo { Status = BuildStatus.Failure, Description = "#12 failed", Url = "https://ci/12", Tooltip = "Build #12" };

        changed.Should().Contain([nameof(RevisionGridRow.BuildStatus), nameof(RevisionGridRow.BuildStatusSymbol), nameof(RevisionGridRow.HasBuildReport)]);
        row.BuildStatusSymbol.Should().Be("❌");
        row.BuildStatusDescription.Should().Be("#12 failed");
        row.BuildStatusToolTip.Should().Be("Build #12");
        row.HasBuildReport.Should().BeTrue();

        revision.BuildStatus = new BuildInfo { Status = BuildStatus.Success, Description = "#13" };
        row.BuildStatusToolTip.Should().Be("#13", "the description without a tooltip");
        row.HasBuildReport.Should().BeFalse("without a report URL");
    }

    [Test]
    public void The_build_report_of_a_row_opens_in_the_browser()
    {
        FakeHost host = new(CreateHistory());
        RevisionGridViewModel viewModel = new(host, _options);
        viewModel.Load();
        viewModel.Rows[0].Revision.BuildStatus = new BuildInfo { Url = "https://ci/1" };

        viewModel.OpenBuildReport(viewModel.Rows[0]);
        viewModel.OpenBuildReport(viewModel.Rows[1]);

        host.OpenedUrls.Should().Equal("https://ci/1");
    }

    [Test]
    public void The_author_of_the_selected_revision_is_highlighted_else_the_user()
    {
        List<GitRevision> history = CreateHistory();
        FakeHost host = new(history) { UserEmail = "carol@example.com" };
        RevisionGridViewModel viewModel = new(host, _options) { MultiSelect = true, HighlightAuthoredRevisions = true };
        viewModel.Load();

        viewModel.SelectedRow = viewModel.Rows.First(r => r.AuthorName == "Bob");
        viewModel.Rows.Where(r => r.IsAuthorHighlighted).Select(r => r.AuthorName).Should().OnlyContain(name => name == "Bob").And.HaveCount(2);
        viewModel.IsAuthoredHighlight(viewModel.SelectedRow).Should().BeTrue();

        // Several revisions: unchanged (AuthorRevisionHighlighting).
        viewModel.SetSelectedRows([viewModel.Rows[0], viewModel.Rows[2]]);
        viewModel.Rows.Where(r => r.IsAuthorHighlighted).Select(r => r.AuthorName).Should().OnlyContain(name => name == "Bob");

        // None: the user.
        viewModel.SetSelectedRows([]);
        viewModel.SelectedRow = null;
        viewModel.Rows.Where(r => r.IsAuthorHighlighted).Select(r => r.AuthorName).Should().Equal("Carol");

        viewModel.HighlightAuthoredRevisions = false;
        viewModel.IsAuthoredHighlight(viewModel.Rows.Single(r => r.AuthorName == "Carol")).Should().BeFalse("the background is a setting; the bold author is not");
        viewModel.GetAuthorToolTip(viewModel.Rows[0]).Should().Be("tooltip of Bob");
    }

    [Test]
    public void Highlighting_the_selected_branch_makes_the_other_revisions_non_relatives_until_reloaded()
    {
        List<GitRevision> history = RevisionGridViewTests.CreateHistory();
        RevisionGridViewModel viewModel = new(new FakeHost(history), _options) { DrawNonRelativesTextGray = true };
        viewModel.Load();
        int experiment = viewModel.Rows.Single(r => r.Subject == "Experiment").Index;
        int head = viewModel.Rows.Single(r => r.Subject == "Update the documentation").Index;
        viewModel.IsRelative(experiment).Should().BeFalse("not an ancestor of HEAD");
        viewModel.IsTextGray(experiment).Should().BeTrue();
        viewModel.IsTextGray(head).Should().BeFalse();
        int relativesChanged = 0;
        viewModel.RelativesChanged += (_, _) => relativesChanged++;

        viewModel.SelectedRow = viewModel.Rows[experiment];
        viewModel.HighlightSelectedBranch();

        viewModel.IsBranchHighlighted.Should().BeTrue();
        relativesChanged.Should().Be(1);
        viewModel.IsRelative(experiment).Should().BeTrue();
        viewModel.IsRelative(head).Should().BeFalse("not an ancestor of the highlighted revision");

        viewModel.Load();
        viewModel.IsBranchHighlighted.Should().BeFalse("until the next refresh");
    }

    [Test]
    public async Task Hovering_a_reference_highlights_its_ancestry()
    {
        List<GitRevision> history = RevisionGridViewTests.CreateHistory();
        FakeHost host = new(history);
        RevisionGridViewModel viewModel = new(host, _options);
        viewModel.Load();
        RevisionRefItem main = viewModel.Rows[0].Refs[0];
        host.HoverResult = new HashSet<ObjectId> { history[0].ObjectId, history[2].ObjectId };

        await viewModel.SetHoverReferenceAsync(main.GitRef, 0, 0, 8);
        viewModel.HoverHighlightedIds.Should().BeEquivalentTo([history[0].ObjectId, history[2].ObjectId]);
        host.HoverRequests.Should().Equal((main.GitRef, 0, 0, 8));

        // Unchanged or superseded: the highlight stays.
        host.HoverThrows = true;
        await viewModel.SetHoverReferenceAsync(main.GitRef, 0, 0, 8);
        viewModel.HoverHighlightedIds.Should().NotBeNull();

        host.HoverThrows = false;
        host.HoverResult = null;
        await viewModel.SetHoverReferenceAsync(null, -1, 0, 8);
        viewModel.HoverHighlightedIds.Should().BeNull();
    }

    [Test]
    public async Task The_avatars_are_loaded_once_per_author_when_the_column_is_shown()
    {
        List<GitRevision> history = CreateHistory();
        FakeHost host = new(history);
        RevisionGridViewModel viewModel = new(host, _options);
        viewModel.Load();

        viewModel.RequestAvatar(viewModel.Rows[0]);
        host.AvatarRequests.Should().BeEmpty("the column is hidden");

        viewModel.ShowAvatarColumn = true;
        foreach (RevisionGridRow row in viewModel.Rows)
        {
            viewModel.RequestAvatar(row);
        }

        await Task.Yield();
        host.AvatarRequests.Should().BeEquivalentTo(["alice@example.com", "bob@example.com", "carol@example.com"]);
        viewModel.Rows.Should().OnlyContain(r => r.Avatar != null && r.Avatar[0] == (byte)r.AuthorName[0]);
    }

    [Test]
    public void The_hotkeys_navigate_in_the_grid_and_run_the_other_commands_of_the_window()
    {
        List<GitRevision> history = RevisionGridViewTests.CreateHistory();
        RevisionGridViewModel viewModel = new(new FakeHost(history), _options)
        {
            Hotkeys = [new HotkeyBinding((int)RevisionGridCommand.GoToParent, HotkeyBinding.Control | 'P')],
        };
        List<RevisionGridCommand> handled = [];
        viewModel.CommandHandler = command =>
        {
            handled.Add(command);
            return command == RevisionGridCommand.ShowStashes;
        };
        viewModel.Load();
        viewModel.SelectedRow = viewModel.Rows[0];

        viewModel.GetHotkey(RevisionGridCommand.GoToParent)!.KeyData.Should().Be(HotkeyBinding.Control | 'P');
        viewModel.ExecuteHotkey(RevisionGridCommand.GoToParent).Should().BeTrue();
        viewModel.SelectedRow!.ObjectId.Should().Be(history[0].FirstParentId);
        viewModel.ExecuteHotkey(RevisionGridCommand.NavigateBackward_AlternativeHotkey).Should().BeTrue();
        viewModel.SelectedRow.Should().BeSameAs(viewModel.Rows[0]);
        viewModel.ExecuteHotkey(RevisionGridCommand.ToggleHighlightSelectedBranch).Should().BeTrue();
        viewModel.IsBranchHighlighted.Should().BeTrue();

        viewModel.ExecuteHotkey(RevisionGridCommand.ShowStashes).Should().BeTrue();
        viewModel.ExecuteHotkey(RevisionGridCommand.DeleteRef).Should().BeFalse();
        handled.Should().Equal(RevisionGridCommand.ShowStashes, RevisionGridCommand.DeleteRef);
    }

    /// <summary>Alice, Bob and Carol, with their emails; newest first.</summary>
    private static List<GitRevision> CreateHistory()
    {
        GitRevision root = Revision("Initial commit", "Alice");
        GitRevision second = Revision("Second", "Bob", root);
        GitRevision third = Revision("Third", "Carol", second);
        GitRevision fourth = Revision("Fourth", "Bob", third);
        return [fourth, third, second, root];

        static GitRevision Revision(string subject, string author, params GitRevision[] parents)
            => new(ObjectId.Random())
            {
                Subject = subject,
                Author = author,
                AuthorEmail = $"{author.ToLowerInvariant()}@example.com",
                ParentIds = [.. parents.Select(p => p.ObjectId)],
            };
    }

    private sealed class FakeHost(IReadOnlyList<GitRevision> revisions) : IRevisionGridHost
    {
        public string CurrentBranch => "main";

        public string UserEmail { get; init; } = "";

        public List<string> OpenedUrls { get; } = [];

        public List<string> AvatarRequests { get; } = [];

        public List<(IGitRef? Ref, int Row, int First, int Count)> HoverRequests { get; } = [];

        public IReadOnlySet<ObjectId>? HoverResult { get; set; }

        public bool HoverThrows { get; set; }

        public bool MatchesQuickSearch(GitRevision revision, string criteria) => false;

        public void LoadRevisions(RevisionGraph graph, Action reportBatch, Action<Exception?> completed, CancellationToken cancellationToken)
        {
            graph.HeadId = revisions[0].ObjectId;
            foreach (GitRevision revision in revisions)
            {
                graph.Add(revision);
            }

            reportBatch();
            completed(null);
        }

        public void RunInBackground(Action work, Action then)
        {
            work();
            then();
        }

        public void OpenUrl(string url) => OpenedUrls.Add(url);

        public string GetAuthorToolTip(GitRevision revision) => $"tooltip of {revision.Author}";

        public Task<byte[]?> GetAvatarAsync(string email, string? name, int size)
        {
            AvatarRequests.Add(email);
            return Task.FromResult<byte[]?>([(byte)name![0]]);
        }

        public Task<IReadOnlySet<ObjectId>?> GetHoverHighlightAsync(RevisionGraph graph, IGitRef? gitRef, int rowIndex, int firstVisibleRow, int visibleRowCount)
        {
            HoverRequests.Add((gitRef, rowIndex, firstVisibleRow, visibleRowCount));
            return HoverThrows ? Task.FromException<IReadOnlySet<ObjectId>?>(new OperationCanceledException()) : Task.FromResult(HoverResult);
        }
    }
}
