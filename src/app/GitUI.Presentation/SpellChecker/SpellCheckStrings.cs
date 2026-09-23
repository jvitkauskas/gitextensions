using GitUI.Presentation.Translations;

namespace GitUI.Presentation.SpellChecker;

/// <summary>Strings of the spell-checked message editor; ids match <c>EditNetSpell</c>.</summary>
public sealed class SpellCheckStrings : ViewStrings
{
    public SpellCheckStrings()
        : base("EditNetSpell")
    {
        Cut = Add("_cutMenuItemText", "Text", "Cut");
        Copy = Add("_copyMenuItemText", "Text", "Copy");
        Paste = Add("_pasteMenuItemText", "Text", "Paste");
        Delete = Add("_deleteMenuItemText", "Text", "Delete");
        SelectAll = Add("_selectAllMenuItemText", "Text", "Select all");
        AddToDictionary = Add("_addToDictionaryText", "Text", "Add to dictionary");
        IgnoreWord = Add("_ignoreWordText", "Text", "Ignore word");
        RemoveWord = Add("_removeWordText", "Text", "Remove word");
        Dictionary = Add("_dictionaryText", "Text", "Dictionary");
        MarkIllFormedLines = Add("_markIllFormedLinesText", "Text", "Mark ill formed lines");
        AutoCompletion = Add("_autoCompletionText", "Text", "Provide auto completion");
    }

    public TranslatedText Cut { get; }

    public TranslatedText Copy { get; }

    public TranslatedText Paste { get; }

    public TranslatedText Delete { get; }

    public TranslatedText SelectAll { get; }

    public TranslatedText AddToDictionary { get; }

    public TranslatedText IgnoreWord { get; }

    public TranslatedText RemoveWord { get; }

    public TranslatedText Dictionary { get; }

    public TranslatedText MarkIllFormedLines { get; }

    public TranslatedText AutoCompletion { get; }
}
