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
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.UserControls.LeftPanel;

namespace GitUI.Avalonia.Controls.LeftPanel;

/// <summary>The converters of the nodes of the left panel.</summary>
public static class LeftPanelConverters
{
    /// <summary>The icon of a node (see <see cref="LeftPanelIcons"/>): the asset of the same name.</summary>
    public static IValueConverter Icon { get; } = new FuncValueConverter<string?, Bitmap?>(key => key is null ? null : FileStatusIconConverter.GetIcon(key));

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
    private LeftPanelViewModel? _viewModel;
    private bool? _expandedBeforeDoubleClick;

    public LeftPanelView()
    {
        InitializeComponent();

        // As contextMenu_Opening: the items for the selection; the menu does not open without any.
        treeMenu.Opening += (_, e) => e.Cancel = FillContextMenu().Count == 0;

        // As OnNodeClick and OnNodeDoubleClick (tunneling, since the tree handles the clicks itself).
        tree.AddHandler(PointerPressedEvent, OnTreePointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        tree.AddHandler(DoubleTappedEvent, OnTreeDoubleTapped, RoutingStrategies.Bubble, handledEventsToo: true);

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

    /// <summary>The tree, e.g. for tests.</summary>
    public TreeView Tree => tree;

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
            menuItem.Icon = new Image { Source = bitmap, Width = 16, Height = 16 };
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
        if (_viewModel is null || (e.Source as StyledElement)?.DataContext is not LeftPanelNode node)
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            _expandedBeforeDoubleClick = node.IsExpanded;
            return;
        }

        bool rightButton = e.GetCurrentPoint(tree).Properties.IsRightButtonPressed;
        bool multiple = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        _viewModel.ClickNode(node, multiple, includingDescendants: e.KeyModifiers.HasFlag(KeyModifiers.Shift), rightButton);

        // Any click selects the node (a right click too, for the context menu), and its revision again if it was selected.
        _viewModel.SelectByClick(node);

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
        bool? expandedBefore = _expandedBeforeDoubleClick;
        _expandedBeforeDoubleClick = null;
        if (_viewModel is null || (e.Source as StyledElement)?.DataContext is not LeftPanelNode node || !node.HasDoubleClickAction)
        {
            return;
        }

        // A double click on the expander only expands or collapses (as the PlusMinus in OnNodeDoubleClick).
        if ((e.Source as Visual)?.FindAncestorOfType<ToggleButton>(includeSelf: true) is not null)
        {
            return;
        }

        // A node with an operation does not expand or collapse (as BeforeDoubleClickExpandCollapse).
        if (node.HasChildren && expandedBefore is bool expanded)
        {
            node.IsExpanded = expanded;
        }

        _viewModel.DoubleClickNode(node);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // As EnsureVerticallyVisible: the selected node is scrolled into view.
        if (e.PropertyName == nameof(LeftPanelViewModel.SelectedNode) && _viewModel?.SelectedNode is { } node)
        {
            Dispatcher.UIThread.Post(() => tree.TreeContainerFromItem(node)?.BringIntoView(), DispatcherPriority.Background);
        }
    }
}
