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
    private readonly List<(MenuItem Item, BrowseSubmenu Submenu)> _modelSubmenus = [];
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

        // As userShell_Click: the button runs the default shell, its drop down the others.
        userShellButton.Click += (_, _) => _viewModel?.RunDefaultShell();

        // As MouseUpHandler of WorkingDirectoryToolStripSplitButton: a right click starts the "Open repository" dialog.
        workingDirButton.AddHandler(PointerReleasedEvent, (_, e) =>
        {
            if (e.InitialPressMouseButton == global::Avalonia.Input.MouseButton.Right)
            {
                _viewModel?.RunCommand.Execute(BrowseCommand.Open);
                e.Handled = true;
            }
        }, global::Avalonia.Interactivity.RoutingStrategies.Tunnel);

        // The Navigate and View menus show the settings of the grid: they are built again once a command ran.
        mainMenu.Closed += (_, _) => Dispatcher.UIThread.Post(RefreshModelSubmenus);

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
        _viewModel?.MenusChanged -= OnMenusChanged;
        _viewModel?.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel?.FocusRequested -= OnFocusRequested;
        _viewModel = DataContext as BrowseViewModel;
        _viewModel?.MenusChanged += OnMenusChanged;
        _viewModel?.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel?.FocusRequested += OnFocusRequested;
        if (_viewModel is null)
        {
            return;
        }

        // Another repository (SetGitModule) keeps the selected tab, as the tab control of FormBrowse.
        if (tabs.SelectedIndex >= 0)
        {
            _viewModel.SelectedTab = (BrowseTab)tabs.SelectedIndex;
        }

        BuildMainMenu();
        workingDirButton.Flyout = CreateFlyout(_viewModel.GetWorkingDirectoryItems());
        worktreesButton.Flyout = CreateFlyout(_viewModel.WorktreeItems);
        userShellButton.Flyout = CreateFlyout(_viewModel.ShellItems);
        FillScriptsToolBar(_viewModel.ScriptItems);
        pullButton.Flyout = CreateFlyout(_viewModel.PullItems);
        stashButton.Flyout = CreateFlyout(_viewModel.StashItems);
    }

    // As LoadUserMenu: a button with the icon and the name of each script.
    private void FillScriptsToolBar(IReadOnlyList<BrowseMenuItem> scripts)
    {
        scriptsToolBar.Children.Clear();
        foreach (BrowseMenuItem script in scripts)
        {
            StackPanel content = new() { Orientation = Orientation.Horizontal, Spacing = 4 };
            if (script.Icon is not null && SettingsIconConverter.Instance.Convert(script.Icon, typeof(object), null, System.Globalization.CultureInfo.InvariantCulture) is global::Avalonia.Media.IImage icon)
            {
                content.Children.Add(new Image { Source = icon, Width = 16, Height = 16 });
            }

            content.Children.Add(new TextBlock { Text = script.Header.Replace("__", "_"), VerticalAlignment = VerticalAlignment.Center });
            Button button = new() { Content = content };
            button.Classes.Add("toolbar");
            button.Click += (_, _) => script.Invoke?.Invoke();
            scriptsToolBar.Children.Add(button);
        }
    }

    // The worktrees are read in the background (UpdateWorktreeToolStripVisibility).
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BrowseViewModel.WorktreeItems) && _viewModel is not null)
        {
            worktreesButton.Flyout = CreateFlyout(_viewModel.WorktreeItems);
        }

        // E.g. the hotkeys that show a tab (FocusDiff, FocusNextTab).
        if (e.PropertyName == nameof(BrowseViewModel.SelectedTab) && _viewModel is not null && tabs.SelectedIndex != (int)_viewModel.SelectedTab)
        {
            tabs.SelectedIndex = (int)_viewModel.SelectedTab;
        }
    }

    // As FocusLeftPanel, RevisionGrid.Focus and ToolStripFilters.SetFocus of the hotkeys.
    private void OnFocusRequested(object? sender, BrowseFocusTarget target)
    {
        switch (target)
        {
            case BrowseFocusTarget.LeftPanel when leftPanel.IsVisible:
                leftPanel.Focus();
                break;
            case BrowseFocusTarget.RevisionGrid:
                revisionGrid.Focus();
                break;
            case BrowseFocusTarget.Filter:
                filterToolBar.FocusFilter();
                break;
        }
    }

    // As RegisterPlugins: the menus are built again, e.g. with the plugins once they are loaded.
    private void OnMenusChanged(object? sender, EventArgs e) => BuildMainMenu();

    private void BuildMainMenu()
    {
        _submenus.Clear();
        _submenuOwners.Clear();
        _modelSubmenus.Clear();
        mainMenu.ItemsSource = _viewModel!.Menus.Select(CreateItem).ToList();
        RefreshModelSubmenus();
    }

    private void RefreshModelSubmenus()
    {
        if (_viewModel is null)
        {
            return;
        }

        foreach ((MenuItem item, BrowseSubmenu submenu) in _modelSubmenus)
        {
            List<Control> items = MenuModelRenderer.CreateItems(_viewModel.GetModelSubmenuItems(submenu));
            item.ItemsSource = items;
            item.IsVisible = items.Count > 0;
        }
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

        MenuItem menuItem = new() { Header = CreateHeader(item), IsEnabled = item.IsEnabled };
        if (item.IsChecked is bool isChecked)
        {
            menuItem.ToggleType = MenuItemToggleType.CheckBox;
            menuItem.IsChecked = isChecked;
        }

        if (item.Icon is not null && SettingsIconConverter.Instance.Convert(item.Icon, typeof(object), null, System.Globalization.CultureInfo.InvariantCulture) is { } icon)
        {
            menuItem.Icon = new Image { Source = (global::Avalonia.Media.IImage)icon, Width = 16, Height = 16 };
        }

        if (item.ToolTip is { } toolTip)
        {
            ToolTip.SetTip(menuItem, toolTip);
        }

        if (item.Submenu is BrowseSubmenu.Navigate or BrowseSubmenu.View)
        {
            _modelSubmenus.Add((menuItem, item.Submenu.Value));
        }
        else if (item.Submenu is BrowseSubmenu submenu)
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
