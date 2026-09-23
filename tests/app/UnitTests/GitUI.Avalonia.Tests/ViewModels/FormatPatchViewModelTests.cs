using GitUI.AvaloniaTests.Views;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUIPluginInterfaces;
using static GitUI.AvaloniaTests.ViewModels.ProcessViewModelTests;
using static GitUI.AvaloniaTests.ViewModels.SmallDialogViewModelTests;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the format patch dialog (phase 4).</summary>
[TestFixture]
public sealed class FormatPatchViewModelTests
{
    private List<GitRevision> _history = null!;
    private FakeFormatPatchHost _host = null!;
    private FakeMessageBoxes _messageBoxes = null!;
    private FormatPatchViewModel _viewModel = null!;

    [SetUp]
    public void SetUp()
    {
        _history = RevisionGridViewTests.CreateHistory();
        RevisionGridViewModel grid = new(new RevisionGridViewTests.FakeRevisionGridHost(_history), new RevisionGridDisplayOptions(false, false)) { MultiSelect = true };
        _host = new FakeFormatPatchHost();
        _messageBoxes = new FakeMessageBoxes();
        _viewModel = new FormatPatchViewModel(new FormatPatchStrings(), grid, @"C:\patches", "main", "Error", _host, _messageBoxes, new FakeFileDialogs());
        grid.Load();
    }

    [TearDown]
    public void TearDown() => _viewModel.Dispose();

    [Test]
    public void Requires_an_output_path_and_a_revision()
    {
        _viewModel.CurrentBranchText.Should().Be("Current branch: main");

        _viewModel.FormatPatchCommand.Execute(null);
        _messageBoxes.Errors.Should().Equal("You need to select at least one revision");

        _viewModel.OutputPath = "";
        _viewModel.FormatPatchCommand.Execute(null);
        _messageBoxes.Errors.Should().EndWith("You need to enter an output path.");
        _host.Calls.Should().BeEmpty();
        _host.Remembered.Should().Equal("");
    }

    [Test]
    public void Creates_the_patch_of_a_revision_or_of_the_range_between_two()
    {
        GitRevision fix = _history[4];
        Select(fix);
        _viewModel.FormatPatchCommand.Execute(null);
        _host.Calls.Should().Equal((fix.ParentIds![0].ToString(), fix.Guid, @"C:\patches", 0));

        _host.Calls.Clear();
        GitRevision head = _history[0];
        Select(head, fix);
        _viewModel.FormatPatchCommand.Execute(null);
        _host.Calls.Should().Equal((fix.ParentIds![0].ToString(), head.Guid, @"C:\patches", 0));
        _messageBoxes.Informations.Should().HaveCount(2);
    }

    [Test]
    public void Creates_a_numbered_patch_per_revision_of_more_than_two_oldest_first()
    {
        GitRevision[] selected = [_history[0], _history[2], _history[4]];
        Select(selected);
        bool? closed = null;
        _viewModel.CloseRequested += (_, accepted) => closed = accepted;

        _viewModel.FormatPatchCommand.Execute(null);

        _host.Calls.Select(c => (c.To, c.Start)).Should().Equal((_history[4].Guid, 1), (_history[2].Guid, 2), (_history[0].Guid, 3));
        closed.Should().BeTrue();
    }

    [Test]
    public void Reports_that_no_patch_was_created()
    {
        _host.Output = "";
        Select(_history[4]);

        _viewModel.FormatPatchCommand.Execute(null);

        _messageBoxes.Errors.Should().Equal("Unable to create patch file(s)");
    }

    private void Select(params GitRevision[] revisions)
        => _viewModel.Grid.SetSelectedRows(_viewModel.Grid.Rows.Where(r => revisions.Contains(r.Revision)));

    private sealed class FakeFormatPatchHost : IFormatPatchHost
    {
        public string Output { get; set; } = "0001-fix.patch";

        public List<(string From, string To, string OutputPath, int Start)> Calls { get; } = [];

        public List<string> Remembered { get; } = [];

        public string FormatPatch(string from, string to, string outputPath, int start)
        {
            Calls.Add((from, to, outputPath, start));
            return Output;
        }

        public void RememberOutputPath(string outputPath) => Remembered.Add(outputPath);
    }
}
