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
using GitUI.SettingControlBindings;
using GitUIPluginInterfaces.BuildServerIntegration;
using GitUIPluginInterfaces.RepositoryHosts;
using NSubstitute;

namespace GitUITests.Plugin;

/// <summary>How the WinForms host (and the Avalonia bridge) renders plugin API v2 (docs/avalonia-port/PLAN.md, phase 7).</summary>
[Apartment(ApartmentState.STA)]
public sealed class PluginApiV2HostTests
{
    [Test]
    public void Plugin_menu_items_are_rendered_as_WinForms_menu_items()
    {
        int clicks = 0;
        using Bitmap icon = new(16, 16);
        PluginMenuItem item = new("&View in GitHub", icon: icon, children:
        [
            new PluginMenuItem("origin", () => clicks++),
            PluginMenuItem.Separator,
            new PluginMenuItem("upstream", () => clicks++) { IsEnabled = false },
        ]);

        ToolStripMenuItem menuItem = (ToolStripMenuItem)PluginMenuItemRenderer.CreateItem(item);

        menuItem.Text.Should().Be("&View in GitHub");
        menuItem.Image.Should().BeSameAs(icon);
        menuItem.DropDownItems.Count.Should().Be(3);
        menuItem.DropDownItems[1].Should().BeOfType<ToolStripSeparator>();
        menuItem.DropDownItems[2].Enabled.Should().BeFalse();
        menuItem.DropDownItems[0].PerformClick();
        clicks.Should().Be(1);
    }

    [Test]
    public void Plugin_menu_items_replace_those_added_before_at_the_end_of_the_menu()
    {
        using ContextMenuStrip menu = new();
        menu.Items.Add("own item");

        PluginMenuItemRenderer.ReplaceItems(menu, [new PluginMenuItem("first")]);
        PluginMenuItemRenderer.ReplaceItems(menu, [new PluginMenuItem("second"), new PluginMenuItem("third")]);

        menu.Items.Cast<ToolStripItem>().Select(i => i.Text).Should().Equal(["own item", "second", "third"]);

        PluginMenuItemRenderer.ReplaceItems(menu, []);
        menu.Items.Cast<ToolStripItem>().Select(i => i.Text).Should().Equal(["own item"]);
    }

    [Test]
    public void Action_setting_link_edits_the_values_shown_by_the_other_bindings()
    {
        StringSetting projectName = new("ProjectName", "Project name", defaultValue: "");
        StringSetting serverUrl = new("ServerUrl", "Server URL", defaultValue: "");
        SettingActionContext? context = null;
        ActionSetting action = new("Choose", c =>
        {
            context = c;
            projectName[c.Values] = $"project of {serverUrl.ValueOrDefault(c.Values)}";
        });
        List<ISettingControlBinding> bindings = [.. new ISetting[] { projectName, serverUrl, action }.Select(SettingControlBindingsProvider.CreateControlBinding)];
        EditedSettingValues.Connect(bindings, () => SettingLevel.Local);
        using Form form = new();
        foreach (ISettingControlBinding binding in bindings)
        {
            form.Controls.Add(binding.GetControl());
        }

        ((TextBox)bindings[1].GetControl()).Text = "https://ci.example.org";
        ((ActionSettingControlBinding)bindings[2]).Execute(bindings[2].GetControl());

        context!.Owner.Should().Be(new WindowOwner(form.Handle), "the form of the link owns the dialogs of the action");
        context.Values.SettingLevel.Should().Be(SettingLevel.Local);
        bindings[0].GetControl().Text.Should().Be("project of https://ci.example.org");
        bindings[2].GetControl().Should().BeOfType<LinkLabel>().Which.Text.Should().Be("Choose");
    }

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
