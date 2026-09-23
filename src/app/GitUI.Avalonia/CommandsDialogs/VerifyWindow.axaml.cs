using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormVerify</c>.</summary>
public partial class VerifyWindow : DialogWindow
{
    private readonly CheckBox _selectAllCheckBox = new() { MinWidth = 0, Padding = new(0), HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center };
    private VerifyViewModel? _viewModel;

    public VerifyWindow()
    {
        InitializeComponent();

        // As FormVerifyShown: fsck runs (in the process dialog) once the window is shown.
        Opened += (_, _) => Dispatcher.UIThread.Post(() => _viewModel?.Initialize());

        // The header of the check box column checks or unchecks all (as DataGridViewCheckBoxHeaderCell).
        _selectAllCheckBox.IsCheckedChanged += (_, _) => _viewModel?.SelectAll(_selectAllCheckBox.IsChecked == true);
        lostObjectsGrid.Columns[0].Header = _selectAllCheckBox;

        lostObjectsGrid.DoubleTapped += (_, e) =>
        {
            // Not on the check box, which the user probably wanted to toggle.
            if (e.Source is not CheckBox)
            {
                _viewModel?.ViewCommand.Execute(null);
            }
        };
        lostObjectsGrid.AddHandler(KeyDownEvent, OnGridKeyDown, RoutingStrategies.Tunnel);

        // As Warnings_CellMouseDown: a right click selects the row, for the context menu.
        lostObjectsGrid.AddHandler(PointerPressedEvent, OnGridPointerPressed, RoutingStrategies.Tunnel);
    }

    private void OnGridPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(lostObjectsGrid).Properties.IsRightButtonPressed
            && (e.Source as Visual)?.FindAncestorOfType<DataGridRow>(includeSelf: true) is { DataContext: LostObjectItem item })
        {
            lostObjectsGrid.SelectedItem = item;
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        _viewModel?.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel = DataContext as VerifyViewModel;
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Column headers are not in the logical tree, so they are not bound.
        VerifyStrings strings = _viewModel.Strings;
        lostObjectsGrid.Columns[1].Header = strings.DateColumn.Text;
        lostObjectsGrid.Columns[2].Header = strings.TypeColumn.Text;
        lostObjectsGrid.Columns[3].Header = strings.SubjectColumn.Text;
        lostObjectsGrid.Columns[4].Header = strings.AuthorColumn.Text;
        lostObjectsGrid.Columns[5].Header = strings.HashColumn.Text;
        lostObjectsGrid.Columns[6].Header = strings.ParentColumn.Text;
        UpdateCommitColumns();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VerifyViewModel.ShowCommitsAndTags))
        {
            UpdateCommitColumns();
        }
    }

    /// <summary>As <c>UpdateFilteredLostObjects</c>: the columns of commits are shown only with them.</summary>
    private void UpdateCommitColumns()
    {
        bool showCommits = _viewModel?.ShowCommitsAndTags ?? true;
        lostObjectsGrid.Columns[3].IsVisible = showCommits;
        lostObjectsGrid.Columns[4].IsVisible = showCommits;
        lostObjectsGrid.Columns[6].IsVisible = showCommits;
    }

    private void OnGridKeyDown(object? sender, KeyEventArgs e)
    {
        // As Warnings_KeyDown: Enter views the object (instead of moving to the next row).
        if (e.Key == Key.Enter && e.KeyModifiers == KeyModifiers.None)
        {
            e.Handled = true;
            _viewModel?.ViewCommand.Execute(null);
        }
    }
}
