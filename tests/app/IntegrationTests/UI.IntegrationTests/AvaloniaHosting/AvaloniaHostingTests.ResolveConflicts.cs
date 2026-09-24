using GitCommands;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.CommandsDialogs;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 5: the merge conflicts dialog, with a real merge conflict.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void StartResolveConflictsDialog_chooses_the_local_side_of_a_merge_conflict()
    {
        string initial = _referenceRepository.CreateCommit("add the file", "base content", "conflict.txt");
        _referenceRepository.CreateCommit("master: change", "master content", "conflict.txt");
        _referenceRepository.CreateBranch("topic", initial);
        _referenceRepository.CheckoutBranch("topic");
        _referenceRepository.CreateCommit("topic: change", "topic content", "conflict.txt");
        _referenceRepository.CheckoutBranch("master");

        // A merge tool in the repository's own configuration (it overrides the user's), so that no message box
        // reports a missing merge tool; it is never run.
        IExecutable git = _referenceRepository.Module.GitExecutable;
        string mergeTool = Path.Combine(Environment.SystemDirectory, "cmd.exe").ToPosixPath();
        git.Execute("config merge.guitool avalonia-test-tool");
        git.Execute("config merge.tool avalonia-test-tool");
        git.Execute($"config mergetool.avalonia-test-tool.path {mergeTool.Quote()}");

        ExecutionResult merge = git.Execute("merge topic", throwOnErrorExit: false);
        merge.ExitCode.Should().NotBe(0, "the merge stops at the conflict");
        _referenceRepository.Module.InTheMiddleOfConflictedMerge().Should().BeTrue();

        IReadOnlyList<string> conflicts = [];
        string? localFileName = null;
        string? description = null;
        string? openMergeTool = null;
        DriveNextDialog(window =>
        {
            ResolveConflictsViewModel viewModel = (ResolveConflictsViewModel)window.DataContext!;
            conflicts = [.. viewModel.Conflicts.Select(c => c.Filename)];
            localFileName = viewModel.LocalFileName;
            description = viewModel.ConflictDescription;
            openMergeTool = viewModel.OpenMergeToolButtonText;
            Capture(window, "resolve-conflicts");
            viewModel.ChooseLocalCommand.Execute(null);
        });

        _commands.StartResolveConflictsDialog(_owner, offerCommit: false).Should().BeTrue();

        conflicts.Should().Equal("conflict.txt");
        localFileName.Should().Be("conflict.txt");
        description.Should().Be("The file has been changed both locally (ours) and remotely (theirs). Merge the changes.");
        openMergeTool.Should().Be("Open in avalonia-test-tool");
        _referenceRepository.Module.InTheMiddleOfConflictedMerge().Should().BeFalse("the conflict is resolved");
        File.ReadAllText(Path.Combine(_referenceRepository.Module.WorkingDir, "conflict.txt")).Should().Be("master content", "the local side is chosen");
        git.GetOutput("ls-files --unmerged").Should().BeEmpty("the resolved file is staged");
        git.GetOutput("status --porcelain").Should().NotContain("conflict.txt", "the local side is the version of HEAD");
        File.Exists(Path.Combine(_referenceRepository.Module.WorkingDirGitDir, "MERGE_HEAD")).Should().BeTrue("the merge is still to be committed");
    }
}
