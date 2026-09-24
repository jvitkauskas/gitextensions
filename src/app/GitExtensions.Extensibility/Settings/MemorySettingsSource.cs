namespace GitExtensions.Extensibility.Settings;

/// <summary>
///  Settings kept in memory, e.g. the values being edited on a settings page (<see cref="SettingActionContext.Values"/>) or the
///  settings of a test.
/// </summary>
public sealed class MemorySettingsSource : SettingsSource
{
    private readonly Dictionary<string, string?> _values = [];

    /// <param name="settingLevel">The level these settings stand for, which decides how the settings pages show and save them.</param>
    public MemorySettingsSource(SettingLevel settingLevel = SettingLevel.Unknown)
    {
        SettingLevel = settingLevel;
    }

    /// <summary>The names of the values that are set.</summary>
    public IEnumerable<string> Names => _values.Where(pair => pair.Value is not null).Select(pair => pair.Key);

    public override string? GetValue(string name) => _values.TryGetValue(name, out string? value) ? value : null;

    public override void SetValue(string name, string? value) => _values[name] = value;
}
