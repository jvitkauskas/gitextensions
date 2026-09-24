using GitCommands;
using GitCommands.UserRepositoryHistory;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitExtUtils.GitUI.Theming;
using GitUI.Avalonia.Hosting;
using GitUI.Avatars;
using GitUI.CommandsDialogs;
using GitUI.CommandsDialogs.SettingsDialog.Pages;
using GitUI.ConsoleEmulation;
using GitUI.Hotkey;
using GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;
using GitUI.Presentation.Services;
using GitUI.Shells;
using GitUI.Theming;
using GitUI.UserControls.RevisionGrid;
using ResourceManager.Hotkey;

namespace GitUI.AvaloniaHosting;

internal static partial class AvaloniaDialogs
{
    /// <summary>
    ///  As the constructor of <c>GeneralSettingsPage</c>: the parent directories of the recent repositories, the suggested
    ///  default clone destinations.
    /// </summary>
    private static IReadOnlyList<string> LoadRecentCloneDestinations()
    {
        IList<Repository> repositoryHistory = ThreadHelper.JoinableTaskFactory.Run(RepositoryHistoryManager.Locals.LoadRecentHistoryAsync);
        return [.. repositoryHistory.Select(x => x.GetParentPath())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)];
    }

    /// <summary>
    ///  What the settings pages (General, Appearance and its children, Advanced, Confirmations, Browse repository window,
    ///  Diff viewer) need from the application: the services of GitUI, the WinForms dialogs (fonts, messages) and the pickers
    ///  of the settings window.
    /// </summary>
    private sealed class GeneralSettingsPagesHost(IGitUICommands commands, IWin32Window? owner)
        : IAppearanceSettingsPageHost, IColorsSettingsPageHost, IConsoleStyleSettingsPageHost, IDiffViewerSettingsPageHost, IFormBrowseRepoSettingsPageHost, IFileDialogService
    {
        /// <summary>The settings window, set when it is created.</summary>
        public DialogWindow? Window { get; set; }

        private IWin32Window? Owner => Window is { } window ? new NativeWindowOwner(window) : owner;

        public void OpenUrl(string url) => OsShellUtil.OpenUrlInDefaultBrowser(url);

        public void OpenUserManual(string subFolder, string anchorName) => OsShellUtil.OpenUrlInDefaultBrowser(UserManual.UserManual.UrlFor(subFolder, anchorName));

        public void ReinitializeTranslatedStrings()
        {
            ResourceManager.TranslatedStrings.Reinitialize();
            TranslatedStrings.Reinitialize();
        }

        public void ShowError(string text) => AvaloniaUi.RunInHostContext(() => MessageBoxes.ShowError(Owner, text));

        // Appearance

        public void ClearAvatarCache() => ThreadHelper.FileAndForget(AvatarService.CacheCleaner.ClearCacheAsync);

        public void UpdateAvatarProvider()
        {
            // Clear the cache directly instead of through a throwaway control, as AppearanceSettingsPage.
            ThreadHelper.FileAndForget(async () =>
            {
                AvatarService.UpdateAvatarProvider();
                await AvatarService.CacheCleaner.ClearCacheAsync();
            });
        }

        // Colors

        public IColorsSettingsPageController CreateController(IColorsSettingsPageView page)
            => new ColorsController(new ColorsSettingsPageController(new ColorsPage(page), new ThemeRepository(), new ThemePathProvider()));

        // Fonts and console style

        public IReadOnlyList<ConsoleEmulatorChoice> ConsoleEmulators
            => [.. commands.GetRequiredService<IConsoleEmulatorsRegistry>().AvailableConsoleEmulators
                .Select(emulator => new ConsoleEmulatorChoice(emulator.Name, emulator.DisplayName, [.. emulator.AvailableThemes]))];

        public SettingsFont? GetFont(SettingsFontKind kind) => ToSettingsFont(kind switch
        {
            SettingsFontKind.Application => AppSettings.Font,
            SettingsFontKind.Code => AppSettings.FixedWidthFont,
            SettingsFontKind.Commit => AppSettings.CommitFont,
            SettingsFontKind.Monospace => AppSettings.MonospaceFont,
            _ => AppSettings.ConEmuConsoleFont,
        });

        public void SetFont(SettingsFontKind kind, SettingsFont? font)
        {
            Font? value = font?.Value as Font;
            switch (kind)
            {
                case SettingsFontKind.Console:
                    AppSettings.ConEmuConsoleFont = value;
                    break;
                case SettingsFontKind.Application when value is not null:
                    AppSettings.Font = value;
                    break;
                case SettingsFontKind.Code when value is not null:
                    AppSettings.FixedWidthFont = value;
                    break;
                case SettingsFontKind.Commit when value is not null:
                    AppSettings.CommitFont = value;
                    break;
                case SettingsFontKind.Monospace when value is not null:
                    AppSettings.MonospaceFont = value;
                    break;
            }
        }

        // As ShowFontDialog of AppearanceFontsSettingsPage and consoleFontChangeButton_Click of ConsoleStyleSettingsPage.
        public SettingsFont? PickFont(SettingsFontKind kind, SettingsFont? current) => AvaloniaUi.RunInHostContext(() =>
        {
            using FontDialog fontDialog = new()
            {
                AllowVerticalFonts = false,
                Color = SystemColors.ControlText,
                FixedPitchOnly = kind == SettingsFontKind.Code,
            };
            try
            {
                fontDialog.Font = current?.Value as Font ?? new Font("Consolas", 12);
                if (fontDialog.ShowDialog(Owner) is DialogResult.OK or DialogResult.Yes)
                {
                    return ToSettingsFont(fontDialog.Font);
                }
            }
            catch (ArgumentException ex) when (kind != SettingsFontKind.Console)
            {
                MessageBoxes.ShowError(Owner, ex.Message);
            }

            return null;
        });

        private static SettingsFont? ToSettingsFont(Font? font)
            => font is null ? null : new SettingsFont(font, font.FontFamily.Name, font.SizeInPoints, font.Bold, font.Italic);

        // Diff viewer

        public void SaveCurrentViewSettingsAsDefault() => AvaloniaDialogs.SaveCurrentViewSettingsAsDefault();

        // Browse repository window

        public IReadOnlyList<ShellChoice> GetShells()
            => [.. commands.GetRequiredService<IShellProvider>().GetShells().Select(shell => new ShellChoice(shell.Name, shell.HasExecutable))];

        public string FocusOutputHistoryHotkey
            => commands.GetRequiredService<IHotkeySettingsManager>()
                .LoadHotkeys(HotkeyCommands.BrowseSettingsName)
                .GetShortcutDisplay(HotkeyCommands.Browse.FocusOutputHistoryAndToggleIfPanel);

        public void ShowShellNotFound() => AvaloniaUi.RunInHostContext(() => MessageBoxes.ShellNotFound(Owner));

        // The pickers of the settings window.

        public Task<IReadOnlyList<string>> PickFilesAsync(bool allowMultiple, string? startDirectory = null)
            => Window is { } window ? new AvaloniaFileDialogService(window).PickFilesAsync(allowMultiple, startDirectory) : Task.FromResult<IReadOnlyList<string>>([]);

        public Task<string?> PickFileAsync(string title, string filterName, string pattern, string? startDirectory = null)
            => Window is { } window ? new AvaloniaFileDialogService(window).PickFileAsync(title, filterName, pattern, startDirectory) : Task.FromResult<string?>(null);

        public Task<string?> PickFolderAsync(string? startDirectory = null)
            => Window is { } window ? new AvaloniaFileDialogService(window).PickFolderAsync(startDirectory) : Task.FromResult<string?>(null);

        public Task<string?> PickSaveFileAsync(string title, string filterName, string extension, string? suggestedFileName = null, string? startDirectory = null)
            => Window is { } window
                ? new AvaloniaFileDialogService(window).PickSaveFileAsync(title, filterName, extension, suggestedFileName, startDirectory)
                : Task.FromResult<string?>(null);
    }

    /// <summary>The page of the Avalonia port as <c>ColorsSettingsPageController</c> sees it.</summary>
    private sealed class ColorsPage(IColorsSettingsPageView page) : IColorsSettingsPage
    {
        public ThemeId SelectedThemeId
        {
            get => page.SelectedThemeId;
            set => page.SelectedThemeId = value;
        }

        public string[] SelectedThemeVariations
        {
            get => page.SelectedThemeVariations;
            set => page.SelectedThemeVariations = value;
        }

        public bool UseSystemVisualStyle
        {
            get => page.UseSystemVisualStyle;
            set => page.UseSystemVisualStyle = value;
        }

        public bool LabelRestartIsNeededVisible
        {
            get => page.LabelRestartIsNeededVisible;
            set => page.LabelRestartIsNeededVisible = value;
        }

        public bool IsChoosingVisualStyleEnabled
        {
            get => page.IsChoosingVisualStyleEnabled;
            set => page.IsChoosingVisualStyleEnabled = value;
        }

        public void ShowThemeLoadingErrorMessage(ThemeId themeId, string[] variations, Exception ex) => page.ShowThemeLoadingErrorMessage(themeId, variations, ex);

        public void PopulateThemeMenu(IEnumerable<ThemeId> themeIds) => page.PopulateThemeMenu(themeIds);
    }

    /// <summary>The <c>ColorsSettingsPageController</c> of the WinForms page, reused by the port.</summary>
    private sealed class ColorsController(ColorsSettingsPageController controller) : IColorsSettingsPageController
    {
        public void ShowThemeSettings() => controller.ShowThemeSettings();

        public void ApplyThemeSettings() => controller.ApplyThemeSettings();

        public void HandleSelectedThemeChanged() => controller.HandleSelectedThemeChanged();

        public void HandleUseSystemVisualStyleChanged() => controller.HandleUseSystemVisualStyleChanged();

        public void HandleUseColorblindVariationChanged() => controller.HandleUseColorblindVariationChanged();

        public void ShowAppThemesDirectory() => controller.ShowAppThemesDirectory();

        public void ShowUserThemesDirectory() => controller.ShowUserThemesDirectory();
    }
}
