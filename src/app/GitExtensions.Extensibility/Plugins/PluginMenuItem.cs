namespace GitExtensions.Extensibility.Plugins;

/// <summary>
///  An item a plugin adds to a menu of the application (plugin API v2): a model the host renders in its UI framework (a
///  WinForms <c>ToolStripMenuItem</c>, an Avalonia <c>MenuItem</c>), instead of the plugin changing a WinForms menu.
/// </summary>
public sealed class PluginMenuItem
{
    /// <param name="text">The text of the item; an ampersand marks its access key.</param>
    /// <param name="onClick">Runs when the item is clicked, on the UI thread; none for an item that only opens <paramref name="children"/>.</param>
    /// <param name="icon">The image of the item, if any.</param>
    /// <param name="children">The items of the submenu of the item, if any.</param>
    public PluginMenuItem(string text, Action? onClick = null, Image? icon = null, IReadOnlyList<PluginMenuItem>? children = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        Text = text;
        OnClick = onClick;
        Icon = icon;
        Children = children ?? [];
    }

    private PluginMenuItem()
    {
        Text = "-";
        Children = [];
        IsSeparator = true;
    }

    /// <summary>A separator line between items.</summary>
    public static PluginMenuItem Separator { get; } = new();

    public string Text { get; }

    public Action? OnClick { get; }

    /// <summary>The image of the item as a GDI+ image, which is only shown on Windows; see <see cref="IconImage"/>.</summary>
    public Image? Icon { get; }

    /// <summary>The image of the item on every system (plugin API v3); the host shows it rather than <see cref="Icon"/>.</summary>
    public PluginImage? IconImage { get; init; }

    /// <summary>The items of the submenu of the item; empty for none.</summary>
    public IReadOnlyList<PluginMenuItem> Children { get; }

    /// <summary>Whether the item can be clicked (<see langword="true"/> by default).</summary>
    public bool IsEnabled { get; init; } = true;

    public bool IsSeparator { get; }

    /// <summary>Runs <see cref="OnClick"/>, if the item is enabled.</summary>
    public void Click()
    {
        if (IsEnabled)
        {
            OnClick?.Invoke();
        }
    }
}
