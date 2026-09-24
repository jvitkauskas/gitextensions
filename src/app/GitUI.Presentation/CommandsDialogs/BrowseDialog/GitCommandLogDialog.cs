using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs.BrowseDialog;

/// <summary>Strings of the git command log; ids match <c>FormGitCommandLog</c>.</summary>
public sealed class GitCommandLogStrings : ViewStrings
{
    public GitCommandLogStrings()
        : base("FormGitCommandLog")
    {
        Title = Add("$this", "Text", "Git Command Log");
        CommandLogTab = Add("tabPageCommandLog", "Text", "Command log");
        CommandCacheTab = Add("tabPageCommandCache", "Text", "Command cache");
        SaveToFile = Add("mnuSaveToFile", "Text", "&Save to file");
        CopyCommandLine = Add("mnuCopyCommandLine", "Text", "&Copy full command line");
        ClearLog = Add("mnuClear", "Text", "C&lear");
        ClearCache = Add("tsmiClearCache", "Text", "C&lear");
        AlwaysOnTop = Add("chkAlwaysOnTop", "Text", "Always on top");
        WordWrap = Add("chkWordWrap", "Text", "Word wrap");
        CaptureCallStacks = Add("chkCaptureCallStacks", "Text", "Capture call stacks");
    }

    public TranslatedText Title { get; }

    public TranslatedText CommandLogTab { get; }

    public TranslatedText CommandCacheTab { get; }

    public TranslatedText SaveToFile { get; }

    public TranslatedText CopyCommandLine { get; }

    public TranslatedText ClearLog { get; }

    public TranslatedText ClearCache { get; }

    public TranslatedText AlwaysOnTop { get; }

    public TranslatedText WordWrap { get; }

    public TranslatedText CaptureCallStacks { get; }
}

/// <summary>A logged command (<c>CommandLogEntry</c>): its line in the list, its details and its full command line.</summary>
public sealed record GitCommandLogItem(string ColumnLine, string Detail, string CommandLine)
{
    public override string ToString() => ColumnLine;
}

/// <summary>A cached command (<c>CacheItem</c> of <c>FormGitCommandLog</c>): its cache key and its arguments without the configuration.</summary>
public sealed record GitCommandCacheItem(string Key, string DisplayString)
{
    public override string ToString() => DisplayString;
}

/// <summary>The command log and the command cache of the host (<c>CommandLog</c>, <c>GitModule.GitCommandCache</c>).</summary>
public interface IGitCommandLogHost
{
    /// <summary>Raised on the UI thread when a command was logged or ended (<c>CommandLog.CommandsChanged</c>).</summary>
    event EventHandler? CommandsChanged;

    /// <summary>Raised on the UI thread when the command cache changed (<c>CommandCache.Changed</c>).</summary>
    event EventHandler? CacheChanged;

    /// <summary>Whether the call stacks of the commands are captured (<c>AppSettings.LogCaptureCallStacks</c>).</summary>
    bool CaptureCallStacks { get; set; }

    IReadOnlyList<GitCommandLogItem> GetCommands();

    IReadOnlyList<GitCommandCacheItem> GetCachedCommands();

    /// <summary>The cached output of a command (<c>CommandCache.TryGet</c>).</summary>
    bool TryGetCachedOutput(string key, out string? output, out string? error);

    void ClearCommands();

    void ClearCache();

    /// <summary>As <c>mnuSaveToFile_Click</c>: asks for a file and writes the log to it (tab or list separated).</summary>
    void SaveCommandsToFile();

    void CopyToClipboard(string text);
}

/// <summary>View model of the git command log (port of <c>FormGitCommandLog</c>), a modeless window.</summary>
public sealed partial class GitCommandLogViewModel : DialogViewModel, IDisposable
{
    /// <summary>The index of the command cache tab in <see cref="SelectedTabIndex"/>.</summary>
    public const int CommandCacheTabIndex = 1;

    private readonly IGitCommandLogHost _host;
    private bool _started;

    public GitCommandLogViewModel(GitCommandLogStrings strings, IGitCommandLogHost host)
    {
        Strings = strings;
        _host = host;
        CaptureCallStacks = host.CaptureCallStacks;
    }

    public GitCommandLogStrings Strings { get; }

    public ObservableCollection<GitCommandLogItem> LogItems { get; } = [];

    public ObservableCollection<GitCommandCacheItem> CacheItems { get; } = [];

    /// <summary>0 for the command log tab, <see cref="CommandCacheTabIndex"/> for the command cache.</summary>
    [ObservableProperty]
    public partial int SelectedTabIndex { get; set; }

    [ObservableProperty]
    public partial GitCommandLogItem? SelectedLogItem { get; set; }

    [ObservableProperty]
    public partial GitCommandCacheItem? SelectedCacheItem { get; set; }

    /// <summary>The details of the selected command (<c>LogOutput</c>).</summary>
    [ObservableProperty]
    public partial string LogOutput { get; private set; } = "";

    /// <summary>The cached output of the selected command (<c>commandCacheOutput</c>).</summary>
    [ObservableProperty]
    public partial string CacheOutput { get; private set; } = "";

    /// <summary>The window stays above the other windows (<c>TopMost</c>).</summary>
    [ObservableProperty]
    public partial bool AlwaysOnTop { get; set; }

    [ObservableProperty]
    public partial bool WordWrap { get; set; }

    [ObservableProperty]
    public partial bool CaptureCallStacks { get; set; }

    /// <summary>As the <c>Load</c> event: lists the commands and follows the changes until <see cref="Dispose"/>.</summary>
    public void Start()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        _host.CommandsChanged += OnCommandsChanged;
        _host.CacheChanged += OnCacheChanged;
        RefreshLogItems();
        RefreshCacheItems();
    }

    /// <summary>As <c>FormClosed</c>: stops following the changes.</summary>
    public void Dispose()
    {
        if (!_started)
        {
            return;
        }

        _started = false;
        _host.CommandsChanged -= OnCommandsChanged;
        _host.CacheChanged -= OnCacheChanged;
    }

    /// <summary>As <c>PrintableChars</c>: the control characters and spaces of the output made visible.</summary>
    public static string? PrintableChars(string? text)
        => text?.Replace("\0", @"\0").Replace("\r", @"\r").Replace("\n", "\\n\n").Replace("\t", "\u00bb").Replace(" ", "\u00b7").Replace("\u001b", @"\x1b");

    [RelayCommand]
    private void SaveToFile() => _host.SaveCommandsToFile();

    [RelayCommand]
    private void ClearLog() => _host.ClearCommands();

    [RelayCommand]
    private void CopyCommandLine()
    {
        if (SelectedLogItem is { } item)
        {
            _host.CopyToClipboard(item.CommandLine);
        }
    }

    [RelayCommand]
    private void ClearCache()
    {
        _host.ClearCache();
        RefreshCacheItems();
    }

    partial void OnSelectedTabIndexChanged(int value)
    {
        RefreshLogItems();
        RefreshCacheItems();
    }

    partial void OnSelectedLogItemChanged(GitCommandLogItem? value)
    {
        // As LogItems_SelectedIndexChanged (the text is kept while the list is refreshed).
        if (value is not null)
        {
            LogOutput = value.Detail;
        }
    }

    partial void OnSelectedCacheItemChanged(GitCommandCacheItem? value)
    {
        // As CommandCacheItems_SelectedIndexChanged.
        if (value is null)
        {
            return;
        }

        CacheOutput = _host.TryGetCachedOutput(value.Key, out string? output, out string? error)
            ? value.Key +
              "\n-------------------------------------\n\n" +
              PrintableChars(output) +
              "\n-------------------------------------\n\n" +
              PrintableChars(error)
            : "";
    }

    partial void OnCaptureCallStacksChanged(bool value) => _host.CaptureCallStacks = value;

    private void OnCommandsChanged(object? sender, EventArgs e) => RefreshLogItems();

    private void OnCacheChanged(object? sender, EventArgs e) => RefreshCacheItems();

    /// <summary>As <c>RefreshLogItems</c>: only the visible tab is refreshed.</summary>
    private void RefreshLogItems()
    {
        if (SelectedTabIndex != CommandCacheTabIndex)
        {
            Refresh(LogItems, _host.GetCommands(), SelectedLogItem, item => SelectedLogItem = item);
        }
    }

    private void RefreshCacheItems()
    {
        if (SelectedTabIndex == CommandCacheTabIndex)
        {
            Refresh(CacheItems, _host.GetCachedCommands(), SelectedCacheItem, item => SelectedCacheItem = item);
        }
    }

    /// <summary>
    ///  As <c>RefreshListBox</c>: the last item is selected if it was (or nothing was), else the item at the same index.
    /// </summary>
    private static void Refresh<T>(ObservableCollection<T> list, IReadOnlyList<T> items, T? selected, Action<T?> select)
        where T : class
    {
        int selectedIndex = selected is null ? -1 : list.IndexOf(selected);
        bool isLastIndexSelected = list.Count == 0 || selectedIndex == list.Count - 1;

        list.Clear();
        foreach (T item in items)
        {
            list.Add(item);
        }

        if (list.Count < 1)
        {
            return;
        }

        select(isLastIndexSelected || selectedIndex < 0 || selectedIndex >= list.Count ? list[^1] : list[selectedIndex]);
    }
}
