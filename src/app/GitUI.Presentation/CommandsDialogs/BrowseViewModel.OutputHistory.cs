using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the output history; ids match <c>OutputHistoryControl</c> and the tab caption of <c>FormBrowse</c>.</summary>
public sealed class OutputHistoryStrings : ViewStrings
{
    public OutputHistoryStrings()
        : base("OutputHistoryControl")
    {
        Copy = Add("tsmiCopy", "Text", "&Copy");
        Clear = Add("tsmiClear", "Text", "C&lear");
        Tab = Add("_outputHistoryTabCaption", "Text", "Output", category: "FormBrowse");
    }

    public TranslatedText Copy { get; }

    public TranslatedText Clear { get; }

    public TranslatedText Tab { get; }
}

/// <summary>What the output history needs from the application (<c>IOutputHistoryProvider</c>).</summary>
public interface IBrowseOutputHistoryHost
{
    /// <summary>Raised on the UI thread when something was added to the history, or it was cleared.</summary>
    event EventHandler? OutputHistoryChanged;

    bool IsOutputHistoryEnabled { get; }

    string OutputHistory { get; }

    void ClearOutputHistory();

    void CopyOutputHistory(string text);
}

/// <summary>The output history tab of the main window (<c>OutputHistoryTabController</c>).</summary>
public sealed partial class BrowseViewModel
{
    private IBrowseOutputHistoryHost? _outputHistoryHost;

    public OutputHistoryStrings OutputHistoryStrings { get; } = ViewStrings.Load<OutputHistoryStrings>();

    /// <summary>Whether the tab is shown (<c>IOutputHistoryProvider.Enabled</c>).</summary>
    public bool HasOutputHistory { get; private set; }

    /// <summary>The output of the git commands run so far.</summary>
    [ObservableProperty]
    public partial string OutputHistoryText { get; private set; } = "";

    /// <summary>As the Copy item: the selection, else the whole history.</summary>
    public void CopyOutputHistory(string? selectedText)
        => _outputHistoryHost?.CopyOutputHistory(string.IsNullOrEmpty(selectedText) ? OutputHistoryText : selectedText);

    [RelayCommand]
    private void ClearOutputHistory() => _outputHistoryHost?.ClearOutputHistory();

    private void InitializeOutputHistory()
    {
        _outputHistoryHost = _host as IBrowseOutputHistoryHost;
        HasOutputHistory = _outputHistoryHost?.IsOutputHistoryEnabled is true;
        if (!HasOutputHistory)
        {
            return;
        }

        OutputHistoryText = _outputHistoryHost!.OutputHistory;
        _outputHistoryHost.OutputHistoryChanged += (_, _) => OutputHistoryText = _outputHistoryHost.OutputHistory;
    }
}
