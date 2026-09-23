using Avalonia.Interactivity;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormCreateBranch</c>.</summary>
public partial class CreateBranchWindow : DialogWindow
{
    public CreateBranchWindow()
    {
        InitializeComponent();
        Opened += (_, _) => branchNameTextBox.Focus();
    }

    private void BranchNameTextBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not CreateBranchViewModel viewModel)
        {
            return;
        }

        int caretIndex = branchNameTextBox.CaretIndex;
        viewModel.NormaliseBranchName();
        branchNameTextBox.CaretIndex = Math.Min(caretIndex, viewModel.BranchName.Length);
    }
}
