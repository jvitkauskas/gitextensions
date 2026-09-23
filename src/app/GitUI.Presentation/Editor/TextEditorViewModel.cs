using CommunityToolkit.Mvvm.ComponentModel;

namespace GitUI.Presentation.Editor;

/// <summary>
///  View model of the Avalonia text editor (the editing mode of the WinForms <c>FileViewer</c>; docs/avalonia-port/PLAN.md,
///  phase 3): the text, whether it is read-only, and the file name that chooses the syntax highlighting.
/// </summary>
public sealed partial class TextEditorViewModel : ObservableObject
{
    private string _loadedText = "";

    /// <summary>The text, as edited.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasChanges))]
    public partial string Text { get; set; } = "";

    [ObservableProperty]
    public partial bool IsReadOnly { get; set; }

    /// <summary>Whether spaces, tabs and line ends are shown (<c>AppSettings.ShowNonPrintingChars</c>).</summary>
    [ObservableProperty]
    public partial bool ShowWhitespace { get; set; }

    /// <summary>The name of the file, whose extension chooses the syntax highlighting.</summary>
    [ObservableProperty]
    public partial string? FileName { get; set; }

    /// <summary>The line (1-based) to show and put the caret on when the text is loaded.</summary>
    [ObservableProperty]
    public partial int? LineToShow { get; set; }

    /// <summary>Whether the text changed since it was loaded or saved.</summary>
    public bool HasChanges => Text != _loadedText;

    /// <summary>Raised when a text is loaded, which the view shows from its start (or <see cref="LineToShow"/>).</summary>
    public event EventHandler? TextLoaded;

    /// <summary>The lines of the diff shown (with their line numbers), or <see langword="null"/> for a plain text.</summary>
    public IReadOnlyList<DiffLine>? DiffLines { get; private set; }

    /// <summary>Shows a diff, read-only, with the added and removed lines colored (as <c>FileViewer.ViewFixedPatch</c>).</summary>
    public void LoadDiff(string text)
    {
        IsReadOnly = true;
        LoadText(text, fileName: null, line: null, DiffLinesAnalyzer.Analyze(text));
    }

    /// <summary>Loads a text, which is unchanged afterwards (as <c>FileViewer.TextLoaded</c>).</summary>
    public void Load(string text, string? fileName = null, int? line = null) => LoadText(text, fileName, line, diffLines: null);

    private void LoadText(string text, string? fileName, int? line, IReadOnlyList<DiffLine>? diffLines)
    {
        DiffLines = diffLines;
        FileName = fileName;
        LineToShow = line;
        _loadedText = text;
        Text = text;
        OnPropertyChanged(nameof(HasChanges));
        TextLoaded?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Marks the current text as saved.</summary>
    public void MarkSaved()
    {
        _loadedText = Text;
        OnPropertyChanged(nameof(HasChanges));
    }
}
