using GitCommands;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitUI.AvaloniaHosting;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs.BrowseDialog;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 2, batch 9: reflog and recent repositories settings, with real git.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void Reflog_lists_the_reflog_and_creates_a_branch_on_an_entry()
    {
        string firstCommit = _referenceRepository.CommitHash!;
        _referenceRepository.CreateCommit("Second commit");

        List<ReflogEntry>? entries = null;
        DriveDialogs(
            window =>
            {
                ReflogViewModel viewModel = (ReflogViewModel)window.DataContext!;
                WaitUntil(() => viewModel.Entries.Count >= 2, () =>
                {
                    entries = [.. viewModel.Entries];
                    Capture(window, "reflog");
                    viewModel.SelectedEntry = viewModel.Entries.Single(e => e.Sha == firstCommit);
                    viewModel.CreateBranchCommand.Execute(null);
                    window.Close();
                });
            },
            window =>
            {
                CreateBranchViewModel viewModel = (CreateBranchViewModel)window.DataContext!;
                viewModel.BranchName = "from-reflog";
                viewModel.CreateCommand.Execute(null);
            },
            AcknowledgeWhenDone);

        AvaloniaDialogs.TryShowReflog(_owner, CreateCommandsWithPassingScripts()).Should().BeTrue();

        entries.Should().NotBeNull();
        entries!.Select(e => e.Ref).Should().StartWith(["HEAD@{0}", "HEAD@{1}"]);
        IGitRef branch = _referenceRepository.Module.GetRefs(RefsFilter.Heads).Single(r => r.Name == "from-reflog");
        branch.ObjectId.ToString().Should().Be(firstCommit);
    }

    [Test]
    public void Recent_repositories_settings_are_saved_on_OK_only()
    {
        bool sortTopRepos = AppSettings.SortTopRepos;
        try
        {
            DriveNextDialog(window =>
            {
                RecentReposSettingsViewModel viewModel = (RecentReposSettingsViewModel)window.DataContext!;
                viewModel.SortTopRepos = !sortTopRepos;
                viewModel.CancelCommand.Execute(null);
            });
            AvaloniaDialogs.TryShowRecentReposSettings(_owner, out bool saved).Should().BeTrue();
            saved.Should().BeFalse();
            AppSettings.SortTopRepos.Should().Be(sortTopRepos);

            DriveNextDialog(window =>
            {
                RecentReposSettingsViewModel viewModel = (RecentReposSettingsViewModel)window.DataContext!;
                viewModel.SortTopRepos = !sortTopRepos;
                Capture(window, "recent-repos-settings");
                viewModel.OkCommand.Execute(null);
            });
            AvaloniaDialogs.TryShowRecentReposSettings(_owner, out saved).Should().BeTrue();
            saved.Should().BeTrue();
            AppSettings.SortTopRepos.Should().Be(!sortTopRepos);
        }
        finally
        {
            AppSettings.SortTopRepos = sortTopRepos;
        }
    }
}
