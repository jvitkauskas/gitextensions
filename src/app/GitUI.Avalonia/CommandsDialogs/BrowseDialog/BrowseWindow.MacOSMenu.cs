using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using GitUI.Avalonia.CommandsDialogs.SettingsDialog;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Services;

namespace GitUI.Avalonia.CommandsDialogs.BrowseDialog;

/// <summary>
///  On macOS, the main menu in the menu bar of the system (a <see cref="NativeMenu"/> of the window, shown while it is the
///  key window, after the application menu) instead of in the window (docs/avalonia-port/CROSS-PLATFORM.md, phase 5). It is
///  built from the same <see cref="BrowseViewModel.Menus"/>; the submenus read when they open (the recent repositories, the
///  Navigate and View menus of the grid) are read when the system asks to update them, once they closed and after a
///  command. The menu of the window stays built, and is hidden once the platform shows the native menu (not in the
///  headless tests).
/// </summary>
/// <remarks>
///  Avalonia.Native fails when another menu instance is set on the window, and the system shows the items it had when
///  the menu opened: the items of a submenu are updated in place (their texts, check marks and actions), items are only
///  added or removed at the end.
/// </remarks>
public partial class BrowseWindow
{
    private readonly List<(NativeMenu Menu, BrowseSubmenu Submenu)> _macOSDynamicMenus = [];
    private readonly ConditionalWeakTable<NativeMenuItem, Action> _macOSActions = [];
    private NativeMenu? _macOSMenu;

    private void InitializeMacOSMenu()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        PropertyChanged += (_, e) =>
        {
            if (e.Property == NativeMenu.IsNativeMenuExportedProperty)
            {
                mainMenu.IsVisible = e.NewValue is not true;
                BuildMacOSMenu();
            }
        };
    }

    /// <summary>Builds the native menu again from the menus of the view model (as <see cref="BuildMainMenu"/>).</summary>
    private void BuildMacOSMenu()
    {
        if (!OperatingSystem.IsMacOS() || _viewModel is null)
        {
            return;
        }

        if (_macOSMenu is null)
        {
            _macOSMenu = new NativeMenu();
            NativeMenu.SetMenu(this, _macOSMenu);
        }

        // Filled once the platform shows it (not in the headless tests, where the menu of the window is used).
        if (GetValue(NativeMenu.IsNativeMenuExportedProperty) is not true)
        {
            return;
        }

        // The top level is built again while no menu is open (e.g. once the plugins are loaded).
        _macOSMenu.Items.Clear();
        _macOSDynamicMenus.Clear();
        foreach (BrowseMenuItem item in _viewModel.Menus)
        {
            _macOSMenu.Items.Add(CreateNativeItem(item));
        }
    }

    private NativeMenuItemBase CreateNativeItem(BrowseMenuItem item)
    {
        if (item.IsSeparator)
        {
            return new NativeMenuItemSeparator();
        }

        NativeMenuItem nativeItem = CreateNativeMenuItem(ToNativeHeader(item.Header, item.Shortcut), item.IsEnabled, item.IsChecked, item.ToolTip, item.Icon);
        if (item.Submenu is BrowseSubmenu submenu)
        {
            NativeMenu dynamicMenu = new();
            FillNativeSubmenu(dynamicMenu, submenu);
            dynamicMenu.NeedsUpdate += (_, _) => FillNativeSubmenu(dynamicMenu, submenu);
            dynamicMenu.Closed += (_, _) => Dispatcher.UIThread.Post(() => FillNativeSubmenu(dynamicMenu, submenu));
            _macOSDynamicMenus.Add((dynamicMenu, submenu));
            nativeItem.Menu = dynamicMenu;
        }
        else if (item.Children is { } children)
        {
            NativeMenu childMenu = new();
            foreach (BrowseMenuItem child in children)
            {
                childMenu.Items.Add(CreateNativeItem(child));
            }

            nativeItem.Menu = childMenu;
        }
        else if (item.Command is BrowseCommand command)
        {
            _macOSActions.AddOrUpdate(nativeItem, () => _viewModel?.RunCommand.Execute(command));
        }
        else if (item.Invoke is { } invoke)
        {
            _macOSActions.AddOrUpdate(nativeItem, invoke);
        }

        return nativeItem;
    }

    private NativeMenuItemBase CreateNativeItem(MenuModelItem item)
    {
        if (item.IsSeparator)
        {
            return new NativeMenuItemSeparator();
        }

        // No gesture: the hotkeys of the window run the commands; a key equivalent of the system would run the item of the
        // last time the menu was read.
        NativeMenuItem nativeItem = CreateNativeMenuItem(ToNativeHeader(item.Header, shortcut: null), item.IsEnabled, item.IsChecked, item.ToolTip, item.IsChecked is null ? item.Icon : null);
        if (item.Children is { } children)
        {
            NativeMenu childMenu = new();
            foreach (NativeMenuItemBase child in MenuModelItem.TrimSeparators(children).Select(CreateNativeItem))
            {
                childMenu.Items.Add(child);
            }

            nativeItem.Menu = childMenu;
        }
        else if (item.Execute is { } execute)
        {
            _macOSActions.AddOrUpdate(nativeItem, execute);
        }

        return nativeItem;
    }

    /// <summary>An item whose click runs its action of <see cref="_macOSActions"/> (replaced when the item is updated).</summary>
    private NativeMenuItem CreateNativeMenuItem(string header, bool isEnabled, bool? isChecked, string? toolTip, object? icon)
    {
        NativeMenuItem nativeItem = new(header) { IsEnabled = isEnabled, ToolTip = toolTip, Icon = ToBitmap(icon) };
        if (isChecked is bool check)
        {
            nativeItem.ToggleType = MenuItemToggleType.CheckBox;
            nativeItem.IsChecked = check;
        }

        nativeItem.Click += (_, _) =>
        {
            if (_macOSActions.TryGetValue(nativeItem, out Action? action))
            {
                action();

                // The submenus that show the state (the settings of the grid) are read again, as the menu of the window after
                // a command.
                Dispatcher.UIThread.Post(RefreshMacOSDynamicMenus);
            }
        };
        return nativeItem;
    }

    private void RefreshMacOSDynamicMenus()
    {
        foreach ((NativeMenu menu, BrowseSubmenu submenu) in _macOSDynamicMenus)
        {
            FillNativeSubmenu(menu, submenu);
        }
    }

    private void FillNativeSubmenu(NativeMenu menu, BrowseSubmenu submenu)
    {
        if (_viewModel is null)
        {
            return;
        }

        List<NativeMenuItemBase> items;
        if (submenu is BrowseSubmenu.Navigate or BrowseSubmenu.View)
        {
            // The settings of the grid, as RefreshModelSubmenus.
            items = [.. MenuModelItem.TrimSeparators(_viewModel.GetModelSubmenuItems(submenu)).Select(CreateNativeItem)];
            if (submenu == BrowseSubmenu.View)
            {
                if (items.Count > 0)
                {
                    items.Add(new NativeMenuItemSeparator());
                }

                items.Add(CreateNativeItem(_viewModel.ToolbarsMenu));
            }
        }
        else
        {
            items = [.. _viewModel.GetSubmenuItems(submenu).Select(CreateNativeItem)];
        }

        if (items.Count == 0)
        {
            // An empty submenu is not shown by the system.
            items.Add(new NativeMenuItem("-") { IsEnabled = false });
        }

        UpdateNativeMenu(menu, items);
    }

    /// <summary>Makes the items of <paramref name="menu"/> those of <paramref name="items"/>, keeping the existing item objects.</summary>
    private void UpdateNativeMenu(NativeMenu menu, IReadOnlyList<NativeMenuItemBase> items)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (i >= menu.Items.Count)
            {
                menu.Items.Add(items[i]);
            }
            else if (menu.Items[i] is NativeMenuItem existing && items[i] is NativeMenuItem fresh && (existing.Menu is null) == (fresh.Menu is null))
            {
                existing.Header = fresh.Header;
                existing.IsEnabled = fresh.IsEnabled;
                existing.ToolTip = fresh.ToolTip;
                existing.Icon = fresh.Icon;
                existing.ToggleType = fresh.ToggleType;
                existing.IsChecked = fresh.IsChecked;
                if (_macOSActions.TryGetValue(fresh, out Action? action))
                {
                    _macOSActions.AddOrUpdate(existing, action);
                }
                else
                {
                    _macOSActions.Remove(existing);
                }

                if (existing.Menu is { } existingMenu && fresh.Menu is { } freshMenu)
                {
                    // The fresh items leave their menu first: an item has one parent.
                    List<NativeMenuItemBase> children = [.. freshMenu.Items];
                    freshMenu.Items.Clear();
                    UpdateNativeMenu(existingMenu, children);
                }
            }
            else if (!(menu.Items[i] is NativeMenuItemSeparator && items[i] is NativeMenuItemSeparator))
            {
                menu.Items[i] = items[i];
            }
        }

        while (menu.Items.Count > items.Count)
        {
            menu.Items.RemoveAt(menu.Items.Count - 1);
        }
    }

    /// <summary>The text of an item: macOS menus have no access keys (<c>_</c>); the text shown on the right follows.</summary>
    internal static string ToNativeHeader(string header, string? shortcut)
    {
        string text = header.Replace("__", "\0").Replace("_", "").Replace('\0', '_');
        return shortcut is { Length: > 0 } ? $"{text}    {shortcut}" : text;
    }

    private static Bitmap? ToBitmap(object? icon)
        => icon is not null && SettingsIconConverter.Instance.Convert(icon, typeof(object), null, System.Globalization.CultureInfo.InvariantCulture) is Bitmap bitmap
            ? bitmap
            : null;
}
