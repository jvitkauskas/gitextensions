using GitExtensions.Extensibility.Plugins;
using GitExtensions.Extensibility.Settings;

namespace GitUI.CommandsDialogs.SettingsDialog;

/// <summary>
/// Type may be a SettingsPage type or a IGitPlugin subclass type.
/// </summary>
public class SettingsPageReferenceByType : SettingsPageReference
{
    public SettingsPageReferenceByType(Type settingsPageType)
    {
        SettingsPageType = settingsPageType;
    }

    public Type SettingsPageType { get; }

    public override bool Equals(object? obj)
    {
        return obj is SettingsPageReferenceByType type && type.SettingsPageType == SettingsPageType;
    }

    public override int GetHashCode()
    {
        return SettingsPageType.GetHashCode();
    }

    public override string ToString()
    {
        return SettingsPageType.ToString();
    }
}

/// <summary>A settings page by the name of its type (as the WinForms page types were named, e.g. <c>GitConfigSettingsPage</c>).</summary>
public sealed class SettingsPageReferenceByName(string pageName) : SettingsPageReference
{
    public string PageName { get; } = pageName;

    public override bool Equals(object? obj) => obj is SettingsPageReferenceByName other && other.PageName == PageName;

    public override int GetHashCode() => PageName.GetHashCode();

    public override string ToString() => PageName;
}

public class SettingsPageReferenceByPlugin : SettingsPageReferenceByType
{
    public SettingsPageReferenceByPlugin(IGitPlugin gitPlugin)
        : base(gitPlugin.GetType())
    {
    }
}
