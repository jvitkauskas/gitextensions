namespace GitCommands.DiffMergeTools;

internal sealed class Smerge : DiffMergeTool
{
    /// <inheritdoc />
    public override string ExeFileName => OperatingSystem.IsWindows() ? "smerge.exe" : "smerge";

    /// <inheritdoc />
    public override string DiffCommand => "mergetool \"$LOCAL\" \"$REMOTE\" -o=\"$MERGED\"";

    /// <inheritdoc />
    public override string MergeCommand => "mergetool \"$BASE\" \"$LOCAL\" \"$REMOTE\" -o=\"$MERGED\"";

    /// <inheritdoc />
    public override string Name => "smerge";

    /// <inheritdoc />
    public override IEnumerable<string> SearchPaths => new[]
    {
        @"Sublime Merge\"
    };

    /// <inheritdoc />
    public override IEnumerable<string> MacOSBundlePaths =>
    [
        "Sublime Merge.app/Contents/SharedSupport/bin/smerge",
    ];
}
