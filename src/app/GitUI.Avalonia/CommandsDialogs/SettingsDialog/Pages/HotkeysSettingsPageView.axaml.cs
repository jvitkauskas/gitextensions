using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

namespace GitUI.Avalonia.CommandsDialogs.SettingsDialog.Pages;

public partial class HotkeysSettingsPageView : UserControl
{
    public HotkeysSettingsPageView()
    {
        InitializeComponent();

        // As TextboxHotkey.ProcessCmdKey: the keys typed are the hotkey (not a modifier alone), all the keys are swallowed.
        hotkeyBox.AddHandler(KeyDownEvent, OnHotkeyKeyDown, RoutingStrategies.Tunnel);
    }

    /// <summary>The text box of the hotkey (<c>txtHotkey</c>), e.g. for tests.</summary>
    public TextBox HotkeyBox => hotkeyBox;

    public DataGrid MappingsGrid => mappingsGrid;

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // The headers are not bound: the columns are not in the logical tree.
        if (DataContext is HotkeysSettingsPageViewModel viewModel)
        {
            mappingsGrid.Columns[0].Header = viewModel.Strings.Command.Text;
            mappingsGrid.Columns[1].Header = viewModel.Strings.Key.Text;
        }
    }

    private void OnHotkeyKeyDown(object? sender, KeyEventArgs e)
    {
        e.Handled = true;
        if (e.Key is Key.LeftShift or Key.RightShift or Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin
            || DataContext is not HotkeysSettingsPageViewModel viewModel)
        {
            return;
        }

        viewModel.KeyData = KeyMapping.ToKeyData(e.Key, e.KeyModifiers);
    }
}
