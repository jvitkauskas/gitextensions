using GitUI.Avalonia.Hosting;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormMergeBranch</c>.</summary>
public partial class MergeBranchWindow : DialogWindow
{
    public MergeBranchWindow()
    {
        InitializeComponent();
        Opened += (_, _) => branchSelector.FocusInput();
    }
}
