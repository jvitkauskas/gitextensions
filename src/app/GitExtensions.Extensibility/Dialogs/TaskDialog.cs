using System.Runtime.InteropServices;

namespace GitExtensions.Extensibility;

/// <summary>The icon of a task dialog (the standard icons of the WinForms <c>TaskDialogIcon</c>).</summary>
public sealed class TaskDialogIcon
{
    private TaskDialogIcon(int resourceId)
    {
        ResourceId = resourceId;
    }

    public static TaskDialogIcon None { get; } = new(0);

    public static TaskDialogIcon Information { get; } = new(-3);

    public static TaskDialogIcon Warning { get; } = new(-1);

    public static TaskDialogIcon Error { get; } = new(-2);

    public static TaskDialogIcon Shield { get; } = new(-4);

    /// <summary>The <c>TD_*_ICON</c> resource of the icon (<c>MAKEINTRESOURCE</c> of a negative number).</summary>
    internal int ResourceId { get; }
}

/// <summary>A button of a task dialog: a standard one (e.g. <see cref="Yes"/>) or one with a text.</summary>
public class TaskDialogButton
{
    /// <summary>A button with <paramref name="text"/>.</summary>
    public TaskDialogButton(string? text = null, bool enabled = true, bool allowCloseDialog = true)
    {
        Text = text;
        Enabled = enabled;
        AllowCloseDialog = allowCloseDialog;
    }

    private TaskDialogButton(int standardId, int commonButtonFlag)
    {
        StandardId = standardId;
        CommonButtonFlag = commonButtonFlag;
    }

    public static TaskDialogButton OK => new(1, 0x0001);

    public static TaskDialogButton Cancel => new(2, 0x0008);

    public static TaskDialogButton Abort => new(3, 0x10000);

    public static TaskDialogButton Retry => new(4, 0x0010);

    public static TaskDialogButton Ignore => new(5, 0x20000);

    public static TaskDialogButton Yes => new(6, 0x0002);

    public static TaskDialogButton No => new(7, 0x0004);

    public static TaskDialogButton Close => new(8, 0x0020);

    public static TaskDialogButton Help => new(9, 0x100000);

    public static TaskDialogButton TryAgain => new(10, 0x40000);

    public static TaskDialogButton Continue => new(11, 0x80000);

    /// <summary>Raised when the button is clicked, before the dialog closes (unless <see cref="AllowCloseDialog"/> is off).</summary>
    public event EventHandler? Click;

    /// <summary>The text of a custom button.</summary>
    public string? Text { get; set; }

    /// <summary>Whether clicking the button closes the dialog.</summary>
    public bool AllowCloseDialog { get; set; } = true;

    /// <summary>Whether the button can be clicked.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Data of the caller.</summary>
    public object? Tag { get; set; }

    /// <summary>The dialog result id (<c>IDOK</c>, <c>IDYES</c>…) of a standard button; 0 for a custom button.</summary>
    internal int StandardId { get; }

    /// <summary>The <c>TDCBF_*_BUTTON</c> flag of a standard button.</summary>
    internal int CommonButtonFlag { get; }

    internal bool IsStandard => StandardId != 0;

    /// <summary>The standard buttons are equal by their id, as the static properties create new instances.</summary>
    public override bool Equals(object? obj)
        => obj is TaskDialogButton other && (ReferenceEquals(this, other) || (IsStandard && StandardId == other.StandardId));

    public override int GetHashCode() => IsStandard ? StandardId : base.GetHashCode();

    public static bool operator ==(TaskDialogButton? left, TaskDialogButton? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(TaskDialogButton? left, TaskDialogButton? right) => !(left == right);

    /// <summary>Raises <see cref="Click"/>, as when the button is clicked (for the dialogs of other UI than Win32).</summary>
    /// <returns>Whether the dialog closes (<see cref="AllowCloseDialog"/>).</returns>
    public bool PerformClick()
    {
        Click?.Invoke(this, EventArgs.Empty);
        return AllowCloseDialog;
    }
}

/// <summary>A command link of a task dialog: a big button with a text and a description.</summary>
public sealed class TaskDialogCommandLinkButton : TaskDialogButton
{
    public TaskDialogCommandLinkButton(string? text = null, string? descriptionText = null, bool enabled = true, bool allowCloseDialog = true)
        : base(text, enabled, allowCloseDialog)
    {
        DescriptionText = descriptionText;
    }

    /// <summary>The note below the text.</summary>
    public string? DescriptionText { get; set; }
}

/// <summary>The check box of a task dialog (e.g. "Don't show again").</summary>
public sealed class TaskDialogVerificationCheckBox
{
    public string? Text { get; set; }

    public bool Checked { get; set; }
}

/// <summary>Where the text of a task dialog expander is shown.</summary>
public enum TaskDialogExpanderPosition
{
    AfterText,
    AfterFootnote,
}

/// <summary>The details of a task dialog, shown or hidden with a button.</summary>
public sealed class TaskDialogExpander
{
    public string? Text { get; set; }

    public string? ExpandedButtonText { get; set; }

    public string? CollapsedButtonText { get; set; }

    public bool Expanded { get; set; }

    public TaskDialogExpanderPosition Position { get; set; }
}

/// <summary>The link clicked in a task dialog.</summary>
public sealed class TaskDialogLinkClickedEventArgs(string linkHref) : EventArgs
{
    public string LinkHref { get; } = linkHref;
}

/// <summary>The contents of a task dialog (as the WinForms <c>TaskDialogPage</c>).</summary>
public sealed class TaskDialogPage
{
    public string? Caption { get; set; }

    public string? Heading { get; set; }

    public string? Text { get; set; }

    public string? Footnote { get; set; }

    public TaskDialogIcon? Icon { get; set; }

    public List<TaskDialogButton> Buttons { get; } = [];

    public TaskDialogButton? DefaultButton { get; set; }

    public TaskDialogVerificationCheckBox? Verification { get; set; }

    public TaskDialogExpander? Expander { get; set; }

    /// <summary>Whether the dialog can be closed with Escape or the close button without a Cancel button.</summary>
    public bool AllowCancel { get; set; }

    public bool AllowMinimize { get; set; }

    public bool SizeToContent { get; set; }

    /// <summary>Whether the <c>&lt;a href="…"&gt;</c> links of the texts are links (see <see cref="LinkClicked"/>).</summary>
    public bool EnableLinks { get; set; }

    public event EventHandler<TaskDialogLinkClickedEventArgs>? LinkClicked;

    /// <summary>Raises <see cref="LinkClicked"/>, as when the link is clicked (for the dialogs of other UI than Win32).</summary>
    public void PerformLinkClick(string href) => LinkClicked?.Invoke(this, new TaskDialogLinkClickedEventArgs(href));
}

/// <summary>
///  The native (comctl32) task dialog, as the WinForms <c>TaskDialog</c> shows it (docs/avalonia-port/PLAN.md, phase 8); off
///  Windows the task dialog of the UI of the application (<see cref="DialogBoxHost"/>).
/// </summary>
public static class TaskDialog
{
    private const int TDF_ENABLE_HYPERLINKS = 0x0001;
    private const int TDF_ALLOW_DIALOG_CANCELLATION = 0x0008;
    private const int TDF_USE_COMMAND_LINKS = 0x0010;
    private const int TDF_EXPAND_FOOTER_AREA = 0x0040;
    private const int TDF_EXPANDED_BY_DEFAULT = 0x0080;
    private const int TDF_VERIFICATION_FLAG_CHECKED = 0x0100;
    private const int TDF_POSITION_RELATIVE_TO_WINDOW = 0x1000;
    private const int TDF_CAN_BE_MINIMIZED = 0x8000;
    private const int TDF_SIZE_TO_CONTENT = 0x01000000;

    private const int TDN_CREATED = 0;
    private const int TDN_BUTTON_CLICKED = 2;
    private const int TDM_ENABLE_BUTTON = 0x0400 + 111;
    private const int TDN_HYPERLINK_CLICKED = 3;
    private const int TDN_VERIFICATION_CLICKED = 8;
    private const int TDN_EXPANDO_BUTTON_CLICKED = 10;

    private const int S_OK = 0;
    private const int S_FALSE = 1;

    private const int FirstCustomButtonId = 100;

    /// <summary>Shows <paramref name="page"/> over the active window of the application.</summary>
    public static TaskDialogButton ShowDialog(TaskDialogPage page) => ShowDialog(0, page);

    /// <summary>Shows <paramref name="page"/> over <paramref name="owner"/>.</summary>
    public static TaskDialogButton ShowDialog(IWin32Window? owner, TaskDialogPage page) => ShowDialog(owner?.Handle ?? 0, page);

    /// <summary>Shows <paramref name="page"/> over the window <paramref name="hwndOwner"/> (or the active window if 0).</summary>
    public static TaskDialogButton ShowDialog(nint hwndOwner, TaskDialogPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        if (DialogBoxHost.Active is { } host)
        {
            return host.ShowTaskDialog(hwndOwner, page);
        }

        return OperatingSystem.IsWindows() ? ShowNativeDialog(hwndOwner, page) : throw new PlatformNotSupportedException();
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static TaskDialogButton ShowNativeDialog(nint hwndOwner, TaskDialogPage page)
    {
        if (hwndOwner == 0)
        {
            hwndOwner = DialogNativeMethods.GetActiveWindow();
        }

        List<nint> strings = [];
        nint buttonsMemory = 0;
        try
        {
            nint Str(string? text)
            {
                if (string.IsNullOrEmpty(text))
                {
                    return 0;
                }

                nint pointer = Marshal.StringToHGlobalUni(text);
                strings.Add(pointer);
                return pointer;
            }

            List<TaskDialogButton> buttons = page.Buttons;
            List<(TaskDialogButton Button, int Id)> customButtons = [];
            int commonButtons = 0;
            bool hasCommandLinks = false;
            foreach (TaskDialogButton button in buttons)
            {
                if (button.IsStandard)
                {
                    commonButtons |= button.CommonButtonFlag;
                }
                else
                {
                    customButtons.Add((button, FirstCustomButtonId + customButtons.Count));
                    hasCommandLinks |= button is TaskDialogCommandLinkButton;
                }
            }

            int ButtonId(TaskDialogButton button)
                => button.IsStandard ? button.StandardId : customButtons.FirstOrDefault(b => ReferenceEquals(b.Button, button)).Id;

            TaskDialogButton? ButtonOf(int id)
                => customButtons.FirstOrDefault(b => b.Id == id).Button ?? buttons.FirstOrDefault(b => b.IsStandard && b.StandardId == id);

            if (customButtons.Count > 0)
            {
                int size = Marshal.SizeOf<DialogNativeMethods.TaskDialogButtonData>();
                buttonsMemory = Marshal.AllocHGlobal(size * customButtons.Count);
                for (int i = 0; i < customButtons.Count; i++)
                {
                    (TaskDialogButton button, int id) = customButtons[i];
                    string? text = button is TaskDialogCommandLinkButton { DescriptionText: { Length: > 0 } description }
                        ? $"{button.Text}\n{description}"
                        : button.Text;
                    Marshal.StructureToPtr(new DialogNativeMethods.TaskDialogButtonData { Id = id, Text = Str(text) }, buttonsMemory + (i * size), fDeleteOld: false);
                }
            }

            int flags = TDF_POSITION_RELATIVE_TO_WINDOW;
            if (page.AllowCancel)
            {
                flags |= TDF_ALLOW_DIALOG_CANCELLATION;
            }

            if (page.AllowMinimize)
            {
                flags |= TDF_CAN_BE_MINIMIZED;
            }

            if (page.SizeToContent)
            {
                flags |= TDF_SIZE_TO_CONTENT;
            }

            if (page.EnableLinks)
            {
                flags |= TDF_ENABLE_HYPERLINKS;
            }

            if (hasCommandLinks)
            {
                flags |= TDF_USE_COMMAND_LINKS;
            }

            if (page.Verification?.Checked == true)
            {
                flags |= TDF_VERIFICATION_FLAG_CHECKED;
            }

            if (page.Expander is { } expander)
            {
                if (expander.Expanded)
                {
                    flags |= TDF_EXPANDED_BY_DEFAULT;
                }

                if (expander.Position == TaskDialogExpanderPosition.AfterFootnote)
                {
                    flags |= TDF_EXPAND_FOOTER_AREA;
                }
            }

            DialogNativeMethods.TaskDialogCallback callback = (hwnd, notification, wordParameter, longParameter, _) =>
            {
                try
                {
                    switch (notification)
                    {
                        case TDN_CREATED:
                            foreach (TaskDialogButton button in buttons.Where(button => !button.Enabled))
                            {
                                DialogNativeMethods.SendMessageW(hwnd, TDM_ENABLE_BUTTON, ButtonId(button), 0);
                            }

                            break;
                        case TDN_BUTTON_CLICKED:
                            return ButtonOf((int)wordParameter) is { } clicked && !clicked.PerformClick() ? S_FALSE : S_OK;
                        case TDN_HYPERLINK_CLICKED:
                            page.PerformLinkClick(Marshal.PtrToStringUni(longParameter) ?? "");
                            break;
                        case TDN_VERIFICATION_CLICKED:
                            page.Verification?.Checked = wordParameter != 0;
                            break;
                        case TDN_EXPANDO_BUTTON_CLICKED:
                            page.Expander?.Expanded = wordParameter != 0;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    // An exception must not cross the native frames of the dialog; report it once it is closed.
                    _pendingException ??= ex;
                }

                return S_OK;
            };

            DialogNativeMethods.TaskDialogConfig config = new()
            {
                Size = (uint)Marshal.SizeOf<DialogNativeMethods.TaskDialogConfig>(),
                Parent = hwndOwner,
                Flags = flags,
                CommonButtons = commonButtons,
                WindowTitle = Str(page.Caption),
                MainIcon = page.Icon is { ResourceId: not 0 } icon ? (nint)(ushort)icon.ResourceId : 0,
                MainInstruction = Str(page.Heading),
                Content = Str(page.Text),
                ButtonCount = (uint)customButtons.Count,
                Buttons = buttonsMemory,
                DefaultButton = page.DefaultButton is { } defaultButton ? ButtonId(defaultButton) : 0,
                VerificationText = Str(page.Verification?.Text),
                ExpandedInformation = Str(page.Expander?.Text),
                ExpandedControlText = Str(page.Expander?.ExpandedButtonText),
                CollapsedControlText = Str(page.Expander?.CollapsedButtonText),
                Footer = Str(page.Footnote),
                Callback = Marshal.GetFunctionPointerForDelegate(callback),
            };

            int result;
            bool verificationChecked;
            using (DialogNativeMethods.ThemingScope theming = new())
            {
                int hresult = DialogNativeMethods.TaskDialogIndirect(ref config, out result, out _, out verificationChecked);
                GC.KeepAlive(callback);
                Marshal.ThrowExceptionForHR(hresult);
            }

            if (_pendingException is { } exception)
            {
                _pendingException = null;
                throw new InvalidOperationException("A handler of the task dialog failed.", exception);
            }

            page.Verification?.Checked = verificationChecked;
            return ButtonOf(result) ?? (result == 2 ? TaskDialogButton.Cancel : TaskDialogButton.OK);
        }
        finally
        {
            foreach (nint pointer in strings)
            {
                Marshal.FreeHGlobal(pointer);
            }

            if (buttonsMemory != 0)
            {
                Marshal.FreeHGlobal(buttonsMemory);
            }
        }
    }

    /// <summary>Shows <paramref name="page"/> once the caller has returned to the message loop.</summary>
    public static async Task<TaskDialogButton> ShowDialogAsync(TaskDialogPage page)
    {
        await Task.Yield();
        return ShowDialog(page);
    }

    /// <summary>Shows <paramref name="page"/> over <paramref name="hwndOwner"/> once the caller has returned to the message loop.</summary>
    public static async Task<TaskDialogButton> ShowDialogAsync(nint hwndOwner, TaskDialogPage page)
    {
        await Task.Yield();
        return ShowDialog(hwndOwner, page);
    }

    [ThreadStatic]
    private static Exception? _pendingException;
}
