using Avalonia.Threading;
using GitUI.Avalonia.Controls.Blame;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormBlame</c>.</summary>
public partial class BlameWindow : DialogWindow
{
    public BlameWindow()
    {
        InitializeComponent();

        // The blame is loaded once the window is shown.
        Opened += (_, _) => Dispatcher.UIThread.Post(() => _ = (DataContext as BlameDialogViewModel)?.InitializeAsync());
    }

    public BlameView Blame => blame;
}
