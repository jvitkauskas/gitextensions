using CommonTestUtils;
using GitCommands;
using GitUI;
using GitUI.AvaloniaHosting;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs.SettingsDialog;
using GitUI.Presentation.HelperDialogs;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 2, batch 8: worktrees, submodules, HOME directory and the help text window, with real git.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void Manage_worktree_lists_the_worktrees_and_prunes_deleted_ones()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"ge-avalonia-worktree-{Guid.NewGuid():N}");
        _referenceRepository.Module.GitExecutable.GetOutput($"worktree add -b listed-worktree \"{directory}\"");
        Directory.Delete(directory, recursive: true);

        List<string>? listed = null;
        bool deletedMarked = false;
        List<string>? afterPrune = null;
        string? processResult = null;
        DriveDialogs(
            window =>
            {
                ManageWorktreeViewModel viewModel = (ManageWorktreeViewModel)window.DataContext!;
                listed = [.. viewModel.Worktrees.Select(w => w.Branch ?? "")];
                deletedMarked = viewModel.Worktrees[1].IsDeleted;
                viewModel.PruneCommand.CanExecute(null).Should().BeTrue();
                Capture(window, "manage-worktree");
                viewModel.PruneCommand.Execute(null);
                afterPrune = [.. viewModel.Worktrees.Select(w => w.Branch ?? "")];
                window.Close();
            },
            window =>
            {
                ProcessViewModel viewModel = (ProcessViewModel)window.DataContext!;
                WhenDone(viewModel, () =>
                {
                    processResult = $"{viewModel.Status}";
                    viewModel.AcknowledgeCommand.Execute(null);
                });
            });

        AvaloniaDialogs.TryShowManageWorktree(_owner, _commands, out bool shouldRefreshRevisionGrid).Should().BeTrue();

        listed.Should().Equal("master", "listed-worktree");
        deletedMarked.Should().BeTrue();
        processResult.Should().Be(nameof(ProcessStatus.Succeeded));
        afterPrune.Should().Equal("master");
        shouldRefreshRevisionGrid.Should().BeFalse("no worktree was created");
    }

    [Test]
    public void Submodules_dialog_lists_the_submodules_and_synchronizes_them()
    {
        using GitModuleTestHelper parent = new("parent");
        using GitModuleTestHelper submodule = new("submodule");
        parent.AddSubmodule(submodule, "sub");
        GitUICommands commands = new(GlobalServiceContainer.CreateDefaultMockServiceContainer(), parent.Module);

        SubmoduleItem? listed = null;
        string? processResult = null;
        DriveDialogs(
            window =>
            {
                SubmodulesViewModel viewModel = (SubmodulesViewModel)window.DataContext!;
                WaitUntil(() => !viewModel.IsLoading && viewModel.Submodules.Count > 0, () =>
                {
                    listed = viewModel.Submodules.FirstOrDefault();
                    viewModel.SelectedSubmodule = listed;
                    Capture(window, "submodules");
                    viewModel.SynchronizeCommand.Execute(null);
                    window.Close();
                });
            },
            window =>
            {
                ProcessViewModel viewModel = (ProcessViewModel)window.DataContext!;
                WhenDone(viewModel, () =>
                {
                    processResult = $"{viewModel.Status}";
                    viewModel.AcknowledgeCommand.Execute(null);
                });
            });

        commands.StartSubmodulesDialog(_owner).Should().BeTrue();

        listed.Should().NotBeNull();
        listed!.Name.Should().Be("sub");
        listed.LocalPath.Should().Be("sub");
        processResult.Should().Be(nameof(ProcessStatus.Succeeded));
    }

    [Test]
    public void Fix_home_dialog_applies_the_chosen_HOME()
    {
        string customHomeDir = AppSettings.CustomHomeDir;
        bool userProfileHomeDir = AppSettings.UserProfileHomeDir;
        string? home = Environment.GetEnvironmentVariable("HOME");
        string otherHome = Path.Combine(Path.GetTempPath(), $"ge-avalonia-home-{Guid.NewGuid():N}");
        Directory.CreateDirectory(otherHome);

        // Opening tells the user where a global config was located, if any (as FormFixHome.LoadSettings);
        // acknowledge it, whether this machine has one or not.
        GitExtUtils.GitUI.UiTimer acknowledgeInformation = new() { Interval = 100 };
        acknowledgeInformation.Tick += (_, _) => CloseTopLevelWindow("Information", except: 0);
        acknowledgeInformation.Start();
        try
        {
            AppSettings.CustomHomeDir = "";
            AppSettings.UserProfileHomeDir = false;

            bool closed = false;
            DriveNextDialog(window =>
            {
                FixHomeViewModel viewModel = (FixHomeViewModel)window.DataContext!;
                window.Closed += (_, _) => closed = true;
                viewModel.IsDefaultHome = false;
                viewModel.IsUserProfileHome = false;
                viewModel.IsOtherHome = true;
                viewModel.OtherHomeDir = otherHome;
                Capture(window, "fix-home");
                viewModel.OkCommand.Execute(null);
            });

            AvaloniaDialogs.TryShowFixHome(_owner).Should().BeTrue();

            closed.Should().BeTrue();
            AppSettings.CustomHomeDir.Should().Be(otherHome);
            Environment.GetEnvironmentVariable("HOME").Should().Be(otherHome);
        }
        finally
        {
            acknowledgeInformation.Dispose();
            AppSettings.CustomHomeDir = customHomeDir;
            AppSettings.UserProfileHomeDir = userProfileHomeDir;
            Environment.SetEnvironmentVariable("HOME", home);
            Directory.Delete(otherHome, recursive: true);
        }
    }

    /// <summary>Runs the message loop until <paramref name="condition"/> holds (for modeless windows).</summary>
    private static void PumpUntil(Func<bool> condition)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(10);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            MessagePump.DoEvents();
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            Thread.Sleep(10);
        }
    }
}
