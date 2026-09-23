using GitCommands;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 5: the pull dialog shown from its WinForms entry points, with real git (the remote process dialog stays WinForms).</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void StartPullDialog_fetches_the_chosen_remote()
    {
        bool closeProcessDialog = AppSettings.CloseProcessDialog;
        GitPullAction formPullAction = AppSettings.FormPullAction;
        AppSettings.CloseProcessDialog = true;
        string upstream = CreateUpstreamClone(out string upstreamCommit);
        try
        {
            string? title = null;
            DriveNextDialog(window =>
            {
                PullViewModel viewModel = (PullViewModel)window.DataContext!;
                viewModel.Remotes.Should().Equal(PullViewModel.AllRemotes, "upstream");
                viewModel.Remote.Should().Be("upstream");
                viewModel.PullSource.Should().Be(upstream);
                viewModel.IsFetch = true;
                title = window.Title;
                Capture(window, "pull");

                // Runs git fetch in the (WinForms) remote process dialog, which closes itself when done.
                viewModel.PullCommand.Execute(null);
            });

            CreateCommandsWithPassingScripts().StartPullDialog(_owner).Should().BeTrue();

            title.Should().StartWith("Fetch (");
            _referenceRepository.Module.GetRefs(RefsFilter.Remotes).Select(r => r.Name).Should().Contain("upstream/master");
            _referenceRepository.Module.RevParse("upstream/master").ToString().Should().Be(upstreamCommit);
            AppSettings.FormPullAction.Should().Be(GitPullAction.Fetch);
        }
        finally
        {
            AppSettings.CloseProcessDialog = closeProcessDialog;
            AppSettings.FormPullAction = formPullAction;
            DeleteDirectory(upstream);
        }
    }

    [Test]
    public void StartPullDialogAndPullImmediately_merges_without_the_dialog()
    {
        bool closeProcessDialog = AppSettings.CloseProcessDialog;
        GitPullAction formPullAction = AppSettings.FormPullAction;
        AppSettings.CloseProcessDialog = true;
        string upstream = CreateUpstreamClone(out string upstreamCommit);
        try
        {
            // No Avalonia dialog is shown: fail if one is.
            AvaloniaDialogHost.DialogShowingForTests = window =>
            {
                _driveFailure = new InvalidOperationException($"Unexpected dialog {window.GetType().Name}");
                window.Opened += (_, _) => window.Close();
            };

            bool done = CreateCommandsWithPassingScripts().StartPullDialogAndPullImmediately(
                out bool pullCompleted, _owner, remoteBranch: "master", remote: "upstream", pullAction: GitPullAction.Merge);

            done.Should().BeTrue();
            pullCompleted.Should().BeTrue();
            _referenceRepository.Module.GetCurrentCheckout().ToString().Should().Be(upstreamCommit, "the upstream commit is fast forwarded");
        }
        finally
        {
            AppSettings.CloseProcessDialog = closeProcessDialog;
            AppSettings.FormPullAction = formPullAction;
            DeleteDirectory(upstream);
        }
    }

    /// <summary>Clones the reference repository, commits in the clone and adds it as the remote <c>upstream</c>.</summary>
    private string CreateUpstreamClone(out string commit)
    {
        string upstream = Path.Combine(Path.GetTempPath(), $"ge-avalonia-pull-{Guid.NewGuid():N}");
        GitModule module = _referenceRepository.Module;
        module.GitExecutable.GetOutput($"clone \"{module.WorkingDir.TrimEnd('\\', '/')}\" \"{upstream}\"");
        File.WriteAllText(Path.Combine(upstream, "upstream.txt"), "upstream change");
        module.GitExecutable.GetOutput($"-C \"{upstream}\" add upstream.txt");
        module.GitExecutable.GetOutput($"-C \"{upstream}\" -c user.name=GitUITests -c user.email=unittests@gitextensions.com commit -m \"upstream commit\"");
        commit = module.GitExecutable.GetOutput($"-C \"{upstream}\" rev-parse HEAD").Trim();
        module.GitExecutable.GetOutput($"remote add upstream \"{upstream.Replace('\\', '/')}\"");

        // The module caches its config file (as ReferenceRepository.CreateRemoteForBranch, reload it).
        module.InvalidateGitSettings();
        module.GetEffectiveSetting("reload now");
        module.GetSettings("reload local settings, too");

        return upstream.Replace('\\', '/');
    }

    private static void DeleteDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (string file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(directory, recursive: true);
    }
}
