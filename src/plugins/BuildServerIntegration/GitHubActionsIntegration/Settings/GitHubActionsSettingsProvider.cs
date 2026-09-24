using System.ComponentModel.Composition;
using GitCommands;
using GitCommands.Remotes;
using GitExtensions.Extensibility.Settings;
using GitExtensions.Extensibility.Translations;
using GitUI;
using GitUIPluginInterfaces.BuildServerIntegration;
using ResourceManager;

namespace GitExtensions.Plugins.GitHubActionsIntegration.Settings;

/// <summary>
///  The settings of the GitHub Actions integration (plugin API v2), as <c>GitHubActionsSettingsUserControl</c> (the
///  WinForms control of plugin API v1, which the host no longer uses): the owner and the repository are suggested from the
///  first GitHub remote.
/// </summary>
[Export(typeof(IBuildServerSettingsProvider))]
[BuildServerSettingsProviderMetadata(GitHubActionsAdapter.PluginName)]
[PartCreationPolicy(CreationPolicy.NonShared)]
public sealed class GitHubActionsSettingsProvider : Translate, IBuildServerSettingsProvider
{
    internal const string DefaultApiUrl = "https://api.github.com";
    internal const string TokenManagementUrl = "https://github.com/settings/personal-access-tokens/new";

    private readonly TranslationString _apiUrl = new("API URL");
    private readonly TranslationString _owner = new("Owner");
    private readonly TranslationString _repository = new("Repository");
    private readonly TranslationString _apiToken = new("API Token");
    private readonly TranslationString _tokenManagement = new("Create a GitHub personal access token");

    private readonly GitHubRemoteParser _remoteParser = new();

    public GitHubActionsSettingsProvider()
    {
        Translator.Translate(this, AppSettings.CurrentTranslation);
    }

    public IEnumerable<ISetting> GetSettings(BuildServerSettingsContext context)
    {
        StringSetting owner = new(GitHubActionsAdapter.SettingOwner, _owner.Text, defaultValue: "");
        StringSetting repository = new(GitHubActionsAdapter.SettingRepository, _repository.Text, defaultValue: "");
        foreach (string remote in context.RemoteUrls)
        {
            if (_remoteParser.TryExtractGitHubDataFromRemoteUrl(remote, out string? remoteOwner, out string? remoteRepository))
            {
                context.SuggestValue(owner, remoteOwner);
                context.SuggestValue(repository, remoteRepository);
                break;
            }
        }

        return
        [
            new StringSetting(GitHubActionsAdapter.SettingApiUrl, _apiUrl.Text, DefaultApiUrl),
            owner,
            repository,
            new PasswordSetting(GitHubActionsAdapter.SettingApiToken, _apiToken.Text, defaultValue: ""),
            new ActionSetting(_tokenManagement.Text, () => OsShellUtil.OpenUrlInDefaultBrowser(TokenManagementUrl)),
        ];
    }
}
