using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands.Git;
using GitExtensions.Extensibility;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the rebase dialog; ids match <c>FormRebase</c>.</summary>
public sealed class RebaseStrings : ViewStrings
{
    public RebaseStrings()
        : base("FormRebase")
    {
        Title = Add("$this", "Text", "Rebase");
        ContinueRebase = Add("_continueRebaseText", "Text", "&Continue rebase");
        SolveConflicts = Add("_solveConflictsText", "Text", "&Solve conflicts");
        SolveConflictsDefault = Add("_solveConflictsText2", "Text", ">&Solve conflicts<");
        ContinueRebaseDefault = Add("_continueRebaseText2", "Text", ">&Continue rebase<");
        NoBranchSelected = Add("_noBranchSelectedText", "Text", "Please select a branch");
        BranchUpToDate = Add("_branchUpToDateText", "Text", "Current branch a is up to date." + Environment.NewLine + "Nothing to rebase.");
        BranchUpToDateCaption = Add("_branchUpToDateCaption", "Text", "Rebase");
        HoverShowImage = Add("_hoverShowImageLabelText", "Text", "Hover to see scenario when fast forward is possible.");
        Abort = Add("btnAbort", "Text", "A&bort");
        AddFiles = Add("btnAddFiles", "Text", "&Add files");
        Commit = Add("btnCommit", "Text", "C&ommit...");
        EditTodo = Add("btnEditTodo", "Text", "&Edit todo...");
        Rebase = Add("btnRebase", "Text", "Rebase");
        Skip = Add("btnSkip", "Text", "S&kip currently applying commit");
        UnresolvedMergeConflicts = Add("btnSolveMergeconflicts", "Text", "There are unresolved merge conflicts\r\n");
        UpdateRefs = Add("checkBoxUpdateRefs", "Text", "Update dependent r&efs");
        Autosquash = Add("chkAutosquash", "Text", "Autos&quash");
        CommitterDateIsAuthorDate = Add("chkCommitterDateIsAuthorDate", "Text", "Co&mmitter date is author date");
        CommitterDateIsAuthorDateToolTip = Add("chkCommitterDateIsAuthorDate", "toolTip1", "Sets the commit date to the original author date\r\n(instead of the current date).");
        IgnoreDate = Add("chkIgnoreDate", "Text", "Ignore &date");
        IgnoreDateToolTip = Add("chkIgnoreDate", "toolTip1", "Sets the author date to the current date (same as\r\ncommit date), ignoring the original author date.");
        Interactive = Add("chkInteractive", "Text", "&Interactive Rebase");
        PreserveMerges = Add("chkPreserveMerges", "Text", "&Preserve Merges");
        SpecificRange = Add("chkSpecificRange", "Text", "Specific ra&nge");
        AutoStash = Add("chkStash", "Text", "A&uto stash");
        RebaseOn = Add("label2", "Text", "&Rebase on");
        CommitsToReapply = Add("lblCommitsToReapply", "Text", "Commits to re-apply:");
        CurrentBranch = Add("lblCurrent", "Text", "Current branch:");
        RangeFrom = Add("lblRangeFrom", "Text", "&From (exc.)");
        RangeTo = Add("lblRangeTo", "Text", "&To");
        RebaseDescription = Add("lblRebase", "Text", "Rebase current branch on top of another branch");
        ShowOptions = Add("llblShowOptions", "Text", "Show options");
    }

    public TranslatedText Title { get; }

    public TranslatedText ContinueRebase { get; }

    public TranslatedText SolveConflicts { get; }

    public TranslatedText SolveConflictsDefault { get; }

    public TranslatedText ContinueRebaseDefault { get; }

    public TranslatedText NoBranchSelected { get; }

    public TranslatedText BranchUpToDate { get; }

    public TranslatedText BranchUpToDateCaption { get; }

    public TranslatedText HoverShowImage { get; }

    public TranslatedText Abort { get; }

    public TranslatedText AddFiles { get; }

    public TranslatedText Commit { get; }

    public TranslatedText EditTodo { get; }

    public TranslatedText Rebase { get; }

    public TranslatedText Skip { get; }

    public TranslatedText UnresolvedMergeConflicts { get; }

    public TranslatedText UpdateRefs { get; }

    public TranslatedText Autosquash { get; }

    public TranslatedText CommitterDateIsAuthorDate { get; }

    public TranslatedText CommitterDateIsAuthorDateToolTip { get; }

    public TranslatedText IgnoreDate { get; }

    public TranslatedText IgnoreDateToolTip { get; }

    public TranslatedText Interactive { get; }

    public TranslatedText PreserveMerges { get; }

    public TranslatedText SpecificRange { get; }

    public TranslatedText AutoStash { get; }

    public TranslatedText RebaseOn { get; }

    public TranslatedText CommitsToReapply { get; }

    public TranslatedText CurrentBranch { get; }

    public TranslatedText RangeFrom { get; }

    public TranslatedText RangeTo { get; }

    public TranslatedText RebaseDescription { get; }

    public TranslatedText ShowOptions { get; }
}

/// <summary>A ref offered to rebase on: a branch, a remote branch or a tag (<see cref="IsHead"/> for local branches).</summary>
public sealed record RebaseRef(string Name, bool IsHead);

/// <summary>How the rebase dialog was opened (the arguments of the <c>FormRebase</c> constructors).</summary>
/// <param name="From">The first commit of a specific range (excluded), if any.</param>
/// <param name="To">The branch to rebase for a specific range; the current branch otherwise.</param>
/// <param name="DefaultBranch">The branch to rebase on.</param>
/// <param name="StartRebaseImmediately">Whether the rebase starts when the dialog opens (the refs are then not listed).</param>
/// <param name="SupportUpdateRefs">Whether git supports <c>--update-refs</c> (<c>GitVersion.SupportUpdateRefs</c>).</param>
/// <param name="AlwaysShowAdvancedOptions"><c>AppSettings.AlwaysShowAdvOpt</c>.</param>
public sealed record RebaseDialogOptions(
    string? From,
    string? To,
    string? DefaultBranch,
    bool Interactive,
    bool StartRebaseImmediately,
    bool SupportUpdateRefs,
    bool AlwaysShowAdvancedOptions);

/// <summary>Operations of the rebase dialog that need the host (git, the settings, the other dialogs).</summary>
public interface IRebaseHost
{
    string GetSelectedBranch();

    /// <summary>The branches, remote branches and tags (but not stashes, notes etc.).</summary>
    IReadOnlyList<RebaseRef> GetRefs();

    bool InTheMiddleOfRebase();

    bool InTheMiddleOfConflictedMerge();

    bool InTheMiddleOfAction();

    bool InTheMiddleOfPatch();

    bool IsDirtyDir();

    /// <summary>The effective value of a boolean git setting, e.g. <c>rebase.autosquash</c>.</summary>
    bool? GetEffectiveBoolSetting(string name);

    /// <summary><c>AppSettings.RebaseAutoStash</c>.</summary>
    bool RebaseAutoStash { get; set; }

    /// <summary>Runs git in the progress dialog (<c>FormProcess.ShowDialog</c>) and returns its output.</summary>
    string RunGit(ArgumentString arguments);

    /// <summary>Runs git in the progress dialog and returns its output (<c>FormProcess.ReadDialog</c>).</summary>
    string ReadGit(ArgumentString arguments);

    /// <summary><c>GitModule.CanContinueAction</c>: whether git stopped where continuing is all there is to do.</summary>
    bool CanContinueAction(string output);

    /// <summary>Opens the merge conflicts dialog (<c>StartResolveConflictsDialog</c>).</summary>
    void ResolveConflicts();

    /// <summary>Opens the add files dialog (<c>StartAddFilesDialog</c>).</summary>
    void AddFiles();

    /// <summary>Opens the commit dialog (<c>StartCommitDialog</c>).</summary>
    void Commit();

    /// <summary>
    ///  As <c>btnChooseFromRevision_Click</c>: lets the user choose the first commit of the range (excluded) among the commits
    ///  of the current branch since its merge base with <paramref name="onto"/>; returns its short id, or <see langword="null"/>
    ///  if cancelled.
    /// </summary>
    string? ChooseFromRevision(string from, string onto);
}

/// <summary>Which button Enter triggers (<c>AcceptButton</c> of <c>FormRebase</c>).</summary>
public enum RebaseDefaultButton
{
    Rebase,
    SolveConflicts,
    Continue,
}

/// <summary>View model of the rebase dialog (port of <c>FormRebase</c>).</summary>
public sealed partial class RebaseViewModel : DialogViewModel
{
    private readonly IRebaseHost _host;
    private readonly IMessageBoxService _messageBoxes;
    private readonly RebaseDialogOptions _options;
    private readonly string _errorCaption;
    private bool _isClosed;

    public RebaseViewModel(
        RebaseStrings strings,
        PatchGridViewModel patchGrid,
        HelpImageViewModel helpImage,
        IRebaseHost host,
        IMessageBoxService messageBoxes,
        RebaseDialogOptions options,
        string errorCaption)
    {
        Strings = strings;
        PatchGrid = patchGrid;
        HelpImage = helpImage;
        HelpImage.HoverNotice = strings.HoverShowImage.Text;
        _host = host;
        _messageBoxes = messageBoxes;
        _options = options;
        _errorCaption = errorCaption;
        IsUpdateRefsVisible = options.SupportUpdateRefs;
        ContinueText = strings.ContinueRebase.AccessKeyText;
        SolveConflictsText = strings.SolveConflicts.AccessKeyText;

        if (options.AlwaysShowAdvancedOptions)
        {
            ShowOptions();
        }

        // As the second FormRebase constructor.
        From = options.From ?? "";
        IsInteractive = options.Interactive;
        IsSpecificRange = !string.IsNullOrEmpty(options.From);
    }

    public RebaseStrings Strings { get; }

    public PatchGridViewModel PatchGrid { get; }

    public HelpImageViewModel HelpImage { get; }

    /// <summary>The text of the "There are unresolved merge conflicts" button, without its trailing line break.</summary>
    public string UnresolvedMergeConflictsText => Strings.UnresolvedMergeConflicts.Text.Trim();

    [ObservableProperty]
    public partial string CurrentBranch { get; private set; } = "";

    /// <summary>The refs to rebase on (<c>cboBranches</c>); none if the rebase starts immediately.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<string> Branches { get; private set; } = [];

    /// <summary>The ref to rebase on, as entered.</summary>
    [ObservableProperty]
    public partial string Branch { get; set; } = "";

    /// <summary>The local branches, for the end of a specific range (<c>cboTo</c>).</summary>
    [ObservableProperty]
    public partial IReadOnlyList<string> ToBranches { get; private set; } = [];

    [ObservableProperty]
    public partial string To { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAutosquash))]
    public partial bool IsInteractive { get; set; }

    [ObservableProperty]
    public partial bool PreserveMerges { get; set; }

    [ObservableProperty]
    public partial bool Autosquash { get; set; }

    [ObservableProperty]
    public partial bool AutoStash { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanInteractive), nameof(CanAutosquash), nameof(CanCommitterDateIsAuthorDate))]
    public partial bool IgnoreDate { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanInteractive), nameof(CanAutosquash), nameof(CanIgnoreDate))]
    public partial bool CommitterDateIsAuthorDate { get; set; }

    [ObservableProperty]
    public partial bool UpdateRefs { get; set; }

    /// <summary>Whether a range is rebased: <see cref="From"/> (excluded) to <see cref="To"/> (<c>chkSpecificRange</c>).</summary>
    [ObservableProperty]
    public partial bool IsSpecificRange { get; set; }

    [ObservableProperty]
    public partial string From { get; set; }

    /// <summary>As <c>ToggleDateCheckboxMutualExclusions</c>: the interactive rebase and preserve merges exclude the date options.</summary>
    public bool CanInteractive => !IgnoreDate && !CommitterDateIsAuthorDate;

    /// <summary>As <c>chkInteractive_CheckedChanged</c> and <c>ToggleDateCheckboxMutualExclusions</c>.</summary>
    public bool CanAutosquash => IsInteractive && CanInteractive;

    public bool CanIgnoreDate => !CommitterDateIsAuthorDate;

    public bool CanCommitterDateIsAuthorDate => !IgnoreDate;

    /// <summary>Whether git supports <c>--update-refs</c>.</summary>
    public bool IsUpdateRefsVisible { get; }

    /// <summary>Whether the "Show options" link is shown (<c>ShowOptions_LinkClicked</c> hides it).</summary>
    [ObservableProperty]
    public partial bool IsShowOptionsVisible { get; private set; } = true;

    /// <summary>The ref to rebase on and the "Show options" link, hidden if the dialog opened during a rebase (<c>rebasePanel</c>).</summary>
    [ObservableProperty]
    public partial bool IsRebasePanelVisible { get; private set; } = true;

    /// <summary>Whether a rebase is in progress, as last checked (<c>EnableButtons</c>).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRebaseVisible), nameof(IsContinueVisible), nameof(IsSolveConflictsVisible))]
    public partial bool IsRebasing { get; private set; }

    /// <summary>Whether there are merge conflicts, as last checked (<c>EnableButtons</c>).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsContinueVisible), nameof(IsSolveConflictsVisible))]
    public partial bool HasConflicts { get; private set; }

    /// <summary><c>chkStash.Enabled</c>: before a rebase and with changes only.</summary>
    [ObservableProperty]
    public partial bool CanAutoStash { get; private set; }

    public bool IsRebaseVisible => !IsRebasing;

    public bool IsContinueVisible => IsRebasing && !HasConflicts;

    public bool IsSolveConflictsVisible => IsRebasing && HasConflicts;

    /// <summary>The text of the continue button, between &gt; &lt; when it is the default button.</summary>
    [ObservableProperty]
    public partial string ContinueText { get; private set; }

    /// <summary>The text of the solve conflicts button, between &gt; &lt; when it is the default button.</summary>
    [ObservableProperty]
    public partial string SolveConflictsText { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRebaseDefault), nameof(IsSolveConflictsDefault), nameof(IsContinueDefault))]
    public partial RebaseDefaultButton DefaultButton { get; private set; }

    public bool IsRebaseDefault => DefaultButton == RebaseDefaultButton.Rebase;

    public bool IsSolveConflictsDefault => DefaultButton == RebaseDefaultButton.SolveConflicts;

    public bool IsContinueDefault => DefaultButton == RebaseDefaultButton.Continue;

    /// <summary>Raised when the default button should get the focus (<c>EnableButtons</c> focuses it during a rebase).</summary>
    public event EventHandler? FocusDefaultButtonRequested;

    /// <summary>As <c>OnShown</c>: lists the refs and the state of the rebase, then starts the rebase if asked to.</summary>
    public void InitializeView()
    {
        // As PatchGrid.OnRuntimeLoad and SelectCurrentlyApplyingPatch.
        PatchGrid.Initialize();

        string selectedHead = _host.GetSelectedBranch();
        CurrentBranch = selectedHead;

        // Offer rebase on refs also for tags (but not stash, notes etc)
        IReadOnlyList<RebaseRef> refs = _options.StartRebaseImmediately ? [] : _host.GetRefs();
        Branches = [.. refs.Select(r => r.Name)];
        if (_options.DefaultBranch is not null)
        {
            Branch = _options.DefaultBranch;
        }

        ToBranches = [.. refs.Where(r => r.IsHead).Select(r => r.Name)];
        To = _options.To ?? selectedHead;

        IsRebasePanelVisible = !_host.InTheMiddleOfRebase();
        EnableButtons();

        // Honor the rebase.autosquash configuration.
        Autosquash = _host.GetEffectiveBoolSetting("rebase.autosquash") is true;
        if (_options.SupportUpdateRefs && _host.GetEffectiveBoolSetting("rebase.updaterefs") is true)
        {
            UpdateRefs = true;
        }

        AutoStash = _host.RebaseAutoStash;
        if (_options.StartRebaseImmediately)
        {
            Rebase();
        }
        else
        {
            ShowOptions();
        }
    }

    /// <summary>As <c>ShowOptions_LinkClicked</c>.</summary>
    [RelayCommand]
    private void ShowOptions() => IsShowOptionsVisible = false;

    /// <summary>As <c>OkClick</c>.</summary>
    [RelayCommand]
    private void Rebase()
    {
        if (string.IsNullOrEmpty(Branch))
        {
            _messageBoxes.ShowError(Strings.NoBranchSelected.Text, _errorCaption);
            return;
        }

        _host.RebaseAutoStash = AutoStash;

        PatchGrid.Skipped.Clear();

        bool? updateRefChoice = null;
        if (_options.SupportUpdateRefs && _host.GetEffectiveBoolSetting("rebase.updaterefs") != UpdateRefs)
        {
            updateRefChoice = UpdateRefs;
        }

        Commands.RebaseOptions rebaseOptions = new()
        {
            Interactive = IsInteractive,
            PreserveMerges = PreserveMerges,
            AutoSquash = Autosquash,
            AutoStash = AutoStash,
            IgnoreDate = IgnoreDate,
            CommitterDateIsAuthorDate = CommitterDateIsAuthorDate,
            UpdateRefs = updateRefChoice,
        };

        if (IsSpecificRange && !string.IsNullOrWhiteSpace(From) && !string.IsNullOrWhiteSpace(To))
        {
            // Rebase onto
            rebaseOptions.OnTo = Branch;
            rebaseOptions.From = From;
            rebaseOptions.BranchName = To;
        }
        else
        {
            rebaseOptions.BranchName = Branch;
        }

        string cmdOutput = _host.ReadGit(Commands.Rebase(rebaseOptions));
        if (cmdOutput.Trim() == "Current branch a is up to date.")
        {
            _messageBoxes.ShowInformation(Strings.BranchUpToDate.Text, Strings.BranchUpToDateCaption.Text);
        }

        if (!_host.InTheMiddleOfAction() && !_host.InTheMiddleOfPatch())
        {
            CloseDialog();
        }

        EnableButtons();
        PatchGrid.Initialize();
        ContinueIfPossible(cmdOutput);
    }

    /// <summary>As <c>ResolvedClick</c>.</summary>
    [RelayCommand]
    private void ContinueRebase()
    {
        string cmdOutput = _host.RunGit(Commands.ContinueRebase());

        if (!_host.InTheMiddleOfRebase())
        {
            CloseDialog();
        }

        EnableButtons();
        PatchGrid.Initialize();
        ContinueIfPossible(cmdOutput);
    }

    /// <summary>As <c>SkipClick</c>.</summary>
    [RelayCommand]
    private void Skip()
    {
        PatchItem? applyingPatch = PatchGrid.PatchFiles?.FirstOrDefault(p => p.IsNext);
        if (applyingPatch is not null)
        {
            applyingPatch.IsSkipped = true;
            PatchGrid.Skipped.Add(applyingPatch);
        }

        _host.RunGit(Commands.SkipRebase());

        if (!_host.InTheMiddleOfRebase())
        {
            CloseDialog();
        }

        EnableButtons();

        PatchGrid.RefreshGrid();
    }

    /// <summary>As <c>AbortClick</c>.</summary>
    [RelayCommand]
    private void Abort() => RunAndCloseWhenDone(Commands.AbortRebase());

    /// <summary>As <c>EditTodoClick</c>.</summary>
    [RelayCommand]
    private void EditTodo() => RunAndCloseWhenDone(Commands.EditTodoRebase());

    /// <summary>As <c>MergetoolClick</c> and <c>SolveMergeConflictsClick</c>.</summary>
    [RelayCommand]
    private void SolveConflicts()
    {
        _host.ResolveConflicts();
        EnableButtons();
    }

    /// <summary>As <c>AddFilesClick</c>.</summary>
    [RelayCommand]
    private void AddFiles() => _host.AddFiles();

    /// <summary>As <c>Commit_Click</c>.</summary>
    [RelayCommand]
    private void Commit()
    {
        _host.Commit();
        EnableButtons();
    }

    /// <summary>As <c>btnChooseFromRevision_Click</c>.</summary>
    [RelayCommand]
    private void ChooseFromRevision()
    {
        if (_host.ChooseFromRevision(From, Branch) is { } chosen)
        {
            From = chosen;
        }
    }

    /// <summary>As <c>AbortClick</c> and <c>EditTodoClick</c>.</summary>
    private void RunAndCloseWhenDone(ArgumentString arguments)
    {
        _host.RunGit(arguments);

        if (!_host.InTheMiddleOfRebase())
        {
            PatchGrid.Skipped.Clear();
            CloseDialog();
        }

        EnableButtons();
        PatchGrid.Initialize();
    }

    /// <summary>
    ///  As <c>BeginInvoke(btnContinueRebase.PerformClick)</c>: continues when git stopped only to be continued, if the continue
    ///  button is still there (<c>PerformClick</c> does nothing for a hidden button or a closed dialog).
    /// </summary>
    private void ContinueIfPossible(string cmdOutput)
    {
        if (!_isClosed && IsContinueVisible && _host.CanContinueAction(cmdOutput))
        {
            ContinueRebase();
        }
    }

    private void CloseDialog()
    {
        _isClosed = true;
        Close(true);
    }

    /// <summary>As <c>EnableButtons</c>.</summary>
    private void EnableButtons()
    {
        bool conflictedMerge = _host.InTheMiddleOfConflictedMerge();
        bool inTheMiddleOfRebase = _host.InTheMiddleOfRebase();
        IsRebasing = inTheMiddleOfRebase;
        HasConflicts = conflictedMerge;
        CanAutoStash = !inTheMiddleOfRebase && _host.IsDirtyDir();

        ContinueText = Strings.ContinueRebase.AccessKeyText;
        SolveConflictsText = Strings.SolveConflicts.AccessKeyText;

        if (conflictedMerge)
        {
            DefaultButton = RebaseDefaultButton.SolveConflicts;
            SolveConflictsText = Strings.SolveConflictsDefault.AccessKeyText;
            FocusDefaultButtonRequested?.Invoke(this, EventArgs.Empty);
        }
        else if (inTheMiddleOfRebase)
        {
            DefaultButton = RebaseDefaultButton.Continue;
            ContinueText = Strings.ContinueRebaseDefault.AccessKeyText;
            FocusDefaultButtonRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
