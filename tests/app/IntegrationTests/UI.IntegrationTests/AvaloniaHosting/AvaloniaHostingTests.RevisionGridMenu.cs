using CommonTestUtils;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.CommandsDialogs.BrowseDialog;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Services;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 4: the context menu of the revision grid in the Avalonia main window, built for a real repository.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void The_grid_menu_lists_the_actions_of_the_selected_commit_and_navigates()
    {
        string first = _referenceRepository.CommitHash!;
        _referenceRepository.CreateBranch("feature", first);
        _referenceRepository.CreateTag("v1.0", first);
        _referenceRepository.CreateCommit("Second commit", "changed content", "file.txt");
        string head = _referenceRepository.CommitHash!;

        List<string> headers = [];
        List<string> copyHeaders = [];
        List<string> checkoutBranches = [];
        ObjectId? afterGoToParent = null;
        bool closed = false;
        DriveNextDialog(window =>
        {
            window.Closed += (_, _) => closed = true;
            BrowseViewModel viewModel = (BrowseViewModel)window.DataContext!;
            WaitUntil(
                () => viewModel.Grid.Rows.Any(r => r.ObjectId.ToString() == head),
                () =>
                {
                    viewModel.Grid.SelectRevision(ObjectId.Parse(first));
                    IReadOnlyList<MenuModelItem> menu = viewModel.Grid.ContextMenuProvider!();
                    headers.AddRange(menu.Select(m => m.Header));
                    copyHeaders.AddRange(menu.Single(m => m.Header == "_Copy to clipboard").Children!.Select(m => m.Header));
                    checkoutBranches.AddRange(menu.Single(m => m.Header == "Chec_kout branch...").Children!.Where(m => !m.IsSeparator).Select(m => m.Header));

                    viewModel.Grid.SelectRevision(ObjectId.Parse(head));
                    MenuModelItem navigate = viewModel.Grid.ContextMenuProvider!().Single(m => m.Header == "_Navigate");
                    navigate.Children!.Single(m => m.Header == "Go to _parent commit").Execute!();
                    afterGoToParent = viewModel.Grid.SelectedRow?.ObjectId;
                    ((BrowseWindow)window).RevisionGrid.OpenContextMenu();
                    Capture(window, "browse-grid-menu");
                    ((BrowseWindow)window).RevisionGrid.LastContextMenu?.Close();
                    window.Close();
                });
        });

        _commands.StartBrowseDialog(_owner, new BrowseArguments()).Should().BeTrue();
        DateTime deadline = DateTime.UtcNow.AddSeconds(30);
        while (!closed && DateTime.UtcNow < deadline)
        {
            MessagePump.DoEvents();
            Thread.Sleep(10);
        }

        closed.Should().BeTrue();
        _driveFailure.Should().BeNull();
        headers.Should().ContainInOrder("_Copy to clipboard", "Chec_kout branch...", "_Merge into current branch...", "_Rebase current branch on", "Reset c_urrent branch to here...",
            "Create new branch here (_x)...", "_Delete branch...", "Create new ta_g here...", "_Delete tag...", "Checkout _this commit...", "Com_pare", "_Navigate", "View");
        string shortHash = first[..8];
        copyHeaders.Should().Contain(h => h.Contains("feature")).And.Contain(h => h.Contains("v1.0")).And.Contain(h => h.Contains(shortHash));
        checkoutBranches.Should().Contain("feature");
        afterGoToParent.Should().Be(ObjectId.Parse(first), "the parent of the second commit");
    }
}
