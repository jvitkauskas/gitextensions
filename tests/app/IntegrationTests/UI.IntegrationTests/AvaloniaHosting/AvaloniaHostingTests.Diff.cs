using GitExtensions.Extensibility.Git;
using GitUI.AvaloniaHosting;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.UserControls.FileStatusList;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 5: the diff dialog on the Avalonia file status list and file viewer, with a real repository.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void Diff_shows_the_changed_files_and_the_diff_of_the_selected_one()
    {
        string first = _referenceRepository.CommitHash!;
        _referenceRepository.CreateCommit("Change A", "changed content of A", "A.txt", "new file", "B.md");
        string second = _referenceRepository.Module.RevParse("HEAD").ToString();

        GitUI.Avalonia.Hosting.DialogWindow? shown = null;
        GitUI.Avalonia.Hosting.AvaloniaDialogHost.DialogShowingForTests = window => shown = window;
        AvaloniaDialogs.TryShowDiff(_commands, ObjectId.Parse(first), ObjectId.Parse(second), "first", "second").Should().BeTrue();
        try
        {
            PumpUntil(() => shown?.DataContext is DiffViewModel { Files.AllEntries: var files } && files.Any());
            DiffViewModel viewModel = (DiffViewModel)shown!.DataContext!;
            viewModel.Files.AllEntries.Select(e => e.Item.Name).Should().BeEquivalentTo("A.txt", "B.md");

            // As a selection in the view: on the Avalonia dispatcher, whose context the loading continues on.
            Avalonia.Threading.Dispatcher.UIThread.Post(() => viewModel.Files.Select(e => e.Item.Name == "A.txt"));
            PumpUntil(() => viewModel.Viewer.Editor.Text.Contains("changed content of A"));
            viewModel.Viewer.Editor.Text.Should().Contain("+changed content of A");
            viewModel.Viewer.Editor.DiffLines.Should().NotBeNull();

            Avalonia.Threading.Dispatcher.UIThread.Post(() => viewModel.Files.Select(e => e.Item.Name == "B.md"));
            PumpUntil(() => viewModel.Viewer.Editor.Text == "new file");
            viewModel.Viewer.Editor.Text.Should().Be("new file", "a new file is shown as it is");
            Capture(shown, "diff");
        }
        finally
        {
            GitUI.Avalonia.Hosting.AvaloniaDialogHost.DialogShowingForTests = null;
            shown?.Close();
            PumpUntil(() => shown?.IsVisible != true);
        }
    }
}
