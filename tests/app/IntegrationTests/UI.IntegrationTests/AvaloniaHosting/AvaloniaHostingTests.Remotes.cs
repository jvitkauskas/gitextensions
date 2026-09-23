using CommonTestUtils;
using GitCommands;
using GitUI.Presentation.CommandsDialogs;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 2, batch 10: the remotes dialog, shown from its WinForms entry point with real git.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void Remotes_dialog_deactivates_a_remote_and_sets_the_tracking_remote_of_a_branch()
    {
        using ReferenceRepository other = new();
        // Through the settings manager, as the dialog does, so that the module's cached settings know the remote.
        new GitCommands.Remotes.ConfigFileRemoteSettingsManager(() => _referenceRepository.Module)
            .SaveRemote(null, "other", other.Module.WorkingDir.TrimEnd('\\'), remotePushUrl: null, remotePuttySshKey: "", remoteColor: null, remotePrefix: null);

        List<string>? listed = null;
        List<string>? remotesWhileInactive = null;
        List<string>? heads = null;
        DriveNextDialog(window =>
        {
            RemotesViewModel viewModel = (RemotesViewModel)window.DataContext!;
            listed = [.. viewModel.Remotes.Select(r => r.Name)];
            Capture(window, "remotes");

            viewModel.SelectedRemote = viewModel.Remotes.Single(r => r.Name == "other");
            viewModel.ToggleStateCommand.Execute(null);
            remotesWhileInactive = [.. _referenceRepository.Module.GetRemoteNames()];
            viewModel.SelectedRemote!.IsDisabled.Should().BeTrue();
            viewModel.ToggleStateCommand.Execute(null);

            viewModel.SelectedTabIndex = 1;
            heads = [.. viewModel.Heads.Select(h => h.LocalName)];
            viewModel.SelectedHead = viewModel.Heads.Single(h => h.LocalName == "master");
            viewModel.SelectedTrackingRemote = "other";
            viewModel.MergeWith = "refs/heads/master";
            window.Close();
        });

        _commands.StartRemotesDialog(_owner).Should().BeTrue();

        listed.Should().Equal("other");
        remotesWhileInactive.Should().NotContain("other");
        _referenceRepository.Module.GetRemoteNames().Should().Contain("other");
        heads.Should().Contain("master");
        _referenceRepository.Module.GetSetting("branch.master.remote").Should().Be("other");
        _referenceRepository.Module.GetSetting("branch.master.merge").Should().Be("refs/heads/master");
    }
}
