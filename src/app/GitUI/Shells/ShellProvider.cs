namespace GitUI.Shells;

public class ShellProvider : IShellProvider
{
    /// <summary>The fallback shell: the interactive CMD shell, in the syntax of ConEmu (its <c>ConEmuConstants.DefaultConsoleCommandLine</c>).</summary>
    public const string DefaultConsoleCommandLine = "{cmd}";

    private static IShellDescriptor DefaultShell = new BashShell();
    private static readonly IShellDescriptor[] Shells = [DefaultShell, new CmdShell(), new PwshShell(), new PowerShellShell()];

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
}
