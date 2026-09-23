using CommunityToolkit.Mvvm.Input;
using GitUI.Presentation.Editor;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the .gitignore editor; ids match <c>FormGitIgnore</c>.</summary>
public sealed class GitIgnoreStrings : ViewStrings
{
    public GitIgnoreStrings()
        : base("FormGitIgnore")
    {
        Help = Add("label1", "Text", $"Specify filepatterns you want git to ignore.\n\nExample:\n{string.Join("\n", GitIgnoreEditorViewModel.DefaultIgnorePatterns)}");
        AddDefault = Add("AddDefault", "Text", "Add default ignores");
        AddPattern = Add("AddPattern", "Text", "Add pattern");
        Save = Add("Save", "Text", "Save");
        Cancel = Add("btnCancel", "Text", "Cancel");
        GenerateLink = Add("lnkGitIgnoreGenerate", "Text", "Generate a custom ignore file for git");
        PatternsLink = Add("lnkGitIgnorePatterns", "Text", "Example ignore patterns");
        NoWorkingDirCaption = Add("_gitignoreOnlyInWorkingDirSupportedCaption", "Text", "No working directory");
        SaveFileQuestionCaption = Add("_saveFileQuestionCaption", "Text", "Save changes?");
    }

    public TranslatedText Help { get; }

    public TranslatedText AddDefault { get; }

    public TranslatedText AddPattern { get; }

    public TranslatedText Save { get; }

    public TranslatedText Cancel { get; }

    public TranslatedText GenerateLink { get; }

    public TranslatedText PatternsLink { get; }

    public TranslatedText NoWorkingDirCaption { get; }

    public TranslatedText SaveFileQuestionCaption { get; }
}

/// <summary>Strings of the file the .gitignore editor edits (<c>IGitIgnoreDialogModel</c>).</summary>
public abstract class GitIgnoreFileStrings : ViewStrings
{
    protected GitIgnoreFileStrings(string category)
        : base(category)
    {
    }

    public TranslatedText Title { get; protected init; } = null!;

    public TranslatedText OnlyInWorkingDirSupported { get; protected init; } = null!;

    public TranslatedText CannotAccessFile { get; protected init; } = null!;

    public TranslatedText CannotAccessFileCaption { get; protected init; } = null!;

    public TranslatedText SaveFileQuestion { get; protected init; } = null!;
}

/// <summary>Strings of the .gitignore of the working directory; ids match <c>GitIgnoreModel</c>.</summary>
public sealed class GitIgnoreModelStrings : GitIgnoreFileStrings
{
    public GitIgnoreModelStrings()
        : base("GitIgnoreModel")
    {
        Title = Add("_editGitignoreTitle", "Text", "Edit .gitignore");
        OnlyInWorkingDirSupported = Add("_gitignoreOnlyInWorkingDirSupported", "Text", ".gitignore is only supported when there is a working directory.");
        CannotAccessFile = Add("_cannotAccessGitignore", "Text", "Failed to save .gitignore.\nCheck if file is accessible.");
        CannotAccessFileCaption = Add("_cannotAccessGitignoreCaption", "Text", "Failed to save .gitignore");
        SaveFileQuestion = Add("_saveFileQuestion", "Text", "Save changes to .gitignore?");
    }
}

/// <summary>Strings of the local excludes (<c>.git/info/exclude</c>); ids match <c>GitLocalExcludeModel</c>.</summary>
public sealed class GitLocalExcludeModelStrings : GitIgnoreFileStrings
{
    public GitLocalExcludeModelStrings()
        : base("GitLocalExcludeModel")
    {
        Title = Add("_editLocalExcludeTitle", "Text", "Edit .git/info/exclude");
        OnlyInWorkingDirSupported = Add("_localExcludeOnlyInWorkingDirSupported", "Text", ".git/info/exclude is only supported when there is a working directory.");
        CannotAccessFile = Add("_cannotAccessLocalExclude", "Text", "Failed to save .git/info/exclude.\nCheck if file is accessible.");
        CannotAccessFileCaption = Add("_cannotAccessLocalExcludeCaption", "Text", "Failed to save .git/info/exclude");
        SaveFileQuestion = Add("_saveFileQuestion", "Text", "Save changes to .git/info/exclude?");
    }
}

/// <summary>Operations of the .gitignore editor that need the host (the file system, other dialogs, the browser).</summary>
public interface IGitIgnoreEditorHost
{
    /// <summary>The content of the file, or <see langword="null"/> if it does not exist or cannot be read.</summary>
    string? Load();

    /// <summary>Writes the file (throws if it cannot be written).</summary>
    void Save(string text);

    /// <summary>The user's default patterns (<c>GitExtensions/DefaultIgnorePatterns.txt</c>), or <see langword="null"/> if not defined.</summary>
    IReadOnlyList<string>? LoadDefaultIgnorePatterns();

    /// <summary>Shows the dialog that adds a pattern to the file.</summary>
    void AddPattern();

    void OpenUrl(string url);
}

/// <summary>View model of the .gitignore / .git/info/exclude editor (port of <c>FormGitIgnore</c>).</summary>
public sealed partial class GitIgnoreEditorViewModel : DialogViewModel
{
    /// <summary>As <c>FormGitIgnore.DefaultIgnorePatterns</c>.</summary>
    internal static readonly string[] DefaultIgnorePatterns =
    [
        "#Ignore thumbnails created by Windows",
        "Thumbs.db",
        "#Ignore files built by Visual Studio",
        "*.obj",
        "*.exe",
        "*.pdb",
        "*.user",
        "*.aps",
        "*.pch",
        "*.vspscc",
        "*_i.c",
        "*_p.c",
        "*.ncb",
        "*.suo",
        "*.tlb",
        "*.tlh",
        "*.bak",
        "*.cache",
        "*.ilk",
        "*.log",
        "[Bb]in",
        "[Dd]ebug*/",
        "*.lib",
        "*.sbr",
        "obj/",
        "[Rr]elease*/",
        "_ReSharper*/",
        "[Tt]est[Rr]esult*",
        ".vs/",
        ".idea/",
        "#Nuget packages folder",
        "packages/"
    ];

    private readonly IGitIgnoreEditorHost _host;
    private readonly IMessageBoxService _messageBoxes;

    public GitIgnoreEditorViewModel(GitIgnoreStrings strings, GitIgnoreFileStrings fileStrings, IGitIgnoreEditorHost host, IMessageBoxService messageBoxes)
    {
        Strings = strings;
        FileStrings = fileStrings;
        _host = host;
        _messageBoxes = messageBoxes;
        Editor.Load(host.Load() ?? "");
    }

    public GitIgnoreStrings Strings { get; }

    public GitIgnoreFileStrings FileStrings { get; }

    public TextEditorViewModel Editor { get; } = new();

    /// <summary>As <c>FormGitIgnore.AddDefaultClick</c>: adds the default patterns that are not in the file yet.</summary>
    [RelayCommand]
    private void AddDefault()
    {
        IReadOnlyList<string> defaultIgnorePatterns = _host.LoadDefaultIgnorePatterns() ?? DefaultIgnorePatterns;
        string currentFileContent = Editor.Text;
        string[] patternsToAdd = [.. defaultIgnorePatterns.Except(currentFileContent.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries))];
        if (patternsToAdd.Length == 0)
        {
            return;
        }

        // Not a load: the file is changed.
        Editor.Text = $"{currentFileContent}{Environment.NewLine}{string.Join(Environment.NewLine, patternsToAdd)}{Environment.NewLine}";
    }

    /// <summary>As <c>FormGitIgnore.AddPattern_Click</c>: saves, adds a pattern in its dialog and reloads the file.</summary>
    [RelayCommand]
    private void AddPattern()
    {
        SaveFile();
        _host.AddPattern();
        if (_host.Load() is { } text)
        {
            Editor.Load(text);
        }
    }

    [RelayCommand]
    private void Save()
    {
        // As FormGitIgnore.SaveClick; closing asks again if saving failed.
        SaveFile();
        Close(accepted: true);
    }

    [RelayCommand]
    private void Cancel() => Close(accepted: false);

    [RelayCommand]
    private void OpenPatterns() => _host.OpenUrl("https://github.com/github/gitignore");

    [RelayCommand]
    private void OpenGenerator() => _host.OpenUrl("https://www.gitignore.io/");

    /// <summary>As <c>FormGitIgnore.SaveGitIgnore</c>: saves the changes, with a final new line.</summary>
    private bool SaveFile()
    {
        if (!Editor.HasChanges)
        {
            return false;
        }

        try
        {
            string text = Editor.Text;
            if (!text.EndsWith(Environment.NewLine))
            {
                text += Environment.NewLine;
            }

            _host.Save(text);
            Editor.MarkSaved();
            return true;
        }
        catch (Exception ex)
        {
            _messageBoxes.ShowError(FileStrings.CannotAccessFile.Text + Environment.NewLine + ex.Message, FileStrings.CannotAccessFileCaption.Text);
            return false;
        }
    }

    /// <summary>As <c>FormGitIgnoreFormClosing</c>: unsaved changes are saved, discarded, or keep the dialog open.</summary>
    public override bool CanClose()
    {
        if (!Editor.HasChanges)
        {
            return true;
        }

        return _messageBoxes.ConfirmWithCancel(FileStrings.SaveFileQuestion.Text, Strings.SaveFileQuestionCaption.Text) switch
        {
            true => SaveFile(),
            false => true,
            null => false,
        };
    }
}
