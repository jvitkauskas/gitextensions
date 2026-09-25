namespace GitExtensions.Extensibility;

/// <summary>
///  The message boxes and task dialogs of the UI of the application, shown where the native (Win32) ones are not:
///  off Windows (docs/avalonia-port/CROSS-PLATFORM.md, phase 2). They are modal and return when closed, as the native ones.
/// </summary>
public interface IDialogBoxHost
{
    /// <summary>Shows a message box over the window of <paramref name="owner"/> (the active window of the application if 0).</summary>
    DialogResult ShowMessageBox(nint owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton);

    /// <summary>Shows a task dialog over the window of <paramref name="owner"/> (the active window of the application if 0).</summary>
    /// <returns>The button that closed the dialog; <see cref="TaskDialogButton.Cancel"/> if it was cancelled.</returns>
    TaskDialogButton ShowTaskDialog(nint owner, TaskDialogPage page);
}

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
