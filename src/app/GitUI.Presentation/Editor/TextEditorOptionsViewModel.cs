using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.Editor;

/// <summary>What the options toolbar of a text editor needs of the application.</summary>
public interface ITextEditorOptionsHost
{
    /// <summary>Whether nonprinting characters are shown (<c>AppSettings.ShowNonPrintingChars</c>); setting it saves it.</summary>
    bool ShowNonPrintingChars { get; set; }

    /// <summary>Opens the settings of the viewer (<c>settingsButton_Click</c>).</summary>
    void OpenSettings();
}

/// <summary>Reads the file of an editor again in another encoding (the encoding combo box of the viewer).</summary>
public interface IEncodingReader
{
    /// <summary>The names of the encodings to choose from (<c>AppSettings.AvailableEncodings</c>).</summary>
    IReadOnlyList<string> AvailableEncodings { get; }

    /// <summary>The name of the encoding the file is read with.</summary>
    string EncodingName { get; }

    /// <summary>Reads the file in the encoding, which it is then saved in.</summary>
    string Read(string encodingName);
}

/// <summary>
///  The options toolbar of the file editors (the <c>fileviewerToolbar</c> of the WinForms <c>FileViewer</c> they are made of, for a
///  text): nonprinting characters, the encoding of the file, the settings.
/// </summary>
public sealed partial class TextEditorOptionsViewModel : ObservableObject
{
    private readonly TextEditorViewModel _editor;
    private readonly ITextEditorOptionsHost _host;
    private readonly IEncodingReader? _encodingReader;
    private bool _readingEncoding;

    /// <param name="encodingReader">Reads the file in another encoding; <see langword="null"/> for no encoding choice.</param>
    public TextEditorOptionsViewModel(TextEditorViewModel editor, ITextEditorOptionsHost host, IEncodingReader? encodingReader = null)
    {
        _editor = editor;
        _host = host;
        _encodingReader = encodingReader;
        ShowNonPrintingChars = host.ShowNonPrintingChars;
        _editor.ShowWhitespace = ShowNonPrintingChars;
        _readingEncoding = true;
        SelectedEncoding = encodingReader?.EncodingName;
        _readingEncoding = false;
        _editor.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TextEditorViewModel.HasChanges))
            {
                OnPropertyChanged(nameof(CanChangeEncoding));
            }
        };
    }

    public FileViewerStrings Strings { get; } = ViewStrings.Load<FileViewerStrings>();

    /// <summary>As <c>ShowNonprintableCharactersToolStripMenuItemClick</c>: shown in the editor, and saved.</summary>
    [ObservableProperty]
    public partial bool ShowNonPrintingChars { get; set; }

    partial void OnShowNonPrintingCharsChanged(bool value)
    {
        _editor.ShowWhitespace = value;
        _host.ShowNonPrintingChars = value;
    }

    /// <summary>Whether the encoding can be chosen.</summary>
    public bool HasEncodings => _encodingReader is not null;

    public IReadOnlyList<string> Encodings => _encodingReader?.AvailableEncodings ?? [];

    /// <summary>The encoding is chosen before the text is changed (reading the file again would lose the changes).</summary>
    public bool CanChangeEncoding => !_editor.HasChanges;

    /// <summary>The encoding the file is read and saved in.</summary>
    [ObservableProperty]
    public partial string? SelectedEncoding { get; set; }

    partial void OnSelectedEncodingChanged(string? value)
    {
        if (_readingEncoding || _encodingReader is null || value is null || !CanChangeEncoding)
        {
            return;
        }

        // As encodingToolStripComboBox_SelectedIndexChanged: the file shown in the encoding.
        _editor.Load(_encodingReader.Read(value), _editor.FileName);
    }

    [RelayCommand]
    private void OpenSettings() => _host.OpenSettings();
}
