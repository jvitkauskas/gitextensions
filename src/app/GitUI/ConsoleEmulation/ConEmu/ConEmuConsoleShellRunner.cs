using System.Globalization;
using ConEmu.Inside;
using GitCommands;
using GitUI.Presentation.Services;
using GitUI.Shells;
using Microsoft;

namespace GitUI.ConsoleEmulation.ConEmu;

/// <summary>
///  Wraps <c>ConEmuControl</c> for the repository browser's terminal tab.
/// </summary>
internal sealed class ConEmuConsoleShellRunner(IShellProvider shellProvider, ConsoleEmulatorSettings settings) : IConsoleShellRunner
{
    private readonly NativeHostWindow _window = new();
    private ConEmuHost? _conEmu;

    /// <summary>The window in which ConEmu runs (in place of the WinForms <c>ConEmuControl</c>).</summary>
    public IEmbeddedNativeView View => _window;

    public bool IsShellRunning => _conEmu?.RunningSession is not null;

    public void ChangeWorkingDirectory(string path)
    {
        if (_conEmu?.RunningSession is not { } session || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string? shellType = AppSettings.ConEmuTerminal.Value;
        IShellDescriptor shell = shellProvider.GetShell(shellType);
        string command = shell.GetChangeDirCommand(path);
        if (string.IsNullOrWhiteSpace(command))
        {
            // Submitting the line without a command to run would execute whatever the user has typed so far
            return;
        }

        switch (shell.Name)
        {
            case BashShell.ShellName:
                // Use a ConEmu macro to send the sequence for clearing the bash command line
                session.BeginGuiMacro("Keys").WithParam("^A").WithParam("^K").ExecuteSync();
                WriteInput(command);
                break;

            default:
                WriteInput($"\x1B{command}");
                break;
        }

        session.BeginGuiMacro("Keys").WithParam("Enter").ExecuteSync();

        return;

        // Writing the input is a ConEmu macro of its own, and each macro is dispatched on a separate
        // thread pool work item. Fire and forget would therefore allow the Enter above to be executed
        // first, leaving the command in the prompt to be executed later together with whatever the
        // user types next.
        void WriteInput(string text) => ThreadHelper.JoinableTaskFactory.Run(() => session.WriteInputTextAsync(text));
    }

    public void FocusTerminal()
    {
        // As ConEmuControl: the focus of the host window goes to the console emulator.
        if (FindConEmuWindow() is not 0 and nint conEmuWindow)
        {
            SetFocus(conEmuWindow);
        }
        else
        {
            _window.Focus();
        }
    }

    public void Dispose()
    {
        _conEmu?.Dispose();
        _conEmu = null;
        _window.Dispose();
    }

    /// <summary>The window of ConEmu in the host window, if any.</summary>
    private nint FindConEmuWindow() => FindWindowExW(_window.Handle, 0, null, null);

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern nint FindWindowExW(nint parent, nint childAfter, string? className, string? windowName);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern nint SetFocus(nint window);

    public void StartShell(string workDir)
    {
        if (IsShellRunning)
        {
            FocusTerminal();
            return;
        }

        ConEmuStartInfo startInfo = new()
        {
            StartupDirectory = workDir,
            WhenConsoleProcessExits = WhenConsoleProcessExits.CloseConsoleEmulator
        };

        string? shellType = AppSettings.ConEmuTerminal.Value;
        startInfo.ConsoleProcessCommandLine = shellProvider.GetShellCommandLine(shellType);

        if (!string.IsNullOrEmpty(AppSettings.GitCommandValue))
        {
            string? dirGit = Path.GetDirectoryName(AppSettings.GitCommandValue);
            if (!string.IsNullOrEmpty(dirGit))
            {
                startInfo.SetEnv("PATH", $"{dirGit};%PATH%");
            }
        }

        try
        {
            Validates.NotNull(settings.Font);

            _conEmu ??= new ConEmuHost(_window.Handle) { IsStatusbarVisible = false };
            _conEmu.Start(
                startInfo,
                ThreadHelper.JoinableTaskFactory,
                settings.Theme,
                settings.Font.Name,
                settings.Font.Size.ToString("F0", CultureInfo.InvariantCulture));
        }
        catch (InvalidOperationException)
        {
#if DEBUG
            MessageBoxes.ShowError(null, "ConEmu appears to be missing. Please perform a full rebuild and try again.");
#else
            throw;
#endif
        }
    }
}
