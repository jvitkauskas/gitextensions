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
        host.Log.Should().Equal("state: docs/readme.md folder:  focused: docs/readme.md");
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

    [Test]
    public void The_git_items_get_the_selection_and_request_a_refresh_when_they_change_the_files()
    {
        FakeMenuHost host = new() { State = new() { IsSkipWorktree = true } };
        FileStatusListViewModel viewModel = Create();
        viewModel.MenuHost = host;
        viewModel.SetDiff(First, Second, CreateStatuses());
        viewModel.UpdateMenuState();
        int refreshes = 0;
        viewModel.RefreshRequested += (_, _) => refreshes++;

        viewModel.StageFilesCommand.Execute(null);
        viewModel.UnstageFilesCommand.Execute(null);
        viewModel.ResetChunkOfFileCommand.Execute(null);
        viewModel.InteractiveAddCommand.Execute(null);
        viewModel.DeleteFilesCommand.Execute(null);
        viewModel.MoveCommand.Execute(null);
        viewModel.AddToIgnoreFileCommand.Execute(false);
        viewModel.AddToIgnoreFileCommand.Execute(true);
        viewModel.ToggleSkipWorktreeCommand.Execute(null);
        viewModel.ToggleAssumeUnchangedCommand.Execute(null);
        viewModel.StopTrackingCommand.Execute(null);
        viewModel.RunSubmoduleActionCommand.Execute(SubmoduleMenuAction.Update);
        refreshes.Should().Be(12);

        // Without a change: cancelled, or not changing the files.
        host.MoveResult = false;
        viewModel.MoveCommand.Execute(null);
        viewModel.RunSubmoduleActionCommand.Execute(SubmoduleMenuAction.Reset);
        viewModel.RememberDiffCommand.Execute(true);
        viewModel.RememberDiffCommand.Execute(false);
        viewModel.DiffWithRememberedCommand.Execute(null);
        viewModel.OpenInVisualStudioCommand.Execute(null);
        viewModel.RunScriptCommand.Execute(new FileStatusScript("Lint", 9001, IsDirect: false));
        refreshes.Should().Be(12);

        host.Log.Where(l => !l.StartsWith("state")).Should().Equal(
            "stage: docs/readme.md",
            "unstage: docs/readme.md",
            "reset chunk: docs/readme.md",
            "interactive add: docs/readme.md",
            "delete: docs/readme.md",
            "move: docs/readme.md folder: ",
            "ignore: docs/readme.md folder: ",
            "exclude: docs/readme.md folder: ",
            "skip worktree False: docs/readme.md",
            "assume unchanged True: docs/readme.md",
            "stop tracking: docs/readme.md",
            "submodule Update: docs/readme.md",
            "move: docs/readme.md folder: ",
            "submodule Reset: docs/readme.md",
            "remember first: docs/readme.md",
            "remember second: docs/readme.md",
            "diff with remembered: docs/readme.md",
            "visual studio: docs/readme.md",
            "script Lint: docs/readme.md folder: ");

        // A folder is renamed, added to .gitignore and given to the scripts.
        viewModel.SelectedNodes.Clear();
        viewModel.SelectedNodes.Add(viewModel.Nodes[1]);
        viewModel.MoveCommand.Execute(null);
        viewModel.AddToIgnoreFileCommand.Execute(false);
        viewModel.RunScriptCommand.Execute(new FileStatusScript("Lint", 9001, IsDirect: true));
        host.Log.TakeLast(3).Should().Equal(
            "move:  folder: src",
            "ignore: src/Util/Helper.cs, src/Program.cs folder: src",
            "script Lint: src/Util/Helper.cs, src/Program.cs folder: src");
    }

    [Test]
    public void Staging_is_the_one_of_the_dialog_when_it_binds_it()
    {
        FakeMenuHost host = new();
        FileStatusListViewModel viewModel = Create();
        viewModel.MenuHost = host;
        viewModel.SetDiff(First, Second, CreateStatuses());
        int refreshes = 0;
        int staged = 0;
        int unstaged = 0;
        viewModel.RefreshRequested += (_, _) => refreshes++;
        viewModel.StageSelectedAction = () => staged++;
        viewModel.UnstageSelectedAction = () => unstaged++;

        viewModel.StageFilesCommand.Execute(null);
        viewModel.UnstageFilesCommand.Execute(null);

        (staged, unstaged, refreshes).Should().Be((1, 1, 0), "the dialog refreshes its lists itself");
        host.Log.Should().BeEmpty();
    }

    [Test]
    public void The_items_of_the_main_window_are_shown_where_the_list_binds_them()
    {
        FakeMenuHost host = new() { State = new() { ShowShowInFileTree = true, CanFilterFileInGrid = true, ShowCherryPick = true } };
        FileStatusListViewModel viewModel = Create();
        viewModel.MenuHost = host;
        viewModel.SetDiff(First, Second, CreateStatuses());

        viewModel.UpdateMenuState();
        (viewModel.MenuState.ShowShowInFileTree, viewModel.MenuState.CanFilterFileInGrid, viewModel.MenuState.ShowCherryPick, viewModel.HasFilterFileInGrid)
            .Should().Be((false, false, false, false), "as FileStatusList without BindContextMenu");

        List<string> runs = [];
        viewModel.ShowInFileTreeAction = () => runs.Add("tree");
        viewModel.FilterFileInGridAction = () => runs.Add("filter");
        viewModel.CherryPickChangesAction = () => runs.Add("cherry-pick");
        viewModel.GetSupportLinePatching = () => true;
        viewModel.UpdateMenuState();
        (viewModel.MenuState.ShowShowInFileTree, viewModel.MenuState.CanFilterFileInGrid, viewModel.MenuState.ShowCherryPick, viewModel.HasFilterFileInGrid)
            .Should().Be((true, true, true, true));
        host.Log[^1].Should().EndWith(" patching", "the host gets whether the diff supports line patching");

        viewModel.ShowInFileTreeCommand.Execute(null);
        viewModel.FilterFileInGridCommand.Execute(null);
        viewModel.CherryPickChangesCommand.Execute(null);
        runs.Should().Equal("tree", "filter", "cherry-pick");

        // As ShowInFileTree_Click: not in the file tree itself.
        FileStatusListViewModel tree = new(new FileStatusListStrings()) { IsFileTreeMode = true, MenuHost = host, ShowInFileTreeAction = () => { } };
        tree.UpdateMenuState();
        tree.MenuState.ShowShowInFileTree.Should().BeFalse();
    }

    [Test]
    public void Diff_the_selected_files_and_find_file_get_the_focused_file_and_all_the_files()
    {
        FakeMenuHost host = new();
        FileStatusListViewModel viewModel = Create();
        viewModel.MenuHost = host;
        viewModel.SetDiff(First, Second, CreateStatuses());

        // The file selected last is the focused one, the second of the diff.
        FileStatusNode program = viewModel.Nodes[1].Children[1];
        viewModel.SelectedNodes.Clear();
        viewModel.SelectedNodes.Add(program);
        viewModel.SelectedNodes.Add(viewModel.Nodes[0].Children[0]);
        viewModel.FocusedEntry!.Item.Name.Should().Be("docs/readme.md");
        viewModel.DiffTwoSelectedCommand.Execute(null);
        host.Log[^1].Should().Be("diff two: src/Program.cs, docs/readme.md focused: docs/readme.md");

        // As FindFile_Click: among all the files, also those the filter hides; the file found is selected.
        viewModel.Filter = "Program";
        host.FoundFile = program.Entry!.Item;
        viewModel.FindFileCommand.Execute(null);
        host.Log[^1].Should().Be("find: 4");
        viewModel.SelectedEntry!.Item.Name.Should().Be("src/Program.cs");
    }

    [Test]
    public void Refreshing_keeps_the_selected_files_else_selects_the_next_file()
    {
        FileStatusListViewModel viewModel = Create();
        viewModel.SetDiff(First, Second, CreateStatuses());
        viewModel.Select(entry => entry.Item.Name == "src/Program.cs");

        // As RefreshArtificial: new statuses of the same files.
        viewModel.RefreshGroups([new FileStatusGroup(First, Second, "", CreateStatuses())]);
        viewModel.SelectedEntry!.Item.Name.Should().Be("src/Program.cs");

        // The selected file is gone (e.g. staged): the file after it (StoreNextItemToSelect).
        viewModel.RefreshGroups([new FileStatusGroup(First, Second, "", [.. CreateStatuses().Where(s => s.Name != "src/Program.cs")])]);
        viewModel.SelectedEntry!.Item.Name.Should().Be("build.cmd");

        // The last file is gone: the first file.
        viewModel.RefreshGroups([new FileStatusGroup(First, Second, "", [.. CreateStatuses().Where(s => s.Name != "build.cmd")])]);
        viewModel.SelectedEntry!.Item.Name.Should().Be("docs/readme.md");
    }

    [Test]
    public void SelectFirstGroup_selects_the_files_of_the_first_diff()
    {
        FileStatusListViewModel viewModel = Create();
        viewModel.SetGroups(
        [
            new FileStatusGroup(First, Second, "First parent", CreateStatuses()),
            new FileStatusGroup(null, Second, "Second parent", [new GitItemStatus("other.txt") { IsNew = true }]),
        ]);

        viewModel.SelectFirstGroup();
        viewModel.SelectedEntries.Select(e => e.Item.Name).Should().BeEquivalentTo("docs/readme.md", "src/Util/Helper.cs", "src/Program.cs", "build.cmd");

        // Without diff groups: all the files.
        viewModel.SetDiff(First, Second, CreateStatuses());
        viewModel.SelectFirstGroup();
        viewModel.SelectedEntries.Should().HaveCount(4);

        // As SelectFileOrFolder: a folder, expanded to show it.
        viewModel.CollapseAllCommand.Execute(null);
        viewModel.SelectFolder(RelativePath.From("src/Util"));
        viewModel.SelectedFolder!.Value.Should().Be("src/Util");
        viewModel.Nodes[1].IsExpanded.Should().BeTrue();
    }

    [Test]
    public void The_difftool_submenus_open_the_chosen_difftool_and_name_the_revisions()
    {
        FakeMenuHost host = new();
        FileStatusListViewModel viewModel = Create();
        viewModel.MenuHost = host;
        viewModel.DescribeRevision = objectId => objectId == First.ObjectId ? "first" : "second";
        bool disabled = false;
        viewModel.DisableCustomDiffToolsAction = () => disabled = true;
        viewModel.SetDiff(First, Second, CreateStatuses());
        viewModel.UpdateMenuState();

        // As OpenWithDifftool_DropDownOpening.
        viewModel.SecondDiffCaption.Should().Be("Second: B second");
        viewModel.FirstDiffCaption.Should().Be("First: A first");

        viewModel.OpenWithCustomDifftoolCommand.Execute(new DifftoolChoice(DifftoolKind.FirstToSelected, "meld"));
        viewModel.DiffWithRememberedCustomCommand.Execute("kdiff3");
        viewModel.DiffTwoSelectedCustomCommand.Execute("meld");
        viewModel.DisableCustomDiffToolsCommand.Execute(null);

        host.Log.Where(line => !line.StartsWith("state:")).Should().Equal(
            "difftool FirstToSelected meld: docs/readme.md",
            "diff with remembered kdiff3: docs/readme.md",
            "diff two meld: docs/readme.md focused: docs/readme.md");
        disabled.Should().BeTrue();

        // Files of several revisions; none selected: no captions.
        viewModel.SetGroups(
        [
            new FileStatusGroup(First, Second, "First parent", CreateStatuses()),
            new FileStatusGroup(Second, First, "Second parent", [new GitItemStatus("other.txt") { IsNew = true }]),
        ]);
        viewModel.SelectedNodes.Clear();
        viewModel.SelectedNodes.Add(viewModel.Nodes[0].Children[0].Children.FirstOrDefault() ?? viewModel.Nodes[0].Children[0]);
        viewModel.SelectedNodes.Add(viewModel.Nodes[1].Children[0]);
        viewModel.UpdateMenuState();
        viewModel.SecondDiffCaption.Should().Be("Second: B <multiple>");
        viewModel.SelectedNodes.Clear();
        viewModel.UpdateMenuState();
        viewModel.SecondDiffCaption.Should().BeNull();
        viewModel.FirstDiffCaption.Should().BeNull();
    }

    [Test]
    public void A_double_click_opens_the_history_or_the_submodule_unless_the_dialog_handles_it()
    {
        FakeMenuHost host = new();
        FileStatusListViewModel viewModel = Create();
        viewModel.MenuHost = host;
        viewModel.SetDiff(First, Second, [new GitItemStatus("lib") { IsSubmodule = true, IsTracked = true, IsChanged = true }, new GitItemStatus("file.txt") { IsTracked = true, IsChanged = true }]);

        // As FileStatusListView_DoubleClick without a handler.
        viewModel.Select(entry => entry.Item.Name == "lib");
        viewModel.ActivateSelection();
        host.OpenSubmoduleOnDoubleClick = true;
        viewModel.ActivateSelection();
        viewModel.Select(entry => entry.Item.Name == "file.txt");
        viewModel.ActivateSelection();
        viewModel.Select(entry => entry.Item.Name == "lib");
        viewModel.OpenSubmoduleCommand.Execute(null);

        host.Log.Where(line => !line.StartsWith("state:")).Should().Equal("history: lib", "open submodule: lib", "history: file.txt", "open submodule: lib");

        // A dialog with its own action (e.g. staging in the commit dialog).
        int activated = 0;
        viewModel.SelectionActivated += (_, _) => activated++;
        viewModel.ActivateSelection();
        activated.Should().Be(1);
        host.Log.Should().HaveCount(4);
    }

    internal sealed class FakeMenuHost : IFileStatusListMenuHost
    {
        public List<string> Log { get; } = [];

        public bool ResetResult { get; set; } = true;

        public FileStatusMenuState State { get; set; } = new() { ShowSaveAs = true };

        public bool MoveResult { get; set; } = true;

        public GitItemStatus? FoundFile { get; set; }

        public FileStatusMenuState GetMenuState(IReadOnlyList<FileStatusEntry> selected, RelativePath? selectedFolder, FileStatusEntry? focused, bool supportLinePatching)
        {
            Log.Add($"state: {Names(selected)} folder: {selectedFolder}{(focused is null ? "" : $" focused: {focused.Item.Name}")}{(supportLinePatching ? " patching" : "")}");
            return State;
        }

        public void OpenWithDifftool(IReadOnlyList<FileStatusEntry> selected, DifftoolKind kind, string? customTool = null) => Log.Add($"difftool {kind}{(customTool is null ? "" : $" {customTool}")}: {Names(selected)}");

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

        public void OpenSubmodule(FileStatusEntry entry) => Log.Add($"open submodule: {entry.Item.Name}");

        public bool OpenSubmoduleOnDoubleClick { get; set; }

        public bool ResetFiles(IReadOnlyList<FileStatusEntry> selected, bool toParent)
        {
            Log.Add($"reset {(toParent ? "parent" : "selected")}: {Names(selected)}");
            return ResetResult;
        }

        public void StageFiles(IReadOnlyList<FileStatusEntry> selected) => Log.Add($"stage: {Names(selected)}");

        public void UnstageFiles(IReadOnlyList<FileStatusEntry> selected) => Log.Add($"unstage: {Names(selected)}");

        public Task ResetChunkOfFileAsync(FileStatusEntry entry)
        {
            Log.Add($"reset chunk: {entry.Item.Name}");
            return Task.CompletedTask;
        }

        public Task InteractiveAddAsync(FileStatusEntry entry)
        {
            Log.Add($"interactive add: {entry.Item.Name}");
            return Task.CompletedTask;
        }

        public void RememberDiff(FileStatusEntry entry, bool first) => Log.Add($"remember {(first ? "first" : "second")}: {entry.Item.Name}");

        public void DiffWithRemembered(FileStatusEntry entry, string? customTool = null) => Log.Add($"diff with remembered{(customTool is null ? "" : $" {customTool}")}: {entry.Item.Name}");

        public void DiffTwoSelected(IReadOnlyList<FileStatusEntry> selected, FileStatusEntry? focused, string? customTool = null) => Log.Add($"diff two{(customTool is null ? "" : $" {customTool}")}: {Names(selected)} focused: {focused?.Item.Name}");

        public void OpenInVisualStudio(FileStatusEntry entry) => Log.Add($"visual studio: {entry.Item.Name}");

        public bool Move(FileStatusEntry? entry, RelativePath? selectedFolder)
        {
            Log.Add($"move: {entry?.Item.Name} folder: {selectedFolder}");
            return MoveResult;
        }

        public bool DeleteFiles(IReadOnlyList<FileStatusEntry> selected)
        {
            Log.Add($"delete: {Names(selected)}");
            return true;
        }

        public GitItemStatus? FindFile(IReadOnlyList<GitItemStatus> candidates)
        {
            Log.Add($"find: {candidates.Count}");
            return FoundFile;
        }

        public bool EditGitIgnore(bool localExcludes)
        {
            Log.Add($"edit {(localExcludes ? "exclude" : "gitignore")}");
            return true;
        }

        public bool AddToIgnoreFile(IReadOnlyList<FileStatusEntry> selected, RelativePath? selectedFolder, bool localExclude)
        {
            Log.Add($"{(localExclude ? "exclude" : "ignore")}: {Names(selected)} folder: {selectedFolder}");
            return true;
        }

        public void SetSkipWorktree(IReadOnlyList<FileStatusEntry> selected, bool skipWorktree) => Log.Add($"skip worktree {skipWorktree}: {Names(selected)}");

        public void SetAssumeUnchanged(IReadOnlyList<FileStatusEntry> selected, bool assumeUnchanged) => Log.Add($"assume unchanged {assumeUnchanged}: {Names(selected)}");

        public bool StopTracking(FileStatusEntry entry)
        {
            Log.Add($"stop tracking: {entry.Item.Name}");
            return true;
        }

        public bool RunSubmoduleAction(IReadOnlyList<FileStatusEntry> selected, SubmoduleMenuAction action)
        {
            Log.Add($"submodule {action}: {Names(selected)}");
            return action != SubmoduleMenuAction.Reset;
        }

        public void RunScript(FileStatusScript script, IReadOnlyList<FileStatusEntry> selected, RelativePath? selectedFolder)
            => Log.Add($"script {script.Name}: {Names(selected)} folder: {selectedFolder}");

        private static string Names(IReadOnlyList<FileStatusEntry> entries) => string.Join(", ", entries.Select(e => e.Item.Name));
    }
}
