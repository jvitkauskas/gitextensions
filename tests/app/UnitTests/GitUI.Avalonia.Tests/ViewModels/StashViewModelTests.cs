using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Editor;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUIPluginInterfaces;
using static GitUI.AvaloniaTests.ViewModels.FileStatusListViewModelTests;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the stash dialog (port of <c>FormStash</c>).</summary>
[TestFixture]
public sealed class StashViewModelTests
{
    [Test]
    public void Managing_stashes_selects_the_newest_stash_and_its_files()
    {
        FakeHost host = new() { Stashes = [new(0, "WIP on master: newest"), new(1, "WIP on master: older")] };
        StashViewModel viewModel = Create(host, manageStashes: true);

        viewModel.InitializeView();

        viewModel.Stashes.Select(s => s.Summary).Should().Equal("Current working directory changes", "@{0}: WIP on master: newest", "@{1}: WIP on master: older");
        viewModel.SelectedStash!.Name.Should().Be("stash@{0}");
        viewModel.Message.Should().Be("WIP on master: newest");
        viewModel.IsMessageReadOnly.Should().BeTrue();
        viewModel.CanApplyOrDrop.Should().BeTrue();
        viewModel.CanStashSelected.Should().BeFalse("only for the working directory");
        host.FilesOf.Should().Equal("stash@{0}");
        viewModel.Files.AllEntries.Should().HaveCount(4);

        viewModel.RefreshCommand.Execute(null);
        viewModel.SelectedStash!.Index.Should().Be(-1, "the stashes are managed only on the first load");
    }

    [Test]
    public void Without_stashes_the_working_directory_is_shown_and_can_be_stashed()
    {
        FakeHost host = new();
        StashViewModel viewModel = Create(host, manageStashes: true);

        viewModel.InitializeView();

        viewModel.SelectedStash!.Index.Should().Be(-1);
        viewModel.Message.Should().BeEmpty();
        viewModel.MessagePlaceholder.Should().Be("There are no stashes.", "shown in the empty message, not saved as the message");
        viewModel.CanApplyOrDrop.Should().BeFalse();
        host.FilesOf.Should().Equal("working directory");

        viewModel.Message = "  my work  ";
        viewModel.Files.Select(e => e.Item.Name == "src/Program.cs");
        viewModel.CanStashSelected.Should().BeTrue();
        viewModel.StashSelectedCommand.Execute(null);
        viewModel.StashAllCommand.Execute(null);

        host.Log.Should().Equal("save untracked=True keep=False ' my work' files=src/Program.cs", "save untracked=True keep=False '' files=");
    }

    [Test]
    public void Initial_stash_is_selected_and_dropping_selects_the_next_one()
    {
        FakeHost host = new() { Stashes = [new(0, "first"), new(1, "second"), new(2, "third")] };
        StashViewModel viewModel = Create(host, manageStashes: false, initialStash: "stash@{2}");
        viewModel.InitializeView();
        viewModel.SelectedStash!.Name.Should().Be("stash@{2}");

        host.ConfirmResult = false;
        viewModel.DropCommand.Execute(null);
        host.Log.Should().BeEmpty("the drop was not confirmed");

        host.ConfirmResult = true;
        viewModel.DropCommand.Execute(null);
        host.Log.Should().Equal("drop stash@{2}");
        viewModel.SelectedStash!.Name.Should().Be("stash@{1}", "the last stash was dropped, the one before is selected");

        viewModel.ApplyCommand.Execute(null);
        host.Log.Should().EndWith("apply stash@{1}");
    }

    [Test]
    public void Next_and_previous_stash_and_the_settings()
    {
        FakeHost host = new() { Stashes = [new(0, "first")], Settings = (true, false) };
        StashViewModel viewModel = Create(host, manageStashes: false);
        viewModel.InitializeView();
        viewModel.KeepIndex.Should().BeTrue();
        viewModel.IncludeUntrackedFiles.Should().BeFalse();

        viewModel.SelectNextStash(next: false).Should().BeTrue();
        viewModel.SelectedStash!.Name.Should().Be("stash@{0}");
        viewModel.SelectNextStash(next: false).Should().BeFalse("it is the oldest");
        viewModel.SelectNextStash(next: true).Should().BeTrue();
        viewModel.SelectedStash!.Index.Should().Be(-1);

        viewModel.IncludeUntrackedFiles = true;
        viewModel.CanClose().Should().BeTrue();
        host.Settings.Should().Be((true, true));
    }

    [Test]
    public void The_hotkeys_select_the_next_and_previous_stash_and_refresh()
    {
        FakeHost host = new() { Stashes = [new(0, "first"), new(1, "second")] };
        StashViewModel viewModel = Create(host, manageStashes: false);
        viewModel.InitializeView();
        viewModel.SelectedStash = viewModel.Stashes[0];

        // As FormStash.ExecuteCommand: newer stashes first in the list.
        viewModel.ExecuteHotkeyCommand((int)StashHotkeyCommand.PreviousStash).Should().BeTrue();
        viewModel.SelectedStash.Should().BeSameAs(viewModel.Stashes[1]);
        viewModel.ExecuteHotkeyCommand((int)StashHotkeyCommand.NextStash).Should().BeTrue();
        viewModel.SelectedStash.Should().BeSameAs(viewModel.Stashes[0]);
        viewModel.ExecuteHotkeyCommand((int)StashHotkeyCommand.NextStash).Should().BeFalse("it is the newest");

        viewModel.ExecuteHotkeyCommand((int)StashHotkeyCommand.Refresh).Should().BeTrue();
        viewModel.Stashes.Should().NotBeEmpty();
        viewModel.ExecuteHotkeyCommand(99).Should().BeFalse();

        // As Stashed.BindContextMenu(View.CherryPickAllChanges): the files can cherry-pick.
        viewModel.Files.CherryPickChangesAction.Should().NotBeNull();
    }

    private static StashViewModel Create(FakeHost host, bool manageStashes, string? initialStash = null)
        => new(new StashStrings(), host, new DiffViewModelTests.FakeViewerHost(), new FileStatusListStrings(), new FileStatusTreeOptions(), manageStashes, initialStash);

    internal sealed class FakeHost : IStashHost
    {
        public List<GitStash> Stashes { get; set; } = [];

        public List<string> Log { get; } = [];

        public List<string> FilesOf { get; } = [];

        public bool ConfirmResult { get; set; } = true;

        public (bool KeepIndex, bool IncludeUntrackedFiles) Settings { get; set; } = (false, true);

        public IReadOnlyList<GitStash> GetStashes() => Stashes;

        public Task<IReadOnlyList<FileStatusGroup>> GetFilesAsync(GitStash? stash, CancellationToken cancellationToken)
        {
            FilesOf.Add(stash?.Name ?? "working directory");
            return Task.FromResult<IReadOnlyList<FileStatusGroup>>([new FileStatusGroup(First, Second, "", CreateStatuses())]);
        }

        public void Save(bool includeUntrackedFiles, bool keepIndex, string message, IReadOnlyList<string>? files)
            => Log.Add($"save untracked={includeUntrackedFiles} keep={keepIndex} '{message}' files={string.Join(", ", files ?? [])}");

        public bool ConfirmDrop() => ConfirmResult;

        public void Drop(string stashName)
        {
            Log.Add($"drop {stashName}");
            Stashes.RemoveAt(Stashes.Count - 1);
        }

        public void Apply(string stashName) => Log.Add($"apply {stashName}");

        public (bool KeepIndex, bool IncludeUntrackedFiles) LoadSettings() => Settings;

        public void SaveSettings(bool keepIndex, bool includeUntrackedFiles) => Settings = (keepIndex, includeUntrackedFiles);
    }
}
