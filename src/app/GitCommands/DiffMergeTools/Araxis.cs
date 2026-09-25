namespace GitCommands.DiffMergeTools;

internal sealed class Araxis : DiffMergeTool
{
    /// <inheritdoc />
    public override string ExeFileName => OperatingSystem.IsWindows() ? "Compare.exe" : "compare";

    /// <inheritdoc />
    /// <remarks>The command line tools of macOS take their options with <c>-</c>.</remarks>
    public override string MergeCommand => OperatingSystem.IsMacOS()
        ? "-merge -wait -a2 -3 \"$LOCAL\" \"$BASE\" \"$REMOTE\" \"$MERGED\""
        : "/merge /wait /a2 /3 \"$LOCAL\" \"$BASE\" \"$REMOTE\" \"$MERGED\"";

    /// <inheritdoc />
    /// <remarks>Araxis Merge exists for Windows and macOS (on Linux, <c>compare</c> is the one of ImageMagick).</remarks>
    public override bool IsAvailable => OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();

    /// <inheritdoc />
    public override string Name => "araxis";

    /// <inheritdoc />
    public override IEnumerable<string> SearchPaths => new[]
    {
        @"Araxis\",
        @"Araxis\Araxis Merge",
        Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Apps", "Araxis", "Araxis Merge")
    };

    /// <inheritdoc />
    /// <remarks>Its command line tools are in <c>Utilities</c>; the <c>compare</c> of the PATH may be the one of ImageMagick.</remarks>
    public override IEnumerable<string> MacOSBundlePaths =>
    [
        "Araxis Merge.app/Contents/Utilities/compare",
    ];
}
