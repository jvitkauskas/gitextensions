using System.Text;
using GitCommands;
using GitCommands.Git;
using GitCommands.Git.Extended;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.CommandsDialogs;
using GitUI.CommandsDialogs.BrowseDialog;
using GitUI.ScriptsEngine;
using GitUI.UserControls;
using GitUI.UserControls.RevisionGrid;
using GitUIPluginInterfaces;
using ResourceManager;

namespace GitUI;

/// <summary>The rules of the context menu of the files on the selection (moved out of the WinForms <c>FileStatusList</c>).</summary>
internal static class FileStatusListSelection
{
    internal static ContextMenuSelectionInfo GetSelectionInfo(FileStatusItem[] selectedItems, RelativePath? selectedFolder, bool isBareRepository, bool supportLinePatching, IFullPathResolver fullPathResolver)
    {
        // Some items are not supported if more than one revision is selected
        List<GitRevision> revisions = [.. selectedItems.SecondRevs()];
        GitRevision? selectedRev = revisions.Count == 1 ? revisions[0] : null;

        // First (A) is parent if one revision selected or if parent, then selected
        List<ObjectId> parentIds = [.. selectedItems.FirstIds()];

        // Combined diff, range diff etc are for display only, no manipulations
        bool isStatusOnly = selectedItems.Any(item => item.Item.IsRangeDiff || item.Item.IsStatusOnly);
        bool isDisplayOnlyDiff = parentIds.Contains(ObjectId.CombinedDiffId) || isStatusOnly;
        int selectedGitItemCount = selectedItems.Length;

        bool isAnyTracked = selectedItems.Any(item => item.Item.IsTracked);
        bool isAnyIndex = selectedItems.Any(item => item.Item.Staged == StagedStatus.Index);
        bool isAnyWorkTree = selectedItems.Any(item => item.Item.Staged == StagedStatus.WorkTree);
        bool supportPatches = selectedGitItemCount == 1 && supportLinePatching;
        bool isDeleted = selectedItems.Any(item => item.Item.IsDeleted);
        bool isAnySubmodule = selectedItems.Any(item => item.Item.IsSubmodule);
        (bool allFilesExist, bool allDirectoriesExist, bool allFilesOrUntrackedDirectoriesExist) = FileOrUntrackedDirExists(selectedItems, fullPathResolver);

        ContextMenuSelectionInfo selectionInfo = new(
            SelectedRevision: selectedRev,
            SelectedFolder: selectedFolder,
            IsDisplayOnlyDiff: isDisplayOnlyDiff,
            IsStatusOnly: isStatusOnly,
            SelectedGitItemCount: selectedGitItemCount,
            IsAnyItemIndex: isAnyIndex,
            IsAnyItemWorkTree: isAnyWorkTree,
            IsBareRepository: isBareRepository,
            AllFilesExist: allFilesExist,
            AllDirectoriesExist: allDirectoriesExist,
            AllFilesOrUntrackedDirectoriesExist: allFilesOrUntrackedDirectoriesExist,
            IsAnyTracked: isAnyTracked,
            SupportPatches: supportPatches,
            IsDeleted: isDeleted,
            IsAnySubmodule: isAnySubmodule);
        return selectionInfo;

        static (bool allFilesExist, bool allDirectoriesExist, bool allFilesOrUntrackedDirectoriesExist) FileOrUntrackedDirExists(FileStatusItem[] items, IFullPathResolver fullPathResolver)
        {
            bool allFilesExist = items.Length != 0;
            bool allDirectoriesExist = allFilesExist;
            bool allFilesOrUntrackedDirectoriesExist = allFilesExist;
            foreach (FileStatusItem item in items)
            {
                string? path = fullPathResolver.Resolve(item.Item.Name);
                bool fileExists = File.Exists(path);
                bool directoryExists = Directory.Exists(path);
                allFilesExist &= fileExists;
                allDirectoriesExist &= directoryExists;
                bool fileOrUntrackedDirectoryExists = fileExists || (!item.Item.IsTracked && allDirectoriesExist);
                allFilesOrUntrackedDirectoriesExist &= fileOrUntrackedDirectoryExists;

                if (!allFilesExist && !allDirectoriesExist && !allFilesOrUntrackedDirectoriesExist)
                {
                    break;
                }
            }

            return (allFilesExist, allDirectoriesExist, allFilesOrUntrackedDirectoriesExist);
        }
    }

    /// <summary>
    ///  Return whether it is possible to reset to the first commit.
    /// </summary>
    /// <param name="parentId">The parent commit id.</param>
    /// <param name="selectedItems">The selected file status items.</param>
    /// <returns><see langword="true"/> if it is possible to reset to first id.</returns>
    internal static bool CanResetToFirst(ObjectId parentId, IEnumerable<FileStatusItem> selectedItems)
    {
        return CanResetToSecond(parentId) || (parentId == ObjectId.IndexId && selectedItems.SecondIds().All(i => i == ObjectId.WorkTreeId));
    }

    /// <summary>
    ///  Return whether it is possible to reset to the second (selected) commit.
    /// </summary>
    /// <param name="resetId">The selected commit id.</param>
    /// <returns><see langword="true"/> if it is possible to reset to first id.</returns>
    internal static bool CanResetToSecond(ObjectId resetId) => !resetId.IsZeroOrArtificial;
}
