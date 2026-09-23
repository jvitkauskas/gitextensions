using Avalonia.Controls;
using Avalonia.Input;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormGoToCommit</c>.</summary>
public partial class GoToCommitWindow : DialogWindow
{
    public GoToCommitWindow()
    {
        InitializeComponent();
        Opened += (_, _) =>
        {
            commitExpressionTextBox.Focus();
            commitExpressionTextBox.SelectAll();
        };

        commitExpressionTextBox.GotFocus += (_, _) => SetSource(GoToCommitSource.Expression);
        tagsComboBox.GotFocus += (_, _) => SetSource(GoToCommitSource.Tag);
        branchesComboBox.GotFocus += (_, _) => SetSource(GoToCommitSource.Branch);

        // As the WinForms combo boxes: picking an item from the list goes to it.
        tagsComboBox.SelectionChanged += (_, e) => GoToPicked(tagsComboBox, e, GoToCommitSource.Tag);
        branchesComboBox.SelectionChanged += (_, e) => GoToPicked(branchesComboBox, e, GoToCommitSource.Branch);
    }

    private GoToCommitViewModel? ViewModel => DataContext as GoToCommitViewModel;

    private void SetSource(GoToCommitSource source)
    {
        if (ViewModel is { } viewModel)
        {
            viewModel.Source = source;
        }
    }

    private void GoToPicked(ComboBox comboBox, SelectionChangedEventArgs e, GoToCommitSource source)
    {
        if (comboBox.IsDropDownOpen && e.AddedItems.Count == 1 && e.AddedItems[0] is GitRefItem item)
        {
            ViewModel?.GoTo(source, item);
        }
    }
}
