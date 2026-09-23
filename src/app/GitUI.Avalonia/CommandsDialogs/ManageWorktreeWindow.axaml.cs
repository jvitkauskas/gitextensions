using Avalonia.Controls;
using Avalonia.Input;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormManageWorktree</c>.</summary>
public partial class ManageWorktreeWindow : DialogWindow
{
    public ManageWorktreeWindow()
    {
        InitializeComponent();

        worktreesGrid.LoadingRow += (_, e) => e.Row.Classes.Set("deleted", e.Row.DataContext is GitWorktree { IsDeleted: true });

        // As FormManageWorktree: double click or Enter opens the selected worktree.
        worktreesGrid.DoubleTapped += (_, _) => OpenSelected();
        worktreesGrid.AddHandler(KeyDownEvent, OnGridKeyDown, handledEventsToo: true);
        Opened += (_, _) => worktreesGrid.Focus();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // Column headers are not in the logical tree, so they are not bound.
        if (DataContext is ManageWorktreeViewModel { Strings: var strings })
        {
            worktreesGrid.Columns[0].Header = strings.Path.Text;
            worktreesGrid.Columns[1].Header = strings.Type.Text;
            worktreesGrid.Columns[2].Header = strings.Branch.Text;
            worktreesGrid.Columns[3].Header = strings.Sha1.Text;
        }
    }

    private void OnGridKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && e.KeyModifiers == KeyModifiers.None)
        {
            e.Handled = true;
            OpenSelected();
        }
    }

    private void OpenSelected()
    {
        if (DataContext is ManageWorktreeViewModel viewModel && viewModel.OpenCommand.CanExecute(null))
        {
            viewModel.OpenCommand.Execute(null);
        }
    }
}
