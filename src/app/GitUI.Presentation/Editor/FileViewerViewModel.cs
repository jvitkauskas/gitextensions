using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands.Settings;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls.FileStatusList;

namespace GitUI.Presentation.Editor;

/// <summary>Strings of the file viewer; ids match <c>FileViewer</c>.</summary>
public sealed class FileViewerStrings : ViewStrings
{
    public FileViewerStrings()
        : base("FileViewer")
    {
        BinaryFile = Add("_binaryFile", "Text", "Binary file: {0}");
        BinaryFileDetected = Add("_binaryFileDetected", "Text", "Binary file: {0} (Detected)");
        CannotViewImage = Add("_cannotViewImage", "Text", "Cannot view image {0}");
        NextChange = Add("nextChangeButton", "ToolTipText", "Next change");
        PreviousChange = Add("previousChangeButton", "ToolTipText", "Previous change");
        IncreaseContextLines = Add("increaseNumberOfLines", "ToolTipText", "Increase the number of lines of context");
        DecreaseContextLines = Add("decreaseNumberOfLines", "ToolTipText", "Decrease the number of lines of context");
        ShowEntireFile = Add("showEntireFileButton", "ToolTipText", "Show entire file");
        ShowNonPrintingChars = Add("showNonPrintChars", "ToolTipText", "Show nonprinting characters");
        IgnoreWhitespaceAtEol = Add("ignoreWhitespaceAtEol", "ToolTipText", "Ignore whitespace changes at end of line");
        IgnoreWhitespaceChanges = Add("ignoreWhiteSpaces", "ToolTipText", "Ignore changes in amount of whitespace");
        IgnoreAllWhitespaceChanges = Add("ignoreAllWhitespaces", "ToolTipText", "Ignore all whitespace changes");
        Settings = Add("settingsButton", "ToolTipText", "Settings");
        StageSelectedLines = Add("stageSelectedLinesToolStripMenuItem", "Text", "Stage selected line(s)");
        UnstageSelectedLines = Add("unstageSelectedLinesToolStripMenuItem", "Text", "Unstage selected line(s)");
        ResetSelectedLines = Add("resetSelectedLinesToolStripMenuItem", "Text", "Reset selected line(s)");
        Copy = Add("copyToolStripMenuItem", "Text", "&Copy");
        CopyPatch = Add("copyPatchToolStripMenuItem", "Text", "Copy &patch");
        CopyNewVersion = Add("copyNewVersionToolStripMenuItem", "Text", "Copy &new version");
        CopyOldVersion = Add("copyOldVersionToolStripMenuItem", "Text", "Copy &old version");
        IncreaseContextLinesMenu = Add("increaseNumberOfLinesToolStripMenuItem", "Text", "&Increase the number of lines of context");
        DecreaseContextLinesMenu = Add("decreaseNumberOfLinesToolStripMenuItem", "Text", "&Decrease the number of lines of context");
        ShowEntireFileMenu = Add("showEntireFileToolStripMenuItem", "Text", "Show &entire file");
        ShowNonPrintingCharsMenu = Add("showNonprintableCharactersToolStripMenuItem", "Text", "S&how nonprinting characters");
        IgnoreWhitespaceAtEolMenu = Add("ignoreWhitespaceAtEolToolStripMenuItem", "Text", "Ignore whitespace changes at end of &line");
        IgnoreWhitespaceChangesMenu = Add("ignoreWhitespaceChangesToolStripMenuItem", "Text", "Ignore changes in &amount of whitespace");
        IgnoreAllWhitespaceChangesMenu = Add("ignoreAllWhitespaceChangesToolStripMenuItem", "Text", "Ignore all &whitespace changes");
        Find = Add("findToolStripMenuItem", "Text", "&Find...");
        Replace = Add("replaceToolStripMenuItem", "Text", "&Replace...");
        GoToLine = Add("goToLineToolStripMenuItem", "Text", "&Go to line");
        ShowSyntaxHighlighting = Add("showSyntaxHighlighting", "ToolTipText", "Show syntax highlighting");
        ShowSyntaxHighlightingMenu = Add("showSyntaxHighlightingToolStripMenuItem", "Text", "Show synta&x highlighting");
        DiffAppearance = Add("diffAppearanceToolStripMenuItem", "Text", "Diff appea&rance");
        ShowPatch = Add("showPatchToolStripMenuItem", "Text", "&Patch");
        ShowGitWordColoring = Add("showGitWordColoringToolStripMenuItem", "Text", "Git wor&d diff");
        ShowDifftastic = Add("showDifftasticToolStripMenuItem", "Text", "Diff&tastic");
        TreatAllFilesAsText = Add("treatAllFilesAsTextToolStripMenuItem", "Text", "&Treat all files as text");
    }

    public TranslatedText ShowSyntaxHighlighting { get; }

    public TranslatedText ShowSyntaxHighlightingMenu { get; }

    public TranslatedText DiffAppearance { get; }

    public TranslatedText ShowPatch { get; }

    public TranslatedText ShowGitWordColoring { get; }

    public TranslatedText ShowDifftastic { get; }

    public TranslatedText TreatAllFilesAsText { get; }

    public TranslatedText StageSelectedLines { get; }

    public TranslatedText UnstageSelectedLines { get; }

    public TranslatedText ResetSelectedLines { get; }

    public TranslatedText Copy { get; }

    public TranslatedText CopyPatch { get; }

    public TranslatedText CopyNewVersion { get; }

    public TranslatedText CopyOldVersion { get; }

    public TranslatedText IncreaseContextLinesMenu { get; }

    public TranslatedText DecreaseContextLinesMenu { get; }

    public TranslatedText ShowEntireFileMenu { get; }

    public TranslatedText ShowNonPrintingCharsMenu { get; }

    public TranslatedText IgnoreWhitespaceAtEolMenu { get; }

    public TranslatedText IgnoreWhitespaceChangesMenu { get; }

    public TranslatedText IgnoreAllWhitespaceChangesMenu { get; }

    public TranslatedText Find { get; }

    public TranslatedText Replace { get; }

    public TranslatedText GoToLine { get; }

    public TranslatedText BinaryFile { get; }

    public TranslatedText BinaryFileDetected { get; }

    public TranslatedText CannotViewImage { get; }

    public TranslatedText NextChange { get; }

    public TranslatedText PreviousChange { get; }

    public TranslatedText IncreaseContextLines { get; }

    public TranslatedText DecreaseContextLines { get; }

    public TranslatedText ShowEntireFile { get; }

    public TranslatedText ShowNonPrintingChars { get; }

    public TranslatedText IgnoreWhitespaceAtEol { get; }

    public TranslatedText IgnoreWhitespaceChanges { get; }

    public TranslatedText IgnoreAllWhitespaceChanges { get; }

    public TranslatedText Settings { get; }
}

/// <summary>How the content of a file viewer is shown.</summary>
public enum FileViewKind
{
    /// <summary>A text: a file, a message or an error.</summary>
    Text,

    /// <summary>A diff (a patch), possibly with git's colors.</summary>
    Diff,

    /// <summary>An image.</summary>
    Image,
}

/// <summary>What the file viewer shows (the result of the WinForms <c>FileViewer.View*Async</c> methods).</summary>
/// <param name="FileName">The file, whose extension chooses the syntax highlighting of a text.</param>
/// <param name="HasGitColors">Whether a diff is git's colored output (with ANSI escape sequences).</param>
/// <param name="Image">The content of an image.</param>
/// <param name="SupportsLinePatching">Whether lines can be staged, unstaged or reset (as <c>FileViewer.SupportLinePatching</c>).</param>
/// <param name="FilePreamble">The preamble of the encoding of a new file read from the working directory (<c>FilePreamble</c>).</param>
/// <param name="DiffMode">The kind of a diff (the view mode of <c>FileViewer.View*Async</c>).</param>
/// <param name="CanOpenWithDifftool">Whether F3 opens the difftool without a search (the <c>openWithDifftool</c> of <c>ViewChangesAsync</c>).</param>
/// <param name="DifftasticWidth">The width of the output of difftastic (<c>DFT_WIDTH</c>).</param>
public sealed record FileViewContent(
    FileViewKind Kind,
    string Text,
    string? FileName = null,
    bool HasGitColors = false,
    byte[]? Image = null,
    bool SupportsLinePatching = false,
    byte[]? FilePreamble = null,
    DiffViewMode DiffMode = DiffViewMode.Diff,
    bool CanOpenWithDifftool = false,
    int DifftasticWidth = 80)
{
    public static FileViewContent Empty { get; } = new(FileViewKind.Text, "");
}

/// <summary>The settings of the diff and the text that the toolbar of the viewer toggles (saved in <c>AppSettings</c>).</summary>
public sealed record FileViewerSettings(
    bool ShowNonPrintingChars = false,
    bool ShowEntireFile = false,
    int NumberOfContextLines = 3,
    IgnoreWhitespaceKind IgnoreWhitespace = IgnoreWhitespaceKind.None,
    bool ShowSyntaxHighlighting = true);

/// <summary>The options of the viewer that the changes of a file depend on, besides <see cref="FileViewerSettings"/>.</summary>
/// <param name="EncodingName">The encoding chosen in the viewer, or <see langword="null"/> for the files encoding.</param>
/// <param name="TreatAllFilesAsText">Whether binary files are diffed as texts (<c>TreatAllFilesAsText</c>, not saved).</param>
/// <param name="ViewerWidth">The width of the viewer, which the width of the output of difftastic depends on.</param>
public sealed record FileViewRequest(string? EncodingName = null, bool TreatAllFilesAsText = false, double ViewerWidth = 0);

/// <summary>The line patches of the context menu (<c>StageSelectedLines</c>, <c>UnstageSelectedLines</c>, <c>ResetSelectedLines</c>).</summary>
public enum LinePatchOperation
{
    Stage,
    Unstage,
    Reset,
}

/// <summary>The hotkey commands of the viewer, with the codes of <c>FileViewer.Command</c> (the "FileViewer" hotkey settings).</summary>
public enum FileViewerHotkeyCommand
{
    Find = 0,
    GoToLine = 1,
    IncreaseNumberOfVisibleLines = 2,
    DecreaseNumberOfVisibleLines = 3,
    ShowEntireFile = 4,
    TreatFileAsText = 5,
    NextChange = 6,
    PreviousChange = 7,
    FindNextOrOpenWithDifftool = 8,
    FindPrevious = 9,
    NextOccurrence = 10,
    PreviousOccurrence = 11,
    StageLines = 12,
    UnstageLines = 13,
    ResetLines = 14,
    IgnoreAllWhitespace = 15,
    Replace = 16,
    ShowSyntaxHighlighting = 17,
    ShowGitWordColoring = 18,
    ShowDifftastic = 19,
}

/// <summary>The items of the context menu that apply (as <c>SetVisibilityDiffContextMenu</c>).</summary>
public sealed record FileViewerMenuState(bool CanStage, bool CanUnstage, bool CanReset, bool CanCopyPatch, bool IsDiff);

/// <summary>Gets the changes of a file (the WinForms <c>GitUIExtensions.ViewChangesAsync</c>) from the repository.</summary>
public interface IFileViewerHost
{
    /// <summary>The changes of the file between its revisions, if possible as a diff (in <see cref="DiffAppearance"/>).</summary>
    Task<FileViewContent> GetChangesAsync(FileStatusEntry entry, FileViewRequest request, CancellationToken cancellationToken);

    /// <summary>The file in the revision, or in the working directory (as <c>FileViewer.ViewGitItemAsync</c>).</summary>
    Task<FileViewContent> GetFileAsync(GitItemStatus file, ObjectId objectId, string? encodingName, CancellationToken cancellationToken);

    /// <summary>The theme colors git's colors are shown with.</summary>
    IThemeColors ThemeColors { get; }

    /// <summary>Whether git colors the background (<c>AppSettings.ReverseGitColoring</c>).</summary>
    bool ReverseGitColoring { get; }

    /// <summary>The settings of the toolbar; setting them saves them.</summary>
    FileViewerSettings Settings { get; set; }

    /// <summary>The names of the encodings to choose from (<c>AppSettings.AvailableEncodings</c>).</summary>
    IReadOnlyList<string> AvailableEncodings { get; }

    /// <summary>The name of the files encoding of the repository (<c>GitModule.FilesEncoding</c>).</summary>
    string FilesEncoding { get; }

    /// <summary>Opens the settings of the diff viewer (<c>settingsButton_Click</c>).</summary>
    void OpenSettings();

    /// <summary>How diffs are shown (<c>AppSettings.DiffDisplayAppearance</c>); setting it saves it.</summary>
    DiffDisplayAppearance DiffAppearance { get; set; }

    /// <summary>Whether the difftastic difftool is configured (<c>FileViewer.IsDifftasticEnabled</c>).</summary>
    bool IsDifftasticEnabled { get; }

    /// <summary>The column of the vertical ruler of the viewer, 0 for none (<c>AppSettings.DiffVerticalRulerPosition</c>).</summary>
    int VerticalRulerPosition { get; }

    /// <summary>As the <c>OpenWithDiffTool</c> of <c>ViewChangesAsync</c>: opens the changes of the file in the difftool.</summary>
    void OpenWithDifftool(FileStatusEntry entry);

    /// <summary>The configured hotkeys of the viewer (the "FileViewer" hotkey settings).</summary>
    IReadOnlyList<Services.HotkeyBinding> Hotkeys { get; }

    /// <summary>
    ///  As <c>StageSelectedLines</c>, <c>ResetNoncommittedSelectedLines</c> and <c>ApplySelectedLines</c>: applies the selected
    ///  lines of the diff (asking before a reset).
    /// </summary>
    /// <returns><see langword="true"/> if git applied (or tried to apply) the patch.</returns>
    bool ApplyLinePatch(LinePatchOperation operation, FileStatusEntry entry, StagedStatus stagedStatus, string text, int selectionStart, int selectionLength, byte[]? filePreamble);

    /// <summary>Puts the text on the clipboard, with the line endings of <c>core.autocrlf</c> if <paramref name="adjustLineEndings"/>.</summary>
    void CopyToClipboard(string text, bool adjustLineEndings);
}

/// <summary>
///  View model of the Avalonia file viewer (the viewing mode of the WinForms <c>FileViewer</c>; docs/avalonia-port/PLAN.md,
///  phase 3): the changes of a file as a diff, the file as a text, or an image, with the options of its toolbar.
/// </summary>
public sealed partial class FileViewerViewModel : ObservableObject
{
    private readonly IFileViewerHost _host;
    private CancellationTokenSource? _loading;
    private Func<CancellationToken, Task<FileViewContent>>? _reload;
    private string? _reloadDefaultText;
    private FileStatusEntry? _entry;
    private FileViewContent _content = FileViewContent.Empty;
    private bool _allowLinePatching;

    public FileViewerViewModel(IFileViewerHost host)
    {
        _host = host;
        Settings = host.Settings;
        Editor.ShowWhitespace = Settings.ShowNonPrintingChars;
        SelectedEncoding = host.FilesEncoding;
    }

    public FileViewerStrings Strings { get; } = ViewStrings.Load<FileViewerStrings>();

    /// <summary>The text or the diff.</summary>
    public TextEditorViewModel Editor { get; } = new() { IsReadOnly = true };

    /// <summary>The image shown instead of the text, if any.</summary>
    [ObservableProperty]
    public partial byte[]? Image { get; private set; }

    /// <summary>What is shown, which chooses the buttons of the toolbar (as <c>SetVisibilityDiffContextMenu</c>).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDiff), nameof(CanChangeContextLines), nameof(CanIgnoreWhitespaceChanges), nameof(CanIgnoreWhitespaceAtEol), nameof(IsDiffAppearanceVisible))]
    public partial FileViewKind Kind { get; private set; }

    /// <summary>The kind of the diff shown (see <see cref="IsDiff"/>).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanChangeContextLines), nameof(CanIgnoreWhitespaceChanges), nameof(CanIgnoreWhitespaceAtEol), nameof(IsDiffAppearanceVisible))]
    public partial DiffViewMode DiffMode { get; private set; }

    /// <summary>
    ///  Whether a diff (or grep results) is shown, with next / previous change and the syntax highlighting button (as
    ///  <c>IsPartialTextView</c>).
    /// </summary>
    public bool IsDiff => Kind == FileViewKind.Diff;

    /// <summary>Whether the number of lines of context can change (<c>isPartialFlexibleView</c>, also for the syntax highlighting item).</summary>
    public bool CanChangeContextLines => IsDiff && DiffMode != DiffViewMode.FixedDiff;

    /// <summary>Whether whitespace changes can be ignored (<c>diffCanBeModified</c>).</summary>
    public bool CanIgnoreWhitespaceChanges => IsDiff && DiffMode.IsDiffView() && DiffMode is not (DiffViewMode.FixedDiff or DiffViewMode.Difftastic);

    /// <summary>Whether whitespace changes at the end of lines can be ignored (also by difftastic, <c>DFT_STRIP_CR</c>).</summary>
    public bool CanIgnoreWhitespaceAtEol => CanIgnoreWhitespaceChanges || (IsDiff && DiffMode == DiffViewMode.Difftastic);

    /// <summary>Whether the appearance of the diff can be chosen (<c>isDiffAppearanceVisible</c>).</summary>
    public bool IsDiffAppearanceVisible => IsDiff && DiffMode is DiffViewMode.Diff or DiffViewMode.Difftastic;

    /// <summary>How diffs are shown (<c>AppSettings.DiffDisplayAppearance</c>).</summary>
    public DiffDisplayAppearance DiffAppearance => _host.DiffAppearance;

    /// <summary>Whether difftastic can be chosen (<c>IsDifftasticEnabled</c>).</summary>
    public bool IsDifftasticEnabled => _host.IsDifftasticEnabled;

    /// <summary>As <c>TreatAllFilesAsText</c>: binary files are diffed as texts (not saved).</summary>
    [ObservableProperty]
    public partial bool TreatAllFilesAsText { get; private set; }

    /// <summary>The width of the viewer, which the view reports (the width of the output of difftastic depends on it).</summary>
    public double ViewerWidth { get; set; }

    /// <summary>Whether F3 opens the difftool when there is no search (the <c>OpenWithDifftool</c> of <c>FileViewerInternal</c>).</summary>
    public bool CanOpenWithDifftool => _content.CanOpenWithDifftool && _entry is not null;

    /// <summary>As <c>FindNextAsync</c> without a search: opens the changes in the difftool.</summary>
    public void OpenWithDifftool()
    {
        if (CanOpenWithDifftool)
        {
            _host.OpenWithDifftool(_entry!);
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IgnoresWhitespaceAtEol), nameof(IgnoresWhitespaceChanges), nameof(IgnoresAllWhitespace))]
    public partial FileViewerSettings Settings { get; private set; }

    // As OnIgnoreWhitespaceChanged: each kind also checks the lesser ones.
    public bool IgnoresWhitespaceAtEol => Settings.IgnoreWhitespace is not IgnoreWhitespaceKind.None;

    public bool IgnoresWhitespaceChanges => Settings.IgnoreWhitespace is IgnoreWhitespaceKind.Change or IgnoreWhitespaceKind.AllSpace;

    public bool IgnoresAllWhitespace => Settings.IgnoreWhitespace is IgnoreWhitespaceKind.AllSpace;

    /// <summary>The encodings of the combo box of the toolbar.</summary>
    public IReadOnlyList<string> Encodings => _host.AvailableEncodings;

    /// <summary>The encoding of the file, the files encoding unless another is chosen.</summary>
    [ObservableProperty]
    public partial string? SelectedEncoding { get; set; }

    /// <summary>The file shown (as <c>ViewChangesAsync</c>), <see langword="null"/> to clear.</summary>
    /// <param name="defaultText">The text shown if there are no changes (the <c>defaultText</c> of <c>ViewChangesAsync</c>).</param>
    public Task ShowChangesAsync(FileStatusEntry? entry, string? defaultText = null)
        => LoadAsync(entry, entry is null ? null : cancellationToken => _host.GetChangesAsync(entry, new FileViewRequest(EncodingName, TreatAllFilesAsText, ViewerWidth), cancellationToken), defaultText);

    /// <summary>The file in the revision, or in the working directory for the artificial commits (as <c>ViewGitItemAsync</c>).</summary>
    public Task ShowFileAsync(GitItemStatus file, ObjectId objectId)
        => LoadAsync(entry: null, cancellationToken => _host.GetFileAsync(file, objectId, EncodingName, cancellationToken), defaultText: null);

    private string? EncodingName => SelectedEncoding is { } name && name != _host.FilesEncoding ? name : null;

    private async Task LoadAsync(FileStatusEntry? entry, Func<CancellationToken, Task<FileViewContent>>? getContent, string? defaultText)
    {
        _entry = entry;
        _reload = getContent;
        _reloadDefaultText = defaultText;

        // As the CancellationTokenSequence of the WinForms dialogs: the previous file stops loading.
        CancellationTokenSource? previous = _loading;
        _loading = null;
        if (previous is not null)
        {
            // Synchronously: awaiting CancelAsync could continue off the UI thread, and no long callbacks are registered.
#pragma warning disable VSTHRD103 // Call async methods when in an async method
            previous.Cancel();
#pragma warning restore VSTHRD103

            // Not disposed: the work in the background may still use its token (a disposed source throws ObjectDisposedException).
        }

        if (getContent is null)
        {
            Show(FileViewContent.Empty);
            return;
        }

        CancellationTokenSource loading = new();
        _loading = loading;
        FileViewContent content;
        try
        {
            content = await getContent(loading.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            // As the exception handler of FileViewer.
            content = new FileViewContent(FileViewKind.Text, "Unsupported file: \n\n" + ex);
        }

        if (defaultText is not null && content.Kind != FileViewKind.Image && string.IsNullOrEmpty(content.Text))
        {
            content = new FileViewContent(FileViewKind.Text, defaultText);
        }

        if (!loading.IsCancellationRequested)
        {
            Show(content);
        }
    }

    public void Show(FileViewContent content)
    {
        // As TextLoaded: the lines can be patched again once the file is shown.
        _content = content;
        _allowLinePatching = content.SupportsLinePatching;
        Image = content.Kind == FileViewKind.Image ? content.Image : null;
        DiffViewMode diffMode = content.DiffMode;
        FileViewKind kind = content.Kind;
        bool hasGitColors = content.HasGitColors;
        if (kind == FileViewKind.Text && IsDiffFile(content.FileName))
        {
            // As ResetView: a .diff or .patch file is shown as a fixed diff, with the colors of the escape sequences it has (ViewTextAsync).
            kind = FileViewKind.Diff;
            diffMode = DiffViewMode.FixedDiff;
            hasGitColors = AnsiEscapeParser.HasEscapes(content.Text);
        }

        DiffMode = diffMode;
        Kind = kind;

        // As SetText: the vertical ruler of the settings (difftastic sets its own).
        Editor.VerticalRulerColumn = Math.Max(0, _host.VerticalRulerPosition);
        switch (kind)
        {
            case FileViewKind.Diff:
                // As ResetView: the syntax highlighting of the file, if chosen.
                Editor.LoadDiff(content.Text, new DiffLoadOptions(
                    GitColors: hasGitColors || diffMode is DiffViewMode.Difftastic or DiffViewMode.Grep or DiffViewMode.RangeDiff ? _host.ThemeColors : null,
                    ReverseGitColoring: _host.ReverseGitColoring,
                    Mode: diffMode,
                    IsGitWordDiff: diffMode.IsNormalDiffView() && _host.DiffAppearance == DiffDisplayAppearance.GitWordDiff,
                    HighlightingFileName: Settings.ShowSyntaxHighlighting ? content.FileName : null,
                    DifftasticWidth: content.DifftasticWidth));
                break;
            default:
                Editor.Load(content.Text, content.FileName);
                break;
        }

        OnPropertyChanged(nameof(DiffAppearance));
    }

    private static bool IsDiffFile(string? fileName)
        => fileName is not null
            && (fileName.EndsWith(".diff", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".patch", StringComparison.OrdinalIgnoreCase));

    /// <summary>As <c>OnExtraDiffArgumentsChanged</c>: the file is shown again with the new options.</summary>
    private void Reload()
    {
        if (_reload is not null)
        {
            _ = LoadAsync(_entry, _reload, _reloadDefaultText);
        }
    }

    private void ChangeSettings(FileViewerSettings settings, bool reload = true)
    {
        _host.Settings = settings;
        Settings = settings;
        if (reload)
        {
            Reload();
        }
    }

    /// <summary>As <c>IncreaseNumberOfLinesToolStripMenuItemClick</c>.</summary>
    [RelayCommand]
    private void IncreaseContextLines() => ChangeSettings(Settings with { NumberOfContextLines = Settings.NumberOfContextLines + 1 });

    /// <summary>As <c>DecreaseNumberOfLinesToolStripMenuItemClick</c>.</summary>
    [RelayCommand]
    private void DecreaseContextLines() => ChangeSettings(Settings with { NumberOfContextLines = Math.Max(0, Settings.NumberOfContextLines - 1) });

    /// <summary>As <c>ShowEntireFileToolStripMenuItemClick</c>.</summary>
    [RelayCommand]
    private void ToggleShowEntireFile() => ChangeSettings(Settings with { ShowEntireFile = !Settings.ShowEntireFile });

    /// <summary>As <c>ShowNonprintableCharactersToolStripMenuItemClick</c>: no reload, the editor shows the characters.</summary>
    [RelayCommand]
    private void ToggleNonPrintingChars()
    {
        ChangeSettings(Settings with { ShowNonPrintingChars = !Settings.ShowNonPrintingChars }, reload: false);
        Editor.ShowWhitespace = Settings.ShowNonPrintingChars;
    }

    /// <summary>As the ignore whitespace buttons: a kind is switched on, or off if it is on.</summary>
    [RelayCommand]
    private void ToggleIgnoreWhitespace(IgnoreWhitespaceKind kind)
        => ChangeSettings(Settings with { IgnoreWhitespace = Settings.IgnoreWhitespace == kind ? IgnoreWhitespaceKind.None : kind });

    /// <summary>As <c>ShowSyntaxHighlighting_Click</c>.</summary>
    [RelayCommand]
    private void ToggleSyntaxHighlighting() => ChangeSettings(Settings with { ShowSyntaxHighlighting = !Settings.ShowSyntaxHighlighting });

    /// <summary>As <c>TreatAllFilesAsTextToolStripMenuItemClick</c>.</summary>
    [RelayCommand]
    private void ToggleTreatAllFilesAsText()
    {
        TreatAllFilesAsText = !TreatAllFilesAsText;
        Reload();
    }

    /// <summary>
    ///  As <c>ResetPatchAppearanceToolStripMenuItemClick</c>, <c>ToggleGitWordColoringToolStripMenuItemClick</c> and
    ///  <c>ToggleDifftasticToolStripMenuItemClick</c>: the patch is chosen, the others toggle.
    /// </summary>
    [RelayCommand]
    private void ChangeDiffAppearance(DiffDisplayAppearance appearance)
    {
        _host.DiffAppearance = appearance == DiffDisplayAppearance.Patch || _host.DiffAppearance != appearance ? appearance : DiffDisplayAppearance.Patch;
        OnPropertyChanged(nameof(DiffAppearance));
        Reload();
    }

    [RelayCommand]
    private void OpenSettings() => _host.OpenSettings();

    /// <summary>The hotkeys of the viewer (<c>FileViewer.HotkeySettingsName</c>).</summary>
    public IReadOnlyList<Services.HotkeyBinding> Hotkeys => _host.Hotkeys;

    /// <summary>
    ///  As <c>LinePatchingBlocksUntilReload</c>: after a line patch, no other one until the diff is shown again (for the
    ///  working directory and the index, whose diff the patch changes).
    /// </summary>
    public bool LinePatchingBlocksUntilReload { get; set; }

    /// <summary>As <c>PatchApplied</c>: raised after a line patch, e.g. to show the changes again.</summary>
    public event EventHandler? PatchApplied;

    /// <summary>As <c>ViewItemStagedStatus</c>: what the shown diff changes (the working directory, the index or a commit).</summary>
    public StagedStatus StagedStatus
    {
        get
        {
            if (_entry is null)
            {
                return StagedStatus.Unknown;
            }

            StagedStatus stagedStatus = _entry.Item.Staged;
            if (stagedStatus == StagedStatus.Unknown)
            {
                stagedStatus = GitCommands.GitModule.GetStagedStatus(
                    _entry.FirstRevision?.ObjectId ?? default,
                    _entry.SecondRevision.ObjectId,
                    _entry.SecondRevision.FirstParentId);
                _entry.Item.Staged = stagedStatus;
            }

            return stagedStatus;
        }
    }

    /// <summary>As <c>SetVisibilityDiffContextMenu</c>: stage and unstage depend on the diff, which looks the same to the user.</summary>
    public FileViewerMenuState GetMenuState()
    {
        bool supportsLinePatching = _content.SupportsLinePatching && _entry is not null;
        bool isIndex = supportsLinePatching && StagedStatus == StagedStatus.Index;

        // A range diff patch is undefined, could be new/old commit or to parents.
        bool isCopyPatch = IsDiff && DiffMode.IsNormalDiffView() && _host.DiffAppearance == DiffDisplayAppearance.Patch;
        return new FileViewerMenuState(
            CanStage: supportsLinePatching && !isIndex,
            CanUnstage: isIndex,
            CanReset: supportsLinePatching,
            CanCopyPatch: isCopyPatch,
            IsDiff: IsDiff);
    }

    /// <summary>
    ///  As <c>StageSelectedLines</c>, <c>UnstageSelectedLines</c> and <c>ResetSelectedLines</c> (also their hotkeys): the selected
    ///  lines are applied.
    /// </summary>
    /// <returns><see langword="false"/> if the operation does not apply to the shown diff (the hotkey is not used).</returns>
    public bool ApplyLinePatch(LinePatchOperation operation, int selectionStart, int selectionLength)
    {
        FileViewerMenuState state = GetMenuState();
        bool applies = operation switch
        {
            LinePatchOperation.Stage => state.CanStage,
            LinePatchOperation.Unstage => state.CanUnstage,
            _ => state.CanReset,
        };
        if (!applies)
        {
            return false;
        }

        if (!_allowLinePatching || _entry is null)
        {
            // The diff is not shown again yet.
            return true;
        }

        if (_host.ApplyLinePatch(operation, _entry, StagedStatus, Editor.Text, selectionStart, selectionLength, _content.FilePreamble))
        {
            if (LinePatchingBlocksUntilReload)
            {
                _allowLinePatching = false;
            }

            PatchApplied?.Invoke(this, EventArgs.Empty);
        }

        return true;
    }

    /// <summary>As <c>CopyToolStripMenuItemClick</c>: the selection, without the prefixes of the diff lines.</summary>
    public void Copy(string selectedText, int selectionStart)
    {
        if (string.IsNullOrEmpty(selectedText))
        {
            return;
        }

        // As GetFullDiffPrefixes: a difftastic diff and grep results are copied as they are.
        string[]? prefixes = !IsDiff ? null : IsCombinedDiff ? DiffViewMode.CombinedDiff.GetFullDiffPrefixes() : DiffMode.GetFullDiffPrefixes();
        string text = prefixes is null ? selectedText : RemoveDiffPrefixes(Editor.Text, selectedText, selectionStart, prefixes);
        _host.CopyToClipboard(text, adjustLineEndings: true);
    }

    /// <summary>As <c>CopyPatchToolStripMenuItemClick</c>: the selection as it is, or all the patch.</summary>
    public void CopyPatch(string selectedText)
    {
        string text = string.IsNullOrEmpty(selectedText) ? Editor.Text : selectedText;
        if (!string.IsNullOrEmpty(text))
        {
            _host.CopyToClipboard(text, adjustLineEndings: false);
        }
    }

    /// <summary>
    ///  As <c>copyNewVersionToolStripMenuItem_Click</c> (<paramref name="newVersion"/>) and <c>copyOldVersionToolStripMenuItem_Click</c>:
    ///  the selection (or all) without the removed (or added) lines.
    /// </summary>
    public void CopyVersion(string selectedText, int selectionStart, bool newVersion)
        => _host.CopyToClipboard(GetVersionText(Editor.Text, selectedText, selectionStart, IsDiff, newVersion ? '-' : '+'), adjustLineEndings: true);

    private bool IsCombinedDiff => DiffMode == DiffViewMode.CombinedDiff || _entry?.FirstRevision?.ObjectId == ObjectId.CombinedDiffId;

    /// <summary>As <c>CopyToolStripMenuItemClick</c>: the prefixes are kept when the header is selected.</summary>
    public static string RemoveDiffPrefixes(string fullText, string selectedText, int selectionStart, bool isCombinedDiff)
        => RemoveDiffPrefixes(fullText, selectedText, selectionStart, (isCombinedDiff ? DiffViewMode.CombinedDiff : DiffViewMode.Diff).GetFullDiffPrefixes()!);

    /// <summary>As <c>CopyToolStripMenuItemClick</c> with the prefixes of the highlight service (for a range diff, the header never matches).</summary>
    public static string RemoveDiffPrefixes(string fullText, string selectedText, int selectionStart, string[] prefixes)
    {
        int headerEnd = fullText.IndexOf("\n@@", StringComparison.Ordinal);
        if (headerEnd > selectionStart)
        {
            return selectedText;
        }

        // An artificial space if the selection does not start a line, removed with the prefixes.
        string code = selectionStart > 0 && fullText[selectionStart - 1] != '\n' ? " " + selectedText : selectedText;
        return string.Join("\n", code.Split('\n').Select(line => prefixes.FirstOrDefault(line.StartsWith) is { } prefix ? line[prefix.Length..] : line));
    }

    /// <summary>As <c>FileViewerInternal.CopyNotStartingWith</c>.</summary>
    public static string GetVersionText(string fullText, string selectedText, int selectionStart, bool isDiff, char excludedStart)
    {
        string text = selectedText;
        int position = selectionStart;
        if (string.IsNullOrEmpty(text))
        {
            text = fullText;
            position = 0;
        }

        if (!isDiff)
        {
            return text;
        }

        if (position > 0 && fullText[position - 1] != '\n')
        {
            text = " " + text;
        }

        IEnumerable<string> lines = text.Split('\n')
            .Where(s => s.Length == 0 || s[0] != excludedStart || (s.Length > 2 && s[1] == s[0] && s[2] == s[0]));
        int headerEnd = fullText.IndexOf("\n@@", StringComparison.Ordinal);
        if (headerEnd <= position)
        {
            const string specials = " -+";
            lines = lines.Select(s => s.Length > 0 && specials.Contains(s[0]) ? s[1..] : s);
        }

        return string.Join("\n", lines);
    }

    partial void OnSelectedEncodingChanged(string? value) => Reload();

    /// <summary>
    ///  As <c>GoToNextChange</c> and <c>GoToPreviousChange</c>: the first line of the next (or previous) block of added or
    ///  removed lines (<c>IsSearchMatch</c>; for a range diff, of commit headers) from <paramref name="currentLine"/> (1-based),
    ///  or <see langword="null"/> if there is none.
    /// </summary>
    public int? GetChangeLine(int currentLine, bool backwards)
    {
        if (Editor.DiffLines is not { } lines)
        {
            return null;
        }

        DiffViewMode mode = Editor.DiffMode;
        HashSet<int> changed = [.. lines.Where(l => mode.IsSearchMatch(l.Kind)).Select(l => l.LineNumInDiff)];
        List<int> starts = [.. changed.Where(line => !changed.Contains(line - 1)).Order()];
        return backwards
            ? starts.LastOrDefault(line => line < currentLine) is int previous and > 0 ? previous : null
            : starts.FirstOrDefault(line => line > currentLine) is int next and > 0 ? next : null;
    }
}
