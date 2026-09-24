using GitExtensions.Extensibility.Settings;
using GitExtUtils.GitUI;
using GitUIPluginInterfaces;
using GitUIPluginInterfaces.BuildServerIntegration;

namespace GitUI.BuildServerIntegration;

/// <summary>The settings of the build server plugins (moved out of the WinForms <c>BuildServerSettingsProviderControl</c>).</summary>
public static class BuildServerSettingsProviders
{
    /// <summary>The settings of the build server type (plugin API v2), if its plugin declares them.</summary>
    public static IBuildServerSettingsProvider? FindProvider(string? buildServerType)
        => buildServerType is null
            ? null
            : ManagedExtensibility.GetExports<IBuildServerSettingsProvider, IBuildServerTypeMetadata>()
                .SingleOrDefault(export => export.Metadata.BuildServerType == buildServerType)?.Value;
}
