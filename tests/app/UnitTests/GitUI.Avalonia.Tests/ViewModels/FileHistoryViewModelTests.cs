using System.ComponentModel;
using GitExtensions.Extensibility.Git;
using GitUI.AvaloniaTests.Views;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.UserControls.Blame;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUIPluginInterfaces;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the file history (port of <c>FormFileHistory</c>).</summary>
[TestFixture]
public sealed class FileHistoryViewModelTests
{
    internal static readonly GitRevision Added = new(ObjectId.Parse("a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1")) { Subject = "Add the file", Author = "Alice", ParentIds = [] };

    internal static readonly GitRevision Renamed = new(ObjectId.Parse("b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2")) { Subject = "Rename the file", Author = "Bob", ParentIds = [Added.ObjectId] };

    internal static readonly GitRevision Changed = new(ObjectId.Parse("c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3")) { Subject = "Change the file", Author = "Carol", ParentIds = [Renamed.ObjectId] };

    internal static readonly GitRevision WorkTree = new(ObjectId.WorkTreeId) { Subject = "Working directory", ParentIds = [Changed.ObjectId] };

    [Test]
    public void The_history_is_loaded_on_show_with_the_first_real_revision_selected()
    {
        (FileHistoryViewModel viewModel, FakeHost host, DiffViewModelTests.FakeViewerHost viewer) = Create(history: [WorkTree, Changed, Renamed, Added]);

        viewModel.Title.Should().Be("File History - src/file.cs - C:\\repo");
        viewModel.IsGridVisible.Should().BeFalse();
        viewModel.Initialize();

        viewModel.IsGridVisible.Should().BeTrue();
        viewModel.Grid.SelectedRow!.Revision.Should().BeSameAs(Changed);
        viewModel.SelectedTab.Should().Be(FileHistoryTab.Diff);
        viewer.Requested.Should().Equal("src/file.cs");
        host.AvailabilityChecks.Should().Equal(("src/file.cs", Changed.ObjectId));
    }

    [Test]
    public void The_history_is_not_loaded_on_show_unless_set_so()
    {
        (FileHistoryViewModel viewModel, _, _) = Create(settings: new FakeSettings { LoadHistoryOnShow = false });

        viewModel.Initialize();
        viewModel.IsGridVisible.Should().BeFalse();
        viewModel.Grid.Rows.Should().BeEmpty();

        viewModel.LoadFileHistoryCommand.Execute(null);
        viewModel.IsGridVisible.Should().BeTrue();
        viewModel.Grid.Rows.Should().HaveCount(3);
    }

    [Test]
    public void The_blame_is_loaded_on_show_even_without_the_history_setting()
    {
        (FileHistoryViewModel viewModel, _, _) = Create(settings: new FakeSettings { LoadHistoryOnShow = false, LoadBlameOnShow = true }, showBlame: true);

        viewModel.Initialize();

        viewModel.SelectedTab.Should().Be(FileHistoryTab.Blame);
        viewModel.IsGridVisible.Should().BeTrue();
    }

    [Test]
    public void A_renamed_file_is_shown_with_its_name_in_the_revision()
    {
        (FileHistoryViewModel viewModel, _, DiffViewModelTests.FakeViewerHost viewer) = Create();
        viewModel.Initialize();

        Select(viewModel, Added);

        viewModel.Title.Should().Be("File History - src/file.cs (src/old.cs) - C:\\repo");
        viewer.Requested[^1].Should().Be("src/old.cs");
    }

    [Test]
    public void The_tabs_follow_the_revision_and_the_file()
    {
        (FileHistoryViewModel viewModel, FakeHost host, _) = Create(history: [WorkTree, Changed, Renamed, Added]);
        viewModel.Initialize();
        viewModel.SelectedTab = FileHistoryTab.Commit;

        // The artificial commits have no commit, file or blame; the diff is shown.
        Select(viewModel, WorkTree);
        viewModel.IsCommitTabVisible.Should().BeFalse();
        viewModel.IsViewTabVisible.Should().BeFalse();
        viewModel.IsBlameTabVisible.Should().BeFalse();
        viewModel.SelectedTab.Should().Be(FileHistoryTab.Diff);

        // Without the file, only the commit is shown.
        host.Missing.Add(Renamed.ObjectId);
        Select(viewModel, Renamed);
        viewModel.IsCommitTabVisible.Should().BeTrue();
        viewModel.IsDiffTabVisible.Should().BeFalse();
        viewModel.SelectedTab.Should().Be(FileHistoryTab.Commit);
        viewModel.CommitTabHeader.Should().Be("Commit - Git could not identify the file \"src/file.cs\"");

        Select(viewModel, Changed);
        viewModel.CommitTabHeader.Should().Be("Commit");
        viewModel.IsDiffTabVisible.Should().BeTrue();
        viewModel.IsViewTabVisible.Should().BeTrue();
        viewModel.IsBlameTabVisible.Should().BeTrue();
    }

    [Test]
    public async Task Each_tab_shows_the_selected_revision()
    {
        BlameViewModelTests.FakeHost blameHost = new();
        (FileHistoryViewModel viewModel, _, DiffViewModelTests.FakeViewerHost viewer) = Create(blameHost: blameHost);
        viewModel.Initialize();

        viewModel.SelectedTab = FileHistoryTab.View;
        viewer.Requested[^1].Should().Be("src/file.cs@c3c3c3c3");

        viewModel.SelectedTab = FileHistoryTab.Blame;
        blameHost.Blamed.Should().Equal(("src/file.cs", Changed.ObjectId));

        viewModel.SelectedTab = FileHistoryTab.Commit;
        await Task.Yield();
        viewModel.CommitDiff.CommitInfo.HasRevision.Should().BeTrue();
    }

    [Test]
    public async Task Blaming_a_revision_selects_it_in_the_grid()
    {
        (FileHistoryViewModel viewModel, _, _) = Create(blameHost: new BlameViewModelTests.FakeHost());
        viewModel.Initialize();
        viewModel.SelectedTab = FileHistoryTab.Blame;
        await Task.Yield();

        viewModel.Blame.RevisionGrid!.SelectFileInRevision(Renamed.ObjectId, "src/file.cs").Should().BeTrue();
        viewModel.Grid.SelectedRow!.Revision.Should().BeSameAs(Renamed);
        viewModel.Blame.RevisionGrid.SelectFileInRevision(ObjectId.Random(), "src/file.cs").Should().BeFalse("not listed");
    }

    [Test]
    public void Settings_are_saved_and_reload_what_they_change()
    {
        FakeSettings settings = new();
        (FileHistoryViewModel viewModel, _, _) = Create(settings: settings);
        viewModel.Initialize();
        int loads = 0;
        viewModel.Grid.Loaded += (_, _) => loads++;

        viewModel.FullHistory = true;
        settings.FullHistory.Should().BeTrue();
        loads.Should().Be(1);

        viewModel.SimplifyMerges = true;
        loads.Should().Be(2, "the full history is shown");

        viewModel.BlameShowAuthor = false;
        settings.BlameShowAuthor.Should().BeFalse();
        viewModel.BlameShowAuthorDate.Should().BeTrue("the author or its date is shown");

        viewModel.BlameShowAuthorDate = false;
        viewModel.BlameShowAuthor.Should().BeTrue();
        loads.Should().Be(2, "the blame options reload the blame only");
    }

    [Test]
    public void The_menu_follows_the_selection()
    {
        (FileHistoryViewModel viewModel, FakeHost host, _) = Create(history: [WorkTree, Changed, Renamed, Added]);
        viewModel.Initialize();

        viewModel.GetMenuState().Should().Be(new FileHistoryMenuState(CanDiffToLocal: true, CanOpenWithDifftool: true, CanManipulateCommit: true, CanSaveAs: true, CanCopy: true));

        Select(viewModel, WorkTree);
        viewModel.GetMenuState().Should().Be(new FileHistoryMenuState(CanDiffToLocal: false, CanOpenWithDifftool: true, CanManipulateCommit: false, CanSaveAs: true, CanCopy: false));

        Select(viewModel, Changed, Added);
        viewModel.GetMenuState().Should().Be(new FileHistoryMenuState(CanDiffToLocal: false, CanOpenWithDifftool: true, CanManipulateCommit: false, CanSaveAs: false, CanCopy: true));
        viewModel.OpenWithDifftoolCommand.Execute(false);
        host.Difftools.Should().Equal($"{Changed.ObjectId.ToShortString()},{Added.ObjectId.ToShortString()}:src/file.cs:src/file.cs:False");
    }

    [Test]
    public void Commands_act_on_the_selected_revision()
    {
        (FileHistoryViewModel viewModel, FakeHost host, _) = Create();
        viewModel.Initialize();
        Select(viewModel, Added);

        viewModel.SaveAsCommand.Execute(null);
        viewModel.CherryPickCommand.Execute(null);
        viewModel.RevertCommand.Execute(null);
        viewModel.ViewSelectedRevisions();

        host.Actions.Should().Equal("save a1a1a1a1 src/old.cs", "cherry-pick a1a1a1a1", "revert a1a1a1a1", "view a1a1a1a1");
    }

    [Test]
    public void The_links_of_the_blame_select_their_commit()
    {
        (FileHistoryViewModel viewModel, FakeHost host, _) = Create();
        viewModel.Initialize();
        viewModel.Blame.CommitInfo.OnLinkClicked("gitext://gotocommit/b2b2b2b2");
        viewModel.Grid.SelectedRow!.Revision.Should().BeSameAs(Renamed);

        host.Resolved[ObjectId.Parse("d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4")] = "unlisted";
        viewModel.Blame.CommitInfo.OnLinkClicked("gitext://gotobranch/unlisted");
        host.Actions.Should().Equal("filtered d4d4d4d4");
    }

    internal static (FileHistoryViewModel ViewModel, FakeHost Host, DiffViewModelTests.FakeViewerHost Viewer) Create(
        List<GitRevision>? history = null,
        FakeSettings? settings = null,
        BlameViewModelTests.FakeHost? blameHost = null,
        bool showBlame = false)
    {
        FakeHost host = new() { Settings = settings ?? new FakeSettings() };
        DiffViewModelTests.FakeViewerHost viewer = new();
        RevisionGridViewModel grid = new(new RevisionGridViewTests.FakeRevisionGridHost(history ?? [Changed, Renamed, Added]), new RevisionGridDisplayOptions(RelativeDate: true, ShowAuthorDate: false)) { MultiSelect = true };

        // As the view: the selected row is reported as the selection.
        grid.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(RevisionGridViewModel.SelectedRow) && grid.SelectedRow is { } row)
            {
                grid.SetSelectedRows([row]);
            }
        };
        FileHistoryViewModel viewModel = new(
            new FileHistoryStrings(),
            host,
            grid,
            CommitDiffViewModelTests.Create(new CommitDiffViewModelTests.FakeHost(), viewer),
            viewer,
            BlameViewModelTests.Create(blameHost ?? new BlameViewModelTests.FakeHost()),
            "\"src\\file.cs\"",
            showBlame: showBlame);
        return (viewModel, host, viewer);
    }

    /// <summary>Selects the revisions, the first one latest (as the WinForms grid lists them).</summary>
    internal static void Select(FileHistoryViewModel viewModel, params GitRevision[] revisions)
    {
        List<RevisionGridRow> rows = [.. revisions.Reverse().Select(r => viewModel.Grid.Rows.Single(row => row.Revision == r))];
        viewModel.Grid.SetSelectedRows(rows);
    }

    internal sealed class FakeSettings : IFileHistorySettings
    {
        public bool FollowRenames { get; set; } = true;

        public bool FollowRenamesExactOnly { get; set; }

        public bool FullHistory { get; set; }

        public bool SimplifyMerges { get; set; }

        public bool LoadHistoryOnShow { get; set; } = true;

        public bool LoadBlameOnShow { get; set; } = true;

        public bool IgnoreWhitespaceOnBlame { get; set; }

        public bool DetectCopyInFileOnBlame { get; set; }

        public bool DetectCopyInAllOnBlame { get; set; }

        public bool BlameDisplayAuthorFirst { get; set; }

        public bool BlameShowAuthorAvatar { get; set; } = true;

        public bool BlameShowAuthor { get; set; } = true;

        public bool BlameShowAuthorDate { get; set; } = true;

        public bool BlameShowAuthorTime { get; set; } = true;

        public bool BlameShowLineNumbers { get; set; }

        public bool BlameShowOriginalFilePath { get; set; } = true;
    }

    internal sealed class FakeHost : IFileHistoryHost
    {
        public required IFileHistorySettings Settings { get; init; }

        public HashSet<ObjectId> Missing { get; } = [];

        public List<(string FileName, ObjectId ObjectId)> AvailabilityChecks { get; } = [];

        public List<string> Difftools { get; } = [];

        public List<string> Actions { get; } = [];

        public Dictionary<ObjectId, string> Resolved { get; } = [];

        public string WorkingDirectory => @"C:\repo";

        public bool IsSubmodule => false;

        public string NoChangesText => "No changes";

        public string? GetFileName(GitRevision revision) => revision == Added ? "src/old.cs" : revision.IsArtificial ? null : "src/file.cs";

        public bool IsFileAvailable(string fileName, GitRevision revision)
        {
            AvailabilityChecks.Add((fileName, revision.ObjectId));
            return !Missing.Contains(revision.ObjectId);
        }

        public bool IsInWorkingDirectory(string fileName) => true;

        public GitRevision GetActualRevision(GitRevision revision) => revision;

        public GitRevision? GetRevision(ObjectId objectId) => null;

        public ObjectId? ResolveCommit(string commitOrRef, bool isRef)
            => isRef
                ? Resolved.FirstOrDefault(pair => pair.Value == commitOrRef).Key is { IsZero: false } id ? id : null
                : new[] { Added, Renamed, Changed }.FirstOrDefault(r => r.Guid.StartsWith(commitOrRef))?.ObjectId;

        public void OpenWithDifftool(IReadOnlyList<GitRevision> revisions, string fileName, string? revisionFileName, bool toLocal, string? customTool = null)
            => Difftools.Add($"{string.Join(",", revisions.Select(r => r.ObjectId.ToShortString()))}:{fileName}:{revisionFileName}:{toLocal}{(customTool is null ? "" : $":{customTool}")}");

        public void SaveAs(GitRevision revision, string fileName) => Actions.Add($"save {revision.ObjectId.ToShortString()} {fileName}");

        public void CherryPick(GitRevision revision) => Actions.Add($"cherry-pick {revision.ObjectId.ToShortString()}");

        public void Revert(GitRevision revision) => Actions.Add($"revert {revision.ObjectId.ToShortString()}");

        public void ViewRevisions(IReadOnlyList<GitRevision> revisions) => Actions.Add($"view {revisions[0].ObjectId.ToShortString()}");

        public void ShowGitCommandLog() => Actions.Add("log");

        public void ShowRevisionFiltered(ObjectId objectId) => Actions.Add($"filtered {objectId.ToShortString()}");

        public IReadOnlyList<RevisionCopyItem> GetCopyItems(IReadOnlyList<GitRevision> revisions)
            => [new RevisionCopyItem(RevisionCopyItemKind.Item, "Commit &hash", string.Join("\n", revisions.Select(r => r.Guid)))];

        public void CopyToClipboard(string text) => Actions.Add($"copy {text}");
    }
}
