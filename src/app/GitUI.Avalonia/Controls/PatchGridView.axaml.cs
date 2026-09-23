using System.ComponentModel;
using Avalonia.Controls;
using GitUI.Presentation.UserControls;

namespace GitUI.Avalonia.Controls;

/// <summary>Avalonia port of <c>PatchGrid</c>; behaviour lives in <c>PatchGridViewModel</c>.</summary>
public partial class PatchGridView : UserControl
{
    private PatchGridViewModel? _viewModel;

    public PatchGridView()
    {
        InitializeComponent();

        patchesGrid.LoadingRow += (_, e) => e.Row.Classes.Set("next", e.Row.DataContext is PatchItem { IsNext: true });

        // As Patches_DoubleClick.
        patchesGrid.DoubleTapped += (_, e) =>
        {
            if (e.Source is Control { DataContext: PatchItem } && _viewModel?.OpenSelectedCommand.CanExecute(null) == true)
            {
                _viewModel.OpenSelectedCommand.Execute(null);
            }
        };
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = DataContext as PatchGridViewModel;
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Column headers are not in the logical tree, so they are not bound.
        PatchGridStrings strings = _viewModel.Strings;
        string[] headers = [strings.Status.Text, strings.Action.Text, strings.FileName.Text, strings.Subject.Text, strings.Author.Text, strings.Date.Text, strings.CommitHash.Text];
        for (int i = 0; i < headers.Length; i++)
        {
            patchesGrid.Columns[i].Header = headers[i];
        }

        // As PatchGrid.UpdateState.
        patchesGrid.Columns[1].IsVisible = _viewModel.IsManagingRebase;
        patchesGrid.Columns[2].IsVisible = !_viewModel.IsManagingRebase;
        patchesGrid.Columns[6].IsVisible = _viewModel.IsManagingRebase;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // As DisplayPatches: the commit or patch being applied is scrolled into view.
        if (e.PropertyName == nameof(PatchGridViewModel.SelectedPatch) && _viewModel?.SelectedPatch is { } selected)
        {
            patchesGrid.ScrollIntoView(selected, null);
        }
    }
}
