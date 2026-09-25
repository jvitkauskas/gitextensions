using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Settings;

namespace GitExtensions.Extensibility.Plugins;

public interface IGitPlugin
{
    Guid Id { get; }

    string? Name { get; }

    string? Description { get; }

    /// <summary>The icon of the plugin as a GDI+ image, which is only shown on Windows; see <see cref="IconImage"/>.</summary>
    Image? Icon { get; }

    /// <summary>
    ///  The icon of the plugin on every system (plugin API v3). The host shows it rather than <see cref="Icon"/>.
    /// </summary>
    PluginImage? IconImage => null;

    IGitPluginSettingsContainer? SettingsContainer { get; set; }

    bool HasSettings { get; }

    IEnumerable<ISetting> GetSettings();

    void Register(IGitUICommands gitUiCommands);

    void Unregister(IGitUICommands gitUiCommands);

    /// <summary>
    /// Runs the plugin and returns whether the RevisionGrid should be refreshed.
    /// </summary>
    bool Execute(GitUIEventArgs args);
}
