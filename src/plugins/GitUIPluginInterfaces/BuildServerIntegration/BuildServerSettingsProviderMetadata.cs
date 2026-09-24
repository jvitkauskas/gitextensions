using System.ComponentModel.Composition;

namespace GitUIPluginInterfaces.BuildServerIntegration;

/// <summary>The build server type of an <see cref="IBuildServerSettingsProvider"/> (plugin API v2).</summary>
[MetadataAttribute]
[AttributeUsage(AttributeTargets.Class)]
public class BuildServerSettingsProviderMetadata : BuildServerAdapterMetadataAttribute
{
    public BuildServerSettingsProviderMetadata(string buildServerType)
        : base(buildServerType)
    {
    }
}
