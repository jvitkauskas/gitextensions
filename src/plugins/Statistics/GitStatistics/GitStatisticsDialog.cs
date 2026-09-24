using GitCommands;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.AvaloniaHosting;
using GitUI.Presentation.Translations;

namespace GitExtensions.Plugins.GitStatistics;

/// <summary>Shows the Avalonia port of <c>FormGitStatistics</c> (docs/avalonia-port/PLAN.md, phase 7).</summary>
internal static class GitStatisticsDialog
{
    /// <summary>Returns <see langword="false"/> when the port is disabled, in which case the caller shows the WinForms form.</summary>
    public static bool TryShow(GitUIEventArgs args, string codeFilePattern, bool countSubmodules, string directoriesToIgnore)
    {
        IGitExecutorProvider executorProvider = args.GitUICommands.GetRequiredService<IGitExecutorProvider>();
        AvaloniaPluginDialogs.ShowDialog(
            () => new GitStatisticsWindow
            {
                DataContext = new GitStatisticsViewModel(
                    ViewStrings.Load<GitStatisticsStrings>(),
                    args.GitModule,
                    path => new GitModule(executorProvider, path),
                    codeFilePattern,
                    countSubmodules,
                    directoriesToIgnore,
                    AvaloniaPluginDialogs.BackgroundRunner),
            },
            args.Owner);
        return true;
    }
}
