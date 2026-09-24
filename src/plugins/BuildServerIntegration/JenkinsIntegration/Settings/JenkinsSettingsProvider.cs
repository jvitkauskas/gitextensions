using System.ComponentModel.Composition;
using GitCommands;
using GitExtensions.Extensibility.Settings;
using GitExtensions.Extensibility.Translations;
using GitUIPluginInterfaces.BuildServerIntegration;
using ResourceManager;

namespace JenkinsIntegration.Settings;

/// <summary>
///  The settings of the Jenkins integration (plugin API v2), as <see cref="JenkinsSettingsUserControl"/> (the WinForms
///  control of plugin API v1, which the host no longer uses).
/// </summary>
[Export(typeof(IBuildServerSettingsProvider))]
[BuildServerSettingsProviderMetadata(JenkinsAdapter.PluginName)]
[PartCreationPolicy(CreationPolicy.NonShared)]
public sealed class JenkinsSettingsProvider : Translate, IBuildServerSettingsProvider
{
    internal const string ServerUrlKey = "BuildServerUrl";
    internal const string ProjectNameKey = "ProjectName";
    internal const string IgnoreBuildBranchKey = "IgnoreBuildBranch";

    private readonly TranslationString _serverUrl = new("Jenkins server URL");
    private readonly TranslationString _projectName = new("Project name");
    private readonly TranslationString _ignoreBuildBranch = new("Ignore build for branch");

    public JenkinsSettingsProvider()
    {
        Translator.Translate(this, AppSettings.CurrentTranslation);
    }

    public IEnumerable<ISetting> GetSettings(BuildServerSettingsContext context)
    {
        StringSetting projectName = new(ProjectNameKey, _projectName.Text, defaultValue: "");
        context.SuggestValue(projectName, context.DefaultProjectName);

        return
        [
            new StringSetting(ServerUrlKey, _serverUrl.Text, defaultValue: ""),
            projectName,
            new StringSetting(IgnoreBuildBranchKey, _ignoreBuildBranch.Text, defaultValue: ""),
        ];
    }
}
