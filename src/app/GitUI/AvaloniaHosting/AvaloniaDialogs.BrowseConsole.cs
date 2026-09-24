using GitCommands;
using GitExtUtils;
using GitUI.ConsoleEmulation;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Services;

namespace GitUI.AvaloniaHosting;

/// <summary>The console tab of the Avalonia main window (<c>FillTerminalTab</c>): the terminal as a child window.</summary>
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

    /// <summary>The shell runner of the console emulator, whose native window the tab embeds.</summary>
    private sealed class BrowseTerminal(IConsoleShellRunner runner, Func<string> getWorkingDir) : IBrowseTerminal
    {
        public IEmbeddedView View => runner.View;

        public bool IsShellRunning => runner.IsShellRunning;

        public void StartShell() => runner.StartShell(getWorkingDir());

        public void Focus() => runner.FocusTerminal();

        public void Dispose() => runner.Dispose();
    }
}
