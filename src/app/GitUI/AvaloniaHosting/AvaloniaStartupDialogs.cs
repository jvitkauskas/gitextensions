namespace GitUI.AvaloniaHosting;

/// <summary>Avalonia ports of the dialogs that the application shows at startup, before the main window.</summary>
public static class AvaloniaStartupDialogs
{
    /// <summary>
    ///  Shows the Avalonia port of <c>FormChooseTranslation</c>, which sets <c>AppSettings.Translation</c>;
    ///  returns <see langword="false"/> if the port is disabled.
    /// </summary>
    public static bool TryShowChooseTranslation() => AvaloniaDialogs.TryShowChooseTranslation(owner: null);

    /// <summary>As <c>FormFixHome.CheckHomePath</c>: the dialog to fix HOME if it has no git configuration.</summary>
    public static void CheckHomePath() => HomeDirectoryCheck.CheckHomePath();

    /// <summary>
    ///  As <c>ChecklistSettingsPage.CheckSettings</c> at startup: whether all the checks of the checklist pass (the settings
    ///  dialog is shown otherwise).
    /// </summary>
    public static bool CheckSettings(GitExtensions.Extensibility.Git.IGitUICommands commands, GitUI.CommandsDialogs.SettingsDialog.CommonLogic commonLogic)
        => AvaloniaDialogs.CheckSettings(commands, commonLogic);
}
