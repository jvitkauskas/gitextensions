using GitCommands;
using GitExtUtils;
using GitUI.Models;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.AvaloniaHosting;

/// <summary>The output history tab of the Avalonia main window (<c>OutputHistoryTabController</c>).</summary>
internal static partial class AvaloniaDialogs
{
    private sealed partial class BrowseHost : IBrowseOutputHistoryHost
    {
        private readonly List<EventHandler> _outputHistoryHandlers = [];
        private EventHandler? _outputHistoryChanged;

        // The provider lives as long as the application and raises on any thread: the handler of this repository is
        // removed by Dispose, and it forwards on the UI thread (as Update of OutputHistoryControllerBase).
        public event EventHandler? OutputHistoryChanged
        {
            add
            {
                if (_outputHistoryChanged is null)
                {
                    EventHandler forward = (_, _) => ThreadHelper.FileAndForget(async () =>
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        _outputHistoryChanged?.Invoke(this, EventArgs.Empty);
                    });
                    OutputHistoryProvider.HistoryChanged += forward;
                    _outputHistoryHandlers.Add(forward);
                }

                _outputHistoryChanged += value;
            }

            remove => _outputHistoryChanged -= value;
        }

        public bool IsOutputHistoryEnabled => OutputHistoryProvider.Enabled;

        public bool ShowOutputHistoryAsTab => AppSettings.ShowOutputHistoryAsTab.Value;

        public bool IsOutputHistoryPanelVisible
        {
            get => AppSettings.OutputHistoryPanelVisible.Value;
            set => AppSettings.OutputHistoryPanelVisible.Value = value;
        }

        public string OutputHistory => OutputHistoryProvider.History;

        private IOutputHistoryProvider OutputHistoryProvider => _commands.GetRequiredService<IOutputHistoryProvider>();

        public void ClearOutputHistory() => OutputHistoryProvider.ClearHistory();

        public void CopyOutputHistory(string text) => ClipboardUtil.TrySetText(text);

        private void UnsubscribeOutputHistory()
        {
            foreach (EventHandler handler in _outputHistoryHandlers)
            {
                OutputHistoryProvider.HistoryChanged -= handler;
            }

            _outputHistoryHandlers.Clear();
            _outputHistoryChanged = null;
        }
    }
}
