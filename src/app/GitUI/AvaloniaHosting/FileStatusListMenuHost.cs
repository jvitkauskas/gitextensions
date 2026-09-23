using System.Text;
using GitCommands;
using GitCommands.Git;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.CommandsDialogs.BrowseDialog;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUI.UserControls;
using GitUIPluginInterfaces;
using ResourceManager;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  The actions of the menu of the Avalonia file status list: a port of the handlers of <c>FileStatusList.ContextMenu</c>
///  for any diff (docs/avalonia-port/PLAN.md, phase 5); keep it in sync. The visibility comes from the WinForms
///  <c>GetSelectionInfo</c>, <c>RevisionDiffController</c> and <c>FileStatusListContextMenuController</c>.
/// </summary>
internal sealed class FileStatusListMenuHost(IGitUICommands commands, DialogWindow window) : IFileStatusListMenuHost
{
    private readonly FileStatusListMenuStrings _strings = Presentation.Translations.ViewStrings.Load<FileStatusListMenuStrings>();
    private readonly IFileStatusListContextMenuController _itemContextMenuController = new FileStatusListContextMenuController();

    private IGitModule Module => commands.Module;

    private AvaloniaDialogs.NativeWindowOwner Owner => new(window);

    private IFullPathResolver FullPathResolver => new FullPathResolver(() => Module.WorkingDir);

    private RevisionDiffController RevisionDiffController => new(() => Module, FullPathResolver);

    /// <summary>As <c>FileStatusList.UpdateStatusOfMenuItems</c> and <c>OpenWithDifftool_DropDownOpening</c>.</summary>
    public FileStatusMenuState GetMenuState(IReadOnlyList<FileStatusEntry> selected, RelativePath? selectedFolder)
    {
        FileStatusItem[] selectedItems = ToItems(selected);
        IFullPathResolver fullPathResolver = FullPathResolver;
        ContextMenuSelectionInfo selectionInfo = FileStatusList.GetSelectionInfo(selectedItems, selectedFolder, isBareRepository: Module.IsBareRepository(), supportLinePatching: false, fullPathResolver);
        RevisionDiffController controller = RevisionDiffController;
        ContextMenuDiffToolInfo diffToolInfo = GetContextMenuDiffToolInfo(selectedItems, fullPathResolver);

        // As InitResetFileToToolStripMenuItem: only the first of multiple parents or children is shown.
        ObjectId selectedId = selectedItems.SecondIds().FirstOrDefault();
        ObjectId parentId = selectedItems.FirstIds().FirstOrDefault();
        bool canReset = controller.ShouldShowResetFileMenus(selectionInfo);
        bool canResetToSecond = canReset && FileStatusList.CanResetToSecond(selectedId);
        bool canResetToFirst = canReset && FileStatusList.CanResetToFirst(parentId, selectedItems);

        bool canOpenFile = selectionInfo.SelectedGitItemCount == 1 && selectionInfo.AllFilesExist;
        return new FileStatusMenuState
        {
            CanOpenWithDifftool = controller.ShouldShowDifftoolMenus(selectionInfo),
            CanDiffFirstToSelected = _itemContextMenuController.ShouldShowMenuFirstToSelected(diffToolInfo),
            CanDiffFirstToLocal = _itemContextMenuController.ShouldShowMenuFirstToLocal(diffToolInfo),
            CanDiffSelectedToLocal = _itemContextMenuController.ShouldShowMenuSelectedToLocal(diffToolInfo),
            ShowDiffToLocal = !_itemContextMenuController.ShouldHideToLocal(diffToolInfo),
            ShowOpenWorkingDirectoryFile = canOpenFile,
            ShowEditWorkingDirectoryFile = controller.ShouldShowMenuEditWorkingDirectoryFile(selectionInfo),
            ShowOpenRevisionFile = controller.ShouldShowMenuOpenRevision(selectionInfo),
            CanOpenRevisionFile = controller.ShouldShowMenuShowInFileTree(selectionInfo),
            ShowSaveAs = controller.ShouldShowMenuSaveAs(selectionInfo),
            CanCopyPaths = controller.ShouldShowMenuCopyFileName(selectionInfo),
            ShowShowInFolder = controller.ShouldShowMenuShowInFolder(selectionInfo),
            CanShowInFolder = selectedItems.Any(item => fullPathResolver.Resolve(item.Item.Name) is string filePath && FormBrowseUtil.FileOrParentDirectoryExists(filePath)),
            CanShowFileHistory = controller.ShouldShowMenuFileHistory(selectionInfo),
            CanBlame = controller.ShouldShowMenuBlame(selectionInfo),
            ResetToSelectedText = canResetToSecond ? _strings.SelectedRevision.Text + GetDescriptionForRevision(selectedId) : null,
            ResetToParentText = canResetToFirst ? _strings.FirstRevision.Text + GetDescriptionForRevision(parentId) : null,
        };
    }

    /// <summary>As <c>FileStatusList.OpenFilesWithDiffTool</c>.</summary>
    public void OpenWithDifftool(IReadOnlyList<FileStatusEntry> selected, DifftoolKind kind)
    {
        RevisionDiffKind diffKind = kind switch
        {
            DifftoolKind.FirstToLocal => RevisionDiffKind.DiffALocal,
            DifftoolKind.SelectedToLocal => RevisionDiffKind.DiffBLocal,
            _ => RevisionDiffKind.DiffAB,
        };
        AvaloniaUi.RunInHostContext(() =>
        {
            foreach (FileStatusEntry entry in selected)
            {
                if (entry.FirstRevision?.ObjectId == ObjectId.CombinedDiffId)
                {
                    // CombinedDiff cannot be viewed in a difftool
                    continue;
                }

                // If FirstRevision is null, compare to root commit
                GitRevision?[] revisions = [entry.SecondRevision, entry.FirstRevision];
                commands.OpenWithDifftool(Owner, revisions, entry.Item.Name, entry.Item.OldName, diffKind, entry.Item.IsTracked);
            }
        });
    }

    /// <summary>As <c>OpenWorkingDirectoryFile_Click</c> and <c>OpenWorkingDirectoryFileWith_Click</c>.</summary>
    public void OpenWorkingDirectoryFile(FileStatusEntry entry, bool openWith)
    {
        if (FullPathResolver.Resolve(entry.Item.Name) is string fileName)
        {
            AvaloniaUi.RunInHostContext(() =>
            {
                if (openWith)
                {
                    OsShellUtil.OpenAs(fileName.ToNativePath());
                }
                else
                {
                    OsShellUtil.Open(fileName.ToNativePath());
                }
            });
        }
    }

    /// <summary>As <c>EditWorkingDirectoryFile_Click</c>.</summary>
    public bool EditWorkingDirectoryFile(FileStatusEntry entry)
    {
        string? fileName = FullPathResolver.Resolve(entry.Item.Name);
        AvaloniaUi.RunInHostContext(() => commands.StartFileEditorDialog(fileName));
        return true;
    }

    /// <summary>As <c>OpenRevisionFile_Click</c> and <c>SaveSelectedItemToTempFile</c>.</summary>
    public void OpenRevisionFile(FileStatusEntry entry, bool openWith)
    {
        ThreadHelper.FileAndForget(async () =>
        {
            ObjectId blob = Module.GetFileBlobHash(entry.Item.Name, entry.SecondRevision.ObjectId);
            if (blob.IsZero)
            {
                return;
            }

            string fileName = (Path.GetTempPath() + PathUtil.GetFileName(entry.Item.Name)).ToNativePath();
            await Module.SaveBlobAsAsync(fileName, blob.ToString());
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (openWith)
            {
                OsShellUtil.OpenAs(fileName);
            }
            else
            {
                OsShellUtil.Open(fileName);
            }
        });
    }

    /// <summary>As <c>SaveAs_Click</c>.</summary>
    public void SaveAs(IReadOnlyList<FileStatusEntry> selected) => AvaloniaUi.RunInHostContext(() =>
    {
        List<FileStatusItem> files = [.. ToItems(selected)];
        Func<string, string?>? userSelection = null;
        if (files.Count == 1)
        {
            userSelection = fullName =>
            {
                using SaveFileDialog dialog = new()
                {
                    InitialDirectory = Path.GetDirectoryName(fullName),
                    FileName = Path.GetFileName(fullName),
                    DefaultExt = Path.GetExtension(fullName),
                    AddExtension = true
                };
                dialog.Filter = $"{_strings.SaveFileFilterCurrentFormat.Text}(*.{dialog.DefaultExt})|*.{dialog.DefaultExt}|{_strings.SaveFileFilterAllFiles.Text}(*.*)|*.*";
                return dialog.ShowDialog(Owner) == DialogResult.OK ? dialog.FileName : null;
            };
        }
        else if (files.Count > 1)
        {
            userSelection = baseSourceDirectory =>
            {
                using FolderBrowserDialog dialog = new() { InitialDirectory = baseSourceDirectory, ShowNewFolderButton = true };
                return dialog.ShowDialog(Owner) == DialogResult.OK ? dialog.SelectedPath : null;
            };
        }

        if (userSelection is not null)
        {
            RevisionDiffController.SaveFiles(files, userSelection);
        }
    });

    /// <summary>As <c>CopyPathsToolStripMenuItem</c> with the paths of <c>FileStatusList</c>.</summary>
    public void CopyPaths(IReadOnlyList<FileStatusEntry> selected, RelativePath? selectedFolder, CopyPathKind kind)
    {
        IEnumerable<string?> paths = selectedFolder is not null ? [selectedFolder.Value] : selected.Select(entry => entry.Item.Name);
        (string prefixDir, Func<string, string> convertPath) = kind switch
        {
            CopyPathKind.FullWsl => (Module.WorkingDir, PathUtil.ToWslPath),
            CopyPathKind.FullCygwin => (Module.WorkingDir, PathUtil.ToCygwinPath),
            CopyPathKind.RelativeNative => ("", PathUtil.ToNativePath),
            CopyPathKind.RelativePosix => ("", PathUtil.ToPosixPath),
            _ => (Module.WorkingDir, (Func<string, string>)PathUtil.ToNativePath),
        };

        string filePaths = paths
            .Where(path => path is not null)
            .Distinct()
            .Select(path => prefixDir.Length == 0 && path!.Length == 0 ? "." : convertPath(Path.Combine(prefixDir, path!)))
            .Join(Environment.NewLine);
        if (!string.IsNullOrWhiteSpace(filePaths))
        {
            ClipboardUtil.TrySetText(filePaths);
        }
    }

    /// <summary>As <c>FormBrowse.OpenContainingFolder</c>.</summary>
    public void ShowInFolder(IReadOnlyList<FileStatusEntry> selected, RelativePath? selectedFolder)
    {
        if (Module.WorkingDir is not string workingDir)
        {
            return;
        }

        IEnumerable<string> relativePaths = selectedFolder is not null
            ? [selectedFolder.Length == 0 ? "" : $"{selectedFolder.Value}/"]
            : selected.Select(entry => entry.Item.Name);
        foreach (string relativePath in relativePaths)
        {
            string filePath = Path.Combine(workingDir, relativePath.ToNativePath());
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                FormBrowseUtil.ShowFileOrParentFolderInFileExplorer(filePath);
            }
        }
    }

    /// <summary>As <c>StartFileHistoryDialog</c> (without a current revision of a file tree).</summary>
    public void ShowFileHistory(FileStatusEntry? entry, RelativePath? selectedFolder, bool blame)
    {
        (string? fileName, GitRevision? revision) = selectedFolder is not null
            ? (selectedFolder.Length == 0 ? null : selectedFolder.Value, null)
            : entry is { Item.IsTracked: true }
                ? (entry.Item.Name, entry.SecondRevision)
                : (null, null);
        if (fileName is not null)
        {
            AvaloniaUi.RunInHostContext(() => commands.StartFileHistoryDialog(Owner, fileName, revision, showBlame: blame));
        }
    }

    /// <summary>As <c>ResetSelectedItemsWithConfirmation</c>.</summary>
    public bool ResetFiles(IReadOnlyList<FileStatusEntry> selected, bool toParent) => AvaloniaUi.RunInHostContext(() =>
    {
        FileStatusItem[] items = ToItems(selected);
        if (items.Length == 0)
        {
            return false;
        }

        // The "new" state could change when resetting, allow user to tick the checkbox.
        bool hasNewFiles = !items.All(item => item.Item.IsChanged);
        bool hasExistingFiles = items.Any(item => !(item.Item.IsUncommittedAdded || (item.Item.IsRenamed && item.Item.Staged == StagedStatus.Index)));

        string revDescription = toParent
            ? $"{_strings.FirstRevision.Text}{DescribeRevisions([.. items.FirstRevs()])}"
            : $"{_strings.SelectedRevision.Text}{DescribeRevisions([.. items.SecondRevs()])}";
        string confirmationMessage = string.Format(_strings.ResetSelectedChanges.Text, revDescription);

        FormResetChanges.ActionEnum resetType = FormResetChanges.ShowResetDialog(Owner, hasExistingFiles, hasNewFiles, confirmationMessage);
        if (resetType == FormResetChanges.ActionEnum.Cancel)
        {
            return false;
        }

        bool resetAndDelete = resetType == FormResetChanges.ActionEnum.ResetAndDelete;
        foreach (ObjectId id in toParent ? items.FirstIds() : items.SecondIds())
        {
            if (toParent ? !FileStatusList.CanResetToFirst(id, items) : !FileStatusList.CanResetToSecond(id))
            {
                // Cannot reset to artificial commit, may be included in multi selections
                continue;
            }

            GitItemStatus[] resetItems = [.. toParent ? items.Items() : items.Items().Select(item => item.InvertStatus())];
            Module.ResetChanges(id, resetItems, resetAndDelete: resetAndDelete, FullPathResolver, out StringBuilder output, progressAction: null);
            if (output.Length > 0)
            {
                MessageBoxes.Show(Owner, output.ToString(), TranslatedStrings.ResetChangesCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        return true;
    });

    private static FileStatusItem[] ToItems(IReadOnlyList<FileStatusEntry> entries)
        => [.. entries.Select(entry => new FileStatusItem(entry.FirstRevision, entry.SecondRevision, entry.Item, entry.BaseA ?? default, entry.BaseB ?? default))];

    /// <summary>As <c>FileStatusList.GetContextMenuDiffToolInfo</c>.</summary>
    private static ContextMenuDiffToolInfo GetContextMenuDiffToolInfo(FileStatusItem[] selectedItems, IFullPathResolver fullPathResolver)
    {
        List<GitRevision> revisions = [.. selectedItems.SecondRevs()];
        GitRevision? selectedRev = revisions.Count == 1 ? revisions[0] : null;
        List<ObjectId> parentIds = [.. selectedItems.FirstIds()];
        GitRevisionTester revisionTester = new(fullPathResolver);
        return new ContextMenuDiffToolInfo(
            selectedRevision: selectedRev,
            selectedItemParentRevs: parentIds,
            allAreNew: selectedItems.All(i => i.Item.IsNew),
            allAreDeleted: selectedItems.All(i => i.Item.IsDeleted),
            firstIsParent: revisionTester.AllFirstAreParentsToSelected(parentIds, selectedRev),
            localExists: revisionTester.AnyLocalFileExists(selectedItems.Select(i => i.Item)));
    }

    /// <summary>As <c>FileStatusList.DescribeRevisions</c>.</summary>
    private string? DescribeRevisions(List<GitRevision> revisions) => revisions.Count switch
    {
        1 => GetDescriptionForRevision(revisions[0]?.ObjectId ?? default),
        > 1 => _strings.MultipleDescription.Text,
        _ => null
    };

    /// <summary>As <c>FileStatusList.GetDescriptionForRevision</c> without a describer.</summary>
    private static string GetDescriptionForRevision(ObjectId objectId)
        => objectId == ObjectId.WorkTreeId ? ResourceManager.TranslatedStrings.Workspace
            : objectId == ObjectId.IndexId ? ResourceManager.TranslatedStrings.Index
            : objectId.ToShortString();
}
