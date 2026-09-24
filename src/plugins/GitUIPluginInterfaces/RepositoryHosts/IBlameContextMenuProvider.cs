using GitExtensions.Extensibility.Plugins;

namespace GitUIPluginInterfaces.RepositoryHosts;

/// <summary>
///  Plugin API v2 of <see cref="IRepositoryHostPlugin.ConfigureContextMenu"/>: a repository host plugin that implements it adds
///  items to the context menu of the blame as a menu model, which the host renders (in WinForms or in Avalonia), instead of
///  changing a WinForms <c>ContextMenuStrip</c>. The host then does not call <see cref="IRepositoryHostPlugin.ConfigureContextMenu"/>.
/// </summary>
public interface IBlameContextMenuProvider
{
    /// <summary>
    ///  The items to add at the end of the context menu of the blame, each time the menu opens (on the UI thread; keep it
    ///  quick, e.g. cache what it reads from git).
    /// </summary>
    /// <param name="context">The file, the line and the blamed revision the menu is opened for.</param>
    IReadOnlyList<PluginMenuItem> GetBlameContextMenuItems(GitBlameContext context);
}
