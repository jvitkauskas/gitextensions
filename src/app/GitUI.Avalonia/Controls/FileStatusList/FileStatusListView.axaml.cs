using Avalonia.Controls;
using GitUI.Presentation.UserControls.FileStatusList;

namespace GitUI.Avalonia.Controls.FileStatusList;

/// <summary>
///  The Avalonia file status list (port of the WinForms <c>FileStatusList</c>; docs/avalonia-port/PLAN.md, phase 5):
///  the files as a tree with their status icons, multiple selection, the filter and the sorting.
/// </summary>
public partial class FileStatusListView : UserControl
{
    public FileStatusListView()
    {
        InitializeComponent();

        // As ItemContextMenu_Opening: the items for the selection.
        treeMenu.Opening += (_, _) => (DataContext as FileStatusListViewModel)?.UpdateMenuState();

        // A double click on a file activates the selection (a folder expands instead).
        filesTree.DoubleTapped += (_, e) =>
        {
            if (DataContext is FileStatusListViewModel viewModel
                && (e.Source as global::Avalonia.StyledElement)?.DataContext is FileStatusNode { Entry: not null })
            {
                viewModel.ActivateSelection();
            }
        };
    }

    /// <summary>The context menu, e.g. for tests.</summary>
    public ContextMenu Menu => treeMenu;

    /// <summary>The tree, e.g. for tests.</summary>
    public TreeView Tree => filesTree;

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // The tree adds and removes the nodes the user selects, and follows the selection of the view model.
        if (DataContext is FileStatusListViewModel viewModel)
        {
            filesTree.SelectedItems = viewModel.SelectedNodes;
        }
    }
}
