using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the rename branch dialog; ids match <c>FormRenameBranch</c>.</summary>
public sealed class RenameBranchStrings : ViewStrings
{
    public RenameBranchStrings()
        : base("FormRenameBranch")
    {
        Title = Add("$this", "Text", "Rename branch");
        NewName = Add("label1", "Text", "New name");
        Rename = Add("Ok", "Text", "Rename");
    }

    public TranslatedText Title { get; }

    public TranslatedText NewName { get; }

    public TranslatedText Rename { get; }
}
