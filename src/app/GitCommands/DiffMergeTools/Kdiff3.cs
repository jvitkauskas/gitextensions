namespace GitCommands.DiffMergeTools;

internal sealed class Kdiff3 : DiffMergeTool
{
    /// <inheritdoc />
    public override string ExeFileName => OperatingSystem.IsWindows() ? "kdiff3.exe" : "kdiff3";

    /// <inheritdoc />
    public override string MergeCommand => "\"$BASE\" \"$LOCAL\" \"$REMOTE\" -o \"$MERGED\"";

    /// <inheritdoc />
    public override string Name => "kdiff3";

    /// <inheritdoc />
    public override IEnumerable<string> SearchPaths => new[]
    {
        // regkdiff3path
        @"KDiff3",
        @"KDiff3\bin"
    };

    /// <inheritdoc />
    public override IEnumerable<string> MacOSBundlePaths =>
    [
        "kdiff3.app/Contents/MacOS/kdiff3",
    ];
}
