using GitCommands;

namespace GitUI.Shells;

/// <summary>A shell of Linux or macOS (bash, zsh, fish, the shell of the user), interactive in the built-in terminal.</summary>
public sealed class UnixShell : ShellDescriptor
{
    public UnixShell(string name, string path)
    {
        Name = name;
        Icon = EmbeddedIcons.Get("Console");
        ExecutableName = name;
        ExecutablePath = path;
        ExecutableCommandLine = $"{path.Quote()} -i";
    }

    public override string GetChangeDirCommand(string path) => $"cd {path.QuoteNE()}";
}
