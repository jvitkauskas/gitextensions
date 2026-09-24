using System.Text;
using GitCommands;
using GitCommands.Git;
using GitCommands.Git.Extended;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.CommandsDialogs.BrowseDialog;
using GitUI.HelperDialogs;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUI.ScriptsEngine;
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

    // As FileStatusList: the remembered file is shared by all lists.
    private readonly RememberFileContextMenuController _rememberFileContextMenuController = RememberFileContextMenuController.Default;

    private IGitModule Module => commands.Module;

    private AvaloniaDialogs.NativeWindowOwner Owner => new(window);

    private IFullPathResolver FullPathResolver => new FullPathResolver(() => Module.WorkingDir);

    private RevisionDiffController RevisionDiffController => new(() => Module, FullPathResolver);

    /// <summary>As <c>FileStatusList.UpdateStatusOfMenuItems</c> and <c>OpenWithDifftool_DropDownOpening</c>.</summary>
    public FileStatusMenuState GetMenuState(IReadOnlyList<FileStatusEntry> selected, RelativePath? selectedFolder, FileStatusEntry? focused, bool supportLinePatching)
    {
        FileStatusItem[] selectedItems = ToItems(selected);
        IFullPathResolver fullPathResolver = FullPathResolver;
        ContextMenuSelectionInfo selectionInfo = FileStatusList.GetSelectionInfo(selectedItems, selectedFolder, isBareRepository: Module.IsBareRepository(), supportLinePatching, fullPathResolver);
        RevisionDiffController controller = RevisionDiffController;
        ContextMenuDiffToolInfo diffToolInfo = GetContextMenuDiffToolInfo(selectedItems, fullPathResolver);

        // As InitResetFileToToolStripMenuItem: only the first of multiple parents or children is shown.
        ObjectId selectedId = selectedItems.SecondIds().FirstOrDefault();
        ObjectId parentId = selectedItems.FirstIds().FirstOrDefault();
        bool canReset = controller.ShouldShowResetFileMenus(selectionInfo);
        bool canResetToSecond = canReset && FileStatusList.CanResetToSecond(selectedId);
        bool canResetToFirst = canReset && FileStatusList.CanResetToFirst(parentId, selectedItems);

        bool canOpenFile = selectionInfo.SelectedGitItemCount == 1 && selectionInfo.AllFilesExist;

        // As UpdateStatusOfMenuItems.
        bool isSubmodule = selectionInfo.SelectedGitItemCount == 1 && selectionInfo.IsAnySubmodule;
        bool isSingleFile = selectionInfo.SelectedGitItemCount == 1 && !isSubmodule;
        bool canIgnoreFiles = selectionInfo.IsAnyItemWorkTree && !isSubmodule;

        // As OpenWithDifftool_DropDownOpening: the order is the order in the list, but the (last) selected is known.
        int firstIndex = selectedItems.Length == 2 && focused is not null && focused.Equals(selected[0]) ? 1 : 0;
        int secondIndex = 1 - firstIndex;
        FileStatusItem? remembered = _rememberFileContextMenuController.RememberedDiffFileItem;
        FileStatusItem? single = selectedItems.Length == 1 ? selectedItems[0] : null;
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
            ShowSubmoduleItems = controller.ShouldShowSubmoduleMenus(selectionInfo),
            ShowStage = controller.ShouldShowMenuStage(selectionInfo),
            ShowUnstage = controller.ShouldShowMenuUnstage(selectionInfo),
            ShowResetChunkAndInteractiveAdd = selectionInfo.IsAnyItemWorkTree && isSingleFile,
            ShowCherryPick = controller.ShouldShowMenuCherryPick(selectionInfo),
            ShowRememberDiff = single is not null,
            CanRememberSecondRevDiff = single is not null && _rememberFileContextMenuController.ShouldEnableFirstItemDiff(single, isSecondRevision: true),
            CanRememberFirstRevDiff = single is not null && _rememberFileContextMenuController.ShouldEnableFirstItemDiff(single, isSecondRevision: false),
            ShowDiffTwoSelected = selectedItems.Length == 2,
            CanDiffTwoSelected = selectedItems.Length == 2
                && _rememberFileContextMenuController.ShouldEnableFirstItemDiff(selectedItems[firstIndex])
                && _rememberFileContextMenuController.ShouldEnableSecondItemDiff(selectedItems[secondIndex]),
            DiffWithRememberedText = single is not null && remembered is not null
                ? TranslatedText.ToAccessKeyText(string.Format(_strings.DiffSelectedWithRememberedFile.Text, remembered.Item.Name.Replace("&", "&&")))
                : null,
            CanDiffWithRemembered = single is not null && !IsRemembered(single) && _rememberFileContextMenuController.ShouldEnableSecondItemDiff(single),
            ShowOpenInVisualStudio = controller.ShouldShowMenuEditWorkingDirectoryFile(selectionInfo) && VisualStudioIntegration.IsVisualStudioInstalled,
            ShowMove = controller.ShouldShowMenuMove(selectionInfo),
            DeleteFileText = controller.ShouldShowMenuDeleteFile(selectionInfo) ? TranslatedText.ToAccessKeyText(ResourceManager.TranslatedStrings.GetDeleteFile(selectionInfo.SelectedGitItemCount)) : null,
            ShowShowInFileTree = controller.ShouldShowMenuShowInFileTree(selectionInfo),
            CanFilterFileInGrid = controller.ShouldShowMenuFileHistory(selectionInfo),
            ShowFindFile = true,
            ShowIgnore = canIgnoreFiles,
            ShowSkipWorktreeAndAssumeUnchanged = canIgnoreFiles && selectionInfo.IsAnyTracked,
            IsSkipWorktree = selectedItems.Any(item => item.Item.IsSkipWorktree),
            IsAssumeUnchanged = selectedItems.Any(item => item.Item.IsAssumeUnchanged),
            ShowStopTracking = isSingleFile && selectionInfo.IsAnyTracked,
            Scripts = GetScripts(),
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

    /// <summary>As <c>StageFile_Click</c> without the stage action of a dialog.</summary>
    public void StageFiles(IReadOnlyList<FileStatusEntry> selected)
    {
        GitItemStatus[] files = [.. selected.Where(entry => entry.Item.Staged == StagedStatus.WorkTree).Select(entry => entry.Item)];
        Module.StageFiles(files, out _);
    }

    /// <summary>As <c>UnstageFile_Click</c> without the unstage action of a dialog.</summary>
    public void UnstageFiles(IReadOnlyList<FileStatusEntry> selected)
    {
        GitItemStatus[] files = [.. selected.Where(entry => entry.Item.Staged == StagedStatus.Index).Select(entry => entry.Item)];
        Module.BatchUnstageFiles(files);
    }

    /// <summary>As <c>ResetChunkOfFile_Click</c>.</summary>
    public async Task ResetChunkOfFileAsync(FileStatusEntry entry)
    {
        await Module.ResetInteractiveAsync(entry.Item);
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
    }

    /// <summary>As <c>InteractiveAdd_Click</c>.</summary>
    public async Task InteractiveAddAsync(FileStatusEntry entry)
    {
        await Module.AddInteractiveAsync(entry.Item);
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
    }

    /// <summary>
    ///  As <c>RememberFirstRevDiff_Click</c> and <c>RememberSecondRevDiff_Click</c>. Deliberate difference: the first revision
    ///  is remembered with an inverted copy of the status, where WinForms swaps the names of the file of the list.
    /// </summary>
    public void RememberDiff(FileStatusEntry entry, bool first)
    {
        FileStatusItem item = ToItems([entry])[0];
        if (first)
        {
            if (entry.FirstRevision is null)
            {
                return;
            }

            item = new FileStatusItem(firstRev: entry.SecondRevision, secondRev: entry.FirstRevision, item: string.IsNullOrWhiteSpace(entry.Item.OldName) ? entry.Item : entry.Item.InvertStatus());
        }

        _rememberFileContextMenuController.RememberedDiffFileItem = item;
    }

    /// <summary>As <c>DiffWithRemembered_Click</c>.</summary>
    public void DiffWithRemembered(FileStatusEntry entry)
    {
        FileStatusItem item = ToItems([entry])[0];

        // For first item, the second revision is explicitly remembered
        string? first = _rememberFileContextMenuController.GetGitCommit(Module.GetFileBlobHash, _rememberFileContextMenuController.RememberedDiffFileItem, isSecondRevision: true);

        // Fallback to first revision if second cannot be used
        bool isSecond = _rememberFileContextMenuController.ShouldEnableSecondItemDiff(item, isSecondRevision: true);
        string? second = _rememberFileContextMenuController.GetGitCommit(Module.GetFileBlobHash, item, isSecondRevision: isSecond);
        AvaloniaUi.RunInHostContext(() => Module.OpenFilesWithDifftool(first, second, customTool: null));
    }

    /// <summary>As <c>DiffTwoSelected_Click</c>.</summary>
    public void DiffTwoSelected(IReadOnlyList<FileStatusEntry> selected, FileStatusEntry? focused)
    {
        FileStatusItem[] diffFiles = ToItems(selected);
        if (diffFiles.Length != 2)
        {
            return;
        }

        // The order is always the order in the list, not clicked order, but the (last) selected is known
        int firstIndex = focused is not null && focused.Equals(selected[0]) ? 1 : 0;
        int secondIndex = 1 - firstIndex;

        // Fallback to first revision if second revision cannot be used
        bool isFirstItemSecondRev = _rememberFileContextMenuController.ShouldEnableFirstItemDiff(diffFiles[firstIndex], isSecondRevision: true);
        string? first = _rememberFileContextMenuController.GetGitCommit(Module.GetFileBlobHash, diffFiles[firstIndex], isSecondRevision: isFirstItemSecondRev);
        bool isSecondItemSecondRev = _rememberFileContextMenuController.ShouldEnableSecondItemDiff(diffFiles[secondIndex], isSecondRevision: true);
        string? second = _rememberFileContextMenuController.GetGitCommit(Module.GetFileBlobHash, diffFiles[secondIndex], isSecondRevision: isSecondItemSecondRev);
        AvaloniaUi.RunInHostContext(() => Module.OpenFilesWithDifftool(first, second, customTool: null));
    }

    /// <summary>As <c>OpenInVisualStudio_Click</c> (without the line of the viewer).</summary>
    public void OpenInVisualStudio(FileStatusEntry entry)
    {
        if (VisualStudioIntegration.IsVisualStudioInstalled && FullPathResolver.Resolve(entry.Item.Name)?.NormalizePath() is string itemName)
        {
            AvaloniaUi.RunInHostContext(() => VisualStudioIntegration.OpenFile(itemName));
        }
    }

    /// <summary>As <c>Move_Click</c>: asked again (with the last name) until the name is valid, git succeeds or the user cancels.</summary>
    public bool Move(FileStatusEntry? entry, RelativePath? selectedFolder) => AvaloniaUi.RunInHostContext(() =>
    {
        string? oldName = entry?.Item.Name;
        bool isFolder = oldName is null;
        oldName ??= selectedFolder?.Value;
        if (oldName is null)
        {
            return false;
        }

        string? title = _strings.Move.Text.RemoveMnemonicMarker();
        string newName = oldName;
        bool refresh = false;
        while (true)
        {
            using IUserInputPrompt prompt = commands.GetRequiredService<ISimplePromptCreator>().Create(title, label: _strings.NewName.Text, defaultValue: newName);
            if (prompt.ShowDialog(Owner) != DialogResult.OK)
            {
                return refresh;
            }

            newName = prompt.UserInput;
            MoveCommand.Arguments arguments = new(isFolder, oldName, NewName: newName);
            MoveCommand moveCommand = new(Module.GitExecutable);
            if (!moveCommand.Validate(arguments))
            {
                continue;
            }

            refresh = true;
            try
            {
                moveCommand.Execute(arguments);
                return true;
            }
            catch (Exception exception)
            {
                MessageBoxes.ShowError(Owner, exception.Message, title);
            }
        }
    });

    /// <summary>As <c>DeleteFile_Click</c>: the files (not the submodules) of an artificial commit, unstaged first.</summary>
    public bool DeleteFiles(IReadOnlyList<FileStatusEntry> selected) => AvaloniaUi.RunInHostContext(() =>
    {
        FileStatusItem[] items = ToItems(selected);
        bool refresh = false;
        try
        {
            if (items.Length == 0 || !items[0].SecondRevision.IsArtificial
                || MessageBoxes.Show(Owner, _strings.DeleteSelectedFiles.Text, _strings.DeleteSelectedFilesCaption.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return false;
            }

            refresh = true;
            IEnumerable<FileStatusItem> files = items.Where(item => !item.Item.IsSubmodule);

            // If any file is staged, it must be unstaged
            Module.BatchUnstageFiles(files.Where(item => item.Item.Staged == StagedStatus.Index).Select(item => item.Item));
            foreach (FileStatusItem item in files)
            {
                string? path = FullPathResolver.Resolve(item.Item.Name);
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }
                else
                {
                    File.Delete(path!);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBoxes.Show(Owner, _strings.DeleteFailed.Text + Environment.NewLine + ex.Message, TranslatedStrings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        return refresh;
    });

    /// <summary>As <c>FindFile_Click</c>, with the WinForms search window.</summary>
    public GitItemStatus? FindFile(IReadOnlyList<GitItemStatus> candidates) => AvaloniaUi.RunInHostContext(() =>
    {
        IFindFilePredicateProvider findFilePredicateProvider = new FindFilePredicateProvider();
        string workingDir = Module.WorkingDir;
        using SearchWindow<GitItemStatus> searchWindow = new(name =>
        {
            Func<string?, bool> predicate = findFilePredicateProvider.Get(name, workingDir);
            return candidates.Where(item => predicate(item.Name) || predicate(item.OldName));
        });
        searchWindow.ShowDialog(Owner);
        return searchWindow.SelectedItem;
    });

    /// <summary>As <c>AddFileToIgnoreFile</c>.</summary>
    public bool AddToIgnoreFile(IReadOnlyList<FileStatusEntry> selected, RelativePath? selectedFolder, bool localExclude)
    {
        string[] fileNames = selectedFolder is { Length: > 0 } folder
            ? [$"/{folder.Value}/"]
            : [.. selected.Select(entry => "/" + entry.Item.Name)];
        return fileNames.Length > 0 && AvaloniaUi.RunInHostContext(() => commands.StartAddToGitIgnoreDialog(Owner, localExclude, fileNames));
    }

    /// <summary>As <c>SkipWorktree_Click</c>.</summary>
    public void SetSkipWorktree(IReadOnlyList<FileStatusEntry> selected, bool skipWorktree)
        => Module.SkipWorktreeFiles([.. selected.Select(entry => entry.Item)], skipWorktree, out _);

    /// <summary>As <c>AssumeUnchanged_Click</c>.</summary>
    public void SetAssumeUnchanged(IReadOnlyList<FileStatusEntry> selected, bool assumeUnchanged)
        => Module.AssumeUnchangedFiles([.. selected.Select(entry => entry.Item)], assumeUnchanged, out _);

    /// <summary>As <c>StopTracking_Click</c>.</summary>
    public bool StopTracking(FileStatusEntry entry)
    {
        string filename = entry.Item.Name;
        if (Module.StopTrackingFile(filename))
        {
            return true;
        }

        AvaloniaUi.RunInHostContext(() => MessageBoxes.Show(Owner, string.Format(_strings.StopTrackingFail.Text, filename), TranslatedStrings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error));
        return false;
    }

    /// <summary>
    ///  As <c>UpdateSubmodule_Click</c>, <c>ResetSubmoduleChanges_Click</c>, <c>StashSubmoduleChanges_Click</c> and
    ///  <c>CommitSubmoduleChanges_Click</c>.
    /// </summary>
    public bool RunSubmoduleAction(IReadOnlyList<FileStatusEntry> selected, SubmoduleMenuAction action) => AvaloniaUi.RunInHostContext(() =>
    {
        string[] submodules = [.. selected.Where(entry => entry.Item.IsSubmodule).Select(entry => entry.Item.Name).Distinct()];
        switch (action)
        {
            case SubmoduleMenuAction.Update:
                FormProcess.ShowDialog(Owner, commands, arguments: Commands.SubmoduleUpdate(submodules), Module.WorkingDir, input: null, useDialogSettings: true);
                break;
            case SubmoduleMenuAction.Reset:
                // Show a form asking the user if they want to reset the changes.
                FormResetChanges.ActionEnum resetType = FormResetChanges.ShowResetDialog(Owner, true, true);
                if (resetType == FormResetChanges.ActionEnum.Cancel)
                {
                    return false;
                }

                foreach (string name in submodules)
                {
                    Module.GetSubmodule(name).ResetAllChanges(clean: resetType == FormResetChanges.ActionEnum.ResetAndDelete);
                }

                break;
            case SubmoduleMenuAction.Stash:
                foreach (string name in submodules)
                {
                    commands.WithGitModule(Module.GetSubmodule(name)).StashSave(Owner, AppSettings.IncludeUntrackedFilesInManualStash);
                }

                break;
            case SubmoduleMenuAction.Commit:
                foreach (string name in submodules)
                {
                    commands.WithWorkingDirectory(FullPathResolver.Resolve(name.EnsureTrailingPathSeparator())).StartCommitDialog(Owner);
                }

                break;
        }

        return true;
    });

    /// <summary>As <c>ExecuteCommand</c> of a script: the selected files (or folder) are the files of the script.</summary>
    public void RunScript(FileStatusScript script, IReadOnlyList<FileStatusEntry> selected, RelativePath? selectedFolder)
    {
        if (commands.GetRequiredService<IScriptsManager>().GetScript(script.Id) is not { } scriptInfo)
        {
            return;
        }

        string[] paths = selectedFolder is not null ? [selectedFolder.Value] : [.. selected.Select(entry => entry.Item.Name)];
        AvaloniaUi.RunInHostContext(() => commands.GetRequiredService<IScriptsRunner>().RunScript(scriptInfo, Owner, commands, new ScriptOptionsProvider(() => paths, () => null, () => null)));
    }

    /// <summary>As <c>AddUserScripts</c>: the enabled scripts, the ones of <c>ScriptEvent.ShowInFileList</c> in the menu itself.</summary>
    private IReadOnlyList<FileStatusScript> GetScripts()
        => commands.GetService(typeof(IScriptsManager)) is IScriptsManager scriptsManager
            ? [.. scriptsManager.GetScripts().Where(script => script.Enabled)
                .Select(script => new FileStatusScript(script.Name ?? "", script.HotkeyCommandIdentifier, IsDirect: script.OnEvent == ScriptEvent.ShowInFileList))]
            : [];

    /// <summary>As <c>diffFiles[0] != RememberedDiffFileItem</c>: the remembered second revision of this file.</summary>
    private bool IsRemembered(FileStatusItem item)
        => _rememberFileContextMenuController.RememberedDiffFileItem is { } remembered
            && ReferenceEquals(remembered.Item, item.Item)
            && remembered.SecondRevision.ObjectId == item.SecondRevision.ObjectId;

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
