using GitCommands;
using GitCommands.Config;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Settings;
using GitExtUtils;
using GitUI.Avalonia.HelperDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.ConsoleEmulation;
using GitUI.Infrastructure;
using GitUI.Presentation.HelperDialogs;
using GitUI.Presentation.Translations;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  The progress dialog of a remote command, as its exit handler sees it (<c>FormRemoteProcess</c> or its Avalonia port):
///  the owner of the questions asked, the output, and a retry, e.g. with other arguments.
/// </summary>
internal interface IRemoteProcessDialog : IWin32Window
{
    string? Remote { get; }

    /// <summary>The arguments of the process (as <c>FormProcess.ProcessArguments</c>), which a retry uses.</summary>
    string ProcessArguments { get; set; }

    string GetOutputString();

    void Retry();
}

/// <summary>As <c>FormProcess.HandleOnExit</c>: returns whether the exit is handled (e.g. retried).</summary>
internal delegate bool RemoteProcessExitHandler(ref bool isError, IRemoteProcessDialog dialog);

/// <summary>The end of a remote command.</summary>
/// <param name="Aborted">The user aborted it (as <c>DialogResult.Abort</c>).</param>
internal readonly record struct RemoteProcessResult(bool ErrorOccurred, bool Aborted, string Output);

internal static partial class AvaloniaDialogs
{
    /// <summary>
    ///  Runs a remote git command (fetch, pull, push, clone, ls-remote) in the progress dialog, which handles the questions
    ///  of PuTTY (<c>FormRemoteProcess</c>): the Avalonia port, or the WinForms form when it is not enabled.
    /// </summary>
    /// <param name="remote">The remote, for the key file of PuTTY and its host key.</param>
    /// <param name="title">The title of the dialog, instead of the working directory.</param>
    /// <param name="urlTryingToConnect">The URL whose host key PuTTY may ask to cache (as <c>SetUrlTryingToConnect</c>).</param>
    public static RemoteProcessResult RunRemoteProcess(
        IWin32Window? owner,
        IGitUICommands commands,
        ArgumentString arguments,
        string? remote = null,
        string? title = null,
        string? urlTryingToConnect = null,
        RemoteProcessExitHandler? onExit = null)
    {
        string workingDirectory = commands.Module.WorkingDir;
        (string process, string resolvedArguments) = ResolveProcess(process: null, arguments, workingDirectory);
        using ConsoleProcess console = new(
            commands.GetRequiredService<IConsoleEmulatorsRegistry>().CreateCommandController(),
            commands,
            process,
            resolvedArguments,
            workingDirectory);

        RemoteProcessDialog? dialog = null;
        ShowDialog(
            () =>
            {
                ProcessWindow window = new();
                ProcessViewModel viewModel = new(
                    ViewStrings.Load<ProcessStrings>(),
                    PathUtil.GetDisplayPath(workingDirectory),
                    console,
                    new ProcessDialogHost(commands, console),
                    new MessageBoxService(window),
                    useDialogSettings: true,
                    console.PostToUiThread);
                if (title is not null)
                {
                    viewModel.BaseTitle = title;
                }

                dialog = new RemoteProcessDialog(commands, window, viewModel, console, remote, urlTryingToConnect, onExit);
                window.DataContext = viewModel;
                return window;
            },
            owner,
            positionName: "FormRemoteProcess");

        return new RemoteProcessResult(dialog!.ViewModel.ErrorOccurred, dialog.ViewModel.Aborted, dialog.ViewModel.Output);
    }

    /// <summary>The Avalonia port of <see cref="ProcessDialogs.ShowRemoteProcess(IWin32Window?, IGitUICommands, ArgumentString)"/>.</summary>
    public static bool TryShowRemoteProcess(IWin32Window? owner, IGitUICommands commands, ArgumentString arguments, out bool success)
    {
        success = false;
        success = !RunRemoteProcess(owner, commands, arguments).ErrorOccurred;
        return true;
    }

    /// <summary>
    ///  Port of <c>FormRemoteProcess</c> on the Avalonia progress dialog: PuTTY's questions (a key to load, a host key to
    ///  cache, a fingerprint to register) and the exit handler of the caller.
    /// </summary>
    private sealed class RemoteProcessDialog : IRemoteProcessDialog
    {
        private readonly IGitUICommands _commands;
        private readonly ProcessWindow _window;
        private readonly ConsoleProcess _console;
        private readonly string? _urlTryingToConnect;
        private readonly RemoteProcessExitHandler? _onExit;
        private readonly RemoteProcessStrings _strings = ViewStrings.Load<RemoteProcessStrings>();
        private bool _restart;
        private bool _plink;

        public RemoteProcessDialog(
            IGitUICommands commands,
            ProcessWindow window,
            ProcessViewModel viewModel,
            ConsoleProcess console,
            string? remote,
            string? urlTryingToConnect,
            RemoteProcessExitHandler? onExit)
        {
            _commands = commands;
            _window = window;
            ViewModel = viewModel;
            _console = console;
            Remote = remote;
            _urlTryingToConnect = urlTryingToConnect;
            _onExit = onExit;

            viewModel.BeforeStart = BeforeProcessStart;
            viewModel.ExitHandler = HandleOnExit;
            viewModel.DataReceived += (_, text) => DataReceived(text);
        }

        public ProcessViewModel ViewModel { get; }

        public nint Handle => _window.NativeHandle;

        public string? Remote { get; }

        public string ProcessArguments
        {
            get => _console.Arguments;
            set => _console.Arguments = value;
        }

        public string GetOutputString() => ViewModel.Output;

        public void Retry() => ViewModel.Retry();

        private IGitModule Module => _commands.Module;

        private void BeforeProcessStart()
        {
            _restart = false;
            _plink = GitSshHelpers.IsPlink;
        }

        // As FormRemoteProcess.HandleOnExit.
        private bool HandleOnExit(ref bool isError)
        {
            if (_restart)
            {
                Retry();
                return true;
            }

            if (isError && _plink)
            {
                string output = GetOutputString();

                // The authentication failed because of a missing key: the user can supply one.
                if (output.Contains("FATAL ERROR") && output.Contains("authentication"))
                {
                    string? loadedKey = null;
                    if (AvaloniaUi.RunInHostContext(() => TryShowPuttyError(this, out bool retry, out loadedKey) && retry))
                    {
                        // The key is saved for the remote, against future authentication errors.
                        if (!string.IsNullOrEmpty(loadedKey) && !string.IsNullOrEmpty(Remote)
                            && string.IsNullOrEmpty(Module.GetSetting("remote.{0}.puttykeyfile")))
                        {
                            Module.SetSetting(string.Format("remote.{0}.puttykeyfile", Remote), loadedKey.ConvertPathToGitSetting()!);
                        }

                        Retry();
                        return true;
                    }
                }

                if (output.Contains("the server's host key is not cached in the registry", StringComparison.OrdinalIgnoreCase)
                    && AvaloniaUi.RunInHostContext(() => ProcessDialogs.AskForCacheHostkey(this, GetRemoteUrl())))
                {
                    Retry();
                    return true;
                }
            }

            return _onExit?.Invoke(ref isError, this) ?? false;
        }

        // As FormRemoteProcess.DataReceived: PuTTY waits for an answer to register the fingerprint of the host.
        private void DataReceived(string text)
        {
            if (!_plink || !text.Contains("If you trust this host, enter \"y\" to add the key to"))
            {
                return;
            }

            _console.PostToUiThread(() =>
            {
                bool register = AvaloniaUi.RunInHostContext(()
                    => MessageBoxes.Show(this, _strings.FingerprintNotRegistered.Text, _strings.FingerprintNotRegisteredCaption.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes);
                if (register)
                {
                    new Plink().Connect(GetRemoteUrl());
                    _restart = true;
                    ViewModel.Reset();
                }
                else
                {
                    ViewModel.Kill();
                }
            });
        }

        private string GetRemoteUrl()
        {
            if (!string.IsNullOrEmpty(_urlTryingToConnect))
            {
                return _urlTryingToConnect;
            }

            string remoteUrl = Module.GetSetting(string.Format(SettingKeyString.RemoteUrl, Remote));
            return string.IsNullOrEmpty(remoteUrl) ? Remote ?? "" : remoteUrl;
        }
    }
}
