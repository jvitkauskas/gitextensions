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

    /// <summary>Reads the selected text of the view (the view sets it).</summary>
    public Func<string>? SelectedTextProvider { get; set; }

    /// <summary>The selected text of the editor, if a view shows it.</summary>
    public string SelectedText => SelectedTextProvider?.Invoke() ?? "";

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

    /// <summary>What kind of diff is shown (see <see cref="DiffLines"/>).</summary>
    public DiffViewMode DiffMode { get; private set; }

    /// <summary>
    ///  Whether the line numbers of a diff have a column for the old file (not for grep results and range diffs, as the
    ///  <c>showLeftColumn</c> of <c>DiffViewerLineNumberControl.DisplayLineNum</c>).
    /// </summary>
    public bool ShowLeftLineNumbers { get; private set; } = true;

    /// <summary>The column of the vertical ruler, 0 for none (as <c>FileViewerInternal.VRulerPosition</c>).</summary>
    [ObservableProperty]
    public partial int VerticalRulerColumn { get; set; }

    /// <summary>
    ///  Shows a diff, read-only, with the added and removed lines colored and their in-line differences marked
    ///  (as <c>FileViewer.ViewFixedPatch</c> and <c>ViewPatch</c>).
    /// </summary>
    /// <param name="gitColors">The theme colors if the text is git's colored output (with ANSI escape sequences).</param>
    /// <param name="reverseGitColoring">Whether git colors the background (<c>AppSettings.ReverseGitColoring</c>).</param>
    public void LoadDiff(string text, IThemeColors? gitColors = null, bool reverseGitColoring = true)
        => LoadDiff(text, new DiffLoadOptions(gitColors, reverseGitColoring));

    /// <summary>
    ///  Shows a diff of any kind, read-only (as <c>FileViewerInternal.SetText</c> with the highlight service of the view mode):
    ///  its lines and line numbers, git's colors, and the in-line differences of the matching lines of a patch.
    /// </summary>
    public void LoadDiff(string text, DiffLoadOptions options)
    {
        IsReadOnly = true;
        GitColoring? gitColoring = null;
        IReadOnlyList<DiffLine> diffLines;
        IReadOnlyList<InlineDiffMarker> inlineDiffMarkers = [];
        IThemeColors colors = options.GitColors ?? DefaultThemeColors.Instance;
        switch (options.Mode)
        {
            case DiffViewMode.Difftastic or DiffViewMode.Grep or DiffViewMode.RangeDiff:
                // As DifftasticHighlightService, GrepHighlightService and RangeDiffHighlightService: always with git's colors,
                // no in-line differences.
                AnalyzedDiff analyzed = options.Mode switch
                {
                    DiffViewMode.Difftastic => DifftasticAnalyzer.Analyze(text, options.DifftasticWidth, colors, options.ReverseGitColoring),
                    DiffViewMode.Grep => GrepAnalyzer.Analyze(text, colors),
                    _ => RangeDiffAnalyzer.Analyze(text, colors),
                };
                text = analyzed.Text;
                gitColoring = new GitColoring(analyzed.Segments, colors, options.ReverseGitColoring);
                diffLines = analyzed.Lines;
                if (options.Mode == DiffViewMode.Difftastic)
                {
                    // As SetText: difftastic sets the position (0 to hide).
                    VerticalRulerColumn = analyzed.VerticalRulerColumn;
                }

                break;

            default:
                // As PatchHighlightService and CombinedDiffHighlightService.
                if (options.GitColors is not null)
                {
                    (text, IReadOnlyList<ColoredSegment> segments) = AnsiEscapeParser.Parse(text, colors);
                    gitColoring = new GitColoring(segments, colors, options.ReverseGitColoring);
                }

                bool isCombinedDiff = options.Mode == DiffViewMode.CombinedDiff || DiffLinesAnalyzer.IsCombinedDiff(text);
                diffLines = DiffLinesAnalyzer.Analyze(text, isCombinedDiff, gitColoring, isGitWordDiff: options.IsGitWordDiff && !isCombinedDiff);

                // As DiffHighlightService.SetHighlighting: git colors the words of a git word diff.
                inlineDiffMarkers = gitColoring is not null && options.IsGitWordDiff ? [] : InlineDiffAnalyzer.Analyze(text, diffLines);
                break;
        }

        DiffMode = options.Mode;
        ShowLeftLineNumbers = options.Mode is not (DiffViewMode.Grep or DiffViewMode.RangeDiff);
        GitColoring = gitColoring;
        InlineDiffMarkers = inlineDiffMarkers;
        LoadText(text, options.HighlightingFileName, line: null, diffLines);
    }

    /// <summary>Loads a text, which is unchanged afterwards (as <c>FileViewer.TextLoaded</c>).</summary>
    public void Load(string text, string? fileName = null, int? line = null)
    {
        GitColoring = null;
        InlineDiffMarkers = [];
        DiffMode = DiffViewMode.Diff;
        ShowLeftLineNumbers = true;
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

/// <summary>How <see cref="TextEditorViewModel.LoadDiff(string, DiffLoadOptions)"/> shows a diff.</summary>
/// <param name="GitColors">The theme colors if the text is git's colored output (always for difftastic, grep and range diffs).</param>
/// <param name="ReverseGitColoring">Whether git colors the background (<c>AppSettings.ReverseGitColoring</c>).</param>
/// <param name="Mode">The kind of diff.</param>
/// <param name="IsGitWordDiff">Whether a colored patch is a git word diff (<c>DiffDisplayAppearance.GitWordDiff</c>).</param>
/// <param name="HighlightingFileName">The file whose syntax highlighting the diff gets (<c>ShowSyntaxHighlightingInDiff</c>), if any.</param>
/// <param name="DifftasticWidth">The width of the output of difftastic (<c>DFT_WIDTH</c>).</param>
public sealed record DiffLoadOptions(
    IThemeColors? GitColors = null,
    bool ReverseGitColoring = true,
    DiffViewMode Mode = DiffViewMode.Diff,
    bool IsGitWordDiff = false,
    string? HighlightingFileName = null,
    int DifftasticWidth = 80);
