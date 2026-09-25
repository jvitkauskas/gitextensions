using Avalonia.Controls;
using Avalonia.Layout;
using GitUI.Avalonia.Hosting;

namespace GitUI.AvaloniaTests.Views;

/// <summary>The order of the buttons of the footers on macOS (docs/avalonia-port/CROSS-PLATFORM.md, phase 5).</summary>
[TestFixture]
public sealed class DialogButtonOrderTests : HeadlessTest
{
    [Test]
    public Task The_buttons_of_a_footer_are_reversed_on_macOS_only() => OnUiThreadAsync(() =>
    {
        (Border footer, StackPanel row) = CreateFooter(new Button { Name = "ok" }, new Button { Name = "cancel" });

        DialogButtonOrder.Apply(footer, reverse: false);
        row.Children.Select(c => c.Name).Should().Equal("ok", "cancel");

        DialogButtonOrder.Apply(footer, reverse: true);
        row.Children.Select(c => c.Name).Should().Equal("cancel", "ok");
    });

    [Test]
    public Task A_row_with_other_controls_keeps_its_order() => OnUiThreadAsync(() =>
    {
        (Border footer, StackPanel row) = CreateFooter(new CheckBox { Name = "keepOpen" }, new Button { Name = "ok" }, new Button { Name = "abort" });

        DialogButtonOrder.Apply(footer, reverse: true);

        row.Children.Select(c => c.Name).Should().Equal("keepOpen", "ok", "abort");
    });

    [Test]
    public Task The_rows_outside_a_footer_keep_their_order() => OnUiThreadAsync(() =>
    {
        StackPanel row = new() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        row.Children.Add(new Button { Name = "first" });
        row.Children.Add(new Button { Name = "second" });
        Border panel = new() { Child = row };

        DialogButtonOrder.Apply(panel, reverse: true);

        row.Children.Select(c => c.Name).Should().Equal("first", "second");
    });

    private static (Border Footer, StackPanel Row) CreateFooter(params Control[] children)
    {
        StackPanel row = new() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        row.Children.AddRange(children);
        Border footer = new() { Child = row };
        footer.Classes.Add("dialogFooter");
        return (footer, row);
    }
}
