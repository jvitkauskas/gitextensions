using GitCommands;
using GitCommands.Git;
using GitCommands.Settings;
using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Settings;
using GitUI.Avalonia.CommandsDialogs.SettingsDialog;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.CommandsDialogs.SettingsDialog;
using GitUI.CommandsDialogs.SettingsDialog.Pages;
using GitUI.Presentation.CommandsDialogs.SettingsDialog;
using GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;
using GitUI.Presentation.Translations;
using SettingsPageViewModel = GitUI.Presentation.CommandsDialogs.SettingsDialog.SettingsPageViewModel;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  Routing of the settings dialog (docs/avalonia-port/PLAN.md, phase 6). The port is unfinished (not all the pages are
///  ported), so it is used only when named in <c>GE_AVALONIA</c> (e.g. <c>GE_AVALONIA=all,FormSettings</c>).
/// </summary>
internal static partial class AvaloniaDialogs
{
    /// <summary>The Avalonia port of <c>FormSettings.ShowSettingsDialog</c>.</summary>
    /// <param name="saved">Whether settings were saved (as <c>DialogResult.OK</c>).</param>
    public static bool TryShowSettings(IWin32Window? owner, IGitUICommands commands, SettingsPageReference? initialPage, out bool saved)
    {
        saved = false;
        if (!AvaloniaUi.IsExplicitlyEnabledFor(nameof(FormSettings)))
        {
            return false;
        }

        CommonLogic commonLogic = new(commands.Module);
        SettingsDialogViewModel viewModel = new(ViewStrings.Load<SettingsDialogStrings>(), new SettingsDialogHost(commonLogic, owner));
        SettingsPagesHost pagesHost = new(commands, commonLogic);
        AddSettingsPages(viewModel, commonLogic, pagesHost, commands.Module.IsValidGitWorkingDir(), commands.Module.WorkingDir);

        // As ShowSettingsDialog: the pages read and write AppSettings in the global settings of the dialog until saved.
        AppSettings.UsingContainer(commonLogic.DistributedSettingsSet.GlobalSettings, () =>
        {
            ShowDialog(
                () =>
                {
                    SettingsWindow window = new() { DataContext = viewModel };
                    pagesHost.Window = window;
                    window.Opened += (_, _) => viewModel.Open(initialPage is SettingsPageReferenceByType byType ? byType.SettingsPageType.Name : null);
                    return window;
                },
                owner,
                positionName: nameof(FormSettings));
        });

        // The pages holding controls (the settings controls of the build server plugins) release them.
        foreach (IDisposable page in viewModel.Pages.OfType<IDisposable>())
        {
            page.Dispose();
        }

        saved = viewModel.IsSaved;
        return true;
    }

    /// <summary>
    ///  As <c>OnRuntimeLoad</c> of <c>FormSettings</c>: the pages in the tree, with the levels of their base class
    ///  (<c>SettingsPageWithHeader</c>: global; <c>DistributedSettingsPage</c> and <c>GitConfigBaseSettingsPage</c>: the
    ///  levels of the repository too, in a repository). The pages not ported yet are left out.
    /// </summary>
    private static void AddSettingsPages(SettingsDialogViewModel viewModel, CommonLogic commonLogic, SettingsPagesHost pagesHost, bool canSaveInsideRepo, string workingDir)
    {
        DistributedSettingsSet distributed = commonLogic.DistributedSettingsSet;
        GitConfigSettingsSet gitConfig = commonLogic.GitConfigSettingsSet;
        Dictionary<SettingsLevel, SettingsSource> none = [];
        Dictionary<SettingsLevel, SettingsSource> global = new() { [SettingsLevel.Global] = distributed.GlobalSettings };
        Dictionary<SettingsLevel, SettingsSource> distributedLevels = canSaveInsideRepo
            ? new()
            {
                [SettingsLevel.Effective] = distributed.EffectiveSettings,
                [SettingsLevel.Local] = distributed.LocalSettings,
                [SettingsLevel.Distributed] = distributed.DistributedSettings,
                [SettingsLevel.Global] = distributed.GlobalSettings,
            }
            : global;
        Dictionary<SettingsLevel, SettingsSource> gitConfigLevels = canSaveInsideRepo
            ? new()
            {
                [SettingsLevel.Effective] = gitConfig.EffectiveSettings,
                [SettingsLevel.Local] = gitConfig.LocalSettings,
                [SettingsLevel.Global] = gitConfig.GlobalSettings,
                [SettingsLevel.System] = gitConfig.SystemSettings,
            }
            : new() { [SettingsLevel.Global] = gitConfig.GlobalSettings };

        SettingsDialogStrings strings = viewModel.Strings;

        // Git Extensions settings
        const string gitExtensions = nameof(GitExtensionsSettingsGroup);
        viewModel.AddPage(new GroupSettingsPageViewModel(strings.GitExtensionsGroup.Text, gitExtensions), null, "GitExtensionsLogo16", none);

        Add(new RevisionLinksSettingsPageViewModel(ViewStrings.Load<RevisionLinksSettingsPageStrings>(), pagesHost), gitExtensions, "Link", distributedLevels);
        Add(new BuildServerIntegrationSettingsPageViewModel(ViewStrings.Load<BuildServerIntegrationSettingsPageStrings>(), pagesHost), gitExtensions, "Integration", distributedLevels);

        if (OperatingSystem.IsWindows())
        {
            Add(new ShellExtensionSettingsPageViewModel(ViewStrings.Load<ShellExtensionSettingsPageStrings>(), pagesHost), gitExtensions, "ShellExtensions", global);
        }

        // >> Detailed
        Add(new DetailedSettingsPageViewModel(ViewStrings.Load<DetailedSettingsPageStrings>()), gitExtensions, "Settings", distributedLevels);
        const string detailed = nameof(DetailedSettingsPage);
        Add(new CommitDialogSettingsPageViewModel(ViewStrings.Load<CommitDialogSettingsPageStrings>()), detailed, "CommitSummary", global);
        Add(new BlameViewerSettingsPageViewModel(ViewStrings.Load<BlameViewerSettingsPageStrings>()), detailed, "Blame", global);

        Add(new SshSettingsPageViewModel(ViewStrings.Load<SshSettingsPageStrings>(), pagesHost, pagesHost), gitExtensions, "Key", global);

        // Git settings
        const string git = nameof(GitSettingsGroup);
        viewModel.AddPage(new GroupSettingsPageViewModel(strings.GitGroup.Text, git), null, "GitLogo16", none);
        Add(new GitSettingsPageViewModel(ViewStrings.Load<GitSettingsPageStrings>(), pagesHost, pagesHost), git, "FolderOpen", global);
        Add(new GitConfigSettingsPageViewModel(ViewStrings.Load<GitConfigSettingsPageStrings>(), pagesHost, pagesHost, workingDir), git, "GeneralSettings", gitConfigLevels);
        Add(new GitConfigAdvancedSettingsPageViewModel(ViewStrings.Load<GitConfigAdvancedSettingsPageStrings>(), pagesHost, GitVersion.Current.SupportUpdateRefs), git, "AdvancedSettings", gitConfigLevels);
        Add(IntroductionSettingsPageViewModel.CreateGitRoot(), git, null, none, asRoot: true);

        // Plugins settings
        const string plugins = nameof(PluginsSettingsGroup);
        viewModel.AddPage(new GroupSettingsPageViewModel(strings.PluginsGroup.Text, plugins), null, "Plugin", none);
        Add(IntroductionSettingsPageViewModel.CreatePluginRoot(), plugins, null, none, asRoot: true);

        void Add(SettingsPageViewModel page, string parent, string? icon, Dictionary<SettingsLevel, SettingsSource> sources, bool asRoot = false)
            => viewModel.AddPage(page, parent, icon, sources, asRoot);
    }

    /// <summary>The end of <c>FormSettings.Save</c> and its error message.</summary>
    private sealed class SettingsDialogHost(CommonLogic commonLogic, IWin32Window? owner) : ISettingsDialogHost
    {
        public string? SaveSettingsSets() => AvaloniaUi.RunInHostContext(() =>
        {
            try
            {
                commonLogic.GitConfigSettingsSet.Save();
                commonLogic.DistributedSettingsSet.Save();
                if (OperatingSystem.IsWindows())
                {
                    FormFixHome.CheckHomePath();
                }

                // Saves some specific settings only, despite its name.
                AppSettings.SaveSettings();
                return null;
            }
            catch (SaveSettingsException ex) when (ex.InnerException is not null)
            {
                return ex.InnerException.Message;
            }
        });

        public void ShowError(string heading, string text) => AvaloniaUi.RunInHostContext(() =>
        {
            TaskDialogPage page = new()
            {
                Text = text,
                Heading = heading,
                Caption = TranslatedStrings.Error,
                Buttons = { TaskDialogButton.OK },
                Icon = TaskDialogIcon.Error,
                AllowCancel = true,
                SizeToContent = true
            };
            TaskDialog.ShowDialog(owner?.Handle ?? 0, page);
        });
    }
}
