using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Plugins;
using GitUI.Avalonia.Hosting;
using GitUI.AvaloniaHosting;
using GitUI.NBugReports;
using GitUI.Presentation.Translations;

namespace GitExtensions.Plugins.DeleteUnusedBranches;

/// <summary>Shows the Avalonia port of <see cref="DeleteUnusedBranchesForm"/> (docs/avalonia-port/PLAN.md, phase 7).</summary>
internal static class DeleteUnusedBranchesDialog
{
    /// <summary>
    ///  Returns <see langword="false"/> when the port is disabled, in which case the caller shows the WinForms form;
    ///  <paramref name="hasDeletedBranch"/> is the return value of the plugin.
    /// </summary>
    public static bool TryShow(GitUIEventArgs args, DeleteUnusedBranchesFormSettings settings, IGitPlugin plugin, out bool hasDeletedBranch)
    {
        hasDeletedBranch = false;
        if (!AvaloniaPluginDialogs.IsEnabledFor(nameof(DeleteUnusedBranchesForm)))
        {
            return false;
        }

        DeleteUnusedBranchesViewModel? viewModel = null;
        AvaloniaPluginDialogs.ShowDialog(
            () =>
            {
                DeleteUnusedBranchesWindow window = new();
                viewModel = new DeleteUnusedBranchesViewModel(
                    ViewStrings.Load<DeleteUnusedBranchesStrings>(),
                    settings,
                    args.GitModule,
                    AvaloniaPluginDialogs.BackgroundRunner,
                    AvaloniaPluginDialogs.CreateMessageBoxService(window),
                    new Host(args.GitUICommands));
                window.DataContext = viewModel;
                return window;
            },
            args.Owner);

        hasDeletedBranch = viewModel?.HasDeletedBranch == true;
        if (viewModel?.SettingsRequested == true)
        {
            // As buttonSettings_Click: the settings open once the dialog is closed.
            args.GitUICommands.StartSettingsDialog(plugin);
        }

        return true;
    }

    private sealed class Host(IGitUICommands commands) : IDeleteUnusedBranchesHost
    {
        public void NotifyRepoChanged() => AvaloniaUi.RunInHostContext(commands.RepoChangedNotifier.Notify);

        public void ReportError(Exception exception) => AvaloniaUi.RunInHostContext(() => BugReportInvoker.Report(exception, isTerminating: false));
    }
}
