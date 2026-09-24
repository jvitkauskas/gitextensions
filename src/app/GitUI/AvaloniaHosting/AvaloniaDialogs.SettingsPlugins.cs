using System.Drawing.Imaging;
using System.Reflection;
using System.Text;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Plugins;
using GitExtensions.Extensibility.Settings;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs.SettingsDialog;
using GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;
using GitUI.Presentation.Translations;
using Microsoft;

namespace GitUI.AvaloniaHosting;

/// <summary>The pages of the plugins in the settings dialog (<c>PluginSettingsPage</c>).</summary>
internal static partial class AvaloniaDialogs
{
    /// <summary>As the end of <c>FormSettings.OnRuntimeLoad</c>: a page for each plugin with settings, sorted by title.</summary>
    /// <param name="getOwner">The owner of the dialogs of the links of the plugins (<see cref="ActionSetting"/>): the settings window.</param>
    private static IEnumerable<(PluginSettingsPageViewModel Page, byte[]? Icon)> CreatePluginSettingsPages(Func<WindowOwner> getOwner)
    {
        PluginSettingsPageStrings strings = ViewStrings.Load<PluginSettingsPageStrings>();
        SettingValueStrings valueStrings = ViewStrings.Load<SettingValueStrings>();
        List<(PluginSettingsPageViewModel Page, byte[]? Icon)> pages;
        lock (PluginRegistry.Plugins)
        {
            pages = [.. PluginRegistry.Plugins
                .Where(plugin => plugin.HasSettings)
                .Select(plugin => (CreatePluginSettingsPage(plugin, strings, valueStrings, getOwner), ToPng(plugin.Icon)))];
        }

        return pages.OrderBy(entry => entry.Page.Title, StringComparer.CurrentCultureIgnoreCase);
    }

    /// <summary>As <c>PluginSettingsPage.CreateSettingsPageFromPlugin</c>.</summary>
    internal static PluginSettingsPageViewModel CreatePluginSettingsPage(IGitPlugin plugin, PluginSettingsPageStrings strings, SettingValueStrings valueStrings, Func<WindowOwner>? getOwner = null)
    {
        Validates.NotNull(plugin.Description);

        // The description as the key of the old settings of the plugin.
        GitPluginSettingsContainer container = new(plugin.Id, plugin.Description);
        PluginSettingsPageViewModel page = new(strings, valueStrings, plugin.Name ?? "", plugin.GetType().Name, settings =>
        {
            container.SetSettingsSource(settings);
            return container;
        });

        // As CreateSettingsControls, with the debug info for exceptions.
        StringBuilder state = new($"{(plugin.HasSettings ? "" : "not ")}having settings");
        try
        {
            ISetting[] settings = plugin.HasSettings ? [.. plugin.GetSettings()] : [];
            state.Append($", enumerable with {settings.Length} setting(s)");
            foreach (ISetting setting in settings)
            {
                page.AddRow(CreatePluginSettingRow(setting, getOwner));
                state.Append('.');
            }
        }
        catch (Exception ex)
        {
            throw new ExternalOperationException(command: $"Cannot load settings for plugin {plugin.Name ?? "unknown"}: {state}", innerException: ex);
        }

        return page;
    }

    /// <summary>As <c>SettingControlBindingsProvider.CreateControlBinding</c>: the value model of the type of the setting.</summary>
    /// <param name="getOwner">The owner of the dialogs of an <see cref="ActionSetting"/>.</param>
    internal static PluginSettingRow CreatePluginSettingRow(ISetting setting, Func<WindowOwner>? getOwner = null)
    {
        string? caption = string.IsNullOrWhiteSpace(setting.Caption) ? null : setting.Caption;
        return setting switch
        {
            BoolSetting s => PluginSettingRow.ForValue(caption, new BoolSettingValue(s)),
            CredentialsSetting s => PluginSettingRow.ForValue(caption, new CredentialsSettingValue(s)),
            PasswordSetting s => PluginSettingRow.ForValue(caption, new PasswordSettingValue(s)),
            StringSetting s => PluginSettingRow.ForValue(caption, new StringSettingValue(s)),
            ChoiceSetting s => PluginSettingRow.ForValue(caption, new ChoiceSettingValue(s)),
            PseudoSetting s => PluginSettingRow.ForText(caption, s.Text),

            // Plugin API v2: a link that runs its action in the host context, on the values being edited.
            ActionSetting s => PluginSettingRow.ForAction(caption, s.Text, values => AvaloniaUi.RunInHostContext(
                () => s.Execute(new SettingActionContext(getOwner?.Invoke() ?? WindowOwner.None, values)))),
            NumberSetting<int> s => PluginSettingRow.ForValue(caption, new IntSettingValue(s)),
            NumberSetting<float> s => PluginSettingRow.ForValue(caption, new NumberTextSettingValue<float>(s)),
            NumberSetting<double> s => PluginSettingRow.ForValue(caption, new NumberTextSettingValue<double>(s)),
            NumberSetting<long> s => PluginSettingRow.ForValue(caption, new NumberTextSettingValue<long>(s)),
            _ => throw new NotSupportedException($"No control binding registered for {setting.GetType().Name}.")
        };
    }

    private static byte[]? ToPng(Image? image)
    {
        if (image is null)
        {
            return null;
        }

        using MemoryStream stream = new();
        image.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }
}
