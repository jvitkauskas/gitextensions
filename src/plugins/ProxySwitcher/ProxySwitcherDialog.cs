using GitCommands;
using GitCommands.Settings;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Settings;
using GitUI.AvaloniaHosting;
using GitUI.Presentation.Translations;

namespace GitExtensions.Plugins.ProxySwitcher;

/// <summary>Shows the Avalonia port of <see cref="ProxySwitcherForm"/> (docs/avalonia-port/PLAN.md, phase 7).</summary>
internal static class ProxySwitcherDialog
{
    /// <summary>Returns <see langword="false"/> when the port is disabled, in which case the caller shows the WinForms form.</summary>
    public static bool TryShow(GitUIEventArgs args, ProxySwitcherPlugin plugin, SettingsSource settings)
    {
        if (!AvaloniaPluginDialogs.IsEnabledFor(nameof(ProxySwitcherForm)))
        {
            return false;
        }

        ProxySwitcherStrings strings = ViewStrings.Load<ProxySwitcherStrings>();
        ProxySettings proxySettings = new(
            plugin.Username.ValueOrDefault(settings),
            plugin.Password.ValueOrDefault(settings),
            plugin.HttpProxy.ValueOrDefault(settings),
            plugin.HttpProxyPort.ValueOrDefault(settings));

        // As ProxySwitcherForm_Load, which closed the form before it was shown.
        if (!ProxySwitcherViewModel.IsConfigured(proxySettings))
        {
            MessageBoxes.ShowError(args.OwnerForm, strings.PleaseSetProxy.Text, strings.Title.Text);
            return true;
        }

        AvaloniaPluginDialogs.ShowDialog(
            () => new ProxySwitcherWindow { DataContext = new ProxySwitcherViewModel(strings, proxySettings, new Git(args.GitModule)) },
            args.OwnerForm);
        return true;
    }

    private sealed class Git(IGitModule module) : IProxySwitcherGit
    {
        public string GetEffectiveProxy() => module.GetEffectiveSetting("http.proxy");

        public string GetGlobalProxy() => new GitConfigSettings(module.GitExecutable, GitSettingLevel.Global).GetValue("http.proxy") ?? "";

        public void Run(ArgumentString arguments) => module.GitExecutable.GetOutput(arguments);
    }
}
