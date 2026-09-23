using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.HelperDialogs;

namespace GitUI.Avalonia.HelperDialogs;

/// <summary>Avalonia port of <c>FormSelectMultipleBranches</c>.</summary>
public partial class SelectMultipleBranchesWindow : DialogWindow
{
    public SelectMultipleBranchesWindow()
    {
        InitializeComponent();
        Opened += (_, _) => (branchesListBox.ContainerFromIndex(0) ?? (Control)branchesListBox).Focus();

        // As a CheckedListBox: Space toggles the selected item.
        branchesListBox.AddHandler(KeyDownEvent, OnBranchesKeyDown, RoutingStrategies.Tunnel);
    }

    private void OnBranchesKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space && branchesListBox.SelectedItem is CheckableItem item)
        {
            item.IsChecked = !item.IsChecked;
            e.Handled = true;
        }
    }
}
