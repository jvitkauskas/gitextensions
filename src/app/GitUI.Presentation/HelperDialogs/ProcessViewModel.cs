using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitUI.Presentation.Services;

namespace GitUI.Presentation.HelperDialogs;

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
    private readonly string _baseTitle;
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
        console.Exited += (_, exitCode) => OnExited(exitCode);
        console.HostTerminated += (_, _) => Post(() => Close(accepted: false));
    }

    /// <summary>Raised when the process prompts for input, so that the view can focus the password box.</summary>
    public event EventHandler? InputRequested;

    public ProcessStrings Strings { get; }

    public IEmbeddedNativeView ConsoleView => _console.View;

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

    private void Reset()
    {
        Status = ProcessStatus.Running;
        _console.Reset();
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

    private void OnExited(int exitCode) => Post(() => Done(isSuccess: exitCode == 0));

    private void Post(Action action) => _postToUiThread(action);

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
