namespace GitExtensions.Extensibility.Git;

public class GitUIPostActionEventArgs : GitUIEventArgs
{
    public bool ActionDone { get; }

    public GitUIPostActionEventArgs(IWin32Window? ownerForm, IGitUICommands gitUICommands, bool actionDone)
        : base(ownerForm, gitUICommands)
    {
        ActionDone = actionDone;
    }

    /// <summary>Creates the arguments with the owner window of plugin API v2 (see <see cref="GitUIEventArgs(WindowOwner, IGitUICommands, Lazy{IReadOnlyList{IGitRef}})"/>).</summary>
    public GitUIPostActionEventArgs(WindowOwner owner, IGitUICommands gitUICommands, bool actionDone)
        : base(owner, gitUICommands)
    {
        ActionDone = actionDone;
    }
}
