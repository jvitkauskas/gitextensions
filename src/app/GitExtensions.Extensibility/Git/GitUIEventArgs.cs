using System.ComponentModel;

namespace GitExtensions.Extensibility.Git;

public class GitUIEventArgs : CancelEventArgs
{
    private readonly IFilteredGitRefsProvider _getRefs;
    private readonly WindowOwner? _owner;

    public GitUIEventArgs(IWin32Window? ownerForm, IGitUICommands gitUICommands, Lazy<IReadOnlyList<IGitRef>>? getRefs = null)
        : base(cancel: false)
    {
        OwnerForm = ownerForm;
        GitUICommands = gitUICommands;
        if (getRefs is null)
        {
            _getRefs = new FilteredGitRefsProvider(GitModule);
        }
        else
        {
            _getRefs = new FilteredGitRefsProvider(getRefs);
        }
    }

    /// <summary>
    ///  Creates the arguments with the owner window of plugin API v2; <see cref="OwnerForm"/> is the same window for the plugins
    ///  of API v1 (its WinForms form, or a wrapper of its handle, e.g. of an Avalonia window).
    /// </summary>
    public GitUIEventArgs(WindowOwner owner, IGitUICommands gitUICommands, Lazy<IReadOnlyList<IGitRef>>? getRefs = null)
        : this(owner.ToWin32Window(), gitUICommands, getRefs)
    {
        _owner = owner;
    }

    public IGitUICommands GitUICommands { get; }

    /// <summary>The window that owns the dialogs of the plugin, as a WinForms owner (plugin API v1).</summary>
    /// <remarks>Plugin API v2: use <see cref="Owner"/>, which needs no WinForms type.</remarks>
    public IWin32Window? OwnerForm { get; }

    /// <summary>
    ///  The window that owns the dialogs and message boxes of the plugin (plugin API v2), e.g. for
    ///  <see cref="PluginMessageBoxes"/>; <see cref="WindowOwner.None"/> if none.
    /// </summary>
    /// <remarks>
    ///  With the arguments created the v1 way, it is the window of <see cref="OwnerForm"/> (read when first asked, as reading
    ///  the handle of a WinForms control creates it).
    /// </remarks>
    public WindowOwner Owner => _owner ?? OwnerForm.ToWindowOwner();

    public IGitModule GitModule => GitUICommands.Module;

    public IReadOnlyList<IGitRef> GetRefs(RefsFilter filter) => _getRefs.GetRefs(filter);
}
