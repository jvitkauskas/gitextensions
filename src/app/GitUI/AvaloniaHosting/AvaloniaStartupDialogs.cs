namespace GitUI.AvaloniaHosting;

/// <summary>Avalonia ports of the dialogs that the application shows at startup, before the main window.</summary>
public static class AvaloniaStartupDialogs
{
    /// <summary>
    ///  Sets up Avalonia on the UI thread of the application, whose synchronization context it then is (as WinForms did
    ///  before, docs/avalonia-port/PLAN.md, phase 8): the shared <see cref="ThreadHelper.JoinableTaskContext"/> is to be
    ///  created on it.
    /// </summary>
    public static void InitializeUi()
    {
        GitUI.Avalonia.Hosting.AvaloniaUi.EnsureInitialized(AvaloniaDialogs.GetOptions);
        global::Avalonia.Threading.AvaloniaSynchronizationContext.InstallIfNeeded();
    }

    /// <summary>
    ///  Shows the Avalonia port of <c>FormChooseTranslation</c>, which sets <c>AppSettings.Translation</c>;
    ///  returns <see langword="false"/> if the port is disabled.
    /// </summary>
    public static bool TryShowChooseTranslation() => AvaloniaDialogs.TryShowChooseTranslation(owner: null);

    /// <summary>
    ///  The askpass mode (<c>GitExtensions askpass &lt;prompt&gt;</c>, off Windows): the prompt of ssh or git, whose answer
    ///  is written to the standard output; the exit code is 1 if the prompt is cancelled.
    /// </summary>
    public static int RunAskPass(string prompt)
    {
        InitializeUi();
        if (AvaloniaDialogs.ShowAskPass(prompt) is not string answer)
        {
            return 1;
        }

        Console.Out.Write(answer + "\n");
        Console.Out.Flush();
        return 0;
    }

    /// <summary>As <c>FormFixHome.CheckHomePath</c>: the dialog to fix HOME if it has no git configuration.</summary>
    public static void CheckHomePath() => HomeDirectoryCheck.CheckHomePath();

    /// <summary>
    ///  As <c>ChecklistSettingsPage.CheckSettings</c> at startup: whether all the checks of the checklist pass (the settings
    ///  dialog is shown otherwise).
    /// </summary>
    public static bool CheckSettings(GitExtensions.Extensibility.Git.IGitUICommands commands, GitUI.CommandsDialogs.SettingsDialog.CommonLogic commonLogic)
        => AvaloniaDialogs.CheckSettings(commands, commonLogic);
}
