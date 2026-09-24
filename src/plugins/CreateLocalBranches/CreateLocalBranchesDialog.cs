using GitExtensions.Extensibility.Git;
using GitUI.AvaloniaHosting;
using GitUI.Presentation.Translations;

namespace GitExtensions.Plugins.CreateLocalBranches;

/// <summary>Shows the Avalonia port of <see cref="CreateLocalBranchesForm"/> (docs/avalonia-port/PLAN.md, phase 7).</summary>
internal static class CreateLocalBranchesDialog
{
    /// <summary>Returns <see langword="false"/> when the port is disabled, in which case the caller shows the WinForms form.</summary>
    public static bool TryShow(GitUIEventArgs args)
    {
        if (!AvaloniaPluginDialogs.IsEnabledFor(nameof(CreateLocalBranchesForm)))
        {
            return false;
        }

        AvaloniaPluginDialogs.ShowDialog(
            () =>
            {
                CreateLocalBranchesWindow window = new();
                window.DataContext = new CreateLocalBranchesViewModel(
                    ViewStrings.Load<CreateLocalBranchesStrings>(),
                    args.GitModule.GitExecutable,
                    AvaloniaPluginDialogs.CreateMessageBoxService(window));
                return window;
            },
            args.OwnerForm);
        return true;
    }
}
