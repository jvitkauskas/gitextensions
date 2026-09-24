using GitExtensions.Extensibility.Settings;
using GitExtensions.Plugins.GitHubActionsIntegration.Settings;
using GitUIPluginInterfaces.BuildServerIntegration;

namespace GitHubActionsIntegrationTests;

/// <summary>The settings of the GitHub Actions integration of plugin API v2.</summary>
[TestFixture]
public sealed class GitHubActionsSettingsProviderTests
{
    [Test]
    public void Settings_are_those_of_the_v1_control()
    {
        ISetting[] settings = [.. new GitHubActionsSettingsProvider().GetSettings(new BuildServerSettingsContext("repo", []))];

        settings.Select(s => s.Name).Should().Equal(["GitHubActionsApiUrl", "GitHubActionsOwner", "GitHubActionsRepository", "GitHubActionsApiToken", "ActionSetting"]);
        settings[0].Should().BeOfType<StringSetting>().Which.DefaultValue.Should().Be("https://api.github.com");
        settings[3].Should().BeOfType<PasswordSetting>("the token is hidden, as in the v1 control");
        settings[4].Should().BeOfType<ActionSetting>().Which.Text.Should().Be("Create a GitHub personal access token");
    }

    [Test]
    public void Owner_and_repository_are_suggested_from_the_first_GitHub_remote()
    {
        BuildServerSettingsContext context = new("repo", ["https://example.org/other.git", "https://github.com/gitextensions/gitextensions.git", "https://github.com/fork/gitextensions.git"]);

        _ = new GitHubActionsSettingsProvider().GetSettings(context).ToList();

        context.SuggestedValues.Should().Equal(new Dictionary<string, string>
        {
            ["GitHubActionsOwner"] = "gitextensions",
            ["GitHubActionsRepository"] = "gitextensions",
        });
    }

    [Test]
    public void Nothing_is_suggested_without_a_GitHub_remote()
    {
        BuildServerSettingsContext context = new("repo", ["https://example.org/other.git"]);

        _ = new GitHubActionsSettingsProvider().GetSettings(context).ToList();

        context.SuggestedValues.Should().BeEmpty();
    }
}
