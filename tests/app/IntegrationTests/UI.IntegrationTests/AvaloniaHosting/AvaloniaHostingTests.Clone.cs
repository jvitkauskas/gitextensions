using GitCommands;
using GitUI.Presentation.CommandsDialogs;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>The clone dialog of phase 2, batch 6, shown from its WinForms entry point with real git.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void StartCloneDialog_clones_a_local_repository()
    {
        bool closeProcessDialog = AppSettings.CloseProcessDialog;
        AppSettings.CloseProcessDialog = true;
        string destination = Path.Combine(Path.GetTempPath(), $"ge-avalonia-clone-{Guid.NewGuid():N}");
        try
        {
            DriveNextDialog(window =>
            {
                CloneViewModel viewModel = (CloneViewModel)window.DataContext!;
                viewModel.From = _referenceRepository.Module.WorkingDir;
                viewModel.Destination = destination;
                viewModel.NewDirectory = "clone";
                Capture(window, "clone");
                viewModel.CloneCommand.Execute(null);
            });

            // The (WinForms) remote process dialog closes itself when the clone succeeds.
            _commands.StartCloneDialog(_owner, url: "", gitModuleChanged: null!).Should().BeTrue();

            Directory.Exists(Path.Combine(destination, "clone", ".git")).Should().BeTrue();
        }
        finally
        {
            AppSettings.CloseProcessDialog = closeProcessDialog;
            if (Directory.Exists(destination))
            {
                foreach (string file in Directory.EnumerateFiles(destination, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }

                Directory.Delete(destination, recursive: true);
            }
        }
    }
}
