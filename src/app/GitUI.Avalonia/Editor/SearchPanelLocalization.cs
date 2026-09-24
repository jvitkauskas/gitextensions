using System.Globalization;
using System.Reflection;
using GitUI.Presentation.Editor;

namespace GitUI.Avalonia.Editor;

/// <summary>
///  Translates the texts of AvaloniaEdit's search panel with the strings of <c>FindAndReplaceForm</c>.
/// </summary>
/// <remarks>
///  AvaloniaEdit reads them from its resources (<c>AvaloniaEdit.SR</c>), whose resource manager is lazily created in a private
///  field: that field is set to a manager that returns the translated texts first. If a version of AvaloniaEdit no longer
///  has the field, the panel simply stays English.
/// </remarks>
internal static class SearchPanelLocalization
{
    private static bool _applied;

    public static void Apply(FindAndReplaceStrings strings)
    {
        if (_applied)
        {
            return;
        }

        _applied = true;
        FieldInfo? field = typeof(AvaloniaEdit.SR).GetField("resourceMan", BindingFlags.NonPublic | BindingFlags.Static);
        if (field?.FieldType != typeof(System.Resources.ResourceManager))
        {
            return;
        }

        field.SetValue(null, new TranslatedResourceManager(AvaloniaEdit.SR.ResourceManager, strings.GetSearchPanelTexts()));
    }

    /// <summary>The texts of the search panel, then AvaloniaEdit's own resources.</summary>
    private sealed class TranslatedResourceManager(System.Resources.ResourceManager resources, IReadOnlyDictionary<string, string> texts) : System.Resources.ResourceManager
    {
        public override string? GetString(string name) => GetString(name, culture: null);

        public override string? GetString(string name, CultureInfo? culture)
            => texts.TryGetValue(name, out string? text) ? text : resources.GetString(name, culture);

        public override object? GetObject(string name, CultureInfo? culture) => resources.GetObject(name, culture);
    }
}
