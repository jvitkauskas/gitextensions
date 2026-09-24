using GitUI.Shells;

namespace GitUI.ConsoleEmulation.BuiltIn;

/// <summary>
///  The terminal emulator built into the application (Iciclecreek.Avalonia.Terminal, running the processes in a pseudo
///  console): a control of the Avalonia windows, where ConEmu and Mintty run in native child windows.
/// </summary>
internal sealed class BuiltInTerminalEmulator(IShellProvider shellProvider) : IConsoleEmulator
{
    public const string EmulatorName = "terminal";

    public string Name => EmulatorName;

    public string DisplayName => "Built-in terminal";

    // The pseudo console of Windows (ConPTY) came with Windows 10 1809.
    public bool IsSupportedInCurrentEnvironment => !OperatingSystem.IsWindows() || OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763);

    public IReadOnlyCollection<string> AvailableThemes { get; } = [.. BuiltInTerminalTheme.All.Select(theme => theme.Name)];

    // The theme that suits the theme of the application.
    public string? DefaultTheme => null;

    public IConsoleCommandRunner CreateCommandRunner(ConsoleEmulatorSettings settings) => new BuiltInTerminalCommandRunner(settings);

    public IConsoleShellRunner CreateShellRunner(ConsoleEmulatorSettings settings) => new BuiltInTerminalShellRunner(shellProvider, settings);
}
