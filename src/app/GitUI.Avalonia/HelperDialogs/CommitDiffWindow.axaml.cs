using Avalonia.Threading;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.HelperDialogs;

namespace GitUI.Avalonia.HelperDialogs;

/// <summary>Avalonia port of <c>FormCommitDiff</c> (with the <c>CommitDiff</c> control).</summary>
public partial class CommitDiffWindow : DialogWindow
{
    public CommitDiffWindow()
    {
        InitializeComponent();

        // The commit and its files are loaded once the window is shown.
        Opened += (_, _) => Dispatcher.UIThread.Post(() => _ = (DataContext as CommitDiffViewModel)?.InitializeAsync());
    }
}
