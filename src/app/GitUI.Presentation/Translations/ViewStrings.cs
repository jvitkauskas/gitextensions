using GitCommands;
using GitExtensions.Extensibility.Translations;

namespace GitUI.Presentation.Translations;

/// <summary>
///  Base class for the translatable strings of a ported view.
/// </summary>
/// <remarks>
///  <para>
///   Each entry is declared with the exact XLIFF id (<c>item.property</c>) of the WinForms form it replaces,
///   under the same category (the form's class name). The existing translations therefore apply unchanged,
///   and <c>TranslationApp</c> produces identical entries whether the WinForms form or the view is scanned.
///  </para>
///  <para>
///   Unlike <c>ResourceManager.Translate</c>, entries are declared explicitly instead of being discovered by
///   reflecting over WinForms controls, so they do not depend on WinForms naming or tooltip conventions.
///  </para>
/// </remarks>
public abstract class ViewStrings : ITranslate
{
    private readonly string _category;
    private readonly List<(string Item, string Property, TranslatedText Text)> _entries = [];

    protected ViewStrings(string category)
    {
        _category = category;
    }

    /// <summary>
    ///  Creates the strings of a view, translated to the current UI language.
    /// </summary>
    public static T Load<T>()
        where T : ViewStrings, new()
    {
        T strings = new();
        Translator.Translate(strings, AppSettings.CurrentTranslation);
        return strings;
    }

    /// <summary>
    ///  Declares a translatable entry, e.g. <c>Add("label1", "Text", "New name")</c> or <c>Add("$this", "Text", "Rename branch")</c> for the title.
    /// </summary>
    protected TranslatedText Add(string item, string property, string neutralText)
    {
        TranslatedText text = new(neutralText);
        _entries.Add((item, property, text));
        return text;
    }

    void ITranslate.AddTranslationItems(ITranslation translation)
    {
        foreach ((string item, string property, TranslatedText text) in _entries)
        {
            // Same filter as TranslationUtil.AllowTranslateProperty: strings without letters are not translatable.
            if (text.NeutralText.Any(char.IsLetter))
            {
                translation.AddTranslationItem(_category, item, property, text.NeutralText);
            }
        }
    }

    void ITranslate.TranslateItems(ITranslation translation)
    {
        foreach ((string item, string property, TranslatedText text) in _entries)
        {
            text.Text = translation.TranslateItem(_category, item, property, () => text.NeutralText) ?? text.NeutralText;
        }
    }

    void IDisposable.Dispose()
    {
        GC.SuppressFinalize(this);
    }
}

/// <summary>
///  A single translatable string of a view.
/// </summary>
public sealed class TranslatedText
{
    internal TranslatedText(string neutralText)
    {
        NeutralText = neutralText;
        Text = neutralText;
    }

    /// <summary>The untranslated (English) text.</summary>
    public string NeutralText { get; }

    /// <summary>The text in the current UI language.</summary>
    public string Text { get; internal set; }

    public override string ToString() => Text;
}
