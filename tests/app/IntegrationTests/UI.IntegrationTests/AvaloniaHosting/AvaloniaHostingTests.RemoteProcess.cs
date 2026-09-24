using GitCommands;
using GitUI.Avalonia.HelperDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.AvaloniaHosting;
using GitUI.HelperDialogs;
using GitUI.Presentation.HelperDialogs;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 5: the remote commands run in the Avalonia progress dialog (the port of <c>FormRemoteProcess</c>).</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void RunRemoteProcess_retries_with_the_arguments_changed_by_the_exit_handler()
    {
        bool closeProcessDialog = AppSettings.CloseProcessDialog;
        AppSettings.CloseProcessDialog = true;
        string upstream = CreateUpstreamClone(out string upstreamCommit);
        try
        {
            List<string> titles = [];
            AvaloniaDialogHost.DialogShowingForTests = window =>
            {
                window.Should().BeOfType<ProcessWindow>("the remote command runs in the Avalonia progress dialog");
                window.Opened += (_, _) => titles.Add(((ProcessViewModel)window.DataContext!).BaseTitle);
            };

            int exits = 0;
            RemoteProcessResult result = AvaloniaDialogs.RunRemoteProcess(
                _owner,
                _commands,
                "ls-remote --heads no-such-remote",
                remote: "upstream",
                title: "List the remote branches",
                onExit: (ref bool isError, IRemoteProcessDialog dialog) =>
                {
                    exits++;
                    if (!isError)
                    {
                        return false;
                    }

                    // As HandlePushOnExit, which retries a rejected push with --force-with-lease.
                    dialog.Remote.Should().Be("upstream");
                    dialog.GetOutputString().Should().Contain("no-such-remote");
                    dialog.ProcessArguments = dialog.ProcessArguments.Replace("no-such-remote", "upstream");
                    dialog.Retry();
                    return true;
                });

            exits.Should().Be(2, "the failed command is retried once");
            result.ErrorOccurred.Should().BeFalse();
            result.Aborted.Should().BeFalse();
            result.Output.Should().Contain(upstreamCommit, "the output of the retried command");
            titles.Should().Equal("List the remote branches");

            // The entry point of the remote commands of GitUICommands (e.g. the scripts and the plugins).
            AvaloniaDialogHost.DialogShowingForTests = window => window.Should().BeOfType<ProcessWindow>();
            FormRemoteProcess.ShowDialog(_owner, _commands, "fetch upstream").Should().BeTrue();
            _referenceRepository.Module.RevParse("upstream/master").ToString().Should().Be(upstreamCommit);
        }
        finally
        {
            AppSettings.CloseProcessDialog = closeProcessDialog;
            DeleteDirectory(upstream);
        }
    }
}
