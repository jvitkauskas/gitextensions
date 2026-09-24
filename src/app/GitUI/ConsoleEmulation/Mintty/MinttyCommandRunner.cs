using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using GitCommands.Logging;
using GitExtensions.Extensibility;
using GitUI.Presentation.Services;

namespace GitUI.ConsoleEmulation.Mintty;

internal sealed partial class MinttyCommandRunner : IConsoleCommandRunner
{
    private readonly string _minttyPath;
    private readonly string _bashPath;
    private readonly ConsoleEmulatorSettings _settings;

    private MinttyControl? _terminal;
    private readonly NativeHostWindow _window = new();

    internal MinttyCommandRunner(string minttyPath, string bashPath, ConsoleEmulatorSettings settings)
    {
        _minttyPath = minttyPath;
        _bashPath = bashPath;
        _settings = settings;
    }

    /// <summary>The window in which mintty runs (in place of the WinForms panel).</summary>
    public IEmbeddedNativeView? View => _window;

    public event EventHandler<ConsoleOutputEventArgs>? CommandOutputReceived;
    public event EventHandler<ConsoleProcessExitEventArgs>? CommandProcessExited;
    public event EventHandler? ConsoleHostTerminated;

    public void WriteCommandProcessInput(string text)
    {
        _terminal?.SendConsoleInput(text);
    }

    public void KillCommandProcess()
    {
        _terminal?.RunningSession?.Kill();
    }

    [MemberNotNull(nameof(_terminal))]
    public void ResetConsole()
    {
        MinttyControl? oldTerminal = _terminal;

        _terminal = new MinttyControl(_window);

        oldTerminal?.Dispose();
    }

    public void Dispose()
    {
        _terminal?.Dispose();
        _terminal = null;
        _window.Dispose();
    }

    public void StartCommand(string command, string arguments, string workDir, Dictionary<string, string> envVariables)
    {
        if (_terminal is null)
        {
            ResetConsole();
        }

        ProcessOperation operation = CommandLog.LogProcessStart(command, arguments, workDir);

        try
        {
            string commandLine = new ArgumentBuilder { command.Quote(), arguments }.ToString();

            MinttyStartInfo startInfo = new()
            {
                ProcessOperation = operation,
                ConsoleProcessCommandLine = commandLine,
                StartupDirectory = workDir,
                ProcessExitedCallback = exitCode =>
                {
                    operation.LogProcessEnd(exitCode);
                    ThreadHelper.InvokeAndForget(() => CommandProcessExited?.Invoke(this, new ConsoleProcessExitEventArgs(exitCode)));
                },
                ConsoleClosedCallback = () =>
                {
                    ThreadHelper.InvokeAndForget(() => ConsoleHostTerminated?.Invoke(this, EventArgs.Empty));
                },
                AnsiOutputLineCallback = line =>
                {
                    line = StripAnsiCodesRegex().Replace(line, string.Empty);
                    ThreadHelper.InvokeAndForget(() => CommandOutputReceived?.Invoke(this, new ConsoleOutputEventArgs(line)));
                }
            };

            foreach ((string name, string value) in envVariables)
            {
                startInfo.EnvironmentVariables[name] = value;
            }

            _terminal.StartCommand(startInfo, _minttyPath, _bashPath, _settings);
        }
        catch (Exception ex)
        {
            operation.LogProcessEnd(ex);
            throw;
        }
    }

    [GeneratedRegex(@"\x1B(?:[@-Z\\-_]|\[[0-?]*[ -/]*[@-~])")]
    private static partial Regex StripAnsiCodesRegex();
}
