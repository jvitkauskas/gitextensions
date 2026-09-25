using GitCommands;
using GitUI.Presentation.Services;
using GitUI.Shells;

namespace GitUI.ConsoleEmulation.BuiltIn;

/// <summary>The shell of the terminal tab in the built-in terminal (as <see cref="ConEmu.ConEmuConsoleShellRunner"/>).</summary>
internal sealed class BuiltInTerminalShellRunner(IShellProvider shellProvider, ConsoleEmulatorSettings settings) : IConsoleShellRunner
{
    private readonly BuiltInTerminal _terminal = new(settings);

    public IEmbeddedView View => _terminal;

    public bool IsShellRunning => _terminal.IsProcessRunning;

    public void ChangeWorkingDirectory(string path)
    {
        if (!IsShellRunning || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        IShellDescriptor shell = shellProvider.GetShell(AppSettings.ConEmuTerminal.Value);
        string command = shell.GetChangeDirCommand(path);
        if (string.IsNullOrWhiteSpace(command))
        {
            // Submitting the line without a command to run would execute whatever the user has typed so far.
            return;
        }

        // Clears what the user has typed first: Ctrl+A Ctrl+K in bash, Escape in cmd and PowerShell.
        string clearLine = shell.Name == BashShell.ShellName ? "\x01\x0B" : "\x1B";
        _terminal.SendInput($"{clearLine}{command}\r");
    }

    public void FocusTerminal() => _terminal.Focus();

    public void StartShell(string workDir)
    {
        if (IsShellRunning)
        {
            FocusTerminal();
            return;
        }

        string commandLine = shellProvider.GetShellCommandLine(AppSettings.ConEmuTerminal.Value);
        if (commandLine == ShellProvider.DefaultConsoleCommandLine)
        {
            // The fallback of the shell provider, in the syntax of ConEmu.
            commandLine = "cmd.exe";
        }

        (string executable, string arguments) = SplitCommandLine(commandLine);

        Dictionary<string, string> environment = [];
        if (!string.IsNullOrEmpty(AppSettings.GitCommandValue) && Path.GetDirectoryName(AppSettings.GitCommandValue) is { Length: > 0 } gitDirectory)
        {
            // As ConEmu: git of the settings first in the path of the shell.
            environment["PATH"] = $"{gitDirectory}{Path.PathSeparator}{Environment.GetEnvironmentVariable("PATH")}";
        }

        _terminal.Start(executable, arguments, workDir, environment);
        FocusTerminal();
    }

    public void Dispose() => _terminal.Dispose();

    /// <summary>The executable (quoted or not) and the arguments of <paramref name="commandLine"/>.</summary>
    internal static (string Executable, string Arguments) SplitCommandLine(string commandLine)
    {
        commandLine = commandLine.Trim();
        int end = commandLine.StartsWith('"')
            ? commandLine.IndexOf('"', 1)
            : commandLine.IndexOf(' ');
        if (end < 0)
        {
            return (commandLine.Trim('"'), "");
        }

        return commandLine.StartsWith('"')
            ? (commandLine[1..end], commandLine[(end + 1)..].Trim())
            : (commandLine[..end], commandLine[(end + 1)..].Trim());
    }
}
