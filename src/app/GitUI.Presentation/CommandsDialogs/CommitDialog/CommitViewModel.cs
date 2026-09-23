using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Editor;
using GitUI.Presentation.SpellChecker;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUIPluginInterfaces;

namespace GitUI.Presentation.CommandsDialogs.CommitDialog;

/// <summary>
///  View model of the commit dialog (port of <c>FormCommit</c>; docs/avalonia-port/PLAN.md, phase 5): the unstaged and
///  staged files with the diff of the selected one, staging, the commit message and the commit (and push).
/// </summary>
public sealed partial class CommitViewModel : DialogViewModel
{
    private static readonly string[] NewLines = ["\r\n", "\n"];

    private readonly ICommitHost _host;
    private readonly ICommitDialogSettings _settings;
    private readonly GitRevision? _editedCommit;
    private readonly CommitDialogKind _initialKind;
    private readonly bool _initializing = true;
    private FileStatusListViewModel _currentFilesList;
    private IReadOnlyList<string> _currentSelection = [];
    private string? _commitTemplate;
    private bool _isMergeCommit;
    private bool _skipUpdate;
    private bool _bypassActivatedEventHandler;
    private bool _initialized;
    private CancellationTokenSource? _loading;
    private FileStatusEntry? _shownEntry;

    public CommitViewModel(
        CommitStrings strings,
        ICommitHost host,
        IFileViewerHost fileViewerHost,
        FileStatusListStrings fileStatusListStrings,
        FileStatusTreeOptions fileStatusTreeOptions,
        CommitDialogKind kind = CommitDialogKind.Normal,
        GitRevision? editedCommit = null,
        ISpellCheckHost? spellCheckHost = null)
    {
        Strings = strings;
        SpellCheck = spellCheckHost is null ? null : new SpellCheckViewModel(ViewStrings.Load<SpellCheckStrings>(), spellCheckHost);
        _host = host;
        _settings = host.Settings;
        _editedCommit = editedCommit;
        _initialKind = kind;
        Options = host.Options;
        Unstaged = new FileStatusListViewModel(fileStatusListStrings, fileStatusTreeOptions) { NoFilesText = strings.NoUnstagedChanges.Text, SelectFirstItemOnSetItems = false };
        Staged = new FileStatusListViewModel(fileStatusListStrings, fileStatusTreeOptions) { NoFilesText = strings.NoStagedChanges.Text, SelectFirstItemOnSetItems = false };
        Diff = new FileViewerViewModel(fileViewerHost) { LinePatchingBlocksUntilReload = true };

        // As SelectedDiff_PatchApplied: the changes are loaded again, keeping the selected file.
        Diff.PatchApplied += (_, _) => RescanChanges();
        _currentFilesList = Unstaged;
        Title = strings.Title.PlainText;
        Kind = kind;
        HasSuperproject = host.HasSuperproject;
        StageInSuperproject = _settings.StageInSuperproject;
        CloseDialogAfterEachCommit = _settings.CloseDialogAfterEachCommit;
        CloseDialogAfterAllFilesCommitted = _settings.CloseDialogAfterAllFilesCommitted;
        RefreshDialogOnFormFocus = _settings.RefreshDialogOnFormFocus;
        SelectStagedOnEnterMessage = _settings.SelectStagedOnEnterMessage;
        ShowOnlyMyMessages = _settings.ShowOnlyMyMessages;
        IsSelectionFilterVisible = Options.ShowSelectionFilter;
        SelectionFilterToolTip = strings.SelectionFilterToolTip.Text;
        MessageWatermark = Options.UseFormCommitMessage ? strings.EnterCommitMessageHint.Text : strings.CommitMessageDisabled.Text;

        Unstaged.SelectionChanged += (_, _) => OnSelectionChanged(Unstaged, Staged);
        Staged.SelectionChanged += (_, _) => OnSelectionChanged(Staged, Unstaged);
        Unstaged.SelectionActivated += (_, _) => StageSelected(skipAssumeUnchanged: false);
        Staged.SelectionActivated += (_, _) => UnstageSelected();
        Unstaged.RefreshRequested += (_, _) => RescanChanges();
        Staged.RefreshRequested += (_, _) => RescanChanges();
        _initializing = false;
    }

    public CommitStrings Strings { get; }

    public CommitDialogOptions Options { get; }

    public FileStatusListViewModel Unstaged { get; }

    public FileStatusListViewModel Staged { get; }

    /// <summary>The diff of the selected file.</summary>
    public FileViewerViewModel Diff { get; }

    /// <summary>The spell checking and auto-completion of the message (<c>EditNetSpell</c>), if available.</summary>
    public SpellCheckViewModel? SpellCheck { get; }

    /// <summary>The commit message.</summary>
    public TextEditorViewModel Message { get; } = new() { ShowLineNumbers = false };

    public string MessageWatermark { get; }

    /// <summary>How the message is formatted as it is typed (<c>FormatAllText</c>).</summary>
    public CommitMessageFormatOptions FormatOptions => new(Options.MaxFirstLineLength, Options.MaxLineLength, Options.SecondLineMustBeEmpty, Options.AutoWrap, Options.IndentAfterFirstLine);

    [ObservableProperty]
    public partial string Title { get; private set; }

    /// <summary>As <c>CommitKind</c>: the message of a fixup or squash commit is not edited unless asked for.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMessageEditable), nameof(ShowModifyMessageButton))]
    public partial CommitDialogKind Kind { get; private set; }

    public bool IsMessageEditable => Options.UseFormCommitMessage && Kind is CommitDialogKind.Normal or CommitDialogKind.Amend;

    public bool ShowModifyMessageButton => Options.UseFormCommitMessage && !IsMessageEditable;

    [ObservableProperty]
    public partial bool IsLoading { get; private set; }

    /// <summary>Whether committing is possible (once the files are loaded).</summary>
    [ObservableProperty]
    public partial bool CanCommit { get; private set; }

    [ObservableProperty]
    public partial bool CanStage { get; private set; }

    [ObservableProperty]
    public partial bool HasMergeConflicts { get; private set; }

    [ObservableProperty]
    public partial string BranchName { get; private set; } = "";

    [ObservableProperty]
    public partial string PushTo { get; private set; } = "";

    [ObservableProperty]
    public partial string Committer { get; private set; } = "";

    /// <summary>The number of staged files of all files (<c>commitStagedCount</c>).</summary>
    [ObservableProperty]
    public partial string StagedCount { get; private set; } = "0/0";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPushForced), nameof(CommitAndPushText))]
    public partial bool Amend { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPushForced), nameof(CommitAndPushText))]
    public partial bool IsAmendEnabled { get; private set; } = true;

    [ObservableProperty]
    public partial bool ResetAuthor { get; set; }

    [ObservableProperty]
    public partial bool CanResetSoft { get; private set; }

    [ObservableProperty]
    public partial bool SignOff { get; set; }

    [ObservableProperty]
    public partial bool NoVerify { get; set; }

    /// <summary>The items of <c>gpgSignCommitToolStripComboBox</c> (not translated in <c>FormCommit</c> either).</summary>
    public static IReadOnlyList<string> GpgSignModes { get; } = ["Git default GPG signing", "Do not sign commit", "Sign with default GPG", "Sign with specific GPG"];

    /// <summary>The index in <see cref="GpgSignModes"/>.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGpgKeyVisible), nameof(IsGpgSignSelected))]
    public partial int GpgSignIndex { get; set; }

    /// <summary>The key of "Sign with specific GPG" (<c>toolStripGpgKeyTextBox</c>).</summary>
    [ObservableProperty]
    public partial string GpgKeyId { get; set; } = "";

    /// <summary>
    ///  The key is entered for "Sign with specific GPG" (<c>gpgSignCommitChanged</c> shows it for the default key, the item
    ///  before).
    /// </summary>
    public bool IsGpgKeyVisible => GpgSignIndex == 3;

    /// <summary>As <c>gpgSignCommitChanged</c>: the commit button has the key image.</summary>
    public bool IsGpgSignSelected => GpgSignIndex > 0;

    /// <summary>As <c>toolbarSelectionFilter.Visible</c>.</summary>
    [ObservableProperty]
    public partial bool IsSelectionFilterVisible { get; set; }

    /// <summary>The regular expression selecting the unstaged files (<c>selectionFilter</c>).</summary>
    [ObservableProperty]
    public partial string SelectionFilter { get; set; } = "";

    [ObservableProperty]
    public partial string SelectionFilterToolTip { get; private set; } = "";

    /// <summary>The last filters that selected files (the items of <c>selectionFilter</c>), the last one first.</summary>
    public System.Collections.ObjectModel.ObservableCollection<string> SelectionFilterHistory { get; } = [];

    /// <summary>The author, as <c>name &lt;mail&gt;</c>, if not the committer.</summary>
    [ObservableProperty]
    public partial string Author { get; set; } = "";

    public bool HasSuperproject { get; }

    [ObservableProperty]
    public partial bool StageInSuperproject { get; set; }

    [ObservableProperty]
    public partial bool CloseDialogAfterEachCommit { get; set; }

    [ObservableProperty]
    public partial bool CloseDialogAfterAllFilesCommitted { get; set; }

    [ObservableProperty]
    public partial bool RefreshDialogOnFormFocus { get; set; }

    [ObservableProperty]
    public partial bool SelectStagedOnEnterMessage { get; set; }

    [ObservableProperty]
    public partial bool ShowOnlyMyMessages { get; set; }

    /// <summary>Whether a changed file exists, which the reset of all changes needs (<c>btnResetAllChanges.Enabled</c>).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CommitAndPushText))]
    public partial bool HasChanges { get; private set; }

    [ObservableProperty]
    public partial bool HasUnstagedChanges { get; private set; }

    /// <summary>As <c>PushForced</c>: after amending or a soft reset, the push is forced if set so.</summary>
    public bool IsPushForced => (Amend || !IsAmendEnabled) && Options.CommitAndPushForcedWhenAmend;

    /// <summary>As <c>UpdateButtonStates</c>: without changes, the button pushes only.</summary>
    public string CommitAndPushText
        => IsPushForced ? Strings.CommitAndForcePush.AccessKeyText
            : HasChanges || Amend ? Strings.CommitAndPush.AccessKeyText
            : _host.PushText;

    /// <summary>The line and column of the caret in the message, which the view reports.</summary>
    [ObservableProperty]
    public partial (int Line, int Column) MessageCaret { get; set; } = (1, 1);

    /// <summary>As <c>OnShown</c>: loads the files and the message.</summary>
    public async Task InitializeAsync()
    {
        if (!_initialized)
        {
            Initialize();

            // As OnRuntimeLoad of EditNetSpell (ToggleAutoCompletion).
            SpellCheck?.LoadAutoCompleteWords();
        }

        _ = UpdateAuthorInfoAsync();

        string message;
        switch (Kind)
        {
            case CommitDialogKind.Fixup or CommitDialogKind.Squash when _editedCommit is not null:
                message = TryAddPrefix(_editedCommit.Subject);
                break;
            case CommitDialogKind.Amend when _editedCommit is not null:
                message = $"{TryAddPrefix(_editedCommit.Subject)}{Environment.NewLine}{Environment.NewLine}{_editedCommit.Body}";
                break;
            default:
                (string retrievedMessage, bool retrievedAmend) = await _host.LoadCommitMessageAsync();
                message = retrievedMessage;
                Amend = !_isMergeCommit && retrievedAmend;
                break;
        }

        if (Options.UseFormCommitMessage && !string.IsNullOrEmpty(message))
        {
            Message.Load(message);
        }
        else
        {
            string template = _host.LoadCommitTemplate() ?? "";
            Message.Load(template);
            _commitTemplate = template;
        }

        return;

        string TryAddPrefix(string suffix)
        {
            string prefix = GetPrefix(_initialKind);
            return suffix.StartsWith(prefix) ? suffix : $"{prefix} {suffix}";
        }
    }

    /// <summary>As <c>CommitKindExtensions.GetPrefix</c>.</summary>
    public static string GetPrefix(CommitDialogKind kind) => kind switch
    {
        CommitDialogKind.Fixup => "fixup!",
        CommitDialogKind.Squash => "squash!",
        CommitDialogKind.Amend => "amend!",
        _ => "",
    };

    /// <summary>As <c>OnFormClosed</c>: the message of a normal or amend commit is kept for the next time.</summary>
    public override bool CanClose()
    {
        if (Kind is CommitDialogKind.Normal or CommitDialogKind.Amend)
        {
            _ = _host.SaveCommitMessageAsync(Message.Text, Amend);
        }

        return true;
    }

    /// <summary>As <c>OnApplicationActivated</c>: the changes are rescanned if set so.</summary>
    public void OnActivated()
    {
        if (!_bypassActivatedEventHandler && RefreshDialogOnFormFocus)
        {
            RescanChanges();
        }
    }

    /// <summary>As <c>UICommands_PostRepositoryChanged</c>.</summary>
    public void OnRepositoryChanged()
    {
        if (!_skipUpdate && !_bypassActivatedEventHandler)
        {
            RescanChanges();
        }
    }

    /// <summary>As <c>Initialize</c>: loads the files and the branch.</summary>
    private void Initialize()
    {
        _initialized = true;
        _ = UpdateBranchInfoAsync();
        IsLoading = true;
        CanCommit = false;
        CanStage = false;
        Unstaged.SetLoading();
        Staged.SetLoading();
        _ = LoadFilesAsync();
        _isMergeCommit = _host.IsMergeCommit;
    }

    private async Task LoadFilesAsync()
    {
#pragma warning disable VSTHRD103 // CancelAsync may resume off the UI thread.
        _loading?.Cancel();
#pragma warning restore VSTHRD103
        CancellationTokenSource loading = new();
        _loading = loading;
        IReadOnlyList<GitItemStatus> files;
        try
        {
            files = await _host.GetAllChangedFilesAsync(loading.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (!loading.IsCancellationRequested)
        {
            LoadUnstagedOutput(files);
        }
    }

    private async Task UpdateBranchInfoAsync()
    {
        CommitBranchInfo info = await _host.GetBranchInfoAsync();
        BranchName = info.PushTo is null ? info.Branch : $"{info.Branch} {char.ConvertFromUtf32(0x2192)}";
        PushTo = info.PushTo ?? "";
        Title = string.Format(Strings.FormTitle.Text, info.Branch, _host.WorkingDirectory);
    }

    /// <summary>As <c>UpdateAuthorInfo</c> (also when the author changed).</summary>
    public void UpdateAuthorInfo() => _ = UpdateAuthorInfoAsync();

    private async Task UpdateAuthorInfoAsync()
    {
        Committer = await _host.GetCommitterAsync(Author);
    }

    /// <summary>As <c>LoadUnstagedOutput</c>: splits the changes into the unstaged and the staged files.</summary>
    private void LoadUnstagedOutput(IReadOnlyList<GitItemStatus> allChangedFiles)
    {
        IReadOnlyList<string> lastSelection = _currentSelection;
        List<GitItemStatus> unstagedFiles = [];
        List<GitItemStatus> stagedFiles = [];
        foreach (GitItemStatus fileStatus in allChangedFiles)
        {
            if (fileStatus.Staged == StagedStatus.WorkTree || fileStatus.IsStatusOnly)
            {
                // Present status only errors in unstaged
                unstagedFiles.Add(fileStatus);
            }
            else if (fileStatus.Staged == StagedStatus.Index)
            {
                stagedFiles.Add(fileStatus);
            }
        }

        SetFiles(unstagedFiles, stagedFiles);
        IsLoading = false;
        CanCommit = true;
        CanStage = true;
        HasMergeConflicts = _host.InTheMiddleOfConflictedMerge();

        if (!Staged.AllEntries.Any())
        {
            _currentFilesList = Unstaged;
        }
        else if (!Unstaged.AllEntries.Any())
        {
            _currentFilesList = Staged;
        }

        RestoreSelectedFiles(lastSelection);
    }

    private void SetFiles(IReadOnlyList<GitItemStatus> unstagedFiles, IReadOnlyList<GitItemStatus> stagedFiles)
    {
        (GitRevision? head, GitRevision index, GitRevision workTree) = _host.GetHeadRevisions();
        _skipUpdate = true;
        try
        {
            Unstaged.SetDiff(index, workTree, unstagedFiles);
            Staged.SetDiff(head, index, stagedFiles);
        }
        finally
        {
            _skipUpdate = false;
        }

        UpdateCounts(unstagedFiles.Count, stagedFiles.Count);
    }

    private void UpdateCounts(int unstagedCount, int stagedCount)
    {
        // As Staged_DataSourceChanged and UpdateButtonStates.
        StagedCount = $"{stagedCount}/{stagedCount + unstagedCount}";
        HasUnstagedChanges = unstagedCount > 0;
        HasChanges = unstagedCount > 0 || stagedCount > 0;
        CanResetSoft = Amend && _host.CanResetSoft();
    }

    /// <summary>As <c>RestoreSelectedFiles</c>: the files selected before, or the first ones.</summary>
    private void RestoreSelectedFiles(IReadOnlyList<string> lastSelection)
    {
        HashSet<string> names = [.. lastSelection];
        if (names.Count > 0 && _currentFilesList.AllEntries.Any(e => names.Contains(e.Item.Name)))
        {
            _currentFilesList.Select(e => names.Contains(e.Item.Name));
            ApplySelection(_currentFilesList);
            return;
        }

        FileStatusListViewModel list = Unstaged.AllEntries.Any() ? Unstaged : Staged;
        list.SelectFirstVisibleItem();
        ApplySelection(list);
    }

    /// <summary>
    ///  Shows the selection of the list, also when selecting did not change it (a single file is selected when set), which
    ///  raises no event.
    /// </summary>
    private void ApplySelection(FileStatusListViewModel list) => OnSelectionChanged(list, list == Unstaged ? Staged : Unstaged);

    /// <summary>As <c>UnstagedSelectionChanged</c> and <c>StagedSelectionChanged</c>: one list has a selection, shown in the diff.</summary>
    private void OnSelectionChanged(FileStatusListViewModel list, FileStatusListViewModel other)
    {
        if (_skipUpdate || list.SelectedEntries.Count == 0)
        {
            return;
        }

        _currentFilesList = list;
        _skipUpdate = true;
        try
        {
            other.Select(_ => false);
        }
        finally
        {
            _skipUpdate = false;
        }

        _currentSelection = [.. list.SelectedEntries.Select(e => e.Item.Name)];
        if (!ReferenceEquals(_shownEntry, list.SelectedEntries[0]))
        {
            _shownEntry = list.SelectedEntries[0];
            _ = Diff.ShowChangesAsync(_shownEntry);
        }
    }

    /// <summary>As <c>Message_Enter</c>: entering the message selects the staged files if set so.</summary>
    public void OnMessageEntered()
    {
        if (SelectStagedOnEnterMessage && Staged.AllEntries.Any() && Staged.SelectedEntries.Count == 0)
        {
            Staged.SelectFirstVisibleItem();
        }
    }

    /// <summary>
    ///  As <c>ExecuteCommand</c>: the hotkeys that need no view (the view handles the focus, the selection of the diff and
    ///  the menus).
    /// </summary>
    public override bool ExecuteHotkeyCommand(int commandCode)
    {
        switch ((CommitHotkeyCommand)commandCode)
        {
            case CommitHotkeyCommand.StageAll:
                if (!Unstaged.AllEntries.Any())
                {
                    return false;
                }

                StageAll();
                return true;
            case CommitHotkeyCommand.OpenWithDifftool:
                _currentFilesList.OpenWithDifftoolCommand.Execute(DifftoolKind.FirstToSelected);
                return true;
            case CommitHotkeyCommand.CreateBranch:
                CreateBranch();
                return true;
            case CommitHotkeyCommand.Refresh:
                RescanChanges();
                return true;
            case CommitHotkeyCommand.SelectNext:
            case CommitHotkeyCommand.SelectNext_AlternativeHotkey1:
            case CommitHotkeyCommand.SelectNext_AlternativeHotkey2:
                MoveSelection(backwards: false, messageFocused: false);
                return true;
            case CommitHotkeyCommand.SelectPrevious:
            case CommitHotkeyCommand.SelectPrevious_AlternativeHotkey1:
            case CommitHotkeyCommand.SelectPrevious_AlternativeHotkey2:
                MoveSelection(backwards: true, messageFocused: false);
                return true;
            default:
                return base.ExecuteHotkeyCommand(commandCode);
        }
    }

    /// <summary>
    ///  As <c>Update</c> of the selection filter (throttled by the view): the unstaged files matching the regular expression
    ///  are selected (all without one); a filter selecting files is remembered.
    /// </summary>
    public void ApplySelectionFilter()
    {
        const int selectionFilterMaxLength = 10;
        string filter = SelectionFilter;
        int matchCount = 0;
        try
        {
            Regex? regex = string.IsNullOrEmpty(filter) ? null : new Regex(filter, RegexOptions.IgnoreCase);
            Unstaged.Select(e => regex is null || regex.IsMatch(e.Item.Name));
            matchCount = Unstaged.SelectedEntries.Count;
            SelectionFilterToolTip = Strings.SelectionFilterToolTip.Text;
        }
        catch (ArgumentException ex)
        {
            SelectionFilterToolTip = string.Format(Strings.SelectionFilterErrorToolTip.Text, ex.Message);
        }

        if (matchCount == 0 || SelectionFilterHistory.Contains(filter))
        {
            return;
        }

        while (SelectionFilterHistory.Count >= selectionFilterMaxLength)
        {
            SelectionFilterHistory.RemoveAt(selectionFilterMaxLength - 1);
        }

        SelectionFilterHistory.Insert(0, filter);
    }

    /// <summary>As <c>MoveSelection</c>: the next or previous file of the current list (the staged one from the message), looping.</summary>
    public void MoveSelection(bool backwards, bool messageFocused)
    {
        if (messageFocused)
        {
            _currentFilesList = Staged;
        }

        FileStatusListViewModel list = _currentFilesList;
        list.SelectNextItem(backwards, loop: true);
        ApplySelection(list);
    }

    [RelayCommand]
    private void RescanChanges()
    {
        Initialize();
        SpellCheck?.LoadAutoCompleteWords();
    }

    /// <summary>As <c>StageClick</c>: stages the selected unstaged files.</summary>
    [RelayCommand]
    private void Stage() => StageSelected(skipAssumeUnchanged: true);

    private void StageSelected(bool skipAssumeUnchanged)
    {
        if (_host.IsBareRepository)
        {
            return;
        }

        StageItems([.. Unstaged.SelectedEntries.Select(e => e.Item).Where(s => !skipAssumeUnchanged || (!s.IsAssumeUnchanged && !s.IsSkipWorktree))]);
    }

    /// <summary>As <c>StageAllAccordingToFilter</c>: stages the unstaged files shown.</summary>
    [RelayCommand]
    private void StageAll()
    {
        StageItems([.. Unstaged.AllEntries.Select(e => e.Item).Where(s => !s.IsAssumeUnchanged && !s.IsSkipWorktree)]);
        Unstaged.Filter = "";
    }

    /// <summary>As <c>Stage</c>: the staged files leave the unstaged list, except dirty submodules.</summary>
    private void StageItems(IReadOnlyList<GitItemStatus> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        CanStage = false;
        try
        {
            string? next = GetNextItemToSelect(Unstaged, items);
            if (!_host.StageFiles(items))
            {
                RescanChanges();
                return;
            }

            IReadOnlyList<GitItemStatus> stagedFiles = _host.GetIndexFiles();
            HashSet<string?> names = [.. items.Select(i => i.Name), .. items.Select(i => i.OldName)];
            List<GitItemStatus> unstagedFiles = [];
            foreach (GitItemStatus item in Unstaged.AllItems)
            {
                if (names.Contains(item.Name) && (!item.IsSubmodule || !item.IsDirty))
                {
                    continue;
                }

                // Dirty submodules need to be kept in unstaged, update the status
                _host.UpdateSubmoduleStatus([item]);
                unstagedFiles.Add(item);
            }

            SetFiles(unstagedFiles, stagedFiles);
            HasMergeConflicts = _host.InTheMiddleOfConflictedMerge();
            if (unstagedFiles.Count == 0)
            {
                _currentFilesList = Staged;
                RestoreSelectedFiles(_currentSelection);
            }
            else
            {
                SelectByName(Unstaged, next);
            }
        }
        finally
        {
            CanStage = true;
            CanCommit = true;
        }

        _host.NotifyRepositoryChanged();
    }

    /// <summary>As <c>UnstageFilesClick</c>: unstages the selected staged files.</summary>
    [RelayCommand]
    private void Unstage() => UnstageSelected();

    private void UnstageSelected()
    {
        if (_host.IsBareRepository)
        {
            return;
        }

        List<GitItemStatus> files = [.. Staged.SelectedEntries.Select(e => e.Item)];
        if (files.Count == 0)
        {
            return;
        }

        int stagedCount = Staged.AllEntries.Count();
        if (stagedCount > 10 && files.Count == stagedCount)
        {
            UnstageAll();
            return;
        }

        UnstageItems(files);
    }

    /// <summary>As <c>Unstage</c>: the unstaged files are added to the unstaged list (renames as a deletion and an addition).</summary>
    private void UnstageItems(List<GitItemStatus> allFiles)
    {
        CanStage = false;
        try
        {
            string? next = GetNextItemToSelect(Staged, allFiles);
            bool shouldRescanChanges = _host.UnstageFiles(allFiles);
            IReadOnlyList<GitItemStatus> stagedFiles = _host.GetIndexFiles();
            List<GitItemStatus> unstagedFiles = [.. Unstaged.AllItems];
            foreach (GitItemStatus item in allFiles)
            {
                if (stagedFiles.Any(i => i.Name == item.Name))
                {
                    continue;
                }

                item.IsTracked = !item.IsNew || item.IsChanged || item.IsDeleted;
                if (!item.IsTracked)
                {
                    // Not re-read from git status, which reports an untracked directory without its submodule information.
                    item.IsSubmodule = false;
                }

                int index = unstagedFiles.FindIndex(i => i.Name == item.Name);
                if (index >= 0)
                {
                    unstagedFiles[index].IsNew = item.IsNew;
                    unstagedFiles[index].IsDeleted = item.IsDeleted;
                    unstagedFiles[index].IsTracked = item.IsTracked;
                    unstagedFiles[index].IsChanged = item.IsChanged;

                    // if this is a submodule, update the status, may be dirty
                    _host.UpdateSubmoduleStatus([unstagedFiles[index]]);
                    continue;
                }

                if (item.IsRenamed && item.OldName is not null)
                {
                    unstagedFiles.Add(new GitItemStatus(item.OldName)
                    {
                        IsDeleted = true,
                        IsTracked = true,
                        Staged = StagedStatus.WorkTree,
                    });
                    item.IsRenamed = false;
                    item.IsNew = true;
                    item.IsTracked = false;
                    item.OldName = string.Empty;
                }

                item.Staged = StagedStatus.WorkTree;
                unstagedFiles.Add(item);
            }

            SetFiles(unstagedFiles, stagedFiles);
            if (stagedFiles.Count == 0)
            {
                _currentFilesList = Unstaged;
                RestoreSelectedFiles(_currentSelection);
            }
            else
            {
                SelectByName(Staged, next);
            }

            if (shouldRescanChanges)
            {
                RescanChanges();
            }
        }
        finally
        {
            CanStage = true;
        }

        _host.NotifyRepositoryChanged();
    }

    /// <summary>As <c>UnstageAllFiles</c>: a merge or a filter unstages the files one by one, otherwise the index is reset.</summary>
    [RelayCommand]
    private void UnstageAll()
    {
        if (_isMergeCommit || Staged.IsFilterActive)
        {
            UnstageItems([.. Staged.AllEntries.Select(e => e.Item)]);
            Staged.Filter = "";
            return;
        }

        _host.UnstageAll();
        _currentFilesList = Unstaged;
        Initialize();
    }

    /// <summary>As <c>StoreNextItemToSelect</c>: the first file after the handled ones.</summary>
    private static string? GetNextItemToSelect(FileStatusListViewModel list, IReadOnlyList<GitItemStatus> handled)
    {
        HashSet<string> names = [.. handled.Select(i => i.Name)];
        List<string> files = [.. list.AllEntries.Select(e => e.Item.Name)];
        int last = files.FindLastIndex(names.Contains);
        return files.Skip(last + 1).FirstOrDefault(n => !names.Contains(n)) ?? files.LastOrDefault(n => !names.Contains(n));
    }

    private void SelectByName(FileStatusListViewModel list, string? name)
    {
        if (name is not null && list.AllEntries.Any(e => e.Item.Name == name))
        {
            list.Select(e => e.Item.Name == name);
        }
        else
        {
            list.SelectFirstVisibleItem();
        }

        ApplySelection(list);
    }

    [RelayCommand]
    private void Commit() => CheckForStagedAndCommit(push: false);

    /// <summary>As <c>CommitAndPush_Click</c>: without changes, the button only pushes.</summary>
    [RelayCommand]
    private void CommitAndPush()
    {
        if (!HasChanges && !Amend && !IsPushForced)
        {
            _host.Push(IsPushForced);
            return;
        }

        CheckForStagedAndCommit(push: true);
    }

    /// <summary>As <c>CheckForStagedAndCommit</c>.</summary>
    private void CheckForStagedAndCommit(bool push)
    {
        bool createAmendCommit = Amend;
        bool resetAuthor = Amend && ResetAuthor;
        bool pushForced = IsPushForced;
        BypassActivatedEventHandler(() =>
        {
            if (ConfirmOrStageCommit())
            {
                DoCommit(createAmendCommit, resetAuthor, push, pushForced);
            }
        });

        bool ConfirmOrStageCommit()
        {
            if (createAmendCommit)
            {
                // Amend may be used just to change the commit message or timestamp: no prompt for an empty commit.
                return _host.ConfirmAmend();
            }

            if (Staged.AllEntries.Any())
            {
                return true;
            }

            if (_isMergeCommit)
            {
                return _host.ConfirmEmptyMergeCommit();
            }

            switch (_host.AskWithoutStagedFiles(Unstaged.IsFilterActive, Unstaged.AllEntries.Any()))
            {
                case NoStagedFilesChoice.StageAllAndCommit:
                    StageAll();

                    // If staging failed (e.g. a line endings conflict), the user already got an error.
                    return Staged.AllEntries.Any();
                case NoStagedFilesChoice.EmptyCommit:
                    return true;
                default:
                    return false;
            }
        }
    }

    private void DoCommit(bool createAmendCommit, bool resetAuthor, bool push, bool pushForced)
    {
        if (_host.InTheMiddleOfConflictedMerge())
        {
            _host.ShowMergeConflicts();
            return;
        }

        string message = Message.Text;
        if (Options.UseFormCommitMessage && (string.IsNullOrEmpty(message) || message == _commitTemplate))
        {
            _host.ShowEnterCommitMessage();
            return;
        }

        if (Options.UseFormCommitMessage && !IsCommitMessageValid(message))
        {
            return;
        }

        if (!Options.DontConfirmCommitIfNoBranch && !_host.ConfirmDetachedHeadCommit(_editedCommit?.ObjectId))
        {
            return;
        }

        try
        {
            bool success = _host.Commit(new CommitRequest(
                message,
                createAmendCommit,
                SignOff,
                Author,
                NoVerify,
                AllowEmpty: !Staged.AllEntries.Any(),
                resetAuthor,
                UsingCommitTemplate: !string.IsNullOrEmpty(_commitTemplate),
                GpgSign: GpgSignIndex == 0 ? null : GpgSignIndex > 1,
                GpgKeyId: IsGpgKeyVisible ? GpgKeyId : ""));
            if (!success)
            {
                return;
            }

            Kind = CommitDialogKind.Normal;
            bool pushCompleted = true;
            try
            {
                if (push)
                {
                    pushCompleted = _host.Push(pushForced);
                }
            }
            finally
            {
                Message.Load("");
                IsAmendEnabled = true;
                Amend = false;
                NoVerify = false;
            }

            if (pushCompleted && HasSuperproject && StageInSuperproject)
            {
                _host.StageInSuperprojectNow();
            }

            if (CloseDialogAfterEachCommit || (CloseDialogAfterAllFilesCommitted && !Unstaged.AllEntries.Any()))
            {
                Close(accepted: true);
                return;
            }

            InitializeStaged();
        }
        catch (Exception e)
        {
            _host.ShowError($"Exception: {e.Message}");
        }
    }

    /// <summary>As <c>InitializedStaged</c>: the staged files again, after a commit.</summary>
    private void InitializeStaged()
    {
        HasMergeConflicts = _host.InTheMiddleOfConflictedMerge();
        (GitRevision? head, GitRevision index, GitRevision _) = _host.GetHeadRevisions();
        IReadOnlyList<GitItemStatus> stagedFiles = _host.GetIndexFiles();
        _skipUpdate = true;
        try
        {
            Staged.SetDiff(head, index, stagedFiles);
        }
        finally
        {
            _skipUpdate = false;
        }

        UpdateCounts(Unstaged.AllEntries.Count(), stagedFiles.Count);
    }

    /// <summary>As <c>IsCommitMessageValid</c>: the limits of the lines, the empty second line and the regular expression.</summary>
    private bool IsCommitMessageValid(string message)
    {
        if (Options.MaxFirstLineLength > 0)
        {
            string firstLine = message.Split(NewLines, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            if (firstLine.Length > Options.MaxFirstLineLength && !_host.ConfirmValidation(Strings.CommitMsgFirstLineInvalid.Text))
            {
                return false;
            }
        }

        if (Options.MaxLineLength > 0)
        {
            foreach (string line in message.Split(NewLines, StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.Length > Options.MaxLineLength && !_host.ConfirmValidation(string.Format(Strings.CommitMsgLineInvalid.Text, line)))
                {
                    return false;
                }
            }
        }

        if (Options.SecondLineMustBeEmpty)
        {
            string[] lines = message.Split(NewLines, StringSplitOptions.None);
            if (lines.Length > 2 && lines[1].Length != 0 && !_host.ConfirmValidation(Strings.CommitMsgSecondLineNotEmpty.Text))
            {
                return false;
            }
        }

        if (!string.IsNullOrEmpty(Options.ValidationRegex))
        {
            try
            {
                if (!message.StartsWith(GetPrefix(CommitDialogKind.Fixup))
                    && !message.StartsWith(GetPrefix(CommitDialogKind.Squash))
                    && !Regex.IsMatch(GetTextToValidate(message), Options.ValidationRegex)
                    && !_host.ConfirmValidation(Strings.CommitMsgRegExNotMatched.Text))
                {
                    return false;
                }
            }
            catch (ArgumentException)
            {
                // An invalid expression does not block the commit.
            }
        }

        return true;

        static string GetTextToValidate(string text)
        {
            if (!text.StartsWith(GetPrefix(CommitDialogKind.Amend)) || !text.Contains('\n'))
            {
                return text;
            }

            string[] lines = text.Split(NewLines, StringSplitOptions.None);
            return lines.Length > 2 && lines[1].Length == 0 ? string.Join(Environment.NewLine, lines.AsSpan(2)) : text;
        }
    }

    partial void OnAmendChanged(bool value)
    {
        if (_initializing)
        {
            return;
        }

        // As Amend_CheckedChanged.
        if (!value)
        {
            ResetAuthor = false;
        }

        if (value && string.IsNullOrEmpty(Message.Text) && _host.GetHeadMessage() is { } headMessage)
        {
            Message.Text = headMessage.Trim();
        }

        CanResetSoft = value && _host.CanResetSoft();
        if (SelectStagedOnEnterMessage)
        {
            OnMessageEntered();
        }
    }

    partial void OnStageInSuperprojectChanged(bool value) => _settings.StageInSuperproject = value;

    partial void OnCloseDialogAfterEachCommitChanged(bool value) => _settings.CloseDialogAfterEachCommit = value;

    partial void OnCloseDialogAfterAllFilesCommittedChanged(bool value) => _settings.CloseDialogAfterAllFilesCommitted = value;

    partial void OnRefreshDialogOnFormFocusChanged(bool value) => _settings.RefreshDialogOnFormFocus = value;

    partial void OnSelectStagedOnEnterMessageChanged(bool value) => _settings.SelectStagedOnEnterMessage = value;

    partial void OnShowOnlyMyMessagesChanged(bool value) => _settings.ShowOnlyMyMessages = value;

    /// <summary>As <c>modifyCommitMessageButton_Click</c>: the message of a fixup or squash commit becomes editable.</summary>
    [RelayCommand]
    private void ModifyCommitMessage() => Kind = CommitDialogKind.Normal;

    /// <summary>The previous messages for the message menu (as <c>CommitMessageToolStripMenuItemDropDownOpening</c>).</summary>
    public IReadOnlyList<string> GetPreviousMessages() => _host.GetPreviousMessages(ShowOnlyMyMessages);

    /// <summary>As <c>CommitMessageToolStripMenuItemDropDownItemClicked</c>.</summary>
    public void UsePreviousMessage(string message) => Message.Text = message.Trim();

    /// <summary>The templates for the templates menu: those of the plugins, then those of the settings (with names).</summary>
    public (IReadOnlyList<CommitTemplateItem> Registered, IReadOnlyList<CommitTemplateItem> FromSettings) GetCommitTemplates()
    {
        (IReadOnlyList<CommitTemplateItem> registered, IReadOnlyList<CommitTemplateItem> fromSettings) = _host.GetCommitTemplates();
        return ([.. registered.Where(t => !string.IsNullOrEmpty(t.Name))], [.. fromSettings.Where(t => !string.IsNullOrEmpty(t.Name))]);
    }

    /// <summary>As the click on a template: its text replaces the message (with the matches of the branch, if a regex).</summary>
    public void ApplyTemplate(CommitTemplateItem template)
    {
        Message.Text = ReplaceBranchPatterns(template.Text, template.IsRegex);
        Message.RequestFocus();
    }

    /// <summary>
    ///  As <c>ReplaceMessage(message, regexEnabled)</c>: <c>{{pattern}}[index]</c> is replaced with the group of the pattern in
    ///  the current branch.
    /// </summary>
    private string ReplaceBranchPatterns(string message, bool regexEnabled)
    {
        if (!regexEnabled)
        {
            return message;
        }

        try
        {
            foreach (Match regexMatch in ReplaceMessageRegex().Matches(message))
            {
                string pattern = regexMatch.Groups["pattern"].Value;
                int groupIndex = int.TryParse(regexMatch.Groups["index"].ValueSpan, out int parsedIndex) ? parsedIndex : 1;
                MatchCollection matches = new Regex(pattern).Matches(_host.GetCurrentBranch());
                string replaceText = matches.Count > 0 && matches[0].Groups.Count > groupIndex ? matches[0].Groups[groupIndex].Value : "";
                message = message.Replace(regexMatch.Groups[0].Value, replaceText);
            }
        }
        catch (ArgumentException)
        {
            // An invalid pattern leaves the rest of the message.
        }

        return message;
    }

    [GeneratedRegex(@"\{\{(?<pattern>.*?)\}\}(?:\[(?<index>\d+)\])?", RegexOptions.ExplicitCapture)]
    private static partial Regex ReplaceMessageRegex();

    /// <summary>As a type of the Conventional Commits menu: the type of the subject (optionally with a scope).</summary>
    public void ApplyConventionalType(string keyword, bool insertScope = false)
    {
        string message = Message.Text;
        (string title, int selectionStart) = ConventionalCommits.PrefixOrReplaceKeyword(message, Message.CaretOffset, keyword, insertScope);
        Message.Text = ConventionalCommits.ReplaceFirstLine(message, title);
        Message.CaretOffset = Math.Min(selectionStart, Message.Text.Length);
        Message.RequestFocus();
    }

    /// <summary>As a footer of the Conventional Commits menu: a new last line.</summary>
    public void AddConventionalFooter(string footer, bool keepCursorPosition = false)
    {
        int caret = Message.CaretOffset;
        Message.Text = ConventionalCommits.AddFooter(Message.Text, footer);
        Message.CaretOffset = keepCursorPosition ? caret : Message.Text.Length;
        Message.RequestFocus();
    }

    [RelayCommand]
    private void EditCommitTemplateSettings() => _host.EditCommitTemplateSettings();

    [RelayCommand]
    private void OpenConventionalCommitsDocumentation() => _host.OpenUrl(ConventionalCommits.DocumentationUrl);

    /// <summary>As <c>ResetSoftClick</c>: HEAD is reset to its parent, keeping the changes staged.</summary>
    [RelayCommand]
    private void ResetSoft()
    {
        if (!_host.ConfirmResetSoft())
        {
            return;
        }

        try
        {
            _host.ResetSoft();
            IsAmendEnabled = false;
            Amend = false;
        }
        finally
        {
            _host.NotifyRepositoryChanged();
            Initialize();
        }
    }

    [RelayCommand]
    private void SolveMergeConflicts()
    {
        if (_host.ResolveConflicts())
        {
            Initialize();
        }
    }

    [RelayCommand]
    private void ResetAllChanges() => ResetChanges(onlyWorkTree: false);

    [RelayCommand]
    private void ResetUnstagedChanges() => ResetChanges(onlyWorkTree: true);

    private void ResetChanges(bool onlyWorkTree)
    {
        BypassActivatedEventHandler(() => _host.ResetChanges([.. Unstaged.AllEntries.Select(e => e.Item)], onlyWorkTree));
        Initialize();
    }

    [RelayCommand]
    private void StashStaged()
    {
        BypassActivatedEventHandler(_host.StashStaged);
        Initialize();
    }

    [RelayCommand]
    private void CreateBranch()
    {
        if (_host.CreateBranch())
        {
            _ = UpdateBranchInfoAsync();
        }
    }

    [RelayCommand]
    private void EditCommitter()
    {
        _host.EditCommitterSettings();
        UpdateAuthorInfo();
    }

    private void BypassActivatedEventHandler(Action action)
    {
        _bypassActivatedEventHandler = true;
        try
        {
            action();
        }
        finally
        {
            _bypassActivatedEventHandler = false;
        }
    }
}
