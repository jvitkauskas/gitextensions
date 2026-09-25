using GitExtensions.Extensibility.Git;
using GitUI.Presentation.CommandsDialogs.CommitDialog;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUIPluginInterfaces;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the commit dialog (port of <c>FormCommit</c>).</summary>
[TestFixture]
public sealed class CommitViewModelTests
{
    [Test]
    public async Task The_changes_are_split_into_unstaged_and_staged_files()
    {
        FakeHost host = new();
        (CommitViewModel viewModel, DiffViewModelTests.FakeViewerHost viewer) = Create(host);

        await viewModel.InitializeAsync();

        viewModel.Title.Should().Be("Commit to main (C:\\repo)");
        viewModel.BranchName.Should().Be("main \u2192");
        viewModel.PushTo.Should().Be("origin/main");
        Names(viewModel.Unstaged).Should().Equal("a.txt", "b.txt");
        Names(viewModel.Staged).Should().Equal("c.txt");
        viewModel.StagedCount.Should().Be("1/3");
        viewModel.CanCommit.Should().BeTrue();
        viewModel.HasChanges.Should().BeTrue();
        viewModel.Unstaged.SelectedEntry!.Item.Name.Should().Be("a.txt");
        viewer.Requested.Should().Equal("a.txt");
        viewModel.Message.Text.Should().Be("stored message");
        viewModel.Committer.Should().Be("Committer Alice <alice@example.org>");
    }

    [Test]
    public async Task Selecting_a_staged_file_clears_the_unstaged_selection()
    {
        (CommitViewModel viewModel, DiffViewModelTests.FakeViewerHost viewer) = Create(new FakeHost());
        await viewModel.InitializeAsync();

        viewModel.Staged.Select(e => e.Item.Name == "c.txt");

        viewModel.Unstaged.SelectedEntries.Should().BeEmpty();
        viewer.Requested[^1].Should().Be("c.txt");
    }

    [Test]
    public async Task Staging_moves_the_files_and_selects_the_next_one()
    {
        FakeHost host = new();
        (CommitViewModel viewModel, _) = Create(host);
        await viewModel.InitializeAsync();

        viewModel.StageCommand.Execute(null);

        host.Staged.Should().Equal("a.txt");
        Names(viewModel.Unstaged).Should().Equal("b.txt");
        Names(viewModel.Staged).Should().Equal("a.txt", "c.txt");
        viewModel.Unstaged.SelectedEntry!.Item.Name.Should().Be("b.txt");
        viewModel.StagedCount.Should().Be("2/3");

        viewModel.StageAllCommand.Execute(null);
        viewModel.Unstaged.AllEntries.Should().BeEmpty();
        viewModel.Staged.SelectedEntries.Should().NotBeEmpty("the staged files are selected once all are staged");
    }

    [Test]
    public async Task After_staging_only_the_next_unstaged_file_is_selected_and_shown()
    {
        // Each list then has a single file, which the list selects when set (without a selection event).
        FakeHost host = new();
        host.Index.Clear();
        (CommitViewModel viewModel, DiffViewModelTests.FakeViewerHost viewer) = Create(host);
        await viewModel.InitializeAsync();

        viewModel.StageCommand.Execute(null);

        viewModel.Unstaged.SelectedEntry!.Item.Name.Should().Be("b.txt");
        viewModel.Staged.SelectedEntries.Should().BeEmpty();
        viewer.Requested[^1].Should().Be("b.txt");
    }

    [Test]
    public async Task Unstaging_moves_the_files_back_and_splits_renames()
    {
        FakeHost host = new();
        host.Index.Add(new GitItemStatus("new.txt") { IsRenamed = true, OldName = "old.txt", IsTracked = true, Staged = StagedStatus.Index });
        (CommitViewModel viewModel, _) = Create(host);
        await viewModel.InitializeAsync();

        viewModel.Staged.Select(e => e.Item.Name == "new.txt");
        viewModel.UnstageCommand.Execute(null);

        host.Unstaged.Should().Equal("new.txt");
        Names(viewModel.Staged).Should().Equal("c.txt");
        Names(viewModel.Unstaged).Should().Equal("a.txt", "b.txt", "new.txt", "old.txt");
        viewModel.Unstaged.AllItems.Single(i => i.Name == "old.txt").IsDeleted.Should().BeTrue();
        viewModel.Unstaged.AllItems.Single(i => i.Name == "new.txt").IsNew.Should().BeTrue();

        viewModel.UnstageAllCommand.Execute(null);
        host.ResetMixed.Should().BeTrue();
    }

    [Test]
    public async Task A_commit_needs_a_message_and_then_commits_the_staged_files()
    {
        FakeHost host = new() { StoredMessage = "" };
        (CommitViewModel viewModel, _) = Create(host);
        await viewModel.InitializeAsync();

        viewModel.CommitCommand.Execute(null);
        host.Shown.Should().Equal("enter message");
        host.Commits.Should().BeEmpty();

        viewModel.Message.Text = "Fix the bug";
        viewModel.SignOff = true;
        viewModel.Author = "Bob <bob@example.org>";
        viewModel.CommitCommand.Execute(null);

        host.Commits.Should().Equal(new CommitRequest("Fix the bug", Amend: false, SignOff: true, "Bob <bob@example.org>", NoVerify: false, AllowEmpty: false, ResetAuthor: false, UsingCommitTemplate: false));
        viewModel.Message.Text.Should().BeEmpty();
        Names(viewModel.Staged).Should().BeEmpty("the staged files are reloaded after the commit");
    }

    [Test]
    public async Task The_message_is_validated()
    {
        FakeHost host = new() { Options = new CommitDialogOptions { MaxFirstLineLength = 10, SecondLineMustBeEmpty = true, ValidationRegex = "^JIRA-" } };
        (CommitViewModel viewModel, _) = Create(host);
        await viewModel.InitializeAsync();

        viewModel.Message.Text = "A too long first line\nsecond\nthird";
        viewModel.CommitCommand.Execute(null);

        host.Questions.Should().Equal(
            "First line of commit message contains too many characters.\nDo you want to continue?",
            "Second line of commit message is not empty.\nDo you want to continue?",
            "Commit message does not match RegEx.\nDo you want to continue?");
        host.Commits.Should().HaveCount(1, "every question was answered yes");

        host.AnswerValidation = false;
        viewModel.Message.Text = "A too long first line";
        viewModel.CommitCommand.Execute(null);
        host.Commits.Should().HaveCount(1);
    }

    [Test]
    public async Task Without_staged_files_the_unstaged_files_can_be_staged_and_committed()
    {
        FakeHost host = new() { NoStagedChoice = NoStagedFilesChoice.StageAllAndCommit };
        host.Index.Clear();
        (CommitViewModel viewModel, _) = Create(host);
        await viewModel.InitializeAsync();

        viewModel.CommitCommand.Execute(null);

        host.Staged.Should().Equal("a.txt", "b.txt");
        host.Commits.Should().ContainSingle().Which.AllowEmpty.Should().BeFalse();

        host.NoStagedChoice = NoStagedFilesChoice.EmptyCommit;
        viewModel.Message.Text = "Empty";
        viewModel.CommitCommand.Execute(null);
        host.Commits[^1].AllowEmpty.Should().BeTrue();
    }

    [Test]
    public async Task Without_changes_the_commit_and_push_button_pushes_only()
    {
        FakeHost host = new();
        host.WorkTree.Clear();
        host.Index.Clear();
        (CommitViewModel viewModel, _) = Create(host);
        await viewModel.InitializeAsync();

        // The text of the push button is a WinForms text (TranslatedStrings.ButtonPush): its mnemonic becomes an access key.
        viewModel.CommitAndPushText.Should().Be("_Push");
    }

    [Test]
    public async Task Amending_asks_first_and_starts_with_the_head_message()
    {
        FakeHost host = new() { StoredMessage = "", Options = new CommitDialogOptions { CommitAndPushForcedWhenAmend = true } };
        (CommitViewModel viewModel, _) = Create(host);
        await viewModel.InitializeAsync();
        viewModel.CommitAndPushText.Should().Be("Commit & _push");

        viewModel.Amend = true;

        viewModel.Message.Text.Should().Be("Head message");
        viewModel.CanResetSoft.Should().BeTrue();
        viewModel.IsPushForced.Should().BeTrue();
        viewModel.CommitAndPushText.Should().Be("Commit & force _push");

        viewModel.ResetAuthor = true;
        viewModel.CommitAndPushCommand.Execute(null);
        host.Shown.Should().Equal("confirm amend");
        host.Commits.Should().ContainSingle().Which.Should().Match<CommitRequest>(r => r.Amend && r.ResetAuthor);
        host.Pushes.Should().Equal(true);
        viewModel.Amend.Should().BeFalse();
    }

    [Test]
    public async Task A_fixup_commit_has_its_message_until_modified()
    {
        GitRevision edited = new(ObjectId.Parse("d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4d4")) { Subject = "Add the feature" };
        (CommitViewModel viewModel, _) = Create(new FakeHost(), CommitDialogKind.Fixup, edited);

        await viewModel.InitializeAsync();

        viewModel.Message.Text.Should().Be("fixup! Add the feature");
        viewModel.IsMessageEditable.Should().BeFalse();
        viewModel.ShowModifyMessageButton.Should().BeTrue();
        viewModel.ModifyCommitMessageCommand.Execute(null);
        viewModel.IsMessageEditable.Should().BeTrue();
    }

    [Test]
    public async Task Closing_keeps_the_message_and_a_commit_closes_if_set_so()
    {
        FakeHost host = new();
        (CommitViewModel viewModel, _) = Create(host);
        await viewModel.InitializeAsync();

        viewModel.CanClose().Should().BeTrue();
        host.Saved.Should().Equal(("stored message", false));

        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;
        viewModel.CloseDialogAfterEachCommit = true;
        host.Settings.CloseDialogAfterEachCommit.Should().BeTrue();
        viewModel.CommitCommand.Execute(null);
        closed.Should().BeTrue();
    }

    [Test]
    public async Task Previous_messages_replace_the_message()
    {
        FakeHost host = new();
        (CommitViewModel viewModel, _) = Create(host);
        await viewModel.InitializeAsync();

        viewModel.GetPreviousMessages().Should().Equal("Last message", "Earlier message");
        viewModel.ShowOnlyMyMessages = true;
        viewModel.GetPreviousMessages().Should().Equal("Mine");
        viewModel.UsePreviousMessage(" Earlier message \n");
        viewModel.Message.Text.Should().Be("Earlier message");
    }

    [Test]
    public async Task Hotkeys_stage_all_and_move_the_selection_in_a_loop()
    {
        FakeHost host = new();
        (CommitViewModel viewModel, DiffViewModelTests.FakeViewerHost viewer) = Create(host);
        await viewModel.InitializeAsync();

        viewModel.ExecuteHotkeyCommand((int)CommitHotkeyCommand.SelectNext).Should().BeTrue();
        viewModel.Unstaged.SelectedEntry!.Item.Name.Should().Be("b.txt");
        viewModel.ExecuteHotkeyCommand((int)CommitHotkeyCommand.SelectNext_AlternativeHotkey1).Should().BeTrue();
        viewModel.Unstaged.SelectedEntry!.Item.Name.Should().Be("a.txt", "the selection loops to the first file");
        viewModel.ExecuteHotkeyCommand((int)CommitHotkeyCommand.SelectPrevious_AlternativeHotkey2).Should().BeTrue();
        viewModel.Unstaged.SelectedEntry!.Item.Name.Should().Be("b.txt", "and back to the last one");
        viewer.Requested.Should().Equal("a.txt", "b.txt", "a.txt", "b.txt");

        viewModel.MoveSelection(backwards: false, messageFocused: true);
        viewModel.Staged.SelectedEntry!.Item.Name.Should().Be("c.txt", "from the message, the staged files are selected");
        viewModel.Unstaged.SelectedEntries.Should().BeEmpty();

        viewModel.ExecuteHotkeyCommand((int)CommitHotkeyCommand.StageAll).Should().BeTrue();
        host.Staged.Should().Equal("a.txt", "b.txt");
        viewModel.ExecuteHotkeyCommand((int)CommitHotkeyCommand.StageAll).Should().BeFalse("nothing is left to stage");
        viewModel.ExecuteHotkeyCommand((int)CommitHotkeyCommand.FocusCommitMessage).Should().BeFalse("the view moves the focus");
    }

    [Test]
    public async Task The_gpg_signing_is_passed_to_the_commit()
    {
        // The later commits are empty: the first one commits the staged file.
        FakeHost host = new() { NoStagedChoice = NoStagedFilesChoice.EmptyCommit };
        (CommitViewModel viewModel, _) = Create(host);
        await viewModel.InitializeAsync();

        viewModel.GpgSignIndex.Should().Be(0);
        viewModel.IsGpgSignSelected.Should().BeFalse();
        viewModel.GpgSignIndex = 3;
        viewModel.IsGpgKeyVisible.Should().BeTrue();
        viewModel.GpgKeyId = "ABCD1234";
        viewModel.CommitCommand.Execute(null);
        viewModel.GpgSignIndex = 2;
        viewModel.IsGpgKeyVisible.Should().BeFalse();
        viewModel.Message.Text = "Second";
        viewModel.CommitCommand.Execute(null);
        viewModel.GpgSignIndex = 1;
        viewModel.Message.Text = "Third";
        viewModel.CommitCommand.Execute(null);

        host.Commits.Select(c => (c.GpgSign, c.GpgKeyId)).Should().Equal((true, "ABCD1234"), (true, ""), (false, ""));
        viewModel.IsGpgSignSelected.Should().BeTrue("as in FormCommit, not signing also shows the key");
    }

    [Test]
    public async Task The_selection_filter_selects_the_matching_unstaged_files_and_remembers_the_filter()
    {
        FakeHost host = new() { Options = new CommitDialogOptions { ShowSelectionFilter = true } };
        (CommitViewModel viewModel, _) = Create(host);
        await viewModel.InitializeAsync();
        viewModel.IsSelectionFilterVisible.Should().BeTrue();

        viewModel.SelectionFilter = "^B";
        viewModel.ApplySelectionFilter();
        viewModel.Unstaged.SelectedEntries.Select(e => e.Item.Name).Should().Equal("b.txt");
        viewModel.SelectionFilterHistory.Should().Equal("^B");

        viewModel.SelectionFilter = "(";
        viewModel.ApplySelectionFilter();
        viewModel.SelectionFilterToolTip.Should().StartWith("Error ");
        viewModel.SelectionFilter = "zzz";
        viewModel.ApplySelectionFilter();
        viewModel.SelectionFilterToolTip.Should().Be("Enter a regular expression to select unstaged files.");
        viewModel.SelectionFilterHistory.Should().Equal(["^B"], "filters that select nothing are not remembered");

        for (int i = 0; i < 12; i++)
        {
            viewModel.SelectionFilter = $"txt|{i}";
            viewModel.ApplySelectionFilter();
        }

        viewModel.SelectionFilterHistory.Should().HaveCount(10).And.StartWith("txt|11");
        viewModel.Unstaged.SelectedEntries.Should().HaveCount(2);
    }

    [Test]
    public async Task Merge_conflicts_block_the_commit()
    {
        FakeHost host = new() { Conflicts = true };
        (CommitViewModel viewModel, _) = Create(host);
        await viewModel.InitializeAsync();

        viewModel.HasMergeConflicts.Should().BeTrue();
        viewModel.CommitCommand.Execute(null);

        host.Shown.Should().Equal("merge conflicts");
        host.Commits.Should().BeEmpty();
    }

    internal static (CommitViewModel ViewModel, DiffViewModelTests.FakeViewerHost Viewer) Create(FakeHost host, CommitDialogKind kind = CommitDialogKind.Normal, GitRevision? editedCommit = null)
    {
        DiffViewModelTests.FakeViewerHost viewer = new();
        CommitViewModel viewModel = new(new CommitStrings(), host, viewer, new FileStatusListStrings(), new FileStatusTreeOptions(), kind, editedCommit);
        return (viewModel, viewer);
    }

    private static IEnumerable<string> Names(FileStatusListViewModel list) => list.AllEntries.Select(e => e.Item.Name).Order();

    internal sealed class FakeSettings : ICommitDialogSettings
    {
        public bool CloseDialogAfterEachCommit { get; set; }

        public bool CloseDialogAfterAllFilesCommitted { get; set; }

        public bool RefreshDialogOnFormFocus { get; set; }

        public bool SelectStagedOnEnterMessage { get; set; }

        public bool ShowOnlyMyMessages { get; set; }

        public bool StageInSuperproject { get; set; }
    }

    /// <summary>A repository with two changed files and a staged one; staging moves them into the index.</summary>
    internal sealed class FakeHost : ICommitHost
    {
        public List<GitItemStatus> WorkTree { get; } =
        [
            new("a.txt") { IsChanged = true, IsTracked = true, Staged = StagedStatus.WorkTree },
            new("b.txt") { IsNew = true, Staged = StagedStatus.WorkTree },
        ];

        public List<GitItemStatus> Index { get; } =
        [
            new("c.txt") { IsChanged = true, IsTracked = true, Staged = StagedStatus.Index },
        ];

        public List<string> Staged { get; } = [];

        public List<string> Unstaged { get; } = [];

        public bool ResetMixed { get; private set; }

        public List<CommitRequest> Commits { get; } = [];

        public List<bool> Pushes { get; } = [];

        public List<string> Shown { get; } = [];

        public List<string> Questions { get; } = [];

        public List<(string Message, bool Amend)> Saved { get; } = [];

        public string StoredMessage { get; init; } = "stored message";

        public bool AnswerValidation { get; set; } = true;

        public bool Conflicts { get; init; }

        public NoStagedFilesChoice NoStagedChoice { get; set; } = NoStagedFilesChoice.Cancel;

        public CommitDialogOptions Options { get; init; } = new();

        public ICommitDialogSettings Settings { get; } = new FakeSettings();

        public string WorkingDirectory => @"C:\repo";

        public string PushText => "&Push";

        public bool IsBareRepository => false;

        public bool HasSuperproject => false;

        public bool IsMergeCommit => false;

        public bool InTheMiddleOfConflictedMerge() => Conflicts;

        public bool CanResetSoft() => true;

        public Task<IReadOnlyList<GitItemStatus>> GetAllChangedFilesAsync(FileStatusFileOptions options, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<GitItemStatus>>([.. WorkTree, .. Index]);

        public IReadOnlyList<GitItemStatus> GetIndexFiles() => [.. Index];

        public (GitRevision? Head, GitRevision Index, GitRevision WorkTree) GetHeadRevisions()
        {
            ObjectId head = ObjectId.Parse("a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1");
            return (new GitRevision(head), new GitRevision(ObjectId.IndexId) { ParentIds = [head] }, new GitRevision(ObjectId.WorkTreeId) { ParentIds = [ObjectId.IndexId] });
        }

        public void UpdateSubmoduleStatus(IReadOnlyList<GitItemStatus> items)
        {
        }

        public Task<CommitBranchInfo> GetBranchInfoAsync() => Task.FromResult(new CommitBranchInfo("main", "origin/main"));

        public Task<string> GetCommitterAsync(string author)
            => Task.FromResult(string.IsNullOrEmpty(author) ? "Committer Alice <alice@example.org>" : $"Committer Alice <alice@example.org> Author {author}");

        public bool StageFiles(IReadOnlyList<GitItemStatus> files)
        {
            foreach (GitItemStatus file in files)
            {
                Staged.Add(file.Name);
                WorkTree.RemoveAll(f => f.Name == file.Name);
                Index.Add(new GitItemStatus(file.Name) { IsChanged = file.IsChanged, IsNew = file.IsNew, IsTracked = true, Staged = StagedStatus.Index });
            }

            return true;
        }

        public bool UnstageFiles(IReadOnlyList<GitItemStatus> files)
        {
            foreach (GitItemStatus file in files)
            {
                Unstaged.Add(file.Name);
                Index.RemoveAll(f => f.Name == file.Name);
            }

            return false;
        }

        public void UnstageAll()
        {
            ResetMixed = true;
            WorkTree.AddRange(Index.Select(f => new GitItemStatus(f.Name) { IsChanged = true, IsTracked = true, Staged = StagedStatus.WorkTree }));
            Index.Clear();
        }

        public Task<(string Message, bool Amend)> LoadCommitMessageAsync() => Task.FromResult((StoredMessage, false));

        public string? LoadCommitTemplate() => null;

        public Task SaveCommitMessageAsync(string message, bool amend)
        {
            Saved.Add((message, amend));
            return Task.CompletedTask;
        }

        public IReadOnlyList<string> GetPreviousMessages(bool onlyMine) => onlyMine ? ["Mine"] : ["Last message", "Earlier message"];

        public string? GetHeadMessage() => "Head message\n";

        public string Branch { get; init; } = "main";

        public (IReadOnlyList<GitCommands.CommitTemplateItem> Registered, IReadOnlyList<GitCommands.CommitTemplateItem> FromSettings) Templates { get; init; } = ([], []);

        public (IReadOnlyList<GitCommands.CommitTemplateItem> Registered, IReadOnlyList<GitCommands.CommitTemplateItem> FromSettings) GetCommitTemplates() => Templates;

        /// <summary>The icons of the templates, by name.</summary>
        public Dictionary<string, byte[]> TemplateIcons { get; } = [];

        public byte[]? GetTemplateIcon(GitCommands.CommitTemplateItem template) => TemplateIcons.GetValueOrDefault(template.Name);

        /// <summary>The message of the changes in the submodules, which the staged files are logged for.</summary>
        public string? SubmodulesChangesMessage { get; set; }

        public string? GetListOfChangesInSubmodules(IReadOnlyList<GitItemStatus> stagedFiles)
        {
            Shown.Add($"submodules of {string.Join(", ", stagedFiles.Select(f => f.Name))}");
            return SubmodulesChangesMessage;
        }

        public string GetCurrentBranch() => Branch;

        public void EditCommitTemplateSettings() => Shown.Add("template settings");

        public void OpenUrl(string url) => Shown.Add(url);

        public bool ConfirmAmend()
        {
            Shown.Add("confirm amend");
            return true;
        }

        public bool ConfirmEmptyMergeCommit() => true;

        public NoStagedFilesChoice AskWithoutStagedFiles(bool filterActive, bool hasUnstagedFiles) => NoStagedChoice;

        public void ShowMergeConflicts() => Shown.Add("merge conflicts");

        public void ShowEnterCommitMessage() => Shown.Add("enter message");

        public bool ConfirmValidation(string text)
        {
            Questions.Add(text);
            return AnswerValidation;
        }

        public bool ConfirmDetachedHeadCommit(ObjectId? editedCommit) => true;

        public bool Commit(CommitRequest request)
        {
            Commits.Add(request);
            Index.Clear();
            return true;
        }

        public bool Push(bool forced)
        {
            Pushes.Add(forced);
            return true;
        }

        public void StageInSuperprojectNow()
        {
        }

        public bool ConfirmResetSoft() => true;

        public void ResetSoft() => Shown.Add("reset soft");

        public bool ResolveConflicts() => true;

        public void ResetChanges(IReadOnlyList<GitItemStatus> unstagedFiles, bool onlyWorkTree) => Shown.Add($"reset {unstagedFiles.Count} {onlyWorkTree}");

        public void StashStaged() => Shown.Add("stash staged");

        public bool CreateBranch() => true;

        public void EditCommitterSettings() => Shown.Add("committer");

        public void NotifyRepositoryChanged()
        {
        }

        public void ShowError(string message) => Shown.Add(message);
    }
}
