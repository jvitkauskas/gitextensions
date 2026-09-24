namespace GitExtensions.Extensibility;

/// <summary>The buttons of a message box of <see cref="PluginMessageBoxes"/> (plugin API v2).</summary>
public enum PluginMessageBoxButtons
{
    Ok,
    OkCancel,
    YesNo,
    YesNoCancel,
    RetryCancel,
    AbortRetryIgnore,
}

/// <summary>The icon of a message box of <see cref="PluginMessageBoxes"/> (plugin API v2).</summary>
public enum PluginMessageBoxIcon
{
    None,
    Information,
    Warning,
    Error,
    Question,
}

/// <summary>The button of a message box of <see cref="PluginMessageBoxes"/> that is selected first (plugin API v2).</summary>
public enum PluginMessageBoxDefaultButton
{
    Button1,
    Button2,
    Button3,
}

/// <summary>The button the user chose in a message box of <see cref="PluginMessageBoxes"/> (plugin API v2).</summary>
public enum PluginMessageBoxResult
{
    /// <summary>The message box was closed without a button, e.g. by the host.</summary>
    None,
    Ok,
    Cancel,
    Yes,
    No,
    Retry,
    Abort,
    Ignore,
}

/// <summary>
///  The message boxes of the plugins (plugin API v2): as <see cref="MessageBoxes"/>, which is typed with WinForms, but with an
///  owner of any UI framework (<see cref="WindowOwner"/>) and without WinForms types.
/// </summary>
/// <remarks>
///  They are shown by WinForms for now (<see cref="MessageBoxes"/> and <c>TaskDialog</c>); the host replaces them by its own
///  when WinForms goes away (docs/avalonia-port/PLAN.md, phase 8), without changes to the plugins.
/// </remarks>
public static class PluginMessageBoxes
{
    /// <summary>The implementation; replaced by tests (and by the host once WinForms goes away).</summary>
    internal static IPluginMessageBoxService Service { get; set; } = NativePluginMessageBoxService.Instance;

    /// <summary>Shows a message box over <paramref name="owner"/>.</summary>
    /// <returns>The button the user chose.</returns>
    public static PluginMessageBoxResult Show(
        WindowOwner owner,
        string text,
        string caption,
        PluginMessageBoxButtons buttons = PluginMessageBoxButtons.Ok,
        PluginMessageBoxIcon icon = PluginMessageBoxIcon.None,
        PluginMessageBoxDefaultButton defaultButton = PluginMessageBoxDefaultButton.Button1)
        => Service.Show(owner, text, caption, buttons, icon, defaultButton);

    /// <summary>Shows an error message, with the caption "Error" by default (as <see cref="MessageBoxes.ShowError"/>).</summary>
    public static void ShowError(WindowOwner owner, string text, string? caption = null)
        => Show(owner, text, caption ?? "Error", PluginMessageBoxButtons.Ok, PluginMessageBoxIcon.Error);

    /// <summary>Shows a warning.</summary>
    public static void ShowWarning(WindowOwner owner, string text, string caption)
        => Show(owner, text, caption, PluginMessageBoxButtons.Ok, PluginMessageBoxIcon.Warning);

    /// <summary>Shows an information.</summary>
    public static void ShowInformation(WindowOwner owner, string text, string caption)
        => Show(owner, text, caption, PluginMessageBoxButtons.Ok, PluginMessageBoxIcon.Information);

    /// <summary>Asks a Yes/No question (as <see cref="MessageBoxes.Confirm"/>).</summary>
    /// <returns><see langword="true"/> if the user chose Yes.</returns>
    public static bool Confirm(
        WindowOwner owner,
        string text,
        string caption,
        PluginMessageBoxIcon icon = PluginMessageBoxIcon.Question,
        PluginMessageBoxDefaultButton defaultButton = PluginMessageBoxDefaultButton.Button1)
        => Show(owner, text, caption, PluginMessageBoxButtons.YesNo, icon, defaultButton) == PluginMessageBoxResult.Yes;

    /// <summary>
    ///  Asks to choose between buttons with the given texts (as a WinForms <c>TaskDialog</c> with custom buttons), without
    ///  blocking the caller.
    /// </summary>
    /// <param name="heading">The main instruction.</param>
    /// <param name="text">The text below the heading, if any.</param>
    /// <param name="buttons">The texts of the buttons, in order.</param>
    /// <returns>The index of the chosen button in <paramref name="buttons"/>, or -1 if the dialog was cancelled (Esc, close).</returns>
    public static Task<int> ShowChoiceAsync(
        WindowOwner owner,
        string caption,
        string heading,
        string? text,
        PluginMessageBoxIcon icon,
        IReadOnlyList<string> buttons)
    {
        ArgumentNullException.ThrowIfNull(buttons);
        if (buttons.Count == 0)
        {
            throw new ArgumentException("At least one button is needed.", nameof(buttons));
        }

        return Service.ShowChoiceAsync(owner, caption, heading, text, icon, buttons);
    }
}

/// <summary>Shows the message boxes of <see cref="PluginMessageBoxes"/>.</summary>
internal interface IPluginMessageBoxService
{
    PluginMessageBoxResult Show(WindowOwner owner, string text, string caption, PluginMessageBoxButtons buttons, PluginMessageBoxIcon icon, PluginMessageBoxDefaultButton defaultButton);

    Task<int> ShowChoiceAsync(WindowOwner owner, string caption, string heading, string? text, PluginMessageBoxIcon icon, IReadOnlyList<string> buttons);
}

/// <summary>
///  The message boxes of <see cref="PluginMessageBoxes"/> shown by WinForms: <see cref="MessageBoxes"/> and <c>TaskDialog</c>.
///  The one place to replace when WinForms goes away (docs/avalonia-port/PLAN.md, phase 8).
/// </summary>
internal sealed class NativePluginMessageBoxService : IPluginMessageBoxService
{
    public static NativePluginMessageBoxService Instance { get; } = new();

    private NativePluginMessageBoxService()
    {
    }

    public PluginMessageBoxResult Show(WindowOwner owner, string text, string caption, PluginMessageBoxButtons buttons, PluginMessageBoxIcon icon, PluginMessageBoxDefaultButton defaultButton)
        => ToResult(MessageBoxes.Show(owner.ToWin32Window(), text, caption, ToNative(buttons), ToNative(icon), ToNative(defaultButton)));

    public async Task<int> ShowChoiceAsync(WindowOwner owner, string caption, string heading, string? text, PluginMessageBoxIcon icon, IReadOnlyList<string> buttons)
    {
        TaskDialogButton[] taskDialogButtons = [.. buttons.Select(button => new TaskDialogButton(button))];
        TaskDialogPage page = new()
        {
            Caption = caption,
            Heading = heading,
            Text = text,
            Icon = ToTaskDialogIcon(icon),
            AllowCancel = true,
        };
        foreach (TaskDialogButton button in taskDialogButtons)
        {
            page.Buttons.Add(button);
        }

        TaskDialogButton result = owner.IsNone
            ? await TaskDialog.ShowDialogAsync(page)
            : await TaskDialog.ShowDialogAsync(owner.Handle, page);
        return Array.IndexOf(taskDialogButtons, result);
    }

    internal static MessageBoxButtons ToNative(PluginMessageBoxButtons buttons) => buttons switch
    {
        PluginMessageBoxButtons.Ok => MessageBoxButtons.OK,
        PluginMessageBoxButtons.OkCancel => MessageBoxButtons.OKCancel,
        PluginMessageBoxButtons.YesNo => MessageBoxButtons.YesNo,
        PluginMessageBoxButtons.YesNoCancel => MessageBoxButtons.YesNoCancel,
        PluginMessageBoxButtons.RetryCancel => MessageBoxButtons.RetryCancel,
        PluginMessageBoxButtons.AbortRetryIgnore => MessageBoxButtons.AbortRetryIgnore,
        _ => throw new ArgumentOutOfRangeException(nameof(buttons), buttons, null),
    };

    internal static MessageBoxIcon ToNative(PluginMessageBoxIcon icon) => icon switch
    {
        PluginMessageBoxIcon.None => MessageBoxIcon.None,
        PluginMessageBoxIcon.Information => MessageBoxIcon.Information,
        PluginMessageBoxIcon.Warning => MessageBoxIcon.Warning,
        PluginMessageBoxIcon.Error => MessageBoxIcon.Error,
        PluginMessageBoxIcon.Question => MessageBoxIcon.Question,
        _ => throw new ArgumentOutOfRangeException(nameof(icon), icon, null),
    };

    internal static MessageBoxDefaultButton ToNative(PluginMessageBoxDefaultButton defaultButton) => defaultButton switch
    {
        PluginMessageBoxDefaultButton.Button1 => MessageBoxDefaultButton.Button1,
        PluginMessageBoxDefaultButton.Button2 => MessageBoxDefaultButton.Button2,
        PluginMessageBoxDefaultButton.Button3 => MessageBoxDefaultButton.Button3,
        _ => throw new ArgumentOutOfRangeException(nameof(defaultButton), defaultButton, null),
    };

    internal static PluginMessageBoxResult ToResult(DialogResult result) => result switch
    {
        DialogResult.OK => PluginMessageBoxResult.Ok,
        DialogResult.Cancel => PluginMessageBoxResult.Cancel,
        DialogResult.Yes => PluginMessageBoxResult.Yes,
        DialogResult.No => PluginMessageBoxResult.No,
        DialogResult.Retry => PluginMessageBoxResult.Retry,
        DialogResult.Abort => PluginMessageBoxResult.Abort,
        DialogResult.Ignore => PluginMessageBoxResult.Ignore,
        _ => PluginMessageBoxResult.None,
    };

    internal static TaskDialogIcon? ToTaskDialogIcon(PluginMessageBoxIcon icon) => icon switch
    {
        PluginMessageBoxIcon.Information => TaskDialogIcon.Information,
        PluginMessageBoxIcon.Warning => TaskDialogIcon.Warning,
        PluginMessageBoxIcon.Error => TaskDialogIcon.Error,

        // As MessageBoxIcon.Question: TaskDialog has no question icon.
        PluginMessageBoxIcon.Question => TaskDialogIcon.Information,
        _ => null,
    };
}
