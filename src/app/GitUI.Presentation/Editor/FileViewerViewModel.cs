using CommunityToolkit.Mvvm.ComponentModel;
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
    }

    public TranslatedText BinaryFile { get; }

    public TranslatedText BinaryFileDetected { get; }

    public TranslatedText CannotViewImage { get; }
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

/// <summary>Gets the changes of a file (the WinForms <c>GitUIExtensions.ViewChangesAsync</c>) from the repository.</summary>
public interface IFileViewerHost
{
    /// <summary>The changes of the file between its revisions, if possible as a diff.</summary>
    Task<FileViewContent> GetChangesAsync(FileStatusEntry entry, CancellationToken cancellationToken);

    /// <summary>The theme colors git's colors are shown with.</summary>
    IThemeColors ThemeColors { get; }

    /// <summary>Whether git colors the background (<c>AppSettings.ReverseGitColoring</c>).</summary>
    bool ReverseGitColoring { get; }
}

/// <summary>
///  View model of the Avalonia file viewer (the viewing mode of the WinForms <c>FileViewer</c>; docs/avalonia-port/PLAN.md,
///  phase 3): the changes of a file as a diff, the file as a text, or an image.
/// </summary>
public sealed partial class FileViewerViewModel : ObservableObject
{
    private readonly IFileViewerHost _host;
    private CancellationTokenSource? _loading;

    public FileViewerViewModel(IFileViewerHost host)
    {
        _host = host;
    }

    /// <summary>The text or the diff.</summary>
    public TextEditorViewModel Editor { get; } = new() { IsReadOnly = true };

    /// <summary>The image shown instead of the text, if any.</summary>
    [ObservableProperty]
    public partial byte[]? Image { get; private set; }

    /// <summary>The file shown (as <c>ViewChangesAsync</c>), <see langword="null"/> to clear.</summary>
    public async Task ShowChangesAsync(FileStatusEntry? entry)
    {
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

        if (entry is null)
        {
            Show(FileViewContent.Empty);
            return;
        }

        CancellationTokenSource loading = new();
        _loading = loading;
        FileViewContent content;
        try
        {
            content = await _host.GetChangesAsync(entry, loading.Token);
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

        if (!loading.IsCancellationRequested)
        {
            Show(content);
        }
    }

    public void Show(FileViewContent content)
    {
        Image = content.Kind == FileViewKind.Image ? content.Image : null;
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
}
