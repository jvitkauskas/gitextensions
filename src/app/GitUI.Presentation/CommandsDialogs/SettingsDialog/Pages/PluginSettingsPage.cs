using GitExtensions.Extensibility.Settings;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

/// <summary>Strings of the plugin settings; ids match <c>PluginSettingsPage</c>.</summary>
public sealed class PluginSettingsPageStrings : ViewStrings
{
    public PluginSettingsPageStrings()
        : base("PluginSettingsPage")
    {
        NoSettings = Add("labelNoSettings", "Text", "There are no settings available for this plugin.");
    }

    public TranslatedText NoSettings { get; }
}

/// <summary>Strings of the values of the settings pages; ids match <c>SettingsPageBase</c> (<c>DistributedSettingsPage</c>).</summary>
public sealed class SettingValueStrings : ViewStrings
{
    public SettingValueStrings()
        : base("SettingsPageBase")
    {
        NumberPlaceholder = Add("_numberSettingPlaceholder", "Text", "no value set");
        StringPlaceholder = Add("_stringSettingPlaceholder", "Text", @"no value set; for empty string, enter ""{0}"" without the double quotes");
    }

    public TranslatedText NumberPlaceholder { get; }

    public TranslatedText StringPlaceholder { get; }

    /// <summary>The placeholder of a text setting, naming <see cref="StringSettingValue.EmptyStringValue"/>.</summary>
    public string StringPlaceholderText => string.Format(StringPlaceholder.Text, StringSettingValue.EmptyStringValue);
}

/// <summary>
///  A row of <see cref="PluginSettingsPageViewModel"/>, as a row of <c>TableSettingsLayout</c>: the caption and the value of a
///  setting, or a text (a <c>PseudoSetting</c>) which is a link when it can be activated.
/// </summary>
public sealed class PluginSettingRow
{
    private readonly Action? _activate;
    private readonly Action<SettingsSource>? _execute;

    private PluginSettingRow(string? caption, SettingValue? value, string? text, Action? activate, Action<SettingsSource>? execute = null)
    {
        Caption = caption;
        Value = value;
        Text = text;
        _activate = activate;
        _execute = execute;
    }

    public string? Caption { get; }

    /// <summary>The value of the setting, shown by the control of its type; null for a text.</summary>
    public SettingValue? Value { get; }

    public string? Text { get; }

    public bool IsText => Value is null;

    public bool IsLink => _activate is not null || _execute is not null;

    public bool IsPlainText => IsText && !IsLink;

    public static PluginSettingRow ForValue(string? caption, SettingValue value) => new(caption, value, text: null, activate: null);

    public static PluginSettingRow ForText(string? caption, string text, Action? activate = null) => new(caption, value: null, text, activate);

    /// <summary>
    ///  A link that runs <paramref name="execute"/> on the values being edited on the page (an <c>ActionSetting</c>, plugin API
    ///  v2): it reads and changes them, and the page shows the changed values.
    /// </summary>
    public static PluginSettingRow ForAction(string? caption, string text, Action<SettingsSource> execute)
        => new(caption, value: null, text, activate: null, execute);

    /// <summary>The page of the row, whose values the action of the link edits; set when the row is added.</summary>
    internal PluginSettingsPageViewModel? Page { get; set; }

    /// <summary>As the click of the link of the plugin.</summary>
    public void Activate()
    {
        if (_execute is not null)
        {
            if (Page is { } page)
            {
                page.RunWithEditedValues(_execute);
            }
            else
            {
                _execute(new MemorySettingsSource());
            }

            return;
        }

        _activate?.Invoke();
    }
}

/// <summary>
///  Port of <c>PluginSettingsPage</c> (an <c>AutoLayoutSettingsPage</c>, distributed settings): the settings of a plugin, one per
///  row, saved in the settings of the level through the settings container of the plugin (<c>GitPluginSettingsContainer</c>).
/// </summary>
public sealed class PluginSettingsPageViewModel : SettingsPageViewModel
{
    private readonly Func<SettingsSource, SettingsSource> _pluginSettings;
    private readonly string _pageName;

    /// <summary>The level of the settings shown last.</summary>
    private SettingLevel _shownLevel = SettingLevel.Unknown;

    /// <param name="title">The name of the plugin.</param>
    /// <param name="pageName">The name of the type of the plugin (the page reference of the WinForms page).</param>
    /// <param name="pluginSettings">The settings of the plugin in the settings of a level (the plugin settings container).</param>
    public PluginSettingsPageViewModel(PluginSettingsPageStrings strings, SettingValueStrings valueStrings, string title, string pageName, Func<SettingsSource, SettingsSource> pluginSettings)
    {
        Strings = strings;
        ValueStrings = valueStrings;
        Title = title;
        _pageName = pageName;
        _pluginSettings = pluginSettings;
    }

    public PluginSettingsPageStrings Strings { get; }

    public SettingValueStrings ValueStrings { get; }

    public override string Title { get; }

    public override string PageName => _pageName;

    public override IEnumerable<string> SearchKeywords => Rows.Select(row => row.Caption ?? row.Text ?? "").Where(text => text.Length > 0);

    public List<PluginSettingRow> Rows { get; } = [];

    /// <summary>As <c>labelNoSettings</c>.</summary>
    public bool HasNoSettings => Rows.Count == 0;

    /// <summary>Adds a row, as <c>AddSettingControl</c>; the values are loaded and saved with the settings of the level.</summary>
    public void AddRow(PluginSettingRow row)
    {
        if (row.Value is not null)
        {
            Add(row.Value);
        }

        row.Page = this;
        Rows.Add(row);
    }

    /// <summary>
    ///  Shows the values of <paramref name="pluginSettings"/> (the settings of the plugin themselves, not those of a level), e.g.
    ///  for the settings of a build server shown on another page.
    /// </summary>
    public void LoadValues(SettingsSource pluginSettings)
    {
        ArgumentNullException.ThrowIfNull(pluginSettings);
        _shownLevel = pluginSettings.SettingLevel;
        base.SettingsToPage(pluginSettings);
    }

    /// <summary>Saves the values in <paramref name="pluginSettings"/> (the settings of the plugin themselves).</summary>
    public void SaveValues(SettingsSource pluginSettings)
    {
        ArgumentNullException.ThrowIfNull(pluginSettings);
        base.PageToSettings(pluginSettings);
    }

    /// <summary>The values being edited, not saved yet (except the credentials, which are kept in the credential manager).</summary>
    public MemorySettingsSource GetEditedValues()
    {
        MemorySettingsSource values = new(_shownLevel);
        foreach (SettingValue value in EditedValues)
        {
            value.Save(values);
        }

        return values;
    }

    /// <summary>
    ///  Runs <paramref name="action"/> on the values being edited (as <see cref="GetEditedValues"/>), then shows the values it
    ///  changed (the action of an <c>ActionSetting</c>).
    /// </summary>
    public void RunWithEditedValues(Action<SettingsSource> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        MemorySettingsSource values = GetEditedValues();
        action(values);
        foreach (SettingValue value in EditedValues)
        {
            value.Load(values);
        }
    }

    private IEnumerable<SettingValue> EditedValues => Rows.Select(row => row.Value).OfType<SettingValue>().Where(value => value is not CredentialsSettingValue);

    protected override void SettingsToPage(SettingsSource? settings)
    {
        SettingsSource? pluginSettings = settings is null ? null : _pluginSettings(settings);
        _shownLevel = pluginSettings?.SettingLevel ?? SettingLevel.Unknown;
        base.SettingsToPage(pluginSettings);
    }

    protected override void PageToSettings(SettingsSource? settings) => base.PageToSettings(settings is null ? null : _pluginSettings(settings));
}
