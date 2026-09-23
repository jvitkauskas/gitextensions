using GitCommands;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI;
using GitUI.Presentation.CommandsDialogs;
using GitUI.ScriptsEngine;
using ResourceManager;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>The branch dialogs of phase 2, batch 3, shown from their WinForms entry points with real git.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void StartDeleteBranchDialog_deletes_a_merged_branch()
    {
        _referenceRepository.CreateBranch("to-delete", _referenceRepository.CommitHash!);

        DriveDialogs(
            window =>
            {
                DeleteBranchViewModel viewModel = (DeleteBranchViewModel)window.DataContext!;
                viewModel.Branches.Text.Should().Be("to-delete");
                Capture(window, "delete-branch");
                viewModel.DeleteCommand.Execute(null);
            },
            AcknowledgeWhenDone);

        _commands.StartDeleteBranchDialog(_owner, "to-delete").Should().BeTrue();

        _referenceRepository.Module.GetRefs(RefsFilter.Heads).Select(r => r.Name).Should().NotContain("to-delete");
    }

    [Test]
    public void StartMergeBranchDialog_merges_the_branch()
    {
        System.ComponentModel.Design.ServiceContainer services = GlobalServiceContainer.CreateDefaultMockServiceContainer();
        services.RemoveService(typeof(IScriptsRunner));
        services.AddService<IScriptsRunner>(new PassingScriptsRunner());
        GitUICommands commands = new(services, _referenceRepository.Module);
        _referenceRepository.CreateBranch("feature", _referenceRepository.CommitHash!);
        _referenceRepository.CheckoutBranch("feature");
        string featureCommit = _referenceRepository.CreateCommit("Feature commit", "feature content");
        _referenceRepository.CheckoutBranch("master");

        DriveDialogs(
            window =>
            {
                MergeBranchViewModel viewModel = (MergeBranchViewModel)window.DataContext!;
                viewModel.CurrentBranch.Should().Be("master");
                viewModel.Branches.Text = "feature";
                Capture(window, "merge-branch");
                viewModel.MergeCommand.Execute(null);
            },
            AcknowledgeWhenDone);

        commands.StartMergeBranchDialog(_owner, branch: null).Should().BeTrue();

        _referenceRepository.Module.RevParse("master").ToString().Should().Be(featureCommit, "the merge fast-forwards");
    }

    [Test]
    public void StartDeleteRemoteBranchDialog_lists_the_remote_branches()
    {
        _referenceRepository.CreateRemoteForBranch();
        _referenceRepository.Module.GitExecutable.GetOutput("update-ref refs/remotes/origin/master HEAD");
        string? remoteBranch = null;
        IReadOnlyList<IGitRef> remoteRefs = _referenceRepository.Module.GetRefs(RefsFilter.Remotes);
        remoteRefs.Should().NotBeEmpty();

        DriveNextDialog(window =>
        {
            DeleteRemoteBranchViewModel viewModel = (DeleteRemoteBranchViewModel)window.DataContext!;
            remoteBranch = viewModel.Branches.Text;
            viewModel.Branches.Branches.Should().Contain(remoteRefs[0].Name);
            viewModel.DeleteCommand.CanExecute(null).Should().BeFalse();
            Capture(window, "delete-remote-branch");
            window.Close();
        });

        _commands.StartDeleteRemoteBranchDialog(_owner, remoteRefs[0].Name).Should().BeTrue();

        remoteBranch.Should().Be(remoteRefs[0].Name);
    }

    /// <summary>Event scripts that always pass (the default mock fails them).</summary>
    private sealed class PassingScriptsRunner : IScriptsRunner
    {
        public bool RunEventScripts<THostForm>(ScriptEvent scriptEvent, THostForm form)
            where THostForm : IGitModuleForm, IScriptOptionsForm, IWin32Window
            => true;

        public bool RunScript(ScriptInfo scriptInfo, IWin32Window owner, IGitUICommands commands, IScriptOptionsProvider scriptOptionsProvider) => true;
    }
}
