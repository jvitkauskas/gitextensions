using System.Xml.Linq;
using GitExtensions.Extensibility.Translations;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs.CommitDialog;
using GitUI.Presentation.Translations;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>
///  The ported views reuse the XLIFF ids of the WinForms forms they replace, so that existing translations keep
///  working. These tests pin every declared entry to the checked-in English.xlf.
/// </summary>
[TestFixture]
public sealed class ViewStringsTests
{
    private static readonly Lazy<Dictionary<(string Category, string Id), string>> _englishXlf = new(LoadEnglishXlf);

    private static IEnumerable<TestCaseData> AllViewStrings()
    {
        yield return new TestCaseData(new AboutStrings()).SetArgDisplayNames(nameof(AboutStrings));
        yield return new TestCaseData(new RenameBranchStrings()).SetArgDisplayNames(nameof(RenameBranchStrings));
        yield return new TestCaseData(new CommitTemplateSettingsStrings()).SetArgDisplayNames(nameof(CommitTemplateSettingsStrings));
    }

    [TestCaseSource(nameof(AllViewStrings))]
    public void Every_entry_matches_English_xlf(ViewStrings strings)
    {
        RecordingTranslation recorded = new();

        ((ITranslate)strings).AddTranslationItems(recorded);

        recorded.Items.Should().NotBeEmpty();
        foreach ((string category, string item, string property, string neutralValue) in recorded.Items)
        {
            string id = $"{item}.{property}";
            _englishXlf.Value.Should().ContainKey((category, id), $"'{category}/{id}' must exist in English.xlf");
            Normalize(_englishXlf.Value[(category, id)]).Should().Be(Normalize(neutralValue), $"the source text of '{category}/{id}'");
        }
    }

    [Test]
    public void TranslateItems_applies_translations_and_falls_back_to_neutral_text()
    {
        RenameBranchStrings strings = new();
        RecordingTranslation translation = new() { Translations = { [("FormRenameBranch", "label1", "Text")] = "Neuer Name" } };

        ((ITranslate)strings).TranslateItems(translation);

        strings.NewName.Text.Should().Be("Neuer Name");
        strings.Rename.Text.Should().Be("Rename");
        strings.NewName.NeutralText.Should().Be("New name");
    }

    // English.xlf stores multi-line sources with whatever line endings git checked out.
    private static string Normalize(string text) => text.ReplaceLineEndings("\n");

    private static Dictionary<(string Category, string Id), string> LoadEnglishXlf()
    {
        string path = Path.Combine(FindRepoRoot(), "src", "app", "GitUI", "Translation", "English.xlf");
        XDocument document = XDocument.Load(path);
        XNamespace ns = document.Root!.Name.Namespace;

        return document.Descendants(ns + "file")
            .SelectMany(file => file.Descendants(ns + "trans-unit").Select(unit => (
                Category: (string)file.Attribute("original")!,
                Id: (string)unit.Attribute("id")!,
                Source: (string)unit.Element(ns + "source")!)))
            .ToDictionary(u => (u.Category, u.Id), u => u.Source);

        static string FindRepoRoot()
        {
            DirectoryInfo? directory = new(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "GitExtensions.slnx")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found");
        }
    }

    private sealed class RecordingTranslation : ITranslation
    {
        public List<(string Category, string Item, string Property, string NeutralValue)> Items { get; } = [];

        public Dictionary<(string Category, string Item, string Property), string> Translations { get; } = [];

        public void AddTranslationItem(string category, string item, string property, string neutralValue)
            => Items.Add((category, item, property, neutralValue));

        public string? TranslateItem(string category, string item, string property, Func<string?> provideDefaultValue)
            => Translations.TryGetValue((category, item, property), out string? value) ? value : provideDefaultValue();
    }
}
