using CommonTestUtils;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.CommandsDialogs.BrowseDialog;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 7: the file tree tab of the Avalonia main window, on a real repository.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void The_file_tree_tab_lists_the_files_of_the_selected_commit_and_shows_them()
    {
        _referenceRepository.CreateCommit("Second commit", "changed content", "file.txt");
        Directory.CreateDirectory(Path.Combine(_referenceRepository.Module.WorkingDir, "folder"));
        _referenceRepository.CreateCommit("Third commit", "nested content", "folder/nested.txt");
        string head = _referenceRepository.CommitHash!;

        List<string> files = [];
        string? shown = null;
        bool closed = false;
        DriveNextDialog(window =>
        {
            window.Closed += (_, _) => closed = true;
            BrowseViewModel viewModel = (BrowseViewModel)window.DataContext!;
            WaitUntil(
                () => viewModel.Grid.SelectedRow?.ObjectId.ToString() == head,
                () =>
                {
                    ((BrowseWindow)window).Tabs.SelectedIndex = 2;
                    WaitUntil(
                        () => viewModel.FileTree!.AllEntries.Any(),
                        () =>
                        {
                            files.AddRange(viewModel.FileTree!.AllEntries.Select(e => e.Item.Name));
                            viewModel.FileTree.Select(e => e.Item.Name == "folder/nested.txt");
                            WaitUntil(
                                () => viewModel.TreeViewer!.Editor.Text.Length > 0,
                                () =>
                                {
                                    shown = viewModel.TreeViewer!.Editor.Text;
                                    Capture(window, "browse-file-tree");
                                    window.Close();
                                });
                        });
                });
        });

        _commands.StartBrowseDialog(_owner, new BrowseArguments { SelectedId = ObjectId.Parse(head) }).Should().BeTrue();
        DateTime deadline = DateTime.UtcNow.AddSeconds(40);
        while (!closed && DateTime.UtcNow < deadline)
        {
            MessagePump.DoEvents();
            Thread.Sleep(10);
        }

        closed.Should().BeTrue();
        _driveFailure.Should().BeNull();
        files.Should().Contain(["file.txt", "folder/nested.txt"], "all the files of the commit, not only its changes");
        shown.Should().Contain("nested content");
    }
}
