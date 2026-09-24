using GitCommands;
using GitCommands.Submodules;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Translations;

namespace GitUI.AvaloniaHosting;

/// <summary>The submodules button of the Avalonia main window (<c>toolStripButtonLevelUp</c>).</summary>
internal static partial class AvaloniaDialogs
{
    private sealed partial class BrowseHost
    {
        private EventHandler<IReadOnlyList<BrowseMenuItem>?>? _submoduleMenuChanged;
        private ISubmoduleStatusProvider? _submoduleStatusProvider;
        private IReadOnlyList<(BrowseMenuItem Item, string Path, string Format)>? _submoduleItems;

        // The left panel reads the structure of the submodules (UpdateSubmodulesStructure); the button shows the result.
        public event EventHandler<IReadOnlyList<BrowseMenuItem>?>? SubmoduleMenuChanged
        {
            add
            {
                if (_submoduleStatusProvider is null)
                {
                    _submoduleStatusProvider = _commands.GetRequiredService<ISubmoduleStatusProvider>();
                    _submoduleStatusProvider.StatusUpdating += OnSubmoduleStatusUpdating;
                    _submoduleStatusProvider.StatusUpdated += OnSubmoduleButtonStatusUpdated;
                }

                _submoduleMenuChanged += value;
            }

            remove => _submoduleMenuChanged -= value;
        }

        public bool HasSuperproject => Module.SuperprojectModule is not null;

        public bool CanShowSubmodules => Module.IsValidGitWorkingDir() && !Module.IsBareRepository();

        public void GoToSuperproject()
        {
            if (Module.SuperprojectModule is { } superproject)
            {
                _session.SetGitModule(superproject);
            }
        }

        private void StopSubmoduleMenu()
        {
            if (_submoduleStatusProvider is not null)
            {
                _submoduleStatusProvider.StatusUpdating -= OnSubmoduleStatusUpdating;
                _submoduleStatusProvider.StatusUpdated -= OnSubmoduleButtonStatusUpdated;
                _submoduleStatusProvider = null;
            }

            _submoduleMenuChanged = null;
        }

        // As SubmoduleStatusProvider_StatusUpdating: "Loading..." until updated.
        private void OnSubmoduleStatusUpdating(object? sender, EventArgs e)
            => ThreadHelper.FileAndForget(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                _submoduleMenuChanged?.Invoke(this, null);
            });

        // As SubmoduleStatusProvider_StatusUpdated: the structure when it changed, and the status of the items.
        private void OnSubmoduleButtonStatusUpdated(object? sender, SubmoduleStatusEventArgs e)
            => ThreadHelper.FileAndForget(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(e.Token);
                if (_submoduleMenuChanged is null)
                {
                    return;
                }

                if (e.StructureUpdated || _submoduleItems is null)
                {
                    _submoduleItems = CreateSubmoduleItems(e.Info);
                }

                _submoduleMenuChanged.Invoke(this, [.. _submoduleItems.Select(entry => WithStatus(entry, e.Info))]);
            });

        // As PopulateToolbar.
        private List<(BrowseMenuItem Item, string Path, string Format)> CreateSubmoduleItems(SubmoduleInfoResult result)
        {
            BrowseToolbarStrings strings = ViewStrings.Load<BrowseToolbarStrings>();
            List<(BrowseMenuItem Item, string Path, string Format)> items = [.. result.OurSubmodules.Select(submodule => SubmoduleItem(submodule, "{0}"))];
            if (result.OurSubmodules.Count == 0)
            {
                items.Add((new BrowseMenuItem(strings.NoSubmodulesPresent.Text, null) { IsEnabled = false }, "", ""));
            }

            if (result.SuperProject is not null)
            {
                items.Add((BrowseMenuItem.Separator, "", ""));

                // Show top project only if it's not our super project
                if (result.TopProject is not null && result.TopProject != result.SuperProject)
                {
                    items.Add(SubmoduleItem(result.TopProject, strings.TopProjectModuleFormat.Text));
                }

                items.Add(SubmoduleItem(result.SuperProject, strings.SuperprojectModuleFormat.Text));
                items.AddRange(result.AllSubmodules.Select(submodule => SubmoduleItem(submodule, "{0}")));
            }

            items.Add((BrowseMenuItem.Separator, "", ""));
            items.Add((new BrowseMenuItem(ViewStrings.Load<BrowseStrings>().UpdateAllSubmodules.AccessKeyText, BrowseCommand.UpdateAllSubmodules, "SubmodulesUpdate"), "", ""));
            if (result.CurrentSubmoduleName is not null)
            {
                string workingDir = Module.WorkingDir;
                items.Add((new BrowseMenuItem(strings.UpdateCurrentSubmodule.Text, null, "FolderSubmodule") { Invoke = () => UpdateCurrentSubmodule(workingDir) }, "", ""));
            }

            return items;

            (BrowseMenuItem Item, string Path, string Format) SubmoduleItem(SubmoduleInfo info, string format)
                => (new BrowseMenuItem(string.Format(format, info.Text).Replace("_", "__"), null, "FolderSubmodule") { Invoke = () => OpenSubmodule(info.Path) }, info.Path, format);
        }

        // As UpdateSubmoduleMenuItemStatus: the image and the changes of the submodule.
        private static BrowseMenuItem WithStatus((BrowseMenuItem Item, string Path, string Format) entry, SubmoduleInfoResult result)
        {
            if (string.IsNullOrWhiteSpace(entry.Path))
            {
                return entry.Item;
            }

            SubmoduleInfo? info = result.TopProject?.Path == entry.Path
                ? result.TopProject
                : result.AllSubmodules.FirstOrDefault(submodule => submodule.Path == entry.Path);
            if (info?.Detailed is not { } details)
            {
                return entry.Item;
            }

            string icon = (details.Status, details.IsDirty) switch
            {
                (null, _) => "FolderSubmodule",
                (SubmoduleStatus.FastForward, true) => "SubmoduleRevisionUpDirty",
                (SubmoduleStatus.FastForward, false) => "SubmoduleRevisionUp",
                (SubmoduleStatus.Rewind, true) => "SubmoduleRevisionDownDirty",
                (SubmoduleStatus.Rewind, false) => "SubmoduleRevisionDown",
                (SubmoduleStatus.NewerTime, true) => "SubmoduleRevisionSemiUpDirty",
                (SubmoduleStatus.NewerTime, false) => "SubmoduleRevisionSemiUp",
                (SubmoduleStatus.OlderTime, true) => "SubmoduleRevisionSemiDownDirty",
                (SubmoduleStatus.OlderTime, false) => "SubmoduleRevisionSemiDown",
                (_, true) => "SubmoduleDirty",
                (_, false) => "FileStatusModified",
            };
            return entry.Item with { Header = string.Format(entry.Format, info.Text + details.AddedAndRemovedText).Replace("_", "__"), Icon = icon };
        }

        // As SubmoduleToolStripButtonClick: the submodule in this window.
        private void OpenSubmodule(string path) => AvaloniaUi.RunInHostContext(() =>
        {
            if (!Directory.Exists(path))
            {
                MessageBoxes.SubmoduleDirectoryDoesNotExist(Owner, path);
                return;
            }

            _session.SetWorkingDir(path);
        });

        // As UpdateSubmoduleToolStripMenuItemClick.
        private void UpdateCurrentSubmodule(string submodule) => AvaloniaUi.RunInHostContext(() =>
        {
            if (Module.SuperprojectModule is { } superproject)
            {
                ProcessDialogs.ShowProcess(Owner, _commands, arguments: GitCommands.Git.Commands.SubmoduleUpdate(submodule), superproject.WorkingDir, input: null, useDialogSettings: true);
            }

            RepositoryChanged?.Invoke(this, EventArgs.Empty);
        });
    }
}
