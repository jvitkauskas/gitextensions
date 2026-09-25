using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GitUI.Avalonia.Controls.FileStatusList;
using GitUI.Avalonia.Controls.FlatTree;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.UserControls.LeftPanel;

namespace GitUI.Avalonia.Controls.LeftPanel;

/// <summary>The converters of the nodes of the left panel.</summary>
public static class LeftPanelConverters
{
    /// <summary>The icon of a node (see <see cref="LeftPanelIcons"/>): the asset of the same name.</summary>
    public static IValueConverter Icon { get; } = new FuncValueConverter<string?, Bitmap?>(key => key is null ? null : FileStatusIconConverter.GetIcon(key));

    /// <summary>Whether the icon is lightened on a dark theme, as the icons <c>RepoObjectsTree</c> adapts (<c>AdaptLightness</c>).</summary>
    public static IValueConverter AdaptsLightness { get; } = new FuncValueConverter<string?, bool>(AdaptsIconLightness);

    // As InitImageList and the menu items of RepoObjectsTree whose images are adapted.
    internal static bool AdaptsIconLightness(string? key)
        => key is "Branch" or "EyeClosed" or "EyeOpened" or "RemoteEnableAndFetch" or "CollapseAll" or "ExpandAll";

    public static IValueConverter Bold { get; } = new FuncValueConverter<bool, FontWeight>(value => value ? FontWeight.Bold : FontWeight.Normal);

    public static IValueConverter Italic { get; } = new FuncValueConverter<bool, FontStyle>(value => value ? FontStyle.Italic : FontStyle.Normal);

    /// <summary>The multi-selected nodes are underlined.</summary>
    public static IValueConverter Underline { get; } = new FuncValueConverter<bool, TextDecorationCollection?>(value => value ? TextDecorations.Underline : null);
}

/// <summary>
///  The Avalonia left panel of the main window (port of <c>RepoObjectsTree</c>; docs/avalonia-port/PLAN.md, phase 7): the trees
///  of references, submodules and worktrees, their toolbar, the search box and the context menu.
/// </summary>
public partial class LeftPanelView : UserControl, IHotkeyControl
{
    /// <summary>How the list reads the nodes of the trees.</summary>
    private static readonly FlatTreeAdapter NodeAdapter = new(
        GetChildren: node => ((LeftPanelNode)node).Children,
        ChildrenPropertyName: nameof(LeftPanelNode.Children),
        IsExpandedPropertyName: nameof(LeftPanelNode.IsExpanded),
        IsExpanded: node => ((LeftPanelNode)node).IsExpanded,
        SetExpanded: (node, isExpanded) => ((LeftPanelNode)node).IsExpanded = isExpanded);

    private LeftPanelViewModel? _viewModel;
    private FlatTreeList? _tree;
    private bool _selectingFromViewModel;

    public LeftPanelView()
    {
        InitializeComponent();

        // As contextMenu_Opening: the items for the selection; the menu does not open without any.
        treeMenu.Opening += (_, e) => e.Cancel = FillContextMenu().Count == 0;

        // As OnNodeClick and OnNodeDoubleClick (tunneling, since the list handles the clicks itself).
        tree.AddHandler(PointerPressedEvent, OnTreePointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        tree.AddHandler(DoubleTappedEvent, OnTreeDoubleTapped, RoutingStrategies.Bubble, handledEventsToo: true);

        // The selected row is the selected node of the view model.
        tree.SelectionChanged += (_, _) =>
        {
            if (!_selectingFromViewModel && _viewModel is not null && tree.SelectedItem is FlatTreeRow { Node: LeftPanelNode node })
            {
                _viewModel.SelectedNode = node;
            }
        };

        // As NativeTreeViewExplorerNavigationDecorator: the arrow keys do not select the revisions, Space and Enter do.
        tree.AddHandler(KeyDownEvent, OnTreeKeyDown, RoutingStrategies.Tunnel);

        // As TxtBranchCriterion_KeyDown: Enter searches (after the suggestion list took its choice).
        searchBox.AddHandler(
            KeyDownEvent,
            (_, e) =>
            {
                if (e.Key == Key.Enter && _viewModel is not null)
                {
                    _viewModel.Search();
                    e.Handled = true;
                }
            },
            RoutingStrategies.Bubble,
            handledEventsToo: true);
    }

    /// <summary>The list of the visible nodes of the trees, e.g. for tests.</summary>
    public ListBox Tree => tree;

    /// <summary>The visible nodes of the trees, e.g. for tests.</summary>
    public FlatTreeList? FlatTree => _tree;

    /// <summary>The context menu, e.g. for tests.</summary>
    public ContextMenu Menu => treeMenu;

    public AutoCompleteBox SearchBox => searchBox;

    /// <summary>Fills the context menu for the selection (as <c>contextMenu_Opening</c>); returns its items (e.g. for tests).</summary>
    public IReadOnlyList<LeftPanelMenuItem> FillContextMenu()
    {
        treeMenu.Items.Clear();
        IReadOnlyList<LeftPanelMenuItem> items = _viewModel?.GetContextMenu() ?? [];
        foreach (LeftPanelMenuItem item in items)
        {
            treeMenu.Items.Add(CreateItem(item));
        }

        return items;
    }

    /// <summary>As <c>ProcessHotkey</c>: the "LeftPanel" hotkeys, except the keys editing the text of the search box.</summary>
    public bool ProcessHotkey(int keyData)
    {
        if (_viewModel?.Hotkeys.FirstOrDefault(h => h.KeyData == keyData) is not { } hotkey)
        {
            return false;
        }

        if (searchBox.IsKeyboardFocusWithin && KeyMapping.IsTextEditKey(keyData))
        {
            return false;
        }

        return _viewModel.ExecuteHotkey((LeftPanelHotkeyCommand)hotkey.CommandCode);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        _viewModel?.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel?.FocusRequested -= OnFocusRequested;
        _viewModel = DataContext as LeftPanelViewModel;
        _viewModel?.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel?.FocusRequested += OnFocusRequested;

        // A node with an operation on double click (e.g. checking out a branch) does not expand or collapse (as
        // BeforeDoubleClickExpandCollapse).
        _tree?.Dispose();
        _tree = _viewModel is null ? null : new FlatTreeList(tree, _viewModel.Trees, NodeAdapter, toggleOnDoubleTap: node => !((LeftPanelNode)node).HasDoubleClickAction);
        SelectRowOfSelectedNode();
    }

    /// <summary>The node shown by the element of a row (its content has the node, its expander the row).</summary>
    private static LeftPanelNode? NodeOf(object? source)
        => (source as StyledElement)?.DataContext switch
        {
            LeftPanelNode node => node,
            FlatTreeRow { Node: LeftPanelNode node } => node,
            _ => null,
        };

    private void SelectRowOfSelectedNode()
    {
        // As the selected node of a tree: its parents are expanded.
        for (LeftPanelNode? parent = _viewModel?.SelectedNode?.Parent; parent is not null; parent = parent.Parent)
        {
            parent.IsExpanded = true;
        }

        _selectingFromViewModel = true;
        try
        {
            _tree?.Select(_viewModel?.SelectedNode);
        }
        finally
        {
            _selectingFromViewModel = false;
        }
    }

    private void OnFocusRequested(object? sender, EventArgs e) => Dispatcher.UIThread.Post(() => tree.Focus(), DispatcherPriority.Background);

    private static Control CreateItem(LeftPanelMenuItem item)
    {
        if (item.IsSeparator)
        {
            return new Separator();
        }

        MenuItem menuItem = new() { Header = item.Header, IsEnabled = item.IsEnabled };
        if (item.Icon is { } icon && FileStatusIconConverter.GetIcon(icon) is { } bitmap)
        {
            Image image = new() { Source = bitmap, Width = 16, Height = 16 };
            ImageLightness.SetAdapt(image, LeftPanelConverters.AdaptsIconLightness(icon));
            menuItem.Icon = image;
        }
        else if (item.Image is { } image)
        {
            try
            {
                menuItem.Icon = new Image { Source = new global::Avalonia.Media.Imaging.Bitmap(new MemoryStream(image)), Width = 16, Height = 16 };
            }
            catch (Exception)
            {
                // Not an image Avalonia can decode: no icon.
            }
        }

        if (item.ToolTip is { } toolTip)
        {
            ToolTip.SetTip(menuItem, toolTip);
        }

        if (item.IsChecked is bool isChecked)
        {
            menuItem.ToggleType = MenuItemToggleType.CheckBox;
            menuItem.IsChecked = isChecked;
        }

        if (item.Children is { } children)
        {
            menuItem.ItemsSource = children.Select(CreateItem).ToList();
        }
        else if (item.Execute is { } execute)
        {
            menuItem.Click += (_, _) => execute();
        }

        return menuItem;
    }

    private void OnTreePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_viewModel is null || NodeOf(e.Source) is not { } node || e.ClickCount == 2)
        {
            return;
        }

        bool rightButton = e.GetCurrentPoint(tree).Properties.IsRightButtonPressed;
        bool multiple = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        _viewModel.ClickNode(node, multiple, includingDescendants: e.KeyModifiers.HasFlag(KeyModifiers.Shift), rightButton);

        // Any click selects the node (a right click too, for the context menu), and its revision again if it was selected.
        _viewModel.SelectByClick(node, alternate: e.KeyModifiers.HasFlag(KeyModifiers.Alt));

        // With Ctrl, the tree would toggle its own selection.
        if (multiple)
        {
            e.Handled = true;
            tree.Focus();
        }
    }

    private void OnTreeKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Up or Key.Down or Key.Left or Key.Right or Key.Home or Key.End or Key.PageUp or Key.PageDown:
                _viewModel?.NavigateByKeyboard();
                break;
            case Key.Space or Key.Enter when e.KeyModifiers == KeyModifiers.None:
                _viewModel?.ActivateSelectedNode();
                break;
        }
    }

    private void OnTreeDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_viewModel is null || NodeOf(e.Source) is not { HasDoubleClickAction: true } node)
        {
            return;
        }

        // A double click on the expander only expands or collapses (as the PlusMinus in OnNodeDoubleClick).
        if ((e.Source as Visual)?.FindAncestorOfType<ToggleButton>(includeSelf: true) is not null)
        {
            return;
        }

        _viewModel.DoubleClickNode(node);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // As EnsureVerticallyVisible: the selected node is selected in the list and scrolled into view.
        if (e.PropertyName == nameof(LeftPanelViewModel.SelectedNode))
        {
            SelectRowOfSelectedNode();
        }
    }
}
