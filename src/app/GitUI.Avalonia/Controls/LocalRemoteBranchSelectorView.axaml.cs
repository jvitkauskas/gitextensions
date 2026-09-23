using Avalonia.Controls;

namespace GitUI.Avalonia.Controls;

/// <summary>Avalonia port of <c>GitUI.UserControls.BranchSelector</c>.</summary>
public partial class LocalRemoteBranchSelectorView : UserControl
{
    public LocalRemoteBranchSelectorView()
    {
        InitializeComponent();
    }

    /// <summary>Focuses the branch input.</summary>
    public void FocusInput() => branchesComboBox.Focus();
}
