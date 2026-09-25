using CommunityToolkit.Mvvm.Input;

namespace GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

/// <summary>What the settings pages need from the application: links, the user manual, errors, the translated strings.</summary>
public interface ISettingsPageServices
{
    /// <summary>As <c>OsShellUtil.OpenUrlInDefaultBrowser</c>.</summary>
    void OpenUrl(string url);

    /// <summary>
    ///  As the info icon of <c>SettingsCheckBox</c> and the help icons of the pages: opens the section of the user manual
    ///  (<c>UserManual.UrlFor</c>).
    /// </summary>
    void OpenUserManual(string subFolder, string anchorName);

    /// <summary>As <c>ResourceManager.TranslatedStrings.Reinitialize</c> and <c>TranslatedStrings.Reinitialize</c>.</summary>
    void ReinitializeTranslatedStrings();

    /// <summary>As <c>MessageBoxes.ShowError</c>.</summary>
    void ShowError(string text);
}

/// <summary>A settings page using the <see cref="ISettingsPageServices"/>, e.g. for the info icons of its check boxes.</summary>
public abstract partial class SettingsPageWithServicesViewModel(ISettingsPageServices services) : SettingsPageViewModel
{
    protected ISettingsPageServices Services { get; } = services;

    /// <summary>
    ///  As clicking the info icon of a <c>SettingsCheckBox</c>: the section of the settings manual
    ///  (<c>ManualSectionAnchorName</c>, in the default <c>ManualSectionSubfolder</c> "settings").
    /// </summary>
    [RelayCommand]
    private void OpenSettingsManual(string anchorName) => Services.OpenUserManual("settings", anchorName);
}

/// <summary>The fonts of the settings (<c>AppSettings.Font</c> and the others).</summary>
public enum SettingsFontKind
{
    /// <summary><c>AppSettings.Font</c>.</summary>
    Application,

    /// <summary><c>AppSettings.FixedWidthFont</c> (the code font, fixed pitch only).</summary>
    Code,

    /// <summary><c>AppSettings.CommitFont</c>.</summary>
    Commit,

    /// <summary><c>AppSettings.MonospaceFont</c>.</summary>
    Monospace,

    /// <summary><c>AppSettings.ConEmuConsoleFont</c>, none for the default of the console.</summary>
    Console,
}

/// <summary>A font of the settings, as the text and the font of the button showing it (<c>SetFontButtonText</c>).</summary>
/// <param name="Value">The font of the host (a <c>FontDescriptor</c>), saved as is.</param>
/// <param name="SizeInPoints">The size of the font, in points.</param>
public sealed record SettingsFont(object Value, string FamilyName, float SizeInPoints, bool IsBold, bool IsItalic)
{
    /// <summary>
    ///  As <c>SetFontButtonText</c>: the name and the rounded size; on macOS with its decimals, since the default UI font is
    ///  9.75 points there (13 pixels), which would read 10.
    /// </summary>
    public string Text => OperatingSystem.IsMacOS()
        ? $"{FamilyName}, {SizeInPoints.ToString("0.##", System.Globalization.CultureInfo.CurrentCulture)}"
        : $"{FamilyName}, {(int)(SizeInPoints + 0.5f)}";

    /// <summary>The size in device independent pixels, to show the font.</summary>
    public double DisplaySize => SizeInPoints * 96 / 72;
}

/// <summary>The fonts of the settings and the font dialog (<c>FontDialog</c>).</summary>
public interface ISettingsFontsHost
{
    /// <summary>The font of the settings; <see langword="null"/> for the default console font.</summary>
    SettingsFont? GetFont(SettingsFontKind kind);

    void SetFont(SettingsFontKind kind, SettingsFont? font);

    /// <summary>
    ///  Shows the font dialog of the font (fixed pitch only for the code font) with the current font, or the default one
    ///  (<c>Consolas, 12</c> for the console); returns the chosen font, or <see langword="null"/> when cancelled.
    /// </summary>
    SettingsFont? PickFont(SettingsFontKind kind, SettingsFont? current);
}
