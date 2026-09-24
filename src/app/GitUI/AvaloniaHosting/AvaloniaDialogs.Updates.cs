using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Git.hub;
using GitCommands;
using GitExtensions.Extensibility;
using GitUI.Avalonia.CommandsDialogs.BrowseDialog;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs.BrowseDialog;
using GitUI.Presentation.CommandsDialogs.BrowseDialog;
using GitUI.Presentation.Translations;
using Microsoft.VisualStudio.Threading;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  Routing of the check for updates dialog (docs/avalonia-port/PLAN.md, phase 2, batch 8).
/// </summary>
internal static partial class AvaloniaDialogs
{
    /// <summary>
    ///  As <c>FormUpdates.SearchForUpdatesAndShow</c>: searches for updates in the background and shows the dialog
    ///  right away if <paramref name="alwaysShow"/>, otherwise only once an update is found.
    /// </summary>
    public static bool TrySearchForUpdatesAndShow(IWin32Window? owner, bool alwaysShow)
    {
        AvaloniaUi.EnsureInitialized(GetOptions);
        UpdatesWindow window = new();
        UpdatesViewModel viewModel = new(
            ViewStrings.Load<UpdatesStrings>(),
            AppSettings.IsPortable(),
            RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant(),
            [.. UserEnvironmentInformation.GetDotnetDesktopRuntimeVersions()],
            new UpdatesHost(window),
            new MessageBoxService(window));
        window.DataContext = viewModel;

        Version currentVersion = AppSettings.AppVersion;
        ThreadHelper.FileAndForget(async () =>
        {
            await TaskScheduler.Default;

            AvailableUpdate? update = null;
            Exception? failure = null;
            try
            {
                update = SearchForUpdate(currentVersion);
            }
            catch (Exception ex) when (ex.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
            {
                // GitHub API rate limiting: nothing to tell the user.
            }
            catch (InvalidAsynchronousStateException)
            {
                // The application is closing.
                return;
            }
            catch (Exception ex)
            {
                failure = ex;
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (failure is not null && window.IsVisible)
            {
                ExceptionUtils.ShowException(new NativeWindowOwner(window), failure, string.Empty, true);
            }

            viewModel.ReportSearchResult(update);
            if (update is not null && !alwaysShow)
            {
                ShowDialog(() => window, owner);
            }
        });

        if (alwaysShow)
        {
            ShowDialog(() => window, owner);
        }

        return true;
    }

    /// <summary>The newest update of <paramref name="currentVersion"/>, as <c>FormUpdates.SearchForUpdates</c>.</summary>
    private static AvailableUpdate? SearchForUpdate(Version currentVersion)
    {
        Client github = new();
        Repository gitExtRepo = github.getRepository("gitextensions", "gitextensions");
        GitHubTree? tree = gitExtRepo?.GetRef("heads/configdata")?.GetTree();
        GitHubTreeEntry? releases = tree?.Tree.FirstOrDefault(entry => "GitExtensions.releases".Equals(entry.Path, StringComparison.InvariantCultureIgnoreCase));
        if (releases?.Blob.Value is null)
        {
            return null;
        }

        ReleaseVersion? update = ReleaseVersion.GetNewerVersions(currentVersion, AppSettings.CheckForReleaseCandidates, ReleaseVersion.Parse(releases.Blob.Value.GetContent()))
            .OrderBy(version => version.ApplicationVersion)
            .LastOrDefault();
        if (update is null)
        {
            return null;
        }

        // The download page links the x64 installer.
        string updateUrl = RuntimeInformation.OSArchitecture == Architecture.X64
            ? update.DownloadPage
            : update.DownloadPage.Replace("-x64-", $"-{RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant()}-");
        return new AvailableUpdate(update.ApplicationVersion.ToString(), updateUrl, update.RequiredNetRuntimeVersion);
    }

    private sealed class UpdatesHost(DialogWindow window) : IUpdatesHost
    {
        public void OpenUrl(string url) => AvaloniaUi.RunInHostContext(() => OsShellUtil.OpenUrlInDefaultBrowser(url));

        public void DownloadAndInstall(string updateUrl, Action<string> reportDownloadFailure)
        {
            // As FormUpdates.btnUpdateNow_Click.
            ThreadHelper.FileAndForget(async () =>
            {
                string fileName = Path.GetFileName(updateUrl);
                string temp = Environment.GetEnvironmentVariable("TEMP") ?? Path.GetTempPath();
                try
                {
                    using HttpClient client = new();
                    await using Stream download = await client.GetStreamAsync(updateUrl);
                    await using FileStream file = File.Create(Path.Join(temp, fileName));
                    await download.CopyToAsync(file);
                }
                catch (Exception ex)
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    reportDownloadFailure(ex.Message);
                    return;
                }

                try
                {
                    Process process = new();
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.FileName = "msiexec.exe";
                    process.StartInfo.Arguments = $"/i \"{temp}\\{fileName}\" /qb LAUNCH=1";
                    process.Start();

                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    window.Close();

                    // As Application.Exit: all the windows are closed, which ends the application.
                    AvaloniaDialogHost.CloseAllWindows();
                }
                catch (Win32Exception)
                {
                }
            });
        }
    }
}
