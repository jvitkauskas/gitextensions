using GitExtensions.Extensibility;

namespace GitUIPluginInterfacesTests;
public class FontParserTests
{
    private static readonly FontDescriptor _defaultFont = new("Arial", 9);

    [TestCase(false, false, "Arial;9;_IC_;0;0")]
    [TestCase(true, false, "Arial;9;_IC_;1;0")]
    [TestCase(false, true, "Arial;9;_IC_;0;1")]
    [TestCase(true, true, "Arial;9;_IC_;1;1")]
    public void AsString_should_persist_font_with_styles(bool isBold, bool isItalic, string? serialised)
    {
        FontDescriptor font = new("Arial", 9, isBold, isItalic);
        font.AsString().Should().Be(serialised);
    }

    [Test]
    public void AsString_should_persist_fractional_size_invariantly()
    {
        new FontDescriptor("Segoe UI", 8.25f).AsString().Should().Be("Segoe UI;8.25;_IC_;0;0");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("\t")]
    public void Parse_should_return_default_if_null_or_empty(string? serialised)
    {
        FontDescriptor font = serialised.Parse(_defaultFont);

        font.Should().BeSameAs(_defaultFont);
    }

    [Test]
    public void Parse_should_return_null_default_if_null_or_empty()
    {
        FontDescriptor? font = ((string?)null).Parse(null);

        font.Should().BeNull();
    }

    [TestCase("Arial")]
    [TestCase("Arial;")]
    [TestCase(";9")]
    public void Parse_should_return_default_if_less_then_two_parts(string? serialised)
    {
        FontDescriptor font = serialised.Parse(_defaultFont);

        font.Should().BeSameAs(_defaultFont);
    }

    [TestCase("Courier;abc;_IC_;0;0")]
    [TestCase("Courier;0;_IC_;0;0")]
    [TestCase("Courier;-2;_IC_;0;0")]
    public void Parse_should_return_default_if_the_size_is_not_positive(string? serialised)
    {
        FontDescriptor font = serialised.Parse(_defaultFont);

        font.Should().BeSameAs(_defaultFont);
    }

    [TestCase("Courier;8.25;", "Courier", 8.25f, false, false)]
    [TestCase("Courier;12;_IC_", "Courier", 12f, false, false)]
    [TestCase("Courier;11,3;", "Courier", 11.3f, false, false)]
    [TestCase("Courier;11,3;ru", "Courier", 11.3f, false, false)]
    [TestCase("Courier;12;_IC_;0;0", "Courier", 12f, false, false)]
    [TestCase("Courier;12;_IC_;1;0", "Courier", 12f, true, false)]
    [TestCase("Courier;12;_IC_;0;1", "Courier", 12f, false, true)]
    [TestCase("Courier;12;_IC_;1;1", "Courier", 12f, true, true)]
    public void Parse_should_parse(string? serialised, string name, float size, bool isBold, bool isItalic)
    {
        FontDescriptor font = serialised.Parse(_defaultFont);

        font.Should().Be(new FontDescriptor(name, size, isBold, isItalic));
    }

    [Test]
    public void Parse_should_read_what_AsString_wrote()
    {
        FontDescriptor font = new("Cascadia Mono", 10.5f, IsBold: true);

        font.AsString().Parse(_defaultFont).Should().Be(font);
    }
}
