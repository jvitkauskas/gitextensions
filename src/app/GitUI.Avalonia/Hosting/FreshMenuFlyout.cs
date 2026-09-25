using Avalonia.Controls;

namespace GitUI.Avalonia.Hosting;

/// <summary>
///  A menu whose items are read when it opens. A <see cref="MenuFlyout"/> keeps the items it had when first shown (the items
///  added later, also in its <c>Opening</c>, are not shown), so a new one is shown each time.
/// </summary>
public static class FreshMenuFlyout
{
    /// <summary>A click on <paramref name="button"/> shows a menu of the items <paramref name="getItems"/> returns then.</summary>
    public static void ShowOnClick(Button button, Func<IEnumerable<Control>> getItems, PlacementMode placement = PlacementMode.BottomEdgeAlignedLeft)
        => button.Click += (_, _) => Show(button, getItems(), placement);

    /// <summary>Shows a menu of <paramref name="items"/> at <paramref name="target"/>; none without items.</summary>
    /// <returns>The menu shown, if any (e.g. for tests).</returns>
    public static MenuFlyout? Show(Control target, IEnumerable<Control> items, PlacementMode placement = PlacementMode.BottomEdgeAlignedLeft)
    {
        MenuFlyout flyout = new() { Placement = placement };
        foreach (Control item in items)
        {
            flyout.Items.Add(item);
        }

        if (flyout.Items.Count == 0)
        {
            return null;
        }

        flyout.ShowAt(target);
        return flyout;
    }
}
