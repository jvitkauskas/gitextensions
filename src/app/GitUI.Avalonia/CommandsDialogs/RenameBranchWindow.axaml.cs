using Avalonia.Interactivity;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormRenameBranch</c>; behaviour lives in <see cref="RenameBranchViewModel"/>.</summary>
public partial class RenameBranchWindow : DialogWindow
{
    public RenameBranchWindow()
    {
        InitializeComponent();
        Opened += (_, _) =>
        {
            branchNameTextBox.Focus();
            branchNameTextBox.SelectAll();
        };
    }

    private void BranchNameTextBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not RenameBranchViewModel viewModel)
        {
            return;
        }

        int caretIndex = branchNameTextBox.CaretIndex;
        viewModel.NormaliseNewName();
        branchNameTextBox.CaretIndex = Math.Min(caretIndex, viewModel.NewName.Length);
    }
}
