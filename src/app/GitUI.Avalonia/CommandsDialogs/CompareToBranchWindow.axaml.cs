using GitUI.Avalonia.Hosting;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormCompareToBranch</c>.</summary>
public partial class CompareToBranchWindow : DialogWindow
{
    public CompareToBranchWindow()
    {
        InitializeComponent();
        Opened += (_, _) => branchSelector.FocusInput();
    }
}
