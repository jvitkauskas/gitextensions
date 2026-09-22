using GitCommands;
using GitCommands.Git;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.CommandsDialogs.CommitDialog;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.CommandsDialogs.AboutBoxDialog;
using GitUI.CommandsDialogs.BrowseDialog;
using GitUI.CommandsDialogs.CommitDialog;
using GitUI.HelperDialogs;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs.CommitDialog;
using GitUI.Presentation.Translations;
using GitUI.Properties;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  Routes dialogs to their Avalonia ports (docs/avalonia-port/PLAN.md). Each <c>TryShow*</c> method returns
///  <see langword="false"/> when the Avalonia port is disabled, in which case the caller shows the WinForms form.
/// </summary>
/// <remarks>
///  This is the composition side of the port: it lives in GitUI because it gathers data and services from the
///  WinForms application (settings, environment info, progress dialogs) and hands them to the view models.
/// </remarks>
internal static class AvaloniaDialogs
{
    public static bool TryShowAbout(IWin32Window? owner)
    {
        if (!AvaloniaUi.IsEnabledFor(nameof(FormAbout)))
        {
            return false;
        }

        ShowDialog(
            () =>
            {
                AboutWindow window = new();
                window.DataContext = new AboutViewModel(
                    ViewStrings.Load<AboutStrings>(),
                    AppSettings.ApplicationName,
                    UserEnvironmentInformation.GetInformation().Replace("- ", "").TrimEnd(),
                    GetContributors(),
                    FormDonate.DonationUrl,
                    new AboutDialogHost(window));
                return window;
            },
            owner);
        return true;

        // Same list as FormAbout shows.
        static IReadOnlyList<string> GetContributors()
            => new[] { Resources.Team, Resources.Coders, Resources.Translators, Resources.Designers }
                .Select(c => c.Replace(Environment.NewLine, ""))
                .SelectMany(line => line.LazySplit(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(contributor => contributor.Trim())
                .ToList();
    }

    public static bool TryShowRenameBranch(IWin32Window? owner, IGitUICommands commands, string branch, out bool renamed)
    {
        renamed = false;
        if (!AvaloniaUi.IsEnabledFor(nameof(FormRenameBranch)))
        {
            return false;
        }

        renamed = ShowDialog(
            () =>
            {
                RenameBranchWindow window = new();
                window.DataContext = new RenameBranchViewModel(
                    ViewStrings.Load<RenameBranchStrings>(),
                    branch,
                    commands.GetRequiredService<IGitBranchNameNormaliser>(),
                    new GitBranchNameOptions(AppSettings.AutoNormaliseSymbol),
                    AppSettings.AutoNormaliseBranchName,
                    (oldName, newName) => AvaloniaUi.RunInHostContext(() => FormProcess.ShowDialog(
                        new NativeWindowOwner(window),
                        commands,
                        arguments: Commands.RenameBranch(oldName, newName),
                        commands.Module.WorkingDir,
                        input: null,
                        useDialogSettings: true)));
                return window;
            },
            owner);
        return true;
    }

    public static bool TryShowCommitTemplateSettings(IWin32Window? owner)
    {
        if (!AvaloniaUi.IsEnabledFor(nameof(FormCommitTemplateSettings)))
        {
            return false;
        }

        ShowDialog(
            () => new CommitTemplateSettingsWindow
            {
                DataContext = new CommitTemplateSettingsViewModel(
                    ViewStrings.Load<CommitTemplateSettingsStrings>(),
                    new AppSettingsCommitMessageSettingsStore()),
            },
            owner);
        return true;
    }

    private static bool ShowDialog(Func<DialogWindow> createWindow, IWin32Window? owner)
    {
        AvaloniaUi.EnsureInitialized(GetOptions);
        return AvaloniaDialogHost.ShowDialog(createWindow(), owner?.Handle ?? 0);
    }

    private static AvaloniaUiOptions GetOptions()
    {
        Font font = AppSettings.Font;
        return new AvaloniaUiOptions(
            IsDarkTheme: Application.IsDarkModeEnabled,
            FontFamily: font.FontFamily.Name,
            FontSize: font.SizeInPoints * 96 / 72);
    }

    /// <summary>Lets an Avalonia window own WinForms dialogs (e.g. progress or message boxes it opens).</summary>
    private sealed class NativeWindowOwner(DialogWindow window) : IWin32Window
    {
        public nint Handle => window.NativeHandle;
    }

    private sealed class AboutDialogHost(DialogWindow window) : IAboutDialogHost
    {
        public void OpenUrl(string url) => AvaloniaUi.RunInHostContext(() => OsShellUtil.OpenUrlInDefaultBrowser(url));

        public void ShowContributors() => AvaloniaUi.RunInHostContext(() =>
        {
            using FormContributors formContributors = new();
            formContributors.ShowDialog(new NativeWindowOwner(window));
        });

        public void CopyEnvironmentInfo() => AvaloniaUi.RunInHostContext(UserEnvironmentInformation.CopyInformation);
    }
}
