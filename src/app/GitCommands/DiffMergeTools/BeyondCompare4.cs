namespace GitCommands.DiffMergeTools;

internal sealed class BeyondCompare4 : DiffMergeTool
{
    /// <inheritdoc />
    /// <remarks>On macOS the command line tool is <c>bcomp</c> (it waits, as <c>bcomp.exe</c>); <c>bcompare</c> on Linux.</remarks>
    public override string ExeFileName => OperatingSystem.IsWindows() ? "bcomp.exe" : OperatingSystem.IsMacOS() ? "bcomp" : "bcompare";

    /// <inheritdoc />
    public override string Name => "bc";

    /// <inheritdoc />
    public override IEnumerable<string> SearchPaths => new[]
    {
        @"Beyond Compare 4 (x86)\",
        @"Beyond Compare 4\"
    };

    /// <inheritdoc />
    public override IEnumerable<string> MacOSBundlePaths =>
    [
        "Beyond Compare.app/Contents/MacOS/bcomp",
    ];
}
