using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Threading;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs.BrowseDialog;

namespace GitUI.Avalonia.CommandsDialogs.BrowseDialog;

/// <summary>Avalonia port of <c>FormUpdates</c>.</summary>
public partial class UpdatesWindow : DialogWindow
{
    private UpdatesViewModel? _viewModel;

    public UpdatesWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        _viewModel?.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel = DataContext as UpdatesViewModel;
        _viewModel?.PropertyChanged += OnViewModelPropertyChanged;
        base.OnDataContextChanged(e);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // As FormUpdates.Done: focus the action that updates, once the bindings have shown it.
        if (e.PropertyName == nameof(UpdatesViewModel.IsUpdateFound) && _viewModel?.IsUpdateFound == true)
        {
            bool canUpdateNow = _viewModel.CanUpdateNow;
            Dispatcher.UIThread.Post(() => (canUpdateNow ? updateNowButton : (Control)directDownloadLink).Focus());
        }
    }
}
