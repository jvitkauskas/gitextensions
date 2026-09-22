using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Services;

namespace GitUI.Presentation.ScriptsEngine;

/// <summary>
///  View model of the prompt for a script's <c>{UserInput}</c> (port of <c>SimplePrompt</c>, which is not translated).
/// </summary>
public sealed partial class SimplePromptViewModel : DialogViewModel
{
    public SimplePromptViewModel(string? title, string? label, string? defaultValue)
    {
        Title = string.IsNullOrWhiteSpace(title) ? "User input" : title;
        Label = string.IsNullOrWhiteSpace(label) ? "Please specify your input:" : $"{label}:";
        Input = defaultValue ?? "";
    }

    public string Title { get; }

    public string Label { get; }

    public string OkText => "_OK";

    [ObservableProperty]
    public partial string Input { get; set; }

    /// <summary>The accepted input.</summary>
    public string UserInput { get; private set; } = "";

    [RelayCommand]
    private void Ok()
    {
        UserInput = Input;
        Close(accepted: true);
    }
}

/// <summary>View model of the prompt for a script's <c>{SelectedFiles}</c>-like file input (port of <c>FormFilePrompt</c>).</summary>
public sealed partial class FilePromptViewModel(FilePromptStrings strings, IFileDialogService fileDialogs) : DialogViewModel
{
    public FilePromptStrings Strings { get; } = strings;

    [ObservableProperty]
    public partial string FilePath { get; set; } = "";

    /// <summary>The accepted input.</summary>
    public string UserInput { get; private set; } = "";

    [RelayCommand]
    private async Task BrowseAsync()
    {
        IReadOnlyList<string> files = await fileDialogs.PickFilesAsync(allowMultiple: true, startDirectory: ".");
        if (files.Count > 0)
        {
            FilePath = string.Join(" ", files.Select(file => file.Quote()));
        }
    }

    [RelayCommand]
    private void Ok()
    {
        if (string.IsNullOrEmpty(FilePath))
        {
            Close(accepted: false);
            return;
        }

        UserInput = FilePath;
        Close(accepted: true);
    }
}
