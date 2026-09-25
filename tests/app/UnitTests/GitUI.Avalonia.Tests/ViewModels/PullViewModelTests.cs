using GitExtensions.Extensibility.Git;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.UserControls;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the pull dialog (phase 5).</summary>
[TestFixture]
public sealed class PullViewModelTests
{
    // The cases of FormPullTests.Should_correctly_setup_for_defined_pull_action (a repository without remotes, "Merge" by default).
    [TestCase(GitPullAction.None, true, false, false, false, false, false, false)]
    [TestCase(GitPullAction.Merge, true, false, false, false, false, false, false)]
    [TestCase(GitPullAction.Rebase, false, false, false, true, false, false, false)]
    [TestCase(GitPullAction.Fetch, false, false, false, false, true, true, true)]
    [TestCase(GitPullAction.FetchAll, false, false, false, false, true, true, true)]
    [TestCase(GitPullAction.FetchPruneAll, false, true, false, false, true, true, true)]
    public void Sets_up_the_options_of_the_pull_action(
        GitPullAction pullAction, bool merge, bool prune, bool pruneTags, bool rebase, bool fetch, bool pruneEnabled, bool pruneTagsEnabled)
    {
        PullViewModel viewModel = Create(new FakePullHost { Remotes = [] }, Options(pullAction: pullAction));

        viewModel.IsMerge.Should().Be(merge);
        viewModel.IsRebase.Should().Be(rebase);
        viewModel.IsFetch.Should().Be(fetch);
        viewModel.Prune.Should().Be(prune);
        viewModel.PruneTags.Should().Be(pruneTags);
        viewModel.IsPruneEnabled.Should().Be(pruneEnabled);
        viewModel.IsPruneTagsEnabled.Should().Be(pruneTagsEnabled);
        viewModel.AutoStash.Should().BeFalse();
        viewModel.Remote.Should().Be(PullViewModel.AllRemotes);
        viewModel.CanMergeOrRebase.Should().BeFalse("all remotes can only be fetched");
    }

    [Test]
    public void Selects_the_remote_of_the_branch_and_shows_its_url()
    {
        FakePullHost host = new();
        host.Settings["branch.main.remote"] = "upstream";
        PullViewModel viewModel = Create(host, Options());

        viewModel.Remotes.Should().Equal(PullViewModel.AllRemotes, "origin", "upstream");
        viewModel.Remote.Should().Be("upstream");
        viewModel.PullSource.Should().Be("https://example.com/upstream.git");
        viewModel.IsMerge.Should().BeTrue();
        viewModel.CanMergeOrRebase.Should().BeTrue();
        viewModel.LocalBranch.Should().Be("main");
        viewModel.IsLocalBranchEnabled.Should().BeFalse();
        viewModel.Title.Should().Be("Pull (C:/repo)");
        viewModel.PullButtonText.Should().Be("_Pull");
        viewModel.HelpImage.IsOnHoverShowImage2.Should().BeTrue();
        viewModel.HelpImage.HoverNotice.Should().Be("Hover to see scenario when fast forward is possible.");
        viewModel.ShowUnshallow.Should().BeFalse();
    }

    [Test]
    public void The_given_remote_and_branch_are_preselected()
    {
        PullViewModel viewModel = Create(new FakePullHost(), Options(remote: "UPSTREAM", remoteBranch: "feature"));

        viewModel.Remote.Should().Be("upstream");
        viewModel.RemoteBranch.Should().Be("feature");
    }

    [Test]
    public void Without_a_remote_of_the_branch_the_first_remote_is_selected()
    {
        PullViewModel viewModel = Create(new FakePullHost(), Options());

        viewModel.Remote.Should().Be("origin");
        viewModel.PullSource.Should().Be("https://example.com/origin.git");
    }

    [Test]
    public void The_merge_options_update_the_local_branch_tags_prune_and_title()
    {
        PullViewModel viewModel = Create(new FakePullHost(), Options());

        viewModel.IsFetch = true;
        viewModel.IsLocalBranchEnabled.Should().BeTrue();
        viewModel.LocalBranch.Should().BeEmpty();
        viewModel.IsAllTagsEnabled.Should().BeTrue();
        viewModel.IsPruneEnabled.Should().BeTrue();
        viewModel.IsPruneTagsEnabled.Should().BeTrue();
        viewModel.Title.Should().Be("Fetch (C:/repo)");
        viewModel.PullButtonText.Should().Be("_Fetch");
        viewModel.Action.Should().Be(PullMergeAction.Fetch);
        viewModel.HelpImage.IsOnHoverShowImage2.Should().BeFalse();

        viewModel.IsAllTags = true;
        viewModel.IsRebase = true;
        viewModel.IsMerge.Should().BeFalse();
        viewModel.IsFetch.Should().BeFalse();
        viewModel.IsReachableTags.Should().BeTrue("all tags cannot be fetched by a pull");
        viewModel.IsAllTagsEnabled.Should().BeFalse();
        viewModel.IsPruneEnabled.Should().BeFalse();
        viewModel.LocalBranch.Should().Be("main");
        viewModel.Title.Should().Be("Pull (C:/repo)");
    }

    [Test]
    public void Prune_tags_needs_prune_and_all_tags()
    {
        PullViewModel viewModel = Create(new FakePullHost(), Options(pullAction: GitPullAction.Fetch));

        viewModel.PruneTags = true;
        viewModel.Prune.Should().BeTrue();
        viewModel.IsAllTags.Should().BeTrue();

        viewModel.Prune = false;
        viewModel.PruneTags.Should().BeFalse();
    }

    [Test]
    public void All_remotes_can_only_be_fetched()
    {
        PullViewModel viewModel = Create(new FakePullHost(), Options());

        viewModel.Remote = PullViewModel.AllRemotes;

        viewModel.IsFetch.Should().BeTrue();
        viewModel.CanMergeOrRebase.Should().BeFalse();
        viewModel.PullSource.Should().BeEmpty();

        viewModel.Remote = "origin";
        viewModel.CanMergeOrRebase.Should().BeTrue();
    }

    [Test]
    public void Pulling_from_a_url_offers_the_recent_urls_and_keeps_the_text()
    {
        PullViewModel viewModel = Create(new FakePullHost(), Options());

        viewModel.IsPullFromUrl = true;

        viewModel.UrlHistory.Should().Equal("C:/other");
        viewModel.PullSource.Should().Be("https://example.com/origin.git");
        viewModel.CanMergeOrRebase.Should().BeTrue();
    }

    [Test]
    public void The_remote_branches_of_the_remote_are_listed_when_dropped_down()
    {
        FakePullHost host = new();
        PullViewModel viewModel = Create(host, Options());

        viewModel.LoadRemoteBranchesCommand.Execute(null);
        viewModel.RemoteBranches.Should().Equal("", "feature", "main");

        viewModel.IsPullFromUrl = true;
        viewModel.RemoteBranches.Should().BeEmpty("changing the source resets the list");
        viewModel.LoadRemoteBranchesCommand.Execute(null);
        viewModel.RemoteBranches.Should().Equal("", "main", "dev");
        host.PuttyKeyRemotes.Should().Equal("origin", "");
    }

    [Test]
    public void Leaving_another_local_branch_suggests_it_as_the_remote_branch()
    {
        PullViewModel viewModel = Create(new FakePullHost(), Options(pullAction: GitPullAction.Fetch));

        viewModel.LocalBranch = "dev";
        viewModel.OnLocalBranchLeave();

        viewModel.RemoteBranch.Should().Be("dev");
    }

    [Test]
    public void Fetch_runs_git_with_the_options_and_the_scripts()
    {
        FakePullHost host = new();
        PullViewModel viewModel = Create(host, Options(pullAction: GitPullAction.Fetch));
        viewModel.RemoteBranch = "feature";
        viewModel.IsNoTags = true;
        viewModel.Prune = true;
        viewModel.AutoStash = true;

        viewModel.PullChanges().Should().Be(PullOutcome.Pulled);

        host.Commands.Should().ContainSingle().Which.Should().Be(new PullCommand(
            Fetch: true, Source: "origin", LocalBranch: "", RemoteBranch: "feature", Rebase: false, FetchTags: false,
            Unshallow: false, Prune: true, PruneTags: false, IsPullAll: false, PruneRemote: "origin"));
        host.Calls.Should().Equal("SaveSettings Fetch True", "BeforeScripts True", "RunPull", "AfterScripts True");
        viewModel.ErrorOccurred.Should().BeFalse();
    }

    [Test]
    public void Fetch_of_all_remotes_has_no_branches()
    {
        FakePullHost host = new();
        PullViewModel viewModel = Create(host, Options(pullAction: GitPullAction.FetchAll));

        viewModel.PullChanges().Should().Be(PullOutcome.Pulled);

        host.Commands.Single().Should().Be(new PullCommand(
            Fetch: true, Source: "--all", LocalBranch: null, RemoteBranch: null, Rebase: false, FetchTags: null,
            Unshallow: false, Prune: false, PruneTags: false, IsPullAll: true, PruneRemote: PullViewModel.AllRemotes));
        host.PuttyKeyRemotes.Should().Equal("origin,upstream");
    }

    [Test]
    public void Merge_pulls_the_branch_with_auto_stash_and_updates_submodules()
    {
        FakePullHost host = new() { HasChanges = true };
        host.Settings["branch.main.remote"] = "origin";
        PullViewModel viewModel = Create(host, Options());
        viewModel.AutoStash = true;

        viewModel.PullChanges().Should().Be(PullOutcome.Pulled);

        host.Commands.Single().Should().Be(new PullCommand(
            Fetch: false, Source: "origin", LocalBranch: null, RemoteBranch: "", Rebase: false, FetchTags: null,
            Unshallow: false, Prune: false, PruneTags: false, IsPullAll: false, PruneRemote: "origin"));
        host.Calls.Should().Equal("SaveSettings Merge True", "BeforeScripts False", "StashSave", "RunPull", "UpdateSubmodules", "StashPop", "AfterScripts False");
    }

    [Test]
    public void A_failed_pull_handles_the_conflicts_and_keeps_the_stash()
    {
        FakePullHost host = new() { HasChanges = true, ProcessResult = new PullProcessResult(Aborted: false, ErrorOccurred: true), InTheMiddleOfAction = true };
        host.Settings["branch.main.remote"] = "origin";
        PullViewModel viewModel = Create(host, Options());
        viewModel.AutoStash = true;

        viewModel.PullChanges().Should().Be(PullOutcome.Pulled);

        viewModel.ErrorOccurred.Should().BeTrue();
        host.Calls.Should().Equal("SaveSettings Merge True", "BeforeScripts False", "StashSave", "RunPull", "HandleMergeConflicts", "AfterScripts False");
    }

    [Test]
    public void Pulling_from_another_remote_than_the_configured_one_needs_a_remote_branch()
    {
        FakePullHost host = new() { UseLocalBranch = false };
        host.Settings["branch.main.remote"] = "origin";
        PullViewModel viewModel = Create(host, Options(remote: "upstream"));

        viewModel.PullChanges().Should().Be(PullOutcome.NotPulled);
        host.Questions.Should().Equal("Pull from upstream/main");
        host.Commands.Should().BeEmpty();

        host.UseLocalBranch = true;
        viewModel.PullChanges().Should().Be(PullOutcome.Pulled);
        host.Commands.Single().RemoteBranch.Should().Be("main");
        host.Commands.Single().LocalBranch.Should().Be("main");
    }

    [Test]
    public void Validation_errors_keep_the_dialog_open()
    {
        FakePullHost host = new();
        ProcessViewModelTests.FakeMessageBoxes messageBoxes = new();
        PullViewModel viewModel = Create(host, Options(), messageBoxes);
        bool closed = false;
        viewModel.CloseRequested += (_, _) => closed = true;

        viewModel.RemoteBranch = "*";
        viewModel.PullCommand.Execute(null);
        viewModel.IsPullFromUrl = true;
        viewModel.PullSource = "";
        viewModel.PullCommand.Execute(null);

        closed.Should().BeFalse();
        messageBoxes.Errors.Should().HaveCount(2);
        messageBoxes.Errors[1].Should().Be("Please select a source directory");
        host.Commands.Should().BeEmpty();
    }

    [Test]
    public void Pull_closes_the_dialog_when_git_ran()
    {
        PullViewModel viewModel = Create(new FakePullHost(), Options(pullAction: GitPullAction.Fetch));
        bool? accepted = null;
        viewModel.CloseRequested += (_, result) => accepted = result;

        viewModel.PullCommand.Execute(null);

        accepted.Should().BeTrue();
    }

    [Test]
    public void Rebasing_a_merge_commit_is_confirmed()
    {
        FakePullHost host = new() { MergeCommitExists = true, RebaseMergeAnswer = null };
        host.Settings["branch.main.merge"] = "refs/heads/main";
        PullViewModel viewModel = Create(host, Options(pullAction: GitPullAction.Rebase));

        viewModel.PullChanges().Should().Be(PullOutcome.Cancelled);
        host.MergeCommitChecks.Should().Equal("origin/main..main");

        host.RebaseMergeAnswer = false;
        viewModel.PullChanges().Should().Be(PullOutcome.NotPulled);
        host.Commands.Should().BeEmpty();
    }

    [Test]
    public void Fetch_and_prune_all_without_the_dialog_is_confirmed()
    {
        FakePullHost host = new() { ConfirmPruneAll = false };
        PullViewModel viewModel = Create(host, Options(pullAction: GitPullAction.FetchPruneAll));

        viewModel.PullWithoutDialog(remote: null, GitPullAction.FetchPruneAll).Should().Be(PullOutcome.Cancelled);
        host.Questions.Should().Equal("Prune remote branches from [ All ]");
        host.Commands.Should().BeEmpty();
    }

    [Test]
    public void Manage_remotes_reloads_the_remotes_and_keeps_the_selection()
    {
        FakePullHost host = new();
        PullViewModel viewModel = Create(host, Options(remote: "upstream"));
        viewModel.PullSource = "edited";
        host.Remotes = ["origin", "upstream", "third"];

        viewModel.ManageRemotesCommand.Execute(null);

        host.Calls.Should().Equal("Remotes upstream");
        viewModel.Remotes.Should().Equal(PullViewModel.AllRemotes, "origin", "upstream", "third");
        viewModel.Remote.Should().Be("upstream");
        viewModel.PullSource.Should().Be("edited", "rebinding the remotes does not validate them");
    }

    [Test]
    public void Solving_conflicts_offers_to_commit()
    {
        FakePullHost host = new();
        ProcessViewModelTests.FakeMessageBoxes messageBoxes = new();
        PullViewModel viewModel = Create(host, Options(), messageBoxes);

        viewModel.SolveConflictsCommand.Execute(null);

        host.Calls.Should().Equal("MergeTool", "Commit");

        // As FormPull: a question (MessageBoxIcon.Question).
        messageBoxes.Questions.Should().ContainSingle();
    }

    internal static PullOptions Options(GitPullAction pullAction = GitPullAction.None, string? remote = null, string? remoteBranch = null)
        => new(
            SelectedBranch: "main",
            DefaultRemoteBranch: remoteBranch,
            DefaultRemote: remote,
            PullAction: pullAction,
            DefaultPullAction: GitPullAction.Merge,
            AutoStash: false,
            IsShallow: false,
            WorkingDirDisplayPath: "C:/repo",
            ErrorCaption: "Error");

    internal static PullViewModel Create(FakePullHost host, PullOptions options, ProcessViewModelTests.FakeMessageBoxes? messageBoxes = null)
        => new(
            new PullStrings(),
            options,
            new HelpImageViewModel(new HelpImageStrings(), isVisible: true, isExpanded: true, _ => { }),
            host,
            messageBoxes ?? new ProcessViewModelTests.FakeMessageBoxes());

    internal sealed class FakePullHost : IPullHost
    {
        public IReadOnlyList<string> Remotes { get; set; } = ["origin", "upstream"];

        public Dictionary<string, string> Settings { get; } = new()
        {
            ["remote.origin.url"] = "https://example.com/origin.git",
            ["remote.upstream.url"] = "https://example.com/upstream.git",
        };

        public List<string> Calls { get; } = [];

        public List<PullCommand> Commands { get; } = [];

        public List<string> Questions { get; } = [];

        public List<string> PuttyKeyRemotes { get; } = [];

        public List<string> MergeCommitChecks { get; } = [];

        public bool HasChanges { get; set; }

        public bool InTheMiddleOfAction { get; set; }

        public bool MergeCommitExists { get; set; }

        public bool? RebaseMergeAnswer { get; set; } = true;

        public bool UseLocalBranch { get; set; } = true;

        public bool ConfirmPruneAll { get; set; } = true;

        public PullProcessResult ProcessResult { get; set; }

        public IReadOnlyList<string> LoadRemotes() => Remotes;

        public string GetSetting(string name) => Settings.TryGetValue(name, out string? value) ? value : "";

        public IReadOnlyList<PullRef> GetRefs(bool remotes)
            => remotes
                ? [new("origin/main", "main"), new("upstream/main", "main"), new("origin/feature", "feature")]
                : [new("main", "main"), new("dev", "dev")];

        public IReadOnlyList<string> LoadUrlHistory() => ["C:/other"];

        public void AddLocalSourceToHistory(string path) => Calls.Add($"History {path}");

        public void LoadPuttyKeys(IReadOnlyList<string> remotes) => PuttyKeyRemotes.Add(string.Join(",", remotes));

        public Task<string?> PickFolderAsync(string? startDirectory) => Task.FromResult<string?>("C:/picked");

        public bool IsDetachedHead() => false;

        public bool ExistsMergeCommit(string remoteBranch, string branch)
        {
            MergeCommitChecks.Add($"{remoteBranch}..{branch}");
            return MergeCommitExists;
        }

        public string GetNameRev(string name) => name.Replace("refs/heads/", "");

        public void SaveSettings(GitPullAction pullAction, bool autoStash) => Calls.Add($"SaveSettings {pullAction} {autoStash}");

        public bool? ConfirmRebaseMergeCommit(string text, string caption) => RebaseMergeAnswer;

        public DetachedHeadPullChoice AskPullOnDetachedHead(string text) => DetachedHeadPullChoice.Continue;

        public bool StartCheckoutBranch() => true;

        public bool ConfirmUseLocalBranch(string caption, string heading, string text, string commandLinkText, bool showDontShowAgain)
        {
            Questions.Add(commandLinkText);
            return UseLocalBranch;
        }

        public bool ConfirmFetchAndPruneAll(string text, string caption)
        {
            Questions.Add(caption);
            return ConfirmPruneAll;
        }

        public bool RunBeforeScripts(bool fetchOnly)
        {
            Calls.Add($"BeforeScripts {fetchOnly}");
            return true;
        }

        public void RunAfterScripts(bool fetchOnly) => Calls.Add($"AfterScripts {fetchOnly}");

        public bool HasChangesToStash() => HasChanges;

        public void StashSave() => Calls.Add("StashSave");

        public PullProcessResult RunPull(PullCommand command)
        {
            Calls.Add("RunPull");
            Commands.Add(command);
            return ProcessResult;
        }

        public bool HasSubmodules() => false;

        public bool AreSubmodulesInitialized() => true;

        public bool? UpdateSubmodulesWithoutAsking => null;

        public void StartUpdateSubmodulesDialog() => Calls.Add("StartUpdateSubmodulesDialog");

        public void UpdateSubmodules() => Calls.Add("UpdateSubmodules");

        public bool IsInTheMiddleOfRebase() => false;

        public bool IsInTheMiddleOfAction() => InTheMiddleOfAction;

        public bool StartContinueRebaseDialog() => true;

        public bool HandleMergeConflicts()
        {
            Calls.Add("HandleMergeConflicts");
            return true;
        }

        public bool ConfirmApplyStash() => true;

        public void StashPop() => Calls.Add("StashPop");

        public void StartRemotesDialog(string? remote) => Calls.Add($"Remotes {remote}");

        public void StartStashDialog() => Calls.Add("Stash");

        public Task RunMergeToolAsync()
        {
            Calls.Add("MergeTool");
            return Task.CompletedTask;
        }

        public void StartCommitDialog() => Calls.Add("Commit");
    }
}
