using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Editor;
using GitUI.Presentation.Services;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUIPluginInterfaces;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>The commands of the "RevisionDiff" hotkeys; the values are the ones of <c>HotkeyCommands.RevisionDiff</c>.</summary>
public enum RevisionDiffHotkeyCommand
{
    DeleteSelectedFiles = 0,
    ShowHistory = 1,
    Blame = 2,
    OpenWithDifftool = 3,
    EditFile = 4,
    OpenAsTempFile = 5,
    OpenAsTempFileWith = 6,
    OpenWithDifftoolFirstToLocal = 7,
    OpenWithDifftoolSelectedToLocal = 8,
    ResetSelectedFiles = 9,
    StageSelectedFile = 10,
    UnStageSelectedFile = 11,
    ShowFileTree = 12,
    FilterFileInGrid = 13,
    SelectFirstGroupChanges = 14,
    FindFile = 15,
    OpenWorkingDirectoryFileWith = 16,
    FindInCommitFilesUsingGitGrep_DiffTab = 17,
    GoToFirstParent = 18,
    GoToLastParent = 19,
    OpenWorkingDirectoryFile = 20,
    OpenInVisualStudio = 21,
    AddFileToGitIgnore = 22,
    RenameMove = 23,
    FindInCommitFilesUsingGitGrep_FileTreeTab = 24,
}

/// <summary>
///  The menus of the files of the diff and file tree tabs (as <c>RevisionDiffControl.Bind</c> / <c>BindContextMenu</c>) and
///  their hotkeys (<c>RevisionDiffControl.ExecuteCommand</c>, <c>FileStatusList.ExecuteCommand</c>).
/// </summary>
public sealed partial class BrowseViewModel
{
    private string? _pendingTreePath;
    private bool _pendingTreeFolder;

    /// <summary>The "RevisionDiff" hotkeys, of the diff and file tree tabs.</summary>
    public IReadOnlyList<HotkeyBinding> RevisionDiffHotkeys { get; init; } = [];

    /// <summary>As <c>ProcessCmdKey</c> of <c>RevisionDiffControl</c>: the hotkey of the focused tab.</summary>
    public bool ProcessRevisionDiffHotkey(int keyData, bool fileTree)
        => RevisionDiffHotkeys.FirstOrDefault(h => h.KeyData == keyData) is { } hotkey
            && ExecuteRevisionDiffCommand((RevisionDiffHotkeyCommand)hotkey.CommandCode, fileTree);

    /// <summary>
    ///  Runs the command on the selected files of the diff (or file tree) tab, as the item of its context menu: nothing if the
    ///  item is disabled or hidden for the selection.
    /// </summary>
    public bool ExecuteRevisionDiffCommand(RevisionDiffHotkeyCommand command, bool fileTree)
    {
        if ((fileTree ? FileTree : Files) is not { } files)
        {
            return false;
        }

        switch (command)
        {
            case RevisionDiffHotkeyCommand.GoToFirstParent:
                Grid.GoToFirstParent();
                return true;
            case RevisionDiffHotkeyCommand.GoToLastParent:
                Grid.GoToLastParent();
                return true;
            case RevisionDiffHotkeyCommand.SelectFirstGroupChanges:
                files.SelectFirstGroup();
                return true;
        }

        files.UpdateMenuState();
        FileStatusMenuState state = files.MenuState;
        return command switch
        {
            RevisionDiffHotkeyCommand.DeleteSelectedFiles => Run(state.ShowDeleteFile, () => files.DeleteFilesCommand.Execute(null)),
            RevisionDiffHotkeyCommand.ShowHistory => Run(state.CanShowFileHistory, () => files.ShowFileHistoryCommand.Execute(false)),
            RevisionDiffHotkeyCommand.Blame => Run(state.CanBlame, () => files.ShowFileHistoryCommand.Execute(true)),
            RevisionDiffHotkeyCommand.OpenWithDifftool => Run(state.CanOpenWithDifftool && state.CanDiffFirstToSelected, () => files.OpenWithDifftoolCommand.Execute(DifftoolKind.FirstToSelected)),
            RevisionDiffHotkeyCommand.OpenWithDifftoolFirstToLocal => Run(state.CanOpenWithDifftool && state.ShowDiffToLocal && state.CanDiffFirstToLocal, () => files.OpenWithDifftoolCommand.Execute(DifftoolKind.FirstToLocal)),
            RevisionDiffHotkeyCommand.OpenWithDifftoolSelectedToLocal => Run(state.CanOpenWithDifftool && state.ShowDiffToLocal && state.CanDiffSelectedToLocal, () => files.OpenWithDifftoolCommand.Execute(DifftoolKind.SelectedToLocal)),
            RevisionDiffHotkeyCommand.EditFile => Run(state.ShowEditWorkingDirectoryFile, () => files.EditWorkingDirectoryFileCommand.Execute(null)),
            RevisionDiffHotkeyCommand.OpenAsTempFile => Run(state.ShowOpenRevisionFile && state.CanOpenRevisionFile, () => files.OpenRevisionFileCommand.Execute(false)),
            RevisionDiffHotkeyCommand.OpenAsTempFileWith => Run(state.ShowOpenRevisionFile && state.CanOpenRevisionFile, () => files.OpenRevisionFileCommand.Execute(true)),
            RevisionDiffHotkeyCommand.OpenWorkingDirectoryFile => Run(state.ShowOpenWorkingDirectoryFile, () => files.OpenWorkingDirectoryFileCommand.Execute(false)),
            RevisionDiffHotkeyCommand.OpenWorkingDirectoryFileWith => Run(state.ShowOpenWorkingDirectoryFile, () => files.OpenWorkingDirectoryFileCommand.Execute(true)),

            // As ResetSelectedFilesWithConfirmation: to the first revision (for the working directory, the index or HEAD).
            RevisionDiffHotkeyCommand.ResetSelectedFiles => Run(state.ResetToParentText is not null, () => files.ResetFilesCommand.Execute(true)),
            RevisionDiffHotkeyCommand.StageSelectedFile => Run(state.ShowStage, () => files.StageFilesCommand.Execute(null)),
            RevisionDiffHotkeyCommand.UnStageSelectedFile => Run(state.ShowUnstage, () => files.UnstageFilesCommand.Execute(null)),
            RevisionDiffHotkeyCommand.ShowFileTree => Run(state.ShowShowInFileTree, () => files.ShowInFileTreeCommand.Execute(null)),
            RevisionDiffHotkeyCommand.FilterFileInGrid => Run(files.HasFilterFileInGrid && state.CanFilterFileInGrid, () => files.FilterFileInGridCommand.Execute(null)),
            RevisionDiffHotkeyCommand.FindFile => Run(state.ShowFindFile, () => files.FindFileCommand.Execute(null)),
            RevisionDiffHotkeyCommand.OpenInVisualStudio => Run(state.ShowOpenInVisualStudio, () => files.OpenInVisualStudioCommand.Execute(null)),
            RevisionDiffHotkeyCommand.AddFileToGitIgnore => Run(state.ShowIgnore, () => files.AddToIgnoreFileCommand.Execute(false)),
            RevisionDiffHotkeyCommand.RenameMove => Run(state.ShowMove, () => files.MoveCommand.Execute(null)),
            _ => false,
        };

        static bool Run(bool enabled, Action action)
        {
            if (enabled)
            {
                action();
            }

            return enabled;
        }
    }

    // As RevisionDiffControl.Bind and BindContextMenu (the filter in the grid comes with the filters): the items of the main
    // window, and the refresh of the changes after the actions of the menus.
    private void InitializeFileMenus()
    {
        Files.CherryPickChangesAction = CherryPickChanges;
        Files.GetSupportLinePatching = () => Viewer.GetMenuState().CanReset;
        Files.RefreshRequested += (_, _) => RequestDiffRefresh();
        Viewer.PatchApplied += (_, _) => RequestDiffRefresh();
        if (FileTree is not null)
        {
            Files.ShowInFileTreeAction = () => ShowInFileTree(Files);
            FileTree.RefreshRequested += (_, _) => RequestFileTreeRefresh();
        }
    }

    // As RevisionDiffControl.RequestRefresh: the status of the working directory at once, and the files of the artificial commits.
    private void RequestDiffRefresh()
    {
        (_host as IBrowseStatusHost)?.RequestStatusRefresh();
        IReadOnlyList<GitRevision> selected = Grid.GetSelectedRevisionsLatestSelectedFirst();
        if (selected.Any(revision => revision.IsArtificial))
        {
            _ = ShowDiffsAsync(selected, refresh: true);
        }
    }

    // As RequestRefresh of the file tree: the files of an artificial commit are loaded again.
    private void RequestFileTreeRefresh()
    {
        (_host as IBrowseStatusHost)?.RequestStatusRefresh();
        if (_treeRevision?.IsArtificial == true)
        {
            _treeUpToDate = false;
            UpdateFileTree(revisionChanged: false);
        }
    }

    // As FileViewer.CherryPickAllChanges: all the changes of the file are applied to the working directory.
    private void CherryPickChanges()
    {
        int length = Viewer.Editor.Text.Length;
        if (length > 0)
        {
            Viewer.ApplyLinePatch(LinePatchOperation.Stage, selectionStart: 0, selectionLength: length);
        }
    }

    // As RevisionDiffControl.FilterFileInGrid: the revisions of the selected folder, else of the selected files.
    private void FilterFileInGrid(FileStatusListViewModel files)
    {
        string pathFilter = files.SelectedFolder is { } folder
            ? folder.Value
            : string.Join(" ", files.SelectedEntries.Select(entry => entry.Item.Name.ToPosixPath().QuoteNE()));
        Filters?.ApplyPathFilter(pathFilter);
    }

    // As OpenInFileTreeTab (tsmiShowInFileTree): the file tree tab, with the folder or the file selected once the tree is loaded.
    private void ShowInFileTree(FileStatusListViewModel files)
    {
        (string? path, bool isFolder) = files.SelectedFolder is { } folder
            ? (folder.Value, true)
            : (files.SelectedEntries.FirstOrDefault()?.Item.Name, false);
        if (path is null)
        {
            return;
        }

        _pendingTreePath = path;
        _pendingTreeFolder = isFolder;
        SelectInFileTree(path, isFolder);
        SelectedTab = BrowseTab.FileTree;
    }

    private void SelectInFileTree(string path, bool isFolder)
    {
        if (isFolder)
        {
            FileTree!.SelectFolder(RelativePath.From(path));
        }
        else
        {
            FileTree!.Select(entry => entry.Item.Name == path);
        }
    }
}
