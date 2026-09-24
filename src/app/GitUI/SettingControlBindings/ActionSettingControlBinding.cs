using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Settings;

namespace GitUI.SettingControlBindings;

/// <summary>The link of an <see cref="ActionSetting"/> (plugin API v2), which runs its action on the values being edited.</summary>
internal sealed class ActionSettingControlBinding : SettingControlBinding<ActionSetting, LinkLabel>
{
    public ActionSettingControlBinding(ActionSetting setting)
        : base(setting, customControl: null)
    {
    }

    /// <summary>The values being edited on the page, set by the page (<see cref="EditedSettingValues.Connect"/>).</summary>
    public EditedSettingValues? EditedValues { get; set; }

    public override LinkLabel CreateControl()
    {
        LinkLabel link = new() { Text = Setting.Text, AutoSize = true };
        link.LinkClicked += (_, _) => Execute(link);
        return link;
    }

    public override void LoadSetting(SettingsSource settings, LinkLabel control)
    {
    }

    public override void SaveSetting(SettingsSource settings, LinkLabel control)
    {
    }

    /// <summary>Runs the action, as a click of the link.</summary>
    internal void Execute(Control link)
    {
        WindowOwner owner = ((IWin32Window?)link.FindForm() ?? link).ToWindowOwner();
        if (EditedValues is { } editedValues)
        {
            editedValues.Run(values => Setting.Execute(new SettingActionContext(owner, values)));
        }
        else
        {
            Setting.Execute(new SettingActionContext(owner, new MemorySettingsSource()));
        }
    }
}
