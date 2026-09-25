namespace GitCommands;

/// <summary>
///  The program of <c>SSH_ASKPASS</c> off Windows (docs/avalonia-port/CROSS-PLATFORM.md, phase 4): ssh and git run it with
///  the prompt as its only argument, so a small sh script runs <c>GitExtensions askpass &lt;prompt&gt;</c>, whose prompt
///  writes the answer to its output. Windows has GitExtSshAskPass.exe.
/// </summary>
public static class AskPassScript
{
    public const string FileName = "GitExtensions-askpass.sh";

    /// <summary>The content of the script for the executable of Git Extensions.</summary>
    public static string GetContent(string gitExtensionsExecutable)
        => $"#!/bin/sh\nexec '{gitExtensionsExecutable.Replace("'", "'\\''")}' askpass \"$@\"\n";

    /// <summary>Writes the script in <paramref name="folder"/> (if it changed) and makes it executable; its path.</summary>
    public static string Write(string folder, string gitExtensionsExecutable)
    {
        string path = Path.Join(folder, FileName);
        string content = GetContent(gitExtensionsExecutable);
        if (!File.Exists(path) || File.ReadAllText(path) != content)
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(path, content);
        }

        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                | UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }

        return path;
    }
}
