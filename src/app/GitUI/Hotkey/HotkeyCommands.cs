namespace GitUI.Hotkey;

/// <summary>
///  The commands of the hotkeys and the names of their settings, as the WinForms forms and controls defined them (e.g.
///  <c>FormBrowse.Command</c> and <c>FormBrowse.HotkeySettingsName</c>): the saved hotkeys refer to them by name and value.
/// </summary>
public static class HotkeyCommands
{
    /// <summary>The hotkey settings of <c>FormBrowse</c>.</summary>
    public const string BrowseSettingsName = "Browse";

    /// <summary>The commands of <c>FormBrowse.Command</c>.</summary>
    public enum Browse
    {
        // Focus or visuals
        FocusLeftPanel = 25,
        FocusRevisionGrid = 3,
        FocusCommitInfo = 4,
        FocusDiff = 5,
        FocusFileTree = 6,
        FocusGpgInfo = 26,
        FocusGitConsole = 29,
        FocusBuildServerStatus = 30,
        FocusOutputHistoryAndToggleIfPanel = 47,
        FocusNextTab = 31,
        FocusPrevTab = 32,

        FocusFilter = 18,

        ToggleLeftPanel = 21,

        // START menu
        OpenRepo = 45,

        // DASHBOARD menu

        // REPOSITORY menu
        CloseRepository = 15,
        ManageWorkTrees = 49,

        // COMMANDS menu
        Commit = 7,
        CheckoutBranch = 10,
        PullOrFetch = 39,
        Push = 40,
        CreateBranch = 41,
        MergeBranches = 42,
        CreateTag = 43,
        Rebase = 44,

        // PLUGINS menu

        // TOOLS menu
        GitBash = 0,
        GitGui = 1,
        GitGitK = 2,
        OpenSettings = 20,

        // HELP menu

        // Toolbar
        AddNotes = 8,
        FindFileInSelectedCommit = 9,
        QuickPullOrFetch = 48, // Default user action configured in toolbar
        QuickFetch = 11,
        QuickPull = 12,
        QuickPush = 13,
        Stash = 16,
        StashStaged = 46,
        StashPop = 17,
        GoToSuperproject = 27,
        GoToSubmodule = 28,

        // Diff or File Tree tab
        OpenWithDifftool = 19,
        EditFile = 22,
        OpenAsTempFile = 23,
        OpenAsTempFileWith = 24,
        OpenWithDifftoolFirstToLocal = 33,
        OpenWithDifftoolSelectedToLocal = 34,

        // Revision grid
        OpenCommitsWithDifftool = 35,
        ToggleBetweenArtificialAndHeadCommits = 36,
        GoToChild = 37,
        GoToParent = 38,

        /* deprecated: RotateApplicationIcon = 14, */
    }

    /// <summary>The hotkey settings of <c>FormCommit</c>.</summary>
    public const string CommitSettingsName = "Commit";

    /// <summary>The commands of <c>FormCommit.Command</c>.</summary>
    public enum Commit
    {
        /* obsolete: AddToGitIgnore = 0, */
        /* obsolete: DeleteSelectedFiles = 1, */
        FocusUnstagedFiles = 2,
        FocusSelectedDiff = 3,
        FocusStagedFiles = 4,
        FocusCommitMessage = 5,
        /* obsolete: ResetSelectedFiles = 6, */
        /* obsolete: StageSelectedFile = 7, */
        /* obsolete: UnStageSelectedFile = 8, */
        /* obsolete: ShowHistory = 9, */
        ToggleSelectionFilter = 10,
        StageAll = 11,
        OpenWithDifftool = 12,
        /* obsolete: OpenFile = 13, */
        /* obsolete: OpenFileWith = 14, */
        /* obsolete: EditFile = 15, */
        AddSelectionToCommitMessage = 16,
        CreateBranch = 17,
        Refresh = 18,
        SelectNext = 19, // Ctrl+N
        SelectNext_AlternativeHotkey1 = 20, // Alt+Down
        SelectNext_AlternativeHotkey2 = 21, // Alt+Right
        SelectPrevious = 22, // Ctrl+P
        SelectPrevious_AlternativeHotkey1 = 23, // Alt+Up
        SelectPrevious_AlternativeHotkey2 = 24, // Alt+Left
        ConventionalCommit_PrefixMessage = 25, // Ctrl+T
        ConventionalCommit_PrefixMessageWithScope = 26, // Ctrl+Shift+T
    }

    /// <summary>The hotkey settings of <c>RevisionGridControl</c>.</summary>
    public const string RevisionGridSettingsName = "RevisionGrid";

    /// <summary>The commands of <c>RevisionGridControl.Command</c>.</summary>
    public enum RevisionGrid
    {
        ToggleRevisionGraph = 0,
        RevisionFilter = 1,
        ToggleAuthorDateCommitDate = 2,
        ToggleOrderRevisionsByDate = 3,
        ToggleShowRelativeDate = 4,
        ToggleDrawNonRelativesGray = 5,
        ToggleShowGitNotes = 6,
        ToggleShowGitNotesColumn = 47,
        //// <snip>
        ToggleHideMergeCommits = 8,
        ShowAllBranches = 9,
        ShowCurrentBranchOnly = 10,
        ShowFilteredBranches = 11,
        ShowRemoteBranches = 12,
        ShowFirstParent = 13,
        GoToParent = 14,
        GoToFirstParent = 45,
        GoToLastParent = 46,
        GoToChild = 15,
        ToggleHighlightSelectedBranch = 16,
        NextQuickSearch = 17,
        PrevQuickSearch = 18,
        SelectCurrentRevision = 19,
        GoToCommit = 20,
        NavigateBackward = 21,
        NavigateForward = 22,
        SelectAsBaseToCompare = 23,
        CompareToBase = 24,
        CreateFixupCommit = 25,
        ToggleShowTags = 26,
        CompareToWorkingDirectory = 27,
        CompareToCurrentBranch = 28,
        CompareToBranch = 29,
        CompareSelectedCommits = 30,
        GoToMergeBase = 31,
        OpenCommitsWithDifftool = 32,
        ToggleBetweenArtificialAndHeadCommits = 33,
        ShowReflogReferences = 34,
        ShowStashes = 35,
        ResetRevisionFilter = 36,
        ResetRevisionPathFilter = 37,
        SelectNextForkPointAsDiffBase = 38,
        NavigateBackward_AlternativeHotkey = 39,
        NavigateForward_AlternativeHotkey = 40,
        DeleteRef = 41,
        RenameRef = 42,
        CreateSquashCommit = 43,
        CreateAmendCommit = 44,
    }

    /// <summary>The hotkey settings of <c>RevisionDiffControl</c>.</summary>
    public const string RevisionDiffSettingsName = "BrowseDiff";

    /// <summary>The commands of <c>RevisionDiffControl.Command</c>.</summary>
    public enum RevisionDiff
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

    /// <summary>The hotkey settings of <c>FileViewer</c>.</summary>
    public const string FileViewerSettingsName = "FileViewer";

    /// <summary>The commands of <c>FileViewer.Command</c>.</summary>
    public enum FileViewer
    {
        Find = 0,
        Replace = 16,
        FindNextOrOpenWithDifftool = 8,
        FindPrevious = 9,
        GoToLine = 1,
        IncreaseNumberOfVisibleLines = 2,
        DecreaseNumberOfVisibleLines = 3,
        ShowEntireFile = 4,
        ShowSyntaxHighlighting = 17,
        ShowGitWordColoring = 18,
        ShowDifftastic = 19,
        TreatFileAsText = 5,
        NextChange = 6,
        PreviousChange = 7,
        NextOccurrence = 10,
        PreviousOccurrence = 11,
        StageLines = 12,
        UnstageLines = 13,
        ResetLines = 14,
        IgnoreAllWhitespace = 15,
    }

    /// <summary>The hotkey settings of <c>RepoObjectsTree</c>.</summary>
    public const string LeftPanelSettingsName = "LeftPanel";

    /// <summary>The commands of <c>RepoObjectsTree.Command</c>.</summary>
    public enum LeftPanel
    {
        Delete = 0,
        Rename = 1,
        Search = 2,
        MultiSelect = 3,
        MultiSelectWithChildren = 4,
    }

    /// <summary>The hotkey settings of <c>FormResolveConflicts</c>.</summary>
    public const string ResolveConflictsSettingsName = "FormMergeConflicts";

    /// <summary>The commands of <c>FormResolveConflicts.Commands</c>.</summary>
    public enum ResolveConflicts
    {
        Merge = 0,
        Rescan = 1,
        ChooseRemote = 2,
        ChooseLocal = 3,
        ChooseBase = 4
    }

    /// <summary>The hotkey settings of <c>FormStash</c>.</summary>
    public const string StashSettingsName = "Stash";

    /// <summary>The commands of <c>FormStash.Command</c>.</summary>
    public enum Stash
    {
        NextStash = 0,
        PreviousStash = 1,
        Refresh = 2
    }

    /// <summary>The hotkey settings of the scripts (<c>FormSettings.HotkeySettingsName</c>).</summary>
    public const string ScriptsSettingsName = "Scripts";
}
