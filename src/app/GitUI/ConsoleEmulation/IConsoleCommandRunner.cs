using GitUI.Presentation.Services;

namespace GitUI.ConsoleEmulation;

/// <summary>
///  Represents a console that executes a console command and displays its output.
/// </summary>
public interface IConsoleCommandRunner : IDisposable
{
    /// <summary>
    ///  Gets the native window of the console, which the progress dialog embeds; <see langword="null"/> for the plain text
    ///  console, whose output the dialog shows itself.
    /// </summary>
    IEmbeddedNativeView? View { get; }

    /// <summary>
    ///  Occurs when the process writes output.
    /// </summary>
    event EventHandler<ConsoleOutputEventArgs> CommandOutputReceived;

    /// <summary>
    ///  Occurs when the command process exits.
    /// </summary>
    event EventHandler<ConsoleProcessExitEventArgs> CommandProcessExited;

    /// <summary>
    ///  Occurs when the console host terminates independently of the command it runs.
    /// </summary>
    event EventHandler? ConsoleHostTerminated;

    /// <summary>
    ///  Terminates the running process.
    /// </summary>
    void KillCommandProcess();

    /// <summary>
    ///  Resets the console and terminates any running target process.
    /// </summary>
    void ResetConsole();

    /// <summary>
    ///  Starts a new command process inside the console.
    /// </summary>
    void StartCommand(string command, string arguments, string workDir, Dictionary<string, string> envVariables);

    /// <summary>
    ///  Writes text to the process input stream.
    /// </summary>
    void WriteCommandProcessInput(string text);
}
