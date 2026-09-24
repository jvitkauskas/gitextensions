using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitExtensions.Extensibility.Settings;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

/// <summary>Strings of the hotkeys settings; ids match <c>HotkeysSettingsPage</c> and <c>ControlHotkeys</c>.</summary>
public sealed class HotkeysSettingsPageStrings : ViewStrings
{
    public HotkeysSettingsPageStrings()
        : base("HotkeysSettingsPage")
    {
        Title = Add("$this", "Text", "Hotkeys");
        HotkeyableItems = Add("lHotkeyableItems", "Text", "Hotkeyable Items", category: "ControlHotkeys");
        Command = Add("columnCommand", "Text", "Command", category: "ControlHotkeys");
        Key = Add("columnKey", "Text", "Key", category: "ControlHotkeys");
        Hotkey = Add("lHotkey", "Text", "Hotkey", category: "ControlHotkeys");
        None = Add("txtHotkey", "Text", "None", category: "ControlHotkeys");
        Apply = Add("bApply", "Text", "Apply", category: "ControlHotkeys");
        Clear = Add("bClear", "Text", "Clear", category: "ControlHotkeys");
        ResetToDefaults = Add("bResetToDefaults", "Text", "Reset all Hotkeys to defaults", category: "ControlHotkeys");
    }

    public TranslatedText Title { get; }

    public TranslatedText HotkeyableItems { get; }

    public TranslatedText Command { get; }

    public TranslatedText Key { get; }

    public TranslatedText Hotkey { get; }

    public TranslatedText None { get; }

    public TranslatedText Apply { get; }

    public TranslatedText Clear { get; }

    public TranslatedText ResetToDefaults { get; }
}

/// <summary>A hotkey of a <see cref="HotkeySettingsGroup"/> (a <c>HotkeyCommand</c>): the WinForms <c>Keys</c> as an integer.</summary>
public sealed partial class HotkeyItem : ObservableObject
{
    private readonly Func<int, string> _toText;

    public HotkeyItem(int commandCode, string name, int keyData, Func<int, string> toText)
    {
        CommandCode = commandCode;
        Name = name;
        _toText = toText;
        KeyData = keyData;
    }

    public int CommandCode { get; }

    public string Name { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(KeyText))]
    public partial int KeyData { get; set; }

    /// <summary>As <c>ToText</c> of <c>Keys</c>, e.g. "Ctrl+Shift+A".</summary>
    public string KeyText => _toText(KeyData);
}

/// <summary>The hotkeys of a window (<c>HotkeySettings</c>).</summary>
public sealed class HotkeySettingsGroup(string name, IReadOnlyList<HotkeyItem> commands)
{
    public string Name { get; } = name;

    public IReadOnlyList<HotkeyItem> Commands { get; } = commands;

    public override string ToString() => Name;
}

/// <summary>The hotkeys settings (<c>IHotkeySettingsManager</c>).</summary>
public interface IHotkeysSettingsHost
{
    /// <summary>The preset hotkeys and the configured ones (<c>LoadSettings</c>).</summary>
    IReadOnlyList<HotkeySettingsGroup> LoadSettings();

    /// <summary>The preset hotkeys (<c>CreateDefaultSettings</c>).</summary>
    IReadOnlyList<HotkeySettingsGroup> CreateDefaultSettings();

    void SaveSettings(IReadOnlyList<HotkeySettingsGroup> settings);

    /// <summary>As <c>IsUniqueKey</c>: whether the key is assigned already (shown in red).</summary>
    bool IsUsedKey(int keyData);

    /// <summary>As <c>ToText</c> of <c>Keys</c>.</summary>
    string ToText(int keyData);
}

/// <summary>Port of <c>HotkeysSettingsPage</c> with its <c>ControlHotkeys</c> (global settings).</summary>
public sealed partial class HotkeysSettingsPageViewModel(HotkeysSettingsPageStrings strings, IHotkeysSettingsHost host) : SettingsPageViewModel
{
    public HotkeysSettingsPageStrings Strings { get; } = strings;

    public override string Title => Strings.Title.Text;

    public override string PageName => "HotkeysSettingsPage";

    public override IEnumerable<string> SearchKeywords => Strings.Texts;

    /// <summary>The hotkeys of each window (<c>cmbSettings</c>).</summary>
    [ObservableProperty]
    public partial IReadOnlyList<HotkeySettingsGroup> Settings { get; private set; } = [];

    [ObservableProperty]
    public partial HotkeySettingsGroup? SelectedSettings { get; set; }

    /// <summary>The hotkeys of the selected window (<c>listMappings</c>).</summary>
    public ObservableCollection<HotkeyItem> Commands { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand), nameof(ClearCommand))]
    public partial HotkeyItem? SelectedCommand { get; set; }

    /// <summary>The key typed in <c>txtHotkey</c>.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(KeyText), nameof(IsUsedKey))]
    public partial int KeyData { get; set; }

    public string KeyText => KeyData == 0 ? Strings.None.Text : host.ToText(KeyData);

    /// <summary>As the fore color of <c>txtHotkey</c>: red for a key assigned already.</summary>
    public bool IsUsedKey => KeyData != 0 && host.IsUsedKey(KeyData);

    protected override void SettingsToPage(SettingsSource? settings)
    {
        SetSettings(host.LoadSettings());
        base.SettingsToPage(settings);
    }

    protected override void PageToSettings(SettingsSource? settings)
    {
        host.SaveSettings(Settings);
        base.PageToSettings(settings);
    }

    partial void OnSelectedSettingsChanged(HotkeySettingsGroup? value)
    {
        SelectedCommand = null;
        Commands.Clear();
        foreach (HotkeyItem command in value?.Commands ?? [])
        {
            Commands.Add(command);
        }
    }

    // As UpdateTextBox.
    partial void OnSelectedCommandChanged(HotkeyItem? value) => KeyData = value?.KeyData ?? 0;

    /// <summary>As <c>bApply_Click</c>: the typed key is the key of the selected command.</summary>
    [RelayCommand(CanExecute = nameof(HasSelectedCommand))]
    private void Apply() => SelectedCommand!.KeyData = KeyData;

    /// <summary>As <c>bClear_Click</c>.</summary>
    [RelayCommand(CanExecute = nameof(HasSelectedCommand))]
    private void Clear()
    {
        SelectedCommand!.KeyData = 0;
        KeyData = 0;
    }

    /// <summary>As <c>bResetToDefaults_Click</c>.</summary>
    [RelayCommand]
    private void ResetToDefaults() => SetSettings(host.CreateDefaultSettings());

    private bool HasSelectedCommand() => SelectedCommand is not null;

    private void SetSettings(IReadOnlyList<HotkeySettingsGroup> settings)
    {
        SelectedSettings = null;
        Settings = settings;
    }
}
