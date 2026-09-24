using GitExtensions.Extensibility.Settings;

namespace GitExtensions.Extensibility.Git;

/// <summary>
///  The <see cref="IGitUICommands"/> used by the plugins, with an owner window of plugin API v2 (<see cref="WindowOwner"/>)
///  instead of a WinForms owner: each calls the member of <see cref="IGitUICommands"/> with the owner as a WinForms owner.
/// </summary>
public static class GitUICommandsExtensions
{
    /// <summary>As <see cref="IGitUICommands.StartCommandLineProcessDialog(IWin32Window?, IGitCommand)"/>.</summary>
    public static bool StartCommandLineProcessDialog(this IGitUICommands commands, WindowOwner owner, IGitCommand command)
        => commands.StartCommandLineProcessDialog(owner.ToWin32Window(), command);

    /// <summary>As <see cref="IGitUICommands.StartCommandLineProcessDialog(IWin32Window?, string?, ArgumentString)"/>.</summary>
    public static bool StartCommandLineProcessDialog(this IGitUICommands commands, WindowOwner owner, string? command, ArgumentString arguments)
        => commands.StartCommandLineProcessDialog(owner.ToWin32Window(), command, arguments);

    /// <summary>As <see cref="IGitUICommands.StartGitCommandProcessDialog(IWin32Window?, ArgumentString)"/>.</summary>
    public static bool StartGitCommandProcessDialog(this IGitUICommands commands, WindowOwner owner, ArgumentString arguments)
        => commands.StartGitCommandProcessDialog(owner.ToWin32Window(), arguments);

    /// <summary>As <see cref="IGitUICommands.StartSettingsDialog(IWin32Window?, SettingsPageReference?)"/>.</summary>
    public static bool StartSettingsDialog(this IGitUICommands commands, WindowOwner owner, SettingsPageReference? initialPage = null)
        => commands.StartSettingsDialog(owner.ToWin32Window(), initialPage);

    /// <summary>As <see cref="IGitUICommands.StartPluginSettingsDialog(IWin32Window?)"/>.</summary>
    public static bool StartPluginSettingsDialog(this IGitUICommands commands, WindowOwner owner)
        => commands.StartPluginSettingsDialog(owner.ToWin32Window());

    /// <summary>As <see cref="IGitUICommands.StartBrowseDialog(IWin32Window?, BrowseArguments?)"/>.</summary>
    public static bool StartBrowseDialog(this IGitUICommands commands, WindowOwner owner, BrowseArguments? args = null)
        => commands.StartBrowseDialog(owner.ToWin32Window(), args);

    /// <summary>As <see cref="IGitUICommands.StartCommitDialog(IWin32Window?, string?, bool)"/>.</summary>
    public static bool StartCommitDialog(this IGitUICommands commands, WindowOwner owner, string? commitMessage = null, bool showOnlyWhenChanges = false)
        => commands.StartCommitDialog(owner.ToWin32Window(), commitMessage, showOnlyWhenChanges);
}
