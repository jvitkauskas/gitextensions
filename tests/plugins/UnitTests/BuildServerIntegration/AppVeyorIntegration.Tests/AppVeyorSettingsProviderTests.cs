using AppVeyorIntegration.Settings;
using GitExtensions.Extensibility.Settings;
using GitUIPluginInterfaces.BuildServerIntegration;

namespace AppVeyorIntegrationTests;

/// <summary>The settings of the AppVeyor integration of plugin API v2.</summary>
[TestFixture]
public sealed class AppVeyorSettingsProviderTests
{
    [Test]
    public void Settings_are_those_of_the_v1_control_and_suggest_the_project_name()
    {
        BuildServerSettingsContext context = new("repo", []);

        ISetting[] settings = [.. new AppVeyorSettingsProvider().GetSettings(context)];

        settings.OfType<StringSetting>().Select(s => s.Name).Should().Equal(["AppVeyorProjectName", "AppVeyorAccountName", "AppVeyorAccountToken"]);
        settings.OfType<BoolSetting>().Single().Name.Should().Be("AppVeyorLoadTestsResults");
        settings.OfType<PseudoSetting>().Should().HaveCount(2, "the hints of the v1 control");
        context.SuggestedValues.Should().Equal(new Dictionary<string, string> { ["AppVeyorProjectName"] = "repo" });
        ((IBuildServerSettingsProvider)new AppVeyorSettingsProvider()).Validate(new MemorySettingsSource()).Should().BeNull();
    }
}
