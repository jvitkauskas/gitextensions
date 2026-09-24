using Avalonia.Controls;
using GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

namespace GitUI.Avalonia.CommandsDialogs.SettingsDialog.Pages;

public partial class AppearanceSettingsPageView : UserControl
{
    public AppearanceSettingsPageView()
    {
        InitializeComponent();
    }

    // As Dictionary_DropDown: the dictionaries are listed when the list drops down.
    private void Dictionary_DropDownOpened(object? sender, EventArgs e)
        => (DataContext as AppearanceSettingsPageViewModel)?.RefreshDictionaries();
}
