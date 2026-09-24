namespace GitUI.Presentation.Services;

/// <summary>
///  An item of a menu built by the application (e.g. the context menu of the revision grid, as its WinForms
///  <c>ContextMenuOpening</c>), shown by any view: a command, a submenu, or a separator.
/// </summary>
/// <param name="Header">The text, with its access key as <c>_</c>.</param>
/// <param name="Execute">What the item does; null for a submenu or a separator.</param>
/// <param name="Icon">The image: the name of an asset of GitUI.Avalonia, or PNG data.</param>
/// <param name="Children">The items of a submenu.</param>
/// <param name="IsChecked">Whether the item is checked; null if it is not a check item.</param>
/// <param name="Gesture">The text of its shortcut, e.g. "Ctrl+Shift+C".</param>
/// <param name="ToolTip">The tooltip, if any.</param>
public sealed record MenuModelItem(
    string Header,
    Action? Execute = null,
    object? Icon = null,
    IReadOnlyList<MenuModelItem>? Children = null,
    bool? IsChecked = null,
    bool IsEnabled = true,
    bool IsBold = false,
    string? Gesture = null,
    string? ToolTip = null)
{
    public static MenuModelItem Separator { get; } = new("-");

    public bool IsSeparator => Execute is null && Children is null && Header == "-";

    /// <summary>
    ///  The items without the separators at the start, at the end or next to another separator (as the separators hidden
    ///  by <c>UpdateSeparators</c>).
    /// </summary>
    public static IReadOnlyList<MenuModelItem> TrimSeparators(IEnumerable<MenuModelItem> items)
    {
        List<MenuModelItem> result = [];
        foreach (MenuModelItem item in items)
        {
            if (item.IsSeparator && (result.Count == 0 || result[^1].IsSeparator))
            {
                continue;
            }

            result.Add(item);
        }

        while (result.Count > 0 && result[^1].IsSeparator)
        {
            result.RemoveAt(result.Count - 1);
        }

        return result;
    }
}
