using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.Hosting;
using GitUI.AvaloniaHosting;
using GitUI.Presentation.Translations;

namespace GitExtensions.Plugins.FindLargeFiles;

/// <summary>Shows the Avalonia port of <c>FindLargeFilesForm</c> (docs/avalonia-port/PLAN.md, phase 7).</summary>
internal static class FindLargeFilesDialog
{
    /// <summary>Returns <see langword="false"/> when the port is disabled, in which case the caller shows the WinForms form.</summary>
    public static bool TryShow(GitUIEventArgs args, float threshold)
    {
        IGitUICommands commands = args.GitUICommands;
        AvaloniaPluginDialogs.ShowDialog(
            () =>
            {
                FindLargeFilesWindow window = new();
                window.DataContext = new FindLargeFilesViewModel(
                    ViewStrings.Load<FindLargeFilesStrings>(),
                    threshold,
                    commands.Module,
                    AppSettings.GitCommand,
                    AvaloniaPluginDialogs.BackgroundRunner,
                    AvaloniaPluginDialogs.CreateMessageBoxService(window),
                    command => AvaloniaUi.RunInHostContext(() => commands.StartBatchFileProcessDialog(command)));
                return window;
            },
            args.Owner);
        return true;
    }
}
