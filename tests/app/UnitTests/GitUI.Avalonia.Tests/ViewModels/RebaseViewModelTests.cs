using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.UserControls;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the rebase and apply patch dialogs (ports of <c>FormRebase</c>, <c>FormApplyPatch</c> and <c>PatchGrid</c>).</summary>
[TestFixture]
public sealed class RebaseViewModelTests
{
    /// <summary>The start of the rebase commands (<c>GitCommandConfiguration.Default</c> overrides <c>rebase.autosquash</c> for them).</summary>
    private const string GitRebase = "-c rebase.autosquash=false rebase";

    private static readonly ObjectId _commit1 = ObjectId.Parse("a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1");
    private static readonly ObjectId _commit2 = ObjectId.Parse("b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2");

    [Test]
    public void Rebase_lists_the_refs_and_the_git_settings_when_shown()
    {
        FakeRebaseHost host = new() { AutoSquash = true, UpdateRefsSetting = true, RebaseAutoStash = true, IsDirty = true };
        RebaseViewModel viewModel = CreateRebase(host, Options(defaultBranch: "origin/main"));

        viewModel.InitializeView();

        viewModel.CurrentBranch.Should().Be("feature");
        viewModel.Branches.Should().Equal("main", "feature", "origin/main", "v1.0");
        viewModel.Branch.Should().Be("origin/main");
        viewModel.ToBranches.Should().Equal("main", "feature");
        viewModel.To.Should().Be("feature", "the current branch is rebased by default");
        viewModel.Autosquash.Should().BeTrue("rebase.autosquash is honoured");
        viewModel.UpdateRefs.Should().BeTrue("rebase.updaterefs is honoured");
        viewModel.AutoStash.Should().BeTrue();
        viewModel.CanAutoStash.Should().BeTrue("there are changes to stash");
        viewModel.IsShowOptionsVisible.Should().BeFalse("the options are shown when the rebase does not start immediately");
        viewModel.IsRebasePanelVisible.Should().BeTrue();
        viewModel.IsRebaseVisible.Should().BeTrue();
        viewModel.IsRebaseDefault.Should().BeTrue();
        viewModel.IsContinueVisible.Should().BeFalse();
        viewModel.IsSolveConflictsVisible.Should().BeFalse();
        viewModel.HasConflicts.Should().BeFalse();
        host.Commands.Should().BeEmpty();
    }

    [Test]
    public void Rebase_started_immediately_rebases_on_the_branch_and_closes_when_done()
    {
        FakeRebaseHost host = new() { UpdateRefsSetting = false };
        RebaseViewModel viewModel = CreateRebase(host, Options(defaultBranch: "main", startRebaseImmediately: true));
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.InitializeView();

        viewModel.Branches.Should().BeEmpty("the refs are not listed for an immediate rebase");
        viewModel.IsShowOptionsVisible.Should().BeTrue();
        host.Commands.Should().Equal($"{GitRebase} \"main\"");
        closed.Should().BeTrue();
        host.SavedAutoStash.Should().BeFalse();
    }

    [Test]
    public void Rebase_requires_a_branch()
    {
        FakeRebaseHost host = new();
        ProcessViewModelTests.FakeMessageBoxes messageBoxes = new();
        RebaseViewModel viewModel = CreateRebase(host, Options(), messageBoxes);
        viewModel.InitializeView();

        viewModel.RebaseCommand.Execute(null);

        messageBoxes.Errors.Should().Equal("Please select a branch");
        host.Commands.Should().BeEmpty();
    }

    [Test]
    public void Rebase_passes_the_options()
    {
        FakeRebaseHost host = new() { UpdateRefsSetting = null };
        RebaseViewModel viewModel = CreateRebase(host, Options());
        viewModel.InitializeView();
        viewModel.Branch = "main";
        viewModel.IsInteractive = true;
        viewModel.Autosquash = true;
        viewModel.PreserveMerges = true;
        viewModel.AutoStash = true;

        viewModel.RebaseCommand.Execute(null);

        // UpdateRefs is unchecked but rebase.updaterefs is unset, so the choice is passed (as FormRebase compares bool? with bool).
        host.Commands.Should().Equal($"{GitRebase} -i --autosquash --rebase-merges --no-update-refs --autostash \"main\"");
        host.SavedAutoStash.Should().BeTrue();
    }

    [Test]
    public void Rebase_of_a_specific_range_rebases_onto_the_branch()
    {
        FakeRebaseHost host = new() { UpdateRefsSetting = false };
        RebaseViewModel viewModel = CreateRebase(host, Options(from: "abc123", to: "topic", defaultBranch: "main"));
        viewModel.IsSpecificRange.Should().BeTrue("a start commit is given");
        viewModel.From.Should().Be("abc123");
        viewModel.InitializeView();
        viewModel.To.Should().Be("topic");

        viewModel.RebaseCommand.Execute(null);

        host.Commands.Should().Equal($"{GitRebase} --onto main \"abc123\" \"topic\"");
    }

    [Test]
    public void Rebase_shows_a_message_when_the_branch_is_up_to_date()
    {
        FakeRebaseHost host = new() { Output = "Current branch a is up to date." };
        ProcessViewModelTests.FakeMessageBoxes messageBoxes = new();
        RebaseViewModel viewModel = CreateRebase(host, Options(defaultBranch: "main"), messageBoxes);
        viewModel.InitializeView();

        viewModel.RebaseCommand.Execute(null);

        messageBoxes.Informations.Should().ContainSingle().Which.Should().StartWith("Current branch a is up to date.");
    }

    [Test]
    public void Rebase_date_options_exclude_the_interactive_options()
    {
        RebaseViewModel viewModel = CreateRebase(new FakeRebaseHost(), Options());

        viewModel.CanAutosquash.Should().BeFalse("autosquash needs an interactive rebase");
        viewModel.IsInteractive = true;
        viewModel.CanAutosquash.Should().BeTrue();

        viewModel.IgnoreDate = true;
        viewModel.CanInteractive.Should().BeFalse();
        viewModel.CanAutosquash.Should().BeFalse();
        viewModel.CanCommitterDateIsAuthorDate.Should().BeFalse();
        viewModel.CanIgnoreDate.Should().BeTrue();

        viewModel.IgnoreDate = false;
        viewModel.CommitterDateIsAuthorDate = true;
        viewModel.CanInteractive.Should().BeFalse();
        viewModel.CanIgnoreDate.Should().BeFalse();
    }

    [Test]
    public void Rebase_with_conflicts_offers_to_solve_them()
    {
        FakeRebaseHost host = new() { IsRebasing = true, HasConflicts = true, IsDirty = true };
        RebaseViewModel viewModel = CreateRebase(host, Options());
        int focusRequests = 0;
        viewModel.FocusDefaultButtonRequested += (_, _) => focusRequests++;

        viewModel.InitializeView();

        viewModel.IsRebasePanelVisible.Should().BeFalse();
        viewModel.IsRebaseVisible.Should().BeFalse();
        viewModel.IsSolveConflictsVisible.Should().BeTrue();
        viewModel.IsContinueVisible.Should().BeFalse();
        viewModel.IsSolveConflictsDefault.Should().BeTrue();
        viewModel.SolveConflictsText.Should().Be(">_Solve conflicts<");
        viewModel.ContinueText.Should().Be("_Continue rebase");
        viewModel.CanAutoStash.Should().BeFalse("not during a rebase");
        focusRequests.Should().Be(1);

        host.HasConflicts = false;
        viewModel.SolveConflictsCommand.Execute(null);

        host.ResolveConflictsCount.Should().Be(1);
        viewModel.IsContinueVisible.Should().BeTrue();
        viewModel.IsContinueDefault.Should().BeTrue();
        viewModel.ContinueText.Should().Be(">_Continue rebase<");
        viewModel.SolveConflictsText.Should().Be("_Solve conflicts");
    }

    [Test]
    public void Rebase_continues_while_git_can_continue_and_closes_when_done()
    {
        FakeRebaseHost host = new() { IsRebasing = true, Output = "can continue", CanContinue = true };
        RebaseViewModel viewModel = CreateRebase(host, Options());
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;
        viewModel.InitializeView();
        host.OnCommand = _ =>
        {
            if (host.Commands.Count == 2)
            {
                host.IsRebasing = false;
            }
        };

        viewModel.ContinueRebaseCommand.Execute(null);

        host.Commands.Should().Equal($"{GitRebase} --continue", $"{GitRebase} --continue");
        closed.Should().BeTrue();
    }

    [Test]
    public void Rebase_skip_marks_the_applying_commit_and_abort_forgets_the_skipped_ones()
    {
        FakeRebaseHost host = new()
        {
            IsRebasing = true,
            Patches = () =>
            [
                new PatchItem { Action = "pick", ObjectId = _commit1, IsApplied = true },
                new PatchItem { Action = "pick", ObjectId = _commit2, IsNext = true },
            ],
        };
        List<PatchItem> skipped = [];
        RebaseViewModel viewModel = CreateRebase(host, Options(), skipped: skipped);
        viewModel.InitializeView();
        viewModel.PatchGrid.SelectedPatch!.ObjectId.Should().Be(_commit2, "the applying commit is selected");

        viewModel.SkipCommand.Execute(null);

        host.Commands.Should().Equal($"{GitRebase} --skip");
        skipped.Select(p => p.ObjectId).Should().Equal(_commit2);
        viewModel.PatchGrid.PatchFiles![1].Status.Should().Be("Skipped", "the refreshed grid keeps the skipped commit");

        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;
        host.OnCommand = _ => host.IsRebasing = false;
        viewModel.AbortCommand.Execute(null);

        host.Commands.Should().Equal($"{GitRebase} --skip", $"{GitRebase} --abort");
        skipped.Should().BeEmpty();
        closed.Should().BeTrue();
    }

    [Test]
    public void Rebase_edit_todo_and_the_other_dialogs()
    {
        FakeRebaseHost host = new() { IsRebasing = true };
        RebaseViewModel viewModel = CreateRebase(host, Options());
        viewModel.InitializeView();

        viewModel.EditTodoCommand.Execute(null);
        viewModel.AddFilesCommand.Execute(null);
        viewModel.CommitCommand.Execute(null);

        host.Commands.Should().Equal($"{GitRebase} --edit-todo");
        host.AddFilesCount.Should().Be(1);
        host.CommitCount.Should().Be(1);
    }

    [Test]
    public void Rebase_chooses_the_start_of_the_range()
    {
        FakeRebaseHost host = new() { ChosenRevision = "12345678" };
        RebaseViewModel viewModel = CreateRebase(host, Options(from: "old"));
        viewModel.Branch = "main";

        viewModel.ChooseFromRevisionCommand.Execute(null);

        viewModel.From.Should().Be("12345678");
        host.ChooseArguments.Should().Equal(("old", "main"));

        host.ChosenRevision = null;
        viewModel.ChooseFromRevisionCommand.Execute(null);
        viewModel.From.Should().Be("12345678", "cancelling keeps the commit");
    }

    [Test]
    public void PatchGrid_opens_the_commit_or_the_patch()
    {
        FakePatchGridHost host = new()
        {
            Patches = () =>
            [
                new PatchItem { Action = "pick", ObjectId = _commit1 },
                new PatchItem { Name = "0001", FullName = "C:/repo/.git/rebase-apply/0001" },
                new PatchItem { Name = "0002" },
            ],
        };
        ProcessViewModelTests.FakeMessageBoxes messageBoxes = new();
        PatchGridViewModel grid = new(new PatchGridStrings(), host, messageBoxes, isManagingRebase: true, [], "Error");
        grid.Initialize();
        grid.SelectedPatch.Should().BeNull("no commit is being applied");

        grid.SelectedPatch = grid.Patches[0];
        grid.OpenSelectedCommand.Execute(null);
        grid.SelectedPatch = grid.Patches[1];
        grid.OpenSelectedCommand.Execute(null);
        grid.SelectedPatch = grid.Patches[2];
        grid.OpenSelectedCommand.Execute(null);

        host.ShownCommits.Should().Equal(_commit1);
        host.ShownPatches.Should().Equal("C:/repo/.git/rebase-apply/0001");
        messageBoxes.Errors.Should().Equal("Unable to show details of patch file.");
    }

    [Test]
    public void PatchGrid_marks_the_skipped_patches_before_the_current_one()
    {
        FakePatchGridHost host = new()
        {
            Patches = () =>
            [
                new PatchItem { Name = "0001", IsApplied = true },
                new PatchItem { Name = "0002", IsApplied = true },
                new PatchItem { Name = "0003", IsNext = true },
            ],
        };
        PatchGridViewModel grid = new(new PatchGridStrings(), host, new ProcessViewModelTests.FakeMessageBoxes(), isManagingRebase: false, [new PatchItem { Name = "0002" }, new PatchItem { Name = "0003" }], "Error");

        grid.Initialize();

        grid.Patches.Select(p => p.Status).Should().Equal("Applied", "Skipped", "Applying...");
        grid.SelectedPatch!.Name.Should().Be("0003");
    }

    [Test]
    public void ApplyPatch_loads_the_settings_and_the_state_when_shown()
    {
        FakeApplyPatchHost host = new() { IgnoreWhitespaceSetting = true };
        ApplyPatchViewModel viewModel = CreateApplyPatch(host, patchFile: "C:/patches/fix.patch");

        viewModel.InitializeView();

        viewModel.Title.Should().Be("Apply patch (C:/repo/)");
        viewModel.IsPatchFileMode.Should().BeTrue();
        viewModel.PatchFile.Should().Be("C:/patches/fix.patch");
        viewModel.IgnoreWhitespace.Should().BeTrue();
        viewModel.SignOff.Should().BeFalse();
        host.SavedSettings.Should().BeEmpty("loading the settings does not save them");
        viewModel.CanApply.Should().BeTrue();
        viewModel.IsPatchFileEnabled.Should().BeTrue();
        viewModel.IsPatchDirectoryEnabled.Should().BeFalse();
        viewModel.CanContinue.Should().BeFalse();
        viewModel.CanResolve.Should().BeFalse();
        viewModel.CanRunMergetool.Should().BeFalse();
        viewModel.IsApplyDefault.Should().BeTrue();

        viewModel.SignOff = true;
        viewModel.IsPatchDirectoryMode = true;

        host.SavedSettings.Should().Equal("SignOff=True");
        viewModel.IsPatchFileEnabled.Should().BeFalse();
        viewModel.IsPatchDirectoryEnabled.Should().BeTrue();
    }

    [Test]
    public void ApplyPatch_applies_a_mailbox_patch_or_a_diff_and_closes_when_done()
    {
        string mailbox = Path.GetTempFileName();
        string diff = Path.GetTempFileName();
        try
        {
            File.WriteAllText(mailbox, "From 1234 Mon Sep 17 00:00:00 2001\nSubject: [PATCH] Fix\n");
            File.WriteAllText(diff, "diff --git a/a.txt b/a.txt\n");
            FakeApplyPatchHost host = new();
            ApplyPatchViewModel viewModel = CreateApplyPatch(host, patchFile: mailbox);
            bool? closed = null;
            viewModel.CloseRequested += (_, accepted) => closed = accepted;
            viewModel.InitializeView();
            viewModel.SignOff = true;

            viewModel.ApplyCommand.Execute(null);

            host.Commands.Should().ContainSingle().Which.Should().StartWith("am --3way --signoff");
            host.RepoChangedCount.Should().Be(1);
            closed.Should().BeTrue();

            viewModel.PatchFile = diff;
            viewModel.IgnoreWhitespace = true;
            viewModel.ApplyCommand.Execute(null);
            host.Commands[1].Should().StartWith("apply --ignore-whitespace");
        }
        finally
        {
            File.Delete(mailbox);
            File.Delete(diff);
        }
    }

    [Test]
    public void ApplyPatch_applies_the_patches_of_a_directory()
    {
        FakeApplyPatchHost host = new();
        ApplyPatchViewModel viewModel = CreateApplyPatch(host, patchDirectory: "C:/patches");
        viewModel.IsPatchDirectoryMode.Should().BeTrue();
        viewModel.InitializeView();

        viewModel.ApplyCommand.Execute(null);

        host.AppliedDirectories.Should().Equal(("C:/patches", "am --3way"));
        host.Commands.Should().BeEmpty();
    }

    [Test]
    public void ApplyPatch_requires_a_patch()
    {
        FakeApplyPatchHost host = new();
        ProcessViewModelTests.FakeMessageBoxes messageBoxes = new();
        ApplyPatchViewModel viewModel = CreateApplyPatch(host, patchFile: "", messageBoxes: messageBoxes);
        viewModel.InitializeView();

        viewModel.ApplyCommand.Execute(null);

        messageBoxes.Errors.Should().Equal("Please select a patch to apply");
        host.Commands.Should().BeEmpty();
    }

    [Test]
    public void ApplyPatch_in_progress_offers_the_patch_actions()
    {
        FakeApplyPatchHost host = new()
        {
            IsInPatch = true,
            HasConflicts = true,
            Patches = () =>
            [
                new PatchItem { Name = "0001", IsApplied = true },
                new PatchItem { Name = "0002", IsNext = true },
            ],
        };
        List<PatchItem> skipped = [];
        ApplyPatchViewModel viewModel = CreateApplyPatch(host, patchFile: "", skipped: skipped);
        viewModel.InitializeView();

        viewModel.CanApply.Should().BeFalse();
        viewModel.IsPatchFileEnabled.Should().BeFalse();
        viewModel.CanContinue.Should().BeTrue();
        viewModel.CanRunMergetool.Should().BeTrue();
        viewModel.CanResolve.Should().BeFalse();
        viewModel.IsMergetoolDefault.Should().BeTrue();
        viewModel.MergetoolText.Should().Be(">_Solve conflicts<");

        host.HasConflicts = false;
        viewModel.MergetoolCommand.Execute(null);
        viewModel.CanResolve.Should().BeTrue();
        viewModel.IsResolvedDefault.Should().BeTrue();
        viewModel.ResolvedText.Should().Be(">Conflicts resolved<");
        viewModel.MergetoolText.Should().Be("_Solve conflicts");

        viewModel.ResolvedCommand.Execute(null);
        viewModel.SkipCommand.Execute(null);
        skipped.Select(p => p.Name).Should().Equal("0002");
        viewModel.AbortCommand.Execute(null);

        host.ResolveConflictsCount.Should().Be(1);
        host.Commands.Should().Equal("am --3way --resolved", "am --3way --skip", "am --3way --abort");
        skipped.Should().BeEmpty();
    }

    [Test]
    public async Task ApplyPatch_browses_for_a_patch_file_and_a_directory()
    {
        SmallDialogViewModelTests.FakeFileDialogs fileDialogs = new() { Files = ["C:/patches/other.patch"], Folder = "C:/patches" };
        ApplyPatchViewModel viewModel = CreateApplyPatch(new FakeApplyPatchHost(), patchFile: "C:/patches/fix.patch", fileDialogs: fileDialogs);

        await viewModel.BrowsePatchCommand.ExecuteAsync(null);
        await viewModel.BrowseDirectoryCommand.ExecuteAsync(null);

        viewModel.PatchFile.Should().Be("C:/patches/other.patch");
        viewModel.PatchDirectory.Should().Be("C:/patches");
        fileDialogs.FilePickers.Should().Equal(("Select patch file", "Patch file (*.Patch)", "*.patch"));

        SmallDialogViewModelTests.FakeFileDialogs cancelled = new();
        viewModel = CreateApplyPatch(new FakeApplyPatchHost(), patchFile: "C:/patches/fix.patch", fileDialogs: cancelled);
        await viewModel.BrowsePatchCommand.ExecuteAsync(null);
        viewModel.PatchFile.Should().Be("C:/patches/fix.patch", "cancelling keeps the file");
    }

    internal static RebaseDialogOptions Options(string? from = null, string? to = null, string? defaultBranch = null, bool interactive = false, bool startRebaseImmediately = false)
        => new(from, to, defaultBranch, interactive, startRebaseImmediately, SupportUpdateRefs: true, AlwaysShowAdvancedOptions: false);

    internal static RebaseViewModel CreateRebase(FakeRebaseHost host, RebaseDialogOptions options, ProcessViewModelTests.FakeMessageBoxes? messageBoxes = null, List<PatchItem>? skipped = null)
    {
        messageBoxes ??= new();
        return new RebaseViewModel(
            new RebaseStrings(),
            new PatchGridViewModel(new PatchGridStrings(), host, messageBoxes, isManagingRebase: true, skipped ?? [], "Error"),
            new HelpImageViewModel(new HelpImageStrings(), isVisible: true, isExpanded: true, _ => { }),
            host,
            messageBoxes,
            options,
            "Error");
    }

    internal static ApplyPatchViewModel CreateApplyPatch(
        FakeApplyPatchHost host,
        string? patchFile = null,
        string? patchDirectory = null,
        ProcessViewModelTests.FakeMessageBoxes? messageBoxes = null,
        SmallDialogViewModelTests.FakeFileDialogs? fileDialogs = null,
        List<PatchItem>? skipped = null)
    {
        messageBoxes ??= new();
        return new ApplyPatchViewModel(
            new ApplyPatchStrings(),
            new PatchGridViewModel(new PatchGridStrings(), host, messageBoxes, isManagingRebase: false, skipped ?? [], "Error"),
            host,
            messageBoxes,
            fileDialogs ?? new(),
            "C:/repo/",
            patchFile,
            patchDirectory,
            "Error");
    }

    internal class FakePatchGridHost : IPatchGridHost
    {
        public Func<IReadOnlyList<PatchItem>> Patches { get; init; } = () => [];

        public List<ObjectId> ShownCommits { get; } = [];

        public List<string> ShownPatches { get; } = [];

        public IReadOnlyList<PatchItem> LoadPatches() => Patches();

        public void ShowCommit(ObjectId objectId) => ShownCommits.Add(objectId);

        public void ShowPatch(string fullName) => ShownPatches.Add(fullName);
    }

    internal sealed class FakeRebaseHost : FakePatchGridHost, IRebaseHost
    {
        public bool IsRebasing { get; set; }

        public bool HasConflicts { get; set; }

        public bool IsDirty { get; init; }

        public bool AutoSquash { get; init; }

        public bool? UpdateRefsSetting { get; init; }

        public string Output { get; init; } = "";

        public bool CanContinue { get; init; }

        public string? ChosenRevision { get; set; }

        public List<string> Commands { get; } = [];

        public Action<string>? OnCommand { get; set; }

        public bool? SavedAutoStash { get; private set; }

        public int ResolveConflictsCount { get; private set; }

        public int AddFilesCount { get; private set; }

        public int CommitCount { get; private set; }

        public List<(string From, string Onto)> ChooseArguments { get; } = [];

        public bool RebaseAutoStash
        {
            get => field;
            set
            {
                field = value;
                SavedAutoStash = value;
            }
        }

        public string GetSelectedBranch() => "feature";

        public IReadOnlyList<RebaseRef> GetRefs() => [new("main", true), new("feature", true), new("origin/main", false), new("v1.0", false)];

        public bool InTheMiddleOfRebase() => IsRebasing;

        public bool InTheMiddleOfConflictedMerge() => HasConflicts;

        public bool InTheMiddleOfAction() => IsRebasing;

        public bool InTheMiddleOfPatch() => false;

        public bool IsDirtyDir() => IsDirty;

        public bool? GetEffectiveBoolSetting(string name) => name switch
        {
            "rebase.autosquash" => AutoSquash,
            "rebase.updaterefs" => UpdateRefsSetting,
            _ => null,
        };

        public string RunGit(ArgumentString arguments) => ReadGit(arguments);

        public string ReadGit(ArgumentString arguments)
        {
            Commands.Add(arguments.ToString());
            OnCommand?.Invoke(arguments.ToString());
            return Output;
        }

        public bool CanContinueAction(string output) => CanContinue;

        public void ResolveConflicts() => ResolveConflictsCount++;

        public void AddFiles() => AddFilesCount++;

        public void Commit() => CommitCount++;

        public string? ChooseFromRevision(string from, string onto)
        {
            ChooseArguments.Add((from, onto));
            return ChosenRevision;
        }
    }

    internal sealed class FakeApplyPatchHost : FakePatchGridHost, IApplyPatchHost
    {
        public bool IsInPatch { get; set; }

        public bool HasConflicts { get; set; }

        public bool IgnoreWhitespaceSetting { get; init; }

        public List<string> Commands { get; } = [];

        public List<(string Directory, string Arguments)> AppliedDirectories { get; } = [];

        public List<string> SavedSettings { get; } = [];

        public int RepoChangedCount { get; private set; }

        public int ResolveConflictsCount { get; private set; }

        public bool InTheMiddleOfPatch() => IsInPatch;

        public bool InTheMiddleOfConflictedMerge() => HasConflicts;

        public bool InTheMiddleOfAction() => IsInPatch;

        public string? GetPathForGitExecution(string? path) => path;

        public void RunGit(ArgumentString arguments) => Commands.Add(arguments.ToString());

        public void ApplyPatchDirectory(string directory, ArgumentString arguments) => AppliedDirectories.Add((directory, arguments.ToString()));

        public void NotifyRepoChanged() => RepoChangedCount++;

        public void ResolveConflicts() => ResolveConflictsCount++;

        public void AddFiles()
        {
        }

        public (bool IgnoreWhitespace, bool SignOff) LoadSettings() => (IgnoreWhitespaceSetting, false);

        public void SaveIgnoreWhitespace(bool value) => SavedSettings.Add($"IgnoreWhitespace={value}");

        public void SaveSignOff(bool value) => SavedSettings.Add($"SignOff={value}");
    }
}
