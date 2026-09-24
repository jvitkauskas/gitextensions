using GitExtensions.Extensibility.Settings;

namespace GitUIPluginInterfaces.BuildServerIntegration;

/// <summary>
///  The repository whose build server settings are shown (as the arguments of
///  <c>IBuildServerSettingsUserControl.Initialize</c>), and the values suggested for its unset settings.
/// </summary>
public sealed class BuildServerSettingsContext
{
    private readonly Dictionary<string, string> _suggestedValues = [];

    /// <param name="defaultProjectName">The name of the folder of the repository.</param>
    /// <param name="remoteUrls">The URLs of the remotes of the repository (their push URL if any).</param>
    public BuildServerSettingsContext(string defaultProjectName, IEnumerable<string?> remoteUrls)
    {
        ArgumentNullException.ThrowIfNull(defaultProjectName);
        ArgumentNullException.ThrowIfNull(remoteUrls);
        DefaultProjectName = defaultProjectName;
        RemoteUrls = [.. remoteUrls.OfType<string>()];
    }

    /// <summary>The name of the folder of the repository, the usual name of its project on the build server.</summary>
    public string DefaultProjectName { get; }

    /// <summary>The URLs of the remotes of the repository.</summary>
    public IReadOnlyList<string> RemoteUrls { get; }

    /// <summary>The values suggested for the unset settings, by setting name.</summary>
    public IReadOnlyDictionary<string, string> SuggestedValues => _suggestedValues;

    /// <summary>
    ///  Shows <paramref name="value"/> for <paramref name="setting"/> while it is unset at the level shown, as the v1 controls
    ///  filled in the project name or the values found in the remotes: the value is saved with the page (unless the user
    ///  clears it), so that the adapter reads it. Unlike <see cref="StringSetting.DefaultValue"/>, which is not saved.
    /// </summary>
    public void SuggestValue(ISetting setting, string? value)
    {
        ArgumentNullException.ThrowIfNull(setting);
        if (value is null)
        {
            _suggestedValues.Remove(setting.Name);
        }
        else
        {
            _suggestedValues[setting.Name] = value;
        }
    }

    /// <summary>
    ///  The settings to load the page from: <paramref name="settings"/>, with the suggested values for the unset settings (the
    ///  page is saved in <paramref name="settings"/> itself).
    /// </summary>
    public SettingsSource WithSuggestedValues(SettingsSource settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return _suggestedValues.Count == 0 ? settings : new SuggestingSettingsSource(settings, _suggestedValues);
    }

    private sealed class SuggestingSettingsSource : SettingsSource
    {
        private readonly SettingsSource _settings;
        private readonly IReadOnlyDictionary<string, string> _suggestedValues;

        public SuggestingSettingsSource(SettingsSource settings, IReadOnlyDictionary<string, string> suggestedValues)
        {
            _settings = settings;
            _suggestedValues = suggestedValues;
            SettingLevel = settings.SettingLevel;
        }

        public override string? GetValue(string name)
            => _settings.GetValue(name) ?? _suggestedValues.GetValueOrDefault(name);

        public override void SetValue(string name, string? value) => _settings.SetValue(name, value);
    }
}
