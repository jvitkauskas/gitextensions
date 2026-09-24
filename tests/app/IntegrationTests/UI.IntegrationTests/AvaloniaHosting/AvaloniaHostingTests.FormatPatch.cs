using GitCommands;
using GitUI.Presentation.CommandsDialogs;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 4: the format patch dialog on the Avalonia revision grid, with real git.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void Format_patch_writes_the_patches_of_the_selected_range()
    {
        _referenceRepository.CreateCommit("Second commit", "second");
        _referenceRepository.CreateCommit("Third commit", "third");
        string outputPath = Path.Combine(Path.GetTempPath(), $"ge-avalonia-patches-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputPath);
        string lastFormatPatchDir = AppSettings.LastFormatPatchDir;

        GitExtUtils.GitUI.UiTimer acknowledgeResult = new() { Interval = 100 };
        acknowledgeResult.Tick += (_, _) => CloseTopLevelWindow("Patch result", except: 0);
        acknowledgeResult.Start();
        try
        {
            bool closed = false;
            DriveNextDialog(window =>
            {
                FormatPatchViewModel viewModel = (FormatPatchViewModel)window.DataContext!;
                window.Closed += (_, _) => closed = true;
                WaitUntil(() => !viewModel.Grid.IsLoading, () =>
                {
                    viewModel.OutputPath = outputPath;
                    viewModel.Grid.SetSelectedRows(viewModel.Grid.Rows.Take(2));
                    Capture(window, "format-patch");
                    viewModel.FormatPatchCommand.Execute(null);
                });
            });

            _commands.StartFormatPatchDialog(_owner).Should().BeTrue();

            closed.Should().BeTrue();
            Directory.GetFiles(outputPath, "*.patch").Select(Path.GetFileName).Should().BeEquivalentTo(["0001-Second-commit.patch", "0002-Third-commit.patch"]);
            AppSettings.LastFormatPatchDir.Should().Be(outputPath);
        }
        finally
        {
            acknowledgeResult.Dispose();
            AppSettings.LastFormatPatchDir = lastFormatPatchDir;
            Directory.Delete(outputPath, recursive: true);
        }
    }
}
