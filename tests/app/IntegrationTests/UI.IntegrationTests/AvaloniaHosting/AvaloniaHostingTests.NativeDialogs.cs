using System.Runtime.InteropServices;
using GitExtensions.Extensibility;
using GitExtUtils.GitUI;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>
///  The native dialogs that replace those of WinForms (docs/avalonia-port/PLAN.md, phase 8): the task dialog, the message box
///  and the common dialogs, driven through their windows while they are shown.
/// </summary>
public sealed partial class AvaloniaHostingTests
{
    private const int TDM_CLICK_BUTTON = 0x0400 + 102;
    private const int TDM_CLICK_VERIFICATION = 0x0400 + 113;

    [Test]
    public void Task_dialog_raises_the_click_of_its_command_link_and_returns_it_with_the_verification()
    {
        TaskDialogCommandLinkButton continueButton = new("Continue", "The description of the link");
        TaskDialogButton disabledButton = new("Disabled", enabled: false);
        bool clicked = false;
        continueButton.Click += (_, _) => clicked = true;
        TaskDialogPage page = new()
        {
            Caption = "Task dialog test",
            Heading = "The heading",
            Text = "The text",
            Icon = TaskDialogIcon.Warning,
            Buttons = { continueButton, disabledButton, TaskDialogButton.Cancel },
            Verification = new TaskDialogVerificationCheckBox { Text = "Don't show again" },
            Expander = new TaskDialogExpander { Text = "Details", Position = TaskDialogExpanderPosition.AfterFootnote },
            Footnote = "The footnote",
            SizeToContent = true,
        };

        // The first custom button has the id 100.
        using UiTimer drive = new() { Interval = 300 };
        drive.Tick += (_, _) => SendToTopLevelWindow("Task dialog test", message =>
        {
            drive.Stop();
            message(TDM_CLICK_VERIFICATION, 1, 0);
            message(TDM_CLICK_BUTTON, 100, 0);
        });
        drive.Start();

        TaskDialogButton result = TaskDialog.ShowDialog(_owner, page);

        result.Should().BeSameAs(continueButton);
        clicked.Should().BeTrue("the click is raised before the dialog closes");
        page.Verification.Checked.Should().BeTrue();
    }

    [Test]
    public void Task_dialog_returns_the_standard_button_it_was_closed_with()
    {
        TaskDialogPage page = new()
        {
            Caption = "Task dialog test",
            Text = "Yes or no?",
            Buttons = { TaskDialogButton.Yes, TaskDialogButton.No },
        };

        using UiTimer drive = new() { Interval = 300 };
        drive.Tick += (_, _) => SendToTopLevelWindow("Task dialog test", message =>
        {
            drive.Stop();
            message(TDM_CLICK_BUTTON, 7 /* IDNO */, 0);
        });
        drive.Start();

        TaskDialog.ShowDialog(_owner, page).Should().Be(TaskDialogButton.No);
    }

    [Test]
    public void Message_box_returns_the_button_it_was_closed_with()
    {
        using UiTimer drive = new() { Interval = 300 };
        drive.Tick += (_, _) => SendToTopLevelWindow("Message box test", message =>
        {
            drive.Stop();
            message(0x0111 /* WM_COMMAND */, 7 /* IDNO */, 0);
        });
        drive.Start();

        MessageBoxes.Show(_owner, "Yes or no?", "Message box test", MessageBoxButtons.YesNo, MessageBoxIcon.Question).Should().Be(DialogResult.No);
    }

    [TestCase("open")]
    [TestCase("save")]
    [TestCase("folder")]
    public void File_dialogs_are_cancelled_when_closed(string kind)
    {
        using UiTimer drive = new() { Interval = 500 };
        drive.Tick += (_, _) => SendToTopLevelWindow("File dialog test", message =>
        {
            drive.Stop();
            message(0x0010 /* WM_CLOSE */, 0, 0);
        });
        drive.Start();

        DialogResult result = kind switch
        {
            "open" => new OpenFileDialog { Title = "File dialog test", Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*", InitialDirectory = _referenceRepository.Module.WorkingDir }.ShowDialog(_owner),
            "save" => new SaveFileDialog { Title = "File dialog test", Filter = "Patch (*.patch)|*.patch", DefaultExt = ".patch", FileName = "my.patch" }.ShowDialog(_owner),
            _ => new FolderBrowserDialog { Description = "File dialog test", InitialDirectory = _referenceRepository.Module.WorkingDir }.ShowDialog(_owner),
        };

        result.Should().Be(DialogResult.Cancel);
    }

    [TestCase("Color")]
    [TestCase("Font")]
    public void Color_and_font_dialogs_are_cancelled_when_closed(string title)
    {
        // The titles of the dialogs of Windows (in English).
        using UiTimer drive = new() { Interval = 500 };
        drive.Tick += (_, _) => SendToTopLevelWindow(title, message =>
        {
            drive.Stop();
            message(0x0111 /* WM_COMMAND */, 2 /* IDCANCEL */, 0);
        });
        drive.Start();

        DialogResult result = title == "Color"
            ? new ColorDialog { Color = Color.Red }.ShowDialog(_owner)
            : new FontDialog { Font = new Font("Consolas", 10), FixedPitchOnly = true }.ShowDialog(_owner);

        result.Should().Be(DialogResult.Cancel);
    }

    [Test]
    public void Color_dialog_returns_the_chosen_color()
    {
        using UiTimer drive = new() { Interval = 500 };
        drive.Tick += (_, _) => SendToTopLevelWindow("Color", message =>
        {
            drive.Stop();
            message(0x0111 /* WM_COMMAND */, 1 /* IDOK */, 0);
        });
        drive.Start();

        ColorDialog dialog = new() { Color = Color.FromArgb(0x12, 0x34, 0x56), FullOpen = true };

        dialog.ShowDialog(_owner).Should().Be(DialogResult.OK);
        dialog.Color.ToArgb().Should().Be(Color.FromArgb(0x12, 0x34, 0x56).ToArgb(), "the initial color is accepted");
    }

    [Test]
    public void Bug_report_shows_the_exception_and_quits()
    {
        BugReporter.Serialization.SerializableException exception = new(new InvalidOperationException("Something went wrong", new ArgumentException("The cause")));
        DriveNextDialog(window =>
        {
            GitUI.Presentation.BugReporter.BugReportViewModel viewModel = (GitUI.Presentation.BugReporter.BugReportViewModel)window.DataContext!;
            viewModel.Info.ExceptionType.Should().Be(typeof(InvalidOperationException).FullName);
            viewModel.Info.ExceptionMessage.Should().Be("Something went wrong");
            viewModel.Exceptions.Should().ContainSingle().Which.InnerExceptions.Should().ContainSingle().Which.Type.Should().Be(typeof(ArgumentException).FullName);
            viewModel.CanClose().Should().BeFalse("a terminating application closes with the buttons only");
            Capture(window, "bug-report");
            viewModel.QuitCommand.Execute(null);
        });

        BugReporter.BugReportDialog.Show(_owner, GitUI.AvaloniaHosting.AvaloniaDialogs.GetOptions, exception, exceptionInfo: "", environmentInfo: "environment",
            canIgnore: false, showIgnore: false, focusDetails: false)
            .Should().Be(GitUI.Presentation.BugReporter.BugReportResult.Quit);
    }

    [Test]
    public void File_dialog_filter_must_be_pairs_of_names_and_patterns()
    {
        ((Action)(() => new OpenFileDialog { Filter = "odd" }.ShowDialog(_owner))).Should().Throw<ArgumentException>();
    }

    /// <summary>Sends messages to the top-level window of this thread with <paramref name="title"/>, if it is shown.</summary>
    private static void SendToTopLevelWindow(string title, Action<Action<int, nint, nint>> send)
    {
        nint found = 0;
        EnumThreadWindows(
            GetCurrentThreadId(),
            (handle, _) =>
            {
                System.Text.StringBuilder text = new(256);
                GetWindowText(handle, text, text.Capacity);
                if (text.ToString() == title)
                {
                    found = handle;
                    return false;
                }

                return true;
            },
            0);

        if (found != 0)
        {
            send((message, wordParameter, longParameter) => SendMessageW(found, message, wordParameter, longParameter));
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint SendMessageW(nint window, int message, nint wordParameter, nint longParameter);
}
