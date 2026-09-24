using GitExtensions.Extensibility.Settings;

namespace GitUIPluginInterfaces.BuildServerIntegration;

/// <summary>The settings of a build server integration, as a WinForms control (plugin API v1).</summary>
/// <remarks>
///  Plugin API v2: export an <see cref="IBuildServerSettingsProvider"/> instead, whose settings the host renders in any UI
///  framework; the host uses it rather than a control exported for the same build server type.
/// </remarks>
public interface IBuildServerSettingsUserControl
{
    void Initialize(string defaultProjectName, IEnumerable<string?> remotes);

    void LoadSettings(SettingsSource buildServerConfig);
    void SaveSettings(SettingsSource buildServerConfig);
}
