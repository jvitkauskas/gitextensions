using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitUI.Presentation.Services;

namespace GitUI.Presentation.HelperDialogs;

/// <summary>
///  As <c>FormProcess.HandleOnExit</c>: called when the process exits, with whether it failed; returns whether the exit is
///  handled (e.g. the process is retried), else the dialog is done with <paramref name="isError"/>.
/// </summary>
public delegate bool ProcessExitHandler(ref bool isError);

public enum ProcessStatus
{
    Running,
    Succeeded,
    Failed,
}

/// <summary>
///  View model of the progress dialog that runs a (git) process and shows its output
///  (port of <c>GitUI.HelperDialogs.FormStatus</c> and <c>FormProcess</c>).
/// </summary>
/// <remarks>
///  Console events can arrive on any thread; they are marshalled to the UI thread with the host-provided
///  <c>postToUiThread</c> (JoinableTaskFactory in the app).
/// </remarks>
public sealed partial class ProcessViewModel : DialogViewModel
{
    private const string AnsiEraseToEndOfLine = "\u001B[K";

    private readonly IConsoleProcess _console;
    private readonly IProcessDialogHost _host;
    private readonly IMessageBoxService _messageBoxes;
    private readonly Action<Action> _postToUiThread;
    private readonly StringBuilder _plainTextToAdd = new();
    private string _baseTitle;
    private readonly OutputLog _outputLog = new();
    private bool _isErrorDialog;

    public ProcessViewModel(
        ProcessStrings strings,
        string displayPath,
        IConsoleProcess console,
        IProcessDialogHost host,
        IMessageBoxService messageBoxes,
        bool useDialogSettings,
        Action<Action> postToUiThread)
    {
        Strings = strings;
        _console = console;
        _host = host;
        _messageBoxes = messageBoxes;
        _postToUiThread = postToUiThread;

        _baseTitle = string.IsNullOrWhiteSpace(displayPath) ? strings.Title.PlainText : $"{strings.Title.PlainText} ({displayPath})";
        Title = _baseTitle;

        IsKeepDialogOpenVisible = useDialogSettings;
        UseDialogSettings = useDialogSettings;
        KeepDialogOpen = !host.CloseProcessDialog;
        ShowPasswordState = host.ShowPasswordInput;

        console.OutputReceived += (_, text) => OnOutputReceived(text);
        console.PlainTextWritten += (_, text) => OnPlainTextWritten(text);
        console.Exited += (_, exitCode) => OnExited(exitCode);
        console.HostTerminated += (_, _) => Post(() => Close(accepted: false));
    }

    /// <summary>Raised when the process prompts for input, so that the view can focus the password box.</summary>
    public event EventHandler? InputRequested;

    /// <summary>As <c>FormProcess.DataReceived</c>: each chunk of output, after the dialog handled it; raised on any thread.</summary>
    public event EventHandler<string>? DataReceived;

    /// <summary>As <c>FormProcess.HandleOnExit</c> (<c>FormRemoteProcess</c>, <c>HandleOnExitCallback</c>).</summary>
    public ProcessExitHandler? ExitHandler { get; set; }

    /// <summary>As <c>FormProcess.BeforeProcessStart</c>: called before the process starts (again).</summary>
    public Action? BeforeStart { get; set; }

    public ProcessStrings Strings { get; }

    /// <summary>The title of the dialog instead of the working directory (the <c>Text</c> set on <c>FormRemoteProcess</c>).</summary>
    public string BaseTitle
    {
        get => _baseTitle;
        set
        {
            _baseTitle = value;
            Title = value;
        }
    }

    /// <summary>The native window of the console; <see langword="null"/> for the plain text console (see <see cref="PlainText"/>).</summary>
    public IEmbeddedView? ConsoleView => _console.View;

    /// <summary>Whether the console is plain text, shown by the dialog (<see cref="PlainText"/>).</summary>
    public bool IsPlainText => _console.IsPlainText;

    /// <summary>The text of the plain text console (the RichTextBox of <c>PlainTextConsoleCommandRunner</c>).</summary>
    [ObservableProperty]
    public partial string PlainText { get; private set; } = "";

    public bool UseDialogSettings { get; }

    [ObservableProperty]
    public partial string Title { get; private set; }

    [ObservableProperty]
    public partial ProcessStatus Status { get; private set; }

    /// <summary>Progress in percent, or <see langword="null"/> while unknown (indeterminate).</summary>
    [ObservableProperty]
    public partial double? ProgressValue { get; private set; }

    [ObservableProperty]
    public partial bool IsProgressVisible { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AcknowledgeCommand), nameof(AbortCommand))]
    public partial bool IsDone { get; private set; }

    [ObservableProperty]
    public partial bool IsAbortVisible { get; private set; } = true;

    [ObservableProperty]
    public partial bool IsKeepDialogOpenVisible { get; private set; }

    [ObservableProperty]
    public partial bool KeepDialogOpen { get; set; }

    [ObservableProperty]
    public partial bool IsShowPasswordVisible { get; private set; } = true;

    /// <summary>
    ///  Three-state "show password input": <see langword="null"/> when shown automatically because the process
    ///  prompted for input.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPasswordInputVisible))]
    public partial bool? ShowPasswordState { get; set; }

    public bool IsPasswordInputVisible => IsShowPasswordVisible && ShowPasswordState != false;

    [ObservableProperty]
    public partial string PasswordText { get; set; } = "";

    public bool ErrorOccurred { get; private set; }

    /// <summary>Whether the user aborted the process (as <c>DialogResult.Abort</c> of <c>FormStatus</c>).</summary>
    public bool Aborted { get; private set; }

    /// <summary>The logged output (progress messages excluded).</summary>
    public string Output => _outputLog.GetString();

    /// <summary>
    ///  Starts the process; the view calls this once it is shown.
    /// </summary>
    public void Start()
    {
        if (_isErrorDialog)
        {
            return;
        }

        _host.SetTaskbarProgress(TaskbarProgressState.Indeterminate, 0);
        Reset();

        try
        {
            BeforeStart?.Invoke();
            _console.Start();
        }
        catch (Exception ex)
        {
            _messageBoxes.ShowError(ex.ToString(), Strings.FailedToRunCommand);
            OnExited(1);
        }
    }

    /// <summary>
    ///  Turns the dialog into a report of an operation that already failed (<c>FormStatus.ShowErrorDialog</c>).
    /// </summary>
    public void ShowAsErrorDialog(string title, IEnumerable<string> output)
    {
        _isErrorDialog = true;
        Title = title;
        foreach (string line in output)
        {
            _console.WriteOutput(line);
        }

        IsKeepDialogOpenVisible = false;
        IsAbortVisible = false;
        Done(isSuccess: false, recordHistory: false);
    }

    /// <summary>The view calls this when the dialog has closed.</summary>
    public void OnClosed() => _host.ClearTaskbarProgress();

    partial void OnKeepDialogOpenChanged(bool value)
    {
        _host.CloseProcessDialog = !value;

        // As in FormStatus: switching to "don't keep" when the dialog would have closed already closes it.
        if (!value && IsDone && !ErrorOccurred)
        {
            Close(accepted: true);
        }
    }

    partial void OnShowPasswordStateChanged(bool? value)
    {
        if (value is bool persisted)
        {
            _host.ShowPasswordInput = persisted;
        }
    }

    [RelayCommand]
    private void SendInput()
    {
        _console.WriteInput($"{PasswordText}\n");
        PasswordText = "";
    }

    [RelayCommand(CanExecute = nameof(IsDone))]
    private void Acknowledge() => Close(accepted: true);

    private bool CanAbort() => !IsDone;

    [RelayCommand(CanExecute = nameof(CanAbort))]
    private void Abort()
    {
        try
        {
            Aborted = true;
            _console.Kill();
            _outputLog.Append(Environment.NewLine + "Aborted");
            Done(isSuccess: false);
            Close(accepted: false);
        }
        catch
        {
            // As in FormStatus.
        }
    }

    /// <summary>As <c>FormStatus.Retry</c>: the process is started again, with a new output.</summary>
    public void Retry() => Start();

    /// <summary>As <c>FormProcess.KillProcess</c>: the process is killed; its exit is handled as usual.</summary>
    public void Kill() => _console.Kill();

    /// <summary>As <c>FormStatus.Reset</c>: the output is cleared, e.g. before the process is started again.</summary>
    public void Reset()
    {
        Status = ProcessStatus.Running;
        _console.Reset();
        lock (_plainTextToAdd)
        {
            _plainTextToAdd.Clear();
        }

        PlainText = "";
        _outputLog.Clear();
        IsShowPasswordVisible = true;
        OnPropertyChanged(nameof(IsPasswordInputVisible));
        ProgressValue = null;
        IsProgressVisible = true;
        IsDone = false;
    }

    private void Done(bool isSuccess, bool recordHistory = true)
    {
        ErrorOccurred = !isSuccess;

        if (recordHistory)
        {
            try
            {
                _host.RecordHistory(Output);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
            }
        }

        IsShowPasswordVisible = false;
        OnPropertyChanged(nameof(IsPasswordInputVisible));
        IsProgressVisible = false;
        IsDone = true;
        _host.SetTaskbarProgress(isSuccess ? TaskbarProgressState.Normal : TaskbarProgressState.Error, 100);
        Status = isSuccess ? ProcessStatus.Succeeded : ProcessStatus.Failed;

        if (isSuccess && UseDialogSettings && _host.CloseProcessDialog)
        {
            Close(accepted: true);
        }
    }

    private void OnOutputReceived(string text)
    {
        // A carriage return has its literal meaning here: it terminates transient progress information.
        if (text.EndsWith('\r'))
        {
            string progress = text.TrimEnd();
            Post(() => SetProgress(progress));
            DataReceived?.Invoke(this, text);
            return;
        }

        string line = text.Replace(AnsiEraseToEndOfLine, "");
        _outputLog.Append(line);

        if (_console.IsPlainText)
        {
            // The plain text console does not display process output by itself.
            _console.WriteOutput(line);

            // A line without line feed is a prompt, e.g. for a password.
            if (!line.EndsWith('\n'))
            {
                Post(() =>
                {
                    if (ShowPasswordState == false)
                    {
                        ShowPasswordState = null;
                    }

                    InputRequested?.Invoke(this, EventArgs.Empty);
                });
            }
        }

        DataReceived?.Invoke(this, text);
    }

    private void SetProgress(string text)
    {
        int index = text.LastIndexOf('%');
        if (index > 4 && int.TryParse(text.AsSpan(index - 3, 3), NumberStyles.Integer, CultureInfo.InvariantCulture, out int progress) && progress >= 0)
        {
            ProgressValue = Math.Min(100, progress);
            _host.SetTaskbarProgress(TaskbarProgressState.Normal, progress);
        }

        // Show the last progress message in the title, unless the console shows it already.
        if (_console.IsPlainText)
        {
            Title = text;
        }
    }

    // As FormProcess.OnExit: the exit handler may handle the exit (e.g. retry); a failing handler is an error.
    private void OnExited(int exitCode) => Post(() =>
    {
        bool isError = exitCode != 0;
        try
        {
            if (ExitHandler?.Invoke(ref isError) == true)
            {
                return;
            }
        }
        catch
        {
            isError = true;
        }

        Done(isSuccess: !isError);
    });

    private void Post(Action action) => _postToUiThread(action);

    // As the ProcessOutputThrottle of PlainTextConsoleCommandRunner: the text written meanwhile is shown at once.
    private void OnPlainTextWritten(string text)
    {
        bool isFirst;
        lock (_plainTextToAdd)
        {
            isFirst = _plainTextToAdd.Length == 0;
            _plainTextToAdd.Append(text);
        }

        if (isFirst)
        {
            Post(() =>
            {
                string textToAdd;
                lock (_plainTextToAdd)
                {
                    textToAdd = _plainTextToAdd.ToString();
                    _plainTextToAdd.Clear();
                }

                PlainText += textToAdd;
            });
        }
    }

    /// <summary>Thread-safe output log (as <c>FormStatusOutputLog</c>).</summary>
    private sealed class OutputLog
    {
        private readonly Lock _lock = new();
        private readonly StringBuilder _text = new();

        public void Append(string text)
        {
            text = text.Replace('\v', '\n').ReplaceLineEndings();
            lock (_lock)
            {
                _text.Append(text);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _text.Clear();
            }
        }

        public string GetString()
        {
            lock (_lock)
            {
                return _text.ToString();
            }
        }
    }
}
