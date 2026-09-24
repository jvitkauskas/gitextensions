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
        treeMenu.Opening += (_, _) =>
        {
            if (DataContext is FileStatusListViewModel viewModel)
            {
                viewModel.UpdateMenuState();
                InsertDirectScripts(viewModel);
            }
        };

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

    // As AddUserScripts: the scripts of ScriptEvent.ShowInFileList follow "Run script" in the menu itself, the others are under it.
    private void InsertDirectScripts(FileStatusListViewModel viewModel)
    {
        foreach (MenuItem item in treeMenu.Items.OfType<MenuItem>().Where(item => item.Tag is FileStatusScript).ToList())
        {
            treeMenu.Items.Remove(item);
        }

        runScriptMenuItem.Items.Clear();
        int index = treeMenu.Items.IndexOf(runScriptMenuItem);
        foreach (FileStatusScript script in viewModel.MenuState.Scripts)
        {
            MenuItem item = new()
            {
                Header = script.Name.Replace("_", "__"),
                Tag = script,
                Command = viewModel.RunScriptCommand,
                CommandParameter = script,
            };
            if (script.IsDirect)
            {
                treeMenu.Items.Insert(++index, item);
            }
            else
            {
                runScriptMenuItem.Items.Add(item);
            }
        }
    }

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
