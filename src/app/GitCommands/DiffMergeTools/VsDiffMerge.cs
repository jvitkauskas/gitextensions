using Microsoft.Win32;

namespace GitCommands.DiffMergeTools;

internal sealed class VsDiffMerge : DiffMergeTool
{
    private const string ExeName = "vsdiffmerge.exe";

    /// <inheritdoc />
    public override string MergeCommand => "/m \"$REMOTE\" \"$LOCAL\" \"$BASE\" \"$MERGED\"";

    /// <inheritdoc />
    public override string ExeFileName => ExeName;

    /// <inheritdoc />
    public override bool IsAvailable => OperatingSystem.IsWindows();

    /// <inheritdoc />
    public override string Name => "vsdiffmerge";

    /// <inheritdoc />
    public override IEnumerable<string> SearchPaths => GetSearchPaths();

    private static IEnumerable<string> GetSearchPaths()
    {
        if (!OperatingSystem.IsWindows())
        {
            yield break;
        }

        // VS 2017 and later use instance-based installations, discoverable through vswhere.
        string vswhere = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Microsoft Visual Studio", "Installer", "vswhere.exe");
        if (File.Exists(vswhere))
        {
            string installations = new Executable(vswhere).GetOutput("-prerelease -products * -property installationPath");
            foreach (string folder in GetModernSearchPaths(installations))
            {
                yield return folder;
            }
        }

        string[] vsVersions = ["14.0", "12.0", "11.0"];

        foreach (string version in vsVersions)
        {
            string registryKeyString = $@"SOFTWARE{(Environment.Is64BitProcess ? @"\Wow6432Node\" : "\\")}Microsoft\VisualStudio\{version}";
            using RegistryKey? localMachineKey = Registry.LocalMachine.OpenSubKey(registryKeyString);
            string? path = localMachineKey?.GetValue("InstallDir") as string;
            if (!string.IsNullOrEmpty(path))
            {
                yield return path;
            }
        }
    }

    internal static IEnumerable<string> GetModernSearchPaths(string installations)
        => installations.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(path => Path.Join(path, "Common7", "IDE", "CommonExtensions", "Microsoft", "TeamFoundation", "Team Explorer"));
}
