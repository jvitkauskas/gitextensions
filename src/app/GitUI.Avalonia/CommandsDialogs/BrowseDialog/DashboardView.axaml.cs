using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GitUI.Avalonia.CommandsDialogs.SettingsDialog;
using GitUI.Presentation.CommandsDialogs.BrowseDialog;

namespace GitUI.Avalonia.CommandsDialogs.BrowseDialog;

/// <summary>
///  Avalonia port of <c>Dashboard</c> with its <c>UserRepositoriesList</c>: the start page of the main window without a
///  repository.
/// </summary>
public partial class DashboardView : UserControl
{
    /// <summary>The width of the tiles: from the repositories combobox, or fitting the longest caption.</summary>
    public static readonly StyledProperty<double> TileWidthProperty = AvaloniaProperty.Register<DashboardView, double>(nameof(TileWidth), 350);

    private readonly ContextMenu _repositoryMenu = new();
    private readonly ContextMenu _groupMenu = new();
    private DashboardViewModel? _viewModel;

    public DashboardView()
    {
        InitializeComponent();

        // As searchBox_KeyDown: Enter opens the first repository, Down moves to the tiles.
        searchBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                _viewModel?.OpenFirst();
                e.Handled = true;
            }
            else if (e.Key == Key.Down && GetTileLists().FirstOrDefault() is { ItemCount: > 0 } first)
            {
                first.SelectedIndex = Math.Max(first.SelectedIndex, 0);
                first.ContainerFromIndex(first.SelectedIndex)?.Focus(NavigationMethod.Directional);
                e.Handled = true;
            }
        };

        // As listView1_MouseClick: a left click opens the repository under the mouse.
        groups.AddHandler(PointerReleasedEvent, OnTilePointerReleased, RoutingStrategies.Bubble, handledEventsToo: true);
        groups.AddHandler(KeyDownEvent, OnTileKeyDown, RoutingStrategies.Tunnel);

        // A single selection over the groups (one ListView in WinForms).
        groups.AddHandler(SelectingItemsControl.SelectionChangedEvent, OnTileSelectionChanged);

        // As contextMenuStrip_Opening: the menu of the repository under the mouse, which is selected.
        groups.AddHandler(ContextRequestedEvent, OnTileContextRequested, RoutingStrategies.Bubble);

        // As ListView1_GroupTaskLinkClick: the menu of the group.
        groups.AddHandler(Button.ClickEvent, (_, e) =>
        {
            if (e.Source is Button { DataContext: DashboardGroup group } button && button.Classes.Contains("groupActions"))
            {
                ShowGroupMenu(group, button);
                e.Handled = true;
            }
        });

        // As OnDragEnter and OnDragDrop: a directory dropped on the dashboard is opened.
        AddHandler(DragDrop.DragOverEvent, (_, e) => e.DragEffects = GetDroppedDirectory(e) is null ? DragDropEffects.None : DragDropEffects.Copy);
        AddHandler(DragDrop.DropEvent, (_, e) =>
        {
            if (GetDroppedDirectory(e) is { } directory)
            {
                _viewModel?.OpenDroppedDirectory(directory);
            }
        });
    }

    public TextBox SearchBox => searchBox;

    public double TileWidth
    {
        get => GetValue(TileWidthProperty);
        private set => SetValue(TileWidthProperty, value);
    }

    /// <summary>The menu of the last repository or group whose menu was shown (e.g. for tests).</summary>
    public ContextMenu RepositoryMenu => _repositoryMenu;

    public ContextMenu GroupMenu => _groupMenu;

    /// <summary>The lists of the tiles, one per group.</summary>
    public IEnumerable<ListBox> GetTileLists() => groups.GetVisualDescendants().OfType<ListBox>().Where(l => l.Classes.Contains("tiles"));

    /// <summary>Fills the menu of a repository and opens it over its tile.</summary>
    public void ShowRepositoryMenu(DashboardRepositoryItem item, Control target)
    {
        if (_viewModel is null)
        {
            return;
        }

        Fill(_repositoryMenu, _viewModel.GetRepositoryMenu(item));
        _repositoryMenu.Open(target);
    }

    /// <summary>Fills the menu of a group (its "Actions" link) and opens it below the link.</summary>
    public void ShowGroupMenu(DashboardGroup group, Control target)
    {
        if (_viewModel is null)
        {
            return;
        }

        Fill(_groupMenu, _viewModel.GetGroupMenu(group));
        _groupMenu.Placement = PlacementMode.BottomEdgeAlignedRight;
        _groupMenu.Open(target);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        _viewModel?.Groups.CollectionChanged -= OnGroupsChanged;
        _viewModel = DataContext as DashboardViewModel;
        _viewModel?.Groups.CollectionChanged += OnGroupsChanged;
        UpdateTileWidth();
    }

    private void OnGroupsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => UpdateTileWidth();

    // As GetTileSize: the longest caption and the folder icon, and 50 more; or the width given by the view model.
    private void UpdateTileWidth()
    {
        if (_viewModel is null)
        {
            return;
        }

        if (!double.IsNaN(_viewModel.TileWidth))
        {
            TileWidth = _viewModel.TileWidth;
            return;
        }

        List<string> captions = [.. _viewModel.Repositories.Select(r => r.Caption)];
        if (captions.Count == 0)
        {
            return;
        }

        Typeface typeface = new(FontFamily, FontStyle, FontWeight);
        double textWidth = captions.Max(caption =>
        {
            using TextLayout layout = new(caption, typeface, FontSize, foreground: null);
            return layout.WidthIncludingTrailingWhitespace;
        });
        TileWidth = Math.Ceiling(textWidth + 32 + 50);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // The fonts of AppSettings.Font: 5.5 points larger for the headings, 1 point smaller for the branches.
        double fontSize = this.TryFindResource("ControlContentThemeFontSize", ActualThemeVariant, out object? size) && size is double d ? d : 12;
        Resources["DashboardHeadingFontSize"] = fontSize + (5.5 * 96 / 72);
        Resources["DashboardSecondaryFontSize"] = fontSize - (96.0 / 72);

        // As RecentRepositoriesList_Load: the search box has the focus.
        Dispatcher.UIThread.Post(() => searchBox.Focus());
    }

    private static string? GetDroppedDirectory(DragEventArgs e)
    {
        // As OnDragEnter: a single existing directory.
        IStorageItem[]? items = e.DataTransfer.TryGetFiles();
        if (items is not { Length: 1 } || items[0].TryGetLocalPath() is not { } path || !Directory.Exists(path))
        {
            return null;
        }

        return path;
    }

    private static DashboardRepositoryItem? GetTile(RoutedEventArgs e, out ListBoxItem? container)
    {
        container = (e.Source as Visual)?.FindAncestorOfType<ListBoxItem>(includeSelf: true);
        return container?.DataContext as DashboardRepositoryItem;
    }

    private void OnTilePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Left && GetTile(e, out _) is { } item)
        {
            _viewModel?.Open(item);
        }
    }

    // As ProcessDialogKey (Enter opens the selected repository) and listView1_KeyDown (Up on the first row goes to the search).
    private void OnTileKeyDown(object? sender, KeyEventArgs e)
    {
        if ((e.Source as Visual)?.FindAncestorOfType<ListBox>(includeSelf: true) is not { } list || !list.Classes.Contains("tiles"))
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            _viewModel?.Open(list.SelectedItem as DashboardRepositoryItem);
            e.Handled = true;
        }
        else if (e.Key == Key.Up && list == GetTileLists().FirstOrDefault() && IsOnFirstRow(list))
        {
            searchBox.Focus();
            e.Handled = true;
        }
    }

    private static bool IsOnFirstRow(ListBox list)
        => list.SelectedIndex < 0
            || (list.ContainerFromIndex(list.SelectedIndex) is { } selected && list.ContainerFromIndex(0) is { } first
                && Math.Abs(selected.Bounds.Y - first.Bounds.Y) < 1);

    private void OnTileSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0 || e.Source is not ListBox source)
        {
            return;
        }

        foreach (ListBox list in GetTileLists().Where(l => l != source))
        {
            list.SelectedItem = null;
        }
    }

    private void OnTileContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (GetTile(e, out ListBoxItem? container) is not { } item || container is null)
        {
            return;
        }

        container.IsSelected = true;
        ShowRepositoryMenu(item, container);
        e.Handled = true;
    }

    private static void Fill(ContextMenu menu, IReadOnlyList<DashboardMenuItem> items)
    {
        menu.Items.Clear();
        foreach (Control item in items.Select(CreateItem))
        {
            menu.Items.Add(item);
        }
    }

    private static Control CreateItem(DashboardMenuItem item)
    {
        if (item.IsSeparator)
        {
            return new Separator();
        }

        MenuItem menuItem = new() { Header = item.Header, IsEnabled = item.IsEnabled };
        if (item.Icon is not null && SettingsIconConverter.Instance.Convert(item.Icon, typeof(object), null, CultureInfo.InvariantCulture) is IImage icon)
        {
            menuItem.Icon = new Image { Source = icon, Width = 16, Height = 16 };
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
}
