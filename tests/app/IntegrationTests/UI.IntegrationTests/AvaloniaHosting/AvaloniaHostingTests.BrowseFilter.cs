using GitExtensions.Extensibility;
using GitUI.Avalonia.CommandsDialogs.BrowseDialog;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.UserControls.RevisionGrid;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 7: the filter toolbar of the Avalonia main window, filtering the revisions of a real repository.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void The_filter_toolbar_filters_the_revisions_by_text_and_by_branch()
    {
        Environment.SetEnvironmentVariable(AvaloniaUi.EnvironmentVariable, "all,FormBrowse");
        string first = _referenceRepository.CommitHash!;
        _referenceRepository.CreateBranch("feature", first);
        _referenceRepository.CreateCommit("Second commit", "changed content", "file.txt");
        _referenceRepository.CreateCommit("Third commit", "more content", "file.txt");

        List<string> byText = [];
        List<string> byBranch = [];
        List<string> reset = [];
        bool closed = false;
        DriveNextDialog(window =>
        {
            window.Closed += (_, _) => closed = true;
            BrowseViewModel viewModel = (BrowseViewModel)window.DataContext!;
            FilterToolBarViewModel filters = viewModel.Filters!;
            List<string> Subjects() => [.. viewModel.Grid.Rows.Where(r => !r.Revision.IsArtificial).Select(r => r.Subject)];
            WaitUntil(
                () => Subjects().Count == 3,
                () =>
                {
                    filters.RevisionFilter = "Second";
                    filters.ApplyRevisionFilter();
                    WaitUntil(
                        () => Subjects().Count == 1,
                        () =>
                        {
                            byText.AddRange(Subjects());
                            filters.SetRevisionFilter("");
                            filters.BranchFilter = "feature";
                            filters.ApplyBranchFilter();
                            WaitUntil(
                                () => Subjects().Count == 1 && filters.State.ShowFilteredBranches,
                                () =>
                                {
                                    byBranch.AddRange(Subjects());
                                    Capture(window, "browse-filter");
                                    filters.ResetAllFiltersCommand.Execute(null);
                                    WaitUntil(
                                        () => Subjects().Count == 3,
                                        () =>
                                        {
                                            reset.AddRange(Subjects());
                                            window.Close();
                                        });
                                });
                        });
                });
        });

        _commands.StartBrowseDialog(_owner, new BrowseArguments()).Should().BeTrue();
        DateTime deadline = DateTime.UtcNow.AddSeconds(60);
        while (!closed && DateTime.UtcNow < deadline)
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }

        closed.Should().BeTrue();
        _driveFailure.Should().BeNull();
        byText.Should().Equal("Second commit");
        byBranch.Should().Equal("A commit message");
        reset.Should().HaveCount(3);
    }
}
