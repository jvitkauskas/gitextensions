using Avalonia.Controls;

namespace GitUI.Avalonia.CommandsDialogs.SettingsDialog.Pages;

public partial class ShellExtensionSettingsPageView : UserControl
{
    public ShellExtensionSettingsPageView()
    {
        InitializeComponent();
    }
}

/// <summary>
///  An item of the context menu of the shell extension: a click cycles as <c>chlMenuEntries_ItemCheck</c> (checked, unchecked,
///  indeterminate), not as a three-state check box (unchecked, checked, indeterminate).
/// </summary>
public sealed class ShellMenuEntryCheckBox : CheckBox
{
    protected override Type StyleKeyOverride => typeof(CheckBox);

    protected override void Toggle()
        => SetCurrentValue(IsCheckedProperty, IsChecked switch
        {
            true => false,
            false => null,
            null => true,
        });
}
