using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the plugin and repository host menus of the main window; ids match <c>FormBrowse</c>.</summary>
public sealed class BrowsePluginStrings : ViewStrings
{
    public BrowsePluginStrings()
        : base("FormBrowse")
    {
        PluginsMenu = Add("pluginsToolStripMenuItem", "Text", "&Plugins");
        PluginsLoading = Add("pluginsLoadingToolStripMenuItem", "Text", "Loading...");
        PluginSettings = Add("pluginSettingsToolStripMenuItem", "Text", "Plugins &settings...");
        ForkClone = Add("_forkCloneRepositoryToolStripMenuItem", "Text", "&Fork/Clone repository...");
        ViewPullRequests = Add("_viewPullRequestsToolStripMenuItem", "Text", "View &pull requests...");
        CreatePullRequest = Add("_createPullRequestsToolStripMenuItem", "Text", "&Create pull requests...");
        AddUpstreamRemote = Add("_addUpstreamRemoteToolStripMenuItem", "Text", "&Add upstream remote");
        NavigateMenu = Add("navigateToolStripMenuItem", "Text", "&Navigate");
        ViewMenu = Add("viewToolStripMenuItem", "Text", "&View");
        NoRepositoryHostFound = Add("_noReposHostFound", "Text", "Could not find any relevant repository hosts for the currently open repository.");
        NoRepositoryHostPluginLoaded = Add("_noReposHostPluginLoaded", "Text", "No repository host plugin loaded.");
    }

    public TranslatedText PluginsMenu { get; }

    public TranslatedText PluginsLoading { get; }

    public TranslatedText PluginSettings { get; }

    public TranslatedText ForkClone { get; }

    public TranslatedText ViewPullRequests { get; }

    public TranslatedText CreatePullRequest { get; }

    public TranslatedText AddUpstreamRemote { get; }

    public TranslatedText NavigateMenu { get; }

    public TranslatedText ViewMenu { get; }

    public TranslatedText NoRepositoryHostFound { get; }

    public TranslatedText NoRepositoryHostPluginLoaded { get; }
}

/// <summary>A plugin of the Plugins menu (an <c>IGitPlugin</c>).</summary>
/// <param name="Icon">The image: an asset name or PNG data.</param>
/// <param name="NeedsRepository">Whether it runs only in a repository (<c>IGitPluginForRepository</c>).</param>
/// <param name="Plugin">The plugin, for <see cref="IBrowsePluginsHost.RunPlugin"/>.</param>
public sealed record BrowsePlugin(string Name, object? Icon, bool NeedsRepository, object Plugin);

/// <summary>The commands of the repository host menu (<c>_repositoryHostsToolStripMenuItem</c>).</summary>
public enum RepositoryHostCommand
{
    ForkClone,
    ViewPullRequests,
    CreatePullRequest,
    AddUpstreamRemote,
}

/// <summary>What the Plugins and repository host menus need from the application (<c>RegisterPlugins</c>).</summary>
public interface IBrowsePluginsHost
{
    /// <summary>Raised when the plugins are loaded (they load in the background, as <c>InitializeAndRegisterAllPluginsAsync</c>).</summary>
    event EventHandler? PluginsChanged;

    /// <summary>The plugins, or <see langword="null"/> while they are loading.</summary>
    IReadOnlyList<BrowsePlugin>? Plugins { get; }

    /// <summary>The name of the first git hosting plugin (the title of its menu), none without one.</summary>
    string? RepositoryHostName { get; }

    /// <summary>As the click of a plugin item: <c>IGitPlugin.Execute</c>, and the repository is refreshed if it changed.</summary>
    void RunPlugin(BrowsePlugin plugin);

    /// <summary>As <c>PluginSettingsToolStripMenuItemClick</c>.</summary>
    void OpenPluginSettings();

    void RunRepositoryHostCommand(RepositoryHostCommand command);
}

/// <summary>The Plugins, repository host, Navigate and View menus of the main window.</summary>
public sealed partial class BrowseViewModel
{
    private const string PluginManagerName = "Plugin Manager";
    private IBrowsePluginsHost? _pluginsHost;

    public BrowsePluginStrings PluginStrings { get; } = ViewStrings.Load<BrowsePluginStrings>();

    /// <summary>The items of the Navigate menu (<c>NavigateMenuCommands</c> of the grid), built when it is shown.</summary>
    public Func<IReadOnlyList<MenuModelItem>>? NavigateMenuProvider { get; init; }

    /// <summary>The items of the View menu (<c>ViewMenuCommands</c> of the grid), built when it is shown.</summary>
    public Func<IReadOnlyList<MenuModelItem>>? ViewMenuProvider { get; init; }

    /// <summary>Raised when <see cref="Menus"/> changed, e.g. once the plugins are loaded.</summary>
    public event EventHandler? MenusChanged;

    /// <summary>The items of a menu built by the application (<see cref="BrowseSubmenu.Navigate"/>, <see cref="BrowseSubmenu.View"/>).</summary>
    public IReadOnlyList<MenuModelItem> GetModelSubmenuItems(BrowseSubmenu submenu) => submenu switch
    {
        BrowseSubmenu.Navigate => NavigateMenuProvider?.Invoke() ?? [],
        BrowseSubmenu.View => ViewMenuProvider?.Invoke() ?? [],
        _ => [],
    };

    private void InitializePlugins()
    {
        _pluginsHost = _host as IBrowsePluginsHost;
        if (_pluginsHost is not null)
        {
            _pluginsHost.PluginsChanged += (_, _) =>
            {
                Menus = AddDynamicMenus(_baseMenus);
                MenusChanged?.Invoke(this, EventArgs.Empty);

                // As FormBrowse once the git hosters are loaded: their "Clone fork" links on the dashboard.
                Dashboard?.RefreshStartLinks();
            };
        }
    }

    // As FormBrowse: Start, (Dashboard,) Repository, Navigate, View, Commands, the repository host, Plugins, Tools, Help.
    private IReadOnlyList<BrowseMenuItem> AddDynamicMenus(IReadOnlyList<BrowseMenuItem> menus)
    {
        List<BrowseMenuItem> result = [.. menus];
        int repository = result.FindIndex(m => m.Header == Strings.RepositoryMenu.AccessKeyText);
        if (repository >= 0 && !IsDashboard)
        {
            result.InsertRange(repository + 1,
            [
                new(PluginStrings.NavigateMenu.AccessKeyText, null) { Submenu = BrowseSubmenu.Navigate },
                new(PluginStrings.ViewMenu.AccessKeyText, null) { Submenu = BrowseSubmenu.View },
            ]);
        }

        if (_pluginsHost is null)
        {
            return result;
        }

        int tools = result.FindIndex(m => m.Header == Strings.ToolsMenu.AccessKeyText);
        tools = tools < 0 ? result.Count : tools;
        List<BrowseMenuItem> dynamicMenus = [];

        // Shown when a git hosting plugin is loaded, also on the dashboard (UpdateRepositoryHostsMenu).
        if (_pluginsHost.RepositoryHostName is { } hostName)
        {
            dynamicMenus.Add(new(hostName.Replace("_", "__"), null, Children:
            [
                HostItem(PluginStrings.ForkClone, RepositoryHostCommand.ForkClone),
                HostItem(PluginStrings.ViewPullRequests, RepositoryHostCommand.ViewPullRequests),
                HostItem(PluginStrings.CreatePullRequest, RepositoryHostCommand.CreatePullRequest),
                HostItem(PluginStrings.AddUpstreamRemote, RepositoryHostCommand.AddUpstreamRemote),
            ]));
        }

        // As RegisterPlugins: the plugins by name, then the Plugin Manager and the settings; hidden on the dashboard.
        if (!IsDashboard)
        {
            dynamicMenus.Add(new(PluginStrings.PluginsMenu.AccessKeyText, null, Children: CreatePluginItems(_pluginsHost.Plugins)));
        }

        result.InsertRange(tools, dynamicMenus);
        return result;

        BrowseMenuItem HostItem(TranslatedText text, RepositoryHostCommand command)
            => new(text.AccessKeyText, null) { Invoke = () => _pluginsHost!.RunRepositoryHostCommand(command) };
    }

    private IReadOnlyList<BrowseMenuItem> CreatePluginItems(IReadOnlyList<BrowsePlugin>? plugins)
    {
        BrowseMenuItem settings = new(PluginStrings.PluginSettings.AccessKeyText, null, "Settings") { Invoke = () => _pluginsHost!.OpenPluginSettings() };
        if (plugins is null)
        {
            return [new(PluginStrings.PluginsLoading.AccessKeyText, null) { IsEnabled = false }, BrowseMenuItem.Separator, settings];
        }

        List<BrowseMenuItem> items = [.. plugins
            .Where(plugin => plugin.Name != PluginManagerName)
            .OrderBy(plugin => plugin.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(PluginItem)];
        items.Add(BrowseMenuItem.Separator);
        items.AddRange(plugins.Where(plugin => plugin.Name == PluginManagerName).Select(PluginItem));
        items.Add(settings);
        return items;

        BrowseMenuItem PluginItem(BrowsePlugin plugin)
            => new(plugin.Name.Replace("_", "__"), null, plugin.Icon) { Invoke = () => _pluginsHost!.RunPlugin(plugin) };
    }
}
