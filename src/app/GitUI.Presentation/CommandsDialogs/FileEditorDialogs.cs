using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitUI.Presentation.Editor;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of an editor of a file in the working directory (<c>FormGitAttributes</c>, <c>FormMailMap</c>).</summary>
public abstract class RepoFileEditorStrings : ViewStrings
{
    protected RepoFileEditorStrings(string category)
        : base(category)
    {
    }

    public TranslatedText Title { get; protected init; } = null!;

    public TranslatedText Help { get; protected init; } = null!;

    public TranslatedText Save { get; protected init; } = null!;

    public TranslatedText CannotAccess { get; protected init; } = null!;

    public TranslatedText CannotAccessCaption { get; protected init; } = null!;

    public TranslatedText NoWorkingDir { get; protected init; } = null!;

    public TranslatedText NoWorkingDirCaption { get; protected init; } = null!;

    public TranslatedText SaveFileQuestion { get; protected init; } = null!;

    public TranslatedText SaveFileQuestionCaption { get; protected init; } = null!;
}

/// <summary>Strings of the .gitattributes editor; ids match <c>FormGitAttributes</c>.</summary>
public sealed class GitAttributesEditorStrings : RepoFileEditorStrings
{
    public GitAttributesEditorStrings()
        : base("FormGitAttributes")
    {
        Title = Add("$this", "Text", "Edit .gitattributes");
        Help = Add("label1", "Text", "Edit the git attributes\nDefine attributes per path\n\nExamples\nMark all jpg files as binary:\n*.jpg binary\n\nMark sln files as binary:\n*.sln binary\n\nMark single file as text:\nweirdchars.txt text\n\nFor more information run\ncommand \"git help gitattributes\"\n");
        Save = Add("Save", "Text", "Save");
        CannotAccess = Add("_cannotAccessGitattributes", "Text", "Failed to save .gitattributes.\nCheck if file is accessible.");
        CannotAccessCaption = Add("_cannotAccessGitattributesCaption", "Text", "Failed to save .gitattributes");
        NoWorkingDir = Add("_noWorkingDir", "Text", ".gitattributes is only supported when there is a working directory.");
        NoWorkingDirCaption = Add("_noWorkingDirCaption", "Text", "No working directory");
        SaveFileQuestion = Add("_saveFileQuestion", "Text", "Save changes to .gitattributes?");
        SaveFileQuestionCaption = Add("_saveFileQuestionCaption", "Text", "Save changes?");
    }
}

/// <summary>Strings of the .mailmap editor; ids match <c>FormMailMap</c>.</summary>
public sealed class MailMapEditorStrings : RepoFileEditorStrings
{
    public MailMapEditorStrings()
        : base("FormMailMap")
    {
        Title = Add("$this", "Text", "Edit .mailmap");
        Help = Add("label1", "Text", "Edit the mailmap.\nThis file is meant to correct usernames.\n\nExample:\nHenk Westhuis <Henk@.(none)>\nHenk Westhuis <henk_westhuis@hotmail.com>\n\nFor more information run\ncommand \"git help shortlog\"");
        Save = Add("Save", "Text", "Save");
        CannotAccess = Add("_cannotAccessMailmap", "Text", "Failed to save .mailmap.\nCheck if file is accessible.");
        CannotAccessCaption = Add("_cannotAccessMailmapCaption", "Text", "Failed to save .mailmap");
        NoWorkingDir = Add("_mailmapOnlyInWorkingDirSupported", "Text", ".mailmap is only supported when there is a working directory.");
        NoWorkingDirCaption = Add("_mailmapOnlyInWorkingDirSupportedCaption", "Text", "No working directory");
        SaveFileQuestion = Add("_saveFileQuestion", "Text", "Save changes to .mailmap?");
        SaveFileQuestionCaption = Add("_saveFileQuestionCaption", "Text", "Save changes?");
    }
}

/// <summary>Operations of an editor of a file in the working directory that need the host (the file system).</summary>
public interface IRepoFileEditorHost
{
    /// <summary>The content of the file, or empty if it does not exist.</summary>
    string Load();

    /// <summary>Writes the file (throws if it cannot be written).</summary>
    void Save(string text);
}

/// <summary>View model of an editor of a file in the working directory (port of <c>FormGitAttributes</c> and <c>FormMailMap</c>).</summary>
public sealed partial class RepoFileEditorViewModel : DialogViewModel
{
    private readonly IRepoFileEditorHost _host;
    private readonly IMessageBoxService _messageBoxes;

    public RepoFileEditorViewModel(RepoFileEditorStrings strings, string fileName, IRepoFileEditorHost host, IMessageBoxService messageBoxes)
    {
        Strings = strings;
        _host = host;
        _messageBoxes = messageBoxes;
        Editor.Load(host.Load(), fileName);
    }

    public RepoFileEditorStrings Strings { get; }

    public TextEditorViewModel Editor { get; } = new();

    [RelayCommand]
    private void Save()
    {
        if (SaveFile())
        {
            Close(accepted: true);
        }
    }

    /// <summary>As <c>FormGitAttributes.SaveFile</c>: the file ends with a new line.</summary>
    private bool SaveFile()
    {
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
            _messageBoxes.ShowError(Strings.CannotAccess.Text + Environment.NewLine + ex.Message, Strings.CannotAccessCaption.Text);
            return false;
        }
    }

    /// <summary>As <c>FormGitAttributesClosing</c>: unsaved changes are saved, discarded, or keep the dialog open.</summary>
    public override bool CanClose()
    {
        if (!Editor.HasChanges)
        {
            return true;
        }

        return _messageBoxes.ConfirmWithCancel(Strings.SaveFileQuestion.Text, Strings.SaveFileQuestionCaption.Text) switch
        {
            true => SaveFile(),
            false => true,
            null => false,
        };
    }
}

/// <summary>Strings of the file editor; ids match <c>FormEditor</c>.</summary>
public sealed class FileEditorStrings : ViewStrings
{
    public FileEditorStrings()
        : base("FormEditor")
    {
        Title = Add("$this", "Text", "Editor");
        Warning = Add("labelWarning", "Text", "Here be dragons!\nChanging this file by hand can be harmful and might break something.\nIf you are not sure just close this window.");
        Save = Add("toolStripSaveButton", "ToolTipText", "Save");
        SaveChanges = Add("_saveChanges", "Text", "Do you want to save changes?");
        SaveChangesCaption = Add("_saveChangesCaption", "Text", "Save changes");
        CannotOpenFile = Add("_cannotOpenFile", "Text", "Cannot open file:");
        CannotSaveFile = Add("_cannotSaveFile", "Text", "Cannot save file:");
    }

    public TranslatedText Title { get; }

    public TranslatedText Warning { get; }

    public TranslatedText Save { get; }

    public TranslatedText SaveChanges { get; }

    public TranslatedText SaveChangesCaption { get; }

    public TranslatedText CannotOpenFile { get; }

    public TranslatedText CannotSaveFile { get; }
}

/// <summary>Operations of the file editor that need the host (the file system, the repository's encoding).</summary>
public interface IFileEditorHost
{
    /// <summary>Writes the text to the file (throws if it cannot be written).</summary>
    void Save(string fileName, string text);
}

/// <summary>View model of the file editor, also used as git's editor (port of <c>FormEditor</c>).</summary>
public sealed partial class FileEditorViewModel : DialogViewModel
{
    private readonly string _fileName;
    private readonly IFileEditorHost _host;
    private readonly IMessageBoxService _messageBoxes;
    private readonly string _errorCaption;

    /// <param name="text">The content of the file.</param>
    /// <param name="showWarning">Whether to warn that editing the file by hand can break something.</param>
    /// <param name="lineNumber">The line to show, if any.</param>
    public FileEditorViewModel(
        FileEditorStrings strings,
        string fileName,
        string text,
        bool showWarning,
        bool readOnly,
        int? lineNumber,
        string errorCaption,
        IFileEditorHost host,
        IMessageBoxService messageBoxes)
    {
        Strings = strings;
        _fileName = fileName;
        ShowWarning = showWarning;
        _errorCaption = errorCaption;
        _host = host;
        _messageBoxes = messageBoxes;
        Editor.Load(text, fileName, lineNumber);
        Editor.IsReadOnly = readOnly;
        Editor.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TextEditorViewModel.HasChanges))
            {
                SaveCommand.NotifyCanExecuteChanged();
            }
        };
    }

    public FileEditorStrings Strings { get; }

    /// <summary>The window title: the file name, as <c>FormEditor.OpenFile</c> sets it.</summary>
    public string Title => _fileName;

    public bool ShowWarning { get; }

    public TextEditorViewModel Editor { get; } = new();

    /// <summary>Whether the dialog ended with the file saved or unchanged (<c>DialogResult.OK</c>), not discarded or cancelled.</summary>
    public bool Accepted { get; private set; }

    private bool CanSave() => Editor.HasChanges;

    /// <summary>Saves (also Ctrl+S), showing the error if the file cannot be written.</summary>
    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        try
        {
            SaveChanges();
        }
        catch (Exception ex)
        {
            _messageBoxes.ShowError($"{Strings.CannotSaveFile.Text}{Environment.NewLine}{ex.Message}", _errorCaption);
        }
    }

    private void SaveChanges()
    {
        _host.Save(_fileName, Editor.Text);
        Editor.MarkSaved();
    }

    /// <summary>As <c>FormEditor_FormClosing</c>.</summary>
    public override bool CanClose()
    {
        if (!Editor.HasChanges)
        {
            Accepted = true;
            return true;
        }

        switch (_messageBoxes.ConfirmWithCancel(Strings.SaveChanges.Text, Strings.SaveChangesCaption.Text))
        {
            case true:
                try
                {
                    SaveChanges();
                }
                catch (Exception ex)
                {
                    // OK closes without saving, Cancel keeps the editor open.
                    if (!_messageBoxes.Confirm($"{Strings.CannotSaveFile.Text}{Environment.NewLine}{ex.Message}", _errorCaption))
                    {
                        return false;
                    }
                }

                Accepted = true;
                return true;

            case null:
                return false;

            default:
                Accepted = false;
                return true;
        }
    }
}
