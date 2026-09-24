using System.ComponentModel.Composition;
using GitCommands;
using GitExtensions.Extensibility.Settings;
using GitExtensions.Extensibility.Translations;
using GitUIPluginInterfaces.BuildServerIntegration;
using ResourceManager;

namespace AppVeyorIntegration.Settings;

/// <summary>
///  The settings of the AppVeyor integration (plugin API v2), as <see cref="AppVeyorSettingsUserControl"/> (the WinForms
///  control of plugin API v1, which the host no longer uses).
/// </summary>
[Export(typeof(IBuildServerSettingsProvider))]
[BuildServerSettingsProviderMetadata(AppVeyorAdapter.PluginName)]
[PartCreationPolicy(CreationPolicy.NonShared)]
public sealed class AppVeyorSettingsProvider : Translate, IBuildServerSettingsProvider
{
    internal const string ProjectNameKey = "AppVeyorProjectName";
    internal const string AccountNameKey = "AppVeyorAccountName";
    internal const string AccountTokenKey = "AppVeyorAccountToken";
    internal const string LoadTestsResultsKey = "AppVeyorLoadTestsResults";

    private readonly TranslationString _projectNames = new("Project(s) Name(s)");
    private readonly TranslationString _projectNamesHint = new("If you want to use the result of different projects, separate your different projects names by a '|'.");
    private readonly TranslationString _accountName = new("Account name");
    private readonly TranslationString _apiToken = new("Api token");
    private readonly TranslationString _apiTokenHint = new("Token used to be able to query AppVeyor rest Api. You will find out in the user account menu.");
    private readonly TranslationString _loadTestResults = new("Display test results in build status summary for each build result (network intensive!)");

    public AppVeyorSettingsProvider()
    {
        Translator.Translate(this, AppSettings.CurrentTranslation);
    }

    public IEnumerable<ISetting> GetSettings(BuildServerSettingsContext context)
    {
        StringSetting projectNames = new(ProjectNameKey, _projectNames.Text, defaultValue: "");
        context.SuggestValue(projectNames, context.DefaultProjectName);

        return
        [
            projectNames,
            new PseudoSetting(_projectNamesHint.Text),
            new StringSetting(AccountNameKey, _accountName.Text, defaultValue: ""),
            new StringSetting(AccountTokenKey, _apiToken.Text, defaultValue: ""),
            new PseudoSetting(_apiTokenHint.Text),
            new BoolSetting(LoadTestsResultsKey, _loadTestResults.Text, defaultValue: false),
        ];
    }
}
