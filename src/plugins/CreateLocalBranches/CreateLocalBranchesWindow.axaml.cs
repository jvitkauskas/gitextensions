using GitUI.Avalonia.Hosting;

namespace GitExtensions.Plugins.CreateLocalBranches;

/// <summary>Avalonia port of <c>CreateLocalBranchesForm</c>; behaviour lives in <c>CreateLocalBranchesViewModel</c>.</summary>
public partial class CreateLocalBranchesWindow : DialogWindow
{
    public CreateLocalBranchesWindow()
    {
        InitializeComponent();
        Opened += (_, _) => createButton.Focus();
    }
}
