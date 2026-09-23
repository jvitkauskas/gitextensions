using GitUI.Avalonia.Hosting;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormDeleteBranch</c>.</summary>
public partial class DeleteBranchWindow : DialogWindow
{
    public DeleteBranchWindow()
    {
        InitializeComponent();
        Opened += (_, _) => branchSelector.FocusInput();
    }
}
