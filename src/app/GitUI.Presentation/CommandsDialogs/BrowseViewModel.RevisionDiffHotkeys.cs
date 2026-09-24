using GitUI.Presentation.Services;
using GitUI.Presentation.UserControls.FileStatusList;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>The commands of the "RevisionDiff" hotkeys; the values are the ones of <c>RevisionDiffControl.Command</c>.</summary>
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

/// <summary>The hotkeys of the diff and file tree tabs (<c>RevisionDiffControl.ExecuteCommand</c>, <c>FileStatusList.ExecuteCommand</c>).</summary>
public sealed partial class BrowseViewModel
{
    private string? _pendingTreePath;

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
            case RevisionDiffHotkeyCommand.ShowFileTree when !fileTree && FileTree is not null && files.SelectedEntry is { } entry:
                ShowInFileTree(entry.Item.Name);
                return true;
            case RevisionDiffHotkeyCommand.FilterFileInGrid when Filters is not null && files.SelectedEntry is { } entry:
                Filters.ApplyPathFilter(entry.Item.Name.QuoteNE());
                return true;
        }

        files.UpdateMenuState();
        FileStatusMenuState state = files.MenuState;
        return command switch
        {
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

    // As tsmiShowInFileTree: the file tree tab, with the file selected once the tree is loaded.
    private void ShowInFileTree(string path)
    {
        _pendingTreePath = path;
        FileTree!.Select(entry => entry.Item.Name == path);
        SelectedTab = BrowseTab.FileTree;
    }
}
