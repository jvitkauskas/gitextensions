using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.Avalonia.HelperDialogs;

/// <summary>Avalonia port of <c>FormResetAnotherBranch</c>.</summary>
public partial class ResetAnotherBranchWindow : DialogWindow
{
    public ResetAnotherBranchWindow()
    {
        InitializeComponent();
        Opened += (_, _) =>
        {
            branchesComboBox.Focus();

            // As FormResetAnotherBranch: without a suggested branch, the list is opened.
            if (DataContext is ResetAnotherBranchViewModel { Branch.Length: 0, Branches.Count: > 0 })
            {
                branchesComboBox.IsDropDownOpen = true;
            }
        };
    }
}
