using Avalonia.Controls;
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
    private BrowseViewModel? _viewModel;

    public BrowseWindow()
    {
        InitializeComponent();

        // As OnRuntimeLoad: the revisions are loaded once the window is shown.
        Opened += (_, _) => Dispatcher.UIThread.Post(() => _viewModel?.Initialize(SelectedId));
        tabs.SelectionChanged += (_, _) =>
        {
            if (_viewModel is not null && tabs.SelectedIndex >= 0)
            {
                _viewModel.SelectedTab = (BrowseTab)tabs.SelectedIndex;
            }
        };
    }

    /// <summary>The revision to select first (<c>BrowseArguments.SelectedId</c>).</summary>
    public ObjectId? SelectedId { get; init; }

    public RevisionGridView RevisionGrid => revisionGrid;

    public Menu MainMenu => mainMenu;

    public TabControl Tabs => tabs;

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        _viewModel = DataContext as BrowseViewModel;
        if (_viewModel is null)
        {
            return;
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

        MenuItem menuItem = new() { Header = item.Header };
        if (item.Icon is not null && SettingsIconConverter.Instance.Convert(item.Icon, typeof(object), null, System.Globalization.CultureInfo.InvariantCulture) is { } icon)
        {
            menuItem.Icon = new Image { Source = (global::Avalonia.Media.IImage)icon, Width = 16, Height = 16 };
        }

        if (item.Children is { } children)
        {
            menuItem.ItemsSource = children.Select(CreateItem).ToList();
        }
        else if (item.Command is BrowseCommand command)
        {
            menuItem.Command = _viewModel!.RunCommand;
            menuItem.CommandParameter = command;
        }

        return menuItem;
    }
}
