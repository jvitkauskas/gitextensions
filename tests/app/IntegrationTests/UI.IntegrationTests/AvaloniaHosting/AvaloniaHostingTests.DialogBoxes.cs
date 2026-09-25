using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using GitExtensions.Extensibility;
using GitUI.Avalonia.Hosting;
using GitUI.AvaloniaHosting;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>
///  The message boxes and task dialogs of Avalonia, which off Windows replace the native ones (docs/avalonia-port/CROSS-PLATFORM.md,
///  phase 2), used here on Windows (<see cref="DialogBoxHost.UseOnWindows"/>): the synchronous calls return the button clicked.
/// </summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void Message_boxes_of_Avalonia_return_the_button_clicked()
    {
        UseAvaloniaDialogBoxes(() =>
        {
            DriveNextDialog(window => Click(((MessageBoxWindow)window).Buttons[1]));

            DialogResult result = MessageBoxes.Show(_owner, "Delete the branch?", "Delete branch", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            result.Should().Be(DialogResult.No);
        });
    }

    [Test]
    public void Task_dialogs_of_Avalonia_return_the_button_clicked_and_the_verification()
    {
        UseAvaloniaDialogBoxes(() =>
        {
            TaskDialogPage page = new() { Heading = "Uncommitted changes", Verification = new TaskDialogVerificationCheckBox { Text = "Don't ask again" } };
            TaskDialogCommandLinkButton stash = new("Stash");
            page.Buttons.Add(stash);
            page.Buttons.Add(TaskDialogButton.Cancel);
            DriveNextDialog(window =>
            {
                TaskDialogWindow dialog = (TaskDialogWindow)window;
                dialog.GetLogicalDescendants().OfType<CheckBox>().Single().IsChecked = true;
                Click(dialog.Buttons[0]);
            });

            TaskDialogButton result = TaskDialog.ShowDialog(_owner, page);

            result.Should().BeSameAs(stash);
            page.Verification.Checked.Should().BeTrue();
        });
    }

    [Test]
    public void A_message_box_of_Avalonia_can_be_shown_over_a_modal_dialog_and_nested()
    {
        UseAvaloniaDialogBoxes(() =>
        {
            DialogResult? inner = null;
            DriveDialogs(
                window =>
                {
                    // A synchronous message box from the handler of the first one: the result is there when the call returns.
                    inner = MessageBoxes.Show(new WindowOwner(window.NativeHandle).ToWin32Window(), "Nested?", "Nested", MessageBoxButtons.OKCancel, MessageBoxIcon.None);
                    Click(((MessageBoxWindow)window).Buttons[0]);
                },
                window => Click(((MessageBoxWindow)window).Buttons[1]));

            DialogResult outer = MessageBoxes.Show(_owner, "Outer?", "Outer", MessageBoxButtons.YesNo, MessageBoxIcon.None);

            inner.Should().Be(DialogResult.Cancel);
            outer.Should().Be(DialogResult.Yes);
        });
    }

    private static void UseAvaloniaDialogBoxes(Action test)
    {
        IDialogBoxHost? host = DialogBoxHost.Current;
        AvaloniaDialogBoxHost.Register();
        DialogBoxHost.UseOnWindows = true;
        try
        {
            test();
        }
        finally
        {
            DialogBoxHost.UseOnWindows = false;
            DialogBoxHost.Current = host;
        }
    }

    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
}
