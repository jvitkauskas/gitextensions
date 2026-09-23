using GitUI.Avalonia.Hosting;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormDeleteRemoteBranch</c>.</summary>
public partial class DeleteRemoteBranchWindow : DialogWindow
{
    public DeleteRemoteBranchWindow()
    {
        InitializeComponent();
        Opened += (_, _) => branchSelector.FocusInput();
    }
}
