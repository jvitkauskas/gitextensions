using GitCommands.Settings;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Editor;
using GitUI.Presentation.Services;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUIPluginInterfaces;
using static GitUI.AvaloniaTests.ViewModels.FileStatusListViewModelTests;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the diff dialog (port of <c>FormDiff</c>) and the file viewer.</summary>
[TestFixture]
public sealed class DiffViewModelTests
{
    private static readonly GitRevision MergeBase = new(ObjectId.Parse("c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3"));

    [Test]
    public async Task Shows_the_files_of_the_second_commit_compared_to_the_first_and_the_selected_diff()
    {
        FakeHost host = new();
        FakeViewerHost viewer = new();
        DiffViewModel viewModel = Create(host, viewer);

        await viewModel.InitializeAsync();

        host.Requests.Should().Equal($"{Second.ObjectId.ToShortString()} vs {First.ObjectId.ToShortString()}");
        viewModel.Files.Nodes.Should().NotBeEmpty();
        await viewer.Shown;
        viewer.Requested.Should().Equal("docs/readme.md");
        viewModel.Viewer.Editor.Text.Should().Be("diff of docs/readme.md");
        viewModel.Viewer.Editor.DiffLines.Should().NotBeNull();
        viewModel.CanCompareDirectories.Should().BeTrue();
        viewModel.CompareToMergeBaseText.Should().Be("Compare to merge _base (c3c3c3c3)");
    }

    [Test]
    public async Task Compares_to_the_merge_base_and_swaps_the_commits()
    {
        FakeHost host = new();
        DiffViewModel viewModel = Create(host, new FakeViewerHost());
        await viewModel.InitializeAsync();

        // The fake host completes at once, so the files are loaded when the property is set.
        viewModel.CompareToMergeBase = true;
        host.Requests[^1].Should().Be($"{Second.ObjectId.ToShortString()} vs {MergeBase.ObjectId.ToShortString()}");

        viewModel.CompareToMergeBase = false;
        await viewModel.SwapCommand.ExecuteAsync(null);
        host.Requests[^1].Should().Be($"{First.ObjectId.ToShortString()} vs {Second.ObjectId.ToShortString()}");
        viewModel.FirstDisplayName.Should().Be("feature");
        viewModel.SecondDisplayName.Should().Be("main");

        viewModel.CompareDirectoriesCommand.Execute(null);
        host.DirectoryDiffs.Should().Equal($"{Second.Guid}..{First.Guid}");
    }

    [Test]
    public async Task Picks_another_branch_or_commit()
    {
        GitRevision other = new(ObjectId.Parse("d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4")) { Subject = "Other commit" };
        FakeHost host = new() { Branch = ("release", other), Commit = other };
        DiffViewModel viewModel = Create(host, new FakeViewerHost());

        await viewModel.PickFirstBranchCommand.ExecuteAsync(null);
        viewModel.FirstDisplayName.Should().Be("release");
        viewModel.FirstRevision.Should().BeSameAs(other);

        await viewModel.PickSecondCommitCommand.ExecuteAsync(null);
        viewModel.SecondDisplayName.Should().Be("Other commit");
        host.Requests[^1].Should().Be($"{other.ObjectId.ToShortString()} vs {other.ObjectId.ToShortString()}");

        host.Commit = null;
        await viewModel.PickFirstCommitCommand.ExecuteAsync(null);
        viewModel.FirstDisplayName.Should().Be("release", "the choice was cancelled");
    }

    [Test]
    public void Working_directory_as_the_first_commit_cannot_be_compared_with_the_directory_diff_tool()
    {
        DiffViewModel viewModel = new(
            new DiffStrings(), new FakeHost(), new FakeViewerHost(), new FileStatusListStrings(), new FileStatusTreeOptions(),
            new GitRevision(ObjectId.WorkTreeId), Second, "Working directory", "main", mergeBase: null);

        viewModel.CanCompareDirectories.Should().BeFalse();
        viewModel.HasMergeBase.Should().BeFalse();
    }

    [Test]
    public void File_viewer_shows_texts_images_and_git_colored_diffs()
    {
        FileViewerViewModel viewer = new(new FakeViewerHost());

        viewer.Show(new FileViewContent(FileViewKind.Text, "using System;", "Program.cs"));
        viewer.Editor.FileName.Should().Be("Program.cs");
        viewer.Editor.DiffLines.Should().BeNull();

        viewer.Show(new FileViewContent(FileViewKind.Diff, "\u001b[1mdiff --git a/a b/a\u001b[m\n@@ -1 +1 @@\n\u001b[7;31m-a\u001b[m\n\u001b[7;32m+b\u001b[m\n", HasGitColors: true));
        viewer.Editor.Text.Should().NotContain("\u001b");
        viewer.Editor.GitColoring.Should().NotBeNull();

        viewer.Show(new FileViewContent(FileViewKind.Image, "", "logo.png", Image: [1, 2, 3]));
        viewer.Image.Should().Equal(1, 2, 3);
        viewer.Show(FileViewContent.Empty);
        viewer.Image.Should().BeNull();
    }

    internal static DiffViewModel Create(FakeHost host, FakeViewerHost viewer)
        => new(new DiffStrings(), host, viewer, new FileStatusListStrings(), new FileStatusTreeOptions(), First, Second, "main", "feature", MergeBase);

    internal sealed class FakeHost : IDiffHost
    {
        public List<string> Requests { get; } = [];

        public List<string> DirectoryDiffs { get; } = [];

        public (string, GitRevision?)? Branch { get; init; }

        public GitRevision? Commit { get; set; }

        public Task<IReadOnlyList<FileStatusGroup>> GetDiffsAsync(IReadOnlyList<GitRevision> revisions, CancellationToken cancellationToken)
        {
            Requests.Add($"{revisions[0].ObjectId.ToShortString()} vs {revisions[1].ObjectId.ToShortString()}");
            return Task.FromResult<IReadOnlyList<FileStatusGroup>>([new FileStatusGroup(revisions[1], revisions[0], "", CreateStatuses())]);
        }

        public (string DisplayName, GitRevision? Revision)? PickBranch(GitRevision preselect) => Branch;

        public GitRevision? PickCommit(GitRevision preselect) => Commit;

        public void OpenDirectoryDiff(GitRevision first, GitRevision second) => DirectoryDiffs.Add($"{first.Guid}..{second.Guid}");
    }

    internal sealed class FakeViewerHost : IFileViewerHost
    {
        private readonly TaskCompletionSource _shown = new();

        public List<string> Requested { get; } = [];

        public Task Shown => _shown.Task;

        public IThemeColors ThemeColors { get; set; } = DefaultThemeColors.Instance;

        public bool ReverseGitColoring => true;

        public FileViewerSettings Settings { get; set; } = new();

        public IReadOnlyList<string> AvailableEncodings { get; } = ["UTF-8", "Western European (Windows)"];

        public string FilesEncoding => "UTF-8";

        public int SettingsOpened { get; private set; }

        public void OpenSettings() => SettingsOpened++;

        public DiffDisplayAppearance DiffAppearance { get; set; } = DiffDisplayAppearance.Patch;

        public bool IsDifftasticEnabled { get; set; }

        public int VerticalRulerPosition { get; set; }

        public List<string> DifftoolsOpened { get; } = [];

        public void OpenWithDifftool(FileStatusEntry entry) => DifftoolsOpened.Add(entry.Item.Name);

        /// <summary>The requests of the changes (with the options of the viewer).</summary>
        public List<FileViewRequest> Requests { get; } = [];

        /// <summary>The content returned instead of the diff, if any.</summary>
        public FileViewContent? Content { get; set; }

        public IReadOnlyList<HotkeyBinding> Hotkeys { get; set; } = [];

        /// <summary>Whether the changes support line patches.</summary>
        public bool SupportsLinePatching { get; set; }

        public List<string> Patches { get; } = [];

        public List<(string Text, bool AdjustLineEndings)> Copied { get; } = [];

        public bool ApplyLinePatch(LinePatchOperation operation, FileStatusEntry entry, StagedStatus stagedStatus, string text, int selectionStart, int selectionLength, byte[]? filePreamble)
        {
            Patches.Add($"{operation} {stagedStatus} {selectionStart}+{selectionLength}");
            return true;
        }

        public void CopyToClipboard(string text, bool adjustLineEndings) => Copied.Add((text, adjustLineEndings));

        /// <summary>The diff returned instead of the default one.</summary>
        public string? Diff { get; set; }

        public Task<FileViewContent> GetChangesAsync(FileStatusEntry entry, FileViewRequest request, CancellationToken cancellationToken)
        {
            Requested.Add(request.EncodingName is null ? entry.Item.Name : $"{entry.Item.Name} ({request.EncodingName})");
            Requests.Add(request);
            _shown.TrySetResult();
            return Task.FromResult(Content ?? new FileViewContent(FileViewKind.Diff, Diff ?? $"diff of {entry.Item.Name}", SupportsLinePatching: SupportsLinePatching, CanOpenWithDifftool: true));
        }

        public Task<FileViewContent> GetFileAsync(GitItemStatus file, ObjectId objectId, string? encodingName, CancellationToken cancellationToken)
        {
            Requested.Add($"{file.Name}@{objectId.ToShortString()}");
            _shown.TrySetResult();
            return Task.FromResult(new FileViewContent(FileViewKind.Text, $"{file.Name} in {objectId.ToShortString()}", file.Name));
        }
    }
}
