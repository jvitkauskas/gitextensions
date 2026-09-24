using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitUI.Presentation.Editor;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUIPluginInterfaces;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the stash dialog; ids match <c>FormStash</c>.</summary>
public sealed class StashStrings : ViewStrings
{
    public StashStrings()
        : base("FormStash")
    {
        Title = Add("$this", "Text", "Stash");
        Apply = Add("Apply", "Text", "&Apply Selected Stash");
        ApplyToolTip = Add("Apply", "toolTip", "Apply the selected stash on top of the current working directory state");
        Drop = Add("Clear", "Text", "&Drop Selected Stash");
        DropToolTip = Add("Clear", "toolTip", "Remove the selected stash from the list");
        StashAll = Add("Stash", "Text", "S&tash all changes");
        StashAllToolTip = Add("Stash", "toolTip", "Save local changes to a new stash, then revert local changes");
        KeepIndex = Add("StashKeepIndex", "Text", "&Keep index");
        KeepIndexToolTip = Add("StashKeepIndex", "toolTip", "All changes already added to the index are left intact");
        StashSelected = Add("StashSelectedFiles", "Text", "Stash &selected changes");
        StashSelectedToolTip = Add("StashSelectedFiles", "toolTip", "Stash changes for the selected files, then revert them to the original state");
        StashesToolTip = Add("Stashes", "ToolTipText", "Select a stash");
        CurrentWorkingDirChanges = Add("_currentWorkingDirChanges", "Text", "Current working directory changes");
        NoStashes = Add("_noStashes", "Text", "There are no stashes.");
        IncludeUntracked = Add("chkIncludeUntrackedFiles", "Text", "&Include untracked files");
        IncludeUntrackedToolTip = Add("chkIncludeUntrackedFiles", "toolTip", "All untracked files are also stashed and then cleaned");
        Message = Add("messageLabel", "Text", "&Message:");
        Show = Add("showToolStripLabel", "Text", "S&how:");
    }

    public TranslatedText Title { get; }

    public TranslatedText Apply { get; }

    public TranslatedText ApplyToolTip { get; }

    public TranslatedText Drop { get; }

    public TranslatedText DropToolTip { get; }

    public TranslatedText StashAll { get; }

    public TranslatedText StashAllToolTip { get; }

    public TranslatedText KeepIndex { get; }

    public TranslatedText KeepIndexToolTip { get; }

    public TranslatedText StashSelected { get; }

    public TranslatedText StashSelectedToolTip { get; }

    public TranslatedText StashesToolTip { get; }

    public TranslatedText CurrentWorkingDirChanges { get; }

    public TranslatedText NoStashes { get; }

    public TranslatedText IncludeUntracked { get; }

    public TranslatedText IncludeUntrackedToolTip { get; }

    public TranslatedText Message { get; }

    public TranslatedText Show { get; }
}

/// <summary>Operations of the stash dialog that need the host (git, the settings, the process dialog).</summary>
public interface IStashHost
{
    IReadOnlyList<GitStash> GetStashes();

    /// <summary>
    ///  The files of the stash, or of the working directory (as <c>FormStash.LoadGitItemStatuses</c>: the working directory and
    ///  the index compared to HEAD).
    /// </summary>
    Task<IReadOnlyList<FileStatusGroup>> GetFilesAsync(GitStash? stash, CancellationToken cancellationToken);

    /// <summary>Saves the changes (of the files, if any) in a new stash (<c>StashSave</c>).</summary>
    void Save(bool includeUntrackedFiles, bool keepIndex, string message, IReadOnlyList<string>? files);

    /// <summary>Asks to confirm dropping a stash, unless the user chose not to be asked (<c>AppSettings.DontConfirmStashDrop</c>).</summary>
    bool ConfirmDrop();

    void Drop(string stashName);

    void Apply(string stashName);

    /// <summary>The check boxes as the user left them (<c>AppSettings.StashKeepIndex</c>, <c>IncludeUntrackedFilesInManualStash</c>).</summary>
    (bool KeepIndex, bool IncludeUntrackedFiles) LoadSettings();

    void SaveSettings(bool keepIndex, bool includeUntrackedFiles);
}

/// <summary>View model of the stash dialog (port of <c>FormStash</c>): the stashes, their files and the stash operations.</summary>
public sealed partial class StashViewModel : DialogViewModel
{
    private readonly IStashHost _host;
    private readonly GitStash _currentWorkingDirStashItem;
    private int _lastSelectedStashIndex = -1;
    private bool _manageStashes;
    private CancellationTokenSource? _loading;

    /// <param name="manageStashes">Whether the first stash is selected (else the working directory changes).</param>
    /// <param name="initialStash">The stash to select first, e.g. <c>stash@{1}</c>.</param>
    public StashViewModel(
        StashStrings strings,
        IStashHost host,
        IFileViewerHost fileViewerHost,
        FileStatusListStrings fileStatusListStrings,
        FileStatusTreeOptions fileStatusTreeOptions,
        bool manageStashes,
        string? initialStash = null)
    {
        Strings = strings;
        _host = host;
        _manageStashes = manageStashes;
        _currentWorkingDirStashItem = new GitStash(-1, strings.CurrentWorkingDirChanges.Text);
        if (initialStash is not null
            && initialStash.IndexOf('{') is int start and >= 0
            && initialStash.IndexOf('}', start) is int end and > 0
            && int.TryParse(initialStash[(start + 1)..end], out int index))
        {
            _lastSelectedStashIndex = index + 1;
        }

        (KeepIndex, IncludeUntrackedFiles) = host.LoadSettings();
        Files = new FileStatusListViewModel(fileStatusListStrings, fileStatusTreeOptions);
        Viewer = new FileViewerViewModel(fileViewerHost);

        // As FileViewer_TopScrollReached and FileViewer_BottomScrollReached.
        Viewer.ScrollOnThrough(() => Files);
        Files.SelectionChanged += (_, _) =>
        {
            _ = Viewer.ShowChangesAsync(Files.SelectedEntry);
            OnPropertyChanged(nameof(CanStashSelected));
        };
        Files.RefreshRequested += (_, _) => RefreshAll(force: false);
    }

    public StashStrings Strings { get; }

    public FileStatusListViewModel Files { get; }

    public FileViewerViewModel Viewer { get; }

    /// <summary>The working directory changes, then the stashes, the newest first.</summary>
    public ObservableCollection<GitStash> Stashes { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWorkingDirectorySelected), nameof(IsMessageReadOnly), nameof(CanApplyOrDrop), nameof(CanStashSelected))]
    public partial GitStash? SelectedStash { get; set; }

    [ObservableProperty]
    public partial string Message { get; set; } = "";

    /// <summary>"There are no stashes." if there are none, shown in the empty message.</summary>
    [ObservableProperty]
    public partial string? MessagePlaceholder { get; private set; }

    [ObservableProperty]
    public partial bool KeepIndex { get; set; }

    [ObservableProperty]
    public partial bool IncludeUntrackedFiles { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; private set; }

    public bool IsWorkingDirectorySelected => SelectedStash == _currentWorkingDirStashItem;

    /// <summary>As <c>InitializeSoft</c>: the message of a new stash is only written for the working directory.</summary>
    public bool IsMessageReadOnly => !IsWorkingDirectorySelected;

    /// <summary>As the Apply and Drop buttons: not for the working directory.</summary>
    public bool CanApplyOrDrop => SelectedStash is not null && !IsWorkingDirectorySelected;

    /// <summary>As <c>EnablePartialStash</c>.</summary>
    public bool CanStashSelected => IsWorkingDirectorySelected && Files.SelectedEntries.Count > 0;

    /// <summary>As <c>FormStashShown</c>.</summary>
    public void InitializeView() => RefreshAll(force: true);

    [RelayCommand]
    private void Refresh() => RefreshAll(force: true);

    [RelayCommand]
    private void StashAll()
    {
        _host.Save(IncludeUntrackedFiles, KeepIndex, GetStashMessage(), files: null);
        Initialize();
    }

    [RelayCommand]
    private void StashSelected()
    {
        _host.Save(IncludeUntrackedFiles, KeepIndex, GetStashMessage(), [.. Files.SelectedEntries.Select(e => e.Item.Name)]);
        Initialize();
    }

    /// <summary>As <c>ClearClick</c>.</summary>
    [RelayCommand]
    private void Drop()
    {
        if (SelectedStash is not { } stash || !CanApplyOrDrop || !_host.ConfirmDrop())
        {
            return;
        }

        _lastSelectedStashIndex = Stashes.IndexOf(stash);
        _host.Drop(stash.Name);
        Initialize();
    }

    [RelayCommand]
    private void Apply()
    {
        if (SelectedStash is { } stash && CanApplyOrDrop)
        {
            _host.Apply(stash.Name);
            Initialize();
        }
    }

    /// <summary>As the hotkeys NextStash / PreviousStash: newer stashes are first in the list.</summary>
    public bool SelectNextStash(bool next)
    {
        int index = (SelectedStash is null ? -1 : Stashes.IndexOf(SelectedStash)) + (next ? -1 : 1);
        if (index < 0 || index >= Stashes.Count)
        {
            return false;
        }

        SelectedStash = Stashes[index];
        return true;
    }

    /// <summary>As <c>FormStashFormClosing</c>: the check boxes are kept.</summary>
    public override bool CanClose()
    {
        _host.SaveSettings(KeepIndex, IncludeUntrackedFiles);
        return true;
    }

    private string GetStashMessage() => !string.IsNullOrWhiteSpace(Message) ? " " + Message.Trim() : string.Empty;

    /// <summary>As <c>RefreshAll</c>: the working directory changes are only reloaded when shown.</summary>
    private void RefreshAll(bool force)
    {
        if (force || IsWorkingDirectorySelected)
        {
            Initialize();
        }
    }

    /// <summary>As <c>FormStash.Initialize</c>.</summary>
    private void Initialize()
    {
        List<GitStash> stashes = [_currentWorkingDirStashItem, .. _host.GetStashes()];
        Message = "";
        SelectedStash = null;
        Stashes.Clear();
        foreach (GitStash stash in stashes)
        {
            Stashes.Add(stash);
        }

        if (_lastSelectedStashIndex > 0)
        {
            // Last operation was a drop, select next index
            if (_lastSelectedStashIndex >= Stashes.Count)
            {
                _lastSelectedStashIndex--;
            }

            SelectedStash = Stashes[_lastSelectedStashIndex];
            _lastSelectedStashIndex = -1;
        }
        else if (_manageStashes && Stashes.Count > 1)
        {
            // more than just the default ("Current working directory changes")
            SelectedStash = Stashes[1];

            // First load done, show worktree on next refresh
            _manageStashes = false;
        }
        else
        {
            // (no stashes) -> select default ("Current working directory changes")
            SelectedStash = Stashes[0];
        }
    }

    /// <summary>As <c>StashesSelectedIndexChanged</c> and <c>InitializeSoft</c>.</summary>
    partial void OnSelectedStashChanged(GitStash? value)
    {
        if (value is null)
        {
            return;
        }

        Message = value != _currentWorkingDirStashItem ? value.Message : "";

        // FormStash puts this text in the message, which "Stash all changes" then saves as the message of the stash.
        MessagePlaceholder = Stashes.Count == 1 ? Strings.NoStashes.Text : null;

        _ = LoadFilesAsync(value);
    }

    private async Task LoadFilesAsync(GitStash stash)
    {
        CancellationTokenSource? previous = _loading;
        if (previous is not null)
        {
            // Synchronously: awaiting CancelAsync could continue off the UI thread, and no long callbacks are registered.
#pragma warning disable VSTHRD103 // Call async methods when in an async method
            previous.Cancel();
#pragma warning restore VSTHRD103

            // Not disposed: the work in the background may still use its token (a disposed source throws ObjectDisposedException).
        }

        CancellationTokenSource loading = new();
        _loading = loading;
        IsLoading = true;
        Files.SetLoading();
        IReadOnlyList<FileStatusGroup> groups;
        try
        {
            groups = await _host.GetFilesAsync(stash == _currentWorkingDirStashItem ? null : stash, loading.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (!loading.IsCancellationRequested)
        {
            Files.SetGroups(groups);
            IsLoading = false;
        }
    }
}
