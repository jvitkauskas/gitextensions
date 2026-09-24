using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Presentation;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitExtensions.Plugins.DeleteUnusedBranches;

/// <summary>Strings of the Avalonia port of <see cref="DeleteUnusedBranchesForm"/>; ids match the form.</summary>
public sealed class DeleteUnusedBranchesStrings : ViewStrings
{
    public DeleteUnusedBranchesStrings()
        : base("DeleteUnusedBranchesForm")
    {
        Title = Add("$this", "Text", "Delete obsolete branches");
        OlderThanDays = Add("label1", "Text", "Delete branches older than x days");
        MergedInto = Add("label2", "Text", "Delete branches fully merged into branch");
        IncludeRemoteBranches = Add("IncludeRemoteBranches", "Text", "Delete remote branches from");
        UseRegexFilter = Add("useRegexFilter", "Text", "Use regex to filter branches");
        RegexCaseInsensitive = Add("useRegexCaseInsensitive", "Text", "Case insensitive");
        RegexDoesNotMatch = Add("regexDoesNotMatch", "Text", "Does not match");
        IncludeUnmergedBranches = Add("includeUnmergedBranches", "Text", "Include unmerged branches");
        SearchBranchesButton = Add("RefreshBtn", "Text", "Search branches");
        NameColumn = Add("nameDataGridViewTextBoxColumn", "HeaderText", "Name");
        DateColumn = Add("dateDataGridViewTextBoxColumn", "HeaderText", "Last activity");
        AuthorColumn = Add("Author", "HeaderText", "Last author");
        MessageColumn = Add("Message", "HeaderText", "Last message");
        Settings = Add("buttonSettings", "Text", "Settings");
        Delete = Add("Delete", "Text", "Delete");
        Close = Add("Cancel", "Text", "Close");

        DeleteCaption = Add("_deleteCaption", "Text", "Delete");
        SelectBranchesToDelete = Add("_selectBranchesToDelete", "Text", "Select branches to delete using checkboxes in '{0}' column.");
        AreYouSureToDelete = Add("_areYouSureToDelete", "Text", "Are you sure to delete {0} selected branches?");
        DangerousAction = Add("_dangerousAction", "Text", "DANGEROUS ACTION!\nBranches will be deleted on the remote '{0}'. This can not be undone.\nAre you sure you want to continue?");
        DeletingBranches = Add("_deletingBranches", "Text", "Deleting branches...");
        DeletingUnmergedBranches = Add("_deletingUnmergedBranches", "Text", "Deleting unmerged branches will result in dangling commits. Use with caution!");
        ChooseBranchesToDelete = Add("_chooseBranchesToDelete", "Text", "Choose branches to delete. Only branches that are fully merged in '{0}' will be deleted.");
        PressToSearch = Add("_pressToSearch", "Text", "Press '{0}' to search for branches to delete.");
        Cancel = Add("_cancel", "Text", "Cancel");
        SearchBranches = Add("_searchBranches", "Text", "Search branches");
        Loading = Add("_loading", "Text", "Loading...");
        BranchesSelected = Add("_branchesSelected", "Text", "{0}/{1} branches selected.");
    }

    public TranslatedText Title { get; }

    public TranslatedText OlderThanDays { get; }

    public TranslatedText MergedInto { get; }

    public TranslatedText IncludeRemoteBranches { get; }

    public TranslatedText UseRegexFilter { get; }

    public TranslatedText RegexCaseInsensitive { get; }

    public TranslatedText RegexDoesNotMatch { get; }

    public TranslatedText IncludeUnmergedBranches { get; }

    public TranslatedText SearchBranchesButton { get; }

    public TranslatedText NameColumn { get; }

    public TranslatedText DateColumn { get; }

    public TranslatedText AuthorColumn { get; }

    public TranslatedText MessageColumn { get; }

    public TranslatedText Settings { get; }

    public TranslatedText Delete { get; }

    public TranslatedText Close { get; }

    public TranslatedText DeleteCaption { get; }

    public TranslatedText SelectBranchesToDelete { get; }

    public TranslatedText AreYouSureToDelete { get; }

    public TranslatedText DangerousAction { get; }

    public TranslatedText DeletingBranches { get; }

    public TranslatedText DeletingUnmergedBranches { get; }

    public TranslatedText ChooseBranchesToDelete { get; }

    public TranslatedText PressToSearch { get; }

    public TranslatedText Cancel { get; }

    public TranslatedText SearchBranches { get; }

    public TranslatedText Loading { get; }

    public TranslatedText BranchesSelected { get; }
}

/// <summary>What the Avalonia port of <see cref="DeleteUnusedBranchesForm"/> needs from the application.</summary>
public interface IDeleteUnusedBranchesHost
{
    /// <summary>Notifies the application that the repository changed (<c>RepoChangedNotifier.Notify</c>).</summary>
    void NotifyRepoChanged();

    /// <summary>Reports an unexpected error (<c>BugReportInvoker.Report</c>).</summary>
    void ReportError(Exception exception);
}

/// <summary>A row of the branch list, the <see cref="Branch"/> with its check box.</summary>
public sealed class DeleteUnusedBranchRow : ObservableObject
{
    private readonly Action _deleteChanged;

    public DeleteUnusedBranchRow(Branch branch, Action deleteChanged)
    {
        Branch = branch;
        _deleteChanged = deleteChanged;
    }

    public Branch Branch { get; }

    public string Name => Branch.Name;

    public DateTime Date => Branch.Date;

    public string Author => Branch.Author;

    public string Message => Branch.Message;

    public bool Delete
    {
        get => Branch.Delete;
        set
        {
            if (SetProperty(Branch.Delete, value, Branch, (branch, delete) => branch.Delete = delete))
            {
                _deleteChanged();
            }
        }
    }
}

/// <summary>View model of the Avalonia port of <see cref="DeleteUnusedBranchesForm"/>.</summary>
public sealed partial class DeleteUnusedBranchesViewModel : DialogViewModel
{
    private readonly IGitModule _module;
    private readonly IBackgroundRunner _backgroundRunner;
    private readonly IMessageBoxService _messageBoxes;
    private readonly IDeleteUnusedBranchesHost _host;
    private readonly GitBranchOutputCommandParser _commandOutputParser = new();
    private CancellationTokenSource? _refreshCancellation;
    private bool _updatingSelection;
    private bool _initializing;
    private string _searchButtonPlainText;

    public DeleteUnusedBranchesViewModel(
        DeleteUnusedBranchesStrings strings,
        DeleteUnusedBranchesFormSettings settings,
        IGitModule module,
        IBackgroundRunner backgroundRunner,
        IMessageBoxService messageBoxes,
        IDeleteUnusedBranchesHost host)
    {
        Strings = strings;
        _module = module;
        _backgroundRunner = backgroundRunner;
        _messageBoxes = messageBoxes;
        _host = host;

        // As DeleteUnusedBranchesForm.OnLoad (without its change handlers, which LoadAsync runs once).
        _initializing = true;
        MergedIntoBranch = settings.MergedInBranch;
        OlderThanDays = settings.DaysOlderThan;
        IncludeRemoteBranches = settings.DeleteRemoteBranchesFromFlag;
        Remote = settings.RemoteName;
        UseRegexFilter = settings.UseRegexToFilterBranchesFlag;
        RegexFilter = settings.RegexFilter;
        RegexCaseInsensitive = settings.RegexCaseInsensitiveFlag;
        RegexDoesNotMatch = settings.RegexInvertedFlag;
        IncludeUnmergedBranches = settings.IncludeUnmergedBranchesFlag;
        SearchButtonText = strings.SearchBranchesButton.AccessKeyText;
        _searchButtonPlainText = strings.SearchBranchesButton.PlainText;
        _initializing = false;
    }

    public DeleteUnusedBranchesStrings Strings { get; }

    public ObservableCollection<DeleteUnusedBranchRow> Branches { get; } = [];

    /// <summary>Whether a branch was deleted (the return value of the plugin).</summary>
    public bool HasDeletedBranch { get; private set; }

    /// <summary>Whether the dialog was closed to open the settings of the plugin (<c>buttonSettings_Click</c>).</summary>
    public bool SettingsRequested { get; private set; }

    [ObservableProperty]
    public partial string MergedIntoBranch { get; set; }

    [ObservableProperty]
    public partial int OlderThanDays { get; set; }

    [ObservableProperty]
    public partial bool IncludeRemoteBranches { get; set; }

    [ObservableProperty]
    public partial string Remote { get; set; }

    [ObservableProperty]
    public partial bool UseRegexFilter { get; set; }

    [ObservableProperty]
    public partial string RegexFilter { get; set; }

    [ObservableProperty]
    public partial bool RegexCaseInsensitive { get; set; }

    [ObservableProperty]
    public partial bool RegexDoesNotMatch { get; set; }

    [ObservableProperty]
    public partial bool IncludeUnmergedBranches { get; set; }

    [ObservableProperty]
    public partial string InstructionText { get; private set; } = "";

    [ObservableProperty]
    public partial string StatusText { get; private set; } = "";

    /// <summary>The text of the search button: "Search branches", or "Cancel" while searching.</summary>
    [ObservableProperty]
    public partial string SearchButtonText { get; private set; }

    /// <summary>Whether branches are being searched (the loading image).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBusy))]
    public partial bool IsRefreshing { get; private set; }

    /// <summary>Whether branches are being deleted (the options and buttons are disabled).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBusy), nameof(IsIdle))]
    public partial bool IsDeleting { get; private set; }

    public bool IsBusy => IsRefreshing || IsDeleting;

    public bool IsIdle => !IsDeleting;

    /// <summary>The check box in the header of the delete column: whether all branches are checked.</summary>
    [ObservableProperty]
    public partial bool AllSelected { get; set; }

    /// <summary>As the end of <c>OnLoad</c>: the texts, the warning about unmerged branches and the first search.</summary>
    public Task LoadAsync()
    {
        ClearResults();
        WarnAboutUnmergedBranches();
        return RefreshAsync();
    }

    partial void OnMergedIntoBranchChanged(string value) => OnOptionChanged();

    partial void OnOlderThanDaysChanged(int value) => OnOptionChanged();

    partial void OnIncludeRemoteBranchesChanged(bool value) => OnOptionChanged();

    partial void OnRemoteChanged(string value) => OnOptionChanged();

    partial void OnUseRegexFilterChanged(bool value) => OnOptionChanged();

    partial void OnRegexFilterChanged(string value) => OnOptionChanged();

    /// <summary>As <c>includeUnmergedBranches_CheckedChanged</c>.</summary>
    partial void OnIncludeUnmergedBranchesChanged(bool value)
    {
        if (!_initializing)
        {
            ClearResults();
            WarnAboutUnmergedBranches();
        }
    }

    private void OnOptionChanged()
    {
        if (!_initializing)
        {
            ClearResults();
        }
    }

    /// <summary>As <c>CheckBoxHeader_OnCheckBoxClicked</c>: checks or unchecks all branches.</summary>
    partial void OnAllSelectedChanged(bool value)
    {
        if (_updatingSelection)
        {
            return;
        }

        _updatingSelection = true;
        try
        {
            foreach (DeleteUnusedBranchRow row in Branches)
            {
                row.Delete = value;
            }
        }
        finally
        {
            _updatingSelection = false;
        }

        StatusText = GetDefaultStatusText();
    }

    private void WarnAboutUnmergedBranches()
    {
        if (IncludeUnmergedBranches)
        {
            _messageBoxes.ShowWarning(Strings.DeletingUnmergedBranches.Text, Strings.DeleteCaption.Text);
        }
    }

    /// <summary>As <c>ClearResults</c>.</summary>
    private void ClearResults()
    {
        InstructionText = string.Format(Strings.ChooseBranchesToDelete.Text, MergedIntoBranch);
        StatusText = string.Format(Strings.PressToSearch.Text, _searchButtonPlainText);
        Branches.Clear();
    }

    /// <summary>As <c>Refresh_Click</c> and <c>RefreshObsoleteBranchesAsync</c>: searches, or cancels the search.</summary>
    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task RefreshAsync()
    {
        if (_refreshCancellation is { } running)
        {
            await running.CancelAsync();
            SetRefreshing(false);
            return;
        }

        SetRefreshing(true);
        CancellationToken cancellationToken = _refreshCancellation!.Token;

        RefreshContext context = new(
            IncludeRemoteBranches,
            IncludeUnmergedBranches,
            MergedIntoBranch,
            Remote,
            UseRegexFilter ? RegexFilter : null,
            RegexCaseInsensitive,
            RegexDoesNotMatch,
            TimeSpan.FromDays(OlderThanDays),
            cancellationToken);
        string currentBranch = _module.GetSelectedBranch();

        (IReadOnlyList<Branch> Branches, string? Error, string? ErrorCaption) result;
        try
        {
            result = await _backgroundRunner.RunAsync(() => GetObsoleteBranches(context, currentBranch), cancellationToken);
        }
        catch when (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (result.Error is not null)
        {
            _messageBoxes.ShowError(result.Error, result.ErrorCaption!);
        }

        Branches.Clear();
        foreach (Branch branch in result.Branches)
        {
            Branches.Add(new DeleteUnusedBranchRow(branch, OnBranchDeleteChanged));
        }

        UpdateAllSelected();
        SetRefreshing(false);
    }

    /// <summary>As <c>Delete_Click</c>.</summary>
    [RelayCommand]
    private async Task DeleteAsync()
    {
        List<Branch> selectedBranches = [.. Branches.Where(row => row.Delete).Select(row => row.Branch)];
        if (selectedBranches.Count == 0)
        {
            // The WinForms form cleared the header of the check box column, so it named an empty column here.
            _messageBoxes.ShowError(string.Format(Strings.SelectBranchesToDelete.Text, Strings.Delete.PlainText), Strings.DeleteCaption.Text);
            return;
        }

        if (!_messageBoxes.Confirm(string.Format(Strings.AreYouSureToDelete.Text, selectedBranches.Count), Strings.DeleteCaption.Text))
        {
            return;
        }

        string remoteName = Remote;
        string remoteBranchPrefix = remoteName + "/";
        List<Branch> remoteBranches = IncludeRemoteBranches
            ? [.. selectedBranches.Where(branch => branch.Name.StartsWith(remoteBranchPrefix))]
            : [];

        if (remoteBranches.Count > 0
            && !_messageBoxes.Confirm(string.Format(Strings.DangerousAction.Text, remoteName), Strings.DeleteCaption.Text))
        {
            return;
        }

        HasDeletedBranch = true;

        List<Branch> localBranches = [.. selectedBranches.Except(remoteBranches)];
        bool force = IncludeUnmergedBranches;
        IsDeleting = true;
        StatusText = Strings.DeletingBranches.Text;

        Exception? error = await _backgroundRunner.RunAsync(() =>
        {
            try
            {
                // Branches are deleted one by one, because one may fail.
                foreach (Branch remoteBranch in remoteBranches)
                {
                    GitArgumentBuilder args = new("push")
                    {
                        remoteName,
                        $":{remoteBranch.Name[remoteBranchPrefix.Length..]}"
                    };
                    _module.GitExecutable.GetOutput(args);
                }

                foreach (Branch localBranch in localBranches)
                {
                    GitArgumentBuilder args = new("branch")
                    {
                        force ? "-D" : "-d",
                        localBranch.Name
                    };
                    _module.GitExecutable.GetOutput(args);
                }

                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        });

        if (error is not null)
        {
            _host.ReportError(error);
        }

        _host.NotifyRepoChanged();
        IsDeleting = false;
        await RefreshAsync();
    }

    /// <summary>As <c>buttonSettings_Click</c>: closes the dialog, then the plugin opens its settings.</summary>
    [RelayCommand]
    private void OpenSettings()
    {
        SettingsRequested = true;
        Close(accepted: false);
    }

    [RelayCommand]
    private void Cancel() => Close(accepted: false);

    public override bool CanClose()
    {
        _refreshCancellation?.Cancel();
        return true;
    }

    private void SetRefreshing(bool refreshing)
    {
        _refreshCancellation = refreshing ? new CancellationTokenSource() : null;
        IsRefreshing = refreshing;
        SearchButtonText = refreshing ? Strings.Cancel.AccessKeyText : Strings.SearchBranches.AccessKeyText;
        _searchButtonPlainText = refreshing ? Strings.Cancel.PlainText : Strings.SearchBranches.PlainText;
        StatusText = refreshing ? Strings.Loading.Text : GetDefaultStatusText();
    }

    /// <summary>As <c>BranchesGrid_CellContentClick</c>.</summary>
    private void OnBranchDeleteChanged()
    {
        if (_updatingSelection)
        {
            return;
        }

        UpdateAllSelected();
        StatusText = GetDefaultStatusText();
    }

    private void UpdateAllSelected()
    {
        _updatingSelection = true;
        try
        {
            AllSelected = Branches.All(row => row.Delete);
        }
        finally
        {
            _updatingSelection = false;
        }
    }

    private string GetDefaultStatusText()
        => string.Format(Strings.BranchesSelected.Text, Branches.Count(row => row.Delete), Branches.Count);

    /// <summary>As <c>GetObsoleteBranches</c>; runs in the background, with the error of the branch list for the UI thread.</summary>
    private (IReadOnlyList<Branch> Branches, string? Error, string? ErrorCaption) GetObsoleteBranches(RefreshContext context, string currentBranch)
    {
        List<Branch> branches = [];
        DateTime oldBranchLimitDate = DateTime.Now - context.ObsolescenceDuration;
        IEnumerable<string> branchNames = GetObsoleteBranchNames(context, currentBranch, out string? error, out string? errorCaption);
        foreach (string branchName in branchNames)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            GitArgumentBuilder args = new("log")
            {
                "--pretty=\"format:%ci\n%an\n%s\"",
                "--max-count=1",
                branchName.Quote(),
                "--"
            };

            string[] commitLog = _module.GitExecutable.GetOutput(args).Split('\n');
            if (!DateTime.TryParse(commitLog[0], out DateTime commitDate))
            {
                Trace.WriteLine($"Failed to parse commit date from git log output: '{commitLog[0]}' from {commitLog}");
                commitDate = DateTime.MinValue;
            }

            string authorName = commitLog.Length > 1 ? commitLog[1] : string.Empty;
            string message = commitLog.Length > 2 ? commitLog[2] : string.Empty;

            branches.Add(new Branch(branchName, commitDate, authorName, message, commitDate < oldBranchLimitDate));
        }

        return (branches, error, errorCaption);
    }

    /// <summary>As <c>GetObsoleteBranchNames</c>.</summary>
    private IEnumerable<string> GetObsoleteBranchNames(RefreshContext context, string currentBranch, out string? error, out string? errorCaption)
    {
        RegexOptions options = context.RegexIgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
        bool regexMustMatch = !context.RegexDoesNotMatch;

        GitArgumentBuilder args = new("branch")
        {
            "--list",
            { context.RemoteBranches, "-r" },
            { !context.IncludeUnmerged, $"--merged {context.ReferenceBranch}" }
        };

        ExecutionResult result = _module.GitExecutable.Execute(args, throwOnErrorExit: false);
        if (!result.ExitedSuccessfully)
        {
            error = result.AllOutput;
            errorCaption = $"git {args}";
            return [];
        }

        error = null;
        errorCaption = null;
        bool withoutRegexFilter = string.IsNullOrEmpty(context.RegexFilter);
        return _commandOutputParser.GetBranchNames(result.StandardOutput, context.RemoteBranches)
            .Where(branchName => branchName != currentBranch && branchName != context.ReferenceBranch)
            .Where(branchName => (!context.RemoteBranches || branchName.StartsWith(context.RemoteRepositoryName + "/"))
                && (withoutRegexFilter || Regex.IsMatch(branchName, context.RegexFilter!, options) == regexMustMatch));
    }

    private sealed record RefreshContext(
        bool RemoteBranches,
        bool IncludeUnmerged,
        string ReferenceBranch,
        string RemoteRepositoryName,
        string? RegexFilter,
        bool RegexIgnoreCase,
        bool RegexDoesNotMatch,
        TimeSpan ObsolescenceDuration,
        CancellationToken CancellationToken);
}
