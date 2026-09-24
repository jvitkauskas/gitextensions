using GitCommands;
using GitCommands.Config;
using GitCommands.Git;
using GitCommands.Remotes;
using GitCommands.UserRepositoryHistory;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitExtUtils.GitUI.Theming;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.Infrastructure;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Translations;
using GitUI.Theming;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  Routing of the remotes dialog (docs/avalonia-port/PLAN.md, phase 2, batch 10).
/// </summary>
internal static partial class AvaloniaDialogs
{
    /// <param name="preselectRemote">The remote to select (<c>FormRemotes.PreselectRemoteOnLoad</c>).</param>
    /// <param name="preselectLocal">The branch to select on the pull behavior tab (<c>FormRemotes.PreselectLocalOnLoad</c>).</param>
    public static bool TryShowRemotes(IWin32Window? owner, IGitUICommands commands, string? preselectRemote, string? preselectLocal)
    {
        if (preselectRemote is not null && preselectLocal is not null)
        {
            throw new ArgumentException($"Only one option allowed: either {nameof(preselectRemote)} or {nameof(preselectLocal)}");
        }

        ShowDialog(
            () =>
            {
                RemotesWindow window = new();
                RemotesViewModel viewModel = new(
                    ViewStrings.Load<RemotesStrings>(),
                    new ConfigFileRemoteSettingsManager(() => commands.Module),
                    commands.GetRequiredService<IGitBranchNameNormaliser>(),
                    new GitBranchNameOptions(replacementToken: AppSettings.AutoNormaliseSymbol, allowTrailingSlash: true),
                    isPuttyEnabled: GitSshHelpers.IsPlink,
                    showAdvancedOptions: AppSettings.AlwaysShowAdvOpt,
                    AppSettings.CustomGenericRemoteNames,
                    new RemotesHost(commands, window),
                    new MessageBoxService(window),
                    new AvaloniaFileDialogService(window));
                viewModel.Initialize(preselectRemote, preselectLocal);
                window.DataContext = viewModel;
                return window;
            },
            owner,
            positionName: nameof(FormRemotes));
        return true;
    }

    private sealed class RemotesHost(IGitUICommands commands, DialogWindow window) : IRemotesHost
    {
        private IWin32Window Owner => new NativeWindowOwner(window);

        private IGitModule Module => commands.Module;

        public IReadOnlyList<string> LoadUrlHistory()
            => [.. ThreadHelper.JoinableTaskFactory.Run(RepositoryHistoryManager.Remotes.LoadRecentHistoryAsync).Select(r => r.Path)];

        public void UpdateUrlHistory(string? oldUrl, string newUrl, string? oldPushUrl, string? newPushUrl)
            => ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                // As FormRemotes.SaveClick.
                IList<Repository> history = await RepositoryHistoryManager.Remotes.LoadRecentHistoryAsync();
                FormRemotesController controller = new();
                controller.RemoteUpdate(history, oldUrl, newUrl);
                if (newPushUrl is not null)
                {
                    controller.RemoteUpdate(history, oldPushUrl, newPushUrl);
                }

                await RepositoryHistoryManager.Remotes.SaveRecentHistoryAsync(history);
            });

        public void OnRemoteSaved(string remoteName) => AvaloniaUi.RunInHostContext(() =>
        {
            // This will cause the module's remote colors to reload.
            Module.ResetRemoteColors();
            commands.StartPullDialogAndPullImmediately(Owner, remote: remoteName, pullAction: GitPullAction.Fetch);
            commands.RepoChangedNotifier.Notify();
        });

        public string? PickColor(string? currentColor) => AvaloniaUi.RunInHostContext(() =>
        {
            using ColorDialog dialog = new()
            {
                Color = currentColor is null ? AppColor.RemoteBranch.GetThemeColor() : ColorTranslator.FromHtml(currentColor),
            };
            return dialog.ShowDialog(Owner) == DialogResult.OK ? ColorTranslator.ToHtml(dialog.Color) : null;
        });

        public string? BrowseSshKey(string filter, string title) => AvaloniaUi.RunInHostContext(() =>
        {
            using OpenFileDialog dialog = new()
            {
                Filter = filter,
                InitialDirectory = ".",
                Title = title,
            };
            return dialog.ShowDialog(Owner) == DialogResult.OK ? dialog.FileName : null;
        });

        public void LoadSshKey(string keyFile) => AvaloniaUi.RunInHostContext(() => PuttyHelpers.StartPageantIfConfigured(() => keyFile));

        public void TestConnection(string url) => ThreadHelper.FileAndForget(() => new Plink().ConnectAsync(url));

        public IReadOnlyList<IGitRef> LoadHeads() => Module.GetRefs(RefsFilter.Heads);

        public IReadOnlyList<string> GetMergeWithCandidates(string remoteName)
        {
            // As FormRemotes.DefaultMergeWithComboDropDown.
            if (string.IsNullOrEmpty(Module.GetSetting(string.Format(SettingKeyString.RemoteUrl, remoteName))))
            {
                return [];
            }

            return [.. Module.GetRefs(RefsFilter.Remotes)
                .Where(remoteHead => remoteHead.Name.Contains(remoteName, StringComparison.CurrentCultureIgnoreCase))
                .Select(remoteHead => remoteHead.LocalName)];
        }
    }
}
