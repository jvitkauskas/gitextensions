using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.HelperDialogs;

namespace GitUI.Avalonia.HelperDialogs;

/// <summary>
///  Avalonia port of <c>FormStatus</c> / <c>FormProcess</c>; behaviour lives in <see cref="ProcessViewModel"/>.
/// </summary>
public partial class ProcessWindow : DialogWindow
{
    private static readonly Dictionary<ProcessStatus, Lazy<WindowIcon>> _statusIcons = new()
    {
        [ProcessStatus.Running] = LoadIcon("StatusBadgeWaiting.png"),
        [ProcessStatus.Succeeded] = LoadIcon("StatusBadgeSuccess.png"),
        [ProcessStatus.Failed] = LoadIcon("StatusBadgeError.png"),
    };

    private ProcessViewModel? _viewModel;

    public ProcessWindow()
    {
        InitializeComponent();
        Icon = _statusIcons[ProcessStatus.Running].Value;

        Opened += (_, _) => _viewModel?.Start();
        Closed += (_, _) => _viewModel?.OnClosed();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.InputRequested -= OnInputRequested;
        }

        _viewModel = DataContext as ProcessViewModel;

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _viewModel.InputRequested += OnInputRequested;
            Icon = _statusIcons[_viewModel.Status].Value;
        }

        base.OnDataContextChanged(e);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ProcessViewModel.Status):
                Icon = _statusIcons[_viewModel!.Status].Value;
                break;

            case nameof(ProcessViewModel.PlainText):
                // As PlainTextConsoleCommandRunner: the end of the output is shown.
                plainTextBox.CaretIndex = plainTextBox.Text?.Length ?? 0;
                break;

            case nameof(ProcessViewModel.IsDone) when _viewModel!.IsDone:
                // As in FormStatus: once done, OK takes the focus (and Enter).
                Dispatcher.UIThread.Post(() => okButton.Focus());
                break;
        }
    }

    private void OnInputRequested(object? sender, EventArgs e) => passwordTextBox.Focus();

    private static Lazy<WindowIcon> LoadIcon(string fileName)
        => new(() => new WindowIcon(AssetLoader.Open(new Uri($"avares://GitUI.Avalonia/Assets/{fileName}"))));
}
