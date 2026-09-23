using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Editor;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUIPluginInterfaces;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the diff dialog; ids match <c>FormDiff</c>.</summary>
public sealed class DiffStrings : ViewStrings
{
    public DiffStrings()
        : base("FormDiff")
    {
        Title = Add("$this", "Text", "Diff");
        AnotherBranchToolTip = Add("_anotherBranchTooltip", "Text", "Select another branch");
        AnotherCommitToolTip = Add("_anotherCommitTooltip", "Text", "Select another commit");
        SwapToolTip = Add("_btnSwapTooltip", "Text", "Swap BASE and Compare commits");
        CompareToMergeBase = Add("_ckCompareToMergeBase", "Text", "Compare to merge &base");
        CompareDirectoriesWithDiffTool = Add("btnCompareDirectoriesWithDiffTool", "Text", "Open diff using &directory diff tool");
        FirstCommit = Add("firstCommitGroup", "Text", "BASE");
        SecondCommit = Add("secondCommitGroup", "Text", "Compare");
    }

    public TranslatedText Title { get; }

    public TranslatedText AnotherBranchToolTip { get; }

    public TranslatedText AnotherCommitToolTip { get; }

    public TranslatedText SwapToolTip { get; }

    public TranslatedText CompareToMergeBase { get; }

    public TranslatedText CompareDirectoriesWithDiffTool { get; }

    public TranslatedText FirstCommit { get; }

    public TranslatedText SecondCommit { get; }
}

/// <summary>Operations of the diff dialog that need the host (git, the other dialogs, the diff tool).</summary>
public interface IDiffHost
{
    /// <summary>The files of the diffs of the first revision to the others (as <c>FileStatusList.SetDiffsAsync</c>).</summary>
    Task<IReadOnlyList<FileStatusGroup>> GetDiffsAsync(IReadOnlyList<GitRevision> revisions, CancellationToken cancellationToken);

    /// <summary>Asks for a branch (the compare to branch dialog); <see langword="null"/> if cancelled.</summary>
    (string DisplayName, GitRevision? Revision)? PickBranch(GitRevision preselect);

    /// <summary>Asks for a commit (the choose commit dialog); <see langword="null"/> if cancelled.</summary>
    GitRevision? PickCommit(GitRevision preselect);

    /// <summary>Opens the directory diff of the difftool (<c>OpenWithDifftoolDirDiff</c>).</summary>
    void OpenDirectoryDiff(GitRevision first, GitRevision second);
}

/// <summary>View model of the diff dialog (port of <c>FormDiff</c>): the changes between two commits.</summary>
public sealed partial class DiffViewModel : DialogViewModel
{
    private readonly IDiffHost _host;
    private readonly GitRevision? _mergeBase;
    private CancellationTokenSource? _populating;

    /// <param name="mergeBase">The merge base of the two commits, if any; it does not change when they are changed.</param>
    public DiffViewModel(
        DiffStrings strings,
        IDiffHost host,
        IFileViewerHost fileViewerHost,
        FileStatusListStrings fileStatusListStrings,
        FileStatusTreeOptions fileStatusTreeOptions,
        GitRevision first,
        GitRevision second,
        string? firstDisplayName,
        string? secondDisplayName,
        GitRevision? mergeBase)
    {
        Strings = strings;
        _host = host;
        _mergeBase = mergeBase;
        FirstRevision = first;
        SecondRevision = second;
        FirstDisplayName = firstDisplayName;
        SecondDisplayName = secondDisplayName;
        Files = new FileStatusListViewModel(fileStatusListStrings, fileStatusTreeOptions);
        Viewer = new FileViewerViewModel(fileViewerHost);
        Files.SelectionChanged += (_, _) => _ = Viewer.ShowChangesAsync(Files.SelectedEntry);
    }

    public DiffStrings Strings { get; }

    public FileStatusListViewModel Files { get; }

    public FileViewerViewModel Viewer { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCompareDirectories))]
    public partial GitRevision? FirstRevision { get; private set; }

    [ObservableProperty]
    public partial GitRevision? SecondRevision { get; private set; }

    [ObservableProperty]
    public partial string? FirstDisplayName { get; private set; }

    [ObservableProperty]
    public partial string? SecondDisplayName { get; private set; }

    [ObservableProperty]
    public partial bool CompareToMergeBase { get; set; }

    public bool HasMergeBase => _mergeBase is not null;

    /// <summary>As <c>ckCompareToMergeBase.Text</c>: with the short hash of the merge base.</summary>
    public string CompareToMergeBaseText => $"{Strings.CompareToMergeBase.AccessKeyText} ({_mergeBase?.ObjectId.ToShortString()})";

    /// <summary>
    ///  As <c>PopulateDiffFiles</c>: git-for-windows fails to compare "from" the working directory with the directory diff
    ///  tool (<c>git difftool --dir-diff -R</c>).
    /// </summary>
    public bool CanCompareDirectories => FirstRevision?.ObjectId != ObjectId.WorkTreeId;

    /// <summary>As <c>Load += PopulateDiffFiles</c>.</summary>
    public Task InitializeAsync() => PopulateDiffFilesAsync();

    partial void OnCompareToMergeBaseChanged(bool value) => _ = PopulateDiffFilesAsync();

    [RelayCommand]
    private Task SwapAsync()
    {
        (FirstRevision, SecondRevision) = (SecondRevision, FirstRevision);
        (FirstDisplayName, SecondDisplayName) = (SecondDisplayName, FirstDisplayName);
        return PopulateDiffFilesAsync();
    }

    [RelayCommand]
    private Task PickFirstBranchAsync() => PickBranchAsync(isFirst: true);

    [RelayCommand]
    private Task PickSecondBranchAsync() => PickBranchAsync(isFirst: false);

    [RelayCommand]
    private Task PickFirstCommitAsync() => PickCommitAsync(isFirst: true);

    [RelayCommand]
    private Task PickSecondCommitAsync() => PickCommitAsync(isFirst: false);

    [RelayCommand]
    private void CompareDirectories()
    {
        GitRevision? first = CompareToMergeBase ? _mergeBase : FirstRevision;
        if (first is not null && SecondRevision is not null)
        {
            _host.OpenDirectoryDiff(first, SecondRevision);
        }
    }

    /// <summary>As <c>PickAnotherBranch</c>.</summary>
    private Task PickBranchAsync(bool isFirst)
    {
        GitRevision? current = isFirst ? FirstRevision : SecondRevision;
        if (current is null || _host.PickBranch(current) is not { } picked)
        {
            return Task.CompletedTask;
        }

        SetRevision(isFirst, picked.Revision, picked.DisplayName);
        return PopulateDiffFilesAsync();
    }

    /// <summary>As <c>PickAnotherCommit</c>.</summary>
    private Task PickCommitAsync(bool isFirst)
    {
        GitRevision? current = isFirst ? FirstRevision : SecondRevision;
        if (current is null || _host.PickCommit(current) is not { } chosen)
        {
            return Task.CompletedTask;
        }

        SetRevision(isFirst, chosen, chosen.Subject);
        return PopulateDiffFilesAsync();
    }

    private void SetRevision(bool isFirst, GitRevision? revision, string? displayName)
    {
        if (isFirst)
        {
            FirstRevision = revision;
            FirstDisplayName = displayName;
        }
        else
        {
            SecondRevision = revision;
            SecondDisplayName = displayName;
        }
    }

    /// <summary>As <c>PopulateDiffFiles</c>: the second commit compared to the first (or their merge base).</summary>
    private async Task PopulateDiffFilesAsync()
    {
        CancellationTokenSource? previous = _populating;
        if (previous is not null)
        {
            // Synchronously: awaiting CancelAsync could continue off the UI thread, and no long callbacks are registered.
#pragma warning disable VSTHRD103 // Call async methods when in an async method
            previous.Cancel();
#pragma warning restore VSTHRD103
            previous.Dispose();
        }

        GitRevision? first = CompareToMergeBase ? _mergeBase : FirstRevision;
        if (first is null || SecondRevision is null)
        {
            _populating = null;
            Files.Clear();
            return;
        }

        CancellationTokenSource populating = new();
        _populating = populating;
        Files.SetLoading();
        IReadOnlyList<FileStatusGroup> groups;
        try
        {
            groups = await _host.GetDiffsAsync([SecondRevision, first], populating.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (!populating.IsCancellationRequested)
        {
            Files.SetGroups(groups);
        }
    }
}
