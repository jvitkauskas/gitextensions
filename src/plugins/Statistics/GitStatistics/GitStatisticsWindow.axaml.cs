using Avalonia.Threading;
using GitUI.Avalonia.Hosting;

namespace GitExtensions.Plugins.GitStatistics;

/// <summary>Avalonia port of <see cref="FormGitStatistics"/>; behaviour lives in <see cref="GitStatisticsViewModel"/>.</summary>
public partial class GitStatisticsWindow : DialogWindow
{
    public GitStatisticsWindow()
    {
        InitializeComponent();

        // As FormGitStatisticsShown: the counting starts once the window is shown.
        Opened += (_, _) => Dispatcher.UIThread.Post(() => _ = (DataContext as GitStatisticsViewModel)?.LoadAsync());
    }
}
