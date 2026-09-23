namespace GitUI.Presentation.CommandsDialogs.SettingsDialog;

/// <summary>
///  View model of the read-only help text window (port of <c>SimpleHelpDisplayDialog</c>); its texts come from
///  the caller, which translates them.
/// </summary>
public sealed class SimpleHelpDisplayViewModel(string title, string content) : DialogViewModel
{
    public string Title { get; } = title;

    public string Content { get; } = content;
}
