using Avalonia.Threading;
using GitUI.Avalonia.Hosting;

namespace GitExtensions.Plugins.GitStatistics;

/// <summary>Avalonia port of <c>FormGitStatistics</c>; behaviour lives in <c>GitStatisticsViewModel</c>.</summary>
public partial class GitStatisticsWindow : DialogWindow
{
    public GitStatisticsWindow()
    {
        InitializeComponent();

        // As FormGitStatisticsShown: the counting starts once the window is shown.
        Opened += (_, _) => Dispatcher.UIThread.Post(() => _ = (DataContext as GitStatisticsViewModel)?.LoadAsync());
    }
}
