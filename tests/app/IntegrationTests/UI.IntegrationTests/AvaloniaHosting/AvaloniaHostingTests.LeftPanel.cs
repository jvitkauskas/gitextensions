using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.CommandsDialogs.BrowseDialog;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.UserControls.LeftPanel;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 7: the left panel of the Avalonia main window (the port of <c>RepoObjectsTree</c>) on a real repository.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void StartBrowseDialog_shows_the_branches_in_the_left_panel_and_selecting_one_selects_its_revision()
    {
        string first = _referenceRepository.CommitHash!;
        _referenceRepository.CreateBranch("feature/left-panel", first);
        _referenceRepository.CreateTag("v1.0", first);
        _referenceRepository.CreateCommit("Second commit", "changed content", "file.txt");
        string head = _referenceRepository.CommitHash!;
        string currentBranch = _referenceRepository.Module.GetSelectedBranch();

        List<string> branches = [];
        List<string> tags = [];
        string? boldBranch = null;
        ObjectId? selectedRevision = null;
        object? selectedInTree = null;
        bool closed = false;
        DriveNextDialog(window =>
        {
            window.Should().BeOfType<BrowseWindow>();
            window.Closed += (_, _) => closed = true;
            BrowseViewModel viewModel = (BrowseViewModel)window.DataContext!;
            LeftPanelViewModel panel = viewModel.LeftPanel!;
            WaitUntil(
                () => !viewModel.Grid.IsLoading
                    && viewModel.Grid.Rows.Count > 1
                    && panel.BranchesTree.Descendants().OfType<LocalBranchNode>().Count() >= 2
                    && panel.TagsTree.Children.Count > 0,
                () =>
                {
                    branches.AddRange(panel.BranchesTree.Descendants().OfType<LocalBranchNode>().Select(n => n.FullPath));
                    tags.AddRange(panel.TagsTree.Descendants().OfType<TagNode>().Select(n => n.FullPath));
                    boldBranch = panel.BranchesTree.Descendants().OfType<LocalBranchNode>().SingleOrDefault(n => n.IsBold)?.FullPath;

                    // As a click on the node: the tree view selects it, and the grid its revision.
                    LocalBranchNode feature = panel.BranchesTree.Descendants().OfType<LocalBranchNode>().Single(n => n.FullPath == "feature/left-panel");
                    panel.ClickNode(feature, multiple: false, includingDescendants: false);
                    panel.SelectedNode = feature;
                    Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                    selectedRevision = viewModel.Grid.SelectedRow?.ObjectId;
                    selectedInTree = ((BrowseWindow)window).LeftPanel.Tree.SelectedItem;
                    Capture(window, "browse-left-panel");
                    window.Close();
                });
        });

        _commands.StartBrowseDialog(_owner, new BrowseArguments { SelectedId = ObjectId.Parse(head) }).Should().BeTrue();

        // With a message loop (as here, and once the application runs), the main window is modeless: wait for it.
        DateTime deadline = DateTime.UtcNow.AddSeconds(30);
        while (!closed && DateTime.UtcNow < deadline)
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }

        closed.Should().BeTrue("the window was driven and closed");
        _driveFailure.Should().BeNull();

        branches.Should().Contain([currentBranch, "feature/left-panel"]);
        boldBranch.Should().Be(currentBranch, "the current branch is bold");
        tags.Should().Contain("v1.0");
        selectedRevision.Should().Be(ObjectId.Parse(first), "selecting a branch selects its revision in the grid");
        selectedInTree.Should().BeOfType<LocalBranchNode>().Which.FullPath.Should().Be("feature/left-panel");
    }
}
