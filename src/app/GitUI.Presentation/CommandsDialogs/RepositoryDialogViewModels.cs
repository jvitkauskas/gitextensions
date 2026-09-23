using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands;
using GitCommands.Git;
using GitCommands.Utils;
using GitExtensions.Extensibility;
using GitUI.Presentation.Services;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>View model of the add submodule dialog (port of <c>FormAddSubmodule</c>).</summary>
public sealed partial class AddSubmoduleViewModel : DialogViewModel
{
    private readonly Func<string, IReadOnlyList<string>> _loadRemoteBranches;
    private readonly Action<ArgumentString> _runGit;
    private readonly IMessageBoxService _messageBoxes;
    private readonly IFileDialogService _fileDialogs;

    /// <param name="loadRemoteBranches">Lists the branches of the repository at the given URL or path.</param>
    /// <param name="runGit">Runs git with the given arguments in the progress dialog.</param>
    public AddSubmoduleViewModel(
        AddSubmoduleStrings strings,
        IReadOnlyList<string> recentRepositories,
        Func<string, IReadOnlyList<string>> loadRemoteBranches,
        Action<ArgumentString> runGit,
        IMessageBoxService messageBoxes,
        IFileDialogService fileDialogs)
    {
        Strings = strings;
        RecentRepositories = recentRepositories;
        _loadRemoteBranches = loadRemoteBranches;
        _runGit = runGit;
        _messageBoxes = messageBoxes;
        _fileDialogs = fileDialogs;
    }

    public AddSubmoduleStrings Strings { get; }

    public IReadOnlyList<string> RecentRepositories { get; }

    /// <summary>The URL or path of the repository to add.</summary>
    [ObservableProperty]
    public partial string Directory { get; set; } = "";

    [ObservableProperty]
    public partial string LocalPath { get; set; } = "";

    [ObservableProperty]
    public partial string Branch { get; set; } = "";

    [ObservableProperty]
    public partial bool Force { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<string> RemoteBranches { get; set; } = [];

    /// <summary>Loads the branches of <see cref="Directory"/>; done when the branch list drops down.</summary>
    [RelayCommand]
    private void LoadRemoteBranches() => RemoteBranches = _loadRemoteBranches(Directory);

    [RelayCommand]
    private async Task BrowseAsync()
    {
        string? folder = await _fileDialogs.PickFolderAsync(Directory);
        if (folder is not null)
        {
            Directory = folder;
        }
    }

    [RelayCommand]
    private void AddSubmodule()
    {
        if (string.IsNullOrEmpty(Directory) || string.IsNullOrEmpty(LocalPath))
        {
            _messageBoxes.ShowError(Strings.RemoteAndLocalPathRequired.Text, Strings.Title.Text);
            return;
        }

        _runGit(Commands.AddSubmodule(Directory, LocalPath, Branch, Force));
        Close(accepted: true);
    }

    partial void OnDirectoryChanged(string value)
    {
        // Suggest the repository name as local path.
        string name = PathUtil.GetRepositoryName(value);
        if (name != "")
        {
            LocalPath = name;
        }
    }
}

/// <summary>View model of the clean working directory dialog (port of <c>FormCleanupRepository</c>).</summary>
public sealed partial class CleanupRepositoryViewModel : DialogViewModel
{
    private readonly string _workingDir;
    private readonly string _workingDirGitDir;
    private readonly Func<ArgumentString, string> _readGit;
    private readonly IMessageBoxService _messageBoxes;
    private readonly IFileDialogService _fileDialogs;

    /// <param name="path">A path to limit the cleanup to (as <c>FormCleanupRepository.SetPathArgument</c>).</param>
    /// <param name="readGit">Runs git with the given arguments in the progress dialog and returns its output.</param>
    public CleanupRepositoryViewModel(
        CleanupRepositoryStrings strings,
        string workingDir,
        string workingDirGitDir,
        string? path,
        Func<ArgumentString, string> readGit,
        IMessageBoxService messageBoxes,
        IFileDialogService fileDialogs)
    {
        Strings = strings;
        _workingDir = workingDir;
        _workingDirGitDir = workingDirGitDir;
        _readGit = readGit;
        _messageBoxes = messageBoxes;
        _fileDialogs = fileDialogs;

        if (!string.IsNullOrEmpty(path))
        {
            IsIncludeFilterEnabled = true;
            IncludePaths = path;
            IsExcludeFilterEnabled = true;
            ExcludePaths = path;
        }
    }

    public CleanupRepositoryStrings Strings { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRemoveAll), nameof(IsRemoveNonIgnored), nameof(IsRemoveIgnored))]
    public partial CleanMode Mode { get; set; } = CleanMode.All;

    public bool IsRemoveAll
    {
        get => Mode == CleanMode.All;
        set => SetMode(value, CleanMode.All);
    }

    public bool IsRemoveNonIgnored
    {
        get => Mode == CleanMode.OnlyNonIgnored;
        set => SetMode(value, CleanMode.OnlyNonIgnored);
    }

    public bool IsRemoveIgnored
    {
        get => Mode == CleanMode.OnlyIgnored;
        set => SetMode(value, CleanMode.OnlyIgnored);
    }

    [ObservableProperty]
    public partial bool RemoveDirectories { get; set; } = true;

    [ObservableProperty]
    public partial bool CleanSubmodules { get; set; }

    [ObservableProperty]
    public partial bool IsIncludeFilterEnabled { get; set; }

    [ObservableProperty]
    public partial string IncludePaths { get; set; } = "";

    [ObservableProperty]
    public partial bool IsExcludeFilterEnabled { get; set; }

    [ObservableProperty]
    public partial string ExcludePaths { get; set; } = "";

    /// <summary>The output of the last cleanup or preview.</summary>
    [ObservableProperty]
    public partial string Output { get; set; } = "";

    /// <summary>The <c>git clean</c> paths: the non-empty lines of <see cref="IncludePaths"/>, quoted.</summary>
    public string? GetIncludePathArgument()
        => IsIncludeFilterEnabled
            ? string.Join(" ", SplitLines(IncludePaths).Select(p => $"\"{p}\""))
            : null;

    /// <summary>The <c>git clean</c> excludes: the non-empty lines of <see cref="ExcludePaths"/> as POSIX <c>--exclude</c> options.</summary>
    public string? GetExcludePathArgument()
        => IsExcludeFilterEnabled
            ? string.Join(" ", SplitLines(ExcludePaths).Select(p => $"--exclude={p.Replace(" ", "?")}".ToPosixPath()))
            : null;

    private static IEnumerable<string> SplitLines(string text) => text.Split(["\r\n", "\n"], StringSplitOptions.None).Where(line => !string.IsNullOrEmpty(line));

    private void SetMode(bool isChecked, CleanMode mode)
    {
        if (isChecked)
        {
            Mode = mode;
        }
    }

    private void CleanUp(bool dryRun)
    {
        string? paths = GetIncludePathArgument();
        string? excludes = GetExcludePathArgument();

        Output = EnvUtils.ReplaceLinuxNewLinesDependingOnPlatform(
            _readGit(Commands.Clean(Mode, dryRun, directories: RemoveDirectories, paths: paths, excludes: excludes))) ?? "";

        if (CleanSubmodules)
        {
            Output += EnvUtils.ReplaceLinuxNewLinesDependingOnPlatform(
                _readGit(Commands.CleanSubmodules(Mode, dryRun, directories: RemoveDirectories, paths: paths)));
        }
    }

    [RelayCommand]
    private void Preview() => CleanUp(dryRun: true);

    [RelayCommand]
    private void Cleanup()
    {
        if (_messageBoxes.Confirm(Strings.ReallyCleanupQuestion.Text, Strings.ReallyCleanupQuestionCaption.Text))
        {
            CleanUp(dryRun: false);
        }
    }

    [RelayCommand]
    private async Task AddIncludePathAsync()
    {
        string? folder = await _fileDialogs.PickFolderAsync(_workingDir);

        // As FormCleanupRepository: only existing subdirectories of the working directory, excluding .git.
        if (folder is null
            || !folder.StartsWith(_workingDir)
            || !System.IO.Directory.Exists(folder)
            || folder.Equals(PathUtil.RemoveTrailingPathSeparator(_workingDirGitDir)))
        {
            return;
        }

        IsIncludeFilterEnabled = true;
        IncludePaths = AppendLine(IncludePaths, folder);
    }

    [RelayCommand]
    private async Task AddExcludePathAsync()
    {
        IReadOnlyList<string> files = await _fileDialogs.PickFilesAsync(allowMultiple: false, _workingDir);
        if (files.Count == 0 || !files[0].StartsWith(_workingDir))
        {
            return;
        }

        IsExcludeFilterEnabled = true;
        ExcludePaths = AppendLine(ExcludePaths, files[0].Replace(_workingDir, ""));
    }

    private static string AppendLine(string text, string line) => text.Length == 0 ? line : text + Environment.NewLine + line;

    [RelayCommand]
    private void Close() => Close(accepted: true);
}

/// <summary>Operations of the submodule conflict dialog that need the host (git, other dialogs).</summary>
public interface IMergeSubmoduleHost
{
    /// <summary>The commit currently checked out in the submodule, or <see langword="null"/>.</summary>
    string? GetCurrentCheckout();

    /// <summary>Stages the submodule in the superproject, showing git errors if any.</summary>
    void StageSubmodule();

    /// <summary>Opens the submodule in a new application instance.</summary>
    void OpenSubmodule();

    /// <summary>Lets the user check out a branch in the submodule, offering the given commits; returns whether one was checked out.</summary>
    bool CheckoutBranch(string localCommit, string remoteCommit);
}

/// <summary>View model of the submodule conflict dialog (port of <c>FormMergeSubmodule</c>).</summary>
public sealed partial class MergeSubmoduleViewModel : DialogViewModel
{
    private readonly string? _localCommit;
    private readonly string? _remoteCommit;
    private readonly IMergeSubmoduleHost _host;

    /// <param name="baseCommit">The commit of the submodule in the merge base, <see langword="null"/> if deleted.</param>
    /// <param name="localCommit">The commit of the submodule in the local branch, <see langword="null"/> if deleted.</param>
    /// <param name="remoteCommit">The commit of the submodule in the remote branch, <see langword="null"/> if deleted.</param>
    public MergeSubmoduleViewModel(MergeSubmoduleStrings strings, string submodule, string? baseCommit, string? localCommit, string? remoteCommit, IMergeSubmoduleHost host)
    {
        Strings = strings;
        Submodule = submodule;
        _localCommit = localCommit;
        _remoteCommit = remoteCommit;
        _host = host;

        Base = baseCommit ?? strings.Deleted.Text;
        Local = localCommit ?? strings.Deleted.Text;
        Remote = remoteCommit ?? strings.Deleted.Text;

        // FormMergeSubmodule only requires base and remote, and would then fail to parse a deleted local commit.
        CanCheckoutBranch = baseCommit is not null && localCommit is not null && remoteCommit is not null;
        Refresh();
    }

    public MergeSubmoduleStrings Strings { get; }

    public string Submodule { get; }

    public string Base { get; }

    public string Local { get; }

    public string Remote { get; }

    [ObservableProperty]
    public partial string Current { get; set; } = "";

    public bool CanCheckoutBranch { get; }

    [RelayCommand]
    private void Refresh() => Current = _host.GetCurrentCheckout() ?? "";

    [RelayCommand]
    private void StageCurrent()
    {
        _host.StageSubmodule();
        Close(accepted: true);
    }

    [RelayCommand]
    private void OpenSubmodule() => _host.OpenSubmodule();

    [RelayCommand]
    private void CheckoutBranch()
    {
        if (!_host.CheckoutBranch(_localCommit!, _remoteCommit!))
        {
            return;
        }

        _host.StageSubmodule();
        Close(accepted: true);
    }
}

/// <summary>View model of the create worktree dialog (port of <c>FormCreateWorktree</c>).</summary>
public sealed partial class CreateWorktreeViewModel : DialogViewModel
{
    private static readonly char[] _invalidCharsInPath = Path.GetInvalidFileNameChars();

    private readonly IReadOnlyList<string> _existingBranches;
    private readonly string? _initialDirectoryPath;
    private readonly IGitBranchNameNormaliser _branchNameNormaliser;
    private readonly GitBranchNameOptions _branchNameOptions;
    private readonly bool _autoNormalise;
    private readonly Func<string, string, bool> _createWorktree;
    private readonly IFileDialogService _fileDialogs;

    /// <param name="existingBranches">The names of the local branches.</param>
    /// <param name="currentBranch">The checked out branch, which cannot be checked out in another worktree.</param>
    /// <param name="initialDirectoryPath">The path of the main worktree; new worktrees are suggested next to it.</param>
    /// <param name="createWorktree">
    ///  Creates the worktree in the directory, checking out the branch (or <c>-b</c> new branch) given as second argument;
    ///  returns whether it succeeded.
    /// </param>
    public CreateWorktreeViewModel(
        CreateWorktreeStrings strings,
        IReadOnlyList<string> existingBranches,
        string currentBranch,
        string? initialDirectoryPath,
        IGitBranchNameNormaliser branchNameNormaliser,
        GitBranchNameOptions branchNameOptions,
        bool autoNormalise,
        Func<string, string, bool> createWorktree,
        IFileDialogService fileDialogs)
    {
        Strings = strings;
        _existingBranches = existingBranches;
        _initialDirectoryPath = initialDirectoryPath;
        _branchNameNormaliser = branchNameNormaliser;
        _branchNameOptions = branchNameOptions;
        _autoNormalise = autoNormalise;
        _createWorktree = createWorktree;
        _fileDialogs = fileDialogs;

        Branches = [.. existingBranches.Where(name => name != currentBranch)];
        CanCheckoutExistingBranch = Branches.Count > 0;
        IsCheckoutExistingBranch = CanCheckoutExistingBranch;
        SelectedBranch = Branches.FirstOrDefault();
        UpdateWorktreeDirectory();
    }

    public CreateWorktreeStrings Strings { get; }

    /// <summary>The branches that can be checked out.</summary>
    public IReadOnlyList<string> Branches { get; }

    public bool CanCheckoutExistingBranch { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCreateNewBranch))]
    [NotifyCanExecuteChangedFor(nameof(CreateCommand))]
    public partial bool IsCheckoutExistingBranch { get; set; }

    public bool IsCreateNewBranch
    {
        get => !IsCheckoutExistingBranch;
        set => IsCheckoutExistingBranch = !value;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateCommand))]
    public partial string? SelectedBranch { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateCommand))]
    public partial string NewBranchName { get; set; } = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateCommand))]
    public partial string WorktreeDirectory { get; set; } = "";

    /// <summary>Whether the directory can hold a new worktree: it does not exist or is empty.</summary>
    public static bool IsTargetFolderValid(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            DirectoryInfo directoryInfo = new(path);
            return !directoryInfo.Exists || (!directoryInfo.EnumerateFiles().Any() && !directoryInfo.EnumerateDirectories().Any());
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    ///  Normalises <see cref="NewBranchName"/> if auto-normalisation is enabled.
    ///  Done when the name box loses focus, and before creating the worktree.
    /// </summary>
    public void NormaliseNewBranchName()
    {
        if (!_autoNormalise || !NewBranchName.Any(PathUtil.IsValidPathChar))
        {
            return;
        }

        NewBranchName = _branchNameNormaliser.Normalise(NewBranchName, _branchNameOptions);
    }

    partial void OnIsCheckoutExistingBranchChanged(bool value) => UpdateWorktreeDirectory();

    partial void OnSelectedBranchChanged(string? value) => UpdateWorktreeDirectory();

    partial void OnNewBranchNameChanged(string value) => UpdateWorktreeDirectory();

    private void UpdateWorktreeDirectory()
    {
        string branchName = IsCheckoutExistingBranch ? SelectedBranch ?? "" : NewBranchName;
        string normalized = string.Join("_", branchName.Split(_invalidCharsInPath, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        WorktreeDirectory = $"{_initialDirectoryPath}_{normalized}";
    }

    private bool CanCreate()
    {
        bool branchValid = IsCheckoutExistingBranch
            ? SelectedBranch is not null
            : !string.IsNullOrWhiteSpace(NewBranchName) && !_existingBranches.Contains(NewBranchName);
        return branchValid && IsTargetFolderValid(WorktreeDirectory);
    }

    [RelayCommand]
    private async Task BrowseAsync()
    {
        string? folder = await _fileDialogs.PickFolderAsync(WorktreeDirectory);
        if (folder is not null)
        {
            WorktreeDirectory = folder;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCreate))]
    private void Create()
    {
        // The branch name may not have lost focus yet (e.g. when triggered via the default button),
        // so normalise it here to avoid passing an invalid name (e.g. containing spaces) to git.
        if (IsCreateNewBranch)
        {
            NormaliseNewBranchName();
        }

        string branchOption = IsCreateNewBranch ? $"-b {NewBranchName}" : SelectedBranch!;
        if (_createWorktree(WorktreeDirectory, branchOption))
        {
            Close(accepted: true);
        }
    }
}

/// <summary>View model of the open local repository dialog (port of <c>FormOpenDirectory</c>).</summary>
public sealed partial class OpenDirectoryViewModel : DialogViewModel
{
    private readonly Func<string, bool> _tryOpen;
    private readonly IMessageBoxService _messageBoxes;
    private readonly IFileDialogService _fileDialogs;
    private readonly string _errorCaption;

    /// <param name="tryOpen">Opens the repository at the path; returns <see langword="false"/> if it is not a valid repository.</param>
    public OpenDirectoryViewModel(
        OpenDirectoryStrings strings,
        IReadOnlyList<string> directories,
        string errorCaption,
        Func<string, bool> tryOpen,
        IMessageBoxService messageBoxes,
        IFileDialogService fileDialogs)
    {
        Strings = strings;
        Directories = directories;
        _errorCaption = errorCaption;
        _tryOpen = tryOpen;
        _messageBoxes = messageBoxes;
        _fileDialogs = fileDialogs;
        Directory = directories.FirstOrDefault() ?? "";
    }

    public OpenDirectoryStrings Strings { get; }

    public IReadOnlyList<string> Directories { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoUpCommand))]
    public partial string Directory { get; set; }

    /// <summary>The suggested directories, as <c>FormOpenDirectory.GetDirectories</c>.</summary>
    public static IReadOnlyList<string> GetDirectories(
        string? defaultCloneDestination,
        string? currentWorkingDir,
        IEnumerable<string> recentRepositories,
        string? recentWorkingDir,
        string? homeDir)
    {
        List<string> directories = [];

        if (!string.IsNullOrWhiteSpace(defaultCloneDestination))
        {
            directories.Add(defaultCloneDestination.EnsureTrailingPathSeparator()!);
        }

        if (!string.IsNullOrWhiteSpace(currentWorkingDir))
        {
            DirectoryInfo di = new(currentWorkingDir);
            if (di.Parent is not null)
            {
                directories.Add(di.Parent.FullName.EnsureTrailingPathSeparator()!);
            }
        }

        directories.AddRange(recentRepositories);

        if (directories.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(recentWorkingDir))
            {
                directories.Add(recentWorkingDir.EnsureTrailingPathSeparator()!);
            }

            if (!string.IsNullOrWhiteSpace(homeDir))
            {
                directories.Add(homeDir.EnsureTrailingPathSeparator()!);
            }
        }

        return [.. directories.Distinct()];
    }

    private bool CanGoUp()
    {
        try
        {
            DirectoryInfo currentDirectory = new(Directory);
            return currentDirectory.Exists && currentDirectory.Parent is not null;
        }
        catch
        {
            return false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoUp))]
    private void GoUp()
    {
        DirectoryInfo? parent = new DirectoryInfo(Directory).Parent;
        if (parent is not null)
        {
            // FormOpenDirectory then types a separator to show the subdirectories for autocompletion.
            Directory = parent.FullName.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        }
    }

    [RelayCommand]
    private async Task BrowseAsync()
    {
        string? folder = await _fileDialogs.PickFolderAsync(Directory);
        if (!string.IsNullOrEmpty(folder))
        {
            Directory = folder;
            Open();
        }
    }

    [RelayCommand]
    private void Open()
    {
        Directory = Directory.Trim();
        if (_tryOpen(Directory))
        {
            Close(accepted: true);
            return;
        }

        _messageBoxes.ShowError(Strings.OpenFailed.Text, _errorCaption);
    }
}
