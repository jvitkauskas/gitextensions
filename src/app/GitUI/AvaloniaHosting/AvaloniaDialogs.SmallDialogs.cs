using System.ComponentModel;
using GitCommands;
using GitCommands.Remotes;
using GitCommands.UserRepositoryHistory;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Editor;
using GitUI.Avalonia.Hosting;
using GitUI.Avalonia.ScriptsEngine;
using GitUI.CommandsDialogs;
using GitUI.CommandsDialogs.AboutBoxDialog;
using GitUI.CommandsDialogs.BrowseDialog;
using GitUI.Editor;
using GitUI.HelperDialogs;
using GitUI.Presentation;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.ScriptsEngine;
using GitUI.Presentation.Translations;
using GitUI.Properties;
using GitUI.ScriptsEngine;
using ResourceManager;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  Routing of the first batch of small dialogs (docs/avalonia-port/PLAN.md, phase 2).
/// </summary>
internal static partial class AvaloniaDialogs
{
    public static bool TryShowCommandlineHelp()
    {
        if (!AvaloniaUi.IsEnabledFor(nameof(FormCommandlineHelp)))
        {
            return false;
        }

        // The command list is a (non-translated) resource of the WinForms form.
        string commands = new ComponentResourceManager(typeof(FormCommandlineHelp)).GetString("_NO_TRANSLATE_commands.Text") ?? "";
        ShowDialog(() => new CommandlineHelpWindow { DataContext = new CommandlineHelpViewModel(ViewStrings.Load<CommandlineHelpStrings>(), commands) }, owner: null);
        return true;
    }

    public static bool TryShowAddFiles(IWin32Window? owner, IGitUICommands commands, string? filter)
    {
        if (!AvaloniaUi.IsEnabledFor(nameof(FormAddFiles)))
        {
            return false;
        }

        ShowDialog(
            () =>
            {
                AddFilesWindow window = new();
                window.DataContext = new AddFilesViewModel(
                    ViewStrings.Load<AddFilesStrings>(),
                    filter,
                    arguments => AvaloniaUi.RunInHostContext(() => FormProcess.ShowDialog(
                        new NativeWindowOwner(window), commands, arguments, commands.Module.WorkingDir, input: null, useDialogSettings: false)));
                return window;
            },
            owner,
            positionName: nameof(FormAddFiles));
        return true;
    }

    public static bool TryShowDonate(IWin32Window? owner)
    {
        if (!AvaloniaUi.IsEnabledFor(nameof(FormDonate)))
        {
            return false;
        }

        ShowDialog(
            () => new DonateWindow
            {
                DataContext = new DonateViewModel(ViewStrings.Load<DonateStrings>(), FormDonate.DonationUrl, url => AvaloniaUi.RunInHostContext(() => OsShellUtil.OpenUrlInDefaultBrowser(url))),
            },
            owner);
        return true;
    }

    public static bool TryShowContributors(IWin32Window? owner)
    {
        if (!AvaloniaUi.IsEnabledFor(nameof(FormContributors)))
        {
            return false;
        }

        ShowDialog(
            () => new ContributorsWindow { DataContext = new ContributorsViewModel(Resources.Team, Resources.Coders, Resources.Translators, Resources.Designers) },
            owner);
        return true;
    }

    public static bool TryShowResetChanges(IWin32Window? owner, bool hasExistingFiles, bool hasNewFiles, string? confirmationMessage, out FormResetChanges.ActionEnum action)
    {
        action = FormResetChanges.ActionEnum.Cancel;
        if (!AvaloniaUi.IsEnabledFor(nameof(FormResetChanges)))
        {
            return false;
        }

        ResetChangesViewModel viewModel = new(ViewStrings.Load<ResetChangesStrings>(), hasExistingFiles, hasNewFiles, confirmationMessage);
        ShowDialog(() => new ResetChangesWindow { DataContext = viewModel }, owner);
        action = viewModel.SelectedAction switch
        {
            ResetChangesAction.Reset => FormResetChanges.ActionEnum.Reset,
            ResetChangesAction.ResetAndDelete => FormResetChanges.ActionEnum.ResetAndDelete,
            _ => FormResetChanges.ActionEnum.Cancel,
        };
        return true;
    }

    public static bool TryShowDeleteTag(IWin32Window? owner, IGitUICommands commands, string? tag, out bool deleted)
    {
        deleted = false;
        if (!AvaloniaUi.IsEnabledFor(nameof(FormDeleteTag)))
        {
            return false;
        }

        IGitModule module = commands.Module;
        IReadOnlyList<string> tags = [.. module.GetRefs(RefsFilter.Tags).Select(r => r.LocalName)];
        IReadOnlyList<string> remotes = [.. new ConfigFileRemoteSettingsManager(() => module).LoadRemotes(false).Select(r => r.Name!)];

        deleted = ShowDialog(
            () =>
            {
                DeleteTagWindow window = new();
                window.DataContext = new DeleteTagViewModel(
                    ViewStrings.Load<DeleteTagStrings>(),
                    tags,
                    tag,
                    remotes,
                    module.GetCurrentRemote(),
                    () => UserManual.UserManual.UrlFor("tag", "delete-tag"),
                    new DeleteTagHost(window, commands));
                return window;
            },
            owner,
            positionName: nameof(FormDeleteTag));
        return true;
    }

    public static bool TryShowInit(IWin32Window? owner, IGitUICommands commands, string dir, EventHandler<GitModuleEventArgs>? gitModuleChanged)
    {
        if (!AvaloniaUi.IsEnabledFor(nameof(FormInit)))
        {
            return false;
        }

        IList<Repository> history = ThreadHelper.JoinableTaskFactory.Run(RepositoryHistoryManager.Locals.LoadRecentHistoryAsync);

        ShowDialog(
            () =>
            {
                InitWindow window = new();
                window.DataContext = new InitViewModel(
                    ViewStrings.Load<InitStrings>(),
                    [.. history.Select(r => r.Path)],
                    string.IsNullOrEmpty(dir) ? AppSettings.DefaultCloneDestinationPath : dir,
                    TranslatedStrings.Error,
                    new InitRepositoryHost(commands, gitModuleChanged, owner),
                    new MessageBoxService(window),
                    new AvaloniaFileDialogService(window));
                return window;
            },
            owner,
            positionName: nameof(FormInit));
        return true;
    }

    public static bool TryShowGoToLine(IWin32Window? owner, int maxLineNumber, out int? lineNumber)
    {
        lineNumber = null;
        if (!AvaloniaUi.IsEnabledFor(nameof(FormGoToLine)))
        {
            return false;
        }

        GoToLineViewModel viewModel = new(ViewStrings.Load<GoToLineStrings>(), maxLineNumber);
        if (ShowDialog(() => new GoToLineWindow { DataContext = viewModel }, owner))
        {
            lineNumber = viewModel.LineNumber;
        }

        return true;
    }

    /// <summary>The Avalonia port of <c>SimplePrompt</c>, or <see langword="null"/> to use the WinForms one.</summary>
    public static IUserInputPrompt? CreateSimplePrompt(string? title, string? label, string? defaultValue)
        => AvaloniaUi.IsEnabledFor(nameof(SimplePrompt))
            ? new UserInputPrompt(
                () => new SimplePromptViewModel(title, label, defaultValue),
                () => new SimplePromptWindow(),
                viewModel => ((SimplePromptViewModel)viewModel).UserInput)
            : null;

    /// <summary>The Avalonia port of <c>FormFilePrompt</c>, or <see langword="null"/> to use the WinForms one.</summary>
    public static IUserInputPrompt? CreateFilePrompt()
        => AvaloniaUi.IsEnabledFor(nameof(FormFilePrompt))
            ? new UserInputPrompt(
                window => new FilePromptViewModel(ViewStrings.Load<FilePromptStrings>(), new AvaloniaFileDialogService(window)),
                () => new FilePromptWindow(),
                viewModel => ((FilePromptViewModel)viewModel).UserInput)
            : null;

    /// <summary>A script input prompt (<see cref="IUserInputPrompt"/>) shown as an Avalonia dialog.</summary>
    private sealed class UserInputPrompt : IUserInputPrompt
    {
        private readonly Func<DialogWindow, DialogViewModel> _createViewModel;
        private readonly Func<DialogWindow> _createWindow;
        private readonly Func<DialogViewModel, string> _getInput;

        public UserInputPrompt(Func<DialogViewModel> createViewModel, Func<DialogWindow> createWindow, Func<DialogViewModel, string> getInput)
            : this(_ => createViewModel(), createWindow, getInput)
        {
        }

        public UserInputPrompt(Func<DialogWindow, DialogViewModel> createViewModel, Func<DialogWindow> createWindow, Func<DialogViewModel, string> getInput)
        {
            _createViewModel = createViewModel;
            _createWindow = createWindow;
            _getInput = getInput;
        }

        public string UserInput { get; private set; } = "";

        public DialogResult ShowDialog(IWin32Window owner)
        {
            DialogViewModel? viewModel = null;
            bool accepted = AvaloniaDialogs.ShowDialog(
                () =>
                {
                    // The view model may need the window (e.g. for file pickers), so the window comes first.
                    DialogWindow window = _createWindow();
                    viewModel = _createViewModel(window);
                    window.DataContext = viewModel;
                    return window;
                },
                owner);

            if (!accepted)
            {
                return DialogResult.Cancel;
            }

            UserInput = _getInput(viewModel!);
            return DialogResult.OK;
        }

        public void Dispose()
        {
        }
    }

    /// <summary>Lets an Avalonia dialog run event scripts, which need a host form.</summary>
    private sealed class ScriptHost(DialogWindow window, IGitUICommands commands) : IGitModuleForm, IScriptOptionsForm, IWin32Window
    {
        public IGitUICommands UICommands => commands;

        public nint Handle => window.NativeHandle;

        public IScriptOptionsProvider GetScriptOptionsProvider() => ScriptOptionsProviderBase.Default;
    }

    private sealed class DeleteTagHost(DialogWindow window, IGitUICommands commands) : IDeleteTagHost
    {
        public void DeleteLocalTag(string tagName) => commands.Module.DeleteTag(tagName);

        public void DeleteRemoteTag(string remote, string tagName) => AvaloniaUi.RunInHostContext(() =>
        {
            // As in FormDeleteTag.RemoveRemoteTag.
            IScriptsRunner scriptsRunner = commands.GetRequiredService<IScriptsRunner>();
            ScriptHost scriptHost = new(window, commands);
            if (!scriptsRunner.RunEventScripts(ScriptEvent.BeforePush, scriptHost))
            {
                return;
            }

            RemoteProcessResult result = RunRemoteProcess(new NativeWindowOwner(window), commands, $"push \"{remote}\" :refs/tags/{tagName}");
            if (!commands.Module.InTheMiddleOfAction() && !result.ErrorOccurred)
            {
                scriptsRunner.RunEventScripts(ScriptEvent.AfterPush, scriptHost);
            }
        });

        public void OpenUrl(string url) => AvaloniaUi.RunInHostContext(() => OsShellUtil.OpenUrlInDefaultBrowser(url));
    }

    private sealed class InitRepositoryHost(IGitUICommands commands, EventHandler<GitModuleEventArgs>? gitModuleChanged, IWin32Window? owner) : IInitRepositoryHost
    {
        public bool FileExists(string path) => File.Exists(path);

        public string Init(string directory, bool central)
        {
            GitModule module = new(commands.GetRequiredService<IGitExecutorProvider>(), directory);
            if (!Directory.Exists(module.WorkingDir))
            {
                Directory.CreateDirectory(module.WorkingDir);
            }

            return module.Init(bare: central, shared: central);
        }

        public void OnRepositoryCreated(string directory) => AvaloniaUi.RunInHostContext(() =>
        {
            GitModule module = new(commands.GetRequiredService<IGitExecutorProvider>(), directory);
            gitModuleChanged?.Invoke(owner, new GitModuleEventArgs(module));
            ThreadHelper.JoinableTaskFactory.Run(() => RepositoryHistoryManager.Locals.AddAsMostRecentAsync(directory));
        });
    }
}
