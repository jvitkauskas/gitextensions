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
    }

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
public sealed record FileViewContent(FileViewKind Kind, string Text, string? FileName = null, bool HasGitColors = false, byte[]? Image = null)
{
    public static FileViewContent Empty { get; } = new(FileViewKind.Text, "");
}

/// <summary>The settings of the diff and the text that the toolbar of the viewer toggles (saved in <c>AppSettings</c>).</summary>
public sealed record FileViewerSettings(
    bool ShowNonPrintingChars = false,
    bool ShowEntireFile = false,
    int NumberOfContextLines = 3,
    IgnoreWhitespaceKind IgnoreWhitespace = IgnoreWhitespaceKind.None);

/// <summary>Gets the changes of a file (the WinForms <c>GitUIExtensions.ViewChangesAsync</c>) from the repository.</summary>
public interface IFileViewerHost
{
    /// <summary>The changes of the file between its revisions, if possible as a diff.</summary>
    /// <param name="encodingName">The encoding chosen in the viewer, or <see langword="null"/> for the files encoding.</param>
    Task<FileViewContent> GetChangesAsync(FileStatusEntry entry, string? encodingName, CancellationToken cancellationToken);

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
    [NotifyPropertyChangedFor(nameof(IsDiff))]
    public partial FileViewKind Kind { get; private set; }

    public bool IsDiff => Kind == FileViewKind.Diff;

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
        => LoadAsync(entry is null ? null : cancellationToken => _host.GetChangesAsync(entry, EncodingName, cancellationToken), defaultText);

    /// <summary>The file in the revision, or in the working directory for the artificial commits (as <c>ViewGitItemAsync</c>).</summary>
    public Task ShowFileAsync(GitItemStatus file, ObjectId objectId)
        => LoadAsync(cancellationToken => _host.GetFileAsync(file, objectId, EncodingName, cancellationToken), defaultText: null);

    private string? EncodingName => SelectedEncoding is { } name && name != _host.FilesEncoding ? name : null;

    private async Task LoadAsync(Func<CancellationToken, Task<FileViewContent>>? getContent, string? defaultText)
    {
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
            previous.Dispose();
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
        Image = content.Kind == FileViewKind.Image ? content.Image : null;
        Kind = content.Kind;
        switch (content.Kind)
        {
            case FileViewKind.Diff:
                Editor.LoadDiff(content.Text, content.HasGitColors ? _host.ThemeColors : null, _host.ReverseGitColoring);
                break;
            default:
                Editor.Load(content.Text, content.FileName);
                break;
        }
    }

    /// <summary>As <c>OnExtraDiffArgumentsChanged</c>: the file is shown again with the new options.</summary>
    private void Reload()
    {
        if (_reload is not null)
        {
            _ = LoadAsync(_reload, _reloadDefaultText);
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

    [RelayCommand]
    private void OpenSettings() => _host.OpenSettings();

    partial void OnSelectedEncodingChanged(string? value) => Reload();

    /// <summary>
    ///  As <c>GoToNextChange</c> and <c>GoToPreviousChange</c>: the first line of the next (or previous) block of added or
    ///  removed lines from <paramref name="currentLine"/> (1-based), or <see langword="null"/> if there is none.
    /// </summary>
    public int? GetChangeLine(int currentLine, bool backwards)
    {
        if (Editor.DiffLines is not { } lines)
        {
            return null;
        }

        HashSet<int> changed = [.. lines.Where(l => l.Kind is DiffLineKind.Plus or DiffLineKind.Minus).Select(l => l.LineNumInDiff)];
        List<int> starts = [.. changed.Where(line => !changed.Contains(line - 1)).Order()];
        return backwards
            ? starts.LastOrDefault(line => line < currentLine) is int previous and > 0 ? previous : null
            : starts.FirstOrDefault(line => line > currentLine) is int next and > 0 ? next : null;
    }
}
