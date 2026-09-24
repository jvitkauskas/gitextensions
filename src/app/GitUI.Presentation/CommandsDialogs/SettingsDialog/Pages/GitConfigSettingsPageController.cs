using GitCommands;

namespace GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

/// <summary>Port of <c>GitConfigSettingsPageController</c>: the directory where the file picker of a diff or merge tool opens.</summary>
internal sealed class GitConfigSettingsPageController
{
    public string GetInitialDirectory(string? path, string? toolPreferredPath) =>
        CalculateInitialDirectory(path) ??
        CalculateInitialDirectory(toolPreferredPath) ??
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

    private static string? CalculateInitialDirectory(string? suppliedPath)
    {
        if (string.IsNullOrWhiteSpace(suppliedPath))
        {
            return null;
        }

        // The path can be either a folder or a file. If the path is a folder but lacks the trailing slash,
        // Path.GetDirectoryName will return the parent directory, so the supplied directory is slash-terminated.
        if (Directory.Exists(suppliedPath))
        {
            suppliedPath = suppliedPath.EnsureTrailingPathSeparator();
        }

        // Path.GetDirectoryName returns directory information for path, or null if path denotes a root directory or is null.
        // Returns Empty if path does not contain directory information.
        string initialDirectory = Path.GetDirectoryName(suppliedPath) ?? suppliedPath;
        if (!string.IsNullOrWhiteSpace(initialDirectory) && Directory.Exists(initialDirectory))
        {
            return initialDirectory.EnsureTrailingPathSeparator();
        }

        return null;
    }
}
