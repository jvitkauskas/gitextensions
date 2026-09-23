using GitUI.AvaloniaHosting;
using GitUI.Presentation.CommandsDialogs.CommitDialog;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 5: the commit dialog, with a real repository.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void Commit_dialog_stages_and_commits_a_file()
    {
        _referenceRepository.CreateRepoFile("new.txt", "new content\n");
        _referenceRepository.CreateRepoFile("other.txt", "other content\n");

        List<string> unstaged = [];
        List<string> staged = [];
        string? title = null;
        DriveDialogs(
            window => WaitUntil(
                () => window.DataContext is CommitViewModel { IsLoading: false, CanCommit: true },
                () =>
                {
                    CommitViewModel viewModel = (CommitViewModel)window.DataContext!;
                    unstaged.AddRange(viewModel.Unstaged.AllEntries.Select(e => e.Item.Name));
                    viewModel.Unstaged.Select(e => e.Item.Name == "new.txt");
                    viewModel.StageCommand.Execute(null);
                    staged.AddRange(viewModel.Staged.AllEntries.Select(e => e.Item.Name));
                    viewModel.Message.Text = "Add the new file";
                    title = null;

                    // After a render of the changes, for the screenshot.
                    DateTime renderedAt = DateTime.UtcNow.AddMilliseconds(400);
                    WaitUntil(
                        () => window.Title?.StartsWith("Commit to") == true && DateTime.UtcNow > renderedAt,
                        () =>
                        {
                            title = window.Title;
                            Capture(window, "commit");
                            viewModel.CommitCommand.Execute(null);
                            window.Close();
                        });
                }),
            AcknowledgeWhenDone);

        AvaloniaDialogs.TryShowCommit(_owner, CreateCommandsWithPassingScripts()).Should().BeTrue();

        unstaged.Should().BeEquivalentTo("new.txt", "other.txt");
        staged.Should().Equal("new.txt");
        title.Should().StartWith("Commit to master (");
        _referenceRepository.Module.GetPreviousCommitMessages(1, "HEAD", "").Single()!.Trim().Should().Be("Add the new file");
        _referenceRepository.Module.GetIndexFilesWithSubmodulesStatus().Should().BeEmpty();
    }
}
