using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.UserControls.FileStatusList;

/// <summary>Strings of the menu of the file status list; ids match <c>FileStatusList</c>.</summary>
public sealed class FileStatusListMenuStrings : ViewStrings
{
    public FileStatusListMenuStrings()
        : base("FileStatusList")
    {
        OpenWithDifftool = Add("tsmiOpenWithDifftool", "Text", "Open with &difftool");
        DiffFirstToSelected = Add("tsmiDiffFirstToSelected", "Text", "&First -> Second");
        DiffFirstToLocal = Add("tsmiDiffFirstToLocal", "Text", "First -> &Working directory");
        DiffSelectedToLocal = Add("tsmiDiffSelectedToLocal", "Text", "&Second -> Working directory");
        OpenWorkingDirectoryFile = Add("tsmiOpenWorkingDirectoryFile", "Text", "&Open working directory file");
        OpenWorkingDirectoryFileWith = Add("tsmiOpenWorkingDirectoryFileWith", "Text", "Open working directory file with...");
        EditWorkingDirectoryFile = Add("tsmiEditWorkingDirectoryFile", "Text", "&Edit working directory file");
        OpenRevisionFile = Add("tsmiOpenRevisionFile", "Text", "Ope&n this revision (temp file)");
        OpenRevisionFileWith = Add("tsmiOpenRevisionFileWith", "Text", "Open this revision &with... (temp file)");
        SaveAs = Add("tsmiSaveAs", "Text", "S&ave selected as...");
        CopyPaths = Add("tsmiCopyPaths", "Text", "Copy &path(s)");
        ShowInFolder = Add("tsmiShowInFolder", "Text", "Show &in folder");
        FileHistory = Add("tsmiFileHistory", "Text", "File &history");
        Blame = Add("tsmiBlame", "Text", "&Blame");
        ResetFileTo = Add("tsmiResetFileTo", "Text", "&Reset file(s) to");
        FirstRevision = Add("_firstRevision", "Text", "First: A ");
        SelectedRevision = Add("_selectedRevision", "Text", "Second: B ");
        MultipleDescription = Add("_multipleDescription", "Text", "<multiple>");
        ResetSelectedChanges = Add("_resetSelectedChangesText", "Text", "Are you sure you want to reset all selected files to {0}?");
        SaveFileFilterAllFiles = Add("_saveFileFilterAllFiles", "Text", "All files");
        SaveFileFilterCurrentFormat = Add("_saveFileFilterCurrentFormat", "Text", "Current format");
        SortBy = Add("_sortByContextMenu", "Text", "&Sort and group by");
        GroupByFilePathTree = Add("tsmiGroupByFilePathTree", "Text", "Group by file &path - tree");
        GroupByFilePathFlat = Add("tsmiGroupByFilePathFlat", "Text", "Group by &file path - flat");
        GroupByFileExtensionTree = Add("tsmiGroupByFileExtensionTree", "Text", "Group by file &extension - tree");
        GroupByFileExtensionFlat = Add("tsmiGroupByFileExtensionFlat", "Text", "Group by file e&xtension - flat");
        GroupByFileStatusTree = Add("tsmiGroupByFileStatusTree", "Text", "Group by file &status - tree");
        GroupByFileStatusFlat = Add("tsmiGroupByFileStatusFlat", "Text", "Group by file s&tatus - flat");
    }

    public TranslatedText OpenWithDifftool { get; }

    public TranslatedText DiffFirstToSelected { get; }

    public TranslatedText DiffFirstToLocal { get; }

    public TranslatedText DiffSelectedToLocal { get; }

    public TranslatedText OpenWorkingDirectoryFile { get; }

    public TranslatedText OpenWorkingDirectoryFileWith { get; }

    public TranslatedText EditWorkingDirectoryFile { get; }

    public TranslatedText OpenRevisionFile { get; }

    public TranslatedText OpenRevisionFileWith { get; }

    public TranslatedText SaveAs { get; }

    public TranslatedText CopyPaths { get; }

    public TranslatedText ShowInFolder { get; }

    public TranslatedText FileHistory { get; }

    public TranslatedText Blame { get; }

    public TranslatedText ResetFileTo { get; }

    public TranslatedText FirstRevision { get; }

    public TranslatedText SelectedRevision { get; }

    public TranslatedText MultipleDescription { get; }

    public TranslatedText ResetSelectedChanges { get; }

    public TranslatedText SaveFileFilterAllFiles { get; }

    public TranslatedText SaveFileFilterCurrentFormat { get; }

    public TranslatedText SortBy { get; }

    public TranslatedText GroupByFilePathTree { get; }

    public TranslatedText GroupByFilePathFlat { get; }

    public TranslatedText GroupByFileExtensionTree { get; }

    public TranslatedText GroupByFileExtensionFlat { get; }

    public TranslatedText GroupByFileStatusTree { get; }

    public TranslatedText GroupByFileStatusFlat { get; }
}

/// <summary>Strings of the copy paths menu; ids match <c>CopyPathsToolStripMenuItem</c>, which is translated with <c>FormBrowse</c>.</summary>
public sealed class CopyPathsStrings : ViewStrings
{
    public CopyPathsStrings()
        : base("FormBrowse")
    {
        FullNative = Add("copyFullPathsNativeToolStripMenuItem", "Text", "Copy &full path(s) - native");
        FullWsl = Add("copyFullPathsWslToolStripMenuItem", "Text", "Copy full path(s) - &WSL");
        FullCygwin = Add("copyFullPathsCygwinToolStripMenuItem", "Text", "Copy full path(s) - &Cygwin");
        RelativeNative = Add("copyRelativePathsNativeToolStripMenuItem", "Text", "Copy relative path(s) - &native");
        RelativePosix = Add("copyRelativePathsPosixToolStripMenuItem", "Text", "Copy relative path(s) - &POSIX");
    }

    public TranslatedText FullNative { get; }

    public TranslatedText FullWsl { get; }

    public TranslatedText FullCygwin { get; }

    public TranslatedText RelativeNative { get; }

    public TranslatedText RelativePosix { get; }
}

/// <summary>The difftool comparisons of the menu (the WinForms <c>RevisionDiffKind</c>).</summary>
public enum DifftoolKind
{
    /// <summary>First (A) to second (B), <c>DiffAB</c>.</summary>
    FirstToSelected,

    /// <summary>First (A) to the working directory, <c>DiffALocal</c>.</summary>
    FirstToLocal,

    /// <summary>Second (B) to the working directory, <c>DiffBLocal</c>.</summary>
    SelectedToLocal,
}

/// <summary>How paths are copied (the items of <c>CopyPathsToolStripMenuItem</c>).</summary>
public enum CopyPathKind
{
    FullNative,
    FullWsl,
    FullCygwin,
    RelativeNative,
    RelativePosix,
}

/// <summary>
///  Which items of the menu are shown and enabled for the selection (as <c>FileStatusList.UpdateStatusOfMenuItems</c> decides
///  with <c>RevisionDiffController</c>).
/// </summary>
public sealed record FileStatusMenuState
{
    public static FileStatusMenuState None { get; } = new();

    public bool CanOpenWithDifftool { get; init; }

    public bool CanDiffFirstToSelected { get; init; }

    public bool CanDiffFirstToLocal { get; init; }

    public bool CanDiffSelectedToLocal { get; init; }

    /// <summary>Whether the comparisons to the working directory are shown (not for the working directory itself).</summary>
    public bool ShowDiffToLocal { get; init; } = true;

    public bool ShowOpenWorkingDirectoryFile { get; init; }

    public bool ShowEditWorkingDirectoryFile { get; init; }

    public bool ShowOpenRevisionFile { get; init; }

    public bool CanOpenRevisionFile { get; init; }

    public bool ShowSaveAs { get; init; }

    public bool CanCopyPaths { get; init; }

    public bool ShowShowInFolder { get; init; }

    public bool CanShowInFolder { get; init; }

    public bool CanShowFileHistory { get; init; }

    public bool CanBlame { get; init; }

    public bool CanResetFileTo => ResetToSelectedText is not null || ResetToParentText is not null;

    /// <summary>The text of "reset to the second (B) revision", <see langword="null"/> if not possible.</summary>
    public string? ResetToSelectedText { get; init; }

    /// <summary>The text of "reset to the first (A) revision", <see langword="null"/> if not possible.</summary>
    public string? ResetToParentText { get; init; }
}

/// <summary>The actions of the menu of the file status list that need the host (git, the shell, the other dialogs).</summary>
public interface IFileStatusListMenuHost
{
    /// <param name="selectedFolder">The selected folder, if a single folder is selected (as <c>FileStatusList.SelectedFolder</c>).</param>
    FileStatusMenuState GetMenuState(IReadOnlyList<FileStatusEntry> selected, RelativePath? selectedFolder);

    void OpenWithDifftool(IReadOnlyList<FileStatusEntry> selected, DifftoolKind kind);

    void OpenWorkingDirectoryFile(FileStatusEntry entry, bool openWith);

    /// <summary>Edits the file in the file editor; returns whether the list should be refreshed.</summary>
    bool EditWorkingDirectoryFile(FileStatusEntry entry);

    void OpenRevisionFile(FileStatusEntry entry, bool openWith);

    void SaveAs(IReadOnlyList<FileStatusEntry> selected);

    void CopyPaths(IReadOnlyList<FileStatusEntry> selected, RelativePath? selectedFolder, CopyPathKind kind);

    void ShowInFolder(IReadOnlyList<FileStatusEntry> selected, RelativePath? selectedFolder);

    void ShowFileHistory(FileStatusEntry? entry, RelativePath? selectedFolder, bool blame);

    /// <summary>Resets the files after a confirmation; returns whether the list should be refreshed.</summary>
    bool ResetFiles(IReadOnlyList<FileStatusEntry> selected, bool toParent);
}
