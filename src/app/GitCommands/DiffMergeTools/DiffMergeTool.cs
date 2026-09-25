namespace GitCommands.DiffMergeTools;

/// <summary>
/// A base class for diff/merge tool configurations.
/// </summary>
internal abstract class DiffMergeTool
{
    private const string DefaultDiffCommand = "\"$LOCAL\" \"$REMOTE\"";
    private const string DefaultMergeCommand = "\"$LOCAL\" \"$REMOTE\" \"$BASE\" \"$MERGED\"";

    /// <summary>
    /// Gets the diff command will be invoked by git.
    /// <see langword="null"/> or <see cref="string.Empty"/> if <see cref="IsDiffTool"/> is <see langword="false"/>.
    /// </summary>
    public virtual string DiffCommand => DefaultDiffCommand;

    /// <summary>
    /// Gets the diff/merge exe file name.
    /// </summary>
    public abstract string ExeFileName { get; }

    /// <summary>
    /// Indicates whether the tool can be used as a diff tool.
    /// Default: <see langword="true"/>.
    /// </summary>
    public virtual bool IsDiffTool => true;

    /// <summary>
    /// Indicates whether the tool can be used as a merge tool.
    /// Default: <see langword="true"/>.
    /// </summary>
    public virtual bool IsMergeTool => true;

    /// <summary>
    /// Gets the merge command will be invoked by git.
    /// <see langword="null"/> or <see cref="string.Empty"/> if <see cref="IsMergeTool"/> is <see langword="false"/>.
    /// </summary>
    public virtual string MergeCommand => DefaultMergeCommand;

    /// <summary>
    /// Gets the name of the diff/merge tool that will be shown to the user.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the list of possible locations of the diff/merge tool.
    /// These location will be used to help the user to automatically locate the tool.
    /// </summary>
    public abstract IEnumerable<string> SearchPaths { get; }

    /// <summary>Whether the tool exists on this system (the tools of Windows only are not offered elsewhere).</summary>
    public virtual bool IsAvailable => true;

    /// <summary>
    ///  Gets the paths of the program of the tool inside its application bundle on macOS, relative to an Applications
    ///  folder (<c>/Applications</c>, <c>~/Applications</c>), e.g. <c>kdiff3.app/Contents/MacOS/kdiff3</c>.
    /// </summary>
    public virtual IEnumerable<string> MacOSBundlePaths => [];

    /// <summary>
    ///  Whether the program returns at once unless its output is a pipe (FileMerge's <c>opendiff</c>): the command that
    ///  git runs then ends with <c>| cat</c>, as git's own definition of the tool, so that git waits for the tool.
    /// </summary>
    public virtual bool WaitsOnlyWhenPiped => false;
}
