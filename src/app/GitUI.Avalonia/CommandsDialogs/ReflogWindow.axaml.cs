using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormReflog</c>.</summary>
public partial class ReflogWindow : DialogWindow
{
    public ReflogWindow()
    {
        InitializeComponent();

        // As FormReflog: the row under the mouse is selected, and a click opens the actions on it.
        reflogGrid.PointerMoved += (_, e) =>
        {
            if ((e.Source as Visual)?.FindAncestorOfType<DataGridRow>(includeSelf: true) is { DataContext: ReflogEntry entry }
                && DataContext is ReflogViewModel viewModel)
            {
                viewModel.SelectedEntry = entry;
            }
        };
        reflogGrid.PointerReleased += (_, e) =>
        {
            if (e.InitialPressMouseButton == MouseButton.Left
                && (e.Source as Visual)?.FindAncestorOfType<DataGridRow>(includeSelf: true) is not null)
            {
                reflogContextMenu.Open(reflogGrid);
            }
        };
        Opened += (_, _) => referencesComboBox.Focus();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // Column headers are not in the logical tree, so they are not bound.
        if (DataContext is ReflogViewModel { Strings: var strings })
        {
            reflogGrid.Columns[0].Header = strings.Sha.Text;
            reflogGrid.Columns[1].Header = strings.Ref.Text;
            reflogGrid.Columns[2].Header = strings.Action.Text;
        }
    }
}
