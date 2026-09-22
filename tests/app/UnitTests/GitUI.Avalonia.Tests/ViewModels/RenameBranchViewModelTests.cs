using GitCommands.Git;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.AvaloniaTests.ViewModels;

[TestFixture]
public sealed class RenameBranchViewModelTests
{
    private readonly List<(string OldName, string NewName)> _renames = [];
    private bool _renameSucceeds = true;

    [SetUp]
    public void SetUp()
    {
        _renames.Clear();
        _renameSucceeds = true;
    }

    [Test]
    public void Starts_with_the_old_name()
    {
        CreateViewModel("feature/old").NewName.Should().Be("feature/old");
    }

    [Test]
    public void Rename_renames_and_closes_accepted()
    {
        RenameBranchViewModel viewModel = CreateViewModel("feature/old");
        bool? accepted = null;
        viewModel.CloseRequested += (_, result) => accepted = result;

        viewModel.NewName = "feature/new";
        viewModel.RenameCommand.Execute(null);

        _renames.Should().Equal(("feature/old", "feature/new"));
        accepted.Should().BeTrue();
    }

    [Test]
    public void Rename_to_the_same_name_closes_without_renaming()
    {
        RenameBranchViewModel viewModel = CreateViewModel("feature/old");
        bool? accepted = null;
        viewModel.CloseRequested += (_, result) => accepted = result;

        viewModel.RenameCommand.Execute(null);

        _renames.Should().BeEmpty();
        accepted.Should().BeFalse();
    }

    [Test]
    public void Failed_rename_keeps_the_dialog_open()
    {
        _renameSucceeds = false;
        RenameBranchViewModel viewModel = CreateViewModel("feature/old");
        bool closeRequested = false;
        viewModel.CloseRequested += (_, _) => closeRequested = true;

        viewModel.NewName = "feature/taken";
        viewModel.RenameCommand.Execute(null);

        _renames.Should().ContainSingle();
        closeRequested.Should().BeFalse();
    }

    [Test]
    public void Rename_normalises_the_name_first()
    {
        RenameBranchViewModel viewModel = CreateViewModel("feature/old");

        viewModel.NewName = "feature/new name";
        viewModel.RenameCommand.Execute(null);

        _renames.Should().Equal(("feature/old", "feature/new_name"));
        viewModel.NewName.Should().Be("feature/new_name");
    }

    [Test]
    public void Normalisation_can_be_disabled()
    {
        RenameBranchViewModel viewModel = CreateViewModel("feature/old", autoNormalise: false);

        viewModel.NewName = "feature/new name";
        viewModel.NormaliseNewName();

        viewModel.NewName.Should().Be("feature/new name");
    }

    private RenameBranchViewModel CreateViewModel(string oldName, bool autoNormalise = true)
        => new(
            new RenameBranchStrings(),
            oldName,
            new FakeBranchNameNormaliser(),
            new GitBranchNameOptions("_"),
            autoNormalise,
            (from, to) =>
            {
                _renames.Add((from, to));
                return _renameSucceeds;
            });
}
