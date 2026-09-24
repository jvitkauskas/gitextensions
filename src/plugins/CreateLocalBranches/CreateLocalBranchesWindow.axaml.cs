using GitUI.Avalonia.Hosting;

namespace GitExtensions.Plugins.CreateLocalBranches;

/// <summary>Avalonia port of <see cref="CreateLocalBranchesForm"/>; behaviour lives in <see cref="CreateLocalBranchesViewModel"/>.</summary>
public partial class CreateLocalBranchesWindow : DialogWindow
{
    public CreateLocalBranchesWindow()
    {
        InitializeComponent();
        Opened += (_, _) => createButton.Focus();
    }
}
