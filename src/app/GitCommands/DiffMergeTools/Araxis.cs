namespace GitCommands.DiffMergeTools;

internal sealed class Araxis : DiffMergeTool
{
    /// <inheritdoc />
    public override string ExeFileName => OperatingSystem.IsWindows() ? "Compare.exe" : "compare";

    /// <inheritdoc />
    public override string MergeCommand => "/merge /wait /a2 /3 \"$LOCAL\" \"$BASE\" \"$REMOTE\" \"$MERGED\"";

    /// <inheritdoc />
    /// <remarks>Araxis Merge exists for Windows and macOS (on Linux, <c>compare</c> is the one of ImageMagick).</remarks>
    public override bool IsAvailable => OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();

    /// <inheritdoc />
    public override string Name => "araxis";

    /// <inheritdoc />
    public override IEnumerable<string> SearchPaths => new[]
    {
        @"Araxis\"
    };
}
