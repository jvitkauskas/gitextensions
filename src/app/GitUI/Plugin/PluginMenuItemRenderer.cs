using GitExtensions.Extensibility.Plugins;

namespace GitUI;

/// <summary>Shows the menu items of the plugins (<see cref="PluginMenuItem"/>, plugin API v2) as WinForms menu items.</summary>
public static class PluginMenuItemRenderer
{
    /// <summary>The tag of the items added by <see cref="ReplaceItems"/>.</summary>
    private static readonly object _pluginItemTag = new();

    /// <summary>The WinForms item of <paramref name="item"/>, with its submenu.</summary>
    public static ToolStripItem CreateItem(PluginMenuItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.IsSeparator)
        {
            return new ToolStripSeparator();
        }

        ToolStripMenuItem menuItem = new(item.Text, item.Icon) { Enabled = item.IsEnabled };
        foreach (PluginMenuItem child in item.Children)
        {
            menuItem.DropDownItems.Add(CreateItem(child));
        }

        if (item.OnClick is not null)
        {
            menuItem.Click += (_, _) => item.Click();
        }

        return menuItem;
    }

    /// <summary>
    ///  Replaces the items of the plugins previously added to <paramref name="menu"/> by this method with
    ///  <paramref name="items"/>, at the end of the menu.
    /// </summary>
    public static void ReplaceItems(ToolStrip menu, IEnumerable<PluginMenuItem> items)
    {
        ArgumentNullException.ThrowIfNull(menu);
        ArgumentNullException.ThrowIfNull(items);
        for (int i = menu.Items.Count - 1; i >= 0; i--)
        {
            ToolStripItem existing = menu.Items[i];
            if (existing.Tag == _pluginItemTag)
            {
                // Not disposed: that could dispose the icon, which the plugin keeps.
                menu.Items.RemoveAt(i);
            }
        }

        foreach (PluginMenuItem item in items)
        {
            ToolStripItem toolStripItem = CreateItem(item);
            toolStripItem.Tag = _pluginItemTag;
            menu.Items.Add(toolStripItem);
        }
    }
}
