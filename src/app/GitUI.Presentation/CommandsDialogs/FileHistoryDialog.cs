using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Editor;
using GitUI.Presentation.HelperDialogs;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls.Blame;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUIPluginInterfaces;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the file history; ids match <c>FormFileHistory</c>.</summary>
public sealed class FileHistoryStrings : ViewStrings
{
    public FileHistoryStrings()
        : base("FormFileHistory")
    {
        Title = Add("$this", "Text", "File History");
        CommitTab = Add("CommitInfoTabPage", "Text", "Commit");
        DiffTab = Add("DiffTab", "Text", "Diff");
        ViewTab = Add("ViewTab", "Text", "View");
        BlameTab = Add("BlameTab", "Text", "Blame");
        FileNotFound = Add("_fileNotFound", "Text", " - Git could not identify the file {0}");
        LoadFileHistory = Add("toolStripSplitLoad", "ToolTipText", "Load file history");
        LoadHistoryOnShow = Add("loadHistoryOnShowToolStripMenuItem", "Text", "Load history on show");
        LoadBlameOnShow = Add("loadBlameOnShowToolStripMenuItem", "Text", "Load blame on show");
        ShowFullHistoryToolTip = Add("ShowFullHistory", "ToolTipText", "Show Full History");
        ShowFullHistory = Add("showFullHistoryToolStripMenuItem", "Text", "Show full history");
        SimplifyMerges = Add("simplifyMergesToolStripMenuItem", "Text", "Simplify merges");
        BlameOptions = Add("toolStripBlameOptions", "Text", "Blame options");
        BlameSettings = Add("blameSettingsToolStripMenuItem", "Text", "Blame settings:");
        IgnoreWhitespace = Add("ignoreWhitespaceToolStripMenuItem", "Text", "Ignore whitespace");
        DetectMoveAndCopyInThisFile = Add("detectMoveAndCopyInThisFileToolStripMenuItem", "Text", "Detect move and copy in this file");
        DetectMoveAndCopyInAllFiles = Add("detectMoveAndCopyInAllFilesToolStripMenuItem", "Text", "Detect move and copy in all files");
        DisplaySettings = Add("displaySettingsToolStripMenuItem", "Text", "Display result settings:");
        DisplayAuthorFirst = Add("displayAuthorFirstToolStripMenuItem", "Text", "Display author first");
        ShowAuthorAvatar = Add("showAuthorAvatarToolStripMenuItem", "Text", "Show author avatar");
        ShowAuthor = Add("showAuthorToolStripMenuItem", "Text", "Show author");
        ShowAuthorDate = Add("showAuthorDateToolStripMenuItem", "Text", "Show author date");
        ShowAuthorTime = Add("showAuthorTimeToolStripMenuItem", "Text", "Show author time");
        ShowLineNumbers = Add("showLineNumbersToolStripMenuItem", "Text", "Show line numbers");
        ShowOriginalFilePath = Add("showOriginalFilePathToolStripMenuItem", "Text", "Show original file path");
        GitCommandLog = Add("gitcommandLogToolStripMenuItem", "ToolTipText", "Git command log");
        CopyToClipboard = Add("copyToClipboardToolStripMenuItem", "Text", "Copy to clipboard");
        OpenWithDifftool = Add("openWithDifftoolToolStripMenuItem", "Text", "Open with difftool");
        DiffToolRemoteLocal = Add("diffToolRemoteLocalStripMenuItem", "Text", "Difftool selected < - > local");
        SaveAs = Add("saveAsToolStripMenuItem", "Text", "Save as");
        ManipulateCommit = Add("manipulateCommitToolStripMenuItem", "Text", "Manipulate commit");
        RevertCommit = Add("revertCommitToolStripMenuItem", "Text", "Revert commit");
        CherryPickCommit = Add("cherryPickThisCommitToolStripMenuItem", "Text", "Cherry pick commit");
        FollowRenames = Add("followFileHistoryToolStripMenuItem", "Text", "Detect and follow renames");
        FollowRenamesExactOnly = Add("followFileHistoryRenamesToolStripMenuItem", "Text", "Detect and follow - exact renames and copies only");
    }

    public TranslatedText Title { get; }

    public TranslatedText CommitTab { get; }

    public TranslatedText DiffTab { get; }

    public TranslatedText ViewTab { get; }

    public TranslatedText BlameTab { get; }

    public TranslatedText FileNotFound { get; }

    public TranslatedText LoadFileHistory { get; }

    public TranslatedText LoadHistoryOnShow { get; }

    public TranslatedText LoadBlameOnShow { get; }

    public TranslatedText ShowFullHistoryToolTip { get; }

    public TranslatedText ShowFullHistory { get; }

    public TranslatedText SimplifyMerges { get; }

    public TranslatedText BlameOptions { get; }

    public TranslatedText BlameSettings { get; }

    public TranslatedText IgnoreWhitespace { get; }

    public TranslatedText DetectMoveAndCopyInThisFile { get; }

    public TranslatedText DetectMoveAndCopyInAllFiles { get; }

    public TranslatedText DisplaySettings { get; }

    public TranslatedText DisplayAuthorFirst { get; }

    public TranslatedText ShowAuthorAvatar { get; }

    public TranslatedText ShowAuthor { get; }

    public TranslatedText ShowAuthorDate { get; }

    public TranslatedText ShowAuthorTime { get; }

    public TranslatedText ShowLineNumbers { get; }

    public TranslatedText ShowOriginalFilePath { get; }

    public TranslatedText GitCommandLog { get; }

    public TranslatedText CopyToClipboard { get; }

    public TranslatedText OpenWithDifftool { get; }

    public TranslatedText DiffToolRemoteLocal { get; }

    public TranslatedText SaveAs { get; }

    public TranslatedText ManipulateCommit { get; }

    public TranslatedText RevertCommit { get; }

    public TranslatedText CherryPickCommit { get; }

    public TranslatedText FollowRenames { get; }

    public TranslatedText FollowRenamesExactOnly { get; }
}

/// <summary>The tabs of the file history.</summary>
public enum FileHistoryTab
{
    Commit,
    Diff,
    View,
    Blame,
}

/// <summary>The settings of the file history (the <c>AppSettings</c> it toggles).</summary>
public interface IFileHistorySettings
{
    bool FollowRenames { get; set; }

    bool FollowRenamesExactOnly { get; set; }

    bool FullHistory { get; set; }

    bool SimplifyMerges { get; set; }

    bool LoadHistoryOnShow { get; set; }

    bool LoadBlameOnShow { get; set; }

    bool IgnoreWhitespaceOnBlame { get; set; }

    bool DetectCopyInFileOnBlame { get; set; }

    bool DetectCopyInAllOnBlame { get; set; }

    bool BlameDisplayAuthorFirst { get; set; }

    bool BlameShowAuthorAvatar { get; set; }

    bool BlameShowAuthor { get; set; }

    bool BlameShowAuthorDate { get; set; }

    bool BlameShowAuthorTime { get; set; }

    bool BlameShowLineNumbers { get; set; }

    bool BlameShowOriginalFilePath { get; set; }
}

/// <summary>What an item of the copy menu of revisions is (as <c>CopyContextMenuItem</c>).</summary>
public enum RevisionCopyItemKind
{
    Caption,
    Item,
    Separator,
}

/// <summary>An item of the copy menu of revisions: its header (WinForms access keys) and the text it copies.</summary>
public sealed record RevisionCopyItem(RevisionCopyItemKind Kind, string Header = "", string Text = "");

/// <summary>Operations of the file history that need the host (git, dialogs).</summary>
public interface IFileHistoryHost
{
    IFileHistorySettings Settings { get; }

    /// <summary>The working directory as shown in the title (<c>PathUtil.GetDisplayPath</c>).</summary>
    string WorkingDirectory { get; }

    /// <summary>Whether the file is a submodule, which has no blame.</summary>
    bool IsSubmodule { get; }

    /// <summary>The text shown for a diff without changes (<c>TranslatedStrings.NoChanges</c>).</summary>
    string NoChangesText { get; }

    /// <summary>
    ///  The name of the file in the revision, following renames (<c>FormFileHistory.GetFileNameForRevision</c>);
    ///  <see langword="null"/> if not known.
    /// </summary>
    string? GetFileName(GitRevision revision);

    /// <summary>Whether the file exists in the revision (or in the working directory for artificial commits).</summary>
    bool IsFileAvailable(string fileName, GitRevision revision);

    /// <summary>Whether the file exists in the working directory (for the difftool to the local file).</summary>
    bool IsInWorkingDirectory(string fileName);

    /// <summary>The revision with its real parents, as the path filter rewrites them (<c>GetActualRevision</c>).</summary>
    GitRevision GetActualRevision(GitRevision revision);

    /// <summary>The revision of a hash, from git if the grid does not list it.</summary>
    GitRevision? GetRevision(ObjectId objectId);

    /// <summary>The commit of an abbreviated hash, a branch or a tag (the commands of the commit info links).</summary>
    ObjectId? ResolveCommit(string commitOrRef, bool isRef);

    /// <summary>As <c>UICommands.OpenWithDifftool</c> (<c>RevisionDiffKind.DiffAB</c>, or <c>DiffBLocal</c> if <paramref name="toLocal"/>).</summary>
    /// <param name="customTool">The difftool of the submenu, else the default one.</param>
    void OpenWithDifftool(IReadOnlyList<GitRevision> revisions, string fileName, string? revisionFileName, bool toLocal, string? customTool = null);

    /// <summary>As <c>saveAsToolStripMenuItem_Click</c>.</summary>
    void SaveAs(GitRevision revision, string fileName);

    void CherryPick(GitRevision revision);

    void Revert(GitRevision revision);

    /// <summary>As <c>RevisionGridControl.ViewSelectedRevisions</c> (a double click).</summary>
    void ViewRevisions(IReadOnlyList<GitRevision> revisions);

    void ShowGitCommandLog();

    void ShowRevisionFiltered(ObjectId objectId);

    /// <summary>The copy menu of the revisions (as <c>CopyContextMenuItem.OnDropDownOpening</c>).</summary>
    IReadOnlyList<RevisionCopyItem> GetCopyItems(IReadOnlyList<GitRevision> revisions);

    void CopyToClipboard(string text);
}

/// <summary>What the context menu of the grid enables (as <c>FileHistoryContextMenuOpening</c>).</summary>
public sealed record FileHistoryMenuState(bool CanDiffToLocal, bool CanOpenWithDifftool, bool CanManipulateCommit, bool CanSaveAs, bool CanCopy);

/// <summary>
///  View model of the file history (port of <c>FormFileHistory</c>; docs/avalonia-port/PLAN.md, phase 5): the revisions
///  that changed a file, with the commit, the diff, the file and the blame of the selected revision.
/// </summary>
public sealed partial class FileHistoryViewModel : DialogViewModel
{
    private readonly IFileHistoryHost _host;
    private readonly IFileHistorySettings _settings;
    private readonly ObjectId? _selectedId;
    private bool _initializing = true;

    public FileHistoryViewModel(
        FileHistoryStrings strings,
        IFileHistoryHost host,
        RevisionGridViewModel grid,
        CommitDiffViewModel commitDiff,
        IFileViewerHost fileViewerHost,
        BlameViewModel blame,
        string fileName,
        ObjectId? selectedId = null,
        bool showBlame = false)
    {
        Strings = strings;
        _host = host;
        _settings = host.Settings;
        _selectedId = selectedId;

        // As the constructor: Windows separators are replaced, the file history is also started from the file tree.
        FileName = fileName.Trim('"').Replace('\\', '/');
        Grid = grid;
        CommitDiff = commitDiff;
        Diff = new FileViewerViewModel(fileViewerHost);
        View = new FileViewerViewModel(fileViewerHost);
        Blame = blame;
        Blame.RevisionGrid = new BlameGrid(this);
        Blame.CommitInfo.CommandClicked += (_, e) => OnBlameCommand(e.Command, e.Data);
        IsBlameTabVisible = !host.IsSubmodule;
        HasBlame = !host.IsSubmodule;
        CanSaveAsVisible = HasBlame;
        CommitTabHeader = strings.CommitTab.PlainText;
        Title = GetTitle(alternativeFileName: null);

        FollowRenames = _settings.FollowRenames;
        FollowRenamesExactOnly = _settings.FollowRenamesExactOnly;
        FullHistory = _settings.FullHistory;
        SimplifyMerges = _settings.SimplifyMerges;
        LoadHistoryOnShow = _settings.LoadHistoryOnShow;
        LoadBlameOnShow = _settings.LoadBlameOnShow && IsBlameTabVisible;
        IgnoreWhitespaceOnBlame = _settings.IgnoreWhitespaceOnBlame;
        DetectCopyInFileOnBlame = _settings.DetectCopyInFileOnBlame;
        DetectCopyInAllOnBlame = _settings.DetectCopyInAllOnBlame;
        BlameDisplayAuthorFirst = _settings.BlameDisplayAuthorFirst;
        BlameShowAuthorAvatar = _settings.BlameShowAuthorAvatar;
        BlameShowAuthor = _settings.BlameShowAuthor;
        BlameShowAuthorDate = _settings.BlameShowAuthorDate;
        BlameShowAuthorTime = _settings.BlameShowAuthorTime;
        BlameShowLineNumbers = _settings.BlameShowLineNumbers;
        BlameShowOriginalFilePath = _settings.BlameShowOriginalFilePath;
        SelectedTab = IsBlameTabVisible && showBlame ? FileHistoryTab.Blame : FileHistoryTab.Diff;
        _initializing = false;

        Grid.SelectionChanged += (_, _) => UpdateSelectedFileViewers();

        // As the WinForms grid without a revision to select: the first real revision is selected.
        Grid.Loaded += (_, _) =>
        {
            if (Grid.SelectedRow is null && Grid.Rows.FirstOrDefault(row => !row.Revision.IsArtificial) is { } first)
            {
                Grid.SelectedRow = first;
            }
        };
    }

    public FileHistoryStrings Strings { get; }

    /// <summary>The file, with Posix separators.</summary>
    public string FileName { get; }

    public RevisionGridViewModel Grid { get; }

    public CommitDiffViewModel CommitDiff { get; }

    public FileViewerViewModel Diff { get; }

    public FileViewerViewModel View { get; }

    public BlameViewModel Blame { get; }

    [ObservableProperty]
    public partial string Title { get; private set; }

    /// <summary>Whether the revisions are shown; they are not until loaded (unless loaded on show).</summary>
    [ObservableProperty]
    public partial bool IsGridVisible { get; private set; }

    [ObservableProperty]
    public partial FileHistoryTab SelectedTab { get; set; }

    [ObservableProperty]
    public partial string CommitTabHeader { get; private set; }

    [ObservableProperty]
    public partial bool IsCommitTabVisible { get; private set; } = true;

    [ObservableProperty]
    public partial bool IsDiffTabVisible { get; private set; } = true;

    [ObservableProperty]
    public partial bool IsViewTabVisible { get; private set; } = true;

    [ObservableProperty]
    public partial bool IsBlameTabVisible { get; private set; }

    /// <summary>Whether the file has a blame (not a submodule), and so the blame options.</summary>
    public bool HasBlame { get; }

    public bool CanSaveAsVisible { get; }

    // The settings of the toolbar and the menu, as their check boxes.

    [ObservableProperty]
    public partial bool FollowRenames { get; set; }

    [ObservableProperty]
    public partial bool FollowRenamesExactOnly { get; set; }

    [ObservableProperty]
    public partial bool FullHistory { get; set; }

    [ObservableProperty]
    public partial bool SimplifyMerges { get; set; }

    [ObservableProperty]
    public partial bool LoadHistoryOnShow { get; set; }

    [ObservableProperty]
    public partial bool LoadBlameOnShow { get; set; }

    [ObservableProperty]
    public partial bool IgnoreWhitespaceOnBlame { get; set; }

    [ObservableProperty]
    public partial bool DetectCopyInFileOnBlame { get; set; }

    [ObservableProperty]
    public partial bool DetectCopyInAllOnBlame { get; set; }

    [ObservableProperty]
    public partial bool BlameDisplayAuthorFirst { get; set; }

    [ObservableProperty]
    public partial bool BlameShowAuthorAvatar { get; set; }

    [ObservableProperty]
    public partial bool BlameShowAuthor { get; set; }

    [ObservableProperty]
    public partial bool BlameShowAuthorDate { get; set; }

    [ObservableProperty]
    public partial bool BlameShowAuthorTime { get; set; }

    [ObservableProperty]
    public partial bool BlameShowLineNumbers { get; set; }

    [ObservableProperty]
    public partial bool BlameShowOriginalFilePath { get; set; }

    /// <summary>As <c>OnRuntimeLoad</c>: loads the history if set so (or the blame is shown and loaded on show).</summary>
    public void Initialize()
    {
        bool autoLoad = (SelectedTab == FileHistoryTab.Blame && LoadBlameOnShow) || LoadHistoryOnShow;
        if (autoLoad)
        {
            LoadFileHistory();
        }
    }

    /// <summary>As <c>LoadFileHistory</c> (and the load button).</summary>
    [RelayCommand]
    public void LoadFileHistory()
    {
        IsGridVisible = true;
        if (string.IsNullOrEmpty(FileName))
        {
            return;
        }

        Grid.Load(Grid.SelectedRow?.ObjectId ?? _selectedId);
    }

    /// <summary>A double click on a revision (<c>FileChangesDoubleClick</c>).</summary>
    public void ViewSelectedRevisions() => _host.ViewRevisions(Grid.GetSelectedRevisionsLatestSelectedFirst());

    [RelayCommand]
    private void ShowGitCommandLog() => _host.ShowGitCommandLog();

    partial void OnSelectedTabChanged(FileHistoryTab value)
    {
        if (!_initializing)
        {
            UpdateSelectedFileViewers();
        }
    }

    partial void OnFollowRenamesChanged(bool value) => ApplySetting(() => _settings.FollowRenames = value, reloadHistory: true);

    partial void OnFollowRenamesExactOnlyChanged(bool value) => ApplySetting(() => _settings.FollowRenamesExactOnly = value, reloadHistory: true);

    partial void OnFullHistoryChanged(bool value) => ApplySetting(() => _settings.FullHistory = value, reloadHistory: true);

    // As ToggleSimplifyMergesFlag: only reloaded if the full history is shown.
    partial void OnSimplifyMergesChanged(bool value) => ApplySetting(() => _settings.SimplifyMerges = value, reloadHistory: FullHistory);

    partial void OnLoadHistoryOnShowChanged(bool value) => ApplySetting(() => _settings.LoadHistoryOnShow = value);

    partial void OnLoadBlameOnShowChanged(bool value) => ApplySetting(() => _settings.LoadBlameOnShow = value);

    partial void OnIgnoreWhitespaceOnBlameChanged(bool value) => ApplySetting(() => _settings.IgnoreWhitespaceOnBlame = value, reloadBlame: true);

    partial void OnDetectCopyInFileOnBlameChanged(bool value) => ApplySetting(() => _settings.DetectCopyInFileOnBlame = value, reloadBlame: true);

    partial void OnDetectCopyInAllOnBlameChanged(bool value) => ApplySetting(() => _settings.DetectCopyInAllOnBlame = value, reloadBlame: true);

    partial void OnBlameDisplayAuthorFirstChanged(bool value) => ApplySetting(() => _settings.BlameDisplayAuthorFirst = value, reloadBlame: true);

    partial void OnBlameShowAuthorAvatarChanged(bool value) => ApplySetting(() => _settings.BlameShowAuthorAvatar = value, reloadBlame: true);

    partial void OnBlameShowAuthorChanged(bool value)
    {
        // As showAuthorToolStripMenuItem_Click: the author or its date is shown.
        if (!_initializing && !value)
        {
            BlameShowAuthorDate = true;
        }

        ApplySetting(() => _settings.BlameShowAuthor = value, reloadBlame: true);
    }

    partial void OnBlameShowAuthorDateChanged(bool value)
    {
        if (!_initializing && !value)
        {
            BlameShowAuthor = true;
        }

        ApplySetting(() => _settings.BlameShowAuthorDate = value, reloadBlame: true);
    }

    partial void OnBlameShowAuthorTimeChanged(bool value) => ApplySetting(() => _settings.BlameShowAuthorTime = value, reloadBlame: true);

    partial void OnBlameShowLineNumbersChanged(bool value) => ApplySetting(() => _settings.BlameShowLineNumbers = value, reloadBlame: true);

    partial void OnBlameShowOriginalFilePathChanged(bool value) => ApplySetting(() => _settings.BlameShowOriginalFilePath = value, reloadBlame: true);

    private void ApplySetting(Action apply, bool reloadHistory = false, bool reloadBlame = false)
    {
        if (_initializing)
        {
            return;
        }

        apply();
        if (reloadHistory)
        {
            LoadFileHistory();
        }
        else if (reloadBlame)
        {
            UpdateSelectedFileViewers(force: true);
        }
    }

    private string GetTitle(string? alternativeFileName)
    {
        // As SetTitle (whose "File History" is not translated either).
        string title = "File History - " + FileName;
        if (!string.IsNullOrEmpty(alternativeFileName) && alternativeFileName != FileName)
        {
            title += $" ({alternativeFileName})";
        }

        return title + " - " + _host.WorkingDirectory;
    }

    private string GetFileNameForRevision(GitRevision revision) => _host.GetFileName(revision) ?? FileName;

    /// <summary>As <c>UpdateSelectedFileViewers</c>: the tabs of the selected revision, and the content of the selected tab.</summary>
    private void UpdateSelectedFileViewers(bool force = false)
    {
        IReadOnlyList<GitRevision> selectedRevisions = Grid.GetSelectedRevisionsLatestSelectedFirst();
        if (selectedRevisions.Count == 0)
        {
            return;
        }

        GitRevision revision = selectedRevisions[0];
        string fileName = GetFileNameForRevision(revision);
        bool isFolder = fileName.EndsWith('/');
        bool fileAvailable = !isFolder && _host.IsFileAvailable(fileName, revision);

        Title = GetTitle(alternativeFileName: fileName);
        CommitTabHeader = Strings.CommitTab.PlainText + (isFolder || fileAvailable ? "" : string.Format(Strings.FileNotFound.Text, $"\"{fileName}\""));

        FileHistoryTab? preferredTab = null;
        IsCommitTabVisible = !revision.IsArtificial;
        if (revision.IsArtificial)
        {
            preferredTab = FileHistoryTab.Diff;
        }

        // Artificial commits of a folder have no tab at all.
        IsDiffTabVisible = fileAvailable;
        if (!fileAvailable)
        {
            preferredTab = FileHistoryTab.Commit;
        }

        bool fileTabsVisible = !revision.IsArtificial && fileAvailable;
        IsViewTabVisible = fileTabsVisible;
        IsBlameTabVisible = fileTabsVisible && !_host.IsSubmodule;

        if (!IsTabVisible(SelectedTab) && preferredTab is { } tab)
        {
            _initializing = true;
            SelectedTab = tab;
            _initializing = false;
        }

        GitItemStatus file = new(name: fileName) { IsTracked = true };
        switch (SelectedTab)
        {
            case FileHistoryTab.Blame:
                _ = Blame.LoadAsync(revision, children: null, fileName, force: force);
                break;

            case FileHistoryTab.View:
                _ = View.ShowFileAsync(file, revision.ObjectId);
                break;

            case FileHistoryTab.Diff:
                FileStatusEntry entry = new(
                    FirstRevision: selectedRevisions.Count > 1 ? selectedRevisions[^1] : null,
                    SecondRevision: revision,
                    file);
                _ = Diff.ShowChangesAsync(entry, _host.NoChangesText);
                break;

            case FileHistoryTab.Commit:
                _ = CommitDiff.SetRevisionAsync(revision.ObjectId, fileName);
                break;
        }
    }

    private bool IsTabVisible(FileHistoryTab tab) => tab switch
    {
        FileHistoryTab.Commit => IsCommitTabVisible,
        FileHistoryTab.Diff => IsDiffTabVisible,
        FileHistoryTab.View => IsViewTabVisible,
        _ => IsBlameTabVisible,
    };

    /// <summary>As <c>FileHistoryContextMenuOpening</c>.</summary>
    public FileHistoryMenuState GetMenuState()
    {
        IReadOnlyList<GitRevision> selected = Grid.GetSelectedRevisionsLatestSelectedFirst();
        return new FileHistoryMenuState(
            CanDiffToLocal: selected.Count == 1 && selected[0].ObjectId != ObjectId.WorkTreeId && _host.IsInWorkingDirectory(FileName),
            CanOpenWithDifftool: selected.Count is >= 1 and <= 2,
            CanManipulateCommit: selected.Count == 1 && !selected[0].IsArtificial,
            CanSaveAs: selected.Count == 1,
            CanCopy: selected.Count >= 1 && !selected[0].IsArtificial);
    }

    /// <summary>The copy menu of the selected revisions.</summary>
    public IReadOnlyList<RevisionCopyItem> GetCopyItems() => _host.GetCopyItems(Grid.GetSelectedRevisionsLatestSelectedFirst());

    public void Copy(RevisionCopyItem item) => _host.CopyToClipboard(item.Text);

    /// <summary>
    ///  The difftools configured in git, for the submenus of the difftool items (<c>LoadCustomDifftools</c>); none without
    ///  several.
    /// </summary>
    public IReadOnlyList<string> CustomDiffTools { get; set; } = [];

    /// <summary>As <c>OpenFilesWithDiffTool</c>.</summary>
    [RelayCommand]
    private void OpenWithDifftool(bool toLocal) => OpenWithCustomDifftool(toLocal, customTool: null);

    /// <summary>As <c>OpenFilesWithDiffTool</c> with the difftool of the submenu.</summary>
    public void OpenWithCustomDifftool(bool toLocal, string? customTool)
    {
        IReadOnlyList<GitRevision> selected = Grid.GetSelectedRevisionsLatestSelectedFirst();
        string? revisionFileName = selected.Count != 0 ? _host.GetFileName(selected[0]) : null;
        _host.OpenWithDifftool(selected, FileName, revisionFileName, toLocal, customTool);
    }

    [RelayCommand]
    private void SaveAs()
    {
        if (Grid.GetSelectedRevisionsLatestSelectedFirst() is [{ } revision, ..])
        {
            _host.SaveAs(revision, GetFileNameForRevision(revision));
        }
    }

    [RelayCommand]
    private void CherryPick()
    {
        if (Grid.GetSelectedRevisionsLatestSelectedFirst() is [{ } revision])
        {
            _host.CherryPick(revision);
        }
    }

    [RelayCommand]
    private void Revert()
    {
        if (Grid.GetSelectedRevisionsLatestSelectedFirst() is [{ } revision])
        {
            _host.Revert(revision);
        }
    }

    /// <summary>As <c>Blame_CommandClick</c>: the links of the blame's commit info select their commit.</summary>
    private void OnBlameCommand(string command, string? data)
    {
        if (data is null || command is not ("gotocommit" or "gotobranch" or "gototag"))
        {
            return;
        }

        if (_host.ResolveCommit(data, isRef: command != "gotocommit") is { } objectId && !Grid.SelectRevision(objectId))
        {
            _host.ShowRevisionFiltered(objectId);
        }
    }

    /// <summary>The grid of the blame: blaming a revision selects it (<c>IRevisionGridFileUpdate.SelectFileInRevision</c>).</summary>
    private sealed class BlameGrid(FileHistoryViewModel owner) : IBlameRevisionGrid
    {
        public GitRevision? GetRevision(ObjectId objectId) => owner.Grid.GetRevision(objectId);

        public GitRevision GetActualRevision(GitRevision revision) => owner._host.GetActualRevision(revision);

        public GitRevision? GetActualRevision(ObjectId objectId)
            => GetRevision(objectId) is { } revision ? GetActualRevision(revision) : owner._host.GetRevision(objectId);

        public bool SelectFileInRevision(ObjectId objectId, string fileName) => owner.Grid.SelectRevision(objectId);
    }
}
