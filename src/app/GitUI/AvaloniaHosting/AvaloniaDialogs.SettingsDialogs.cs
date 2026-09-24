using System.Runtime.CompilerServices;
using GitCommands;
using GitUI.Avalonia.CommandsDialogs.SettingsDialog;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs.SettingsDialog;
using GitUI.CommandsDialogs.SettingsDialog.Pages;
using GitUI.Presentation.CommandsDialogs.SettingsDialog;
using GitUI.Presentation.Translations;
using ResourceManager;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  Routing of the HOME directory dialog and the help text window of the settings (docs/avalonia-port/PLAN.md, phase 2, batch 8).
/// </summary>
internal static partial class AvaloniaDialogs
{
    /// <summary>The open help text window of each owner, which is reused as <c>ScriptsSettingsPage</c> does.</summary>
    private static readonly ConditionalWeakTable<Control, DialogWindow> _helpDisplayWindows = [];

    public static bool TryShowFixHome(IWin32Window? owner)
    {
        FixHomeEnvironment environment = new(
            CustomHomeDir: AppSettings.CustomHomeDir,
            UserProfileHomeDir: AppSettings.UserProfileHomeDir,
            DefaultHomeDir: EnvironmentConfiguration.GetDefaultHomeDir() ?? "",
            UserHome: TryGetEnvironment(() => Environment.GetEnvironmentVariable("HOME", EnvironmentVariableTarget.User)),
            HomeDrivePath: TryGetEnvironment(() => Environment.GetEnvironmentVariable("HOMEDRIVE") + Environment.GetEnvironmentVariable("HOMEPATH")),
            UserProfile: TryGetEnvironment(() => Environment.GetEnvironmentVariable("USERPROFILE")),
            PersonalFolder: TryGetEnvironment(() => Environment.GetFolderPath(Environment.SpecialFolder.Personal)));

        ShowDialog(
            () =>
            {
                FixHomeWindow window = new();
                window.DataContext = new FixHomeViewModel(
                    ViewStrings.Load<FixHomeStrings>(),
                    environment,
                    TranslatedStrings.Error,
                    new FixHomeHost(),
                    new MessageBoxService(window),
                    new AvaloniaFileDialogService(window));
                return window;
            },
            owner);
        return true;

        static string? TryGetEnvironment(Func<string?> get)
        {
            try
            {
                return get();
            }
            catch
            {
                // As FormFixHome: could be a security issue, the user chooses manually.
                return null;
            }
        }
    }

    /// <summary>
    ///  Shows the Avalonia port of <c>SimpleHelpDisplayDialog</c> modelessly over <paramref name="owner"/>, or activates
    ///  the one already open; it closes with the form of <paramref name="owner"/>.
    /// </summary>
    public static bool TryShowSimpleHelpDisplay(Control owner, string title, string content)
    {
        if (_helpDisplayWindows.TryGetValue(owner, out DialogWindow? openWindow) && openWindow.IsVisible)
        {
            openWindow.Activate();
            return true;
        }

        AvaloniaUi.EnsureInitialized(GetOptions);
        SimpleHelpDisplayWindow window = new() { DataContext = new SimpleHelpDisplayViewModel(title, content) };
        _helpDisplayWindows.AddOrUpdate(owner, window);

        if (owner.FindForm() is Form form)
        {
            void CloseWithForm(object? sender, FormClosedEventArgs e) => window.Close();
            form.FormClosed += CloseWithForm;
            window.Closed += (_, _) => form.FormClosed -= CloseWithForm;
        }

        AvaloniaDialogHost.Show(window, owner.Handle);
        return true;
    }

    private sealed class FixHomeHost : IFixHomeHost
    {
        public bool HasGlobalGitConfig(string? path) => FormFixHome.HasGlobalGitConfig(path);

        public string? ApplyHome(string customHomeDir, bool userProfileHomeDir)
        {
            // As FormFixHome.ok_Click.
            AppSettings.CustomHomeDir = customHomeDir;
            AppSettings.UserProfileHomeDir = userProfileHomeDir;
            EnvironmentConfiguration.SetEnvironmentVariables();
            return Environment.GetEnvironmentVariable("HOME");
        }

        public bool DirectoryExists(string? path) => Directory.Exists(path);
    }
}
