using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands.Git;
using GitUI.Presentation.Services;
using GitUI.Presentation.UserControls;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Operations of the delete branch dialog that need the host (git, worktrees, suppressible confirmations).</summary>
public interface IDeleteBranchHost
{
    /// <summary>
    ///  Handles branches checked out in other worktrees (offering to delete the worktrees, as <c>FormDeleteBranch</c>);
    ///  returns the branches that can be deleted.
    /// </summary>
    IReadOnlyList<string> HandleWorktreeBranches(IReadOnlyList<string> branches);

    /// <summary>Asks whether to delete unmerged branches (a suppressible confirmation).</summary>
    bool ConfirmDeleteUnmerged();

    /// <summary>Deletes the branches in the progress dialog; returns whether it succeeded.</summary>
    bool DeleteBranches(IReadOnlyList<string> branches);
}

/// <summary>View model of the delete branch dialog (port of <c>FormDeleteBranch</c>).</summary>
public sealed partial class DeleteBranchViewModel : DialogViewModel
{
    private readonly string? _currentBranch;
    private readonly IReadOnlySet<string>? _mergedBranches;
    private readonly IDeleteBranchHost _host;
    private readonly IMessageBoxService _messageBoxes;

    /// <param name="currentBranch">The checked out branch, <see langword="null"/> if unknown.</param>
    /// <param name="mergedBranches">
    ///  The branches merged into HEAD, or <see langword="null"/> if deleting unmerged branches needs no confirmation.
    /// </param>
    public DeleteBranchViewModel(
        DeleteBranchStrings strings,
        BranchSelectorViewModel branches,
        string? currentBranch,
        IReadOnlySet<string>? mergedBranches,
        IDeleteBranchHost host,
        IMessageBoxService messageBoxes)
    {
        Strings = strings;
        Branches = branches;
        _currentBranch = currentBranch;
        _mergedBranches = mergedBranches;
        _host = host;
        _messageBoxes = messageBoxes;
    }

    public DeleteBranchStrings Strings { get; }

    public BranchSelectorViewModel Branches { get; }

    [RelayCommand]
    private void Delete()
    {
        IReadOnlyList<string> selectedBranches = Branches.GetSelectedBranches(reportInvalid: true);
        if (selectedBranches.Count == 0)
        {
            return;
        }

        if (_currentBranch is not null && selectedBranches.Contains(_currentBranch))
        {
            _messageBoxes.ShowError(string.Format(Strings.CannotDeleteCurrentBranch.Text, _currentBranch), Strings.DeleteBranchCaption.Text);
            return;
        }

        selectedBranches = _host.HandleWorktreeBranches(selectedBranches);
        if (selectedBranches.Count == 0)
        {
            return;
        }

        if (_mergedBranches is not null)
        {
            // Always treat branches as unmerged if there is no current branch (HEAD is detached).
            bool hasUnmergedBranches = _currentBranch is null
                || DetachedHeadParser.IsDetachedHead(_currentBranch)
                || selectedBranches.Any(branch => !_mergedBranches.Contains(branch));
            if (hasUnmergedBranches && !_host.ConfirmDeleteUnmerged())
            {
                return;
            }
        }

        if (_host.DeleteBranches(selectedBranches))
        {
            Close(accepted: true);
        }
    }
}

/// <summary>Operations of the delete remote branch dialog that need the host (git, remotes, event scripts).</summary>
public interface IDeleteRemoteBranchHost
{
    /// <summary>The local branches that track any of the remote branches.</summary>
    IReadOnlyList<string> GetTrackingBranches(IReadOnlyList<string> remoteBranches);

    /// <summary>Whether any of the remote branches is not merged.</summary>
    bool HasUnmergedBranches(IReadOnlyList<string> remoteBranches);

    /// <summary>
    ///  Deletes the remote branches (and optionally their local tracking branches); returns <see langword="false"/> if
    ///  cancelled (e.g. by an event script), which keeps the dialog open.
    /// </summary>
    bool DeleteRemoteBranches(IReadOnlyList<string> remoteBranches, bool deleteLocalTrackingBranches);
}

/// <summary>View model of the delete remote branch dialog (port of <c>FormDeleteRemoteBranch</c>).</summary>
public sealed partial class DeleteRemoteBranchViewModel : DialogViewModel
{
    private const int MaxDisplayedTrackingBranches = 8;

    private readonly IDeleteRemoteBranchHost _host;
    private readonly IMessageBoxService _messageBoxes;

    public DeleteRemoteBranchViewModel(DeleteRemoteBranchStrings strings, BranchSelectorViewModel branches, IDeleteRemoteBranchHost host, IMessageBoxService messageBoxes)
    {
        Strings = strings;
        Branches = branches;
        _host = host;
        _messageBoxes = messageBoxes;

        Branches.SelectionChanged += (_, _) => UpdateTrackingBranches();
        UpdateTrackingBranches();
    }

    public DeleteRemoteBranchStrings Strings { get; }

    public BranchSelectorViewModel Branches { get; }

    /// <summary>The confirmation that the branches are deleted from the remote; required to delete.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    public partial bool DeleteRemote { get; set; }

    [ObservableProperty]
    public partial bool DeleteLocalTrackingBranch { get; set; }

    [ObservableProperty]
    public partial bool CanDeleteLocalTrackingBranch { get; private set; }

    /// <summary>The local tracking branches that would be deleted too.</summary>
    [ObservableProperty]
    public partial string TrackingBranchesText { get; private set; } = "";

    private void UpdateTrackingBranches()
    {
        IReadOnlyList<string> trackingBranches = _host.GetTrackingBranches(Branches.GetSelectedBranches());
        if (trackingBranches.Count == 0)
        {
            DeleteLocalTrackingBranch = false;
            CanDeleteLocalTrackingBranch = false;
            TrackingBranchesText = "";
            return;
        }

        CanDeleteLocalTrackingBranch = true;

        StringBuilder text = new();
        text.AppendLine(Strings.ToDeleteCandidates.Text);
        foreach (string branch in trackingBranches.Take(MaxDisplayedTrackingBranches))
        {
            text.Append(" - ").AppendLine(branch);
        }

        if (trackingBranches.Count > MaxDisplayedTrackingBranches)
        {
            text.AppendLine().AppendFormat(Strings.AndMore.Text, trackingBranches.Count - MaxDisplayedTrackingBranches);
        }

        TrackingBranchesText = text.ToString();
    }

    [RelayCommand(CanExecute = nameof(DeleteRemote))]
    private void Delete()
    {
        IReadOnlyList<string> selectedBranches = Branches.GetSelectedBranches(reportInvalid: true);
        if (_host.HasUnmergedBranches(selectedBranches)
            && !_messageBoxes.Confirm(Strings.ConfirmDeleteUnmerged.Text, Strings.DeleteRemoteBranchesCaption.Text, defaultNo: true))
        {
            return;
        }

        if (_host.DeleteRemoteBranches(selectedBranches, DeleteLocalTrackingBranch))
        {
            Close(accepted: true);
        }
    }
}

/// <summary>The merge settings the merge dialog starts with.</summary>
public sealed record MergeBranchOptions(bool NoFastForward, bool NoCommit, bool AddLogMessages, int LogMessagesCount, bool ShowAdvanced);

/// <summary>A merge as chosen in the merge dialog.</summary>
/// <param name="Strategy">The non-default merge strategy, if any.</param>
/// <param name="MergeMessage">The merge message, if one was specified.</param>
/// <param name="LogMessages">The number of log messages to add, if any.</param>
public sealed record MergeRequest(
    string Branch,
    bool FastForward,
    bool Squash,
    bool NoCommit,
    string? Strategy,
    bool AllowUnrelatedHistories,
    string? MergeMessage,
    int? LogMessages);

/// <summary>Operations of the merge dialog that need the host (settings, git, event scripts).</summary>
public interface IMergeBranchHost
{
    /// <summary>Persists the log message settings, which the merge dialog saves as soon as they change.</summary>
    void SaveLogMessagesSettings(bool addLogMessages, int count);

    void OpenStrategyHelp();

    /// <summary>Merges; returns whether the dialog should close (merged, or stopped on conflicts).</summary>
    bool Merge(MergeRequest request);
}

/// <summary>View model of the merge branch dialog (port of <c>FormMergeBranch</c>).</summary>
public sealed partial class MergeBranchViewModel : DialogViewModel
{
    private readonly IMergeBranchHost _host;

    public MergeBranchViewModel(
        MergeBranchStrings strings,
        BranchSelectorViewModel branches,
        string currentBranch,
        MergeBranchOptions options,
        HelpImageViewModel helpImage,
        IMergeBranchHost host)
    {
        Strings = strings;
        Branches = branches;
        CurrentBranch = currentBranch;
        HelpImage = helpImage;
        HelpImage.HoverNotice = strings.HoverShowImageText.Text;

        NoFastForward = options.NoFastForward;
        HelpImage.IsOnHoverShowImage2 = !options.NoFastForward;
        NoCommit = options.NoCommit;
        AddLogMessages = options.AddLogMessages;
        LogMessagesCount = options.LogMessagesCount;
        ShowAdvanced = options.ShowAdvanced;

        // Assigned last, so that setting up the options does not save them again.
        _host = host;
    }

    public MergeBranchStrings Strings { get; }

    public BranchSelectorViewModel Branches { get; }

    public string CurrentBranch { get; }

    public HelpImageViewModel HelpImage { get; }

    /// <summary>The merge strategies offered for a non-default strategy (not translated).</summary>
    public IReadOnlyList<string> MergeStrategies { get; } = ["resolve", "recursive", "octopus", "ours", "subtree"];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFastForward), nameof(CanSquash))]
    public partial bool NoFastForward { get; set; }

    public bool IsFastForward
    {
        get => !NoFastForward;
        set => NoFastForward = !value;
    }

    [ObservableProperty]
    public partial bool NoCommit { get; set; }

    [ObservableProperty]
    public partial bool ShowAdvanced { get; set; }

    [ObservableProperty]
    public partial bool Squash { get; set; }

    public bool CanSquash => !NoFastForward;

    [ObservableProperty]
    public partial bool AllowUnrelatedHistories { get; set; }

    [ObservableProperty]
    public partial bool UseNonDefaultStrategy { get; set; }

    [ObservableProperty]
    public partial string MergeStrategy { get; set; } = "";

    [ObservableProperty]
    public partial bool AddLogMessages { get; set; }

    [ObservableProperty]
    public partial int LogMessagesCount { get; set; }

    [ObservableProperty]
    public partial bool AddMergeMessage { get; set; }

    [ObservableProperty]
    public partial string MergeMessage { get; set; } = "";

    partial void OnNoFastForwardChanged(bool value)
    {
        HelpImage.IsOnHoverShowImage2 = !value;
        if (value)
        {
            Squash = false;
        }
    }

    partial void OnShowAdvancedChanged(bool value)
    {
        if (!value)
        {
            UseNonDefaultStrategy = false;
            Squash = false;
            AllowUnrelatedHistories = false;
            AddMergeMessage = false;
            MergeStrategy = "";
        }
    }

    partial void OnAddLogMessagesChanged(bool value) => _host?.SaveLogMessagesSettings(value, LogMessagesCount);

    partial void OnLogMessagesCountChanged(int value) => _host?.SaveLogMessagesSettings(AddLogMessages, value);

    [RelayCommand]
    private void OpenStrategyHelp() => _host.OpenStrategyHelp();

    [RelayCommand]
    private void Merge()
    {
        MergeRequest request = new(
            Branches.Text,
            FastForward: !NoFastForward,
            Squash,
            NoCommit,
            UseNonDefaultStrategy && !string.IsNullOrWhiteSpace(MergeStrategy) ? MergeStrategy : null,
            AllowUnrelatedHistories,
            AddMergeMessage ? MergeMessage : null,
            AddLogMessages ? LogMessagesCount : null);

        if (_host.Merge(request))
        {
            Close(accepted: true);
        }
    }
}
