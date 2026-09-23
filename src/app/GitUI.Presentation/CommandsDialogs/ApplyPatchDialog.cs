using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands.Git;
using GitExtensions.Extensibility;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the apply patch dialog; ids match <c>FormApplyPatch</c>.</summary>
public sealed class ApplyPatchStrings : ViewStrings
{
    public ApplyPatchStrings()
        : base("FormApplyPatch")
    {
        Title = Add("_applyPatchMsgBox", "Text", "Apply patch");
        ConflictResolved = Add("_conflictResolvedText", "Text", "Conflicts resolved");
        ConflictMergetool = Add("_conflictMergetoolText", "Text", "&Solve conflicts");
        SelectPatchFileFilter = Add("_selectPatchFileFilter", "Text", "Patch file (*.Patch)");
        SelectPatchFileCaption = Add("_selectPatchFileCaption", "Text", "Select patch file");
        NoFileSelected = Add("_noFileSelectedText", "Text", "Please select a patch to apply");
        Abort = Add("Abort", "Text", "A&bort patch");
        AddFiles = Add("AddFiles", "Text", "&Add files");
        Apply = Add("Apply", "Text", "Apply patch");
        BrowseDir = Add("BrowseDir", "Text", "Bro&wse");
        BrowsePatch = Add("BrowsePatch", "Text", "B&rowse");
        IgnoreWhitespace = Add("IgnoreWhitespace", "Text", "&Ignore Wh.spc.");
        PatchDirMode = Add("PatchDirMode", "Text", "Patch &directory");
        PatchFileMode = Add("PatchFileMode", "Text", "Patch &file");
        SignOff = Add("SignOff", "Text", "Sign-&Off");
        Skip = Add("Skip", "Text", "S&kip patch");
        SolveMergeConflicts = Add("SolveMergeConflicts", "Text", "There are unresolved merge conflicts\r\n");
    }

    public TranslatedText Title { get; }

    public TranslatedText ConflictResolved { get; }

    public TranslatedText ConflictMergetool { get; }

    public TranslatedText SelectPatchFileFilter { get; }

    public TranslatedText SelectPatchFileCaption { get; }

    public TranslatedText NoFileSelected { get; }

    public TranslatedText Abort { get; }

    public TranslatedText AddFiles { get; }

    public TranslatedText Apply { get; }

    public TranslatedText BrowseDir { get; }

    public TranslatedText BrowsePatch { get; }

    public TranslatedText IgnoreWhitespace { get; }

    public TranslatedText PatchDirMode { get; }

    public TranslatedText PatchFileMode { get; }

    public TranslatedText SignOff { get; }

    public TranslatedText Skip { get; }

    public TranslatedText SolveMergeConflicts { get; }
}

/// <summary>Operations of the apply patch dialog that need the host (git, the settings, the other dialogs).</summary>
public interface IApplyPatchHost
{
    bool InTheMiddleOfPatch();

    bool InTheMiddleOfConflictedMerge();

    bool InTheMiddleOfAction();

    /// <summary><c>GitModule.GetPathForGitExecution</c> (e.g. the WSL path of a file).</summary>
    string? GetPathForGitExecution(string? path);

    /// <summary>Runs git in the progress dialog (<c>FormProcess.ShowDialog</c>).</summary>
    void RunGit(ArgumentString arguments);

    /// <summary>Applies the patches of a directory (<c>GitModule.ApplyPatch</c>, which streams them to git).</summary>
    void ApplyPatchDirectory(string directory, ArgumentString arguments);

    /// <summary><c>RepoChangedNotifier.Notify</c>.</summary>
    void NotifyRepoChanged();

    /// <summary>Opens the merge conflicts dialog (<c>StartResolveConflictsDialog</c>).</summary>
    void ResolveConflicts();

    /// <summary>Opens the add files dialog (<c>StartAddFilesDialog</c>).</summary>
    void AddFiles();

    /// <summary><c>AppSettings.ApplyPatchIgnoreWhitespace</c> and <c>ApplyPatchSignOff</c>.</summary>
    (bool IgnoreWhitespace, bool SignOff) LoadSettings();

    void SaveIgnoreWhitespace(bool value);

    void SaveSignOff(bool value);
}

/// <summary>Which button Enter triggers (<c>AcceptButton</c> of <c>FormApplyPatch</c>).</summary>
public enum ApplyPatchDefaultButton
{
    Apply,
    Mergetool,
    Resolved,
}

/// <summary>View model of the apply patch dialog (port of <c>FormApplyPatch</c>).</summary>
public sealed partial class ApplyPatchViewModel : DialogViewModel
{
    private readonly IApplyPatchHost _host;
    private readonly IMessageBoxService _messageBoxes;
    private readonly IFileDialogService _fileDialogs;
    private readonly string _errorCaption;
    private bool _isInitialized;

    /// <param name="workingDirectory">The repository, shown in the title.</param>
    /// <param name="patchFile">The patch file to apply (<c>SetPatchFile</c>), if any.</param>
    /// <param name="patchDirectory">The directory of the patches to apply (<c>SetPatchDir</c>), if any; selects the directory mode.</param>
    /// <param name="errorCaption">The caption of the error messages (<c>TranslatedStrings.Error</c>).</param>
    public ApplyPatchViewModel(
        ApplyPatchStrings strings,
        PatchGridViewModel patchGrid,
        IApplyPatchHost host,
        IMessageBoxService messageBoxes,
        IFileDialogService fileDialogs,
        string workingDirectory,
        string? patchFile,
        string? patchDirectory,
        string errorCaption)
    {
        Strings = strings;
        PatchGrid = patchGrid;
        _messageBoxes = messageBoxes;
        _fileDialogs = fileDialogs;
        _errorCaption = errorCaption;
        Title = $"{strings.Title.PlainText} ({workingDirectory})";
        MergetoolText = strings.ConflictMergetool.AccessKeyText;
        ResolvedText = strings.ConflictResolved.AccessKeyText;

        if (patchDirectory is not null)
        {
            IsPatchFileMode = false;
            PatchDirectory = patchDirectory;
        }
        else
        {
            PatchFile = patchFile ?? "";
        }

        // As MergePatch_Load.
        (IgnoreWhitespace, SignOff) = host.LoadSettings();

        // Assigned last, so that loading the settings does not save them again.
        _host = host;
    }

    public ApplyPatchStrings Strings { get; }

    public PatchGridViewModel PatchGrid { get; }

    /// <summary>As <c>MergePatch_Load</c>: "Apply patch (&lt;working directory&gt;)".</summary>
    public string Title { get; }

    /// <summary>The text of the "There are unresolved merge conflicts" button, without its trailing line break.</summary>
    public string SolveMergeConflictsText => Strings.SolveMergeConflicts.Text.Trim();

    /// <summary>Whether a patch file is applied (<c>PatchFileMode</c>), otherwise a directory of patches (<c>PatchDirMode</c>).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPatchDirectoryMode), nameof(IsPatchFileEnabled), nameof(IsPatchDirectoryEnabled))]
    public partial bool IsPatchFileMode { get; set; } = true;

    public bool IsPatchDirectoryMode
    {
        get => !IsPatchFileMode;
        set => IsPatchFileMode = !value;
    }

    [ObservableProperty]
    public partial string PatchFile { get; set; } = "";

    [ObservableProperty]
    public partial string PatchDirectory { get; set; } = "";

    [ObservableProperty]
    public partial bool IgnoreWhitespace { get; set; }

    [ObservableProperty]
    public partial bool SignOff { get; set; }

    /// <summary>Whether patches are being applied, as last checked (<c>EnableButtons</c>).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanApply), nameof(CanContinue), nameof(IsPatchFileEnabled), nameof(IsPatchDirectoryEnabled), nameof(CanResolve), nameof(CanRunMergetool))]
    public partial bool IsInPatch { get; private set; }

    /// <summary>Whether there are merge conflicts, as last checked (<c>EnableButtons</c>).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanResolve), nameof(CanRunMergetool))]
    public partial bool HasConflicts { get; private set; }

    /// <summary>The apply button, the options and the modes, before applying patches.</summary>
    public bool CanApply => !IsInPatch;

    public bool IsPatchFileEnabled => !IsInPatch && IsPatchFileMode;

    public bool IsPatchDirectoryEnabled => !IsInPatch && !IsPatchFileMode;

    /// <summary>The add files, skip and abort buttons, while applying patches.</summary>
    public bool CanContinue => IsInPatch;

    public bool CanResolve => IsInPatch && !HasConflicts;

    public bool CanRunMergetool => IsInPatch && HasConflicts;

    /// <summary>The text of the solve conflicts button, between &gt; &lt; when it is the default button.</summary>
    [ObservableProperty]
    public partial string MergetoolText { get; private set; }

    /// <summary>The text of the conflicts resolved button, between &gt; &lt; when it is the default button.</summary>
    [ObservableProperty]
    public partial string ResolvedText { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsApplyDefault), nameof(IsMergetoolDefault), nameof(IsResolvedDefault))]
    public partial ApplyPatchDefaultButton DefaultButton { get; private set; }

    public bool IsApplyDefault => DefaultButton == ApplyPatchDefaultButton.Apply;

    public bool IsMergetoolDefault => DefaultButton == ApplyPatchDefaultButton.Mergetool;

    public bool IsResolvedDefault => DefaultButton == ApplyPatchDefaultButton.Resolved;

    /// <summary>Raised when the default button should get the focus (<c>EnableButtons</c> focuses it while applying patches).</summary>
    public event EventHandler? FocusDefaultButtonRequested;

    /// <summary>
    ///  As <c>PatchGrid.OnRuntimeLoad</c> and <c>FormApplyPatchLoad</c> (which the WinForms form does not subscribe to, so its
    ///  buttons are all enabled until the mode changes or a button is used).
    /// </summary>
    public void InitializeView()
    {
        _isInitialized = true;
        PatchGrid.Initialize();
        EnableButtons();
    }

    /// <summary>As <c>BrowsePatch_Click</c> and <c>SelectPatchFile</c>: the current file is kept if cancelled.</summary>
    [RelayCommand]
    private async Task BrowsePatchAsync()
    {
        string? fileName = await _fileDialogs.PickFileAsync(Strings.SelectPatchFileCaption.Text, Strings.SelectPatchFileFilter.Text, "*.patch", @".");
        if (fileName is not null)
        {
            PatchFile = fileName;
        }
    }

    /// <summary>As <c>BrowseDir_Click</c>.</summary>
    [RelayCommand]
    private async Task BrowseDirectoryAsync()
    {
        string? userSelectedPath = await _fileDialogs.PickFolderAsync();
        if (userSelectedPath is not null)
        {
            PatchDirectory = userSelectedPath;
        }
    }

    /// <summary>As <c>Apply_Click</c>.</summary>
    [RelayCommand]
    private void Apply()
    {
        string patchFile = PatchFile;
        string dirText = PatchDirectory;
        bool ignoreWhiteSpace = IgnoreWhitespace;
        bool signOff = SignOff;

        if (string.IsNullOrEmpty(patchFile) && string.IsNullOrEmpty(dirText))
        {
            _messageBoxes.ShowError(Strings.NoFileSelected.Text, _errorCaption);
            return;
        }

        PatchGrid.Skipped.Clear();

        if (IsPatchFileMode)
        {
            ArgumentString arguments = IsDiffFile(patchFile)
                ? Commands.ApplyDiffPatch(ignoreWhiteSpace, patchFile, _host.GetPathForGitExecution)
                : Commands.ApplyMailboxPatch(signOff, ignoreWhiteSpace, patchFile, _host.GetPathForGitExecution);

            _host.RunGit(arguments);
        }
        else
        {
            // No need for PathUtil.GetRepoPath(), file streamed
            ArgumentString arguments = Commands.ApplyMailboxPatch(signOff, ignoreWhiteSpace);

            _host.ApplyPatchDirectory(dirText, arguments);
        }

        _host.NotifyRepoChanged();

        EnableButtons();

        if (!_host.InTheMiddleOfAction() && !_host.InTheMiddleOfPatch())
        {
            Close(true);
        }
    }

    /// <summary>As <c>Mergetool_Click</c> and <c>SolveMergeConflicts_Click</c>.</summary>
    [RelayCommand]
    private void Mergetool()
    {
        _host.ResolveConflicts();
        EnableButtons();
    }

    /// <summary>As <c>Skip_Click</c>.</summary>
    [RelayCommand]
    private void Skip()
    {
        PatchItem? applyingPatch = PatchGrid.PatchFiles?.FirstOrDefault(p => p.IsNext);
        if (applyingPatch is not null)
        {
            applyingPatch.IsSkipped = true;
            PatchGrid.Skipped.Add(applyingPatch);
        }

        _host.RunGit(Commands.Skip());
        EnableButtons();
    }

    /// <summary>As <c>Resolved_Click</c>.</summary>
    [RelayCommand]
    private void Resolved()
    {
        _host.RunGit(Commands.Resolved());
        EnableButtons();
    }

    /// <summary>As <c>Abort_Click</c>.</summary>
    [RelayCommand]
    private void Abort()
    {
        _host.RunGit(Commands.Abort());
        PatchGrid.Skipped.Clear();
        EnableButtons();
    }

    /// <summary>As <c>AddFiles_Click</c>.</summary>
    [RelayCommand]
    private void AddFiles() => _host.AddFiles();

    /// <summary>As <c>PatchFileMode_CheckedChanged</c>.</summary>
    partial void OnIsPatchFileModeChanged(bool value)
    {
        if (_isInitialized)
        {
            EnableButtons();
        }
    }

    /// <summary>As <c>IgnoreWhitespace_CheckedChanged</c>.</summary>
    partial void OnIgnoreWhitespaceChanged(bool value) => _host?.SaveIgnoreWhitespace(value);

    /// <summary>As <c>SignOff_CheckedChanged</c>.</summary>
    partial void OnSignOffChanged(bool value) => _host?.SaveSignOff(value);

    /// <summary>
    ///  As <c>IsDiffFile</c>: looks into the patch file to tell a raw diff (i.e. from <c>git diff -p</c>) from the mailbox format;
    ///  only looks at the start and returns <see langword="false"/> on any problem, never throws.
    /// </summary>
    internal static bool IsDiffFile(string path)
    {
        try
        {
            using StreamReader sr = new(path);
            string? line = sr.ReadLine();

            return line is not null && (line.StartsWith("diff ") || line.StartsWith("Index: "));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>As <c>EnableButtons</c>.</summary>
    private void EnableButtons()
    {
        bool conflictedMerge = _host.InTheMiddleOfConflictedMerge();
        IsInPatch = _host.InTheMiddleOfPatch();
        HasConflicts = conflictedMerge;

        if (PatchGrid.PatchFiles is null || PatchGrid.PatchFiles.Count == 0)
        {
            PatchGrid.Initialize();
        }
        else
        {
            PatchGrid.RefreshGrid();
        }

        ResolvedText = Strings.ConflictResolved.AccessKeyText;
        MergetoolText = Strings.ConflictMergetool.AccessKeyText;

        if (conflictedMerge)
        {
            MergetoolText = TranslatedText.ToAccessKeyText(">" + Strings.ConflictMergetool.Text + "<");
            DefaultButton = ApplyPatchDefaultButton.Mergetool;
            FocusDefaultButtonRequested?.Invoke(this, EventArgs.Empty);
        }
        else if (IsInPatch)
        {
            ResolvedText = TranslatedText.ToAccessKeyText(">" + Strings.ConflictResolved.Text + "<");
            DefaultButton = ApplyPatchDefaultButton.Resolved;
            FocusDefaultButtonRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
