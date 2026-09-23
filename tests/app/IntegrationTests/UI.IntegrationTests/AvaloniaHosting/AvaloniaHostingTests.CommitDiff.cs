using GitExtensions.Extensibility.Git;
using GitUI.Presentation.HelperDialogs;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 5: the commit diff dialog (commit info, files and diff), with a real repository.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void Commit_diff_shows_the_commit_and_its_changes()
    {
        string commit = _referenceRepository.CreateCommit("Change A", "changed content of A", "A.txt");

        string? title = null;
        List<string> header = [];
        List<string> files = [];
        string? diff = null;
        DriveNextDialog(window => WaitUntil(
            () => window.DataContext is CommitDiffViewModel { Viewer.Editor.Text.Length: > 0 } model && model.Files.AllEntries.Any(),
            () =>
            {
                CommitDiffViewModel viewModel = (CommitDiffViewModel)window.DataContext!;
                title = window.Title;
                header.AddRange(viewModel.CommitInfo.Header.Select(l => l.Label));
                files.AddRange(viewModel.Files.AllEntries.Select(e => e.Item.Name));
                diff = viewModel.Viewer.Editor.Text;
                Capture(window, "commit-diff");
                window.Close();
            }));

        _commands.StartFormCommitDiff(ObjectId.Parse(commit)).Should().BeTrue();

        title.Should().StartWith($"Diff - {commit[..8]} - ");
        header.Should().Contain("Commit hash:");
        files.Should().Equal("A.txt");
        diff.Should().Contain("+changed content of A");
    }
}
