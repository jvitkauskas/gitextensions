using GitCommands;
using GitUI.AvaloniaHosting;
using GitUI.Presentation.HelperDialogs;
using GitUIPluginInterfaces;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 4: the choose commit dialog on the Avalonia revision grid, with real git.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void Choose_commit_lists_the_history_preselects_a_commit_and_returns_the_chosen_one()
    {
        string first = _referenceRepository.CommitHash!;
        _referenceRepository.CreateBranch("feature", first);
        string second = _referenceRepository.CreateCommit("Second commit");
        string third = _referenceRepository.CreateCommit("Third commit");
        bool showCurrentBranchOnly = AppSettings.ShowCurrentBranchOnly.Value;

        List<string>? subjects = null;
        string? preselected = null;
        DriveNextDialog(window =>
        {
            ChooseCommitViewModel viewModel = (ChooseCommitViewModel)window.DataContext!;
            WaitUntil(() => !viewModel.Grid.IsLoading, () =>
            {
                subjects = [.. viewModel.Grid.Rows.Select(r => r.Subject)];
                preselected = viewModel.Grid.SelectedRow?.Revision.Guid;
                viewModel.Grid.Rows[0].Refs.Select(r => r.Name).Should().Contain("master");
                Capture(window, "choose-commit");

                viewModel.Grid.SelectRevision(viewModel.Grid.Rows[1].ObjectId);
                viewModel.OkCommand.Execute(null);
            });
        });

        AvaloniaDialogs.TryChooseCommit(_owner, _commands, second, out GitRevision? chosen, showCurrentBranchOnly: true).Should().BeTrue();

        subjects.Should().Equal("Third commit", "Second commit", _referenceRepository.Module.GetRevision(GitExtensions.Extensibility.Git.ObjectId.Parse(first)).Subject);
        preselected.Should().Be(second);
        chosen!.Guid.Should().Be(second);
        third.Should().NotBe(second);
        AppSettings.ShowCurrentBranchOnly.Value.Should().Be(showCurrentBranchOnly, "the main grid's setting is kept");
    }

    [Test]
    public void Choose_commit_with_artificial_commits_stays_on_WinForms()
    {
        AvaloniaDialogs.TryChooseCommit(_owner, _commands, null, out _, showArtificial: true).Should().BeFalse();
    }
}
