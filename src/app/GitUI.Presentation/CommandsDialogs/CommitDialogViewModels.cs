using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitUI.Presentation.Services;
using GitUI.Presentation.UserControls;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>
///  Base of the dialogs that act on a commit that can be a merge (cherry-pick, revert): shows the commit summary and,
///  for a merge, the parents to choose the mainline from.
/// </summary>
public abstract partial class MergeParentViewModel : DialogViewModel
{
    private readonly IMessageBoxService _messageBoxes;
    private readonly string _errorCaption;

    protected MergeParentViewModel(IParentCommitStrings parentStrings, CommitSummaryStrings summaryStrings, IMessageBoxService messageBoxes, string errorCaption)
    {
        ParentStrings = parentStrings;
        Summary = new CommitSummaryViewModel(summaryStrings);
        _messageBoxes = messageBoxes;
        _errorCaption = errorCaption;
    }

    public IParentCommitStrings ParentStrings { get; }

    public CommitSummaryViewModel Summary { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Parents), nameof(IsMerge))]
    public partial RevisionInfo? Revision { get; set; }

    public IReadOnlyList<ParentCommit> Parents => Revision?.Parents ?? [];

    public bool IsMerge => Revision?.IsMerge == true;

    [ObservableProperty]
    public partial ParentCommit? SelectedParent { get; set; }

    partial void OnRevisionChanged(RevisionInfo? value)
    {
        Summary.Summary = value?.Summary;

        // As the WinForms forms: the first parent is selected.
        SelectedParent = value?.IsMerge == true ? value.Parents[0] : null;
    }

    /// <summary>
    ///  The mainline parent number for a merge (1-based), 0 for other commits, or <see langword="null"/> (after reporting
    ///  it) if a merge has no parent selected.
    /// </summary>
    protected int? GetParentNumber()
    {
        if (!IsMerge)
        {
            return 0;
        }

        if (SelectedParent is null)
        {
            _messageBoxes.ShowError(ParentStrings.NoParentSelected.Text, _errorCaption);
            return null;
        }

        return SelectedParent.Number;
    }
}

/// <summary>The options of a cherry-pick, carried over between the dialogs when cherry-picking several commits.</summary>
public sealed record CherryPickOptions(bool AutoCommit, bool AddReference);

/// <summary>Operations of the cherry-pick dialog that need the host (git, commit selection, settings).</summary>
public interface ICherryPickHost
{
    /// <summary>Lets the user choose another commit; returns <see langword="null"/> if cancelled.</summary>
    RevisionInfo? ChooseRevision(string? currentGuid);

    /// <summary>Cherry-picks the commit (resolving conflicts if needed).</summary>
    /// <param name="parentNumber">The mainline parent of a merge (1-based), or 0.</param>
    void CherryPick(string guid, bool autoCommit, int parentNumber, bool addReference);

    /// <summary>Remembers the options as defaults.</summary>
    void SaveOptions(CherryPickOptions options);
}

/// <summary>View model of the cherry-pick dialog (port of <c>FormCherryPick</c>).</summary>
public sealed partial class CherryPickViewModel : MergeParentViewModel
{
    private readonly ICherryPickHost _host;

    public CherryPickViewModel(
        CherryPickStrings strings,
        CommitSummaryStrings summaryStrings,
        RevisionInfo? revision,
        CherryPickOptions options,
        ICherryPickHost host,
        IMessageBoxService messageBoxes,
        string errorCaption)
        : base(strings, summaryStrings, messageBoxes, errorCaption)
    {
        Strings = strings;
        _host = host;
        Revision = revision;
        AutoCommit = options.AutoCommit;
        AddReference = options.AddReference;
    }

    public CherryPickStrings Strings { get; }

    [ObservableProperty]
    public partial bool AutoCommit { get; set; }

    [ObservableProperty]
    public partial bool AddReference { get; set; }

    public CherryPickOptions Options => new(AutoCommit, AddReference);

    [RelayCommand]
    private void ChooseRevision()
    {
        RevisionInfo? revision = _host.ChooseRevision(Revision?.Guid);
        if (revision is not null)
        {
            Revision = revision;
        }
    }

    [RelayCommand]
    private void CherryPick()
    {
        int? parentNumber = GetParentNumber();
        if (parentNumber is null || Revision is null)
        {
            return;
        }

        _host.CherryPick(Revision.Guid, AutoCommit, parentNumber.Value, AddReference);
        _host.SaveOptions(Options);
        Close(accepted: true);
    }

    [RelayCommand]
    private void Abort() => Close(accepted: false);
}

/// <summary>Operations of the revert dialog that need the host (git).</summary>
public interface IRevertCommitHost
{
    /// <summary>Reverts the commit (resolving conflicts if needed).</summary>
    /// <param name="parentNumber">The mainline parent of a merge (1-based), or 0.</param>
    void Revert(string guid, bool autoCommit, int parentNumber);
}

/// <summary>View model of the revert dialog (port of <c>FormRevertCommit</c>).</summary>
public sealed partial class RevertCommitViewModel : MergeParentViewModel
{
    private readonly IRevertCommitHost _host;

    public RevertCommitViewModel(
        RevertCommitStrings strings,
        CommitSummaryStrings summaryStrings,
        RevisionInfo revision,
        IRevertCommitHost host,
        IMessageBoxService messageBoxes,
        string errorCaption)
        : base(strings, summaryStrings, messageBoxes, errorCaption)
    {
        Strings = strings;
        _host = host;
        Revision = revision;
    }

    public RevertCommitStrings Strings { get; }

    [ObservableProperty]
    public partial bool AutoCommit { get; set; }

    [RelayCommand]
    private void Revert()
    {
        int? parentNumber = GetParentNumber();
        if (parentNumber is null)
        {
            return;
        }

        _host.Revert(Revision!.Guid, AutoCommit, parentNumber.Value);
        Close(accepted: true);
    }

    [RelayCommand]
    private void Abort() => Close(accepted: false);
}

/// <summary>The kinds of <c>git reset</c> (mirrors <c>FormResetCurrentBranch.ResetType</c>).</summary>
public enum ResetKind
{
    Soft,
    Mixed,
    Keep,
    Merge,
    Hard,
}

/// <summary>Operations of the reset current branch dialog that need the host (git, documentation).</summary>
public interface IResetCurrentBranchHost
{
    void Reset(ResetKind kind);

    /// <summary>Opens the git documentation of the reset kind.</summary>
    void OpenHelp(ResetKind kind);
}

/// <summary>View model of the reset current branch dialog (port of <c>FormResetCurrentBranch</c>).</summary>
public sealed partial class ResetCurrentBranchViewModel : DialogViewModel
{
    private readonly IResetCurrentBranchHost _host;
    private readonly IMessageBoxService _messageBoxes;

    public ResetCurrentBranchViewModel(
        ResetCurrentBranchStrings strings,
        CommitSummaryStrings summaryStrings,
        string currentBranch,
        CommitSummary revision,
        ResetKind kind,
        IResetCurrentBranchHost host,
        IMessageBoxService messageBoxes)
    {
        Strings = strings;
        BranchInfo = string.Format(strings.BranchInfo.Text, currentBranch);
        Summary = new CommitSummaryViewModel(summaryStrings, revision);
        Kind = kind;
        _host = host;
        _messageBoxes = messageBoxes;
    }

    public ResetCurrentBranchStrings Strings { get; }

    public string BranchInfo { get; }

    public CommitSummaryViewModel Summary { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSoft), nameof(IsMixed), nameof(IsKeep), nameof(IsMerge), nameof(IsHard))]
    public partial ResetKind Kind { get; set; }

    public bool IsSoft
    {
        get => Kind == ResetKind.Soft;
        set => SetKind(value, ResetKind.Soft);
    }

    public bool IsMixed
    {
        get => Kind == ResetKind.Mixed;
        set => SetKind(value, ResetKind.Mixed);
    }

    public bool IsKeep
    {
        get => Kind == ResetKind.Keep;
        set => SetKind(value, ResetKind.Keep);
    }

    public bool IsMerge
    {
        get => Kind == ResetKind.Merge;
        set => SetKind(value, ResetKind.Merge);
    }

    public bool IsHard
    {
        get => Kind == ResetKind.Hard;
        set => SetKind(value, ResetKind.Hard);
    }

    private void SetKind(bool isChecked, ResetKind kind)
    {
        if (isChecked)
        {
            Kind = kind;
        }
    }

    [RelayCommand]
    private void Ok()
    {
        if (Kind == ResetKind.Hard && !_messageBoxes.Confirm(Strings.ResetHardWarning.Text, Strings.ResetCaption.Text))
        {
            return;
        }

        _host.Reset(Kind);
        Close(accepted: true);
    }

    [RelayCommand]
    private void Cancel() => Close(accepted: false);

    [RelayCommand]
    private void OpenHelp() => _host.OpenHelp(Kind);
}

/// <summary>Operations of the reset another branch dialog that need the host (git, settings).</summary>
public interface IResetAnotherBranchHost
{
    /// <summary>Whether the branch is an ancestor of the target commit, i.e. the reset is a fast forward.</summary>
    bool IsAncestor(string branch);

    /// <summary>Resets the branch to the target commit, then optionally checks it out; returns whether it succeeded.</summary>
    bool Reset(string branch, bool checkout);

    void SaveCheckoutAfterReset(bool value);
}

/// <summary>View model of the reset another branch dialog (port of <c>FormResetAnotherBranch</c>).</summary>
public sealed partial class ResetAnotherBranchViewModel : DialogViewModel
{
    private readonly IResetAnotherBranchHost _host;
    private readonly IMessageBoxService _messageBoxes;
    private readonly string _errorCaption;

    /// <param name="branches">The local branches that can be reset (not the current one, nor those at the commit already).</param>
    public ResetAnotherBranchViewModel(
        ResetAnotherBranchStrings strings,
        CommitSummaryStrings summaryStrings,
        IReadOnlyList<string> branches,
        string? defaultBranch,
        CommitSummary revision,
        bool checkoutAfterReset,
        IResetAnotherBranchHost host,
        IMessageBoxService messageBoxes,
        string errorCaption)
    {
        Strings = strings;
        Branches = branches;
        Summary = new CommitSummaryViewModel(summaryStrings, revision);
        _messageBoxes = messageBoxes;
        _errorCaption = errorCaption;

        // Set before the host, so that the initial value is not saved again.
        CheckoutAfterReset = checkoutAfterReset;
        _host = host;
        Branch = defaultBranch ?? "";
        Validate();
    }

    public ResetAnotherBranchStrings Strings { get; }

    public IReadOnlyList<string> Branches { get; }

    public CommitSummaryViewModel Summary { get; }

    [ObservableProperty]
    public partial string Branch { get; set; }

    [ObservableProperty]
    public partial bool ForceReset { get; set; }

    [ObservableProperty]
    public partial bool CheckoutAfterReset { get; set; }

    /// <summary>Whether <see cref="Branch"/> is one of <see cref="Branches"/>.</summary>
    [ObservableProperty]
    public partial bool IsBranchValid { get; private set; }

    /// <summary>Whether the (valid) branch cannot be reset without forcing, because it is not an ancestor of the commit.</summary>
    [ObservableProperty]
    public partial bool IsNonFastForward { get; private set; }

    public bool CanReset => IsBranchValid && (ForceReset || !IsNonFastForward);

    /// <summary>Whether an entered branch is unknown (highlighted in the view).</summary>
    public bool IsBranchInvalid => !IsBranchValid && Branch.Length > 0;

    partial void OnBranchChanged(string value) => Validate();

    partial void OnForceResetChanged(bool value) => Validate();

    partial void OnCheckoutAfterResetChanged(bool value) => _host?.SaveCheckoutAfterReset(value);

    private void Validate()
    {
        IsBranchValid = Branches.Contains(Branch);
        IsNonFastForward = IsBranchValid && !ForceReset && !_host.IsAncestor(Branch);
        OnPropertyChanged(nameof(CanReset));
        OnPropertyChanged(nameof(IsBranchInvalid));
        OkCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanReset))]
    private void Ok()
    {
        if (!IsBranchValid)
        {
            _messageBoxes.ShowError(string.Format(Strings.LocalRefInvalid.Text, Branch), _errorCaption);
            return;
        }

        if (_host.Reset(Branch, CheckoutAfterReset))
        {
            Close(accepted: true);
        }
    }

    [RelayCommand]
    private void Cancel() => Close(accepted: false);
}

/// <summary>Operations of the archive dialog that need the host (git, commit selection).</summary>
public interface IArchiveHost
{
    /// <summary>Lets the user choose a commit; returns <see langword="null"/> if cancelled.</summary>
    RevisionInfo? ChooseRevision(string? currentGuid);

    /// <summary>The files changed (and not deleted) between the commits.</summary>
    IReadOnlyList<string> GetChangedFiles(string? fromGuid, string? toGuid);

    /// <summary>Creates the archive with <c>git archive</c>.</summary>
    /// <param name="format"><c>zip</c> or <c>tar</c>.</param>
    /// <param name="pathArguments">The paths to archive, quoted and separated by spaces; empty for all.</param>
    void Archive(string format, string? revisionGuid, string outputPath, string pathArguments);
}

/// <summary>View model of the archive dialog (port of <c>FormArchive</c>).</summary>
public sealed partial class ArchiveViewModel : DialogViewModel
{
    private readonly string _workingDirName;
    private readonly IArchiveHost _host;
    private readonly IFileDialogService _fileDialogs;
    private readonly IMessageBoxService _messageBoxes;
    private readonly string _errorCaption;

    /// <param name="diffRevision">The commit to archive the changes since, if any.</param>
    /// <param name="path">A path to limit the archive to, if any.</param>
    /// <param name="workingDirName">The name of the working directory, the start of the suggested file name.</param>
    public ArchiveViewModel(
        ArchiveStrings strings,
        CommitSummaryStrings summaryStrings,
        RevisionInfo? revision,
        RevisionInfo? diffRevision,
        string? path,
        string workingDirName,
        IArchiveHost host,
        IFileDialogService fileDialogs,
        IMessageBoxService messageBoxes,
        string errorCaption)
    {
        Strings = strings;
        Summary = new CommitSummaryViewModel(summaryStrings);
        DiffSummary = new CommitSummaryViewModel(summaryStrings);
        _workingDirName = workingDirName;
        _host = host;
        _fileDialogs = fileDialogs;
        _messageBoxes = messageBoxes;
        _errorCaption = errorCaption;

        Revision = revision;
        IsRevisionFilterEnabled = diffRevision is not null;
        DiffRevision = diffRevision;
        if (!string.IsNullOrEmpty(path))
        {
            IsPathFilterEnabled = true;
            Paths = path;
        }
    }

    public ArchiveStrings Strings { get; }

    public CommitSummaryViewModel Summary { get; }

    public CommitSummaryViewModel DiffSummary { get; }

    [ObservableProperty]
    public partial RevisionInfo? Revision { get; set; }

    [ObservableProperty]
    public partial RevisionInfo? DiffRevision { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTar))]
    public partial bool IsZip { get; set; } = true;

    public bool IsTar
    {
        get => !IsZip;
        set => IsZip = !value;
    }

    [ObservableProperty]
    public partial bool IsPathFilterEnabled { get; set; }

    /// <summary>The paths to archive, one per line.</summary>
    [ObservableProperty]
    public partial string Paths { get; set; } = "";

    [ObservableProperty]
    public partial bool IsRevisionFilterEnabled { get; set; }

    partial void OnRevisionChanged(RevisionInfo? value) => Summary.Summary = value?.Summary;

    partial void OnDiffRevisionChanged(RevisionInfo? value) => DiffSummary.Summary = value?.Summary;

    // As FormArchive, the two filters exclude each other.
    partial void OnIsPathFilterEnabledChanged(bool value)
    {
        if (value)
        {
            IsRevisionFilterEnabled = false;
        }
    }

    partial void OnIsRevisionFilterEnabledChanged(bool value)
    {
        if (value)
        {
            IsPathFilterEnabled = false;
        }
    }

    /// <summary>The paths to pass to <c>git archive</c>, as <c>FormArchive.GetPathArgumentFromGui</c>.</summary>
    public string GetPathArgument()
    {
        if (IsPathFilterEnabled)
        {
            return string.Join(" ", SplitLines(Paths).Select(path => path.QuoteNE()));
        }

        if (IsRevisionFilterEnabled)
        {
            return string.Join(" ", _host.GetChangedFiles(DiffRevision?.Guid, Revision?.Guid).Select(file => file.QuoteNE()));
        }

        return "";
    }

    private static string[] SplitLines(string text) => text.Split(["\r\n", "\n"], StringSplitOptions.None);

    [RelayCommand]
    private void ChooseRevision()
    {
        RevisionInfo? revision = _host.ChooseRevision(Revision?.Guid);
        if (revision is not null)
        {
            Revision = revision;
        }
    }

    [RelayCommand]
    private void ChooseDiffRevision()
    {
        RevisionInfo? revision = _host.ChooseRevision(DiffRevision?.Guid ?? "");
        if (revision is not null)
        {
            DiffRevision = revision;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsRevisionFilterEnabled && DiffRevision is null)
        {
            _messageBoxes.ShowError(Strings.NoRevisionSelected.Text, _errorCaption);
            return;
        }

        string? revision = Revision?.Guid;
        string extension = IsZip ? "zip" : "tar";
        string fileName = $"{_workingDirName}_{revision}";
        string[] paths = SplitLines(Paths);
        if (IsPathFilterEnabled && paths.Length == 1 && !string.IsNullOrWhiteSpace(paths[0]))
        {
            fileName += "_" + paths[0].Trim().Replace(".", "_");
        }

        string? outputPath = await _fileDialogs.PickSaveFileAsync(
            Strings.SaveFileDialogCaption.Text,
            IsZip ? Strings.SaveFileDialogFilterZip.Text : Strings.SaveFileDialogFilterTar.Text,
            extension,
            fileName);
        if (outputPath is null)
        {
            return;
        }

        _host.Archive(extension, revision, outputPath, GetPathArgument());
        Close(accepted: true);
    }
}
