using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Threading;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.CommandsDialogs.SettingsDialog;
using GitUI.Avalonia.Controls.RevisionGrid;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.Avalonia.CommandsDialogs.BrowseDialog;

/// <summary>Avalonia port of <c>FormBrowse</c> (first part).</summary>
public partial class BrowseWindow : DialogWindow
{
    private readonly List<(MenuItem Item, BrowseSubmenu Submenu)> _submenus = [];
    private readonly HashSet<MenuItem> _submenuOwners = [];
    private BrowseViewModel? _viewModel;
    private bool _isOpened;

    public BrowseWindow()
    {
        InitializeComponent();

        // As OnRuntimeLoad: the revisions are loaded once the window is shown.
        Opened += (_, _) =>
        {
            _isOpened = true;
            Dispatcher.UIThread.Post(() => _viewModel?.Initialize(SelectedId));
        };
        tabs.SelectionChanged += (_, _) =>
        {
            if (_viewModel is not null && tabs.SelectedIndex >= 0)
            {
                _viewModel.SelectedTab = (BrowseTab)tabs.SelectedIndex;
            }
        };

        // As the DropDownOpening of the recent and favourite repositories: their items are read when the Start menu opens.
        mainMenu.AddHandler(MenuItem.SubmenuOpenedEvent, (_, e) =>
        {
            if (e.Source is MenuItem opened && _submenuOwners.Contains(opened))
            {
                RefreshSubmenus();
            }
        });
    }

    /// <summary>The revision to select first (<c>BrowseArguments.SelectedId</c>).</summary>
    public ObjectId? SelectedId { get; init; }

    public RevisionGridView RevisionGrid => revisionGrid;

    public Menu MainMenu => mainMenu;

    public TabControl Tabs => tabs;

    public DashboardView Dashboard => dashboard;

    /// <summary>
    ///  Shows another view model, e.g. for another repository (as <c>SetGitModule</c>); it is initialized at once if the window
    ///  is already shown.
    /// </summary>
    public void ShowViewModel(BrowseViewModel viewModel)
    {
        DataContext = viewModel;
        if (_isOpened)
        {
            viewModel.Initialize(selectedId: null);
        }
    }

    /// <summary>Reads the items of the recent and favourite repositories menus again.</summary>
    public void RefreshSubmenus()
    {
        if (_viewModel is null)
        {
            return;
        }

        foreach ((MenuItem item, BrowseSubmenu submenu) in _submenus)
        {
            item.ItemsSource = _viewModel.GetSubmenuItems(submenu).Select(CreateItem).ToList();
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        _viewModel = DataContext as BrowseViewModel;
        _submenus.Clear();
        _submenuOwners.Clear();
        if (_viewModel is null)
        {
            return;
        }

        // Another repository (SetGitModule) keeps the selected tab, as the tab control of FormBrowse.
        if (tabs.SelectedIndex >= 0)
        {
            _viewModel.SelectedTab = (BrowseTab)tabs.SelectedIndex;
        }

        mainMenu.ItemsSource = _viewModel.Menus.Select(CreateItem).ToList();
        pullButton.Flyout = CreateFlyout(_viewModel.PullItems);
        stashButton.Flyout = CreateFlyout(_viewModel.StashItems);
    }

    private MenuFlyout CreateFlyout(IReadOnlyList<BrowseMenuItem> items)
    {
        // The items are created at once: items added when a MenuFlyout opens are not shown.
        MenuFlyout flyout = new();
        foreach (Control item in items.Select(CreateItem))
        {
            flyout.Items.Add(item);
        }

        return flyout;
    }

    private Control CreateItem(BrowseMenuItem item)
    {
        if (item.IsSeparator)
        {
            return new Separator();
        }

        MenuItem menuItem = new() { Header = CreateHeader(item) };
        if (item.Icon is not null && SettingsIconConverter.Instance.Convert(item.Icon, typeof(object), null, System.Globalization.CultureInfo.InvariantCulture) is { } icon)
        {
            menuItem.Icon = new Image { Source = (global::Avalonia.Media.IImage)icon, Width = 16, Height = 16 };
        }

        if (item.ToolTip is { } toolTip)
        {
            ToolTip.SetTip(menuItem, toolTip);
        }

        if (item.Submenu is BrowseSubmenu submenu)
        {
            _submenus.Add((menuItem, submenu));
        }
        else if (item.Children is { } children)
        {
            menuItem.ItemsSource = children.Select(CreateItem).ToList();
            if (children.Any(c => c.Submenu is not null))
            {
                _submenuOwners.Add(menuItem);
            }
        }
        else if (item.Command is BrowseCommand command)
        {
            menuItem.Command = _viewModel!.RunCommand;
            menuItem.CommandParameter = command;
        }
        else if (item.Invoke is { } invoke)
        {
            menuItem.Click += (_, _) => invoke();
        }

        return menuItem;
    }

    // The header, with the text of ShortcutKeyDisplayString on the right (the branch of a recent repository).
    private static object CreateHeader(BrowseMenuItem item)
    {
        if (item.Shortcut is not { Length: > 0 } shortcut)
        {
            return item.Header;
        }

        Grid header = new() { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        header.Children.Add(new AccessText { Text = item.Header, VerticalAlignment = VerticalAlignment.Center });
        TextBlock shortcutText = new() { Text = shortcut, Opacity = 0.6, Margin = new global::Avalonia.Thickness(24, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(shortcutText, 1);
        header.Children.Add(shortcutText);
        return header;
    }
}
