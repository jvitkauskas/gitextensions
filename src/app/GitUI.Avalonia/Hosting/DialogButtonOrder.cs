using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.LogicalTree;

namespace GitUI.Avalonia.Hosting;

/// <summary>
///  The order of the buttons of the footers of the dialogs (docs/avalonia-port/CROSS-PLATFORM.md, phase 5): on macOS the
///  default button is the rightmost one (Cancel, OK) where Windows has it first (OK, Cancel). The dialogs are written in
///  the order of Windows; on macOS a row of buttons aligned to the right in a <c>Border.dialogFooter</c> is reversed.
///  Rows with other controls (e.g. a check box before the buttons, a check box being a toggle button) keep their order.
/// </summary>
internal static class DialogButtonOrder
{
    /// <summary>Whether the rows of buttons are reversed: on macOS.</summary>
    public static bool IsReversed { get; } = OperatingSystem.IsMacOS();

    /// <summary>Reverses the rows of buttons of the footers of <paramref name="root"/>, if <paramref name="reverse"/>.</summary>
    public static void Apply(ILogical root, bool reverse)
    {
        if (!reverse)
        {
            return;
        }

        List<StackPanel> rows = [.. root.GetSelfAndLogicalDescendants()
            .OfType<Border>()
            .Where(border => border.Classes.Contains("dialogFooter"))
            .SelectMany(footer => footer.GetLogicalDescendants().OfType<StackPanel>())
            .Where(row => row.Orientation == Orientation.Horizontal
                && row.HorizontalAlignment == HorizontalAlignment.Right
                && row.Children.Count > 1
                && row.Children.All(child => child is Button and not ToggleButton))];
        foreach (StackPanel row in rows)
        {
            List<Control> buttons = [.. row.Children];
            buttons.Reverse();
            row.Children.Clear();
            row.Children.AddRange(buttons);
        }
    }
}
