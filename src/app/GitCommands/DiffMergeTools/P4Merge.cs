namespace GitCommands.DiffMergeTools;

internal sealed class P4Merge : DiffMergeTool
{
    /// <inheritdoc />
    public override string ExeFileName => OperatingSystem.IsWindows() ? "p4merge.exe" : "p4merge";

    /// <inheritdoc />
    public override string MergeCommand => "\"$BASE\" \"$LOCAL\" \"$REMOTE\" \"$MERGED\"";

    /// <inheritdoc />
    public override string Name => "p4merge";

    /// <inheritdoc />
    public override IEnumerable<string> SearchPaths => new[]
    {
        @"Perforce\"
    };

    /// <inheritdoc />
    /// <remarks>The launcher that Perforce documents for the command line, rather than <c>Contents/MacOS/p4merge</c>.</remarks>
    public override IEnumerable<string> MacOSBundlePaths =>
    [
        "p4merge.app/Contents/Resources/launchp4merge",
    ];
}
