using GitCommands;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.UserControls.RevisionGrid;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 5: the file history (the <c>filehistory</c> verb), with a real repository.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void File_history_follows_the_renames_of_the_file()
    {
        string added = _referenceRepository.CreateCommit("Add old", "one\ntwo\nthree\nfour\nfive\nsix\n", "old.txt");
        _referenceRepository.CreateCommit("Unrelated", "other\n", "other.txt");
        _referenceRepository.RenameRepoFile("", "old.txt", "new.txt", "one\ntwo\nthree\nfour\nfive\nsix\nseven\n", "Rename to new");

        // The file history of the browse window is the default; with other windows open, the file history is modeless.
        bool useBrowse = AppSettings.UseBrowseForFileHistory.Value;
        AppSettings.UseBrowseForFileHistory.Value = false;
        // Another window of the application, as the main window.
        GitUI.Avalonia.Hosting.DialogWindow mainWindow = new() { Title = "Main window", Width = 400, Height = 300 };
        GitUI.Avalonia.Hosting.AvaloniaDialogHost.Show(mainWindow, ownerHandle: 0);
        PumpUntil(() => mainWindow.IsVisible);

        GitUI.Avalonia.Hosting.DialogWindow? shown = null;
        GitUI.Avalonia.Hosting.AvaloniaDialogHost.DialogShowingForTests = window => shown = window;
        try
        {
            _commands.GetTestAccessor().RunCommandBasedOnArgument(["ge.exe", "filehistory", "new.txt"]).Should().BeTrue();

            PumpUntil(() => shown?.DataContext is FileHistoryViewModel { Grid.IsLoading: false, Diff.Editor.Text.Length: > 0 });
            FileHistoryViewModel viewModel = (FileHistoryViewModel)shown!.DataContext!;
            viewModel.Grid.Rows.Where(r => !r.Revision.IsArtificial).Select(r => r.Subject).Should().Equal("Rename to new", "Add old");
            shown.Title.Should().StartWith("File History - new.txt - ");
            viewModel.Diff.Editor.Text.Should().Contain("+seven");
            Capture(shown, "file-history");

            // The revision before the rename shows the old name.
            RevisionGridRow first = viewModel.Grid.Rows.Single(r => r.Revision.Guid == added);
            Avalonia.Threading.Dispatcher.UIThread.Post(() => viewModel.Grid.SelectedRow = first);
            PumpUntil(() => viewModel.Title.Contains("(old.txt)"));
            viewModel.Title.Should().StartWith("File History - new.txt (old.txt) - ");
        }
        finally
        {
            AppSettings.UseBrowseForFileHistory.Value = useBrowse;
            GitUI.Avalonia.Hosting.AvaloniaDialogHost.DialogShowingForTests = null;
            shown?.Close();
            PumpUntil(() => shown?.IsVisible != true);
            mainWindow.Close();
            PumpUntil(() => !mainWindow.IsVisible);
        }
    }
}
