using GitExtensions.Extensibility;
using GitExtUtils;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.HelperDialogs;
using GitUI.Presentation.Translations;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  The message boxes, task dialogs and common dialogs of Avalonia (<see cref="MessageBoxWindow"/>, <see cref="TaskDialogWindow"/>,
///  the pickers of the storage provider, <see cref="ColorPickerWindow"/>, <see cref="FontPickerWindow"/>), shown by
///  <see cref="NativeMessageBox"/>, <see cref="TaskDialog"/>, the file dialogs, <see cref="ColorDialog"/> and
///  <see cref="FontPicker"/> off Windows (docs/avalonia-port/CROSS-PLATFORM.md, phase 2), and on Windows too with the
///  environment variable <c>GE_AVALONIA_DIALOGBOXES=1</c>. They are modal over their owner, and return once closed:
///  <see cref="AvaloniaDialogHost.ShowDialog"/> and <see cref="AvaloniaUi.WaitFor{T}"/> run a nested dispatcher frame.
/// </summary>
public sealed class AvaloniaDialogBoxHost : IDialogBoxHost
{
    /// <summary>Makes these the dialogs of <see cref="DialogBoxHost"/>, and the clipboard of Avalonia the one off Windows; called at startup.</summary>
    public static void Register()
    {
        DialogBoxHost.Current = new AvaloniaDialogBoxHost();
        DialogBoxHost.UseOnWindows = Environment.GetEnvironmentVariable("GE_AVALONIA_DIALOGBOXES") == "1";
        ClipboardUtil.Backend = new AvaloniaClipboardBackend();
    }

    public DialogResult ShowMessageBox(nint owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton)
    {
        MessageBoxWindow window = new(text, caption, buttons, icon, defaultButton, LoadStrings());
        ShowDialog(window, owner);
        return window.Result;
    }

    public TaskDialogButton ShowTaskDialog(nint owner, TaskDialogPage page)
    {
        TaskDialogWindow window = new(page, LoadStrings());
        ShowDialog(window, owner);
        return window.Result ?? TaskDialogButton.Cancel;
    }

    public FileDialogResult? ShowFileDialog(nint owner, FileDialogRequest request)
    {
        AvaloniaUi.EnsureInitialized(AvaloniaDialogs.GetOptions);
        return AvaloniaCommonDialogs.WithTopLevel(owner, topLevel => AvaloniaCommonDialogs.ShowFileDialogAsync(topLevel, request));
    }

    public System.Drawing.Color? ShowColorDialog(nint owner, System.Drawing.Color color)
    {
        ColorPickerWindow window = new(color, LoadStrings());
        ShowDialog(window, owner);
        return window.SelectedColor;
    }

    public FontDescriptor? ShowFontDialog(nint owner, FontDescriptor? font, bool fixedPitchOnly)
    {
        AvaloniaUi.EnsureInitialized(AvaloniaDialogs.GetOptions);
        FontPickerWindow window = new(font, fixedPitchOnly, LoadStrings());
        ShowDialog(window, owner);
        return window.SelectedFont;
    }

    private static DialogBoxStrings LoadStrings()
    {
        AvaloniaUi.EnsureInitialized(AvaloniaDialogs.GetOptions);
        return ViewStrings.Load<DialogBoxStrings>();
    }

    private static void ShowDialog(DialogWindow window, nint owner)
        => AvaloniaDialogHost.ShowDialog(window, owner != 0 ? owner : AvaloniaDialogHost.GetActiveWindowHandle());
}
