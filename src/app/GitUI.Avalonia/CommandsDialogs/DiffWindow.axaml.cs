using Avalonia.Threading;
using GitExtUtils.GitUI.Theming;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormDiff</c>.</summary>
public partial class DiffWindow : DialogWindow
{
    public DiffWindow()
    {
        InitializeComponent();

        // As the labels of FormDiff: the BASE in the color of removed lines, the compared commit in the color of added lines.
        firstCommitBorder.Background = AppColorResources.GetBrush(this, AppColor.AnsiTerminalRedBackNormal);
        secondCommitBorder.Background = AppColorResources.GetBrush(this, AppColor.AnsiTerminalGreenBackNormal);

        // As Load += PopulateDiffFiles.
        Opened += (_, _) => Dispatcher.UIThread.Post(() => _ = (DataContext as DiffViewModel)?.InitializeAsync());
    }
}
