using GitExtensions.Extensibility;

namespace GitCommands.Git;

/// <summary>
///  gitk and git gui off Windows (docs/avalonia-port/CROSS-PLATFORM.md, phase 3): Tcl/Tk programs that are not always
///  installed with git (on macOS: <c>brew install git-gui</c>). They are looked for next to git, in its exec path, on the
///  PATH and in the usual folders: the PATH of an application started from the Finder has no <c>/opt/homebrew/bin</c>.
/// </summary>
public static class GitGuiTools
{
    public const string GitK = "gitk";
    public const string GitGui = "git-gui";

    /// <summary>The full path of the program <paramref name="name"/>, or <see langword="null"/> if it is not installed.</summary>
    public static string? Find(string name, IExecutable gitExecutable)
        => Find(name, AppSettings.GitCommand, () => GetExecPath(gitExecutable), File.Exists);

    internal static string? Find(string name, string gitCommand, Func<string?> getExecPath, Func<string, bool> fileExists)
    {
        string? gitFolder = Path.IsPathRooted(gitCommand) ? Path.GetDirectoryName(gitCommand) : null;
        if (FindIn(gitFolder, name, fileExists) is { } nextToGit)
        {
            return nextToGit;
        }

        return FindIn(getExecPath(), name, fileExists) ?? PathUtil.FindInUnixProgramFolders(name, fileExists);
    }

    private static string? FindIn(string? folder, string name, Func<string, bool> fileExists)
        => !string.IsNullOrEmpty(folder) && fileExists(Path.Join(folder, name)) ? Path.Join(folder, name) : null;

    // The folder of the git commands (e.g. /usr/lib/git-core, where Linux distributions put git-gui).
    private static string? GetExecPath(IExecutable gitExecutable)
    {
        try
        {
            return gitExecutable.GetOutput("--exec-path").Trim();
        }
        catch (Exception)
        {
            return null;
        }
    }
}
