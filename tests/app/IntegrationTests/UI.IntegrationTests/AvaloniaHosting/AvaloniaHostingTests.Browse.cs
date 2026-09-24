using GitExtensions.Extensibility;
using GitUI.Avalonia.CommandsDialogs.BrowseDialog;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 7: the Avalonia main window (named in <c>GE_AVALONIA</c> until it is complete) on a real repository.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void StartBrowseDialog_shows_the_revisions_the_commit_and_its_diff()
    {
        Environment.SetEnvironmentVariable(AvaloniaUi.EnvironmentVariable, "all,FormBrowse");
        _referenceRepository.CreateCommit("Second commit", "changed content", "file.txt");
        string head = _referenceRepository.CommitHash!;

        string? title = null;
        List<string> subjects = [];
        List<string> files = [];
        string? commitInfo = null;
        bool closed = false;
        DriveNextDialog(window =>
        {
            window.Should().BeOfType<BrowseWindow>();
            window.Closed += (_, _) => closed = true;
            BrowseViewModel viewModel = (BrowseViewModel)window.DataContext!;
            WaitUntil(
                () => viewModel.Grid.Rows.Count > 0 && viewModel.Files.AllEntries.Any() && viewModel.CommitInfo.RevisionInfo.Length > 0,
                () =>
                {
                    title = window.Title;
                    subjects.AddRange(viewModel.Grid.Rows.Select(r => r.Subject));
                    files.AddRange(viewModel.Files.AllEntries.Select(e => e.Item.Name));
                    commitInfo = viewModel.CommitInfo.RevisionInfo;
                    ((BrowseWindow)window).Tabs.SelectedIndex = 1;
                    Capture(window, "browse");
                    window.Close();
                });
        });

        _commands.StartBrowseDialog(_owner, new BrowseArguments { SelectedId = GitExtensions.Extensibility.Git.ObjectId.Parse(head) }).Should().BeTrue();

        // With a message loop (as here, and once the application runs), the main window is modeless: wait for it.
        DateTime deadline = DateTime.UtcNow.AddSeconds(30);
        while (!closed && DateTime.UtcNow < deadline)
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }

        closed.Should().BeTrue("the window was driven and closed");
        _driveFailure.Should().BeNull();

        title.Should().NotBeNull("the title of IAppTitleGenerator (a mock here)");
        subjects.Should().Contain("Second commit");
        files.Should().Contain("file.txt", "the diff of the selected commit");
        commitInfo.Should().NotBeNullOrEmpty();
    }
}
