using System.ComponentModel;
using Avalonia.Input;
using Avalonia.Threading;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs.RepoHosting;

namespace GitUI.Avalonia.CommandsDialogs.RepoHosting;

/// <summary>Avalonia port of <c>ForkAndCloneForm</c>; behaviour lives in <see cref="ForkAndCloneViewModel"/>.</summary>
public partial class ForkAndCloneWindow : DialogWindow
{
    public ForkAndCloneWindow()
    {
        InitializeComponent();

        // As _searchTB_Enter / _searchTB_Leave: Enter in the search text searches (AcceptButton = searchBtn).
        searchText.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter && DataContext is ForkAndCloneViewModel viewModel && viewModel.SearchCommand.CanExecute(null))
            {
                e.Handled = true;
                viewModel.SearchCommand.Execute(null);
            }
        };

        // As ForkAndCloneForm_Load.
        Opened += (_, _) => Dispatcher.UIThread.Post(() => (DataContext as ForkAndCloneViewModel)?.Load());
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // Column headers are not in the logical tree, so they are not bound.
        if (DataContext is ForkAndCloneViewModel viewModel)
        {
            ForkAndCloneStrings strings = viewModel.Strings;
            myRepositories.Columns[0].Header = strings.NameColumn.Text;
            myRepositories.Columns[1].Header = strings.IsForkColumn.Text;
            myRepositories.Columns[2].Header = strings.ForksColumn.Text;
            myRepositories.Columns[3].Header = strings.IsPrivateColumn.Text;
            searchResults.Columns[0].Header = strings.SearchNameColumn.Text;
            searchResults.Columns[1].Header = strings.SearchOwnerColumn.Text;
            searchResults.Columns[2].Header = strings.SearchIsForkColumn.Text;
            searchResults.Columns[3].Header = strings.SearchForksColumn.Text;
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    /// <summary>As <c>_tabControl_SelectedIndexChanged</c>: the search text is focused on the search tab.</summary>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ForkAndCloneViewModel.SelectedTabIndex) && sender is ForkAndCloneViewModel { IsSearchTab: true })
        {
            Dispatcher.UIThread.Post(() => searchText.Focus());
        }
    }
}
