using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace GitUI.Avalonia.Hosting;

/// <summary>
///  Shows Avalonia dialogs modally over native (WinForms) owner windows.
/// </summary>
/// <remarks>
///  Avalonia's own <c>ShowDialog</c> only accepts an Avalonia owner. While WinForms owns the main window, this
///  reproduces what WinForms' <c>Form.ShowDialog</c> does: it disables the other windows of the UI thread, sets the
///  native owner (so the dialog stays above it and is minimised with it), and runs a nested message loop until
///  the dialog closes. Avalonia's loop dispatches all Win32 messages of the thread, so WinForms windows keep
///  painting, and WinForms/JoinableTaskFactory continuations posted to the UI thread keep running.
/// </remarks>
public static class AvaloniaDialogHost
{
    /// <summary>
    ///  Raised on the UI thread just before a dialog is shown; lets integration tests drive the modal dialog.
    /// </summary>
    internal static Action<DialogWindow>? DialogShowingForTests { get; set; }

    /// <summary>
    ///  Shows <paramref name="window"/> modally and returns whether it was accepted.
    /// </summary>
    /// <param name="window">The dialog to show.</param>
    /// <param name="ownerHandle">
    ///  Native handle of the owner (any window or control of it), or 0 for an unowned dialog such as
    ///  one started from the command line.
    /// </param>
    public static bool ShowDialog(DialogWindow window, nint ownerHandle)
    {
        AvaloniaUi.VerifyUiThread();
        AvaloniaUi.RememberHostContext();

        nint owner = ownerHandle == 0 ? 0 : NativeMethods.GetAncestor(ownerHandle, NativeMethods.GA_ROOT);
        nint dialog = window.NativeHandle;

        if (owner != 0 && dialog != 0)
        {
            NativeMethods.SetWindowLongPtr(dialog, NativeMethods.GWLP_HWNDPARENT, owner);
            window.ShowInTaskbar = false;
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.IsCenteredOnOwner = true;
            CenterOver(window, owner);
        }
        else
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        List<nint> disabledWindows = NativeMethods.DisableEnabledThreadWindows(except: dialog);
        bool reenabled = false;
        void ReenableWindows()
        {
            if (!reenabled)
            {
                reenabled = true;
                NativeMethods.EnableWindows(disabledWindows);
            }
        }

        DispatcherFrame frame = new();

        // Re-enable the owner before the dialog is destroyed, otherwise Windows activates another application.
        window.Closing += (_, e) =>
        {
            if (!e.Cancel)
            {
                ReenableWindows();
            }
        };
        window.Closed += (_, _) =>
        {
            ReenableWindows();
            frame.Continue = false;
        };

        DialogShowingForTests?.Invoke(window);
        window.Show();
        Dispatcher.UIThread.PushFrame(frame);

        if (owner != 0)
        {
            NativeMethods.SetForegroundWindow(owner);
        }

        return window.DialogResult;
    }

    private static void CenterOver(Window window, nint owner)
    {
        if (!NativeMethods.GetWindowRect(owner, out NativeMethods.RECT ownerRect))
        {
            return;
        }

        void Center()
        {
            // A size restored on opening is set but not laid out yet: use it rather than the current client size.
            Size clientSize = new(
                double.IsNaN(window.Width) || window.SizeToContent.HasFlag(SizeToContent.Width) ? window.ClientSize.Width : window.Width,
                double.IsNaN(window.Height) || window.SizeToContent.HasFlag(SizeToContent.Height) ? window.ClientSize.Height : window.Height);
            Size frame = window.FrameSize is { } frameSize ? new Size(frameSize.Width - window.ClientSize.Width, frameSize.Height - window.ClientSize.Height) : default;
            PixelSize pixels = PixelSize.FromSize(new Size(clientSize.Width + frame.Width, clientSize.Height + frame.Height), window.DesktopScaling);
            window.Position = new PixelPoint(
                ownerRect.Left + ((ownerRect.Right - ownerRect.Left - pixels.Width) / 2),
                ownerRect.Top + ((ownerRect.Bottom - ownerRect.Top - pixels.Height) / 2));
        }

        // The size is only final once the window has been laid out (e.g. SizeToContent); center again then.
        if (!double.IsNaN(window.Width) && !double.IsNaN(window.Height))
        {
            window.Position = new PixelPoint(
                ownerRect.Left + (int)((ownerRect.Right - ownerRect.Left - (window.Width * window.DesktopScaling)) / 2),
                ownerRect.Top + (int)((ownerRect.Bottom - ownerRect.Top - (window.Height * window.DesktopScaling)) / 2));
        }

        window.Opened += (_, _) => Center();
    }
}
