using Avalonia.Themes.Fluent;
using GitUI.Avalonia;

namespace GitUI.AvaloniaTests.Views;

/// <summary>The control theme of the Avalonia UI: Fluent, or a community theme chosen in the Colors settings.</summary>
[TestFixture]
public sealed class ControlThemeTests : HeadlessTest
{
    [TestCase(null, null, "fluent")]
    [TestCase("simple", null, "simple")]
    [TestCase("Classic ", null, "classic")]
    [TestCase("simple", "classic", "classic")]
    [TestCase("simple", " ", "simple")]
    [TestCase("semi", null, "fluent")]
    [TestCase("unknown", null, "fluent")]
    [TestCase(null, "unknown", "fluent")]
    public void The_variable_overrides_the_setting_and_an_unknown_name_is_Fluent(string? setting, string? variable, string expected)
    {
        GitExtensionsAvaloniaApp.ResolveControlTheme(setting, variable ?? "").Should().Be(expected);
    }

    [Test]
    public Task Fluent_comes_before_the_styles_of_the_application() => OnUiThreadAsync(() =>
    {
        global::Avalonia.Application.Current!.Styles[0].Should().BeOfType<FluentTheme>();
    });
}
