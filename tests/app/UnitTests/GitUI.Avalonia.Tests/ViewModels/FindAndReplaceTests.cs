using GitUI.Presentation.Editor;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>Tests of the search logic of <c>FindAndReplaceForm</c> kept for the Avalonia editors.</summary>
[TestFixture]
public sealed class FindAndReplaceTests
{
    [TestCase("int value = 42;", 6, "value")]
    [TestCase("int value = 42;", 4, "value")]
    [TestCase("int value = 42;", 9, "value")]
    [TestCase("int value = 42;", 10, "")]
    [TestCase("my_name2", 0, "my_name2")]
    [TestCase("", 0, "")]
    [TestCase("abc", 99, "abc")]
    public void GetWordAt_finds_the_word_around_the_offset(string text, int offset, string expected)
        => TextSearch.GetWordAt(text, offset).Should().Be(expected);

    [Test]
    public void The_search_panel_texts_are_the_FindAndReplaceForm_strings_with_the_keys()
    {
        IReadOnlyDictionary<string, string> texts = new FindAndReplaceStrings().GetSearchPanelTexts();

        texts["SearchLabel"].Should().Be("Find...");
        texts["ReplaceLabel"].Should().Be("Replace...");
        texts["SearchFindPreviousText"].Should().Be("Find previous (Shift+F3)");
        texts["SearchReplaceAll"].Should().Be("Replace All (Alt+A)");
        texts.Should().NotContainKey("SearchUseRegexText");
    }
}
