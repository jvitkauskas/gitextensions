using System.Xml;
using System.Xml.Serialization;
using GitCommands;
using GitUI.CommandsDialogs;
using GitUI.Editor;
using GitUI.ScriptsEngine;
using Microsoft;
using ResourceManager;
using ResourceManager.Hotkey;

namespace GitUI.Hotkey;

/// <summary>
///  Provides the ability to manage hotkeys settings.
/// </summary>
public interface IHotkeySettingsManager : IHotkeySettingsLoader
{
    /// <summary>
    ///  Generates the list of preset hotkeys.
    /// </summary>
    /// <returns>The list of preset hotkeys.</returns>
    IReadOnlyList<HotkeySettings> CreateDefaultSettings();

    /// <summary>
    ///  Determines whether the hotkey is already assigned.
    /// </summary>
    /// <returns><see langword="true"/> if the hotkey is available; otherwise, <see langword="false"/>.</returns>
    bool IsUniqueKey(Keys keyData);

    /// <summary>
    ///  Loads all preset and user-configured hotkeys.
    /// </summary>
    /// <returns>The union of preset and user-configured hotkeys.</returns>
    IReadOnlyList<HotkeySettings> LoadSettings();

    /// <summary>
    ///  Saves the user-configured hotkeys.
    /// </summary>
    /// <param name="settings">The user-configured hotkeys.</param>
    void SaveSettings(IEnumerable<HotkeySettings> settings);
}

internal sealed class HotkeySettingsManager : IHotkeySettingsManager
{
    private static readonly XmlSerializer? _serializer = new(typeof(HotkeySettings[]), [typeof(HotkeyCommand)]);
    private readonly HashSet<Keys> _usedKeys = [];
    private readonly IScriptsManager _scriptsManager;

    public HotkeySettingsManager(IScriptsManager scriptsManager)
    {
        _scriptsManager = scriptsManager;
    }

    public bool IsUniqueKey(Keys keyData) => _usedKeys.Contains(keyData);

    public IReadOnlyList<HotkeySettings> LoadSettings()
    {
        // Get the default settings
        IReadOnlyList<HotkeySettings> defaultSettings = CreateDefaultSettings();
        HotkeySettings[]? loadedSettings = LoadSerializedSettings();

        MergeIntoDefaultSettings(defaultSettings, loadedSettings);

        return defaultSettings;
    }

    private void UpdateUsedKeys(IEnumerable<HotkeySettings> settings)
    {
        _usedKeys.Clear();

        foreach (HotkeySettings setting in settings)
        {
            if (setting.Commands is not null)
            {
                foreach (HotkeyCommand command in setting.Commands)
                {
                    _usedKeys.Add(command.KeyData);
                }
            }
        }
    }

    /// <summary>Serializes and saves the supplied settings.</summary>
    public void SaveSettings(IEnumerable<HotkeySettings> settings)
    {
        try
        {
            UpdateUsedKeys(settings);

            XmlWriterSettings xmlWriterSettings = new()
            {
                Indent = true
            };
            using StringWriter sw = new();
            using XmlWriter xmlWriter = XmlWriter.Create(sw, xmlWriterSettings);

            _serializer!.Serialize(xmlWriter, settings.ToArray());
            AppSettings.SerializedHotkeys = sw.ToString();
        }
        catch
        {
            // ignore
        }
    }

    internal static void MergeIntoDefaultSettings(IEnumerable<HotkeySettings> defaultSettings, IEnumerable<HotkeySettings>? loadedSettings)
    {
        if (loadedSettings is null)
        {
            return;
        }

        Dictionary<string, HotkeyCommand> defaultCommands = [];

        FillDictionaryWithCommands();
        AssignHotkeysFromLoaded();

        void AssignHotkeysFromLoaded()
        {
            foreach (HotkeySettings setting in loadedSettings)
            {
                if (setting.Commands is not null && setting.Name is not null)
                {
                    foreach (HotkeyCommand command in setting.Commands)
                    {
                        string dictKey = CalcDictionaryKey(setting.Name, command.CommandCode);
                        if (defaultCommands.TryGetValue(dictKey, out HotkeyCommand? defaultCommand))
                        {
                            defaultCommand.KeyData = command.KeyData;
                        }
                    }
                }
            }
        }

        void FillDictionaryWithCommands()
        {
            foreach (HotkeySettings setting in defaultSettings)
            {
                if (setting.Commands is not null && setting.Name is not null)
                {
                    foreach (HotkeyCommand command in setting.Commands)
                    {
                        string dictKey = CalcDictionaryKey(setting.Name, command.CommandCode);
                        defaultCommands.Add(dictKey, command);
                    }
                }
            }
        }

        string CalcDictionaryKey(string settingName, int commandCode) => settingName + ":" + commandCode;
    }

    private static HotkeySettings[]? LoadSerializedSettings()
    {
        if (!string.IsNullOrWhiteSpace(AppSettings.SerializedHotkeys))
        {
            return LoadSerializedSettings(AppSettings.SerializedHotkeys);
        }

        return null;
    }

    private static HotkeySettings[]? LoadSerializedSettings(string serializedHotkeys)
    {
        try
        {
            using StringReader reader = new(serializedHotkeys);
            return (HotkeySettings[]?)_serializer!.Deserialize(reader);
        }
        catch
        {
            return null;
        }
    }

    public IReadOnlyList<HotkeySettings> CreateDefaultSettings()
    {
        HotkeyCommand Hk(object en, Keys k) => new((int)en, en.ToString()!) { KeyData = k };

        const Keys OpenWithDifftoolHotkey = Keys.F3;

        // Control is Cmd on macOS, where Cmd+H hides the application: Cmd+Option+F there, as in Xcode.
        Keys replaceHotkey = OperatingSystem.IsMacOS() ? Keys.Control | Keys.Alt | Keys.F : Keys.Control | Keys.H;
        const Keys OpenWithDifftoolFirstToLocalHotkey = Keys.Alt | Keys.F3;
        const Keys OpenWithDifftoolSelectedToLocalHotkey = Keys.Shift | Keys.Alt | Keys.F3;
        const Keys OpenAsTempFileHotkey = Keys.Control | Keys.F3;
        const Keys OpenAsTempFileWithHotkey = Keys.Shift | Keys.Control | Keys.F3;
        const Keys EditFileHotkey = Keys.F4;
        const Keys OpenFileHotkey = Keys.Shift | Keys.F4;
        const Keys OpenFileWithHotkey = Keys.Shift | Keys.Control | Keys.F4;
        const Keys ShowHistoryHotkey = Keys.H;
        const Keys BlameHotkey = Keys.B;

        return new[]
        {
            new HotkeySettings(
                HotkeyCommands.CommitSettingsName,
                Hk(HotkeyCommands.Commit.AddSelectionToCommitMessage, Keys.C),
                Hk(HotkeyCommands.Commit.ConventionalCommit_PrefixMessage, Keys.Control | Keys.T),
                Hk(HotkeyCommands.Commit.ConventionalCommit_PrefixMessageWithScope, Keys.Control | Keys.Shift | Keys.T),
                Hk(HotkeyCommands.Commit.CreateBranch, Keys.Control | Keys.B),
                Hk(HotkeyCommands.Commit.FocusUnstagedFiles, Keys.Control | Keys.D1),
                Hk(HotkeyCommands.Commit.FocusSelectedDiff, Keys.Control | Keys.D2),
                Hk(HotkeyCommands.Commit.FocusStagedFiles, Keys.Control | Keys.D3),
                Hk(HotkeyCommands.Commit.FocusCommitMessage, Keys.Control | Keys.D4),
                Hk(HotkeyCommands.Commit.OpenWithDifftool, OpenWithDifftoolHotkey),
                Hk(HotkeyCommands.Commit.Refresh, Keys.F5),
                Hk(HotkeyCommands.Commit.SelectNext, Keys.Control | Keys.N),
                Hk(HotkeyCommands.Commit.SelectNext_AlternativeHotkey1, Keys.Alt | Keys.Down),
                Hk(HotkeyCommands.Commit.SelectNext_AlternativeHotkey2, Keys.Alt | Keys.Right),
                Hk(HotkeyCommands.Commit.SelectPrevious, Keys.Control | Keys.P),
                Hk(HotkeyCommands.Commit.SelectPrevious_AlternativeHotkey1, Keys.Alt | Keys.Up),
                Hk(HotkeyCommands.Commit.SelectPrevious_AlternativeHotkey2, Keys.Alt | Keys.Left),
                Hk(HotkeyCommands.Commit.StageAll, Keys.Control | Keys.S),
                Hk(HotkeyCommands.Commit.ToggleSelectionFilter, Keys.Control | Keys.F)),
            new HotkeySettings(
                HotkeyCommands.BrowseSettingsName,
                Hk(HotkeyCommands.Browse.AddNotes, Keys.Control | Keys.Shift | Keys.N),
                Hk(HotkeyCommands.Browse.CheckoutBranch, Keys.Control | Keys.OemPeriod),
                Hk(HotkeyCommands.Browse.CloseRepository, Keys.Control | Keys.W),
                Hk(HotkeyCommands.Browse.Commit, Keys.Control | Keys.Space),
                Hk(HotkeyCommands.Browse.CreateBranch, Keys.Control | Keys.B),
                Hk(HotkeyCommands.Browse.CreateTag, Keys.Control | Keys.T),
                Hk(HotkeyCommands.Browse.EditFile, EditFileHotkey),
                Hk(HotkeyCommands.Browse.FindFileInSelectedCommit, Keys.Control | Keys.Shift | Keys.F),
                Hk(HotkeyCommands.Browse.FocusLeftPanel, Keys.Control | Keys.D0),
                Hk(HotkeyCommands.Browse.FocusRevisionGrid, Keys.Control | Keys.D1),
                Hk(HotkeyCommands.Browse.FocusCommitInfo, Keys.Control | Keys.D2),
                Hk(HotkeyCommands.Browse.FocusDiff, Keys.Control | Keys.D3),
                Hk(HotkeyCommands.Browse.FocusFileTree, Keys.Control | Keys.D4),
                Hk(HotkeyCommands.Browse.FocusGpgInfo, Keys.Control | Keys.D5),
                Hk(HotkeyCommands.Browse.FocusGitConsole, Keys.Control | Keys.D6),
                Hk(HotkeyCommands.Browse.FocusBuildServerStatus, Keys.Control | Keys.D7),
                Hk(HotkeyCommands.Browse.FocusOutputHistoryAndToggleIfPanel, Keys.Control | Keys.D9),
                Hk(HotkeyCommands.Browse.FocusNextTab, Keys.Control | Keys.Tab),
                Hk(HotkeyCommands.Browse.FocusPrevTab, Keys.Control | Keys.Shift | Keys.Tab),
                Hk(HotkeyCommands.Browse.FocusFilter, Keys.Control | Keys.E),
                Hk(HotkeyCommands.Browse.GitBash, Keys.Control | Keys.G),
                Hk(HotkeyCommands.Browse.GitGui, Keys.None),
                Hk(HotkeyCommands.Browse.GitGitK, Keys.None),
                Hk(HotkeyCommands.Browse.GoToChild, Keys.Control | Keys.N),
                Hk(HotkeyCommands.Browse.GoToParent, Keys.Control | Keys.P),
                Hk(HotkeyCommands.Browse.GoToSubmodule, Keys.None),
                Hk(HotkeyCommands.Browse.GoToSuperproject, Keys.None),
                Hk(HotkeyCommands.Browse.ManageWorkTrees, Keys.Control | Keys.Alt | Keys.W),
                Hk(HotkeyCommands.Browse.MergeBranches, Keys.Control | Keys.M),
                Hk(HotkeyCommands.Browse.OpenAsTempFile, OpenAsTempFileHotkey),
                Hk(HotkeyCommands.Browse.OpenAsTempFileWith, OpenAsTempFileWithHotkey),
                Hk(HotkeyCommands.Browse.OpenCommitsWithDifftool, Keys.None),
                Hk(HotkeyCommands.Browse.OpenRepo, Keys.Control | Keys.O),
                Hk(HotkeyCommands.Browse.OpenSettings, Keys.Control | Keys.Oemcomma),
                Hk(HotkeyCommands.Browse.OpenWithDifftool, OpenWithDifftoolHotkey),
                Hk(HotkeyCommands.Browse.OpenWithDifftoolFirstToLocal, OpenWithDifftoolFirstToLocalHotkey),
                Hk(HotkeyCommands.Browse.OpenWithDifftoolSelectedToLocal, OpenWithDifftoolSelectedToLocalHotkey),
                Hk(HotkeyCommands.Browse.PullOrFetch, Keys.Control | Keys.Down),
                Hk(HotkeyCommands.Browse.Push, Keys.Control | Keys.Up),
                Hk(HotkeyCommands.Browse.QuickFetch, Keys.Control | Keys.Shift | Keys.Down),
                Hk(HotkeyCommands.Browse.QuickPull, Keys.Control | Keys.Shift | Keys.P),
                Hk(HotkeyCommands.Browse.QuickPullOrFetch, Keys.F8),
                Hk(HotkeyCommands.Browse.QuickPush, Keys.Control | Keys.Shift | Keys.Up),
                Hk(HotkeyCommands.Browse.Rebase, Keys.Control | Keys.Shift | Keys.E),
                Hk(HotkeyCommands.Browse.Stash, Keys.Control | Keys.Alt | Keys.Up),
                Hk(HotkeyCommands.Browse.StashPop, Keys.Control | Keys.Alt | Keys.Down),
                Hk(HotkeyCommands.Browse.StashStaged, Keys.Control | Keys.Shift | Keys.Alt | Keys.Up),
                Hk(HotkeyCommands.Browse.ToggleBetweenArtificialAndHeadCommits, Keys.Control | Keys.OemBackslash),
                Hk(HotkeyCommands.Browse.ToggleLeftPanel, Keys.Control | Keys.Alt | Keys.C)),
            new HotkeySettings(
                HotkeyCommands.LeftPanelSettingsName,
                Hk(HotkeyCommands.LeftPanel.Delete, Keys.Delete),
                Hk(HotkeyCommands.LeftPanel.MultiSelect, Keys.Control | Keys.Space),
                Hk(HotkeyCommands.LeftPanel.MultiSelectWithChildren, Keys.Control | Keys.Shift | Keys.Space),
                Hk(HotkeyCommands.LeftPanel.Rename, Keys.F2),
                Hk(HotkeyCommands.LeftPanel.Search, Keys.F3)),
            new HotkeySettings(
                HotkeyCommands.RevisionGridSettingsName,
                Hk(HotkeyCommands.RevisionGrid.CompareSelectedCommits, Keys.None),
                Hk(HotkeyCommands.RevisionGrid.CompareToBase, Keys.Control | Keys.R),
                Hk(HotkeyCommands.RevisionGrid.CompareToBranch, Keys.None),
                Hk(HotkeyCommands.RevisionGrid.CompareToCurrentBranch, Keys.None),
                Hk(HotkeyCommands.RevisionGrid.CompareToWorkingDirectory, Keys.Control | Keys.D),
                Hk(HotkeyCommands.RevisionGrid.CreateAmendCommit, Keys.None),
                Hk(HotkeyCommands.RevisionGrid.CreateFixupCommit, Keys.Control | Keys.X),
                Hk(HotkeyCommands.RevisionGrid.CreateSquashCommit, Keys.Control | Keys.Shift | Keys.X),
                Hk(HotkeyCommands.RevisionGrid.DeleteRef, Keys.Delete),
                Hk(HotkeyCommands.RevisionGrid.GoToChild, Keys.Control | Keys.N),
                Hk(HotkeyCommands.RevisionGrid.GoToCommit, Keys.Control | Keys.Shift | Keys.G),
                Hk(HotkeyCommands.RevisionGrid.GoToFirstParent, Keys.Control | Keys.Left),
                Hk(HotkeyCommands.RevisionGrid.GoToLastParent, Keys.Control | Keys.Right),
                Hk(HotkeyCommands.RevisionGrid.GoToMergeBase, Keys.Control | Keys.Shift | Keys.K),
                Hk(HotkeyCommands.RevisionGrid.GoToParent, Keys.Control | Keys.P),
                Hk(HotkeyCommands.RevisionGrid.NavigateBackward, Keys.Alt | Keys.Left),
                Hk(HotkeyCommands.RevisionGrid.NavigateBackward_AlternativeHotkey, Keys.BrowserBack),
                Hk(HotkeyCommands.RevisionGrid.NavigateForward, Keys.Alt | Keys.Right),
                Hk(HotkeyCommands.RevisionGrid.NavigateForward_AlternativeHotkey, Keys.BrowserForward),
                Hk(HotkeyCommands.RevisionGrid.NextQuickSearch, Keys.Alt | Keys.Down),
                Hk(HotkeyCommands.RevisionGrid.OpenCommitsWithDifftool, Keys.None),
                Hk(HotkeyCommands.RevisionGrid.PrevQuickSearch, Keys.Alt | Keys.Up),
                Hk(HotkeyCommands.RevisionGrid.RenameRef, Keys.F2),
                Hk(HotkeyCommands.RevisionGrid.ResetRevisionPathFilter, Keys.Control | Keys.Shift | Keys.H),
                Hk(HotkeyCommands.RevisionGrid.ResetRevisionFilter, Keys.Control | Keys.Shift | Keys.I),
                Hk(HotkeyCommands.RevisionGrid.RevisionFilter, Keys.Control | Keys.I),
                Hk(HotkeyCommands.RevisionGrid.SelectAsBaseToCompare, Keys.Control | Keys.L),
                Hk(HotkeyCommands.RevisionGrid.SelectCurrentRevision, Keys.Control | Keys.Shift | Keys.C),
                Hk(HotkeyCommands.RevisionGrid.SelectNextForkPointAsDiffBase, Keys.Control | Keys.K),
                Hk(HotkeyCommands.RevisionGrid.ShowAllBranches, Keys.Control | Keys.Shift | Keys.A),
                Hk(HotkeyCommands.RevisionGrid.ShowCurrentBranchOnly, Keys.Control | Keys.Shift | Keys.U),
                Hk(HotkeyCommands.RevisionGrid.ShowFilteredBranches, Keys.Control | Keys.Shift | Keys.T),
                Hk(HotkeyCommands.RevisionGrid.ShowFirstParent, Keys.Control | Keys.Shift | Keys.S),
                Hk(HotkeyCommands.RevisionGrid.ShowReflogReferences, Keys.Control | Keys.Shift | Keys.L),
                Hk(HotkeyCommands.RevisionGrid.ShowRemoteBranches, Keys.Control | Keys.Shift | Keys.R),
                Hk(HotkeyCommands.RevisionGrid.ToggleAuthorDateCommitDate, Keys.None),
                Hk(HotkeyCommands.RevisionGrid.ToggleBetweenArtificialAndHeadCommits, Keys.Control | Keys.OemBackslash),
                Hk(HotkeyCommands.RevisionGrid.ToggleDrawNonRelativesGray, Keys.None),
                Hk(HotkeyCommands.RevisionGrid.ToggleHighlightSelectedBranch, Keys.Control | Keys.Shift | Keys.B),
                Hk(HotkeyCommands.RevisionGrid.ToggleOrderRevisionsByDate, Keys.None),
                Hk(HotkeyCommands.RevisionGrid.ToggleRevisionGraph, Keys.None),
                Hk(HotkeyCommands.RevisionGrid.ToggleShowGitNotes, Keys.None),
                Hk(HotkeyCommands.RevisionGrid.ToggleShowGitNotesColumn, Keys.None),
                Hk(HotkeyCommands.RevisionGrid.ToggleHideMergeCommits, Keys.Control | Keys.Shift | Keys.M),
                Hk(HotkeyCommands.RevisionGrid.ToggleShowRelativeDate, Keys.None),
                Hk(HotkeyCommands.RevisionGrid.ToggleShowTags, Keys.Control | Keys.Alt | Keys.T)),
            new HotkeySettings(
                HotkeyCommands.FileViewerSettingsName,
                Hk(HotkeyCommands.FileViewer.Find, Keys.Control | Keys.F),
                Hk(HotkeyCommands.FileViewer.Replace, replaceHotkey),
                Hk(HotkeyCommands.FileViewer.FindNextOrOpenWithDifftool, OpenWithDifftoolHotkey),
                Hk(HotkeyCommands.FileViewer.FindPrevious, Keys.Shift | OpenWithDifftoolHotkey),
                Hk(HotkeyCommands.FileViewer.GoToLine, Keys.Control | Keys.G),
                Hk(HotkeyCommands.FileViewer.IncreaseNumberOfVisibleLines, Keys.Control | Keys.Oemplus),
                Hk(HotkeyCommands.FileViewer.DecreaseNumberOfVisibleLines, Keys.Control | Keys.OemMinus),
                Hk(HotkeyCommands.FileViewer.NextChange, Keys.Alt | Keys.Down),
                Hk(HotkeyCommands.FileViewer.PreviousChange, Keys.Alt | Keys.Up),
                Hk(HotkeyCommands.FileViewer.ShowEntireFile, Keys.Control | Keys.E),
                Hk(HotkeyCommands.FileViewer.ShowSyntaxHighlighting, Keys.X),
                Hk(HotkeyCommands.FileViewer.ShowGitWordColoring, Keys.Control | Keys.D),
                Hk(HotkeyCommands.FileViewer.ShowDifftastic, Keys.Control | Keys.T),
                Hk(HotkeyCommands.FileViewer.TreatFileAsText, Keys.None),
                Hk(HotkeyCommands.FileViewer.NextOccurrence, Keys.Alt | Keys.Right),
                Hk(HotkeyCommands.FileViewer.PreviousOccurrence, Keys.Alt | Keys.Left),
                Hk(HotkeyCommands.FileViewer.StageLines, Keys.S),
                Hk(HotkeyCommands.FileViewer.UnstageLines, Keys.U),
                Hk(HotkeyCommands.FileViewer.ResetLines, Keys.R),
                Hk(HotkeyCommands.FileViewer.IgnoreAllWhitespace, Keys.Control | Keys.Shift | Keys.W)),
            new HotkeySettings(
                HotkeyCommands.ResolveConflictsSettingsName,
                Hk(HotkeyCommands.ResolveConflicts.ChooseBase, Keys.B),
                Hk(HotkeyCommands.ResolveConflicts.ChooseLocal, Keys.L),
                Hk(HotkeyCommands.ResolveConflicts.ChooseRemote, Keys.R),
                Hk(HotkeyCommands.ResolveConflicts.Merge, Keys.M),
                Hk(HotkeyCommands.ResolveConflicts.Rescan, Keys.F5)),
            new HotkeySettings(
                HotkeyCommands.RevisionDiffSettingsName,
                Hk(HotkeyCommands.RevisionDiff.AddFileToGitIgnore, Keys.None),
                Hk(HotkeyCommands.RevisionDiff.Blame, BlameHotkey),
                Hk(HotkeyCommands.RevisionDiff.DeleteSelectedFiles, Keys.Delete),
                Hk(HotkeyCommands.RevisionDiff.EditFile, EditFileHotkey),
                Hk(HotkeyCommands.RevisionDiff.FilterFileInGrid, Keys.F),
                Hk(HotkeyCommands.RevisionDiff.FindFile, Keys.None),
                Hk(HotkeyCommands.RevisionDiff.FindInCommitFilesUsingGitGrep_DiffTab, Keys.Control | Keys.Shift | Keys.F),
                Hk(HotkeyCommands.RevisionDiff.FindInCommitFilesUsingGitGrep_FileTreeTab, Keys.None),
                Hk(HotkeyCommands.RevisionDiff.GoToFirstParent, Keys.Control | Keys.Left),
                Hk(HotkeyCommands.RevisionDiff.GoToLastParent, Keys.Control | Keys.Right),
                Hk(HotkeyCommands.RevisionDiff.OpenAsTempFile, OpenAsTempFileHotkey),
                Hk(HotkeyCommands.RevisionDiff.OpenAsTempFileWith, OpenAsTempFileWithHotkey),
                Hk(HotkeyCommands.RevisionDiff.OpenInVisualStudio, Keys.Control | Keys.Shift | Keys.S),
                Hk(HotkeyCommands.RevisionDiff.OpenWithDifftool, OpenWithDifftoolHotkey),
                Hk(HotkeyCommands.RevisionDiff.OpenWithDifftoolFirstToLocal, OpenWithDifftoolFirstToLocalHotkey),
                Hk(HotkeyCommands.RevisionDiff.OpenWithDifftoolSelectedToLocal, OpenWithDifftoolSelectedToLocalHotkey),
                Hk(HotkeyCommands.RevisionDiff.OpenWorkingDirectoryFile, OpenFileHotkey),
                Hk(HotkeyCommands.RevisionDiff.OpenWorkingDirectoryFileWith, OpenFileWithHotkey),
                Hk(HotkeyCommands.RevisionDiff.RenameMove, Keys.F2),
                Hk(HotkeyCommands.RevisionDiff.ResetSelectedFiles, Keys.R),
                Hk(HotkeyCommands.RevisionDiff.SelectFirstGroupChanges, Keys.Control | Keys.A),
                Hk(HotkeyCommands.RevisionDiff.ShowFileTree, Keys.T),
                Hk(HotkeyCommands.RevisionDiff.ShowHistory, ShowHistoryHotkey),
                Hk(HotkeyCommands.RevisionDiff.StageSelectedFile, Keys.S),
                Hk(HotkeyCommands.RevisionDiff.UnStageSelectedFile, Keys.U)),
            new HotkeySettings(
                HotkeyCommands.StashSettingsName,
                Hk(HotkeyCommands.Stash.NextStash, Keys.Control | Keys.N),
                Hk(HotkeyCommands.Stash.PreviousStash, Keys.Control | Keys.P),
                Hk(HotkeyCommands.Stash.Refresh, Keys.F5)),
            new HotkeySettings(
                HotkeyCommands.ScriptsSettingsName,
                LoadScriptHotkeys())
        };

        HotkeyCommand[] LoadScriptHotkeys()
        {
            /* define unusable int for identifying a shortcut for a custom script is pressed
             * all integers above 9000 represent a script hotkey
             * these integers are never matched in the 'switch' routine on a form and
             * therefore execute the 'default' action
             */
            return [.. _scriptsManager
                .GetScripts()
                .Where(s => !string.IsNullOrEmpty(s.Name))
                .Select(s => new HotkeyCommand(s.HotkeyCommandIdentifier, s.GetDisplayName()) { KeyData = Keys.None })];
        }
    }

    IReadOnlyList<HotkeyCommand> IHotkeySettingsLoader.LoadHotkeys(string hotkeySettingsName)
    {
        HotkeySettings settings = new();
        HotkeySettings scriptKeys = new();
        IEnumerable<HotkeySettings> allSettings = LoadSettings();

        UpdateUsedKeys(allSettings);

        foreach (HotkeySettings setting in allSettings)
        {
            if (setting.Name == hotkeySettingsName)
            {
                settings = setting;
            }

            if (setting.Name == "Scripts")
            {
                scriptKeys = setting;
            }
        }

        // append general hotkeys to every form
        Validates.NotNull(settings.Commands);
        Validates.NotNull(scriptKeys.Commands);
        HotkeyCommand[] allKeys = new HotkeyCommand[settings.Commands.Length + scriptKeys.Commands.Length];
        settings.Commands.CopyTo(allKeys, 0);
        scriptKeys.Commands.CopyTo(allKeys, settings.Commands.Length);

        return allKeys;
    }
}
