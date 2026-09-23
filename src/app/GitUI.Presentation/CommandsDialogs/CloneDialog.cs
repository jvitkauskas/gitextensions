using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands;
using GitExtensions.Extensibility;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the clone dialog; ids match <c>FormClone</c>.</summary>
public sealed class CloneStrings : ViewStrings
{
    public CloneStrings()
        : base("FormClone")
    {
        Title = Add("$this", "Text", "Clone");
        RepositoryToClone = Add("repositoryLabel", "Text", "Repository to &clone:");
        FromBrowse = Add("FromBrowse", "Text", "&Browse");
        Destination = Add("destinationLabel", "Text", "&Destination:");
        ToBrowse = Add("ToBrowse", "Text", "B&rowse");
        Subdirectory = Add("subdirectoryLabel", "Text", "&Subdirectory to create:");
        Branch = Add("brachLabel", "Text", "Br&anch:");
        RepositoryType = Add("groupBox1", "Text", "Repository type");
        PersonalRepository = Add("PersonalRepository", "Text", "&Personal repository");
        CentralRepository = Add("CentralRepository", "Text", "P&ublic repository, no working directory  (--bare)");
        InitializeAllSubmodules = Add("cbIntializeAllSubmodules", "Text", "Initialize all submodules");
        DownloadFullHistory = Add("cbDownloadFullHistory", "Text", "Download full &history");
        DownloadFullHistoryHint = Add(
            "cbDownloadFullHistory",
            "ttHints",
            "The default Git behavior is to download all historical revisions.\r\nIf you turn this off, we'll only download the latest revision for all branches.\r\n\r\nActual command line (if unchecked): --depth 1 --no-single-branch");
        LoadSshKey = Add("LoadSSHKey", "Text", "&Load SSH key");
        CloneButton = Add("Ok", "Text", "Clone");
        InfoNewRepositoryLocation = Add("_infoNewRepositoryLocation", "Text", "The repository will be cloned to a new directory located here:" + Environment.NewLine + "{0}");
        InfoDirectoryExists = Add("_infoDirectoryExists", "Text", "(Directory already exists)");
        InfoDirectoryNew = Add("_infoDirectoryNew", "Text", "(New directory)");
        QuestionOpenRepo = Add("_questionOpenRepo", "Text", "The repository has been cloned successfully." + Environment.NewLine + "Do you want to open the new repository \"{0}\" now?");
        QuestionOpenRepoCaption = Add("_questionOpenRepoCaption", "Text", "Open");
        BranchDefaultRemoteHead = Add("_branchDefaultRemoteHead", "Text", "(default: remote HEAD)");
        BranchNone = Add("_branchNone", "Text", "(none: don't checkout after clone)");
        ErrorDestinationNotSupplied = Add("_errorDestinationNotSupplied", "Text", "You need to specify destination folder.");
        ErrorDestinationNotRooted = Add("_errorDestinationNotRooted", "Text", "Destination folder must be an absolute path.");
        ErrorCloneFailed = Add("_errorCloneFailed", "Text", "Clone Failed");
    }

    public TranslatedText Title { get; }

    public TranslatedText RepositoryToClone { get; }

    public TranslatedText FromBrowse { get; }

    public TranslatedText Destination { get; }

    public TranslatedText ToBrowse { get; }

    public TranslatedText Subdirectory { get; }

    public TranslatedText Branch { get; }

    public TranslatedText RepositoryType { get; }

    public TranslatedText PersonalRepository { get; }

    public TranslatedText CentralRepository { get; }

    public TranslatedText InitializeAllSubmodules { get; }

    public TranslatedText DownloadFullHistory { get; }

    public TranslatedText DownloadFullHistoryHint { get; }

    public TranslatedText LoadSshKey { get; }

    public TranslatedText CloneButton { get; }

    public TranslatedText InfoNewRepositoryLocation { get; }

    public TranslatedText InfoDirectoryExists { get; }

    public TranslatedText InfoDirectoryNew { get; }

    public TranslatedText QuestionOpenRepo { get; }

    public TranslatedText QuestionOpenRepoCaption { get; }

    public TranslatedText BranchDefaultRemoteHead { get; }

    public TranslatedText BranchNone { get; }

    public TranslatedText ErrorDestinationNotSupplied { get; }

    public TranslatedText ErrorDestinationNotRooted { get; }

    public TranslatedText ErrorCloneFailed { get; }
}

/// <summary>A clone as chosen in the clone dialog.</summary>
/// <param name="Branch">The branch to check out: "" for the remote HEAD, <see langword="null"/> for none.</param>
/// <param name="Depth">The history depth for a shallow clone, <see langword="null"/> for the full history.</param>
public sealed record CloneRequest(string From, string Destination, bool Central, bool InitializeSubmodules, string? Branch, int? Depth, bool? SingleBranch, string? PuttySshKey);

/// <summary>Operations of the clone dialog that need the host (git, SSH keys, opening the repository).</summary>
public interface ICloneHost
{
    /// <summary>
    ///  Lists (in the background) the branches of the repository to clone and reports their names on the UI thread;
    ///  authentication problems are resolved with the user first.
    /// </summary>
    void LoadBranches(string from, Action<IReadOnlyList<string>> report);

    /// <summary>Stops listing branches.</summary>
    void CancelLoadingBranches();

    /// <summary>Lets the user pick a PuTTY key and loads it; returns its path, or <see langword="null"/>.</summary>
    string? BrowseAndLoadSshKey();

    /// <summary>Clones in the progress dialog, then offers to open the repository; returns whether it succeeded.</summary>
    bool Clone(CloneRequest request);
}

/// <summary>View model of the clone dialog (port of <c>FormClone</c>).</summary>
public sealed partial class CloneViewModel : DialogViewModel
{
    private readonly ICloneHost _host;
    private readonly IMessageBoxService _messageBoxes;
    private readonly IFileDialogService _fileDialogs;
    private readonly IReadOnlyList<string> _defaultBranchItems;
    private string? _puttySshKey;

    /// <param name="canLoadSshKey">Whether PuTTY is used for SSH, so that a key can be loaded.</param>
    public CloneViewModel(
        CloneStrings strings,
        IReadOnlyList<string> recentRepositories,
        IReadOnlyList<string> recentDestinations,
        string from,
        string destination,
        bool canLoadSshKey,
        ICloneHost host,
        IMessageBoxService messageBoxes,
        IFileDialogService fileDialogs)
    {
        Strings = strings;
        RecentRepositories = recentRepositories;
        RecentDestinations = recentDestinations;
        CanLoadSshKey = canLoadSshKey;
        _host = host;
        _messageBoxes = messageBoxes;
        _fileDialogs = fileDialogs;
        _defaultBranchItems = [strings.BranchDefaultRemoteHead.Text, strings.BranchNone.Text];
        Branches = _defaultBranchItems;
        Branch = _defaultBranchItems[0];
        Destination = destination;
        From = from;
        UpdateInfo();
    }

    public CloneStrings Strings { get; }

    public IReadOnlyList<string> RecentRepositories { get; }

    public IReadOnlyList<string> RecentDestinations { get; }

    public bool CanLoadSshKey { get; }

    /// <summary>The URL or path of the repository to clone.</summary>
    [ObservableProperty]
    public partial string From { get; set; } = "";

    [ObservableProperty]
    public partial string Destination { get; set; } = "";

    [ObservableProperty]
    public partial string NewDirectory { get; set; } = "";

    [ObservableProperty]
    public partial IReadOnlyList<string> Branches { get; private set; }

    [ObservableProperty]
    public partial string Branch { get; set; }

    [ObservableProperty]
    public partial bool IsCentral { get; set; }

    [ObservableProperty]
    public partial bool InitializeSubmodules { get; set; } = true;

    [ObservableProperty]
    public partial bool DownloadFullHistory { get; set; } = true;

    /// <summary>Where the repository will be cloned to.</summary>
    [ObservableProperty]
    public partial string InfoText { get; private set; } = "";

    /// <summary>Whether <see cref="InfoText"/> is a problem (incomplete destination, or a directory that is not empty).</summary>
    [ObservableProperty]
    public partial bool IsInfoWarning { get; private set; }

    partial void OnFromChanged(string value)
    {
        // As FormClone.FromTextUpdate.
        string repositoryName = PathUtil.GetRepositoryName(value);
        if (repositoryName != "")
        {
            NewDirectory = repositoryName;
        }

        Branches = _defaultBranchItems;
        if (!Branches.Contains(Branch))
        {
            Branch = _defaultBranchItems[0];
        }

        UpdateInfo();
    }

    partial void OnDestinationChanged(string value) => UpdateInfo();

    partial void OnNewDirectoryChanged(string value) => UpdateInfo();

    private void UpdateInfo()
    {
        // As FormClone.ToTextUpdate.
        bool destinationUnfilled = string.IsNullOrEmpty(Destination) || Destination.AsSpan().IndexOfAny(Delimiters.InvalidPathCharsSearchValues) >= 0;
        bool subDirectoryUnfilled = string.IsNullOrEmpty(NewDirectory) || NewDirectory.AsSpan().IndexOfAny(Delimiters.InvalidPathCharsSearchValues) >= 0;

        string destinationDirectory = destinationUnfilled ? $"[{Strings.Destination.PlainText}]" : Destination;
        string destinationSubDirectory = subDirectoryUnfilled ? $"[{Strings.Subdirectory.PlainText}]" : NewDirectory;
        string destinationPath = Path.Combine(destinationDirectory, destinationSubDirectory);
        string info = string.Format(Strings.InfoNewRepositoryLocation.Text, destinationPath);

        if (destinationUnfilled || subDirectoryUnfilled)
        {
            InfoText = info;
            IsInfoWarning = true;
        }
        else if (Directory.Exists(destinationPath) && Directory.EnumerateFileSystemEntries(destinationPath).Any())
        {
            InfoText = $"{info} {Strings.InfoDirectoryExists.Text}";
            IsInfoWarning = true;
        }
        else
        {
            InfoText = $"{info} {Strings.InfoDirectoryNew.Text}";
            IsInfoWarning = false;
        }
    }

    /// <summary>Lists the branches of the repository to clone; done when the branch list drops down.</summary>
    [RelayCommand]
    private void LoadBranches()
    {
        string from = From;
        _host.LoadBranches(from, branches =>
        {
            if (From != from)
            {
                return;
            }

            string branch = Branch;
            Branches = [.. _defaultBranchItems, .. branches];
            Branch = Branches.Contains(branch) ? branch : _defaultBranchItems[0];
        });
    }

    [RelayCommand]
    private async Task BrowseFromAsync()
    {
        string? folder = await _fileDialogs.PickFolderAsync(From);
        if (folder is not null)
        {
            From = folder;
        }
    }

    [RelayCommand]
    private async Task BrowseDestinationAsync()
    {
        string? folder = await _fileDialogs.PickFolderAsync(Destination);
        if (folder is not null)
        {
            Destination = folder;
        }
    }

    [RelayCommand]
    private void LoadSshKey() => _puttySshKey = _host.BrowseAndLoadSshKey();

    [RelayCommand]
    private void Clone()
    {
        _host.CancelLoadingBranches();

        if (string.IsNullOrWhiteSpace(Destination))
        {
            _messageBoxes.ShowError(Strings.ErrorDestinationNotSupplied.Text, Strings.ErrorCloneFailed.Text);
            return;
        }

        if (!Path.IsPathRooted(Destination))
        {
            _messageBoxes.ShowError(Strings.ErrorDestinationNotRooted.Text, Strings.ErrorCloneFailed.Text);
            return;
        }

        string destination;
        try
        {
            // This fails if the path is invalid.
            destination = PathUtil.Resolve(Path.Combine(Destination, NewDirectory));
        }
        catch (Exception ex)
        {
            _messageBoxes.ShowError("Exception: " + ex.Message, Strings.ErrorCloneFailed.Text);
            return;
        }

        // A shallow clone with all branches, so that switching branches works as after a full clone (see FormClone.OkClick).
        int? depth = DownloadFullHistory ? null : 1;
        bool? singleBranch = DownloadFullHistory ? null : false;

        string? branch = Branch == Strings.BranchDefaultRemoteHead.Text ? ""
            : Branch == Strings.BranchNone.Text ? null
            : Branch;

        if (_host.Clone(new CloneRequest(From, destination, IsCentral, InitializeSubmodules, branch, depth, singleBranch, _puttySshKey)))
        {
            Close(accepted: true);
        }
    }
}
