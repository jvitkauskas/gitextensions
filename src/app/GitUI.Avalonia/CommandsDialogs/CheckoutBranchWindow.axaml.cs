using Avalonia.Interactivity;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormCheckoutBranch</c>.</summary>
public partial class CheckoutBranchWindow : DialogWindow
{
    public CheckoutBranchWindow()
    {
        InitializeComponent();
        Opened += (_, _) => branchesComboBox.Focus();
    }

    private void CustomBranchNameTextBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not CheckoutBranchViewModel viewModel)
        {
            return;
        }

        int caretIndex = customBranchNameTextBox.CaretIndex;
        viewModel.NormaliseCustomBranchName();
        customBranchNameTextBox.CaretIndex = Math.Min(caretIndex, viewModel.CustomBranchName.Length);
    }
}
