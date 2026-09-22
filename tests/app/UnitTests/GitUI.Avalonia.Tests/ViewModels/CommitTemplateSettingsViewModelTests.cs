using GitUI.Presentation.CommandsDialogs.CommitDialog;

namespace GitUI.AvaloniaTests.ViewModels;

[TestFixture]
public sealed class CommitTemplateSettingsViewModelTests
{
    [Test]
    public void Without_stored_templates_offers_empty_slots_and_selects_the_first()
    {
        CommitTemplateSettingsViewModel viewModel = CreateViewModel(new CommitMessageSettings());

        viewModel.Templates.Should().HaveCount(CommitTemplateSettingsViewModel.TemplateSlotCount);
        viewModel.Templates.Should().OnlyContain(t => t.Name == "" && t.Text == "" && !t.IsRegex);
        viewModel.SelectedTemplate.Should().BeSameAs(viewModel.Templates[0]);
    }

    [Test]
    public void Pads_fewer_stored_templates_and_keeps_more()
    {
        CreateViewModel(new CommitMessageSettings { Templates = [new CommitTemplate("Fix", "fix: ", false)] })
            .Templates.Should().HaveCount(CommitTemplateSettingsViewModel.TemplateSlotCount);

        CommitTemplate[] twelve = [.. Enumerable.Range(1, 12).Select(i => new CommitTemplate($"T{i}", "", false))];
        CreateViewModel(new CommitMessageSettings { Templates = twelve })
            .Templates.Should().HaveCount(12);
    }

    [TestCase("", "1 : <empty>")]
    [TestCase("Bug fix", "1 : Bug fix")]
    [TestCase("12345678901234567890123456789012345678901234567890", "1 : 12345678901234567890123456789012345678901234567890")]
    [TestCase("123456789012345678901234567890123456789012345678901", "1 : 12345678901234567890123456789012345678901234567...")]
    public void DisplayName_matches_the_WinForms_dialog(string name, string expected)
    {
        new CommitTemplateSlotViewModel(0, new CommitTemplate(name, "", false)).DisplayName.Should().Be(expected);
    }

    [Test]
    public void DisplayName_uses_the_translated_placeholder()
    {
        new CommitTemplateSlotViewModel(3, CommitTemplate.Empty, emptyText: "leer").DisplayName.Should().Be("4 : <leer>");
    }

    [Test]
    public void Save_persists_all_values_and_closes_accepted()
    {
        InMemoryStore store = new(new CommitMessageSettings());
        CommitTemplateSettingsViewModel viewModel = new(new CommitTemplateSettingsStrings(), store);
        bool? accepted = null;
        viewModel.CloseRequested += (_, result) => accepted = result;

        viewModel.SelectedTemplate = viewModel.Templates[2];
        viewModel.SelectedTemplate.Name = "Ticket";
        viewModel.SelectedTemplate.IsRegex = true;
        viewModel.MaxFirstLineLength = 72;
        viewModel.AutoWrap = false;
        viewModel.ValidationRegex = "^[A-Z]+-\\d+";
        viewModel.SaveCommand.Execute(null);

        accepted.Should().BeTrue();
        store.Saved.Should().NotBeNull();
        store.Saved!.MaxFirstLineLength.Should().Be(72);
        store.Saved.AutoWrap.Should().BeFalse();
        store.Saved.ValidationRegex.Should().Be("^[A-Z]+-\\d+");
        store.Saved.Templates![2].Should().Be(new CommitTemplate("Ticket", "", true));
    }

    [Test]
    public void Cancel_does_not_save()
    {
        InMemoryStore store = new(new CommitMessageSettings());
        CommitTemplateSettingsViewModel viewModel = new(new CommitTemplateSettingsStrings(), store);
        bool? accepted = null;
        viewModel.CloseRequested += (_, result) => accepted = result;

        viewModel.MaxLineLength = 50;
        viewModel.CancelCommand.Execute(null);

        accepted.Should().BeFalse();
        store.Saved.Should().BeNull();
    }

    private static CommitTemplateSettingsViewModel CreateViewModel(CommitMessageSettings settings)
        => new(new CommitTemplateSettingsStrings(), new InMemoryStore(settings));

    internal sealed class InMemoryStore(CommitMessageSettings settings) : ICommitMessageSettingsStore
    {
        public CommitMessageSettings? Saved { get; private set; }

        public CommitMessageSettings Load() => settings;

        public void Save(CommitMessageSettings settings) => Saved = settings;
    }
}
