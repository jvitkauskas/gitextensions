using GitCommands;
using GitCommands.Logging;
using GitExtensions.Extensibility;
using GitExtUtils;
using GitUI.Avalonia.CommandsDialogs.BrowseDialog;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs.BrowseDialog;
using GitUI.Presentation.CommandsDialogs.BrowseDialog;
using GitUI.Presentation.Translations;

namespace GitUI.AvaloniaHosting;

/// <summary>Routing of the git command log (docs/avalonia-port/PLAN.md).</summary>
internal static partial class AvaloniaDialogs
{
    /// <summary>The open command log: a single instance, as the static field of <c>FormGitCommandLog</c>.</summary>
    private static GitCommandLogWindow? _gitCommandLogWindow;

    /// <summary>
    ///  Shows the Avalonia port of <c>FormGitCommandLog</c> modelessly over <paramref name="owner"/>, or restores or activates
    ///  the one already open (as <c>FormGitCommandLog.ShowOrActivate</c>).
    /// </summary>
    public static bool TryShowGitCommandLog(IWin32Window? owner)
    {
        if (_gitCommandLogWindow is { } openWindow)
        {
            if (openWindow.WindowState == global::Avalonia.Controls.WindowState.Minimized)
            {
                openWindow.WindowState = global::Avalonia.Controls.WindowState.Normal;
            }
            else
            {
                openWindow.Activate();
            }

            return true;
        }

        AvaloniaUi.EnsureInitialized(GetOptions);
        GitCommandLogWindow window = new() { PositionName = "FormGitCommandLog", PositionStore = WindowPositionStore.Instance };
        window.DataContext = new GitCommandLogViewModel(ViewStrings.Load<GitCommandLogStrings>(), new GitCommandLogHost(window));
        _gitCommandLogWindow = window;
        window.Closed += (_, _) => _gitCommandLogWindow = null;

        // An owned form closes with its owner.
        if (owner is not null && Control.FromChildHandle(owner.Handle)?.FindForm() is Form form)
        {
            void CloseWithForm(object? sender, FormClosedEventArgs e) => window.Close();
            form.FormClosed += CloseWithForm;
            window.Closed += (_, _) => form.FormClosed -= CloseWithForm;
        }

        AvaloniaDialogHost.Show(window, owner?.Handle ?? 0);

        // As FormGitCommandLog (ShowInTaskbar = true), although owned.
        window.ShowInTaskbar = true;
        return true;
    }

    /// <summary>The command log (<c>CommandLog</c>) and the command cache (<c>GitModule.GitCommandCache</c>).</summary>
    private sealed class GitCommandLogHost : IGitCommandLogHost
    {
        private readonly DialogWindow _window;
        private EventHandler? _commandsChanged;
        private EventHandler? _cacheChanged;

        public GitCommandLogHost(DialogWindow window)
        {
            _window = window;
        }

        public event EventHandler? CommandsChanged
        {
            add
            {
                if (_commandsChanged is null)
                {
                    CommandLog.CommandsChanged += OnCommandsChanged;
                }

                _commandsChanged += value;
            }

            remove
            {
                _commandsChanged -= value;
                if (_commandsChanged is null)
                {
                    CommandLog.CommandsChanged -= OnCommandsChanged;
                }
            }
        }

        public event EventHandler? CacheChanged
        {
            add
            {
                if (_cacheChanged is null)
                {
                    GitModule.GitCommandCache.Changed += OnCacheChanged;
                }

                _cacheChanged += value;
            }

            remove
            {
                _cacheChanged -= value;
                if (_cacheChanged is null)
                {
                    GitModule.GitCommandCache.Changed -= OnCacheChanged;
                }
            }
        }

        public bool CaptureCallStacks
        {
            get => AppSettings.LogCaptureCallStacks;
            set => AppSettings.LogCaptureCallStacks = value;
        }

        public IReadOnlyList<GitCommandLogItem> GetCommands()
            => [.. CommandLog.Commands.ToArray().Select(entry => new GitCommandLogItem(entry.ColumnLine, entry.Detail, entry.CommandLine))];

        public IReadOnlyList<GitCommandCacheItem> GetCachedCommands()
            => [.. GitModule.GitCommandCache.GetCachedCommands().Select(key => new GitCommandCacheItem(key, CommandLogEntry.GetGitArgumentsWithoutConfiguration(key)))];

        public bool TryGetCachedOutput(string key, out string? output, out string? error)
            => GitModule.GitCommandCache.TryGet(key, out output, out error);

        public void ClearCommands() => CommandLog.Clear();

        public void ClearCache() => GitModule.GitCommandCache.Clear();

        /// <summary>As <c>mnuSaveToFile_Click</c>.</summary>
        public void SaveCommandsToFile() => AvaloniaUi.RunInHostContext(() =>
        {
            using SaveFileDialog fileDialog = new()
            {
                Title = "FormGitCommandLog",
                DefaultExt = ".txt",
                AddExtension = true,
                Filter = "Text files (*.txt)|*.txt|CSV files|*.csv|All files *.*|*.*"
            };
            if (fileDialog.ShowDialog(new NativeWindowOwner(_window)) == DialogResult.OK)
            {
                string separator = fileDialog.FileName.EndsWith("csv") ?
                    System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator :
                    "\t";
                File.WriteAllLines(
                    fileDialog.FileName,
                    CommandLog.Commands.Select(cle => cle.FullLine(separator)));
            }
        });

        public void CopyToClipboard(string text) => ClipboardUtil.TrySetText(text);

        // Commands are logged on any thread: the lists are refreshed on the UI thread (as InvokeAndForget).
        private void OnCommandsChanged() => global::Avalonia.Threading.Dispatcher.UIThread.Post(() => _commandsChanged?.Invoke(this, EventArgs.Empty));

        private void OnCacheChanged(object? sender, EventArgs e) => global::Avalonia.Threading.Dispatcher.UIThread.Post(() => _cacheChanged?.Invoke(this, EventArgs.Empty));
    }
}
