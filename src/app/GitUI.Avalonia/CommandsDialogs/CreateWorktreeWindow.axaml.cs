using Avalonia.Interactivity;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormCreateWorktree</c>.</summary>
public partial class CreateWorktreeWindow : DialogWindow
{
    public CreateWorktreeWindow()
    {
        InitializeComponent();
    }

    private void NewBranchNameTextBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not CreateWorktreeViewModel viewModel)
        {
            return;
        }

        int caretIndex = newBranchNameTextBox.CaretIndex;
        viewModel.NormaliseNewBranchName();
        newBranchNameTextBox.CaretIndex = Math.Min(caretIndex, viewModel.NewBranchName.Length);
    }
}
