using GitCommands;
using GitUI.Presentation.CommandsDialogs;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 5: the push dialog, pushing to a local bare repository.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void Push_dialog_pushes_the_current_branch_to_its_remote()
    {
        bool closeProcessDialog = AppSettings.CloseProcessDialog;
        bool dontConfirmPushNewBranch = AppSettings.DontConfirmPushNewBranch.Value;
        bool dontConfirmAddTrackingRef = AppSettings.DontConfirmAddTrackingRef;
        AppSettings.CloseProcessDialog = true;
        AppSettings.DontConfirmPushNewBranch.Value = true;
        AppSettings.DontConfirmAddTrackingRef = true;
        string remote = Path.Combine(Path.GetTempPath(), $"ge-avalonia-push-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(remote);
            _referenceRepository.Module.GitExecutable.GetOutput($"init --bare \"{remote}\"");
            _referenceRepository.Module.GitExecutable.GetOutput($"remote add origin \"{remote}\"");
            _referenceRepository.Module.InvalidateGitSettings();

            List<string> remotes = [];
            string? remoteBranch = null;
            DriveNextDialog(window =>
            {
                PushViewModel viewModel = (PushViewModel)window.DataContext!;
                remotes.AddRange(viewModel.Remotes.Select(r => r.Name!));
                remoteBranch = viewModel.RemoteBranch;
                Capture(window, "push");

                // The (WinForms) remote process dialog closes itself when the push succeeds, and the dialog then.
                viewModel.PushCommand.Execute(null);
                if (window.IsVisible)
                {
                    // Not pushed: the test fails instead of waiting.
                    window.Close();
                }
            });

            // The scripts of the default mock services would cancel the push.
            CreateCommandsWithPassingScripts().StartPushDialog(_owner, pushOnShow: false).Should().BeTrue();

            remotes.Should().Equal("origin");
            remoteBranch.Should().Be("master");
            string pushed = _referenceRepository.Module.GitExecutable.GetOutput($"--git-dir=\"{remote}\" rev-parse refs/heads/master").Trim();
            pushed.Should().Be(_referenceRepository.Module.RevParse("HEAD")!.ToString());
            _referenceRepository.Module.GetSetting("branch.master.remote").Should().Be("origin", "the new branch is tracked");
        }
        finally
        {
            AppSettings.CloseProcessDialog = closeProcessDialog;
            AppSettings.DontConfirmPushNewBranch.Value = dontConfirmPushNewBranch;
            AppSettings.DontConfirmAddTrackingRef = dontConfirmAddTrackingRef;
            if (Directory.Exists(remote))
            {
                foreach (string file in Directory.EnumerateFiles(remote, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }

                Directory.Delete(remote, recursive: true);
            }
        }
    }
}
