using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI.AvaloniaHosting;
using GitUI.Presentation.CommandsDialogs;
using GitUIPluginInterfaces;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>The commit dialogs of phase 2, batch 3, shown from their WinForms entry points with real git.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void StartRevertCommitDialog_reverts_the_commit()
    {
        string commit = _referenceRepository.CreateCommit("Change to revert", "changed content");
        GitRevision revision = _referenceRepository.Module.GetRevision(ObjectId.Parse(commit));

        DriveDialogs(
            window =>
            {
                RevertCommitViewModel viewModel = (RevertCommitViewModel)window.DataContext!;
                viewModel.IsMerge.Should().BeFalse();
                viewModel.Summary.Subject.Should().Be("Change to revert");
                viewModel.AutoCommit = true;
                Capture(window, "revert-commit");
                viewModel.RevertCommand.Execute(null);
            },
            AcknowledgeWhenDone);

        _commands.StartRevertCommitDialog(_owner, revision).Should().BeTrue();

        _referenceRepository.Module.GetRevision(loadRefs: false).Subject.Should().StartWith("Revert \"Change to revert\"");
    }

    [Test]
    public void StartCherryPickDialog_cherry_picks_the_commit()
    {
        bool autoCommit = AppSettings.CommitAutomaticallyAfterCherryPick;
        bool addReference = AppSettings.AddCommitReferenceToCherryPick;
        try
        {
            _referenceRepository.CreateBranch("feature", _referenceRepository.CommitHash!);
            _referenceRepository.CheckoutBranch("feature");
            string featureCommit = _referenceRepository.CreateCommit("Feature commit", "feature content", "feature.txt");
            _referenceRepository.CheckoutBranch("master");
            GitRevision revision = _referenceRepository.Module.GetRevision(ObjectId.Parse(featureCommit));

            DriveDialogs(
                window =>
                {
                    CherryPickViewModel viewModel = (CherryPickViewModel)window.DataContext!;
                    viewModel.AutoCommit = true;
                    viewModel.AddReference = true;
                    Capture(window, "cherry-pick");
                    viewModel.CherryPickCommand.Execute(null);
                },
                AcknowledgeWhenDone);

            _commands.StartCherryPickDialog(_owner, revision).Should().BeTrue();

            GitRevision head = _referenceRepository.Module.GetRevision(loadRefs: false);
            head.Subject.Should().Be("Feature commit");
            head.Guid.Should().NotBe(featureCommit);
            AppSettings.AddCommitReferenceToCherryPick.Should().BeTrue("the options are saved when accepted");
        }
        finally
        {
            AppSettings.CommitAutomaticallyAfterCherryPick = autoCommit;
            AppSettings.AddCommitReferenceToCherryPick = addReference;
        }
    }

    [Test]
    public void Reset_current_branch_dialog_resets_softly()
    {
        string first = _referenceRepository.CommitHash!;
        _referenceRepository.CreateCommit("Second commit", "second content");
        GitRevision target = _referenceRepository.Module.GetRevision(ObjectId.Parse(first));

        DriveDialogs(
            window =>
            {
                ResetCurrentBranchViewModel viewModel = (ResetCurrentBranchViewModel)window.DataContext!;
                viewModel.IsSoft.Should().BeTrue();
                Capture(window, "reset-current-branch");
                viewModel.OkCommand.Execute(null);
            },
            AcknowledgeWhenDone);

        AvaloniaDialogs.TryShowResetCurrentBranch(_owner, _commands, target, GitUI.ResetCurrentBranchType.Soft, out bool reset).Should().BeTrue();

        reset.Should().BeTrue();
        _referenceRepository.Module.RevParse("HEAD").ToString().Should().Be(first);
    }

    [Test]
    public void Reset_another_branch_dialog_fast_forwards_the_branch()
    {
        bool checkoutAfterReset = AppSettings.CheckoutOtherBranchAfterReset.Value;
        try
        {
            _referenceRepository.CreateBranch("other", _referenceRepository.CommitHash!);
            string second = _referenceRepository.CreateCommit("Second commit", "second content");
            GitRevision target = _referenceRepository.Module.GetRevision(ObjectId.Parse(second), loadRefs: true);

            DriveDialogs(
                window =>
                {
                    ResetAnotherBranchViewModel viewModel = (ResetAnotherBranchViewModel)window.DataContext!;
                    viewModel.Branches.Should().Equal("other");
                    viewModel.Branch = "other";
                    viewModel.CheckoutAfterReset = false;
                    viewModel.IsNonFastForward.Should().BeFalse();
                    Capture(window, "reset-another-branch");
                    viewModel.OkCommand.Execute(null);
                },
                AcknowledgeWhenDone);

            AvaloniaDialogs.TryShowResetAnotherBranch(_owner, _commands, target, out bool reset).Should().BeTrue();

            reset.Should().BeTrue();
            _referenceRepository.Module.RevParse("other").ToString().Should().Be(second);
        }
        finally
        {
            AppSettings.CheckoutOtherBranchAfterReset.Value = checkoutAfterReset;
        }
    }

    [Test]
    public void StartArchiveDialog_shows_the_revision()
    {
        GitRevision revision = _referenceRepository.Module.GetRevision(ObjectId.Parse(_referenceRepository.CommitHash!));
        string? title = null;

        DriveNextDialog(window =>
        {
            ArchiveViewModel viewModel = (ArchiveViewModel)window.DataContext!;
            title = viewModel.Summary.Title;
            viewModel.IsPathFilterEnabled.Should().BeTrue();
            Capture(window, "archive");
            window.Close();
        });

        _commands.StartArchiveDialog(_owner, revision, path: "src").Should().BeTrue();

        title.Should().Be(revision.ObjectId.ToShortString());
    }
}
