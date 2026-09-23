using Avalonia.Controls;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormPush</c>.</summary>
public partial class PushWindow : DialogWindow
{
    public PushWindow()
    {
        InitializeComponent();

        // As FormPushLoad: the remotes have the focus.
        Opened += (_, _) => remotes.Focus();
    }

    /// <summary>The grid of the multiple branches, e.g. for tests.</summary>
    public DataGrid BranchGrid => branchGrid;

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is not PushViewModel viewModel)
        {
            return;
        }

        // The columns are not in the logical tree, their headers cannot be bound.
        PushStrings strings = viewModel.Strings;
        branchGrid.Columns[0].Header = strings.LocalColumn.Text;
        branchGrid.Columns[1].Header = strings.RemoteColumn.Text;
        branchGrid.Columns[2].Header = strings.AheadBehindColumn.Text;
        branchGrid.Columns[4].Header = strings.ForceColumn.Text;
        branchGrid.Columns[5].Header = strings.DeleteColumn.Text;

        // As menuPushSelection on the header of PushColumn (BranchGrid_ColumnHeaderMouseClick opens it on a left click too).
        MenuFlyout menu = new();
        menu.Items.Add(CreateItem(strings.UnselectAll.Text, tracked: false));
        menu.Items.Add(CreateItem(strings.SelectTracked.Text, tracked: true));
        menu.Items.Add(CreateItem(strings.SelectAll.Text, tracked: null));
        Button header = new() { Name = "pushColumnHeader", Classes = { "link" }, Content = strings.PushColumn.Text, Flyout = menu };
        branchGrid.Columns[3].Header = header;

        MenuItem CreateItem(string text, bool? tracked)
        {
            MenuItem item = new() { Header = text };
            item.Click += (_, _) => viewModel.SelectBranchesToPush(tracked);
            return item;
        }
    }
}
