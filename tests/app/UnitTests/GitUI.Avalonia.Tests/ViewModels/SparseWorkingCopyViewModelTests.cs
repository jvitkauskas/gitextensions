using GitUI.Presentation.CommandsDialogs;
using static GitUI.AvaloniaTests.ViewModels.ProcessViewModelTests;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the sparse working copy dialog (port of <c>FormSparseWorkingCopy</c>).</summary>
[TestFixture]
public sealed class SparseWorkingCopyViewModelTests
{
    [Test]
    public void Loads_the_state_and_the_rules()
    {
        FakeHost host = new() { Enabled = true, Rules = "/src/\n" };

        SparseWorkingCopyViewModel viewModel = Create(host, new FakeMessageBoxes());

        viewModel.IsSparseCheckoutEnabled.Should().BeTrue();
        viewModel.IsRefreshWorkingCopyOnSave.Should().BeTrue("the index bitmap is not updated otherwise");
        viewModel.Rules.Text.Should().Be("/src/\n");
        viewModel.IsWithUnsavedChanges.Should().BeFalse();
        viewModel.EnableToolTip.Should().Be("Sets the Git property “core.sparsecheckout” to True for the local repository.");
        viewModel.RefreshWorkingCopyToolTip.Should().EndWith("read-tree -m -u HEAD");
    }

    [Test]
    public void Save_writes_only_changes_refreshes_the_working_copy_and_closes()
    {
        FakeHost host = new();
        SparseWorkingCopyViewModel viewModel = Create(host, new FakeMessageBoxes());
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.EnableCommand.Execute(null);
        viewModel.Rules.Text = "/docs/";
        viewModel.IsWithUnsavedChanges.Should().BeTrue();
        viewModel.SaveCommand.Execute(null);

        host.Log.Should().Equal("enabled: True", "rules: /docs/", "refresh");
        closed.Should().BeTrue();
        viewModel.CanClose().Should().BeTrue();
    }

    [Test]
    public void Save_without_changes_still_refreshes_unless_unchecked()
    {
        FakeHost host = new() { Enabled = true, Rules = "/*" };
        SparseWorkingCopyViewModel viewModel = Create(host, new FakeMessageBoxes());

        viewModel.SaveCommand.Execute(null);
        host.Log.Should().Equal("refresh");

        host.Log.Clear();
        viewModel = Create(host, new FakeMessageBoxes());
        viewModel.IsRefreshWorkingCopyOnSave = false;
        viewModel.SaveCommand.Execute(null);
        host.Log.Should().BeEmpty();
    }

    [TestCase(true, true, 1)]
    [TestCase(false, true, 0)]
    [TestCase(null, false, 0)]
    public void Cancelling_asks_to_save_unsaved_changes(bool? answer, bool closes, int saves)
    {
        FakeHost host = new();
        FakeMessageBoxes messageBoxes = new() { ConfirmWithCancelResult = answer };
        SparseWorkingCopyViewModel viewModel = Create(host, messageBoxes);
        viewModel.CanClose().Should().BeTrue("there is nothing to save");
        viewModel.IsRefreshWorkingCopyOnSave = false;

        viewModel.EnableCommand.Execute(null);
        viewModel.CanClose().Should().Be(closes);

        messageBoxes.Confirmations.Should().ContainSingle().Which.Should().StartWith("You have made changes to settings or rules.");
        host.Log.Should().HaveCount(saves);
    }

    [TestCase(true, "/*\r\n#/src/\r\n# comment")]
    [TestCase(false, "/src/\n# comment")]
    public void Disabling_offers_to_let_everything_pass_the_rules(bool accept, string savedRules)
    {
        FakeHost host = new() { Enabled = true, Rules = "/src/\n# comment" };
        FakeMessageBoxes messageBoxes = new() { ConfirmResult = accept };
        SparseWorkingCopyViewModel viewModel = Create(host, messageBoxes);
        viewModel.IsRefreshWorkingCopyOnSave = false;

        viewModel.DisableCommand.Execute(null);
        viewModel.SaveCommand.Execute(null);

        messageBoxes.Confirmations.Should().ContainSingle().Which.Should().Contain("with some rules still in the sparse pass-filter");
        host.Log.Should().Equal(accept ? ["enabled: False", $"rules: {savedRules.ReplaceLineEndings()}"] : ["enabled: False"]);
    }

    [Test]
    public void Disabling_with_a_pass_all_rule_does_not_ask()
    {
        FakeHost host = new() { Enabled = true, Rules = "# everything\n/*\n" };
        FakeMessageBoxes messageBoxes = new();
        SparseWorkingCopyViewModel viewModel = Create(host, messageBoxes);
        viewModel.IsRefreshWorkingCopyOnSave = false;

        viewModel.DisableCommand.Execute(null);
        viewModel.SaveCommand.Execute(null);

        messageBoxes.Confirmations.Should().BeEmpty();
        host.Log.Should().Equal("enabled: False");
    }

    [Test]
    public void Reports_failures_to_load_and_to_save()
    {
        FakeHost host = new() { LoadFailure = new IOException("Locked"), SaveFailure = new IOException("Denied") };
        FakeMessageBoxes messageBoxes = new();
        SparseWorkingCopyViewModel viewModel = Create(host, messageBoxes);
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.Rules.Text = "/src/";
        viewModel.SaveCommand.Execute(null);

        messageBoxes.Errors.Should().HaveCount(2);
        messageBoxes.Errors[0].Should().StartWith("Cannot load the text of the sparse file.").And.EndWith("Locked");
        messageBoxes.Errors[1].Should().StartWith("Could not save the modified settings and rules.").And.EndWith("Denied");
        closed.Should().BeTrue("the dialog closes even if saving failed, as FormSparseWorkingCopy");
        viewModel.CanClose().Should().BeTrue();
    }

    private static SparseWorkingCopyViewModel Create(FakeHost host, FakeMessageBoxes messageBoxes)
        => new(new SparseWorkingCopyStrings(), host, messageBoxes);

    internal sealed class FakeHost : ISparseWorkingCopyHost
    {
        public bool Enabled { get; set; }

        public string? Rules { get; set; }

        public Exception? LoadFailure { get; init; }

        public Exception? SaveFailure { get; init; }

        public List<string> Log { get; } = [];

        public bool IsSparseCheckoutEnabled => Enabled;

        public void SetSparseCheckoutEnabled(bool enabled)
        {
            Enabled = enabled;
            Log.Add($"enabled: {enabled}");
        }

        public string? LoadRules() => LoadFailure is null ? Rules : throw LoadFailure;

        public void SaveRules(string text)
        {
            if (SaveFailure is not null)
            {
                throw SaveFailure;
            }

            Rules = text;
            Log.Add($"rules: {text}");
        }

        public void RefreshWorkingCopy() => Log.Add("refresh");
    }
}
