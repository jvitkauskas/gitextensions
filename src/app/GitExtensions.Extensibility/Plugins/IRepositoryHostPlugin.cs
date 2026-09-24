namespace GitExtensions.Extensibility.Plugins;

/// <summary>
///  Define that the plugin provides features (clone, ...) related to an online git hosting service.
///  A plugin implementing this interface **must** have the `Export` attribute declared this way:
///  [Export(typeof(IRepositoryHostPlugin))]
/// </summary>
public interface IRepositoryHostPlugin : IGitPlugin
{
    IReadOnlyList<IHostedRepository> SearchForRepository(string search);
    IReadOnlyList<IHostedRepository> GetRepositoriesOfUser(string user);
    IHostedRepository GetRepository(string user, string repositoryName);

    IReadOnlyList<IHostedRepository> GetMyRepos();

    /// <summary>Adds the items of the plugin to the WinForms context menu of the blame (plugin API v1).</summary>
    /// <remarks>
    ///  Plugin API v2: implement <c>GitUIPluginInterfaces.RepositoryHosts.IBlameContextMenuProvider</c> instead, whose menu
    ///  model the host renders in any UI framework; the host then does not call this method.
    /// </remarks>
    [Obsolete("Use IBlameContextMenuProvider.GetBlameContextMenuItems (plugin API v2).")]
    void ConfigureContextMenu(ContextMenuStrip contextMenu);

    bool ConfigurationOk { get; }

    bool GitModuleIsRelevantToMe();
    IReadOnlyList<IHostedRemote> GetHostedRemotesForModule();
    string? OwnerLogin { get; }

    Task<string?> AddUpstreamRemoteAsync();
}
