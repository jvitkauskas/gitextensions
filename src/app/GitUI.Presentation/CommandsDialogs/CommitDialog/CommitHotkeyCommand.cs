namespace GitUI.Presentation.CommandsDialogs.CommitDialog;

/// <summary>
///  The hotkey commands of the commit dialog, with the codes of <c>FormCommit.Command</c> (the hotkeys are loaded from
///  the "Commit" hotkey settings).
/// </summary>
public enum CommitHotkeyCommand
{
    FocusUnstagedFiles = 2,
    FocusSelectedDiff = 3,
    FocusStagedFiles = 4,
    FocusCommitMessage = 5,
    ToggleSelectionFilter = 10,
    StageAll = 11,
    OpenWithDifftool = 12,
    AddSelectionToCommitMessage = 16,
    CreateBranch = 17,
    Refresh = 18,
    SelectNext = 19,
    SelectNext_AlternativeHotkey1 = 20,
    SelectNext_AlternativeHotkey2 = 21,
    SelectPrevious = 22,
    SelectPrevious_AlternativeHotkey1 = 23,
    SelectPrevious_AlternativeHotkey2 = 24,
    ConventionalCommit_PrefixMessage = 25,
    ConventionalCommit_PrefixMessageWithScope = 26,
}
