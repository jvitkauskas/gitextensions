using GitUI.Presentation.Services;

namespace GitUI.Presentation.HelperDialogs;

/// <summary>
///  The process run by the progress dialog, and the console showing its output
///  (a terminal emulator or plain text, as configured by the user).
/// </summary>
public interface IConsoleProcess
{
    /// <summary>
    ///  <see langword="true"/> for the plain text console: it does not display output by itself (the dialog passes it
    ///  on through <see cref="WriteOutput"/>) and has no room for progress, which the dialog shows in its title.
    /// </summary>
    bool IsPlainText { get; }

    /// <summary>The console control to embed in the dialog.</summary>
    IEmbeddedNativeView View { get; }

    /// <summary>Raised for each chunk of process output; may be raised on any thread.</summary>
    event EventHandler<string>? OutputReceived;

    /// <summary>Raised with the exit code when the process exits; may be raised on any thread.</summary>
    event EventHandler<int>? Exited;

    /// <summary>Raised when the console host itself goes away (e.g. the user closed the terminal).</summary>
    event EventHandler? HostTerminated;

    void Start();

    /// <summary>Kills the process tree and releases what it may have left locked (the git index).</summary>
    void Kill();

    void Reset();

    void WriteInput(string text);

    /// <summary>Displays text in a plain text console (see <see cref="IsPlainText"/>).</summary>
    void WriteOutput(string text);
}

public enum TaskbarProgressState
{
    Indeterminate,
    Normal,
    Error,
}

/// <summary>
///  Application services used by the progress dialog.
/// </summary>
public interface IProcessDialogHost
{
    /// <summary><c>AppSettings.CloseProcessDialog</c>: close automatically after success.</summary>
    bool CloseProcessDialog { get; set; }

    /// <summary><c>AppSettings.ShowProcessDialogPasswordInput</c>.</summary>
    bool ShowPasswordInput { get; set; }

    /// <summary>Records the finished process in the output history.</summary>
    void RecordHistory(string output);

    void SetTaskbarProgress(TaskbarProgressState state, int percent);

    void ClearTaskbarProgress();
}
