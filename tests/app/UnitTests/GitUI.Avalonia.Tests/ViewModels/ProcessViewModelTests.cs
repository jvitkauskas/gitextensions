using GitUI.Presentation.HelperDialogs;
using GitUI.Presentation.Services;

namespace GitUI.AvaloniaTests.ViewModels;

[TestFixture]
public sealed class ProcessViewModelTests
{
    private FakeConsole _console = null!;
    private FakeHost _host = null!;
    private FakeMessageBoxes _messageBoxes = null!;

    [SetUp]
    public void SetUp()
    {
        _console = new FakeConsole();
        _host = new FakeHost();
        _messageBoxes = new FakeMessageBoxes();
    }

    [Test]
    public void Title_contains_the_working_directory()
    {
        CreateViewModel(displayPath: "~/repo").Title.Should().Be("Process (~/repo)");
        CreateViewModel(displayPath: "").Title.Should().Be("Process");
    }

    [Test]
    public void Start_runs_the_process_and_shows_progress()
    {
        ProcessViewModel viewModel = CreateViewModel();

        viewModel.Start();

        _console.Started.Should().BeTrue();
        viewModel.Status.Should().Be(ProcessStatus.Running);
        viewModel.IsProgressVisible.Should().BeTrue();
        viewModel.ProgressValue.Should().BeNull("progress is indeterminate until git reports a percentage");
        viewModel.AbortCommand.CanExecute(null).Should().BeTrue();
        viewModel.AcknowledgeCommand.CanExecute(null).Should().BeFalse();
        _host.TaskbarStates.Should().Contain(TaskbarProgressState.Indeterminate);
    }

    [Test]
    public void Progress_lines_update_progress_and_the_title_of_a_plain_text_console()
    {
        ProcessViewModel viewModel = CreateViewModel();
        viewModel.Start();

        _console.Emit("Receiving objects:  42% (42/100)\r");

        viewModel.ProgressValue.Should().Be(42);
        viewModel.Title.Should().Be("Receiving objects:  42% (42/100)");
        viewModel.Output.Should().BeEmpty("progress messages are not logged");
    }

    [Test]
    public void Progress_does_not_replace_the_title_when_a_terminal_shows_it()
    {
        _console.IsPlainText = false;
        ProcessViewModel viewModel = CreateViewModel(displayPath: "repo");
        viewModel.Start();

        _console.Emit("Receiving objects:  42% (42/100)\r");

        viewModel.ProgressValue.Should().Be(42);
        viewModel.Title.Should().Be("Process (repo)");
    }

    [Test]
    public void The_dialog_shows_the_text_of_a_plain_text_console_and_clears_it_on_reset()
    {
        ProcessViewModel viewModel = CreateViewModel();
        viewModel.Start();

        viewModel.IsPlainText.Should().BeTrue();
        viewModel.ConsoleView.Should().BeNull("the plain text console has no window");
        _console.Emit("first line\n");
        _console.Emit("second line\n");

        viewModel.PlainText.Should().Be("first line\nsecond line\n");

        viewModel.Reset();
        viewModel.PlainText.Should().BeEmpty();
    }

    [Test]
    public void Output_is_logged_and_written_to_a_plain_text_console()
    {
        ProcessViewModel viewModel = CreateViewModel();
        viewModel.Start();

        _console.Emit("Switched to branch 'main'\u001B[K\n");

        viewModel.Output.Should().Be("Switched to branch 'main'" + Environment.NewLine);
        _console.Written.Should().Equal("Switched to branch 'main'\n");
    }

    [Test]
    public void A_prompt_shows_the_password_input()
    {
        ProcessViewModel viewModel = CreateViewModel();
        bool inputRequested = false;
        viewModel.InputRequested += (_, _) => inputRequested = true;
        viewModel.Start();
        viewModel.IsPasswordInputVisible.Should().BeFalse();

        _console.Emit("Password for 'https://example.org': ");

        viewModel.ShowPasswordState.Should().BeNull("the input is shown automatically (indeterminate)");
        viewModel.IsPasswordInputVisible.Should().BeTrue();
        inputRequested.Should().BeTrue();
    }

    [Test]
    public void SendInput_writes_the_password_to_the_process()
    {
        ProcessViewModel viewModel = CreateViewModel();
        viewModel.Start();

        viewModel.PasswordText = "secret";
        viewModel.SendInputCommand.Execute(null);

        _console.Input.Should().Equal("secret\n");
        viewModel.PasswordText.Should().BeEmpty();
    }

    [Test]
    public void Success_closes_automatically_unless_kept_open()
    {
        _host.CloseProcessDialog = true;
        ProcessViewModel viewModel = CreateViewModel();
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;
        viewModel.Start();

        _console.Exit(0);

        viewModel.Status.Should().Be(ProcessStatus.Succeeded);
        viewModel.ErrorOccurred.Should().BeFalse();
        closed.Should().BeTrue();
        _host.RecordedHistory.Should().ContainSingle();
    }

    [Test]
    public void Success_stays_open_when_kept_open()
    {
        _host.CloseProcessDialog = false;
        ProcessViewModel viewModel = CreateViewModel();
        bool closeRequested = false;
        viewModel.CloseRequested += (_, _) => closeRequested = true;
        viewModel.Start();

        _console.Exit(0);

        closeRequested.Should().BeFalse();
        viewModel.IsDone.Should().BeTrue();
        viewModel.AcknowledgeCommand.CanExecute(null).Should().BeTrue();
        viewModel.AbortCommand.CanExecute(null).Should().BeFalse();
        viewModel.IsProgressVisible.Should().BeFalse();
    }

    [Test]
    public void Unticking_keep_open_after_success_closes_and_saves_the_setting()
    {
        _host.CloseProcessDialog = false;
        ProcessViewModel viewModel = CreateViewModel();
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;
        viewModel.Start();
        _console.Exit(0);

        viewModel.KeepDialogOpen = false;

        _host.CloseProcessDialog.Should().BeTrue();
        closed.Should().BeTrue();
    }

    [Test]
    public void Failure_stays_open()
    {
        _host.CloseProcessDialog = true;
        ProcessViewModel viewModel = CreateViewModel();
        bool closeRequested = false;
        viewModel.CloseRequested += (_, _) => closeRequested = true;
        viewModel.Start();

        _console.Exit(128);

        viewModel.Status.Should().Be(ProcessStatus.Failed);
        viewModel.ErrorOccurred.Should().BeTrue();
        closeRequested.Should().BeFalse();
        _host.TaskbarStates.Should().EndWith(TaskbarProgressState.Error);
    }

    [Test]
    public void Abort_kills_the_process_and_closes()
    {
        ProcessViewModel viewModel = CreateViewModel();
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;
        viewModel.Start();

        viewModel.AbortCommand.Execute(null);

        _console.Killed.Should().BeTrue();
        viewModel.ErrorOccurred.Should().BeTrue();
        viewModel.Output.Should().EndWith("Aborted");
        closed.Should().BeFalse();
    }

    [Test]
    public void A_process_that_cannot_start_is_reported_as_failed()
    {
        _console.StartException = new InvalidOperationException("no git");
        ProcessViewModel viewModel = CreateViewModel();

        viewModel.Start();

        _messageBoxes.Errors.Should().ContainSingle().Which.Should().Contain("no git");
        viewModel.Status.Should().Be(ProcessStatus.Failed);
    }

    [Test]
    public void Error_dialog_shows_given_output_without_running_anything()
    {
        ProcessViewModel viewModel = CreateViewModel();

        viewModel.ShowAsErrorDialog("Push failed", ["line 1\n", "line 2\n"]);
        viewModel.Start();

        _console.Started.Should().BeFalse();
        viewModel.Title.Should().Be("Push failed");
        viewModel.Status.Should().Be(ProcessStatus.Failed);
        viewModel.IsAbortVisible.Should().BeFalse();
        viewModel.IsKeepDialogOpenVisible.Should().BeFalse();
        _console.Written.Should().Equal("line 1\n", "line 2\n");
        _host.RecordedHistory.Should().BeEmpty();
    }

    [Test]
    public void Keep_open_is_hidden_without_dialog_settings()
    {
        CreateViewModel(useDialogSettings: false).IsKeepDialogOpenVisible.Should().BeFalse();
    }

    private ProcessViewModel CreateViewModel(string displayPath = "repo", bool useDialogSettings = true)
        => new(new ProcessStrings(), displayPath, _console, _host, _messageBoxes, useDialogSettings, postToUiThread: action => action());

    [Test]
    public void The_exit_handler_can_retry_or_change_the_error()
    {
        _host.CloseProcessDialog = false;
        ProcessViewModel viewModel = CreateViewModel();
        int exits = 0;
        viewModel.ExitHandler = (ref bool isError) =>
        {
            exits++;
            if (exits == 1)
            {
                // As a push rejected then retried with --force-with-lease.
                isError.Should().BeTrue();
                viewModel.Retry();
                return true;
            }

            isError = false;
            return false;
        };
        int beforeStart = 0;
        viewModel.BeforeStart = () => beforeStart++;
        viewModel.Start();
        _console.Emit("rejected\n");

        _console.Exit(1);
        viewModel.IsDone.Should().BeFalse("the process is retried");
        viewModel.Output.Should().BeEmpty("a retry starts with a new output");
        _console.StartCount.Should().Be(2);
        beforeStart.Should().Be(2);

        _console.Exit(1);
        viewModel.IsDone.Should().BeTrue();
        viewModel.ErrorOccurred.Should().BeFalse("the handler turned the error into a success");
    }

    [Test]
    public void A_failing_exit_handler_is_an_error()
    {
        ProcessViewModel viewModel = CreateViewModel();
        viewModel.ExitHandler = (ref bool isError) => throw new InvalidOperationException();
        viewModel.Start();

        _console.Exit(0);

        viewModel.ErrorOccurred.Should().BeTrue();
    }

    [Test]
    public void The_output_is_passed_on_and_the_title_can_be_set()
    {
        ProcessViewModel viewModel = CreateViewModel(displayPath: "~/repo");
        List<string> received = [];
        viewModel.DataReceived += (_, text) => received.Add(text);
        viewModel.BaseTitle = "Push to origin";
        viewModel.Start();

        _console.Emit("Counting objects: 10% (1/10)\r");
        _console.Emit("If you trust this host, enter \"y\"");

        viewModel.Title.Should().Be("Counting objects: 10% (1/10)", "the progress of a plain text console is shown in the title");
        viewModel.BaseTitle.Should().Be("Push to origin");
        received.Should().Equal("Counting objects: 10% (1/10)\r", "If you trust this host, enter \"y\"");

        viewModel.Kill();
        _console.Killed.Should().BeTrue();
        viewModel.Aborted.Should().BeFalse("killed by the dialog, not aborted by the user");
        viewModel.AbortCommand.Execute(null);
        viewModel.Aborted.Should().BeTrue();
    }

    internal sealed class FakeConsole : IConsoleProcess, IEmbeddedNativeView
    {
        public event EventHandler<string>? OutputReceived;

        public event EventHandler<int>? Exited;

        public event EventHandler? HostTerminated;

        public event EventHandler<string>? PlainTextWritten;

        public bool IsPlainText { get; set; } = true;

        public IEmbeddedView? View => ControlView is { } controlView ? controlView : IsPlainText ? null : this;

        /// <summary>A terminal that is a control of the dialog (as the built-in terminal), instead of a native window.</summary>
        public IEmbeddedControlView? ControlView { get; set; }

        public bool Started { get; private set; }

        public int StartCount { get; private set; }

        public bool Killed { get; private set; }

        public Exception? StartException { get; set; }

        public List<string> Written { get; } = [];

        public List<string> Input { get; } = [];

        public void Emit(string text) => OutputReceived?.Invoke(this, text);

        public void Exit(int exitCode) => Exited?.Invoke(this, exitCode);

        public void Terminate() => HostTerminated?.Invoke(this, EventArgs.Empty);

        public void Start()
        {
            if (StartException is not null)
            {
                throw StartException;
            }

            Started = true;
            StartCount++;
        }

        public void Kill() => Killed = true;

        public void Reset() => Written.Clear();

        public void WriteInput(string text) => Input.Add(text);

        public void WriteOutput(string text)
        {
            // As the plain text runner: the text is shown by the dialog.
            Written.Add(text);
            PlainTextWritten?.Invoke(this, text);
        }

        public nint Attach(nint parentWindow) => 0;

        public void Detach()
        {
        }
    }

    internal sealed class FakeHost : IProcessDialogHost
    {
        public bool CloseProcessDialog { get; set; }

        public bool ShowPasswordInput { get; set; }

        public List<string> RecordedHistory { get; } = [];

        public List<TaskbarProgressState> TaskbarStates { get; } = [];

        public void RecordHistory(string output) => RecordedHistory.Add(output);

        public void SetTaskbarProgress(TaskbarProgressState state, int percent) => TaskbarStates.Add(state);

        public void ClearTaskbarProgress()
        {
        }
    }

    internal sealed class FakeMessageBoxes : IMessageBoxService
    {
        public List<string> Errors { get; } = [];

        public void ShowError(string text, string caption) => Errors.Add(text);

        public List<string> Informations { get; } = [];

        public void ShowInformation(string text, string caption) => Informations.Add(text);

        public List<string> Warnings { get; } = [];

        public void ShowWarning(string text, string caption) => Warnings.Add(text);

        public List<string> Confirmations { get; } = [];

        public bool ConfirmResult { get; set; } = true;

        public bool Confirm(string text, string caption, bool defaultNo = false)
        {
            Confirmations.Add(text);
            return ConfirmResult;
        }

        /// <summary>The questions asked with the Question icon (also in <see cref="Confirmations"/>).</summary>
        public List<string> Questions { get; } = [];

        public bool ConfirmQuestion(string text, string caption)
        {
            Questions.Add(text);
            return Confirm(text, caption);
        }

        /// <summary>The answer to the next yes/no/cancel questions.</summary>
        public bool? ConfirmWithCancelResult { get; set; } = true;

        public bool? ConfirmWithCancel(string text, string caption)
        {
            Confirmations.Add(text);
            return ConfirmWithCancelResult;
        }
    }
}
