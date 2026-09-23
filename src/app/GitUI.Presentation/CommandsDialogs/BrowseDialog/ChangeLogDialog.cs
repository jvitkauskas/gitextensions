using GitUI.Presentation.Editor;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs.BrowseDialog;

/// <summary>Strings of the change log dialog; ids match <c>FormChangeLog</c>.</summary>
public sealed class ChangeLogStrings : ViewStrings
{
    public ChangeLogStrings()
        : base("FormChangeLog")
    {
        Title = Add("$this", "Text", "Change log");
    }

    public TranslatedText Title { get; }
}

/// <summary>View model of the change log dialog (port of <c>FormChangeLog</c>): the change log, read-only.</summary>
public sealed class ChangeLogViewModel : DialogViewModel
{
    public ChangeLogViewModel(ChangeLogStrings strings, string changeLog)
    {
        Strings = strings;
        ChangeLog.Load(changeLog, fileName: "ChangeLog.md");
    }

    public ChangeLogStrings Strings { get; }

    public TextEditorViewModel ChangeLog { get; } = new() { IsReadOnly = true };
}
