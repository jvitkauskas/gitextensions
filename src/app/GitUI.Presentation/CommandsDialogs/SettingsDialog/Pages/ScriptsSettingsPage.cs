using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitExtensions.Extensibility.Settings;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

/// <summary>Strings of the scripts settings; ids match <c>ScriptsSettingsPage</c>.</summary>
public sealed class ScriptsSettingsPageStrings : ViewStrings
{
    public ScriptsSettingsPageStrings()
        : base("ScriptsSettingsPage")
    {
        Title = Add("$this", "Text", "Scripts");
        ArgumentsHelp = Add("_scriptSettingsPageHelpDisplayArgumentsHelp", "Text", "Arguments help");
        ArgumentsHelpContent = Add("_scriptSettingsPageHelpDisplayContent", "Text", """
            Use {option} for normal replacement.
            Use {{option}} for quoted replacement.

            User inputs:
            {UserInput}
            {UserInput:a popup label}
            {UserInput:a popup label=a default value}
            {UserInput:a popup label=a default value using {sLocalBranch}}
            {UserFiles}

            Working directory:
            {WorkingDir}

            Repository:
            {RepoName}

            Selected commits:
            {sHashes}

            Selected revision:
            {sTag}
            {sBranch}
            {sLocalBranch}
            {sRemoteBranch}
            {sRemoteBranchName}   (without the remote's name)
            {sRemote}
            {sRemoteUrl}
            {sRemotePathFromUrl}
            {sHash}
            {sMessage}
            {sSubject}
            {sAuthor}
            {sCommitter}
            {sAuthorDate}
            {sCommitDate}

            Currently checked out revision:
            {HEAD}   (checked out branch name or checked out commit hash)
            {cTag}
            {cBranch}
            {cLocalBranch}
            {cRemoteBranch}
            {cRemoteBranchName}   (without the remote's name)
            {cHash}
            {cMessage}
            {cSubject}
            {cAuthor}
            {cCommitter}
            {cAuthorDate}
            {cCommitDate}
            {cDefaultRemote}
            {cDefaultRemoteUrl}
            {cDefaultRemotePathFromUrl}

            Diff selection:
            {SelectedRelativePaths}   (relative paths as they were in the selected commit)
            {LineNumber}
            {ColumnNumber}
            """.ReplaceLineEndings("\n"));
    }

    public TranslatedText Title { get; }

    public TranslatedText ArgumentsHelp { get; }

    public TranslatedText ArgumentsHelpContent { get; }
}

/// <summary>
///  A script being edited (<c>ScriptInfoProxy</c>, whose properties the property grid shows): the event as the name of its
///  <c>ScriptEvent</c>.
/// </summary>
public sealed partial class ScriptItem : ObservableObject
{
    public int HotkeyCommandIdentifier { get; set; }

    [ObservableProperty]
    public partial string? Name { get; set; }

    [ObservableProperty]
    public partial bool Enabled { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CommandLine))]
    public partial string? Command { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CommandLine))]
    public partial string? Arguments { get; set; }

    [ObservableProperty]
    public partial string OnEvent { get; set; } = "None";

    /// <summary>The name of an image of the application.</summary>
    [ObservableProperty]
    public partial string? Icon { get; set; }

    /// <summary>An .ico file, or a file whose associated icon is used.</summary>
    [ObservableProperty]
    public partial string? IconFilePath { get; set; }

    [ObservableProperty]
    public partial bool AskConfirmation { get; set; }

    [ObservableProperty]
    public partial bool RunInBackground { get; set; }

    [ObservableProperty]
    public partial bool IsPowerShell { get; set; }

    [ObservableProperty]
    public partial bool AddToRevisionGridContextMenu { get; set; }

    /// <summary>The image shown in the list (<c>GetIconImageKey</c>), as PNG data; set by the page.</summary>
    [ObservableProperty]
    public partial byte[]? Image { get; set; }

    /// <summary>The command column and the tooltip of the list.</summary>
    public string CommandLine => $"{Command} {Arguments}";
}

/// <summary>An image a script can show (an image of <c>GitUI.Properties.Images</c>).</summary>
public sealed record ScriptIconChoice(string Name, byte[] Image);

/// <summary>The scripts (<c>IScriptsManager</c>) and their images.</summary>
public interface IScriptsSettingsHost
{
    /// <summary>The smallest id of a user script (<c>ScriptsManager.MinimumUserScriptID</c>).</summary>
    int MinimumUserScriptId { get; }

    /// <summary>The names of the events (<c>ScriptEvent</c>).</summary>
    IReadOnlyList<string> EventNames { get; }

    IReadOnlyList<ScriptItem> LoadScripts();

    /// <summary>Replaces the scripts and saves them in <c>AppSettings.OwnScripts</c>.</summary>
    void SaveScripts(IReadOnlyList<ScriptItem> scripts);

    /// <summary>The images of the application, sorted by name (the embedded icons).</summary>
    IReadOnlyList<ScriptIconChoice> LoadIcons();

    /// <summary>The icon of a file (an .ico file, or its associated icon), if any.</summary>
    byte[]? GetFileIcon(string path);
}

/// <summary>Port of <c>ScriptsSettingsPage</c> (global settings): the list of the scripts and the properties of the selected one.</summary>
public sealed partial class ScriptsSettingsPageViewModel(ScriptsSettingsPageStrings strings, IScriptsSettingsHost host) : SettingsPageViewModel
{
    private IReadOnlyList<ScriptIconChoice>? _icons;

    public ScriptsSettingsPageStrings Strings { get; } = strings;

    public override string Title => Strings.Title.Text;

    public override string PageName => "ScriptsSettingsPage";

    public override IEnumerable<string> SearchKeywords => [Strings.Title.Text, Strings.ArgumentsHelp.Text];

    public ObservableCollection<ScriptItem> Scripts { get; } = [];

    public IReadOnlyList<string> EventNames => host.EventNames;

    /// <summary>The images for <see cref="ScriptItem.Icon"/>, loaded when the page is shown.</summary>
    public IReadOnlyList<ScriptIconChoice> Icons
    {
        get => _icons ?? [];
        private set => SetProperty(ref _icons, value);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedScript), nameof(SelectedIcon))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand), nameof(MoveUpCommand), nameof(MoveDownCommand))]
    public partial ScriptItem? SelectedScript { get; set; }

    public bool HasSelectedScript => SelectedScript is not null;

    /// <summary>The image chosen for the selected script.</summary>
    public ScriptIconChoice? SelectedIcon
    {
        get => SelectedScript?.Icon is string name ? Icons.FirstOrDefault(icon => string.Equals(icon.Name, name, StringComparison.OrdinalIgnoreCase)) : null;
        set
        {
            if (SelectedScript is ScriptItem script && !string.Equals(script.Icon, value?.Name, StringComparison.OrdinalIgnoreCase))
            {
                script.Icon = value?.Name;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>The window's pickers, set by the view.</summary>
    public IFileDialogService? FileDialogs { get; set; }

    /// <summary>Raised to show the arguments help (<c>SimpleHelpDisplayDialog</c>), with the title and the content.</summary>
    public event EventHandler<(string Title, string Content)>? ArgumentsHelpRequested;

    /// <summary>As <c>OnPageShown</c>: the images are loaded once.</summary>
    public override void OnPageShown()
    {
        if (_icons is null)
        {
            Icons = host.LoadIcons();
            foreach (ScriptItem script in Scripts)
            {
                ClearUnknownIcon(script);
                UpdateImage(script);
            }

            OnPropertyChanged(nameof(SelectedIcon));
        }
    }

    protected override void SettingsToPage(SettingsSource? settings)
    {
        foreach (ScriptItem script in Scripts)
        {
            script.PropertyChanged -= OnScriptChanged;
        }

        Scripts.Clear();
        foreach (ScriptItem script in host.LoadScripts())
        {
            AddScript(script);
        }

        SelectedScript = Scripts.FirstOrDefault();
        base.SettingsToPage(settings);
    }

    protected override void PageToSettings(SettingsSource? settings)
    {
        host.SaveScripts([.. Scripts]);
        base.PageToSettings(settings);
    }

    /// <summary>As <c>btnAdd_Click</c>: a new enabled script with the next id.</summary>
    [RelayCommand]
    private void Add()
    {
        ScriptItem script = new()
        {
            HotkeyCommandIdentifier = Math.Max(host.MinimumUserScriptId, Scripts.Select(s => s.HotkeyCommandIdentifier).DefaultIfEmpty(0).Max()) + 1,
            Name = "<New Script>",
            Enabled = true,
        };
        AddScript(script);
        SelectedScript = script;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedScript))]
    private void Delete()
    {
        ScriptItem script = SelectedScript!;
        script.PropertyChanged -= OnScriptChanged;
        Scripts.Remove(script);
        SelectedScript = Scripts.FirstOrDefault();
    }

    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private void MoveUp() => Move(-1);

    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private void MoveDown() => Move(+1);

    [RelayCommand]
    private void ShowArgumentsHelp()
        => ArgumentsHelpRequested?.Invoke(this, (Strings.ArgumentsHelp.Text, Strings.ArgumentsHelpContent.Text.Replace("\n", Environment.NewLine)));

    /// <summary>As the <c>ExecutableFileNameEditor</c> of the command.</summary>
    [RelayCommand]
    private async Task BrowseCommandAsync()
    {
        if (SelectedScript is ScriptItem script && FileDialogs is not null
            && (await FileDialogs.PickFilesAsync(allowMultiple: false, GetDirectory(script.Command))).FirstOrDefault() is string path)
        {
            script.Command = path;
        }
    }

    /// <summary>As the <c>FileNameEditor</c> of the icon file.</summary>
    [RelayCommand]
    private async Task BrowseIconFileAsync()
    {
        if (SelectedScript is ScriptItem script && FileDialogs is not null
            && (await FileDialogs.PickFilesAsync(allowMultiple: false, GetDirectory(script.IconFilePath))).FirstOrDefault() is string path)
        {
            script.IconFilePath = path;
        }
    }

    private bool CanMoveUp() => SelectedScript is not null && Scripts.IndexOf(SelectedScript) > 0;

    private bool CanMoveDown() => SelectedScript is not null && Scripts.IndexOf(SelectedScript) < Scripts.Count - 1;

    private void Move(int offset)
    {
        ScriptItem script = SelectedScript!;
        int index = Scripts.IndexOf(script);
        Scripts.Move(index, Math.Clamp(index + offset, 0, Scripts.Count - 1));
        SelectedScript = script;
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
    }

    private void AddScript(ScriptItem script)
    {
        ClearUnknownIcon(script);
        UpdateImage(script);
        script.PropertyChanged += OnScriptChanged;
        Scripts.Add(script);
    }

    private void OnScriptChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is ScriptItem script && e.PropertyName is nameof(ScriptItem.Icon) or nameof(ScriptItem.IconFilePath))
        {
            UpdateImage(script);
            if (script == SelectedScript)
            {
                OnPropertyChanged(nameof(SelectedIcon));
            }
        }
    }

    // As BindScripts: an unknown image is cleared (once the images are known).
    private void ClearUnknownIcon(ScriptItem script)
    {
        if (_icons is not null && script.Icon is string name && !_icons.Any(icon => string.Equals(icon.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            script.Icon = null;
        }
    }

    // As GetIconImageKey: the icon of the file if it exists, else the image of the application.
    private void UpdateImage(ScriptItem script)
    {
        script.Image = (!string.IsNullOrEmpty(script.IconFilePath) && File.Exists(script.IconFilePath) ? host.GetFileIcon(script.IconFilePath) : null)
            ?? (script.Icon is string name ? _icons?.FirstOrDefault(icon => string.Equals(icon.Name, name, StringComparison.OrdinalIgnoreCase))?.Image : null);
    }

    private static string? GetDirectory(string? path)
    {
        try
        {
            return string.IsNullOrWhiteSpace(path) ? null : Path.GetDirectoryName(path);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    partial void OnSelectedScriptChanged(ScriptItem? value)
    {
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
    }
}
