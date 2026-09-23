using Avalonia.Threading;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormStash</c>.</summary>
public partial class StashWindow : DialogWindow
{
    public StashWindow()
    {
        InitializeComponent();

        // As FormStashShown: the stashes are loaded once the window is shown.
        Opened += (_, _) => Dispatcher.UIThread.Post(() => (DataContext as StashViewModel)?.InitializeView());
    }
}
