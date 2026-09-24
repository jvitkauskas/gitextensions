using Avalonia.VisualTree;
using GitExtensions.Extensibility;
using GitUI.Avalonia.CommandsDialogs.BrowseDialog;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>The text input of a modeless Avalonia window in the WinForms message loop (<c>AvaloniaKeyboardMessageFilter</c>).</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void A_modeless_Avalonia_window_gets_the_characters_of_its_key_presses()
    {
        string? typed = null;
        bool closed = false;
        DriveNextDialog(window =>
        {
            window.Closed += (_, _) => closed = true;
            BrowseViewModel viewModel = (BrowseViewModel)window.DataContext!;
            WaitUntil(
                () => viewModel.Grid.Rows.Count > 0,
                () =>
                {
                    ((BrowseWindow)window).Activate();
                    global::Avalonia.Controls.ComboBox filterBox = window.GetVisualDescendants().OfType<global::Avalonia.Controls.ComboBox>().Single(c => c.Name == "revisionFilterBox");
                    filterBox.Focus();

                    // As a real key press, through the message loop of WinForms (Application.Run, here DoEvents of the test).
                    nint handle = window.TryGetPlatformHandle()!.Handle;
                    PostMessage(handle, WM_KEYDOWN, 'N', 0x00310001);
                    PostMessage(handle, WM_KEYUP, 'N', unchecked((nint)0xC0310001));
                    WaitUntil(
                        () => viewModel.Filters!.RevisionFilter.Length > 0,
                        () =>
                        {
                            typed = viewModel.Filters!.RevisionFilter;
                            window.Close();
                        });
                });
        });

        _commands.StartBrowseDialog(_owner, new BrowseArguments()).Should().BeTrue();
        DateTime deadline = DateTime.UtcNow.AddSeconds(40);
        while (!closed && DateTime.UtcNow < deadline)
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }

        closed.Should().BeTrue();
        _driveFailure.Should().BeNull();
        typed.Should().BeEquivalentTo("n", "the key press is translated to its character, not taken by IsDialogMessage");
    }
}
