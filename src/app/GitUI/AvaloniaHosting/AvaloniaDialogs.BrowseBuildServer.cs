using System.ComponentModel;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Text;
using GitCommands;
using GitCommands.Config;
using GitCommands.Remotes;
using GitCommands.Settings;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.BuildServerIntegration;
using GitExtensions.Extensibility.Configurations;
using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Settings;
using GitExtUtils;
using GitExtUtils.GitUI;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs.SettingsDialog;
using GitUI.CommandsDialogs.SettingsDialog.Pages;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Services;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUI.UserControls;
using GitUIPluginInterfaces;
using GitUIPluginInterfaces.BuildServerIntegration;
using Microsoft.VisualStudio.Threading;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  The build server integration of the Avalonia main window: the build statuses of the grid (<c>BuildServerWatcher</c>) and
///  the build report tab (<c>BuildReportTabPageExtension</c>) with WebView2, or the WinForms <c>WebBrowserControl</c> without
///  the WebView2 runtime, as a child window (<see cref="BrowseWebViews"/>).
/// </summary>
internal static partial class AvaloniaDialogs
{
    /// <summary>The build server adapter of the main window in the tests, instead of the plugins (none if it returns null).</summary>
    internal static Func<IBuildServerAdapter?>? BuildServerAdapterForTests { get; set; }

    /// <summary>Whether the build report tab is enabled in the tests, instead of <c>BuildServerSettings.ShowBuildResultPage</c>.</summary>
    internal static bool? ShowBuildResultPageForTests { get; set; }

    private sealed partial class BrowseHost : IBrowseBuildReportHost
    {
        public bool IsBuildReportEnabled
            => ShowBuildResultPageForTests ?? BuildServerSettings.ShowBuildResultPage.ValueOrDefault(Module.GetEffectiveSettings());

        public IBrowseWebView? CreateWebView()
            => BrowseWebViews.Create(
                BrowseWebViews.GetAvailableBrowserVersion,
                () => new WebView2BrowseWebView(BrowseWebViews.UserDataFolder, OpenUrl));

        public void OpenUrl(string url) => OsShellUtil.OpenUrlInDefaultBrowser(url);
    }

    /// <summary>
    ///  Port of <c>BuildServerWatcher</c> for the Avalonia grid: after each load of the revisions, the build statuses of the
    ///  build server integration plugin (the configured or detected one) are set on the revisions shown
    ///  (<c>GitRevision.BuildStatus</c>), which the build status column and the build report tab show.
    /// </summary>
    /// <remarks>The logic is copied from <c>BuildServerWatcher</c>, which is tied to the WinForms grid; re-port its changes.</remarks>
    private sealed class GridBuildServerWatcher : IBuildServerWatcher, IDisposable
    {
        private static readonly TimeSpan ShortPollInterval = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan LongPollInterval = TimeSpan.FromSeconds(120);
        private readonly CancellationTokenSequence _launchCancellation = new();
        private readonly Lock _buildServerCredentialsLock = new();
        private readonly Lock _observerLock = new();
        private readonly IGitUICommands _commands;
        private readonly RevisionGridViewModel _grid;
        private readonly Func<IWin32Window> _owner;
        private readonly IRepoNameExtractor _repoNameExtractor;
        private IDisposable? _buildStatusCancellationToken;
        private IBuildServerAdapter? _buildServerAdapter;
        private bool _disposed;

        public GridBuildServerWatcher(IGitUICommands commands, RevisionGridViewModel grid, Func<IWin32Window> owner)
        {
            _commands = commands;
            _grid = grid;
            _owner = owner;
            _repoNameExtractor = new RepoNameExtractor(() => _commands.Module);

            // As BuildStatusColumnProvider.ApplySettings: the column of an enabled integration (else once a server is detected).
            grid.ShowBuildStatusColumn = BuildServerSettings.IntegrationEnabled.ValueOrDefault(commands.Module.GetEffectiveSettings());

            // As PerformRefreshRevisions: the fetch is cancelled when the revisions are loaded again, and launched when loaded.
            grid.PropertyChanged += OnGridPropertyChanged;
            grid.Loaded += OnGridLoaded;
        }

        private IGitModule Module => _commands.Module;

        public void Dispose()
        {
            _disposed = true;
            _grid.PropertyChanged -= OnGridPropertyChanged;
            _grid.Loaded -= OnGridLoaded;
            CancelBuildStatusFetchOperation();
            _launchCancellation.CancelCurrent();
            _buildServerAdapter?.Dispose();
            _buildServerAdapter = null;
        }

        private void OnGridPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(RevisionGridViewModel.IsLoading) && _grid.IsLoading)
            {
                CancelBuildStatusFetchOperation();
            }
        }

        private void OnGridLoaded(object? sender, EventArgs e)
        {
            if (!_disposed)
            {
                ThreadHelper.FileAndForget(LaunchBuildServerInfoFetchOperationAsync);
            }
        }

        // As BuildServerWatcher.LaunchBuildServerInfoFetchOperationAsync.
        public async Task LaunchBuildServerInfoFetchOperationAsync()
        {
            await TaskScheduler.Default;

            CancelBuildStatusFetchOperation();

            CancellationToken launchToken = _launchCancellation.Next();

            IBuildServerAdapter? buildServerAdapter = await GetBuildServerAdapterAsync().ConfigureAwait(false);

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(launchToken);

            _buildServerAdapter?.Dispose();
            _buildServerAdapter = buildServerAdapter;

            // When a build server adapter is available (including auto-detected), the column is shown as the user's
            // display preferences say (the icon and/or the text).
            if (buildServerAdapter is not null)
            {
                _grid.ShowBuildStatusColumn = true;
            }

            await TaskScheduler.Default;

            if (buildServerAdapter is null || launchToken.IsCancellationRequested)
            {
                return;
            }

            NewThreadScheduler scheduler = NewThreadScheduler.Default;

            // Run this first as it (may) force start queries
            IObservable<BuildInfo> runningBuildsObservable = buildServerAdapter.GetRunningBuilds(scheduler);

            IObservable<BuildInfo> fullDayObservable = buildServerAdapter.GetFinishedBuildsSince(scheduler, DateTime.Today - TimeSpan.FromDays(3));
            IObservable<BuildInfo> fullObservable = buildServerAdapter.GetFinishedBuildsSince(scheduler);

            bool anyRunningBuilds = false;
            IObservable<BuildInfo> delayObservable = Observable.Defer(() => Observable.Empty<BuildInfo>()
                                                                   .DelaySubscription(anyRunningBuilds ? ShortPollInterval : LongPollInterval));

            bool shouldLookForNewlyFinishedBuilds = false;
            DateTime nowFrozen = DateTime.Now;

            // All finished builds have already been retrieved,
            // so looking for new finished builds make sense only if running builds have been found previously
            IObservable<BuildInfo> fromNowObservable = Observable.If(() => shouldLookForNewlyFinishedBuilds,
                buildServerAdapter.GetFinishedBuildsSince(scheduler, nowFrozen)
                            .Finally(() => shouldLookForNewlyFinishedBuilds = false));

            CancelBuildStatusFetchOperation();
            lock (_observerLock)
            {
                _buildStatusCancellationToken = new CompositeDisposable
                    {
                        fullDayObservable.OnErrorResumeNext(fullObservable)
                                         .OnErrorResumeNext(Observable.Empty<BuildInfo>()
                                                                      .DelaySubscription(LongPollInterval)
                                                                      .OnErrorResumeNext(fromNowObservable)
                                                                      .Retry()
                                                                      .Repeat())
                                         .ObserveOn(MainThreadScheduler.Instance)
                                         .Subscribe(UpdateAndReportExceptions),

                        runningBuildsObservable.Do(buildInfo =>
                                                    {
                                                        anyRunningBuilds = true;
                                                        shouldLookForNewlyFinishedBuilds = true;
                                                    })
                                               .OnErrorResumeNext(delayObservable)
                                               .Retry()
                                               .Finally(() => anyRunningBuilds = false)
                                               .Repeat()
                                               .ObserveOn(MainThreadScheduler.Instance)
                                               .Subscribe(UpdateAndReportExceptions)
                    };
            }

            return;

            void UpdateAndReportExceptions(BuildInfo buildInfo)
            {
                TaskManager.HandleExceptions(() => OnBuildInfoUpdate(buildInfo), Application.OnThreadException);
            }
        }

        public void CancelBuildStatusFetchOperation()
        {
            IDisposable? cancellationToken = Interlocked.Exchange(ref _buildStatusCancellationToken, null);

            cancellationToken?.Dispose();
        }

        // As BuildServerWatcher.GetBuildServerCredentials (the credentials in the isolated storage, else asked).
        public IBuildServerCredentials? GetBuildServerCredentials(IBuildServerAdapter buildServerAdapter, bool useStoredCredentialsIfExisting)
        {
            lock (_buildServerCredentialsLock)
            {
                IBuildServerCredentials? buildServerCredentials = new BuildServerCredentials { BuildServerCredentialsType = BuildServerCredentialsType.Guest };
                bool foundInConfig = false;

                const string CredentialsConfigName = "Credentials";
                const string UseGuestAccessKey = "UseGuestAccess";
                const string BuildServerCredentialsTypeKey = "BuildServerCredentialsType";
                const string UsernameKey = "Username";
                const string PasswordKey = "Password";
                const string BearerTokenKey = "BearerToken";
                using (IsolatedStorageFileStream stream = GetBuildServerOptionsIsolatedStorageStream(buildServerAdapter, FileAccess.Read, FileShare.Read))
                {
                    if (stream.Position < stream.Length)
                    {
                        byte[] protectedData = new byte[stream.Length];

                        stream.ReadExactly(protectedData, 0, (int)stream.Length);
                        try
                        {
                            byte[] unprotectedData = ProtectedData.Unprotect(protectedData, null, DataProtectionScope.CurrentUser);
                            using MemoryStream memoryStream = new(unprotectedData);
                            ConfigFile credentialsConfig = new(fileName: "");

                            using (StreamReader textReader = new(memoryStream, Encoding.UTF8))
                            {
                                credentialsConfig.LoadFromString(textReader.ReadToEnd());
                            }

                            IConfigSection? section = credentialsConfig.FindConfigSection(CredentialsConfigName);

                            if (section is not null)
                            {
                                string? buildServerCredentialsType = section.GetValue(BuildServerCredentialsTypeKey);
                                if (!string.IsNullOrWhiteSpace(buildServerCredentialsType))
                                {
                                    if (!Enum.TryParse(buildServerCredentialsType, ignoreCase: true, out BuildServerCredentialsType credentialsType))
                                    {
                                        credentialsType = BuildServerCredentialsType.Guest;
                                    }

                                    buildServerCredentials.BuildServerCredentialsType = credentialsType;
                                }
                                else
                                {
                                    buildServerCredentials.BuildServerCredentialsType =
                                        section.GetValueAsBool(UseGuestAccessKey, true)
                                            ? BuildServerCredentialsType.Guest
                                            : BuildServerCredentialsType.UsernameAndPassword;
                                }

                                buildServerCredentials.Username = section.GetValue(UsernameKey);
                                buildServerCredentials.Password = section.GetValue(PasswordKey);
                                buildServerCredentials.BearerToken = section.GetValue(BearerTokenKey);
                                foundInConfig = true;

                                if (useStoredCredentialsIfExisting)
                                {
                                    return buildServerCredentials;
                                }
                            }
                        }
                        catch (CryptographicException)
                        {
                            // The data is protected per user: the user can reset the credentials.
                            useStoredCredentialsIfExisting = false;
                        }
                    }
                }

                if (!useStoredCredentialsIfExisting || !foundInConfig)
                {
                    buildServerCredentials = ThreadHelper.JoinableTaskFactory.Run(() => ShowBuildServerCredentialsFormAsync(buildServerAdapter.UniqueKey, buildServerCredentials));

                    if (buildServerCredentials is not null)
                    {
                        ConfigFile credentialsConfig = new(fileName: "");

                        IConfigSection section = credentialsConfig.FindOrCreateConfigSection(CredentialsConfigName);

                        section.SetValue(BuildServerCredentialsTypeKey, buildServerCredentials.BuildServerCredentialsType.ToString());
                        section.SetValue(UsernameKey, buildServerCredentials.Username);
                        section.SetValue(PasswordKey, buildServerCredentials.Password);
                        section.SetValue(BearerTokenKey, buildServerCredentials.BearerToken);

                        using IsolatedStorageFileStream stream = GetBuildServerOptionsIsolatedStorageStream(buildServerAdapter, FileAccess.Write, FileShare.None);
                        using MemoryStream memoryStream = new();
                        using (StreamWriter textWriter = new(memoryStream, Encoding.UTF8))
                        {
                            textWriter.Write(credentialsConfig.GetAsString());
                        }

                        byte[] protectedData = ProtectedData.Protect(memoryStream.ToArray(), null, DataProtectionScope.CurrentUser);
                        stream.Write(protectedData, 0, protectedData.Length);

                        return buildServerCredentials;
                    }
                }

                return null;
            }
        }

        // As BuildServerWatcher.ReplaceVariables.
        public string ReplaceVariables(string projectNames)
        {
            (string repoProject, string repoName) = _repoNameExtractor.Get();

            if (!string.IsNullOrWhiteSpace(repoProject))
            {
                projectNames = projectNames.Replace("{cRepoProject}", repoProject);
            }

            if (!string.IsNullOrWhiteSpace(repoName))
            {
                projectNames = projectNames.Replace("{cRepoShortName}", repoName);
            }

            return projectNames;
        }

        private async Task<IBuildServerCredentials?> ShowBuildServerCredentialsFormAsync(string buildServerUniqueKey, IBuildServerCredentials buildServerCredentials)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            return AvaloniaUi.RunInHostContext(() =>
            {
                return TryShowBuildServerCredentials(_owner(), buildServerUniqueKey, buildServerCredentials, out bool accepted) && accepted ? buildServerCredentials : null;
            });
        }

        // As BuildServerWatcher.OnBuildInfoUpdate: the latest build of each revision shown.
        private void OnBuildInfoUpdate(BuildInfo buildInfo)
        {
            lock (_observerLock)
            {
                if (_buildStatusCancellationToken is null)
                {
                    return;
                }
            }

            foreach (ObjectId commitHash in buildInfo.CommitHashList)
            {
                if (_grid.GetRevision(commitHash) is { } revision
                    && (revision.BuildStatus is null || buildInfo.StartDate >= revision.BuildStatus.StartDate))
                {
                    revision.BuildStatus = buildInfo;
                }
            }
        }

        // As BuildServerWatcher.GetBuildServerAdapterAsync.
        private async Task<IBuildServerAdapter?> GetBuildServerAdapterAsync()
        {
            await TaskScheduler.Default;

            SettingsSource effectiveSettings = Module.GetEffectiveSettings();
            if (BuildServerAdapterForTests is { } createForTests)
            {
                IBuildServerAdapter? adapter = createForTests();
                adapter?.Initialize(this, BuildServerSettings.GetSettingsSource(effectiveSettings), () => { }, objectId => _grid.GetRevision(objectId) is not null);
                return adapter;
            }

            string? buildServerName = BuildServerSettings.ServerName[effectiveSettings];

            if (!string.IsNullOrEmpty(buildServerName))
            {
                // A build server type is explicitly configured.
                // Only bail out if integration has been explicitly disabled.
                if (BuildServerSettings.IntegrationEnabled[effectiveSettings] is false)
                {
                    return null;
                }
            }
            else
            {
                // Nothing configured. Auto-detect only when the user hasn't touched
                // integration settings at all (both ServerName and IntegrationEnabled are unset).
                if (BuildServerSettings.IntegrationEnabled[effectiveSettings] is not null)
                {
                    return null;
                }

                buildServerName = TryAutoDetectBuildServerType(BuildServerSettings.GetSettingsSource(effectiveSettings));
                if (string.IsNullOrEmpty(buildServerName))
                {
                    return null;
                }
            }

            // When explicitly configured, let the matching detector populate settings from remotes
            TryPopulateSettingsForBuildServer(buildServerName, BuildServerSettings.GetSettingsSource(effectiveSettings));

            IEnumerable<Lazy<IBuildServerAdapter, IBuildServerTypeMetadata>> exports = ManagedExtensibility.GetExports<IBuildServerAdapter, IBuildServerTypeMetadata>();
            Lazy<IBuildServerAdapter, IBuildServerTypeMetadata>? export = exports.SingleOrDefault(x => x.Metadata.BuildServerType == buildServerName);

            if (export is not null)
            {
                try
                {
                    string? canBeLoaded = export.Metadata.CanBeLoaded;
                    if (!string.IsNullOrEmpty(canBeLoaded))
                    {
                        Debug.Write(export.Metadata.BuildServerType + " adapter could not be loaded: " + canBeLoaded);
                        return null;
                    }

                    IBuildServerAdapter buildServerAdapter = export.Value;

                    buildServerAdapter.Initialize(this, BuildServerSettings.GetSettingsSource(effectiveSettings),
                        () =>
                        {
                            // To run the `StartSettingsDialog()` in the UI Thread
                            ThreadHelper.JoinableTaskFactory.Run(async () =>
                            {
                                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                                AvaloniaUi.RunInHostContext(() => _commands.StartSettingsDialog(_owner(), new SettingsPageReferenceByName("BuildServerIntegrationSettingsPage")));
                            });
                        },
                        objectId => _grid.GetRevision(objectId) is not null);
                    return buildServerAdapter;
                }
                catch (InvalidOperationException ex)
                {
                    Debug.Write(ex);

                    // Invalid arguments, do not return a build server adapter
                }
            }

            return null;
        }

        // As BuildServerWatcher.TryAutoDetectBuildServerType.
        private string? TryAutoDetectBuildServerType(SettingsSource? settingsSource = null)
        {
            try
            {
                List<string> remoteUrls = GetOrderedRemoteUrls();
                if (remoteUrls.Count == 0)
                {
                    return null;
                }

                foreach (Lazy<IBuildServerAutoDetector> export in ManagedExtensibility.GetExports<IBuildServerAutoDetector>())
                {
                    if (export.Value.TryDetect(remoteUrls, settingsSource))
                    {
                        return export.Value.BuildServerType;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Write($"Auto-detect build server failed: {ex}");
            }

            return null;
        }

        // As BuildServerWatcher.TryPopulateSettingsForBuildServer.
        private void TryPopulateSettingsForBuildServer(string buildServerName, SettingsSource settingsSource)
        {
            try
            {
                List<string> remoteUrls = GetOrderedRemoteUrls();
                if (remoteUrls.Count == 0)
                {
                    return;
                }

                foreach (Lazy<IBuildServerAutoDetector> export in ManagedExtensibility.GetExports<IBuildServerAutoDetector>())
                {
                    if (export.Value.BuildServerType == buildServerName)
                    {
                        export.Value.TryDetect(remoteUrls, settingsSource);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Write($"Populate build server settings failed: {ex}");
            }
        }

        // As BuildServerWatcher.GetOrderedRemoteUrls: ordered by AppSettings.PrioritizedBuildServerRemoteNames.
        private List<string> GetOrderedRemoteUrls()
        {
            IReadOnlyList<string> remoteNames = Module.GetRemoteNames();

            string[] prioritizedNames = AppSettings.PrioritizedBuildServerRemoteNames
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            IEnumerable<string> orderedRemotes = remoteNames
                .OrderBy(r =>
                {
                    int index = Array.FindIndex(prioritizedNames, n => string.Equals(n, r, StringComparison.OrdinalIgnoreCase));
                    return index >= 0 ? index : prioritizedNames.Length;
                });

            List<string> remoteUrls = [];
            foreach (string remoteName in orderedRemotes)
            {
                string remoteUrl = Module.GetSetting(string.Format(SettingKeyString.RemoteUrl, remoteName));
                if (!string.IsNullOrWhiteSpace(remoteUrl))
                {
                    remoteUrls.Add(remoteUrl);
                }
            }

            return remoteUrls;
        }

        private static IsolatedStorageFileStream GetBuildServerOptionsIsolatedStorageStream(IBuildServerAdapter buildServerAdapter, FileAccess fileAccess, FileShare fileShare)
        {
            string fileName = string.Format("BuildServer-{0}.options", Convert.ToBase64String(Encoding.UTF8.GetBytes(buildServerAdapter.UniqueKey)));
            return new IsolatedStorageFileStream(fileName, FileMode.OpenOrCreate, fileAccess, fileShare);
        }
    }
}
