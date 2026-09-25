using GitCommands;
using GitCommands.Git;
using GitExtensions.Extensibility.Git;
using Microsoft.Win32;

namespace GitUI.CommandsDialogs.SettingsDialog;

public class CheckSettingsLogic
{
    public readonly CommonLogic CommonLogic;
    private IGitModule? Module => CommonLogic.Module;

    public CheckSettingsLogic(CommonLogic commonLogic)
    {
        CommonLogic = commonLogic;
    }

    public bool AutoSolveAllSettings()
    {
        if (!OperatingSystem.IsWindows())
        {
            // Git and the editor (the file editor of Git Extensions if git has none): the Linux tools and the shell
            // extension are Windows-only.
            return SolveGitCommand() && SolveEditor(CommonLogic);
        }

        bool valid = SolveGitCommand();
        valid = SolveLinuxToolsDir() && valid;
        valid = SolveGitExtensionsDir() && valid;
        valid = SolveEditor(CommonLogic) && valid;

        CommonLogic.GitConfigSettingsSet.Save();
        CommonLogic.DistributedSettingsSet.Save();

        return valid;
    }

    public static bool SolveEditor(CommonLogic commonLogic)
    {
        string? editor = commonLogic.GetGlobalEditor();

        if (string.IsNullOrEmpty(editor))
        {
            Environment.SetEnvironmentVariable(CommonLogic.AmbientGitEditorEnvVariableName, AppSettings.FileEditorCommand);
        }

        return true;
    }

    public static bool SolveLinuxToolsDir(string? possibleNewPath = null)
    {
        if (!OperatingSystem.IsWindows())
        {
            AppSettings.LinuxToolsDir = string.Empty;
            return true;
        }

        string gitpath = AppSettings.GitCommandValue;
        if (!string.IsNullOrWhiteSpace(possibleNewPath))
        {
            gitpath = possibleNewPath.Trim();
        }

        foreach (string toolsPath in new[] { @"usr\bin\", @"bin\" })
        {
            string linuxToolsPath = gitpath.Replace(@"\cmd\git.exe", @"\" + toolsPath)
                .Replace(@"\cmd\git.cmd", @"\" + toolsPath)
                .Replace(@"\bin\git.exe", @"\" + toolsPath);

            if (ContainsSh(linuxToolsPath))
            {
                AppSettings.LinuxToolsDir = linuxToolsPath;
                return true;
            }

            if (CheckIfFileIsInPath("sh.exe") || CheckIfFileIsInPath("sh"))
            {
                if (ContainsSh(AppSettings.LinuxToolsDir))
                {
                    return true;
                }

                AppSettings.LinuxToolsDir = string.Empty;
                return true;
            }

            foreach (string path in GetGitLocations())
            {
                linuxToolsPath = path + toolsPath;
                if (ContainsSh(gitpath))
                {
                    AppSettings.LinuxToolsDir = gitpath;
                    return true;
                }
            }
        }

        return false;

        static bool ContainsSh(string path) => Directory.Exists(path) && (File.Exists(path + "sh.exe") || File.Exists(path + "sh"));
    }

    private static IEnumerable<string> GetGitLocations()
    {
        string? envVariable = Environment.GetEnvironmentVariable("GITEXT_GIT");
        if (!string.IsNullOrEmpty(envVariable))
        {
            yield return envVariable;
        }

        if (OperatingSystem.IsWindows())
        {
            yield return
                CommonLogic.GetRegistryValue(Registry.LocalMachine,
                                 "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Git_is1", "InstallLocation");
        }

        string? programFiles = Environment.GetEnvironmentVariable("ProgramFiles");
        string? programFilesX86 = null;
        if (IntPtr.Size == 8
            || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432")))
        {
            programFilesX86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
        }

        if (programFiles is not null)
        {
            yield return programFiles + @"\Git\";
        }

        if (programFilesX86 is not null)
        {
            yield return programFilesX86 + @"\Git\";
        }

        if (programFiles is not null)
        {
            yield return programFiles + @"\msysgit\";
        }

        if (programFilesX86 is not null)
        {
            yield return programFilesX86 + @"\msysgit\";
        }

        yield return @"C:\msysgit\";

        // cygwin has old git version on windows and bash has a lot of bugs
        yield return @"C:\cygwin\";
        yield return Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs", "Git\\");
    }

    public static bool SolveGitExtensionsDir()
    {
        string? fileName = AppSettings.GetGitExtensionsDirectory();

        if (Directory.Exists(fileName))
        {
            AppSettings.SetInstallDir(fileName!);
            return true;
        }

        return false;
    }

    public static bool SolveGitCommand(string? possibleNewPath = null)
    {
        if (OperatingSystem.IsWindows())
        {
            foreach (string command in GetWindowsCommandLocations())
            {
                if (TestGitCommand(command))
                {
                    return true;
                }
            }

            return false;
        }

        foreach (string command in GetUnixGitCandidates(possibleNewPath, AppSettings.GitCommandValue, File.Exists))
        {
            if (TestGitCommand(command))
            {
                return true;
            }
        }

        return false;

        bool TestGitCommand(string command)
        {
            try
            {
                // Use cached version if possible
                if (AppSettings.GitCommand == command && GitVersion.Current?.IsUnknown is false)
                {
                    return true;
                }

                string output = new Executable(command).GetOutput(arguments: "--version");
                if (!string.IsNullOrEmpty(output))
                {
                    if (command is not null)
                    {
                        AppSettings.GitCommandValue = command;
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                // Ignore exception, we are trying to find a way to execute git.exe
            }

            return false;
        }

        IEnumerable<string> GetWindowsCommandLocations()
        {
            if (File.Exists(possibleNewPath))
            {
                yield return possibleNewPath!;
            }

            if (File.Exists(AppSettings.GitCommandValue))
            {
                yield return AppSettings.GitCommandValue;
            }

            foreach (string path in GetGitLocations())
            {
                if (Directory.Exists(path + @"bin\"))
                {
                    yield return path + @"bin\git.exe";
                }
            }

            foreach (string path in GetGitLocations())
            {
                if (Directory.Exists(path + @"cmd\"))
                {
                    yield return path + @"cmd\git.exe";
                    yield return path + @"cmd\git.cmd";
                }
            }

            yield return "git";
            yield return "git.cmd";
        }
    }

    /// <summary>The folders where git is installed on Linux and macOS (Homebrew, MacPorts), after the PATH.</summary>
    private static readonly string[] _unixGitFolders = ["/usr/bin", "/usr/local/bin", "/opt/homebrew/bin", "/opt/local/bin"];

    /// <summary>
    ///  The git commands to try off Windows (docs/avalonia-port/CROSS-PLATFORM.md, phase 3): the path just chosen, the
    ///  configured path (kept if it exists), git on the PATH, then git in the usual folders.
    /// </summary>
    internal static IEnumerable<string> GetUnixGitCandidates(string? possibleNewPath, string configured, Func<string, bool> fileExists)
    {
        if (!string.IsNullOrEmpty(possibleNewPath) && fileExists(possibleNewPath))
        {
            yield return possibleNewPath;
        }

        if (Path.IsPathRooted(configured) && fileExists(configured))
        {
            yield return configured;
        }

        yield return "git";

        foreach (string folder in _unixGitFolders)
        {
            string git = Path.Join(folder, "git");
            if (fileExists(git))
            {
                yield return git;
            }
        }
    }

    public static bool CheckIfFileIsInPath(string fileName)
    {
        return PathUtil.TryFindFullPath(fileName, out _);
    }

    public bool CanFindGitCmd()
    {
        return !string.IsNullOrEmpty(Module?.GitExecutable.GetOutput(arguments: "--version"));
    }
}
