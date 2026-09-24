using GitCommands;
using GitExtUtils;
using GitUI.ConsoleEmulation;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Services;

namespace GitUI.AvaloniaHosting;

/// <summary>The console tab of the Avalonia main window (<c>FillTerminalTab</c>): the WinForms terminal as a child window.</summary>
internal static partial class AvaloniaDialogs
{
    private sealed partial class BrowseHost : IBrowseConsoleHost
    {
        public bool IsConsoleAvailable
            => AppSettings.ShowConEmuTab.Value && _commands.GetRequiredService<IConsoleEmulatorsRegistry>().AvailableConsoleEmulators.Count > 0;

        public IBrowseTerminal? CreateTerminal()
            => _commands.GetRequiredService<IConsoleEmulatorsRegistry>().CreateShellRunner() is { } runner
                ? new BrowseTerminal(runner, () => Module.WorkingDir)
                : null;
    }

    /// <summary>The shell runner of the console emulator, embedded as <c>ConsoleProcess</c> embeds the command runners.</summary>
    private sealed class BrowseTerminal(IConsoleShellRunner runner, Func<string> getWorkingDir) : IBrowseTerminal, IEmbeddedNativeView
    {
        private static readonly nint HWND_MESSAGE = -3;

        public IEmbeddedNativeView View => this;

        public bool IsShellRunning => runner.IsShellRunning;

        public void StartShell() => runner.StartShell(getWorkingDir());

        public void Focus() => runner.FocusTerminal();

        public nint Attach(nint parentWindow)
        {
            // WinForms creates parentless controls as children of its parking window; move it into the Avalonia window.
            nint handle = runner.Control.Handle;
            NativeMethods.SetParent(handle, parentWindow);
            runner.Control.Visible = true;
            return handle;
        }

        public void Detach()
        {
            // The tab is not shown (or the window is closing): park the control so that WinForms does not lose it.
            if (runner.Control.IsHandleCreated)
            {
                runner.Control.Visible = false;
                NativeMethods.SetParent(runner.Control.Handle, HWND_MESSAGE);
            }
        }

        public void Dispose() => runner.Control.Dispose();
    }
}
