using CommunityToolkit.Mvvm.ComponentModel;
using GitExtensions.Extensibility.Settings;

namespace GitUI.Presentation.CommandsDialogs.SettingsDialog;

/// <summary>
///  The value of a setting shown by a settings page, loaded from and saved to a <see cref="SettingsSource"/>: the port of an
///  <c>ISettingControlBinding</c> (<c>GitUI.SettingControlBindings</c>). At a level other than
///  <see cref="SettingLevel.Effective"/>, a setting can be unset (<see langword="null"/>), which keeps the value of the lower
///  levels.
/// </summary>
public abstract class SettingValue : ObservableObject
{
    public abstract void Load(SettingsSource settings);

    public abstract void Save(SettingsSource settings);
}

/// <summary>As <c>BoolSettingControlBinding</c>: a three-state check box, unchecked-and-checked for the effective settings.</summary>
public sealed class BoolSettingValue(BoolSetting setting) : SettingValue
{
    public BoolSetting Setting { get; } = setting;

    public bool? Value
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>Whether the value can be unset (the third state of the check box), at another level than the effective one.</summary>
    public bool IsThreeState
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public override void Load(SettingsSource settings)
    {
        IsThreeState = settings.SettingLevel != SettingLevel.Effective;
        Value = settings.SettingLevel == SettingLevel.Effective ? Setting.ValueOrDefault(settings) : Setting[settings];
    }

    public override void Save(SettingsSource settings)
    {
        if (settings.SettingLevel == SettingLevel.Effective && Setting.ValueOrDefault(settings) == Value)
        {
            return;
        }

        Setting[settings] = Value;
    }
}

/// <summary>As <c>ChoiceSettingControlBinding</c>: one of the values, or none (unset).</summary>
public sealed class ChoiceSettingValue(ChoiceSetting setting) : SettingValue
{
    public ChoiceSetting Setting { get; } = setting;

    public IReadOnlyList<string> Values => [.. Setting.Values];

    public string? Value
    {
        get;
        set => SetProperty(ref field, value);
    }

    public override void Load(SettingsSource settings)
        => Value = settings.SettingLevel == SettingLevel.Effective ? Setting.ValueOrDefault(settings) : Setting[settings];

    public override void Save(SettingsSource settings)
    {
        if (settings.SettingLevel == SettingLevel.Effective && Setting.ValueOrDefault(settings) == Value)
        {
            return;
        }

        Setting[settings] = Value;
    }
}

/// <summary>
///  As <c>NumberSettingNumericUpDownBinding</c>: an integer, or no value (unset) with a placeholder at another level than the
///  effective one.
/// </summary>
public sealed class IntSettingValue(NumberSetting<int> setting) : SettingValue
{
    public NumberSetting<int> Setting { get; } = setting;

    public decimal? Value
    {
        get;
        set => SetProperty(ref field, value);
    }

    public override void Load(SettingsSource settings)
    {
        object? value = Setting[settings];
        if (value is null && settings.SettingLevel == SettingLevel.Effective)
        {
            value = Setting.DefaultValue;
        }

        Value = value is int number ? number : null;
    }

    public override void Save(SettingsSource settings)
    {
        if (Value is not decimal value)
        {
            Setting[settings] = null;
            return;
        }

        int number = (int)value;
        if (settings.SettingLevel == SettingLevel.Effective && Setting.ValueOrDefault(settings) == number)
        {
            return;
        }

        Setting[settings] = number;
    }
}

/// <summary>
///  As <c>StringSettingControlBinding</c>: a text; an empty text is no value (unset), the empty string is entered as
///  <see cref="EmptyStringValue"/>.
/// </summary>
public sealed class StringSettingValue(StringSetting setting) : SettingValue
{
    /// <summary>As <c>EmptyStringValue</c>: the text standing for an empty string, which is a value.</summary>
    public const string EmptyStringValue = "<empty string>";

    public StringSetting Setting { get; } = setting;

    public string? Value
    {
        get;
        set => SetProperty(ref field, value);
    }

    public override void Load(SettingsSource settings)
    {
        string? value = settings.SettingLevel == SettingLevel.Effective ? Setting.ValueOrDefault(settings) : Setting[settings];
        Value = value is { Length: 0 } ? EmptyStringValue : value;
    }

    public override void Save(SettingsSource settings)
    {
        // Trimmed, as the XML serializer trims it when loading.
        string? value = Value?.Trim();
        Value = value;
        if (string.IsNullOrEmpty(value))
        {
            value = null;
        }
        else if (value == EmptyStringValue)
        {
            value = "";
        }

        if (settings.SettingLevel == SettingLevel.Effective && Setting.ValueOrDefault(settings) == value)
        {
            return;
        }

        Setting[settings] = value;
    }
}

/// <summary>
///  As <c>NumberSettingTextBoxBinding</c>: a number typed in a text box, which is invalid (shown in red) when it cannot be
///  converted; an empty or invalid text is no value (unset).
/// </summary>
public sealed class NumberTextSettingValue<T>(NumberSetting<T> setting) : NumberTextSettingValue
{
    public NumberSetting<T> Setting { get; } = setting;

    /// <summary>As <c>OnTextChanged</c>: an empty text or a number.</summary>
    public override bool IsValid => string.IsNullOrEmpty(Text) || NumberSetting<T>.TryConvertFromString(Text, out _);

    public override void Load(SettingsSource settings)
    {
        object? value = settings.SettingLevel == SettingLevel.Effective ? Setting.ValueOrDefault(settings) : Setting[settings];
        Text = value?.ToString() ?? "";
    }

    public override void Save(SettingsSource settings)
    {
        if (string.IsNullOrEmpty(Text) || !NumberSetting<T>.TryConvertFromString(Text, out object? parsedValue))
        {
            Setting[settings] = null;
            return;
        }

        if (settings.SettingLevel == SettingLevel.Effective && (Setting.ValueOrDefault(settings)?.ToString() ?? "") == Text)
        {
            return;
        }

        Setting[settings] = parsedValue;
    }
}

/// <summary>The number typed in a text box of a <see cref="NumberTextSettingValue{T}"/>, whatever its type.</summary>
public abstract class NumberTextSettingValue : SettingValue
{
    private string _text = "";

    public string Text
    {
        get => _text;
        set
        {
            if (SetProperty(ref _text, value))
            {
                OnPropertyChanged(nameof(IsValid));
            }
        }
    }

    public abstract bool IsValid { get; }
}

/// <summary>As <c>PasswordSettingControlBinding</c>: a hidden text, loaded and saved as <see cref="StringSettingValue"/>.</summary>
public sealed class PasswordSettingValue(PasswordSetting setting) : SettingValue
{
    public PasswordSetting Setting { get; } = setting;

    public string? Value
    {
        get;
        set => SetProperty(ref field, value);
    }

    public override void Load(SettingsSource settings)
    {
        string? value = settings.SettingLevel == SettingLevel.Effective ? Setting.ValueOrDefault(settings) : Setting[settings];
        Value = value is { Length: 0 } ? StringSettingValue.EmptyStringValue : value;
    }

    public override void Save(SettingsSource settings)
    {
        string? value = Value?.Trim();
        Value = value;
        if (string.IsNullOrEmpty(value))
        {
            value = null;
        }
        else if (value == StringSettingValue.EmptyStringValue)
        {
            value = "";
        }

        if (settings.SettingLevel == SettingLevel.Effective && Setting.ValueOrDefault(settings) == value)
        {
            return;
        }

        Setting[settings] = value;
    }
}

/// <summary>
///  As <c>CredentialsSettingControlBinding</c>: a user name and a password kept in the credential manager, for the global and
///  local levels (and the effective one) only.
/// </summary>
public sealed class CredentialsSettingValue(CredentialsSetting setting) : SettingValue
{
    private string _userName = "";
    private string _password = "";

    public CredentialsSetting Setting { get; } = setting;

    public string UserName
    {
        get => _userName;
        set => SetProperty(ref _userName, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    /// <summary>Whether the level keeps credentials.</summary>
    public bool IsEnabled
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public override void Load(SettingsSource settings)
    {
        IsEnabled = IsSupported(settings.SettingLevel);
        if (IsEnabled)
        {
            System.Net.NetworkCredential credentials = Setting.GetValueOrDefault(settings);
            UserName = credentials.UserName;
            Password = credentials.Password;
        }
        else
        {
            UserName = "";
            Password = "";
        }
    }

    public override void Save(SettingsSource settings)
    {
        if (IsSupported(settings.SettingLevel))
        {
            Setting.SaveValue(settings, UserName, Password);

            // As the binding: the actual settings are reloaded.
            Load(settings);
        }
    }

    private static bool IsSupported(SettingLevel level) => level is SettingLevel.Global or SettingLevel.Local or SettingLevel.Effective;
}
