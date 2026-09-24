using Avalonia.Threading;
using GitUI.Avalonia.Hosting;
using AvaloniaCheckBox = Avalonia.Controls.CheckBox;

namespace GitExtensions.Plugins.DeleteUnusedBranches;

/// <summary>Avalonia port of <c>DeleteUnusedBranchesForm</c>; behaviour lives in <c>DeleteUnusedBranchesViewModel</c>.</summary>
public partial class DeleteUnusedBranchesWindow : DialogWindow
{
    /// <summary>The check box of the header of the delete column (<c>DataGridViewCheckBoxHeaderCell</c>).</summary>
    private readonly AvaloniaCheckBox _selectAll = new() { Name = "selectAllCheckBox", IsThreeState = false, MinWidth = 0, Padding = new(0), Margin = new(-8, 0, 0, 0) };

    public DeleteUnusedBranchesWindow()
    {
        InitializeComponent();

        _selectAll.IsCheckedChanged += (_, _) =>
        {
            if (DataContext is DeleteUnusedBranchesViewModel viewModel && _selectAll.IsChecked is bool isChecked && viewModel.AllSelected != isChecked)
            {
                viewModel.AllSelected = isChecked;
            }
        };

        // As OnLoad: the first search once the window is shown.
        Opened += (_, _) => Dispatcher.UIThread.Post(() => _ = (DataContext as DeleteUnusedBranchesViewModel)?.LoadAsync());
    }

    /// <summary>The check box in the header of the delete column.</summary>
    internal AvaloniaCheckBox SelectAllCheckBox => _selectAll;

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // Column headers are not in the logical tree, so they are not bound.
        if (DataContext is DeleteUnusedBranchesViewModel viewModel)
        {
            DeleteUnusedBranchesStrings strings = viewModel.Strings;
            branchesGrid.Columns[0].Header = _selectAll;
            branchesGrid.Columns[1].Header = strings.NameColumn.Text;
            branchesGrid.Columns[2].Header = strings.DateColumn.Text;
            branchesGrid.Columns[3].Header = strings.AuthorColumn.Text;
            branchesGrid.Columns[4].Header = strings.MessageColumn.Text;

            _selectAll.IsChecked = viewModel.AllSelected;
            viewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(DeleteUnusedBranchesViewModel.AllSelected))
                {
                    _selectAll.IsChecked = viewModel.AllSelected;
                }
            };
        }
    }
}
