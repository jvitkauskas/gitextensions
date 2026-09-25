namespace GitCommands.DiffMergeTools;

public readonly struct DiffMergeToolConfiguration
{
    public DiffMergeToolConfiguration(string exeFileName, string path, string? diffCommand, string? mergeCommand)
        : this(exeFileName, path, diffCommand, mergeCommand, waitsOnlyWhenPiped: false)
    {
    }

    /// <param name="waitsOnlyWhenPiped">Whether the full commands (run by git through a shell) end with <c>| cat</c> (see <see cref="PipeSuffix"/>).</param>
    public DiffMergeToolConfiguration(string exeFileName, string path, string? diffCommand, string? mergeCommand, bool waitsOnlyWhenPiped)
    {
        ExeFileName = exeFileName;
        Path = path.ToPosixPath();
        DiffCommand = diffCommand ?? string.Empty;
        MergeCommand = mergeCommand ?? string.Empty;

        string suffix = waitsOnlyWhenPiped ? PipeSuffix : string.Empty;
        FullDiffCommand = string.IsNullOrWhiteSpace(DiffCommand) ? string.Empty : $"\"{Path}\" {DiffCommand}{suffix}";
        FullMergeCommand = string.IsNullOrWhiteSpace(MergeCommand) ? string.Empty : $"\"{Path}\" {MergeCommand}{suffix}";
    }

    /// <summary>
    ///  The end of the full commands of a tool that returns at once unless its output is a pipe (FileMerge's
    ///  <c>opendiff</c>), so that git waits for the tool, as git's own definition of <c>opendiff</c> does.
    /// </summary>
    public const string PipeSuffix = " | cat";

    public string DiffCommand { get; }
    public string MergeCommand { get; }
    public string ExeFileName { get; }
    public string Path { get; }

    public string FullDiffCommand { get; }
    public string FullMergeCommand { get; }
}
