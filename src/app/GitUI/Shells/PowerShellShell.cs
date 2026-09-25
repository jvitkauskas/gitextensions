using GitCommands;

namespace GitUI.Shells;

public class PowerShellShell : ShellDescriptor
{
    public PowerShellShell()
    {
        Name = "powershell";
        Icon = EmbeddedIcons.Get("powershell");

        ExecutableName = "powershell.exe";
        if (OperatingSystem.IsWindows() && PathUtil.TryFindShellPath(ExecutableName, out string? exePath))
        {
            ExecutablePath = exePath;
            ExecutableCommandLine = exePath.Quote();
        }
    }

    public override string GetChangeDirCommand(string path) => $"cd {path.QuoteNE()}";
}
