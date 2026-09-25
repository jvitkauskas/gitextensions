namespace GitCommands.DiffMergeTools;

/// <summary>FileMerge of Xcode, through its command line tool <c>opendiff</c> (macOS only; git's name for it).</summary>
internal sealed class FileMerge : DiffMergeTool
{
    /// <inheritdoc />
    public override string ExeFileName => "opendiff";

    /// <inheritdoc />
    public override string MergeCommand => "\"$LOCAL\" \"$REMOTE\" -ancestor \"$BASE\" -merge \"$MERGED\"";

    /// <inheritdoc />
    public override bool IsAvailable => OperatingSystem.IsMacOS();

    /// <inheritdoc />
    public override string Name => "opendiff";

    /// <inheritdoc />
    /// <remarks><c>/usr/bin/opendiff</c> is on the PATH when Xcode or its command line tools are installed.</remarks>
    public override IEnumerable<string> SearchPaths => [];

    /// <inheritdoc />
    public override bool WaitsOnlyWhenPiped => true;
}
