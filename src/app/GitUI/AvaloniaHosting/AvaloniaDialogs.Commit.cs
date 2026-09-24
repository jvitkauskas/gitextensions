using System.Text.RegularExpressions;
using GitCommands;
using GitCommands.Config;
using GitCommands.Git;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.CommandsDialogs.CommitDialog;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.CommandsDialogs.CommitDialog;
using GitUI.Hotkey;
using GitUI.Presentation.CommandsDialogs.CommitDialog;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUI.ScriptsEngine;
using GitUIPluginInterfaces;
using Microsoft.VisualStudio.Threading;
using ResourceManager;

namespace GitUI.AvaloniaHosting;

/// <summary>Routing of the commit dialog (docs/avalonia-port/PLAN.md, phase 5).</summary>
internal static partial class AvaloniaDialogs
{
    /// <summary>
    ///  Shows the Avalonia port of <c>FormCommit</c>; with <paramref name="showOnlyWhenChanges"/>, only if there are changes
    ///  (as <c>ShowDialogWhenChanges</c>).
    /// </summary>
    public static bool TryShowCommit(IWin32Window? owner, IGitUICommands commands, CommitKind kind = CommitKind.Normal, GitRevision? editedCommit = null, string? commitMessage = null, bool showOnlyWhenChanges = false)
    {
        if (showOnlyWhenChanges && commands.Module.GetAllChangedFilesWithSubmodulesStatus(cancellationToken: default).Count == 0)
        {
            return true;
        }

        ShowDialog(
            () =>
            {
                CommitWindow window = new();
                CommitHost host = new(commands, window, commitMessage);
                CommitViewModel viewModel = new(
                    ViewStrings.Load<CommitStrings>(),
                    host,
                    new FileViewerHost(commands) { Window = window },
                    ViewStrings.Load<FileStatusListStrings>(),
                    GetFileStatusTreeOptions(),
                    (CommitDialogKind)kind,
                    editedCommit,
                    new SpellCheckHost(commands));
                UseFileStatusListMenu(viewModel.Unstaged, commands, window);
                UseFileStatusListMenu(viewModel.Staged, commands, window);
                EventHandler<GitUIEventArgs> onRepositoryChanged = (_, _) => ThreadHelper.FileAndForget(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    viewModel.OnRepositoryChanged();
                });
                commands.PostRepositoryChanged += onRepositoryChanged;
                window.Closed += (_, _) => commands.PostRepositoryChanged -= onRepositoryChanged;
                window.Hotkeys = LoadHotkeys(commands, HotkeyCommands.CommitSettingsName);
                window.DataContext = viewModel;
                return window;
            },
            owner,
            positionName: "FormCommit");
        return true;
    }

    private sealed class CommitDialogSettings : ICommitDialogSettings
    {
        public bool CloseDialogAfterEachCommit { get => AppSettings.CloseCommitDialogAfterCommit; set => AppSettings.CloseCommitDialogAfterCommit = value; }

        public bool CloseDialogAfterAllFilesCommitted { get => AppSettings.CloseCommitDialogAfterLastCommit; set => AppSettings.CloseCommitDialogAfterLastCommit = value; }

        public bool RefreshDialogOnFormFocus { get => AppSettings.RefreshArtificialCommitOnApplicationActivated; set => AppSettings.RefreshArtificialCommitOnApplicationActivated = value; }

        public bool SelectStagedOnEnterMessage { get => AppSettings.CommitDialogSelectStagedOnEnterMessage.Value; set => AppSettings.CommitDialogSelectStagedOnEnterMessage.Value = value; }

        public bool ShowOnlyMyMessages { get => AppSettings.CommitDialogShowOnlyMyMessages; set => AppSettings.CommitDialogShowOnlyMyMessages = value; }

        public bool StageInSuperproject { get => AppSettings.StageInSuperprojectAfterCommit; set => AppSettings.StageInSuperprojectAfterCommit = value; }
    }

    /// <summary>The git operations, dialogs and scripts of <c>FormCommit</c>.</summary>
    internal sealed class CommitHost : ICommitHost, IGitModuleForm, IScriptOptionsForm, IWin32Window
    {
        private readonly IGitUICommands _commands;
        private readonly DialogWindow _window;
        private readonly CommitStrings _strings = ViewStrings.Load<CommitStrings>();
        private readonly CommitMessageManager _commitMessageManager;
        private readonly CommitTemplateManager _commitTemplateManager;

        public CommitHost(IGitUICommands commands, DialogWindow window, string? commitMessage)
        {
            _commands = commands;
            _window = window;

            // The manager shows its errors on the WinForms owner; the message box is on the dialog.
            _commitMessageManager = new CommitMessageManager(owner: null, Module.WorkingDirGitDir, Module.CommitEncoding, commitMessage);
            _commitTemplateManager = new CommitTemplateManager(() => Module);
        }

        private IGitModule Module => _commands.Module;

        private NativeWindowOwner Owner => new(_window);

        public IGitUICommands UICommands => _commands;

        public nint Handle => Owner.Handle;

        public CommitDialogOptions Options { get; } = new()
        {
            UseFormCommitMessage = AppSettings.UseFormCommitMessage,
            MaxFirstLineLength = AppSettings.CommitValidationMaxCntCharsFirstLine,
            MaxLineLength = AppSettings.CommitValidationMaxCntCharsPerLine,
            SecondLineMustBeEmpty = AppSettings.CommitValidationSecondLineMustBeEmpty,
            AutoWrap = AppSettings.CommitValidationAutoWrap,
            IndentAfterFirstLine = AppSettings.CommitValidationIndentAfterFirstLine,
            ValidationRegex = AppSettings.CommitValidationRegEx,
            DontConfirmCommitIfNoBranch = AppSettings.DontConfirmCommitIfNoBranch,
            CommitAndPushForcedWhenAmend = AppSettings.CommitAndPushForcedWhenAmend,
            ShowCommitAndPush = AppSettings.ShowCommitAndPush,
            ShowResetAllChanges = AppSettings.ShowResetAllChanges,
            ShowResetWorkTreeChanges = AppSettings.ShowResetWorkTreeChanges,
            DontConfirmAmend = AppSettings.DontConfirmAmend.Value,
            NumberOfPreviousMessages = AppSettings.CommitDialogNumberOfPreviousMessages,
            ShowSelectionFilter = AppSettings.CommitDialogSelectionFilter,
        };

        public ICommitDialogSettings Settings { get; } = new CommitDialogSettings();

        public string WorkingDirectory => PathUtil.GetDisplayPath(Module.WorkingDir);

        public string PushText => TranslatedStrings.ButtonPush;

        public bool IsBareRepository => Module.IsBareRepository();

        public bool HasSuperproject => Module.SuperprojectModule is not null;

        public bool IsMergeCommit => !Module.RevParse("MERGE_HEAD").IsZero;

        public bool InTheMiddleOfConflictedMerge() => Module.InTheMiddleOfConflictedMerge();

        public bool CanResetSoft() => !Module.RevParse("HEAD~1").IsZero;

        /// <summary>As <c>ComputeUnstagedFiles</c> with the settings of the unstaged list.</summary>
        public async Task<IReadOnlyList<GitItemStatus>> GetAllChangedFilesAsync(FileStatusFileOptions options, CancellationToken cancellationToken)
        {
            await TaskScheduler.Default;
            IReadOnlyList<GitItemStatus> files = Module.GetAllChangedFilesWithSubmodulesStatus(
                excludeIgnoredFiles: !options.ShowIgnoredFiles,
                excludeAssumeUnchangedFiles: !options.ShowAssumeUnchangedFiles,
                excludeSkipWorktreeFiles: !options.ShowSkipWorktreeFiles,
                options.ShowUntrackedFiles ? UntrackedFilesMode.Default : UntrackedFilesMode.No,
                cancellationToken);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            return files;
        }

        public IReadOnlyList<GitItemStatus> GetIndexFiles() => Module.GetIndexFilesWithSubmodulesStatus();

        /// <summary>As <c>GetHeadRevisions</c>.</summary>
        public (GitRevision? Head, GitRevision Index, GitRevision WorkTree) GetHeadRevisions()
        {
            ObjectId headId = Module.RevParse("HEAD");
            GitRevision? head = headId.IsZero ? null : new GitRevision(headId);
            GitRevision index = headId.IsZero ? new GitRevision(ObjectId.IndexId) : new GitRevision(ObjectId.IndexId) { ParentIds = [headId] };
            GitRevision workTree = new(ObjectId.WorkTreeId) { ParentIds = [ObjectId.IndexId] };
            return (head, index, workTree);
        }

        public void UpdateSubmoduleStatus(IReadOnlyList<GitItemStatus> items) => Module.GetSubmoduleCurrentStatus(items);

        /// <summary>As <c>UpdateBranchNameDisplayAsync</c>.</summary>
        public async Task<CommitBranchInfo> GetBranchInfoAsync()
        {
            await TaskScheduler.Default;
            string currentBranchName = Module.GetSelectedBranch();
            IGitRef? currentBranch = Module.GetRefs(RefsFilter.Heads).FirstOrDefault(r => r.LocalName == currentBranchName);
            CommitBranchInfo info;
            if (currentBranch is null)
            {
                info = new CommitBranchInfo(currentBranchName, PushTo: null);
            }
            else if (string.IsNullOrEmpty(currentBranch.TrackingRemote))
            {
                string? defaultRemote = Module.GetRemoteNames().FirstOrDefault(r => r == "origin") ?? Module.GetRemoteNames().OrderBy(r => r).FirstOrDefault();
                info = new CommitBranchInfo(currentBranchName, defaultRemote is not null
                    ? $"{defaultRemote}/{currentBranchName} {_strings.UntrackedRemote.Text}"
                    : _strings.StatusBarBranchWithoutRemote.Text);
            }
            else
            {
                info = new CommitBranchInfo(currentBranchName, $"{currentBranch.TrackingRemote}/{currentBranch.MergeWith}");
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            return info;
        }

        /// <summary>As <c>UpdateAuthorInfo</c>.</summary>
        public async Task<string> GetCommitterAsync(string author)
        {
            await TaskScheduler.Default;
            string committer = $"{_strings.CommitCommitterInfo.Text} {GetSetting(SettingKeyString.UserName)} <{GetSetting(SettingKeyString.UserEmail)}>";
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            return string.IsNullOrWhiteSpace(author) ? committer : $"{committer} {_strings.CommitAuthorInfo.Text} {author}";

            // Not cached, to update the info when the dialog is activated.
            string GetSetting(string key) => Module.GetEffectiveSetting(key, defaultValue: $"/{string.Format(TranslatedStrings.NotConfigured, key)}/");
        }

        /// <summary>As <c>Stage</c>: the errors are shown if set so.</summary>
        public bool StageFiles(IReadOnlyList<GitItemStatus> files)
        {
            bool wereErrors = !Module.StageFiles(files, out string output);
            if (wereErrors && AppSettings.ShowErrorsWhenStagingFiles)
            {
                AvaloniaUi.RunInHostContext(() => ProcessDialogs.ShowErrorDialog(Owner, _commands, _strings.StageDetails.Text, string.Format(_strings.StageFiles.Text + "\n", files.Count), output));
            }

            return !wereErrors;
        }

        public bool UnstageFiles(IReadOnlyList<GitItemStatus> files) => Module.BatchUnstageFiles(files);

        public void UnstageAll() => Module.Reset(ResetMode.Mixed);

        public async Task<(string Message, bool Amend)> LoadCommitMessageAsync()
        {
            string message = await _commitMessageManager.GetMergeOrCommitMessageAsync();
            bool amend = await _commitMessageManager.GetAmendStateAsync();
            return (message, !_commitMessageManager.IsMergeCommit && amend);
        }

        /// <summary>As <c>AssignCommitMessageFromTemplate</c>.</summary>
        public string? LoadCommitTemplate()
        {
            try
            {
                return _commitTemplateManager.LoadGitCommitTemplate();
            }
            catch (FileNotFoundException ex)
            {
                ShowMessage(string.Format(_strings.TemplateNotFound.Text, ex.FileName), _strings.TemplateNotFoundCaption.Text, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, _strings.TemplateLoadErrorCaption.Text, MessageBoxIcon.Error);
            }

            return null;
        }

        public async Task SaveCommitMessageAsync(string message, bool amend)
        {
            await _commitMessageManager.SetMergeOrCommitMessageAsync(message);
            await _commitMessageManager.SetAmendStateAsync(amend);
        }

        /// <summary>As <c>CommitMessageToolStripMenuItemDropDownOpening</c>: the last message first, then those of HEAD.</summary>
        public IReadOnlyList<string> GetPreviousMessages(bool onlyMine)
        {
            string msg = AppSettings.LastCommitMessage;
            int maxCount = AppSettings.CommitDialogNumberOfPreviousMessages;
            string authorPattern = string.Empty;
            if (onlyMine)
            {
                string userName = Module.GetEffectiveSetting(SettingKeyString.UserName);
                string userEmail = Module.GetEffectiveSetting(SettingKeyString.UserEmail);
                authorPattern = $"^{Regex.Escape(userName)} <{Regex.Escape(userEmail)}>$";
            }

            List<string> prevMessages = [.. Module.GetPreviousCommitMessages(maxCount, "HEAD", authorPattern)
                .WhereNotNull()
                .Select(message => message.TrimEnd('\n'))
                .Where(message => !string.IsNullOrWhiteSpace(message))];
            if (!string.IsNullOrWhiteSpace(msg) && !prevMessages.Contains(msg))
            {
                if (prevMessages.Count == maxCount)
                {
                    prevMessages.RemoveAt(maxCount - 1);
                }

                prevMessages.Insert(0, msg);
            }

            return prevMessages;
        }

        public string? GetHeadMessage() => Module.GetPreviousCommitMessages(count: 1, revision: "HEAD", authorPattern: string.Empty).FirstOrDefault();

        public (IReadOnlyList<CommitTemplateItem> Registered, IReadOnlyList<CommitTemplateItem> FromSettings) GetCommitTemplates()
            => ([.. _commitTemplateManager.RegisteredTemplates], CommitTemplateItem.LoadFromSettings() ?? []);

        public string GetCurrentBranch() => Module.GetSelectedBranch();

        public void EditCommitTemplateSettings() => AvaloniaUi.RunInHostContext(() =>
        {
            TryShowCommitTemplateSettings(Owner);
        });

        public void OpenUrl(string url) => OsShellUtil.OpenUrlInDefaultBrowser(url);

        public bool ConfirmAmend() => AvaloniaUi.RunInHostContext(()
            => MessageBoxes.ConfirmSuppressible(Owner, _strings.AmendCommit.Text, _strings.AmendCommitCaption.Text, AppSettings.DontConfirmAmend, icon: TaskDialogIcon.Warning));

        public bool ConfirmEmptyMergeCommit() => AvaloniaUi.RunInHostContext(()
            => MessageBoxes.Show(Owner, _strings.NoFilesStagedAndConfirmAnEmptyMergeCommit.Text, _strings.NoStagedChanges.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes);

        /// <summary>As <c>ConfirmAndStageAllUnstaged</c>.</summary>
        public NoStagedFilesChoice AskWithoutStagedFiles(bool filterActive, bool hasUnstagedFiles) => AvaloniaUi.RunInHostContext(() =>
        {
            TaskDialogPage page = new()
            {
                AllowCancel = true,
                Caption = _strings.NoFilesStagedCommitCaption.Text,
                Icon = TaskDialogIcon.Error,
                Heading = _strings.NoFilesStagedCommitInstructions.Text,
                Buttons = { TaskDialogButton.Cancel },
                SizeToContent = true
            };
            string text = filterActive ? _strings.NoFilesStagedCommitAllFilteredUnstagedOption.Text : _strings.NoFilesStagedCommitAllUnstagedOption.Text;
            TaskDialogCommandLinkButton stageAndCommit = new(text, enabled: hasUnstagedFiles);
            TaskDialogCommandLinkButton emptyCommit = new(_strings.NoFilesStagedMakeEmptyCommitOption.Text);
            page.Buttons.Add(stageAndCommit);
            page.Buttons.Add(emptyCommit);
            TaskDialogButton result = TaskDialog.ShowDialog(Owner.Handle, page);
            return result == stageAndCommit ? NoStagedFilesChoice.StageAllAndCommit
                : result == emptyCommit ? NoStagedFilesChoice.EmptyCommit
                : NoStagedFilesChoice.Cancel;
        });

        public void ShowMergeConflicts() => ShowMessage(_strings.MergeConflicts.Text, _strings.MergeConflictsCaption.Text, MessageBoxIcon.Error);

        public void ShowEnterCommitMessage() => ShowMessage(_strings.EnterCommitMessage.Text, _strings.EnterCommitMessageCaption.Text, MessageBoxIcon.Asterisk);

        public bool ConfirmValidation(string text) => AvaloniaUi.RunInHostContext(()
            => MessageBoxes.Show(Owner, text, _strings.CommitValidationCaption.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk) != DialogResult.No);

        /// <summary>As the question of <c>DoCommit</c> on a detached HEAD.</summary>
        public bool ConfirmDetachedHeadCommit(ObjectId? editedCommit) => AvaloniaUi.RunInHostContext(() =>
        {
            if (!Module.IsDetachedHead() || Module.InTheMiddleOfRebase())
            {
                return true;
            }

            TaskDialogPage page = new()
            {
                Text = _strings.NotOnBranch.Text,
                Heading = TranslatedStrings.ErrorInstructionNotOnBranch,
                Caption = TranslatedStrings.ErrorCaptionNotOnBranch,
                Buttons = { TaskDialogButton.Cancel },
                Icon = TaskDialogIcon.Error,
                AllowCancel = true,
                SizeToContent = true
            };
            TaskDialogCommandLinkButton btnCheckout = new(TranslatedStrings.ButtonCheckoutBranch);
            TaskDialogCommandLinkButton btnCreate = new(TranslatedStrings.ButtonCreateBranch);
            TaskDialogCommandLinkButton btnContinue = new(TranslatedStrings.ButtonContinue);
            page.Buttons.Add(btnCheckout);
            page.Buttons.Add(btnCreate);
            page.Buttons.Add(btnContinue);
            TaskDialogButton result = TaskDialog.ShowDialog(Owner.Handle, page);
            if (result == btnCheckout)
            {
                ObjectId[]? objectIds = editedCommit is { } id ? [id] : null;
                return _commands.StartCheckoutBranch(Owner, objectIds);
            }

            if (result == btnCreate)
            {
                return _commands.StartCreateBranchDialog(Owner, editedCommit ?? default);
            }

            return result == btnContinue;
        });

        /// <summary>As the commit of <c>DoCommit</c>: the message file, the scripts before and after, and <c>git commit</c>.</summary>
        public bool Commit(CommitRequest request) => AvaloniaUi.RunInHostContext(() =>
        {
            if (Options.UseFormCommitMessage)
            {
                // Save last commit message in settings. This way it can be used in multiple repositories.
                AppSettings.LastCommitMessage = request.Message;
                ThreadHelper.JoinableTaskFactory.Run(
                    () => _commitMessageManager.WriteCommitMessageToFileAsync(request.Message, CommitMessageType.Normal,
                                                                              usingCommitTemplate: request.UsingCommitTemplate,
                                                                              ensureCommitMessageSecondLineEmpty: AppSettings.EnsureCommitMessageSecondLineEmpty));
            }

            IScriptsRunner scriptsRunner = _commands.GetRequiredService<IScriptsRunner>();
            if (!scriptsRunner.RunEventScripts(ScriptEvent.BeforeCommit, this))
            {
                return false;
            }

            ArgumentString commitCmd = Commands.Commit(
                request.Amend,
                request.SignOff,
                request.Author,
                Options.UseFormCommitMessage,
                _commitMessageManager.CommitMessagePath,
                Module.GetPathForGitExecution,
                request.NoVerify,
                request.GpgSign,
                request.GpgKeyId,
                request.AllowEmpty,
                request.ResetAuthor);
            bool success = ProcessDialogs.ShowProcess(Owner, _commands, arguments: commitCmd, Module.WorkingDir, input: null, useDialogSettings: true);
            _commands.RepoChangedNotifier.Notify();
            if (!success)
            {
                return false;
            }

            scriptsRunner.RunEventScripts(ScriptEvent.AfterCommit, this);

            // The message has been used and stored.
            ThreadHelper.JoinableTaskFactory.Run(_commitMessageManager.ResetCommitMessageAsync);
            return true;
        });

        public bool Push(bool forced) => AvaloniaUi.RunInHostContext(() =>
        {
            _commands.StartPushDialog(owner: Owner, pushOnShow: true, forceWithLease: forced, out bool pushCompleted);
            return pushCompleted;
        });

        public void StageInSuperprojectNow()
        {
            if (Module.SuperprojectModule is { } superproject && !string.IsNullOrWhiteSpace(Module.SubmodulePath))
            {
                superproject.StageFile(Module.SubmodulePath);
            }
        }

        public bool ConfirmResetSoft() => AvaloniaUi.RunInHostContext(()
            => MessageBoxes.ConfirmSuppressible(Owner, _strings.AmendResetSoft.Text, _strings.AmendCommitCaption.Text, AppSettings.DontConfirmAmend, icon: TaskDialogIcon.Warning));

        public void ResetSoft() => Module.GitExecutable.RunCommand(Commands.Reset(ResetMode.Soft, "HEAD~1"));

        public bool ResolveConflicts() => AvaloniaUi.RunInHostContext(() => _commands.StartResolveConflictsDialog(Owner, false));

        public void ResetChanges(IReadOnlyList<GitItemStatus> unstagedFiles, bool onlyWorkTree)
            => AvaloniaUi.RunInHostContext(() => _commands.StartResetChangesDialog(Owner, [.. unstagedFiles], onlyWorkTree));

        public void StashStaged() => AvaloniaUi.RunInHostContext(() => _commands.StashStaged(owner: Owner));

        public bool CreateBranch() => AvaloniaUi.RunInHostContext(() => _commands.StartCreateBranchDialog(Owner));

        public void EditCommitterSettings()
            => AvaloniaUi.RunInHostContext(() => _commands.StartSettingsDialog(Owner, new CommandsDialogs.SettingsDialog.SettingsPageReferenceByName("GitConfigSettingsPage")));

        public void NotifyRepositoryChanged()
        {
            if (AppSettings.RevisionGraphShowArtificialCommits)
            {
                _commands.RepoChangedNotifier.Notify();
            }
        }

        public void ShowError(string message) => ShowMessage(message, TranslatedStrings.Error, MessageBoxIcon.Error);

        /// <summary>The selected files for the scripts (as <c>GetScriptOptionsProvider</c>).</summary>
        public IScriptOptionsProvider GetScriptOptionsProvider()
        {
            CommitViewModel? viewModel = _window.DataContext as CommitViewModel;
            return new ScriptOptionsProvider(
                () => viewModel is null ? [] : viewModel.Unstaged.SelectedEntries.Concat(viewModel.Staged.SelectedEntries).Select(e => e.Item.Name),
                () => null,
                () => null);
        }

        private void ShowMessage(string text, string caption, MessageBoxIcon icon)
            => AvaloniaUi.RunInHostContext(() => MessageBoxes.Show(Owner, text, caption, MessageBoxButtons.OK, icon));
    }
}
