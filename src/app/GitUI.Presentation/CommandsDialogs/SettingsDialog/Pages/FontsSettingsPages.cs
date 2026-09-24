using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands;
using GitExtensions.Extensibility.Settings;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

/// <summary>Strings of the fonts settings; ids match <c>AppearanceFontsSettingsPage</c>.</summary>
public sealed class AppearanceFontsSettingsPageStrings : ViewStrings
{
    public AppearanceFontsSettingsPageStrings()
        : base("AppearanceFontsSettingsPage")
    {
        Title = Add("$this", "Text", "Fonts");
        Fonts = Add("gbFonts", "Text", "Fonts (restart required)");
        CodeFont = Add("label56", "Text", "Code font");
        ApplicationFont = Add("label26", "Text", "Application font");
        CommitFont = Add("label34", "Text", "Commit font");
        MonospaceFont = Add("label36", "Text", "Monospace font");
        ShowEolMarkerAsGlyph = Add("ShowEolMarkerAsGlyph", "Text", "Show end-of-line markers as glyph instead of \"\\r\\n\" etc.");
    }

    public TranslatedText Title { get; }

    public TranslatedText Fonts { get; }

    public TranslatedText CodeFont { get; }

    public TranslatedText ApplicationFont { get; }

    public TranslatedText CommitFont { get; }

    public TranslatedText MonospaceFont { get; }

    public TranslatedText ShowEolMarkerAsGlyph { get; }
}

/// <summary>Port of <c>AppearanceFontsSettingsPage</c> (global settings).</summary>
public sealed partial class AppearanceFontsSettingsPageViewModel(AppearanceFontsSettingsPageStrings strings, ISettingsFontsHost fonts) : SettingsPageViewModel
{
    public AppearanceFontsSettingsPageStrings Strings { get; } = strings;

    public override string Title => Strings.Title.Text;

    public override string PageName => "AppearanceFontsSettingsPage";

    public override IEnumerable<string> SearchKeywords => Strings.Texts;

    /// <summary>As <c>_diffFont</c>, shown by <c>diffFontChangeButton</c>.</summary>
    [ObservableProperty]
    public partial SettingsFont? CodeFont { get; private set; }

    [ObservableProperty]
    public partial SettingsFont? ApplicationFont { get; private set; }

    [ObservableProperty]
    public partial SettingsFont? CommitFont { get; private set; }

    [ObservableProperty]
    public partial SettingsFont? MonospaceFont { get; private set; }

    [ObservableProperty]
    public partial bool ShowEolMarkerAsGlyph { get; set; }

    [RelayCommand]
    private void ChangeCodeFont() => CodeFont = fonts.PickFont(SettingsFontKind.Code, CodeFont) ?? CodeFont;

    [RelayCommand]
    private void ChangeApplicationFont() => ApplicationFont = fonts.PickFont(SettingsFontKind.Application, ApplicationFont) ?? ApplicationFont;

    [RelayCommand]
    private void ChangeCommitFont() => CommitFont = fonts.PickFont(SettingsFontKind.Commit, CommitFont) ?? CommitFont;

    [RelayCommand]
    private void ChangeMonospaceFont() => MonospaceFont = fonts.PickFont(SettingsFontKind.Monospace, MonospaceFont) ?? MonospaceFont;

    protected override void SettingsToPage(SettingsSource? settings)
    {
        ApplicationFont = fonts.GetFont(SettingsFontKind.Application);
        CodeFont = fonts.GetFont(SettingsFontKind.Code);
        CommitFont = fonts.GetFont(SettingsFontKind.Commit);
        MonospaceFont = fonts.GetFont(SettingsFontKind.Monospace);

        ShowEolMarkerAsGlyph = AppSettings.ShowEolMarkerAsGlyph;

        base.SettingsToPage(settings);
    }

    protected override void PageToSettings(SettingsSource? settings)
    {
        fonts.SetFont(SettingsFontKind.Code, CodeFont);
        fonts.SetFont(SettingsFontKind.Application, ApplicationFont);
        fonts.SetFont(SettingsFontKind.Commit, CommitFont);
        fonts.SetFont(SettingsFontKind.Monospace, MonospaceFont);

        AppSettings.ShowEolMarkerAsGlyph = ShowEolMarkerAsGlyph;

        base.PageToSettings(settings);
    }
}

/// <summary>Strings of the console style settings; ids match <c>ConsoleStyleSettingsPage</c>.</summary>
public sealed class ConsoleStyleSettingsPageStrings : ViewStrings
{
    public ConsoleStyleSettingsPageStrings()
        : base("ConsoleStyleSettingsPage")
    {
        Title = Add("$this", "Text", "Console style");
        DefaultThemeDisplayName = Add("_defaultThemeDisplayName", "Text", "Default");
        ConsoleDefaultFontText = Add("_consoleDefaultFontText", "Text", "Console Default");
        ConsoleSettings = Add("groupBoxConsoleSettings", "Text", "Console settings (restart required)");
        ConsoleEmulator = Add("lblConsoleEmulator", "Text", "Console emulator");
        ConsoleStyle = Add("label1", "Text", "Console style");
        Font = Add("lblFontName", "Text", "Font");
        ResetFontTooltip = Add("consoleFontResetButton", "consoleFontToolTip", "Reset to default");
    }

    public TranslatedText Title { get; }

    public TranslatedText DefaultThemeDisplayName { get; }

    public TranslatedText ConsoleDefaultFontText { get; }

    public TranslatedText ConsoleSettings { get; }

    public TranslatedText ConsoleEmulator { get; }

    public TranslatedText ConsoleStyle { get; }

    public TranslatedText Font { get; }

    public TranslatedText ResetFontTooltip { get; }
}

/// <summary>A console emulator (<c>IConsoleEmulator</c>) and its themes.</summary>
public sealed record ConsoleEmulatorChoice(string Name, string DisplayName, IReadOnlyList<string> AvailableThemes)
{
    public override string ToString() => DisplayName;
}

/// <summary>What the console style settings need from the application: the console emulators and the fonts.</summary>
public interface IConsoleStyleSettingsPageHost : ISettingsFontsHost
{
    /// <summary>As <c>IConsoleEmulatorsRegistry.AvailableConsoleEmulators</c>.</summary>
    IReadOnlyList<ConsoleEmulatorChoice> ConsoleEmulators { get; }
}

/// <summary>Port of <c>ConsoleStyleSettingsPage</c> (global settings).</summary>
public sealed partial class ConsoleStyleSettingsPageViewModel : SettingsPageViewModel
{
    private readonly IConsoleStyleSettingsPageHost _host;

    public ConsoleStyleSettingsPageViewModel(ConsoleStyleSettingsPageStrings strings, IConsoleStyleSettingsPageHost host)
    {
        Strings = strings;
        _host = host;
        ConsoleEmulators = host.ConsoleEmulators;
    }

    public ConsoleStyleSettingsPageStrings Strings { get; }

    public override string Title => Strings.Title.Text;

    public override string PageName => "ConsoleStyleSettingsPage";

    public override IEnumerable<string> SearchKeywords => Strings.Texts;

    /// <summary>The items of <c>cboConsoleEmulator</c>.</summary>
    public IReadOnlyList<ConsoleEmulatorChoice> ConsoleEmulators { get; }

    [ObservableProperty]
    public partial ConsoleEmulatorChoice? ConsoleEmulator { get; set; }

    /// <summary>The items of <c>_NO_TRANSLATE_cboStyle</c>: the default, then the themes of the emulator.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<string> Styles { get; private set; } = [];

    [ObservableProperty]
    public partial int StyleIndex { get; set; } = -1;

    /// <summary>As <c>_NO_TRANSLATE_cboStyle.Enabled</c>: the emulator has themes.</summary>
    [ObservableProperty]
    public partial bool IsStyleEnabled { get; private set; }

    /// <summary>As <c>_consoleFont</c>: none for the default font of the console.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConsoleFontText), nameof(HasConsoleFont))]
    public partial SettingsFont? ConsoleFont { get; private set; }

    /// <summary>The text of <c>consoleFontChangeButton</c>.</summary>
    public string ConsoleFontText => ConsoleFont?.Text ?? Strings.ConsoleDefaultFontText.Text;

    /// <summary>As <c>consoleFontResetButton.Visible</c>.</summary>
    public bool HasConsoleFont => ConsoleFont is not null;

    partial void OnConsoleEmulatorChanged(ConsoleEmulatorChoice? value) => RefreshThemeDropdown();

    /// <summary>As <c>RefreshThemeDropdown</c>: the themes of the emulator, the saved one selected.</summary>
    private void RefreshThemeDropdown()
    {
        if (ConsoleEmulator is not { } emulator)
        {
            Styles = [];
            StyleIndex = -1;
            IsStyleEnabled = false;
            return;
        }

        Styles = [Strings.DefaultThemeDisplayName.Text, .. emulator.AvailableThemes];
        IsStyleEnabled = emulator.AvailableThemes.Count > 0;

        string? saved = AppSettings.ConEmuStyle.Value;
        int matchIndex = string.IsNullOrEmpty(saved)
            ? 0
            : FindThemeIndex(saved);
        StyleIndex = matchIndex >= 0 ? matchIndex : 0;
        return;

        int FindThemeIndex(string theme)
        {
            for (int index = 0; index < emulator.AvailableThemes.Count; index++)
            {
                if (string.Equals(emulator.AvailableThemes[index], theme, StringComparison.OrdinalIgnoreCase))
                {
                    return index + 1; // skip "Default"
                }
            }

            return -1;
        }
    }

    [RelayCommand]
    private void ChangeConsoleFont() => ConsoleFont = _host.PickFont(SettingsFontKind.Console, ConsoleFont) ?? ConsoleFont;

    [RelayCommand]
    private void ResetConsoleFont() => ConsoleFont = null;

    protected override void SettingsToPage(SettingsSource? settings)
    {
        ConsoleEmulatorChoice? emulator = ConsoleEmulators.FirstOrDefault(e => string.Equals(e.Name, AppSettings.ConsoleEmulatorName.Value, StringComparison.OrdinalIgnoreCase))
            ?? ConsoleEmulators.FirstOrDefault();
        if (emulator == ConsoleEmulator)
        {
            RefreshThemeDropdown();
        }
        else
        {
            ConsoleEmulator = emulator;
        }

        ConsoleFont = _host.GetFont(SettingsFontKind.Console);

        base.SettingsToPage(settings);
    }

    protected override void PageToSettings(SettingsSource? settings)
    {
        AppSettings.ConsoleEmulatorName.Value = ConsoleEmulator?.Name ?? "";
        if (StyleIndex >= 0 && StyleIndex < Styles.Count)
        {
            AppSettings.ConEmuStyle.Value = Styles[StyleIndex];
        }

        _host.SetFont(SettingsFontKind.Console, ConsoleFont);

        base.PageToSettings(settings);
    }
}
