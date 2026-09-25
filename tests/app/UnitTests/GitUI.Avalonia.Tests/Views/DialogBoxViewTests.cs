using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GitExtensions.Extensibility;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.HelperDialogs;

namespace GitUI.AvaloniaTests.Views;

/// <summary>
///  The message boxes and task dialogs of Avalonia, shown off Windows instead of the native ones
///  (docs/avalonia-port/CROSS-PLATFORM.md, phase 2).
/// </summary>
[TestFixture]
public sealed class DialogBoxViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        Capture(new MessageBoxWindow("The branch 'feature' is not fully merged.\nDelete it anyway?", "Delete branch", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2, new DialogBoxStrings()), $"messagebox-{theme}");
        Capture(new MessageBoxWindow("The operation failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, new DialogBoxStrings()), $"messagebox-error-{theme}");

        TaskDialogPage page = new()
        {
            Caption = "Git Extensions",
            Heading = "There are uncommitted changes",
            Text = "Checking out another branch discards them. See <a href=\"https://git-scm.com\">the manual</a>.",
            Icon = TaskDialogIcon.Warning,
            EnableLinks = true,
            AllowCancel = true,
            Footnote = "This can be changed in the settings.",
            Verification = new TaskDialogVerificationCheckBox { Text = "&Don't ask again" },
            Expander = new TaskDialogExpander { Text = "M file.txt\nA other.txt", Expanded = true },
        };
        page.Buttons.Add(new TaskDialogCommandLinkButton("&Stash the changes", "They can be applied again later."));
        page.Buttons.Add(new TaskDialogCommandLinkButton("&Reset the changes", "They are lost."));
        page.Buttons.Add(TaskDialogButton.Cancel);
        Capture(new TaskDialogWindow(page, new DialogBoxStrings()), $"taskdialog-{theme}");
    });

    [Test]
    public Task MessageBox_has_the_buttons_of_the_call_in_order_with_the_default_button() => OnUiThreadAsync(() =>
    {
        MessageBoxWindow window = Show(new MessageBoxWindow("Text", "Caption", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2, new DialogBoxStrings()));

        window.Buttons.Select(b => b.Content).Should().Equal("_Yes", "_No", "Cancel");
        window.DefaultButton.Should().BeSameAs(window.Buttons[1]);
        window.Buttons[1].IsDefault.Should().BeTrue();
        window.GetVisualDescendants().OfType<Grid>().FirstOrDefault(g => g.Name == "iconQuestion").Should().NotBeNull();
        window.Close();
    });

    [TestCase(MessageBoxButtons.OK, DialogResult.OK)]
    [TestCase(MessageBoxButtons.OKCancel, DialogResult.Cancel)]
    [TestCase(MessageBoxButtons.YesNoCancel, DialogResult.Cancel)]
    public Task MessageBox_Escape_gives_Cancel_or_the_only_button(MessageBoxButtons buttons, DialogResult expected) => OnUiThreadAsync(() =>
    {
        MessageBoxWindow window = Show(new MessageBoxWindow("Text", "Caption", buttons, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, new DialogBoxStrings()));
        bool closed = false;
        window.Closed += (_, _) => closed = true;

        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        closed.Should().BeTrue();
        window.Result.Should().Be(expected);
    });

    [Test]
    public Task MessageBox_without_Cancel_cannot_be_closed_without_a_button() => OnUiThreadAsync(() =>
    {
        MessageBoxWindow window = Show(new MessageBoxWindow("Text", "Caption", MessageBoxButtons.YesNo, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, new DialogBoxStrings()));
        bool closed = false;
        window.Closed += (_, _) => closed = true;

        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        window.Close();
        Dispatcher.UIThread.RunJobs();
        closed.Should().BeFalse();

        Click(window.Buttons[1]);
        closed.Should().BeTrue();
        window.Result.Should().Be(DialogResult.No);
    });

    [Test]
    public Task MessageBox_copies_its_text_as_the_message_box_of_Windows() => OnUiThreadAsync(() =>
    {
        MessageBoxWindow window = Show(new MessageBoxWindow("Text", "Caption", MessageBoxButtons.OKCancel, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, new DialogBoxStrings()));

        window.CopyText.Should().Be(string.Join(Environment.NewLine, "---------------------------", "Caption", "---------------------------", "Text", "---------------------------", "OK   Cancel", "---------------------------", ""));
        window.Close();
    });

    [Test]
    public Task TaskDialog_button_runs_its_handlers_and_closes_unless_it_does_not_allow_it() => OnUiThreadAsync(() =>
    {
        TaskDialogPage page = new() { Heading = "Heading" };
        TaskDialogButton stay = new("Stay", allowCloseDialog: false);
        TaskDialogCommandLinkButton go = new("Go", "Description");
        int stayClicks = 0;
        stay.Click += (_, _) => stayClicks++;
        page.Buttons.Add(go);
        page.Buttons.Add(stay);
        page.Buttons.Add(TaskDialogButton.OK);
        TaskDialogWindow window = Show(new TaskDialogWindow(page, new DialogBoxStrings()));
        bool closed = false;
        window.Closed += (_, _) => closed = true;

        window.Buttons.Select(b => b.Tag).Should().Equal(go, stay, TaskDialogButton.OK);
        window.Buttons[2].Content.Should().Be("OK");

        Click(window.Buttons[1]);
        stayClicks.Should().Be(1);
        closed.Should().BeFalse();

        Click(window.Buttons[0]);
        closed.Should().BeTrue();
        window.Result.Should().BeSameAs(go);
    });

    [Test]
    public Task TaskDialog_disabled_and_default_buttons() => OnUiThreadAsync(() =>
    {
        TaskDialogPage page = new();
        TaskDialogButton disabled = new("Disabled", enabled: false);
        page.Buttons.Add(disabled);
        page.Buttons.Add(TaskDialogButton.Yes);
        page.Buttons.Add(TaskDialogButton.No);
        page.DefaultButton = TaskDialogButton.No;
        TaskDialogWindow window = Show(new TaskDialogWindow(page, new DialogBoxStrings()));

        window.Buttons[0].IsEnabled.Should().BeFalse();
        window.DefaultButton.Should().BeSameAs(window.Buttons[2]);
        window.Close();
    });

    [TestCase(true, false)]
    [TestCase(false, true)]
    public Task TaskDialog_Escape_cancels_with_AllowCancel_or_a_Cancel_button(bool allowCancel, bool cancelButton) => OnUiThreadAsync(() =>
    {
        TaskDialogPage page = new() { AllowCancel = allowCancel };
        page.Buttons.Add(TaskDialogButton.OK);
        if (cancelButton)
        {
            page.Buttons.Add(TaskDialogButton.Cancel);
        }

        TaskDialogWindow window = Show(new TaskDialogWindow(page, new DialogBoxStrings()));

        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        window.IsVisible.Should().BeFalse();
        (window.Result == TaskDialogButton.Cancel).Should().BeTrue();
    });

    [Test]
    public Task TaskDialog_without_cancel_cannot_be_closed_without_a_button() => OnUiThreadAsync(() =>
    {
        TaskDialogPage page = new();
        page.Buttons.Add(TaskDialogButton.Yes);
        TaskDialogWindow window = Show(new TaskDialogWindow(page, new DialogBoxStrings()));

        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        window.Close();
        Dispatcher.UIThread.RunJobs();

        window.IsVisible.Should().BeTrue();
        Click(window.Buttons[0]);
        window.IsVisible.Should().BeFalse();
    });

    [Test]
    public Task TaskDialog_verification_and_expander_are_written_to_the_page() => OnUiThreadAsync(() =>
    {
        TaskDialogPage page = new()
        {
            Text = "Text",
            Verification = new TaskDialogVerificationCheckBox { Text = "Don't ask again" },
            Expander = new TaskDialogExpander { Text = "Details", CollapsedButtonText = "More", ExpandedButtonText = "Less" },
        };
        page.Buttons.Add(TaskDialogButton.OK);
        TaskDialogWindow window = Show(new TaskDialogWindow(page, new DialogBoxStrings()));
        CheckBox verification = window.GetVisualDescendants().OfType<CheckBox>().FirstOrDefault(c => c.Name == "verification")!;
        Button expander = window.GetVisualDescendants().OfType<Button>().FirstOrDefault(t => t.Name == "expanderButton")!;
        Control expandedText = window.GetVisualDescendants().OfType<Control>().FirstOrDefault(c => c.Name == "expandedText")!;

        expandedText.IsVisible.Should().BeFalse();
        expander.Content.Should().Be("▾  More");

        verification.IsChecked = true;
        Click(expander);

        page.Verification.Checked.Should().BeTrue();
        page.Expander.Expanded.Should().BeTrue();
        expandedText.IsVisible.Should().BeTrue();
        expander.Content.Should().Be("▴  Less");
        window.Close();
    });

    [Test]
    public Task TaskDialog_links_raise_LinkClicked() => OnUiThreadAsync(() =>
    {
        TaskDialogPage page = new() { Text = "Read <a href=\"https://example.org/doc\">the documentation</a> first.", EnableLinks = true };
        page.Buttons.Add(TaskDialogButton.OK);
        string? clicked = null;
        page.LinkClicked += (_, e) => clicked = e.LinkHref;
        TaskDialogWindow window = Show(new TaskDialogWindow(page, new DialogBoxStrings()));

        HyperlinkButton link = window.GetVisualDescendants().OfType<HyperlinkButton>().Single();
        link.Content.Should().Be("the documentation");
        link.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        clicked.Should().Be("https://example.org/doc");
        window.Close();
    });

    [Test]
    public Task TaskDialog_without_EnableLinks_shows_the_markup_as_text() => OnUiThreadAsync(() =>
    {
        TaskDialogPage page = new() { Text = "<a href=\"x\">y</a>" };
        page.Buttons.Add(TaskDialogButton.OK);
        TaskDialogWindow window = Show(new TaskDialogWindow(page, new DialogBoxStrings()));

        window.GetVisualDescendants().OfType<HyperlinkButton>().Should().BeEmpty();
        window.GetVisualDescendants().OfType<TextBlock>().Should().Contain(t => t.Text == "<a href=\"x\">y</a>");
        window.Close();
    });

    private static void Click(Button button)
    {
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
    }

    private static T Show<T>(T window)
        where T : DialogWindow
    {
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static void Capture(DialogWindow window, string name)
    {
        Show(window);
        SaveScreenshot(window.CaptureRenderedFrame(), name);
        window.Close();
    }
}
