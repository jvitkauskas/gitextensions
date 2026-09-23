using System.ComponentModel;
using Avalonia.Media;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormRemotes</c>.</summary>
public partial class RemotesWindow : DialogWindow
{
    private RemotesViewModel? _viewModel;

    public RemotesWindow()
    {
        InitializeComponent();

        // As FormRemotes: entering the URLs suggests URLs for the remote, entering the name suggests one from the URL.
        urlComboBox.GotFocus += (_, _) => _viewModel?.SuggestUrls(push: false);
        pushUrlComboBox.GotFocus += (_, _) => _viewModel?.SuggestUrls(push: true);
        nameTextBox.GotFocus += (_, _) => _viewModel?.SuggestNameFromUrl();
        prefixTextBox.LostFocus += (_, _) => _viewModel?.NormalisePrefix();
        mergeWithComboBox.DropDownOpened += (_, _) => _viewModel?.LoadMergeWithCandidates();

        Opened += (_, _) =>
        {
            if (_viewModel?.Remotes.Count > 0)
            {
                remotesListBox.Focus();
            }
            else
            {
                nameTextBox.Focus();
            }
        };
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        _viewModel?.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel = DataContext as RemotesViewModel;
        _viewModel?.PropertyChanged += OnViewModelPropertyChanged;
        base.OnDataContextChanged(e);

        if (_viewModel is { Strings: var strings })
        {
            // Column headers are not in the logical tree, so they are not bound.
            headsGrid.Columns[0].Header = strings.LocalBranchNameColumn.Text;
            headsGrid.Columns[1].Header = strings.RemoteRepositoryColumn.Text;
            headsGrid.Columns[2].Header = strings.MergeWithColumn.Text;
            ShowColor();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RemotesViewModel.Color))
        {
            ShowColor();
        }
    }

    private void ShowColor()
        => colorSwatch.Background = _viewModel?.Color is { } html && Color.TryParse(html, out Color color) ? new SolidColorBrush(color) : null;
}
