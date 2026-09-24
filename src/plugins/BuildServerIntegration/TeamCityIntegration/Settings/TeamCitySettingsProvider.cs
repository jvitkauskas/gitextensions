using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using GitCommands;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Settings;
using GitExtensions.Extensibility.Translations;
using GitExtUtils;
using GitUIPluginInterfaces.BuildServerIntegration;
using ResourceManager;

namespace TeamCityIntegration.Settings;

/// <summary>
///  The settings of the TeamCity integration (plugin API v2), as <c>TeamCitySettingsUserControl</c> (the WinForms
///  control of plugin API v1, which the host no longer uses): its "..." button and its link are links that fill in the
///  project and the build filter, and an invalid build filter is not saved.
/// </summary>
[Export(typeof(IBuildServerSettingsProvider))]
[BuildServerSettingsProviderMetadata(TeamCityAdapter.PluginName)]
[PartCreationPolicy(CreationPolicy.NonShared)]
public sealed partial class TeamCitySettingsProvider : Translate, IBuildServerSettingsProvider
{
    internal const string ServerUrlKey = "BuildServerUrl";
    internal const string ProjectNameKey = "ProjectName";
    internal const string BuildIdFilterKey = "BuildIdFilter";
    internal const string LogAsGuestKey = "LogAsGuest";

    private readonly TranslationString _serverUrl = new("TeamCity server URL");
    private readonly TranslationString _projectName = new("Project name");
    private readonly TranslationString _projectNameComment = new("Several names splitted by | char");
    private readonly TranslationString _buildIdFilter = new("Build Id Filter");
    private readonly TranslationString _buildIdFilterComment = new("Regexp");
    private readonly TranslationString _chooseBuild = new("Choose the project and the build...");
    private readonly TranslationString _logAsGuest = new("Log as guest to display the build report");
    private readonly TranslationString _extractFromClipboard = new("Extract the data from the build url copied in the clipboard");
    private readonly TranslationString _regexError = new("The \"Build Id Filter\" regular expression is not valid and won't be saved!");
    private readonly TranslationString _noServerUrlMessage = new("Please enter the TeamCity server URL first.");
    private readonly TranslationString _failToLoadProjectMessage = new("Failed to load the projects and build list." + Environment.NewLine + "Please verify the server url.");
    private readonly TranslationString _failToLoadProjectCaption = new("Error when loading the projects and build list");
    private readonly TranslationString _failToExtractDataFromClipboardMessage = new("The clipboard doesn't contain a valid build url." + Environment.NewLine + Environment.NewLine +
            "Please copy in the clipboard the url of the build before retrying." + Environment.NewLine +
            "(Should contain at least the \"buildTypeId\" parameter)");
    private readonly TranslationString _failToExtractDataFromClipboardCaption = new("Build url not valid");

    [GeneratedRegex(@"(\?|\&)(?<buildtypeid>[^=]+)\=(?<buildtype>[^&]+)", RegexOptions.ExplicitCapture)]
    private static partial Regex TeamcityBuildUrl { get; }

    public TeamCitySettingsProvider()
    {
        Translator.Translate(this, AppSettings.CurrentTranslation);
    }

    public IEnumerable<ISetting> GetSettings(BuildServerSettingsContext context)
    {
        StringSetting serverUrl = new(ServerUrlKey, _serverUrl.Text, defaultValue: "");
        StringSetting projectName = new(ProjectNameKey, _projectName.Text, defaultValue: "");
        StringSetting buildIdFilter = new(BuildIdFilterKey, _buildIdFilter.Text, defaultValue: "");
        context.SuggestValue(projectName, context.DefaultProjectName);

        return
        [
            serverUrl,
            projectName,
            new PseudoSetting(_projectNameComment.Text),
            buildIdFilter,
            new PseudoSetting(_buildIdFilterComment.Text),
            new ActionSetting(_chooseBuild.Text, action => ChooseBuild(action, serverUrl, projectName, buildIdFilter)),
            new BoolSetting(LogAsGuestKey, _logAsGuest.Text, defaultValue: false),
            new ActionSetting(_extractFromClipboard.Text, action => ExtractFromClipboard(action, serverUrl, projectName, buildIdFilter)),
        ];
    }

    /// <summary>As <c>TeamCitySettingsUserControl.SaveSettings</c>, which saved nothing with an invalid build filter.</summary>
    public string? Validate(SettingsSource values)
        => BuildServerSettingsHelper.IsRegexValid(values.GetString(BuildIdFilterKey, null) ?? "") ? null : _regexError.Text;

    // As buttonProjectChooser_Click, enabled with a server URL.
    private void ChooseBuild(SettingActionContext action, StringSetting serverUrl, StringSetting projectName, StringSetting buildIdFilter)
    {
        string url = serverUrl.ValueOrDefault(action.Values);
        if (string.IsNullOrWhiteSpace(url))
        {
            PluginMessageBoxes.ShowWarning(action.Owner, _noServerUrlMessage.Text, _failToLoadProjectCaption.Text);
            return;
        }

        string project = projectName.ValueOrDefault(action.Values);
        string filter = buildIdFilter.ValueOrDefault(action.Values);
        try
        {
            if (TeamCityBuildChooserDialog.TryShow(action.Owner, url, project, filter, out (string ProjectName, string BuildIdFilter)? chosen) && chosen is { } build)
            {
                projectName[action.Values] = build.ProjectName;
                buildIdFilter[action.Values] = build.BuildIdFilter;
            }
        }
        catch
        {
            PluginMessageBoxes.ShowError(action.Owner, _failToLoadProjectMessage.Text, _failToLoadProjectCaption.Text);
        }
    }

    // As lnkExtractDataFromBuildUrlCopiedInTheClipboard_LinkClicked.
    private void ExtractFromClipboard(SettingActionContext action, StringSetting serverUrl, StringSetting projectName, StringSetting buildIdFilter)
    {
        if (ClipboardUtil.TryGetText(out string? text) && text.Contains("buildTypeId=") && Uri.TryCreate(text, UriKind.Absolute, out Uri? buildUri))
        {
            string teamCityServerUrl = buildUri.Scheme + "://" + buildUri.Authority;
            serverUrl[action.Values] = teamCityServerUrl;
            using TeamCityAdapter teamCityAdapter = new();
            teamCityAdapter.InitializeHttpClient(teamCityServerUrl);

            foreach (Match paramResult in TeamcityBuildUrl.Matches(buildUri.Query))
            {
                if (paramResult.Success && paramResult.Groups["buildtypeid"].ValueSpan is "buildTypeId")
                {
                    Build buildType;
                    try
                    {
                        buildType = teamCityAdapter.GetBuildType(paramResult.Groups["buildtype"].Value);
                    }
                    catch (Exception)
                    {
                        PluginMessageBoxes.ShowError(action.Owner, _failToLoadProjectMessage.Text, _failToLoadProjectCaption.Text);
                        return;
                    }

                    projectName[action.Values] = buildType.ParentProject;
                    buildIdFilter[action.Values] = buildType.Id;
                    return;
                }
            }
        }

        PluginMessageBoxes.ShowWarning(action.Owner, _failToExtractDataFromClipboardMessage.Text, _failToExtractDataFromClipboardCaption.Text);
    }
}
