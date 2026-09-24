using GitCommands;
using GitCommands.Config;
using GitCommands.Git;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Configurations;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Translations;
using Microsoft.VisualStudio.Threading;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  Routing of the worktree and submodule lists (docs/avalonia-port/PLAN.md, phase 2, batch 8).
/// </summary>
internal static partial class AvaloniaDialogs
{
    /// <param name="shouldRefreshRevisionGrid">Whether a worktree was created (<c>FormManageWorktree.ShouldRefreshRevisionGrid</c>).</param>
    public static bool TryShowManageWorktree(IWin32Window? owner, IGitUICommands commands, out bool shouldRefreshRevisionGrid)
    {
        shouldRefreshRevisionGrid = false;
        ManageWorktreeViewModel? viewModel = null;
        ShowDialog(
            () =>
            {
                ManageWorktreeWindow window = new();
                viewModel = new ManageWorktreeViewModel(
                    ViewStrings.Load<ManageWorktreeStrings>(),
                    commands.Module.WorkingDir,
                    new ManageWorktreeHost(commands, window));
                window.DataContext = viewModel;
                return window;
            },
            owner);
        shouldRefreshRevisionGrid = viewModel?.ShouldRefreshRevisionGrid ?? false;
        return true;
    }

    public static bool TryShowSubmodules(IWin32Window? owner, IGitUICommands commands)
    {
        SubmodulesHost? host = null;
        try
        {
            ShowDialog(
                () =>
                {
                    SubmodulesWindow window = new();
                    host = new SubmodulesHost(commands, window);
                    window.DataContext = new SubmodulesViewModel(ViewStrings.Load<SubmodulesStrings>(), host, new MessageBoxService(window));
                    return window;
                },
                owner);
        }
        finally
        {
            host?.Dispose();
        }

        return true;
    }

    private sealed class ManageWorktreeHost(IGitUICommands commands, DialogWindow window) : IManageWorktreeHost
    {
        private IWin32Window Owner => new NativeWindowOwner(window);

        public IReadOnlyList<GitWorktree> LoadWorktrees() => commands.Module.GetWorktrees();

        public bool IsCurrentWorktree(string path)
            => new DirectoryInfo(commands.Module.WorkingDir).FullName.TrimEnd('\\') == new DirectoryInfo(path).FullName.TrimEnd('\\');

        public void Prune() => AvaloniaUi.RunInHostContext(() => commands.StartCommandLineProcessDialog(Owner, command: null, "worktree prune"));

        public bool Delete(string path) => AvaloniaUi.RunInHostContext(() => commands.WorktreeDelete(Owner, path));

        public bool Switch(string path) => AvaloniaUi.RunInHostContext(() => commands.WorktreeSwitch(Owner, path));

        public bool Create(string mainWorktreePath) => AvaloniaUi.RunInHostContext(() => commands.WorktreeCreate(Owner, mainWorktreePath));
    }

    private sealed class SubmodulesHost(IGitUICommands commands, DialogWindow window) : ISubmodulesHost, IDisposable
    {
        private readonly CancellationTokenSequence _loadSequence = new();

        private IWin32Window Owner => new NativeWindowOwner(window);

        private IGitModule Module => commands.Module;

        public void LoadSubmodules(Action<SubmoduleItem> report, Action completed)
        {
            // As FormSubmodules.Initialize: each submodule is shown as soon as its status is known.
            CancellationToken cancellationToken = _loadSequence.Next();
            ThreadHelper.FileAndForget(async () =>
            {
                await TaskScheduler.Default;
                try
                {
                    foreach (IGitSubmoduleInfo? submodule in Module.GetSubmodulesInfo())
                    {
                        if (submodule is null)
                        {
                            continue;
                        }

                        SubmoduleItem item = new(
                            submodule.Name,
                            submodule.Status,
                            submodule.RemotePath,
                            submodule.LocalPath,
                            submodule.CurrentCommitId.ToString(),
                            submodule.Branch);

                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                        report(item);
                        await TaskScheduler.Default;
                    }
                }
                finally
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        completed();
                    }
                }
            });
        }

        public void Add() => AvaloniaUi.RunInHostContext(() =>
        {
            TryShowAddSubmodule(Owner, commands);
        });

        public void Synchronize(string localPath) => AvaloniaUi.RunInHostContext(()
            => ProcessDialogs.ShowProcess(Owner, commands, arguments: Commands.SubmoduleSync(localPath), Module.WorkingDir, input: null, useDialogSettings: true));

        public void Update(string localPath) => AvaloniaUi.RunInHostContext(()
            => ProcessDialogs.ShowProcess(Owner, commands, arguments: Commands.SubmoduleUpdate(localPath), Module.WorkingDir, input: null, useDialogSettings: true));

        public void Remove(string name, string localPath) => AvaloniaUi.RunInHostContext(() =>
        {
            // As FormSubmodules.RemoveSubmoduleClick.
            Module.UnstageFile(localPath);

            ISubmodulesConfigFile submoduleConfigFile;
            try
            {
                submoduleConfigFile = Module.GetSubmodulesConfigFile();
            }
            catch (GitConfigurationException ex)
            {
                MessageBoxes.ShowGitConfigurationExceptionMessage(Owner, ex);
                return;
            }

            submoduleConfigFile.RemoveConfigSection($"submodule \"{name}\"");
            if (submoduleConfigFile.ConfigSections.Count > 0)
            {
                submoduleConfigFile.Save();
                Module.StageFile(".gitmodules");
            }
            else
            {
                Module.UnstageFile(".gitmodules");
            }

            Module.RemoveConfigSection("submodule", name);
        });

        public void Pull(string localPath) => AvaloniaUi.RunInHostContext(()
            => commands.WithGitModule(Module.GetSubmodule(localPath)).StartPullDialog(Owner));

        public void Dispose() => _loadSequence.Dispose();
    }
}
