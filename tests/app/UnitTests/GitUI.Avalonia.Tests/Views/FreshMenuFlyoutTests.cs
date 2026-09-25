using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitUI.Avalonia.Hosting;

namespace GitUI.AvaloniaTests.Views;

/// <summary>The menus whose items are read when they open (a <see cref="MenuFlyout"/> keeps the items it was first shown with).</summary>
[TestFixture]
public sealed class FreshMenuFlyoutTests : HeadlessTest
{
    [Test]
    public Task Each_click_shows_the_items_of_the_moment() => OnUiThreadAsync(() =>
    {
        List<string> headers = ["first"];
        Button button = new() { Content = "Menu" };
        MenuFlyout? shown;
        Window window = new() { Content = button, Width = 300, Height = 200 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        shown = FreshMenuFlyout.Show(button, [.. headers.Select(h => new MenuItem { Header = h })]);
        Dispatcher.UIThread.RunJobs();
        Shown(shown).Should().Equal("first");
        shown!.Hide();

        // Items changed since: shown as they are now.
        headers.Add("second");
        shown = FreshMenuFlyout.Show(button, [.. headers.Select(h => new MenuItem { Header = h })]);
        Dispatcher.UIThread.RunJobs();
        Shown(shown).Should().Equal("first", "second");
        shown!.Hide();

        FreshMenuFlyout.Show(button, []).Should().BeNull("no menu without items");

        // The click of a button shows one.
        int clicks = 0;
        FreshMenuFlyout.ShowOnClick(button, () =>
        {
            clicks++;
            return [new MenuItem { Header = "clicked" }];
        });
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        clicks.Should().Be(1);
        window.Close();
    });

    // The items shown in the menu: in the window of the menu (a MenuFlyout does not show the items added once it was shown).
    private static IEnumerable<string?> Shown(MenuFlyout? flyout)
        => flyout!.Items.OfType<MenuItem>().Where(item => TopLevel.GetTopLevel(item) is not null).Select(item => item.Header as string);
}
