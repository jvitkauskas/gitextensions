using GitCommands;

namespace GitUI.Shells;

public class PwshShell : ShellDescriptor
{
    public PwshShell()
    {
        Name = "pwsh";
        Icon = EmbeddedIcons.Get("pwsh");

        ExecutableName = "pwsh.exe";
        if (OperatingSystem.IsWindows() && PathUtil.TryFindShellPath(ExecutableName, out string? exePath))
        {
            ExecutablePath = exePath;
            ExecutableCommandLine = exePath.Quote();
        }
    }

    public override string GetChangeDirCommand(string path) => $"cd {path.QuoteNE()}";
}
