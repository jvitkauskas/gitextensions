using Avalonia.Controls;

namespace GitUI.Avalonia.Controls;

/// <summary>Avalonia port of <c>BranchComboBox</c>; behaviour lives in <c>BranchSelectorViewModel</c>.</summary>
public partial class BranchSelector : UserControl
{
    public BranchSelector()
    {
        InitializeComponent();
    }

    /// <summary>Focuses the branch input.</summary>
    public void FocusInput() => branchesComboBox.Focus();
}
