using GitCommands;
using GitCommands.Git;
using GitCommands.Remotes;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Extensions;
using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Settings;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs.SettingsDialog;
using GitUI.CommandsDialogs.SettingsDialog.Pages;
using GitUI.CommandsDialogs.SettingsDialog.ShellExtension;
using GitUI.NBugReports;
using GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;
using GitUIPluginInterfaces;
using GitUIPluginInterfaces.BuildServerIntegration;
using Microsoft.Win32;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  What the ported settings pages of the Git settings, SSH, build server integration, revision links and shell extension need
///  from the application (docs/avalonia-port/PLAN.md, phase 6): <c>CheckSettingsLogic</c>, <c>CommonLogic</c>, the plugins and
///  the dialogs they open.
/// </summary>
internal static partial class AvaloniaDialogs
{
    private sealed class GitSettingsPagesHost(IGitUICommands commands, CommonLogic commonLogic)
        : IGitSettingsPageHost,
        IGitConfigSettingsPageHost,
        ISshSettingsPageHost,
        IBuildServerIntegrationSettingsPageHost,
        IRevisionLinksSettingsPageHost,
        IShellExtensionSettingsPageHost,
        IFileDialogService
    {
        /// <summary>The settings window, set when it is created: the owner of the dialogs and the file pickers.</summary>
        public DialogWindow? Window { get; set; }

        /// <summary>Stored in the registry, as <see cref="SshPath"/>, <see cref="CascadeShellMenuItems"/> and <see cref="AlwaysShowAllCommands"/>.</summary>
        public string GitCommandValue
        {
            get => AppSettings.GitCommandValue;
            set => AppSettings.GitCommandValue = value;
        }

        public string LinuxToolsDir
        {
            get => AppSettings.LinuxToolsDir;
            set => AppSettings.LinuxToolsDir = value;
        }

        public string SshPath
        {
            get => AppSettings.SshPath;
            set => AppSettings.SshPath = value;
        }

        public string CascadeShellMenuItems
        {
            get => AppSettings.CascadeShellMenuItems;
            set => AppSettings.CascadeShellMenuItems = value;
        }

        public bool AlwaysShowAllCommands
        {
            get => AppSettings.AlwaysShowAllCommands;
            set => AppSettings.AlwaysShowAllCommands = value;
        }

        private NativeWindowOwner? Owner => Window is null ? null : new NativeWindowOwner(Window);

        private IGitModule Module => commands.Module;

        // GitSettingsPage

        public bool SolveGitCommand(string? possibleNewPath) => CheckSettingsLogic.SolveGitCommand(possibleNewPath);

        public bool SolveLinuxToolsDir(string? possibleNewPath) => CheckSettingsLogic.SolveLinuxToolsDir(possibleNewPath);

        public (string? GitConfigGlobal, string HomeDir) GetGitEnvironment()
        {
            EnvironmentConfiguration.SetEnvironmentVariables();
            return (EnvironmentConfiguration.GetEnvironmentVariable("GIT_CONFIG_GLOBAL"), EnvironmentConfiguration.GetHomeDir());
        }

        public void ShowFixHome() => AvaloniaUi.RunInHostContext(() =>
        {
            TryShowFixHome(Owner);
        });

        public void OpenUrl(string url) => OsShellUtil.OpenUrlInDefaultBrowser(url);

        // GitConfigSettingsPage and GitConfigAdvancedSettingsPage

        public void SaveGitConfigSettings() => commonLogic.GitConfigSettingsSet.Save();

        public bool CanFindGitCmd() => new CheckSettingsLogic(commonLogic).CanFindGitCmd();

        public IReadOnlyList<string> GetEditors() => EditorHelper.GetEditors();

        public bool ShowAvailableEncodings() => AvaloniaUi.RunInHostContext(() =>
        {
            return TryShowAvailableEncodings(Owner, out bool accepted) && accepted;
        });

        // SshSettingsPage

        /// <summary>As <c>SshSettingsPage.GetPuttyLocations</c>.</summary>
        public IEnumerable<string> GetPuttyLocations()
        {
            string? envVariable = Environment.GetEnvironmentVariable("GITEXT_PUTTY");
            if (!string.IsNullOrEmpty(envVariable))
            {
                yield return envVariable;
            }

            string? programFiles = Environment.GetEnvironmentVariable("ProgramFiles");
            string? programFilesX86 = (IntPtr.Size == 8
                || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432")))
                ? Environment.GetEnvironmentVariable("ProgramFiles(x86)")
                : null;

            yield return programFiles + @"\PuTTY\";
            if (programFilesX86 is not null)
            {
                yield return programFilesX86 + @"\PuTTY\";
            }

            yield return programFiles + @"\TortoiseGit\bin\";
            if (programFilesX86 is not null)
            {
                yield return programFilesX86 + @"\TortoiseGit\bin\";
            }

            yield return programFiles + @"\TortoiseSvn\bin\";
            if (programFilesX86 is not null)
            {
                yield return programFilesX86 + @"\TortoiseSvn\bin\";
            }

            // Old(?) uninstaller
            yield return CommonLogic.GetRegistryValue(Registry.LocalMachine, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\PuTTY_is1", "InstallLocation");
        }

        public void SetGitSshEnvironmentVariable(string path) => GitSshHelpers.SetGitSshEnvironmentVariable(path);

        // BuildServerIntegrationSettingsPage

        /// <summary>As <c>BuildServerIntegrationSettingsPage.Init</c>: the build server plugins, found in the background.</summary>
        public Task<IReadOnlyList<string>> GetBuildServerTypesAsync() => Task.Run<IReadOnlyList<string>>(() =>
        {
            IEnumerable<Lazy<IBuildServerAdapter, IBuildServerTypeMetadata>> exports = ManagedExtensibility.GetExports<IBuildServerAdapter, IBuildServerTypeMetadata>();
            return [.. exports.Select(export => export.Metadata.BuildServerType.Combine(" - ", export.Metadata.CanBeLoaded)!)];
        });

        /// <summary>
        ///  As <c>CreateBuildServerSettingsUserControl</c>: none, as the WinForms settings controls of the plugins of API v1
        ///  are gone with WinForms (docs/avalonia-port/PLAN.md, phase 8); the plugins declare their settings (API v2).
        /// </summary>
        public IBuildServerSettingsControl? CreateSettingsControl(string buildServerType) => null;

        /// <summary>
        ///  Plugin API v2: the settings the plugin declares (<see cref="IBuildServerSettingsProvider"/>), as the settings of the
        ///  plugins, with the values it suggests for the repository.
        /// </summary>
        public BuildServerPluginSettings? CreatePluginSettings(string buildServerType)
        {
            if (string.IsNullOrEmpty(Module.WorkingDir) || GitUI.BuildServerIntegration.BuildServerSettingsProviders.FindProvider(buildServerType) is not { } provider)
            {
                return null;
            }

            string defaultProjectName = Module.WorkingDir.Split(Delimiters.PathSeparators, StringSplitOptions.RemoveEmptyEntries)[^1];
            BuildServerSettingsContext context = new(defaultProjectName, GetRemoteUrls());
            PluginSettingsPageViewModel page = new(
                ViewStrings.Load<PluginSettingsPageStrings>(),
                ViewStrings.Load<SettingValueStrings>(),
                buildServerType,
                pageName: "BuildServerIntegrationSettingsPage",
                pluginSettings: settings => settings);
            foreach (ISetting setting in provider.GetSettings(context))
            {
                page.AddRow(CreatePluginSettingRow(setting, () => Window is { } window ? AvaloniaPluginDialogs.GetWindowOwner(window) : WindowOwner.None));
            }

            return new BuildServerPluginSettings(
                page,
                context.WithSuggestedValues,
                provider.Validate,
                error => AvaloniaUi.RunInHostContext(() => MessageBoxes.ShowError(Owner, error)));
        }

        private IEnumerable<string> GetRemoteUrls()
        {
            ConfigFileRemoteSettingsManager remotesManager = new(() => Module);
            return remotesManager.LoadRemotes(false).Select(r => string.IsNullOrEmpty(r.PushUrl) ? r.Url! : r.PushUrl!);
        }

        // RevisionLinksSettingsPage

        public IReadOnlyList<Remote> GetRemotes()
            => AvaloniaUi.RunInHostContext(() => ThreadHelper.JoinableTaskFactory.Run(Module.GetRemotesAsync));

        // ShellExtensionSettingsPage

        public bool FilesExist() => ShellExtensionManager.FilesExist();

        public bool IsRegistered() => ShellExtensionManager.IsRegistered();

        public void Register() => RunReportingErrors(ShellExtensionManager.Register);

        public void Unregister() => RunReportingErrors(ShellExtensionManager.Unregister);

        public string GetManualUrl() => UserManual.UserManual.UrlFor("settings", "shell-extension");

        // As the errors of the click handlers of the WinForms page, reported by Application.ThreadException.
        private static void RunReportingErrors(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                AvaloniaUi.RunInHostContext(() => BugReportInvoker.Report(ex, isTerminating: false));
            }
        }

        // The file pickers of the pages, over the settings window.

        public Task<IReadOnlyList<string>> PickFilesAsync(bool allowMultiple, string? startDirectory = null)
            => Window is { } window ? new AvaloniaFileDialogService(window).PickFilesAsync(allowMultiple, startDirectory) : Task.FromResult<IReadOnlyList<string>>([]);

        public Task<string?> PickFileAsync(string title, string filterName, string pattern, string? startDirectory = null)
            => Window is { } window ? new AvaloniaFileDialogService(window).PickFileAsync(title, filterName, pattern, startDirectory) : Task.FromResult<string?>(null);

        public Task<string?> PickFileAsync(string title, IReadOnlyList<(string Name, string Pattern)> fileTypes, string? startDirectory = null)
            => Window is { } window ? new AvaloniaFileDialogService(window).PickFileAsync(title, fileTypes, startDirectory) : Task.FromResult<string?>(null);

        public Task<string?> PickFolderAsync(string? startDirectory = null)
            => Window is { } window ? new AvaloniaFileDialogService(window).PickFolderAsync(startDirectory) : Task.FromResult<string?>(null);

        public Task<string?> PickSaveFileAsync(string title, string filterName, string extension, string? suggestedFileName = null, string? startDirectory = null)
            => Window is { } window
                ? new AvaloniaFileDialogService(window).PickSaveFileAsync(title, filterName, extension, suggestedFileName, startDirectory)
                : Task.FromResult<string?>(null);
    }
}
