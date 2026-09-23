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

    /// <summary>Whether the line numbers are shown (a diff shows its own).</summary>
    [ObservableProperty]
    public partial bool ShowLineNumbers { get; set; } = true;

    /// <summary>The name of the file, whose extension chooses the syntax highlighting.</summary>
    [ObservableProperty]
    public partial string? FileName { get; set; }

    /// <summary>The line (1-based) to show and put the caret on when the text is loaded.</summary>
    [ObservableProperty]
    public partial int? LineToShow { get; set; }

    /// <summary>The offset of the caret, which the view reports and follows.</summary>
    [ObservableProperty]
    public partial int CaretOffset { get; set; }

    /// <summary>Raised to move the focus to the editor.</summary>
    public event EventHandler? FocusRequested;

    public void RequestFocus() => FocusRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Whether the text changed since it was loaded or saved.</summary>
    public bool HasChanges => Text != _loadedText;

    /// <summary>Raised when a text is loaded, which the view shows from its start (or <see cref="LineToShow"/>).</summary>
    public event EventHandler? TextLoaded;

    /// <summary>The lines of the diff shown (with their line numbers), or <see langword="null"/> for a plain text.</summary>
    public IReadOnlyList<DiffLine>? DiffLines { get; private set; }

    /// <summary>The in-line differences of the matching removed and added lines of the diff shown; empty for a plain text.</summary>
    public IReadOnlyList<InlineDiffMarker> InlineDiffMarkers { get; private set; } = [];

    /// <summary>The colors of git's output of the diff shown, or <see langword="null"/> if the diff is not colored by git.</summary>
    public GitColoring? GitColoring { get; private set; }

    /// <summary>
    ///  Shows a diff, read-only, with the added and removed lines colored and their in-line differences marked
    ///  (as <c>FileViewer.ViewFixedPatch</c> and <c>ViewPatch</c>).
    /// </summary>
    /// <param name="gitColors">The theme colors if the text is git's colored output (with ANSI escape sequences).</param>
    /// <param name="reverseGitColoring">Whether git colors the background (<c>AppSettings.ReverseGitColoring</c>).</param>
    public void LoadDiff(string text, IThemeColors? gitColors = null, bool reverseGitColoring = true)
    {
        IsReadOnly = true;
        GitColoring? gitColoring = null;
        if (gitColors is not null)
        {
            (text, IReadOnlyList<ColoredSegment> segments) = AnsiEscapeParser.Parse(text, gitColors);
            gitColoring = new GitColoring(segments, gitColors, reverseGitColoring);
        }

        IReadOnlyList<DiffLine> diffLines = DiffLinesAnalyzer.Analyze(text, gitColoring);
        GitColoring = gitColoring;
        InlineDiffMarkers = InlineDiffAnalyzer.Analyze(text, diffLines);
        LoadText(text, fileName: null, line: null, diffLines);
    }

    /// <summary>Loads a text, which is unchanged afterwards (as <c>FileViewer.TextLoaded</c>).</summary>
    public void Load(string text, string? fileName = null, int? line = null)
    {
        GitColoring = null;
        InlineDiffMarkers = [];
        LoadText(text, fileName, line, diffLines: null);
    }

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
