using GitCommands;

namespace GitUI.Shells;

public class CmdShell : ShellDescriptor
{
    public CmdShell()
    {
        Name = "cmd";
        Icon = EmbeddedIcons.Get("cmd");

        ExecutableName = "cmd.exe";
        if (OperatingSystem.IsWindows() && PathUtil.TryFindShellPath(ExecutableName, out string? exePath))
        {
            ExecutablePath = exePath;
            ExecutableCommandLine = exePath.Quote();
        }
    }

    public override string GetChangeDirCommand(string path) => $"cd /D {path.QuoteNE()}";
}
