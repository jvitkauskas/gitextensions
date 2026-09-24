using System.Globalization;
using Avalonia.Controls;
using Avalonia.Media;
using GitUI.Avalonia.CommandsDialogs.SettingsDialog;
using GitUI.Presentation.Services;

namespace GitUI.Avalonia.Hosting;

/// <summary>Shows a <see cref="MenuModelItem"/> menu as Avalonia menu items.</summary>
public static class MenuModelRenderer
{
    /// <summary>The menu items of <paramref name="items"/>, with their submenus.</summary>
    public static List<Control> CreateItems(IEnumerable<MenuModelItem> items)
        => [.. MenuModelItem.TrimSeparators(items).Select(CreateItem)];

    /// <summary>A context menu with <paramref name="items"/>, filled before it opens (items added when opening are not shown).</summary>
    public static ContextMenu CreateContextMenu(IEnumerable<MenuModelItem> items)
    {
        ContextMenu menu = new();
        foreach (Control item in CreateItems(items))
        {
            menu.Items.Add(item);
        }

        return menu;
    }

    private static Control CreateItem(MenuModelItem item)
    {
        if (item.IsSeparator)
        {
            return new Separator();
        }

        MenuItem menuItem = new()
        {
            Header = item.Header,
            IsEnabled = item.IsEnabled,
            InputGesture = item.Gesture is not null && TryParseGesture(item.Gesture) is { } gesture ? gesture : null,
        };
        if (item.IsBold)
        {
            menuItem.FontWeight = FontWeight.Bold;
        }

        if (item.ToolTip is not null)
        {
            ToolTip.SetTip(menuItem, item.ToolTip);
        }

        if (item.IsChecked is bool isChecked)
        {
            menuItem.ToggleType = MenuItemToggleType.CheckBox;
            menuItem.IsChecked = isChecked;
        }
        else if (item.Icon is not null && SettingsIconConverter.Instance.Convert(item.Icon, typeof(object), null, CultureInfo.InvariantCulture) is IImage icon)
        {
            menuItem.Icon = new Image { Source = icon, Width = 16, Height = 16 };
        }

        if (item.Children is { } children)
        {
            menuItem.ItemsSource = CreateItems(children);
        }
        else if (item.Execute is { } execute)
        {
            menuItem.Click += (_, _) => execute();
        }

        return menuItem;
    }

    private static global::Avalonia.Input.KeyGesture? TryParseGesture(string text)
    {
        try
        {
            return global::Avalonia.Input.KeyGesture.Parse(text);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
