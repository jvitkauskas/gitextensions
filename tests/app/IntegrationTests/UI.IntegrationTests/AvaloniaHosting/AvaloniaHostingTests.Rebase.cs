using GitCommands;
using GitExtensions.Extensibility;
using GitUI.Presentation.CommandsDialogs;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 5: the rebase and apply patch dialogs, with a real repository.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void StartRebaseDialog_rebases_the_current_branch()
    {
        string initial = _referenceRepository.CommitHash!;
        string masterTip = _referenceRepository.CreateCommit("master: change", "master content", "master.txt");
        _referenceRepository.CreateBranch("topic", initial);
        _referenceRepository.CheckoutBranch("topic");
        _referenceRepository.CreateCommit("topic: change", "topic content", "topic.txt");

        string? branch = null;
        string? currentBranch = null;
        IReadOnlyList<string> branches = [];
        DriveDialogs(
            window =>
            {
                RebaseViewModel viewModel = (RebaseViewModel)window.DataContext!;
                branch = viewModel.Branch;
                currentBranch = viewModel.CurrentBranch;
                branches = viewModel.Branches;
                viewModel.IsRebaseVisible.Should().BeTrue();
                Capture(window, "rebase");
                viewModel.RebaseCommand.Execute(null);
            },
            AcknowledgeWhenDone);

        _commands.StartRebaseDialog(_owner, onto: "master").Should().BeTrue();

        branch.Should().Be("master");
        currentBranch.Should().Be("topic");
        branches.Should().Contain("master").And.Contain("topic");
        _referenceRepository.Module.RevParse("HEAD~").ToString().Should().Be(masterTip, "topic is rebased on master");
        _referenceRepository.Module.InTheMiddleOfRebase().Should().BeFalse();
    }

    [Test]
    public void StartTheContinueRebaseDialog_shows_the_commits_stopped_by_a_conflict_and_aborts()
    {
        RebaseScenarios.StartConflictingRebase(_referenceRepository);

        bool isRebasing = false;
        bool hasConflicts = false;
        IReadOnlyList<string> statuses = [];
        IReadOnlyList<string?> subjects = [];
        DriveDialogs(
            window =>
            {
                RebaseViewModel viewModel = (RebaseViewModel)window.DataContext!;
                isRebasing = viewModel.IsRebasing;
                hasConflicts = viewModel.HasConflicts;
                statuses = [.. viewModel.PatchGrid.Patches.Select(p => p.Status)];
                subjects = [.. viewModel.PatchGrid.Patches.Select(p => p.Subject?.Trim())];
                Capture(window, "rebase-conflicts");
                viewModel.AbortCommand.Execute(null);
            },
            AcknowledgeWhenDone);

        _commands.StartTheContinueRebaseDialog(_owner).Should().BeTrue();

        isRebasing.Should().BeTrue();
        hasConflicts.Should().BeTrue();
        statuses.Should().Equal("Applied", "Applying...", "");
        subjects.Should().Equal("topic: commit that will apply", "topic: commit that will conflict", "topic: commit to do");
        _referenceRepository.Module.InTheMiddleOfRebase().Should().BeFalse("the rebase is aborted");
        _referenceRepository.Module.GetSelectedBranch().Should().Be("topic");
    }

    [Test]
    public void StartApplyPatchDialog_applies_a_patch_file()
    {
        string patchDirectory = Path.Combine(_referenceRepository.Module.WorkingDir, "..", $"patches-{Guid.NewGuid():N}");
        bool signOff = AppSettings.ApplyPatchSignOff;
        try
        {
            string initial = _referenceRepository.CommitHash!;
            _referenceRepository.CreateBranch("topic", initial);
            _referenceRepository.CheckoutBranch("topic");
            _referenceRepository.CreateCommit("Patch me", "patch content", "patched.txt");
            Directory.CreateDirectory(patchDirectory);
            _referenceRepository.Module.GitExecutable.Execute($"format-patch -1 -o {patchDirectory.ToPosixPath().Quote()}");
            _referenceRepository.CheckoutBranch("master");
            string patchFile = Directory.GetFiles(patchDirectory, "*.patch").Single();

            string? shownPatchFile = null;
            string? title = null;
            DriveDialogs(
                window =>
                {
                    ApplyPatchViewModel viewModel = (ApplyPatchViewModel)window.DataContext!;
                    shownPatchFile = viewModel.PatchFile;
                    title = window.Title;
                    viewModel.CanApply.Should().BeTrue();
                    viewModel.SignOff = true;
                    Capture(window, "apply-patch");
                    viewModel.ApplyCommand.Execute(null);
                },
                AcknowledgeWhenDone);

            _commands.StartApplyPatchDialog(_owner, patchFile).Should().BeTrue();

            shownPatchFile.Should().Be(patchFile);
            title.Should().Be($"Apply patch ({_referenceRepository.Module.WorkingDir})");
            _referenceRepository.Module.GetPreviousCommitMessages(1, "HEAD", "").Single()!.Trim().Should()
                .StartWith("Patch me").And.Contain("Signed-off-by: ", "the patch is signed off");
            _referenceRepository.Module.GetSelectedBranch().Should().Be("master");
            AppSettings.ApplyPatchSignOff.Should().BeTrue("the option is saved");
        }
        finally
        {
            AppSettings.ApplyPatchSignOff = signOff;
            Directory.Delete(patchDirectory, recursive: true);
        }
    }

    [Test]
    public void StartApplyPatchDialog_shows_the_patches_stopped_by_a_conflict_and_aborts()
    {
        string patchDirectory = Path.Combine(_referenceRepository.Module.WorkingDir, "..", $"patches-{Guid.NewGuid():N}");
        try
        {
            RebaseScenarios.StartConflictingAm(_referenceRepository, patchDirectory);

            bool isInPatch = false;
            bool hasConflicts = false;
            bool isInPatchAfterAbort = true;
            IReadOnlyList<string> patches = [];
            DriveDialogs(
                window =>
                {
                    ApplyPatchViewModel viewModel = (ApplyPatchViewModel)window.DataContext!;
                    isInPatch = viewModel.IsInPatch;
                    hasConflicts = viewModel.HasConflicts;
                    patches = [.. viewModel.PatchGrid.Patches.Select(p => $"{p.Name} {p.Status} {p.Subject}")];
                    Capture(window, "apply-patch-conflicts");
                    viewModel.AbortCommand.Execute(null);
                    isInPatchAfterAbort = viewModel.IsInPatch;
                    window.Close();
                },
                AcknowledgeWhenDone);

            _commands.StartApplyPatchDialog(_owner).Should().BeTrue();

            isInPatch.Should().BeTrue();
            hasConflicts.Should().BeTrue();
            patches.Should().Equal("0001 Applying... [PATCH 1/2] topic: first patch", "0002  [PATCH 2/2] topic: second patch");
            isInPatchAfterAbort.Should().BeFalse();
            _referenceRepository.Module.InTheMiddleOfPatch().Should().BeFalse("the patches are aborted");
        }
        finally
        {
            Directory.Delete(patchDirectory, recursive: true);
        }
    }
}
