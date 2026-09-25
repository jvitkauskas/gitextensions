using GitCommands;

namespace GitUI.Shells;

public class ShellProvider : IShellProvider
{
    /// <summary>The fallback shell: the interactive CMD shell, in the syntax of ConEmu (its <c>ConEmuConstants.DefaultConsoleCommandLine</c>).</summary>
    public const string DefaultConsoleCommandLine = "{cmd}";

    private static readonly IShellDescriptor[] Shells = OperatingSystem.IsWindows()
        ? [new BashShell(), new CmdShell(), new PwshShell(), new PowerShellShell()]
        : GetUnixShells(Environment.GetEnvironmentVariable("SHELL"), name => PathUtil.TryFindFullPath(name, out string? path) ? path : null, File.Exists);

    private static readonly IShellDescriptor DefaultShell = Shells[0];

    public IReadOnlyList<IShellDescriptor> GetShells() => Shells;

    public IShellDescriptor GetShell(string? name) => Shells.FirstOrDefault(s => s.Name == name) ?? DefaultShell;

    public string GetShellCommandLine(string? shellType)
    {
        IShellDescriptor shell = GetShell(shellType);

        if (!shell.HasExecutable || shell.ExecutableCommandLine is null)
        {
            // Fallback to default if ExecutableCommandLine is not set
            return DefaultConsoleCommandLine;
        }

        return shell.ExecutableCommandLine;
    }

    /// <summary>
    ///  The shells of Linux and macOS (docs/avalonia-port/CROSS-PLATFORM.md, phase 3): the shell of the user
    ///  (<c>$SHELL</c>) first, then bash, zsh and fish if they are installed; bash (without an executable) if none is.
    /// </summary>
    internal static IShellDescriptor[] GetUnixShells(string? userShell, Func<string, string?> findOnPath, Func<string, bool> fileExists)
    {
        List<IShellDescriptor> shells = [];
        if (!string.IsNullOrEmpty(userShell) && Path.GetFileName(userShell) is { Length: > 0 } userShellName && fileExists(userShell))
        {
            shells.Add(new UnixShell(userShellName, userShell));
        }

        foreach (string name in (string[])["bash", "zsh", "fish"])
        {
            if (!shells.Any(shell => shell.Name == name) && findOnPath(name) is string path)
            {
                shells.Add(new UnixShell(name, path));
            }
        }

        return shells.Count > 0 ? [.. shells] : [new BashShell()];
    }
}
