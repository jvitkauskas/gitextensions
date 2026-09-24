using GitExtensions.Extensibility.Settings;
using GitUIPluginInterfaces.BuildServerIntegration;

namespace GitUIPluginInterfacesTests;

/// <summary>The repository and the suggested values of the build server settings of plugin API v2.</summary>
[TestFixture]
public sealed class BuildServerSettingsContextTests
{
    [Test]
    public void Context_has_the_repository()
    {
        BuildServerSettingsContext context = new("project", ["https://example.org/a.git", null, "https://example.org/b.git"]);

        context.DefaultProjectName.Should().Be("project");
        context.RemoteUrls.Should().Equal(["https://example.org/a.git", "https://example.org/b.git"]);
        context.SuggestedValues.Should().BeEmpty();
    }

    [Test]
    public void Suggested_values_are_shown_while_the_settings_are_unset_and_saved_in_the_settings()
    {
        BuildServerSettingsContext context = new("project", []);
        StringSetting projectName = new("ProjectName", "Project name", defaultValue: "");
        StringSetting serverUrl = new("ServerUrl", "Server URL", defaultValue: "");
        context.SuggestValue(projectName, context.DefaultProjectName);
        MemorySettingsSource settings = new(SettingLevel.Local);

        SettingsSource shown = context.WithSuggestedValues(settings);

        shown.SettingLevel.Should().Be(SettingLevel.Local, "the level decides how the pages show and save the settings");
        projectName[shown].Should().Be("project");
        serverUrl[shown].Should().BeNull();

        // Saved in the settings themselves.
        projectName[shown] = "other";
        settings.GetValue("ProjectName").Should().Be("other");
        projectName[shown].Should().Be("other", "a value set is shown rather than the suggestion");

        settings.SetValue("ProjectName", null);
        projectName[shown].Should().Be("project");
    }

    [Test]
    public void Without_suggested_values_the_settings_are_shown_themselves()
    {
        BuildServerSettingsContext context = new("project", []);
        StringSetting setting = new("Name", "Name", defaultValue: "");
        MemorySettingsSource settings = new();

        context.WithSuggestedValues(settings).Should().BeSameAs(settings);

        context.SuggestValue(setting, "value");
        context.SuggestValue(setting, null);
        context.SuggestedValues.Should().BeEmpty();
    }
}
