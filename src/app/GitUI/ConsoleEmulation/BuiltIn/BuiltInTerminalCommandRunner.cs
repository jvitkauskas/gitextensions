using GitCommands.Logging;
using GitExtensions.Extensibility;
using GitUI.Presentation.Services;

namespace GitUI.ConsoleEmulation.BuiltIn;

/// <summary>A command of the progress dialog run in the built-in terminal (as <see cref="ConEmu.ConEmuConsoleCommandRunner"/>).</summary>
internal sealed class BuiltInTerminalCommandRunner : IConsoleCommandRunner
{
    private readonly BuiltInTerminal _terminal;
    private TerminalOutputProcessor? _outputProcessor;
    private ProcessOperation? _operation;

    public BuiltInTerminalCommandRunner(ConsoleEmulatorSettings settings)
    {
        _terminal = new BuiltInTerminal(settings);
        _terminal.OutputReceived += (_, bytes) => _outputProcessor?.Process(bytes.Span);
        _terminal.ProcessExited += (_, exitCode) => OnProcessExited(exitCode);
    }

    public IEmbeddedView? View => _terminal;

    public event EventHandler<ConsoleOutputEventArgs>? CommandOutputReceived;
    public event EventHandler<ConsoleProcessExitEventArgs>? CommandProcessExited;

    // The terminal is a control of the dialog: it does not go away by itself.
#pragma warning disable CS0067
    public event EventHandler? ConsoleHostTerminated;
#pragma warning restore CS0067

    public void StartCommand(string command, string arguments, string workDir, Dictionary<string, string> envVariables)
    {
        _operation = CommandLog.LogProcessStart(command, arguments, workDir);
        _outputProcessor = new TerminalOutputProcessor(line => CommandOutputReceived?.Invoke(this, new ConsoleOutputEventArgs(line)));

        // As ConEmu echoes the command line.
        _terminal.WriteLine($"{command.Quote()} {arguments}");
        _terminal.Start(command, arguments, workDir, envVariables);
    }

    public void WriteCommandProcessInput(string text) => _terminal.SendInput(text);

    // As ConEmu: Ctrl+C.
    public void KillCommandProcess() => _terminal.SendInput("\x03");

    public void ResetConsole()
    {
        if (_terminal.IsProcessRunning)
        {
            _operation?.LogProcessEnd(new Exception("Process killed"));
            _operation = null;
        }

        _outputProcessor = null;
        _terminal.Stop();
        _terminal.Clear();
    }

    public void Dispose() => _terminal.Dispose();

    private void OnProcessExited(int exitCode)
    {
        _operation?.LogProcessEnd(exitCode);
        _operation = null;
        _outputProcessor?.Flush();
        _outputProcessor = null;
        CommandProcessExited?.Invoke(this, new ConsoleProcessExitEventArgs(exitCode));
    }
}
