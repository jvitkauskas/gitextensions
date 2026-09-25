using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Plugins;
using GitExtensions.Extensibility.Settings;
using GitUI;
using GitUI.AvaloniaHosting;
using GitUI.CommandsDialogs.SettingsDialog.Pages;
using GitUI.Presentation.CommandsDialogs.SettingsDialog;
using GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;
using GitUI.Presentation.Services;
using GitUIPluginInterfaces.BuildServerIntegration;
using GitUIPluginInterfaces.RepositoryHosts;
using NSubstitute;

namespace GitUITests.Plugin;

/// <summary>How the WinForms host (and the Avalonia bridge) renders plugin API v2 (docs/avalonia-port/PLAN.md, phase 7).</summary>
[Apartment(ApartmentState.STA)]
public sealed class PluginApiV2HostTests
{
    [Test]
    public void Action_setting_row_of_the_Avalonia_settings_runs_the_action_on_the_edited_values()
    {
        StringSetting projectName = new("ProjectName", "Project name", defaultValue: "");
        WindowOwner? owner = null;
        ActionSetting action = new("Choose", c =>
        {
            owner = c.Owner;
            projectName[c.Values] = "chosen";
        });
        PluginSettingsPageViewModel page = new(new PluginSettingsPageStrings(), new SettingValueStrings(), "title", "page", settings => settings);
        page.AddRow(AvaloniaDialogs.CreatePluginSettingRow(projectName));
        PluginSettingRow row = AvaloniaDialogs.CreatePluginSettingRow(action, () => new WindowOwner(42));
        page.AddRow(row);
        page.LoadValues(new MemorySettingsSource(SettingLevel.Local));

        row.IsLink.Should().BeTrue();
        row.Text.Should().Be("Choose");
        row.Activate();

        owner.Should().Be(new WindowOwner(42));
        ((StringSettingValue)page.Rows[0].Value!).Value.Should().Be("chosen");
    }

    [Test]
    [Platform(Include = "Win")]
    [System.Runtime.Versioning.SupportedOSPlatform("windows6.1")]
    public void Plugin_menu_items_are_a_menu_model_for_the_Avalonia_menus()
    {
        int clicks = 0;
        using Bitmap icon = new(16, 16);
        IReadOnlyList<MenuModelItem> items = AvaloniaDialogs.ToMenuModel(
        [
            new PluginMenuItem("View in &Git_Hub", icon: icon, children: [new PluginMenuItem("origin", () => clicks++) { IsEnabled = false }]),
            PluginMenuItem.Separator,
        ]);

        items.Should().HaveCount(2);
        items[0].Header.Should().Be("View in _Git__Hub", "as Avalonia access keys");
        items[0].Icon.Should().BeOfType<byte[]>("PNG data");
        items[0].Children!.Single().IsEnabled.Should().BeFalse();
        items[0].Children!.Single().Execute!();
        clicks.Should().Be(0, "a disabled item does nothing");
        items[1].IsSeparator.Should().BeTrue();
    }

    private sealed class FakeBuildServerSettingsProvider : IBuildServerSettingsProvider
    {
        public BuildServerSettingsContext? Context { get; private set; }

        public string? Error { get; init; }

        public SettingsSource? Validated { get; private set; }

        public IEnumerable<ISetting> GetSettings(BuildServerSettingsContext context)
        {
            Context = context;
            StringSetting projectName = new("ProjectName", "Project name", defaultValue: "");
            context.SuggestValue(projectName, context.DefaultProjectName);
            return
            [
                projectName,
                new StringSetting("ServerUrl", "Server URL", defaultValue: ""),
                new ActionSetting("link", () => { }),
                new BoolSetting("LoadTestResults", "Load test results", defaultValue: false),
            ];
        }

        public string? Validate(SettingsSource values)
        {
            Validated = values;
            return Error;
        }
    }
}
