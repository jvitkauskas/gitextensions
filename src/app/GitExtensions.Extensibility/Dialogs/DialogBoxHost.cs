namespace GitExtensions.Extensibility;

/// <summary>
///  The message boxes, task dialogs and common dialogs (files, folders, colors, fonts) of the UI of the application, shown
///  where the native (Win32) ones are not: off Windows (docs/avalonia-port/CROSS-PLATFORM.md, phase 2). They are modal and
///  return when closed, as the native ones. The owner is the native handle of a window (the active window of the application
///  if 0).
/// </summary>
public interface IDialogBoxHost
{
    /// <summary>Shows a message box.</summary>
    DialogResult ShowMessageBox(nint owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton);

    /// <summary>Shows a task dialog.</summary>
    /// <returns>The button that closed the dialog; <see cref="TaskDialogButton.Cancel"/> if it was cancelled.</returns>
    TaskDialogButton ShowTaskDialog(nint owner, TaskDialogPage page);

    /// <summary>Shows a dialog to choose files to open, a file to save or a folder.</summary>
    /// <returns>The chosen paths; <see langword="null"/> if the dialog was cancelled.</returns>
    FileDialogResult? ShowFileDialog(nint owner, FileDialogRequest request);

    /// <summary>Shows a dialog to choose a color.</summary>
    /// <returns>The chosen color; <see langword="null"/> if the dialog was cancelled.</returns>
    Color? ShowColorDialog(nint owner, Color color);

    /// <summary>Shows a dialog to choose a font (of fixed pitch only, for code).</summary>
    /// <returns>The chosen font; <see langword="null"/> if the dialog was cancelled.</returns>
    FontDescriptor? ShowFontDialog(nint owner, FontDescriptor? font, bool fixedPitchOnly);
}

/// <summary>What a file dialog chooses.</summary>
public enum FileDialogKind
{
    /// <summary>Existing files to open.</summary>
    Open,

    /// <summary>A file to save.</summary>
    Save,

    /// <summary>A folder.</summary>
    Folder,
}

/// <summary>A type of files of a file dialog: its name and its patterns (e.g. "*.txt").</summary>
public sealed record FileDialogFileType(string Name, IReadOnlyList<string> Patterns);

/// <summary>A file dialog of the UI of the application, as the native file dialogs are set up (<see cref="FileDialog"/>).</summary>
/// <param name="FileTypeIndex">The one-based index of the selected file type in <paramref name="FileTypes"/>.</param>
/// <param name="FileName">The initial file name (a save dialog suggests it).</param>
/// <param name="DefaultExtension">The extension added to a file name typed without one, without the dot.</param>
public sealed record FileDialogRequest(
    FileDialogKind Kind,
    string? Title,
    IReadOnlyList<FileDialogFileType> FileTypes,
    int FileTypeIndex,
    string? InitialDirectory,
    string? FileName,
    string? DefaultExtension,
    bool Multiselect,
    bool OverwritePrompt);

/// <summary>The paths chosen with a file dialog, and the one-based index of the file type selected.</summary>
public sealed record FileDialogResult(IReadOnlyList<string> Paths, int FileTypeIndex);

/// <summary>The <see cref="IDialogBoxHost"/> of the application, which <see cref="NativeMessageBox"/> and <see cref="TaskDialog"/> use off Windows.</summary>
public static class DialogBoxHost
{
    /// <summary>The dialogs of the UI of the application; set by the application at startup.</summary>
    public static IDialogBoxHost? Current { get; set; }

    /// <summary>
    ///  Whether <see cref="Current"/> is used on Windows too, instead of the native dialogs (to check the dialogs of the other
    ///  systems on Windows).
    /// </summary>
    public static bool UseOnWindows { get; set; }

    /// <summary>The host to show a dialog with, or <see langword="null"/> for the native dialogs of Windows.</summary>
    internal static IDialogBoxHost? Active
        => OperatingSystem.IsWindows() && !UseOnWindows
            ? null
            : Current ?? throw new PlatformNotSupportedException("No dialogs of the UI are set for this system (DialogBoxHost.Current).");
}
