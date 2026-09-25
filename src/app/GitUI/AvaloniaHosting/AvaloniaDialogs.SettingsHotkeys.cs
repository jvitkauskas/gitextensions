using GitUI.Hotkey;
using GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;
using ResourceManager;
using ResourceManager.Hotkey;

namespace GitUI.AvaloniaHosting;

internal static partial class AvaloniaDialogs
{
    /// <summary>The hotkeys of <c>ControlHotkeys</c> (<c>IHotkeySettingsManager</c>), the keys as integers.</summary>
    private sealed class HotkeysSettingsHost(IHotkeySettingsManager manager) : IHotkeysSettingsHost
    {
        public IReadOnlyList<HotkeySettingsGroup> LoadSettings() => ToGroups(manager.LoadSettings());

        public IReadOnlyList<HotkeySettingsGroup> CreateDefaultSettings() => ToGroups(manager.CreateDefaultSettings());

        public void SaveSettings(IReadOnlyList<HotkeySettingsGroup> settings)
            => manager.SaveSettings(settings.Select(group => new HotkeySettings(
                group.Name,
                [.. group.Commands.Select(command => new HotkeyCommand(command.CommandCode, command.Name) { KeyData = (Keys)command.KeyData })])));

        public bool IsUsedKey(int keyData) => manager.IsUniqueKey((Keys)keyData);

        // On macOS with the symbols of its shortcuts: Control is Cmd there.
        public string ToText(int keyData) => OperatingSystem.IsMacOS() ? ((Keys)keyData).ToMacOSText() : ((Keys)keyData).ToText();

        private IReadOnlyList<HotkeySettingsGroup> ToGroups(IReadOnlyList<HotkeySettings> settings)
            => [.. settings.Select(setting => new HotkeySettingsGroup(
                setting.Name ?? "",
                [.. (setting.Commands ?? []).Where(command => command is not null)
                    .Select(command => new HotkeyItem(command.CommandCode, command.Name ?? "", (int)command.KeyData, ToText))]))];
    }
}
