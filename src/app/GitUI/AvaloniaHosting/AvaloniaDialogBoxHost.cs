using GitExtensions.Extensibility;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.HelperDialogs;
using GitUI.Presentation.Translations;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  The message boxes and task dialogs of Avalonia (<see cref="MessageBoxWindow"/>, <see cref="TaskDialogWindow"/>), shown
///  by <see cref="NativeMessageBox"/> and <see cref="TaskDialog"/> off Windows (docs/avalonia-port/CROSS-PLATFORM.md, phase 2),
///  and on Windows too with the environment variable <c>GE_AVALONIA_DIALOGBOXES=1</c>. They are modal over their owner, and
///  return once closed: <see cref="AvaloniaDialogHost.ShowDialog"/> runs a nested dispatcher frame.
/// </summary>
public sealed class AvaloniaDialogBoxHost : IDialogBoxHost
{
    /// <summary>Makes these the dialogs of <see cref="DialogBoxHost"/>; called at startup.</summary>
    public static void Register()
    {
        DialogBoxHost.Current = new AvaloniaDialogBoxHost();
        DialogBoxHost.UseOnWindows = Environment.GetEnvironmentVariable("GE_AVALONIA_DIALOGBOXES") == "1";
    }

    public DialogResult ShowMessageBox(nint owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton)
    {
        AvaloniaUi.EnsureInitialized(AvaloniaDialogs.GetOptions);
        MessageBoxWindow window = new(text, caption, buttons, icon, defaultButton, ViewStrings.Load<DialogBoxStrings>());
        AvaloniaDialogHost.ShowDialog(window, owner != 0 ? owner : AvaloniaDialogHost.GetActiveWindowHandle());
        return window.Result;
    }

    public TaskDialogButton ShowTaskDialog(nint owner, TaskDialogPage page)
    {
        AvaloniaUi.EnsureInitialized(AvaloniaDialogs.GetOptions);
        TaskDialogWindow window = new(page, ViewStrings.Load<DialogBoxStrings>());
        AvaloniaDialogHost.ShowDialog(window, owner != 0 ? owner : AvaloniaDialogHost.GetActiveWindowHandle());
        return window.Result ?? TaskDialogButton.Cancel;
    }
}
