using System.Runtime.InteropServices;
using GitExtensions.Extensibility;
using GitExtUtils.GitUI.Theming;
using GitUI.Shells;

namespace GitUI.ConsoleEmulation.ConEmu;

internal sealed class ConEmuConsoleEmulator(IShellProvider shellProvider) : IConsoleEmulator
{
    private const string DarkThemeFallback = "<Tomorrow Night>";
    private const string LightThemeFallback = "<Tomorrow>";

    private static readonly FontDescriptor DefaultFont = new("Consolas", 12);

    public string Name => "conemu";

    public string DisplayName => "ConEmu";

    // ConEmu ships x86 and x64 binaries only; hosted in an ARM64 process it fails to load them (error 193).
    public bool IsSupportedInCurrentEnvironment => OperatingSystem.IsWindows()
        && RuntimeInformation.ProcessArchitecture is Architecture.X64 or Architecture.X86;

    public IReadOnlyCollection<string> AvailableThemes { get; } =
    [
        "<Default Windows scheme>",
        "<Base16>",
        "<Cobalt2>",
        "<ConEmu>",
        "<Gamma 1>",
        "<Monokai>",
        "<Murena scheme>",
        "<PowerShell>",
        "<Solarized>",
        "<Solarized Git>",
        "<Solarized (Luke Maciak)>",
        "<Solarized (John Doe)>",
        "<Solarized Light>",
        "<SolarMe>",
        "<Standard VGA>",
        "<tc-maxx>",
        "<Terminal.app>",
        "<Tomorrow>",
        "<Tomorrow Night>",
        "<Tomorrow Night Blue>",
        "<Tomorrow Night Bright>",
        "<Tomorrow Night Eighties>",
        "<Twilight>",
        "<Ubuntu>",
        "<xterm>",
        "<Zenburn>",
    ];

    public string? DefaultTheme => null;

    public IConsoleCommandRunner CreateCommandRunner(ConsoleEmulatorSettings settings)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException($"Check {nameof(IsSupportedInCurrentEnvironment)} before calling.");
        }

        return new ConEmuConsoleCommandRunner(settings with
        {
            Theme = ResolveTheme(settings.Theme),
            Font = settings.Font ?? DefaultFont,
        });
    }

    public IConsoleShellRunner CreateShellRunner(ConsoleEmulatorSettings settings)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException($"Check {nameof(IsSupportedInCurrentEnvironment)} before calling.");
        }

        return new ConEmuConsoleShellRunner(shellProvider, settings with
        {
            Theme = ResolveTheme(settings.Theme),
            Font = settings.Font ?? DefaultFont,
        });
    }

    internal static string ResolveTheme(string? configuredTheme)
    {
        return string.IsNullOrEmpty(configuredTheme)
            ? ColorHelper.IsDarkTheme ? DarkThemeFallback : LightThemeFallback
            : configuredTheme;
    }
}
