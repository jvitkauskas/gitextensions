using GitCommands;
using GitExtensions.Extensibility;

namespace GitUI.ScriptsEngine;

public static class PowerShellHelper
{
    internal static void RunPowerShell(string command, string? argument, string workingDir, bool runInBackground)
    {
        // Windows PowerShell in a window of its own; elsewhere PowerShell (pwsh), which has no window to stay in.
        string filename = OperatingSystem.IsWindows() ? "powershell.exe" : "pwsh";
        runInBackground |= !OperatingSystem.IsWindows();
        string arguments = (runInBackground ? "" : "-NoExit") + " -ExecutionPolicy Unrestricted -Command \"" + command + " " + argument + "\"";
        EnvironmentConfiguration.SetEnvironmentVariables();

        IExecutable executable = new Executable(filename, workingDir);
        executable.Start(arguments, createWindow: !runInBackground);
    }
}
