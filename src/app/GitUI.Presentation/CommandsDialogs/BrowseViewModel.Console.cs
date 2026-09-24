using CommunityToolkit.Mvvm.ComponentModel;
using GitUI.Presentation.Services;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>An interactive shell in a terminal emulator (the WinForms <c>IConsoleShellRunner</c>), shown as a native view.</summary>
public interface IBrowseTerminal : IDisposable
{
    IEmbeddedNativeView View { get; }

    bool IsShellRunning { get; }

    /// <summary>Starts or restarts the shell in the working directory.</summary>
    void StartShell();

    void Focus();
}

/// <summary>What the console tab needs from the application.</summary>
public interface IBrowseConsoleHost
{
    /// <summary>Whether the tab is shown: <c>ShowConEmuTab</c> and a console emulator is available.</summary>
    bool IsConsoleAvailable { get; }

    /// <summary>The terminal of the configured console emulator, if it can be created.</summary>
    IBrowseTerminal? CreateTerminal();
}

/// <summary>The console tab of the main window (<c>FillTerminalTab</c>).</summary>
public sealed partial class BrowseViewModel : IDisposable
{
    private IBrowseConsoleHost? _consoleHost;
    private IBrowseTerminal? _terminal;

    /// <summary>Whether the console tab is shown.</summary>
    public bool HasConsole { get; private set; }

    /// <summary>The terminal, created when the tab is first shown.</summary>
    [ObservableProperty]
    public partial IEmbeddedNativeView? ConsoleView { get; private set; }

    public void Dispose()
    {
        _terminal?.Dispose();
        _terminal = null;
        ConsoleView = null;
        DisposeBuildReport();
    }

    private void InitializeConsole()
    {
        _consoleHost = _host as IBrowseConsoleHost;
        HasConsole = _consoleHost?.IsConsoleAvailable is true;
    }

    // As the Selecting handler of FillTerminalTab: the terminal is created when its tab is first selected, and the shell
    // started again if it exited.
    private void UpdateConsole()
    {
        if (!HasConsole || SelectedTab != BrowseTab.Console)
        {
            return;
        }

        if (_terminal is null)
        {
            _terminal = _consoleHost!.CreateTerminal();
            if (_terminal is null)
            {
                return;
            }

            ConsoleView = _terminal.View;
        }

        if (_terminal.IsShellRunning)
        {
            _terminal.Focus();
            return;
        }

        _terminal.StartShell();
    }
}
