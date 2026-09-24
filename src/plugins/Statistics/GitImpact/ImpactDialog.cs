using GitExtensions.Extensibility.Git;
using GitUI.AvaloniaHosting;
using GitUI.Presentation.Translations;

namespace GitExtensions.Plugins.GitImpact;

/// <summary>Shows the Avalonia port of <see cref="FormImpact"/> (docs/avalonia-port/PLAN.md, phase 7).</summary>
internal static class ImpactDialog
{
    /// <summary>Returns <see langword="false"/> when the port is disabled, in which case the caller shows the WinForms form.</summary>
    public static bool TryShow(GitUIEventArgs args)
    {
        AvaloniaPluginDialogs.ShowDialog(
            () => new ImpactWindow
            {
                // As ImpactControl.Init: respect the .mailmap file.
                DataContext = new ImpactViewModel(
                    ViewStrings.Load<ImpactStrings>(),
                    new ImpactLoader(args.GitModule) { RespectMailmap = true },
                    AvaloniaPluginDialogs.BackgroundRunner),
            },
            args.Owner);
        return true;
    }
}
