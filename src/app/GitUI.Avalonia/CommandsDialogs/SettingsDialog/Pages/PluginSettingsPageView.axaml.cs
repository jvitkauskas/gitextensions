using Avalonia.Controls;
using Avalonia.Interactivity;
using GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

namespace GitUI.Avalonia.CommandsDialogs.SettingsDialog.Pages;

public partial class PluginSettingsPageView : UserControl
{
    public PluginSettingsPageView()
    {
        InitializeComponent();
    }

    private void OnLinkClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: PluginSettingRow row })
        {
            row.Activate();
        }
    }
}
