using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitUI.Presentation;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitExtensions.Plugins.Gource;

/// <summary>Strings of the Avalonia port of <see cref="GourceStart"/>; ids match the form.</summary>
public sealed class GourceStartStrings : ViewStrings
{
    public GourceStartStrings()
        : base(nameof(GourceStart))
    {
        Title = Add("$this", "Text", "Gource");
        PathToGource = Add("label1", "Text", "Path to Gource");
        Repository = Add("label2", "Text", "Repository");
        Arguments = Add("ArgumentsLabel", "Text", "Arguments");
        GourceBrowse = Add("GourceBrowse", "Text", "Browse");
        WorkingDirBrowse = Add("WorkingDirBrowse", "Text", "Browse");
        Start = Add("button1", "Text", "Start");
        GourceProject = Add("linkLabel1", "Text", "Gource project");
        GourceCommandLine = Add("linkLabel2", "Text", "Gource command line");
    }

    public TranslatedText Title { get; }

    public TranslatedText PathToGource { get; }

    public TranslatedText Repository { get; }

    public TranslatedText Arguments { get; }

    public TranslatedText GourceBrowse { get; }

    public TranslatedText WorkingDirBrowse { get; }

    public TranslatedText Start { get; }

    public TranslatedText GourceProject { get; }

    public TranslatedText GourceCommandLine { get; }
}

/// <summary>What the Avalonia port of <see cref="GourceStart"/> needs from the application.</summary>
public interface IGourceStartHost
{
    /// <summary>As <c>GourceStart.LoadAvatarsAsync</c>: saves the avatars of the authors; returns their directory.</summary>
    Task<string> LoadAvatarsAsync();

    /// <summary>Starts Gource, detached (as <c>RunRealCmdDetached</c>); throws if it cannot be started.</summary>
    void StartDetached(string command, string arguments, string workingDirectory);

    void OpenUrl(string url);
}

/// <summary>View model of the Avalonia port of <see cref="GourceStart"/>.</summary>
public sealed partial class GourceStartViewModel : DialogViewModel
{
    // Not translated, as in GourceStart.
    public const string CannotFindGource = "Cannot find Gource.\nPlease download Gource and set the correct path.";
    public const string GourceProjectUrl = "https://github.com/acaudwell/Gource/";
    public const string GourceCommandLineUrl = "https://github.com/acaudwell/Gource#readme";

    private readonly IGourceStartHost _host;
    private readonly IMessageBoxService _messageBoxes;
    private readonly IFileDialogService _fileDialogs;
    private readonly string _errorCaption;

    /// <param name="errorCaption">The caption of the error messages (<c>TranslatedStrings.Error</c>).</param>
    public GourceStartViewModel(
        GourceStartStrings strings,
        string pathToGource,
        string workingDirectory,
        string gourceArguments,
        IGourceStartHost host,
        IMessageBoxService messageBoxes,
        IFileDialogService fileDialogs,
        string errorCaption)
    {
        Strings = strings;
        _host = host;
        _messageBoxes = messageBoxes;
        _fileDialogs = fileDialogs;
        _errorCaption = errorCaption;

        GourcePath = pathToGource;
        WorkingDirectory = workingDirectory;
        Arguments = gourceArguments;
        PathToGource = pathToGource;
        GourceArguments = gourceArguments;
    }

    public GourceStartStrings Strings { get; }

    [ObservableProperty]
    public partial string GourcePath { get; set; }

    [ObservableProperty]
    public partial string WorkingDirectory { get; set; }

    [ObservableProperty]
    public partial string Arguments { get; set; }

    /// <summary>The path to save in the settings of the plugin: the one Gource was started with, else the one given.</summary>
    public string PathToGource { get; private set; }

    /// <summary>The arguments to save in the settings of the plugin: the ones Gource was started with, else the ones given.</summary>
    public string GourceArguments { get; private set; }

    /// <summary>As <c>GourceStart.Button1Click</c>.</summary>
    [RelayCommand]
    private async Task StartAsync()
    {
        if (!File.Exists(GourcePath))
        {
            _messageBoxes.ShowError(CannotFindGource, _errorCaption);
            return;
        }

        GourceArguments = Arguments;
        string gourceAvatarsDir = GourceArguments.Contains("$(AVATARS)") ? await _host.LoadAvatarsAsync() : "";
        string arguments = GourceArguments.Replace("$(AVATARS)", gourceAvatarsDir);
        PathToGource = GourcePath;

        try
        {
            _host.StartDetached(GourcePath, arguments, WorkingDirectory);
        }
        catch (Exception ex)
        {
            _messageBoxes.ShowError(ex.Message, _errorCaption);
        }

        Close(accepted: true);
    }

    /// <summary>As <c>GourceBrowseClick</c>.</summary>
    [RelayCommand]
    private async Task BrowseGourceAsync()
    {
        string? startDirectory = string.IsNullOrEmpty(GourcePath) ? null : Path.GetDirectoryName(GourcePath);
        if (await _fileDialogs.PickFileAsync(Strings.PathToGource.PlainText, "Gource (gource.exe)", "gource.exe", startDirectory) is { } path)
        {
            GourcePath = path;
        }
    }

    /// <summary>As <c>WorkingDirBrowseClick</c>.</summary>
    [RelayCommand]
    private async Task BrowseWorkingDirectoryAsync()
    {
        if (await _fileDialogs.PickFolderAsync(WorkingDirectory) is { } path)
        {
            WorkingDirectory = path;
        }
    }

    [RelayCommand]
    private void OpenGourceProject() => _host.OpenUrl(GourceProjectUrl);

    [RelayCommand]
    private void OpenGourceCommandLine() => _host.OpenUrl(GourceCommandLineUrl);
}
