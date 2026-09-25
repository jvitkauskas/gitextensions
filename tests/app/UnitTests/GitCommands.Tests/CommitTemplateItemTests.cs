using GitCommands;
using GitExtensions.Extensibility.Plugins;
using JsonSerializer = GitCommands.Utils.JsonSerializer;

namespace GitCommandsTests;

/// <summary>The templates of the settings, saved as they are by <c>CommitTemplateItem.SaveToSettings</c>.</summary>
public sealed class CommitTemplateItemTests
{
    [Test]
    public void The_templates_are_serialized_without_their_icon()
    {
        CommitTemplateItem[] items = [new("Ticket", "{{([A-Z]+-\\d+)}}: ", PluginImage.FromBytes([1, 2, 3]), isRegex: true), new()];

        string json = JsonSerializer.Serialize(items);
        CommitTemplateItem[]? loaded = JsonSerializer.Deserialize<CommitTemplateItem[]>(json);

        json.Should().NotContain("Icon");
        loaded.Should().HaveCount(2);
        loaded![0].Name.Should().Be("Ticket");
        loaded[0].Text.Should().Be("{{([A-Z]+-\\d+)}}: ");
        loaded[0].IsRegex.Should().BeTrue();
        loaded[0].Icon.Should().BeNull();
    }

    [Test]
    public void The_templates_saved_with_an_icon_member_before_are_read()
    {
        // As saved while the icon was a GDI+ image.
        const string json = """[{"Icon":null,"IsRegex":true,"Name":"Ticket","Text":"text"}]""";

        CommitTemplateItem[]? loaded = JsonSerializer.Deserialize<CommitTemplateItem[]>(json);

        loaded.Should().ContainSingle().Which.Name.Should().Be("Ticket");
        loaded![0].IsRegex.Should().BeTrue();
    }
}
