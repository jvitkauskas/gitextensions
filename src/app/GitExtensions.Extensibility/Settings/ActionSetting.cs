namespace GitExtensions.Extensibility.Settings;

/// <summary>
///  Not a setting (it saves no value): a link on the settings page of a plugin that runs an action (plugin API v2), e.g.
///  opens a web page, or fills in the other settings of the page. It replaces a <see cref="PseudoSetting"/> with a WinForms
///  <c>LinkLabel</c>; the host renders it in its UI framework.
/// </summary>
public sealed class ActionSetting : ISetting
{
    /// <param name="text">The text of the link.</param>
    /// <param name="execute">Runs when the link is clicked, on the UI thread.</param>
    /// <param name="caption">The caption shown before the link, if any.</param>
    public ActionSetting(string text, Action<SettingActionContext> execute, string caption = "")
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(execute);
        Text = text;
        Execute = execute;
        Caption = caption;
    }

    /// <summary>A link that runs <paramref name="execute"/>, which needs neither the owner window nor the values of the page.</summary>
    public ActionSetting(string text, Action execute, string caption = "")
        : this(text, _ => execute(), caption)
    {
        ArgumentNullException.ThrowIfNull(execute);
    }

    public string Name => "ActionSetting";

    public string Caption { get; }

    /// <summary>The text of the link.</summary>
    public string Text { get; }

    /// <summary>The action of the link.</summary>
    public Action<SettingActionContext> Execute { get; }
}

/// <summary>What an <see cref="ActionSetting"/> gets when its link is clicked.</summary>
public sealed class SettingActionContext
{
    public SettingActionContext(WindowOwner owner, SettingsSource values)
    {
        ArgumentNullException.ThrowIfNull(values);
        Owner = owner;
        Values = values;
    }

    /// <summary>The settings window: the owner of the dialogs and message boxes of the action.</summary>
    public WindowOwner Owner { get; }

    /// <summary>
    ///  The values being edited on the page, not saved yet: read them with the settings of the page (e.g.
    ///  <see cref="StringSetting.ValueOrDefault"/>), change them by setting them (e.g. with the indexer of
    ///  <see cref="StringSetting"/>); the page shows the changed values, which are saved with the page. Its
    ///  <see cref="SettingsSource.SettingLevel"/> is the level shown by the page.
    /// </summary>
    public SettingsSource Values { get; }
}
