using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.UserControls.FileStatusList;
using static GitUI.AvaloniaTests.ViewModels.FileStatusListViewModelTests;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the menu of the file status list (its items are checked against WinForms elsewhere).</summary>
[TestFixture]
public sealed class FileStatusListMenuTests
{
    [Test]
    public void Menu_state_comes_from_the_host_for_the_selection()
    {
        FakeMenuHost host = new();
        FileStatusListViewModel viewModel = Create();
        viewModel.HasMenuHost.Should().BeFalse();
        viewModel.UpdateMenuState();
        viewModel.MenuState.Should().BeSameAs(FileStatusMenuState.None);

        viewModel.MenuHost = host;
        viewModel.HasMenuHost.Should().BeTrue();
        viewModel.SetDiff(First, Second, CreateStatuses());
        viewModel.UpdateMenuState();

        viewModel.MenuState.ShowSaveAs.Should().BeTrue();
        host.Log.Should().Equal("state: docs/readme.md folder: ");
    }

    [Test]
    public void Actions_get_the_selected_files_or_folder()
    {
        FakeMenuHost host = new();
        FileStatusListViewModel viewModel = Create();
        viewModel.MenuHost = host;
        viewModel.SetDiff(First, Second, CreateStatuses());

        viewModel.OpenWithDifftoolCommand.Execute(DifftoolKind.FirstToLocal);
        viewModel.OpenRevisionFileCommand.Execute(true);
        viewModel.ShowFileHistoryCommand.Execute(true);

        viewModel.SelectedNodes.Clear();
        viewModel.SelectedNodes.Add(viewModel.Nodes[1]);
        viewModel.SelectedFolder!.Value.Should().Be("src");
        viewModel.CopyPathsCommand.Execute(CopyPathKind.RelativePosix);
        viewModel.ShowInFolderCommand.Execute(null);

        host.Log.Should().Equal(
            "difftool FirstToLocal: docs/readme.md",
            "revision with: docs/readme.md",
            "history blame: docs/readme.md",
            "copy RelativePosix: src/Util/Helper.cs, src/Program.cs folder: src",
            "folder: src/Util/Helper.cs, src/Program.cs folder: src");
    }

    [Test]
    public void Resetting_and_editing_request_a_refresh_and_sorting_is_reported()
    {
        FakeMenuHost host = new();
        FileStatusListViewModel viewModel = Create();
        viewModel.MenuHost = host;
        viewModel.SetDiff(First, Second, CreateStatuses());
        int refreshes = 0;
        int sortings = 0;
        viewModel.RefreshRequested += (_, _) => refreshes++;
        viewModel.SortTypeChanged += (_, _) => sortings++;

        host.ResetResult = false;
        viewModel.ResetFilesCommand.Execute(true);
        refreshes.Should().Be(0, "the reset was cancelled");
        host.ResetResult = true;
        viewModel.ResetFilesCommand.Execute(false);
        viewModel.EditWorkingDirectoryFileCommand.Execute(null);
        refreshes.Should().Be(2);

        viewModel.SetSortTypeCommand.Execute(DiffListSortType.FileExtensionFlat);
        viewModel.Options.SortType.Should().Be(DiffListSortType.FileExtensionFlat);
        sortings.Should().Be(1);
        host.Log.Should().Contain(["reset parent: docs/readme.md", "reset selected: docs/readme.md"]);
    }

    internal sealed class FakeMenuHost : IFileStatusListMenuHost
    {
        public List<string> Log { get; } = [];

        public bool ResetResult { get; set; } = true;

        public FileStatusMenuState State { get; set; } = new() { ShowSaveAs = true };

        public FileStatusMenuState GetMenuState(IReadOnlyList<FileStatusEntry> selected, RelativePath? selectedFolder)
        {
            Log.Add($"state: {Names(selected)} folder: {selectedFolder}");
            return State;
        }

        public void OpenWithDifftool(IReadOnlyList<FileStatusEntry> selected, DifftoolKind kind) => Log.Add($"difftool {kind}: {Names(selected)}");

        public void OpenWorkingDirectoryFile(FileStatusEntry entry, bool openWith) => Log.Add($"open {openWith}: {entry.Item.Name}");

        public bool EditWorkingDirectoryFile(FileStatusEntry entry)
        {
            Log.Add($"edit: {entry.Item.Name}");
            return true;
        }

        public void OpenRevisionFile(FileStatusEntry entry, bool openWith) => Log.Add($"revision{(openWith ? " with" : "")}: {entry.Item.Name}");

        public void SaveAs(IReadOnlyList<FileStatusEntry> selected) => Log.Add($"save as: {Names(selected)}");

        public void CopyPaths(IReadOnlyList<FileStatusEntry> selected, RelativePath? selectedFolder, CopyPathKind kind)
            => Log.Add($"copy {kind}: {Names(selected)} folder: {selectedFolder}");

        public void ShowInFolder(IReadOnlyList<FileStatusEntry> selected, RelativePath? selectedFolder)
            => Log.Add($"folder: {Names(selected)} folder: {selectedFolder}");

        public void ShowFileHistory(FileStatusEntry? entry, RelativePath? selectedFolder, bool blame)
            => Log.Add($"history{(blame ? " blame" : "")}: {entry?.Item.Name}");

        public bool ResetFiles(IReadOnlyList<FileStatusEntry> selected, bool toParent)
        {
            Log.Add($"reset {(toParent ? "parent" : "selected")}: {Names(selected)}");
            return ResetResult;
        }

        private static string Names(IReadOnlyList<FileStatusEntry> entries) => string.Join(", ", entries.Select(e => e.Item.Name));
    }
}
