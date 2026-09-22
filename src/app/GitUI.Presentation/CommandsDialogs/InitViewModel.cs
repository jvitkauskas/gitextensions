using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitUI.Presentation.Services;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Operations of the create repository dialog that need the host (file system, git, repository history).</summary>
public interface IInitRepositoryHost
{
    bool FileExists(string path);

    /// <summary>Creates the directory if needed, runs <c>git init</c> and returns git's output.</summary>
    string Init(string directory, bool central);

    /// <summary>Opens the new repository in the application and adds it to the recent repositories.</summary>
    void OnRepositoryCreated(string directory);
}

/// <summary>View model of the create repository dialog (port of <c>FormInit</c>).</summary>
public sealed partial class InitViewModel : DialogViewModel
{
    private readonly IInitRepositoryHost _host;
    private readonly IMessageBoxService _messageBoxes;
    private readonly IFileDialogService _fileDialogs;
    private readonly string _errorCaption;

    public InitViewModel(
        InitStrings strings,
        IReadOnlyList<string> recentDirectories,
        string initialDirectory,
        string errorCaption,
        IInitRepositoryHost host,
        IMessageBoxService messageBoxes,
        IFileDialogService fileDialogs)
    {
        Strings = strings;
        RecentDirectories = recentDirectories;
        Directory = initialDirectory;
        _errorCaption = errorCaption;
        _host = host;
        _messageBoxes = messageBoxes;
        _fileDialogs = fileDialogs;
    }

    public InitStrings Strings { get; }

    public IReadOnlyList<string> RecentDirectories { get; }

    [ObservableProperty]
    public partial string Directory { get; set; }

    [ObservableProperty]
    public partial bool IsCentral { get; set; }

    /// <summary>
    ///  Whether <paramref name="path"/> is a usable absolute directory path (as <c>FormInit.IsRootedDirectoryPath</c>).
    /// </summary>
    public static bool IsRootedDirectoryPath(string? path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            // Throws for invalid paths (e.g. with special characters) or missing permissions.
            _ = new DirectoryInfo(path);
            return Path.IsPathRooted(path.Trim());
        }
        catch (Exception)
        {
            return false;
        }
    }

    [RelayCommand]
    private async Task BrowseAsync()
    {
        string? folder = await _fileDialogs.PickFolderAsync(IsRootedDirectoryPath(Directory) ? Directory : null);
        if (folder is not null)
        {
            Directory = folder;
        }
    }

    [RelayCommand]
    private void Create()
    {
        string directory = Directory;
        if (!IsRootedDirectoryPath(directory))
        {
            _messageBoxes.ShowError(Strings.ChooseDirectory.Text, Strings.ChooseDirectoryCaption.Text);
            return;
        }

        if (_host.FileExists(directory))
        {
            _messageBoxes.ShowError(Strings.ChooseDirectoryNotFile.Text, _errorCaption);
            return;
        }

        _messageBoxes.ShowInformation(_host.Init(directory, IsCentral), Strings.InitCaption.Text);
        _host.OnRepositoryCreated(directory);
        Close(accepted: true);
    }
}
