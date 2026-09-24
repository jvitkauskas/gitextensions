using System.Runtime.InteropServices;

namespace GitExtensions.Extensibility;

/// <summary>The button of a message box that closed it (the values of the WinForms <c>DialogResult</c>).</summary>
public enum DialogResult
{
    None = 0,
    OK = 1,
    Cancel = 2,
    Abort = 3,
    Retry = 4,
    Ignore = 5,
    Yes = 6,
    No = 7,
    TryAgain = 10,
    Continue = 11,
}

/// <summary>The buttons of a message box (the values of the WinForms <c>MessageBoxButtons</c>).</summary>
public enum MessageBoxButtons
{
    OK = 0x00000000,
    OKCancel = 0x00000001,
    AbortRetryIgnore = 0x00000002,
    YesNoCancel = 0x00000003,
    YesNo = 0x00000004,
    RetryCancel = 0x00000005,
    CancelTryContinue = 0x00000006,
}

/// <summary>The icon of a message box (the values of the WinForms <c>MessageBoxIcon</c>).</summary>
public enum MessageBoxIcon
{
    None = 0,
    Hand = 0x00000010,
    Question = 0x00000020,
    Exclamation = 0x00000030,
    Asterisk = 0x00000040,
    Stop = Hand,
    Error = Hand,
    Warning = Exclamation,
    Information = Asterisk,
}

/// <summary>The default button of a message box (the values of the WinForms <c>MessageBoxDefaultButton</c>).</summary>
public enum MessageBoxDefaultButton
{
    Button1 = 0x00000000,
    Button2 = 0x00000100,
    Button3 = 0x00000200,
    Button4 = 0x00000300,
}

/// <summary>The native (Win32) message box, as the WinForms <c>MessageBox</c> shows it.</summary>
public static class NativeMessageBox
{
    /// <summary>
    ///  Shows a modal message box over <paramref name="owner"/>, or over the active window of the application if none.
    /// </summary>
    public static DialogResult Show(IWin32Window? owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1)
    {
        nint hwndOwner = owner?.Handle ?? 0;
        if (hwndOwner == 0)
        {
            hwndOwner = DialogNativeMethods.GetActiveWindow();
        }

        using DialogNativeMethods.ThemingScope theming = new();
        return (DialogResult)DialogNativeMethods.MessageBoxW(hwndOwner, text, caption, (int)buttons | (int)icon | (int)defaultButton);
    }
}
