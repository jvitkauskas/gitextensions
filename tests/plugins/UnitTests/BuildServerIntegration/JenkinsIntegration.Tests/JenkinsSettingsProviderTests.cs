using GitExtensions.Extensibility.Settings;
using GitUIPluginInterfaces.BuildServerIntegration;
using JenkinsIntegration.Settings;

namespace JenkinsIntegrationTests;

/// <summary>The settings of the Jenkins integration of plugin API v2.</summary>
[TestFixture]
public sealed class JenkinsSettingsProviderTests
{
    [Test]
    public void Settings_are_those_of_the_v1_control_and_suggest_the_project_name()
    {
        BuildServerSettingsContext context = new("repo", []);

        ISetting[] settings = [.. new JenkinsSettingsProvider().GetSettings(context)];

        settings.Should().AllBeOfType<StringSetting>();
        settings.Select(s => s.Name).Should().Equal(["BuildServerUrl", "ProjectName", "IgnoreBuildBranch"]);
        settings.Select(s => s.Caption).Should().Equal(["Jenkins server URL", "Project name", "Ignore build for branch"]);
        context.SuggestedValues.Should().Equal(new Dictionary<string, string> { ["ProjectName"] = "repo" });
    }
}
