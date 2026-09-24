using GitExtensions.Extensibility.Settings;

namespace GitUI.SettingControlBindings;

/// <summary>
///  The values being edited in the controls of a page of setting bindings, for the actions of its
///  <see cref="ActionSetting"/>s (plugin API v2): they are saved in memory (with the level of the page), the action reads and
///  changes them, and the controls show the changed values.
/// </summary>
/// <param name="bindings">The bindings of the page.</param>
/// <param name="getLevel">The level of the settings shown by the page.</param>
internal sealed class EditedSettingValues(IReadOnlyList<ISettingControlBinding> bindings, Func<SettingLevel> getLevel)
{
    /// <summary>Connects the <see cref="ActionSetting"/>s of <paramref name="bindings"/> to the values of the others.</summary>
    public static void Connect(IReadOnlyList<ISettingControlBinding> bindings, Func<SettingLevel> getLevel)
    {
        EditedSettingValues editedValues = new(bindings, getLevel);
        foreach (ActionSettingControlBinding binding in bindings.OfType<ActionSettingControlBinding>())
        {
            binding.EditedValues = editedValues;
        }
    }

    /// <summary>The values shown by the controls (except the credentials, which are kept in the credential manager).</summary>
    public MemorySettingsSource GetValues()
    {
        MemorySettingsSource values = new(getLevel());
        foreach (ISettingControlBinding binding in ValueBindings)
        {
            binding.SaveSetting(values);
        }

        return values;
    }

    /// <summary>Runs <paramref name="action"/> on the values shown by the controls, then shows the values it changed.</summary>
    public void Run(Action<SettingsSource> action)
    {
        MemorySettingsSource values = GetValues();
        action(values);
        foreach (ISettingControlBinding binding in ValueBindings)
        {
            binding.LoadSetting(values);
        }
    }

    // The credentials would be saved in the credential manager.
    private IEnumerable<ISettingControlBinding> ValueBindings => bindings.Where(binding => binding.GetSetting() is not CredentialsSetting);
}
