using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using GitUI.Avalonia.Controls;
using GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

namespace GitUI.Avalonia.CommandsDialogs.SettingsDialog.Pages;

public partial class BuildServerIntegrationSettingsPageView : UserControl
{
    private BuildServerIntegrationSettingsPageViewModel? _viewModel;

    public BuildServerIntegrationSettingsPageView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (VisualRoot is not null)
        {
            Observe(DataContext as BuildServerIntegrationSettingsPageViewModel);
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Observe(DataContext as BuildServerIntegrationSettingsPageViewModel);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Observe(null);
        base.OnDetachedFromVisualTree(e);
    }

    private void Observe(BuildServerIntegrationSettingsPageViewModel? viewModel)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = viewModel;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        ShowSettingsControl();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BuildServerIntegrationSettingsPageViewModel.SettingsControl))
        {
            ShowSettingsControl();
        }
    }

    // As ActivateBuildServerSettingsControl: the WinForms control of the plugin is embedded as a child window, with a new host
    // for each control (a host creates its native control once).
    private void ShowSettingsControl()
    {
        buildServerSettingsPanel.Child = _viewModel?.SettingsControl is { } control
            ? new EmbeddedNativeViewHost { Name = "buildServerSettings", View = control, Height = control.PreferredHeight }
            : null;
    }
}
