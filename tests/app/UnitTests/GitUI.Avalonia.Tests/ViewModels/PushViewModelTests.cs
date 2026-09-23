using GitCommands.Git;
using GitCommands.Remotes;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.CommandsDialogs;
using NSubstitute;
using static GitUI.AvaloniaTests.ViewModels.ProcessViewModelTests;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the push dialog (port of <c>FormPush</c>).</summary>
[TestFixture]
public sealed class PushViewModelTests
{
    [Test]
    public void The_current_branch_is_pushed_to_its_remote_branch()
    {
        FakePushHost host = new();
        PushViewModel viewModel = Create(host, out _);
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.Title.Should().Be(@"Push (C:\repo)");
        viewModel.Remotes.Select(r => r.Name).Should().Equal("origin", "upstream");
        viewModel.SelectedRemote!.Name.Should().Be("origin");
        viewModel.PushDestination.Should().Be("https://example.org/push.git", "the push URL of the remote");
        viewModel.Branches.Should().Equal(PushViewModel.AllRefs, PushViewModel.HeadText, "main", "feature");
        viewModel.Branch.Should().Be("main");
        viewModel.RemoteBranch.Should().Be("main", "the branch it tracks");

        viewModel.PushCommand.Execute(null);

        PushRequest request = host.Requests.Single();
        request.Should().BeEquivalentTo(new
        {
            Tab = PushTab.Branch,
            Destination = "origin",
            Remote = "origin",
            PushToRemote = true,
            LocalBranch = "main",
            RemoteBranch = "main",
            PushAllBranches = false,
            ForcePush = ForcePushOptions.DoNotForce,
            Track = false,
            IsCurrentBranch = true,
            IsCurrentBranchRemote = true,
        });
        closed.Should().BeTrue("the dialog closes after the push");
    }

    [Test]
    public void A_branch_new_for_the_remote_is_confirmed_and_tracked_if_asked()
    {
        FakePushHost host = new() { ConfirmNewBranch = false };
        PushViewModel viewModel = Create(host, out FakeMessageBoxes messageBoxes);

        viewModel.Branch = "feature";
        viewModel.RemoteBranch.Should().Be("feature");
        viewModel.PushChanges().Should().BeFalse();
        host.NewBranchConfirmations.Should().Be(1);
        host.Requests.Should().BeEmpty();

        host.ConfirmNewBranch = true;
        messageBoxes.ConfirmWithCancelResult = true;
        viewModel.PushChanges().Should().BeTrue();
        messageBoxes.Confirmations.Should().ContainSingle().Which.Should().Contain("does not have a tracking reference");
        host.Requests.Single().Track.Should().BeTrue();

        messageBoxes.ConfirmWithCancelResult = null;
        viewModel.PushChanges().Should().BeFalse("cancelled");
    }

    [Test]
    public void Force_push_offers_the_lease_and_the_options_exclude_each_other()
    {
        FakePushHost host = new();
        PushViewModel viewModel = Create(host, out FakeMessageBoxes messageBoxes);

        viewModel.ForceWithLease = true;
        viewModel.ForcePushBranches = true;
        viewModel.ForceWithLease.Should().BeFalse();
        viewModel.GetForcePushOption().Should().Be(ForcePushOptions.Force);

        messageBoxes.ConfirmWithCancelResult = true;
        viewModel.PushChanges().Should().BeTrue();
        messageBoxes.Confirmations.Should().ContainSingle().Which.Should().Contain("safer force with lease");
        host.Requests.Single().ForcePush.Should().Be(ForcePushOptions.ForceWithLease);
        viewModel.ForcePushBranches.Should().BeFalse();

        viewModel.SelectedTab = PushTab.Tag;
        viewModel.ForcePushTags.Should().BeTrue("the tags are forced with the branches");
        viewModel.GetForcePushOption().Should().Be(ForcePushOptions.Force, "tags cannot be pushed with a lease");
    }

    [Test]
    public void Tags_are_pushed_one_or_all()
    {
        FakePushHost host = new();
        PushViewModel viewModel = Create(host, out FakeMessageBoxes messageBoxes);

        viewModel.SelectedTab = PushTab.Tag;
        viewModel.Tags.Should().Equal(PushViewModel.AllRefs, "v1.0");
        viewModel.Tag = "";
        viewModel.PushChanges().Should().BeFalse();
        messageBoxes.Errors.Should().ContainSingle().Which.Should().Contain("select a tag");

        viewModel.Tag = PushViewModel.AllRefs;
        viewModel.PushChanges().Should().BeTrue();
        viewModel.Tag = "v1.0";
        viewModel.PushChanges().Should().BeTrue();

        host.Requests.Select(r => (r.Tab, r.Tag, r.PushAllTags)).Should().Equal((PushTab.Tag, "", true), (PushTab.Tag, "v1.0", false));
    }

    [Test]
    public void Multiple_branches_are_pushed_forced_or_deleted()
    {
        FakePushHost host = new();
        PushViewModel viewModel = Create(host, out _);

        viewModel.SelectedTab = PushTab.MultipleBranches;
        viewModel.MultipleBranches.Select(r => r.LocalBranch).Should().Equal("main", "feature", null);
        viewModel.MultipleBranches[2].CanPush.Should().BeFalse("it only exists at the remote");

        viewModel.SelectBranchesToPush(tracked: true);
        viewModel.MultipleBranches.Select(r => r.Push).Should().Equal(true, false, false);
        viewModel.SelectBranchesToPush(tracked: null);
        viewModel.MultipleBranches.Select(r => r.Push).Should().Equal(true, true, false);

        viewModel.MultipleBranches[1].Force = true;
        viewModel.MultipleBranches[1].Push.Should().BeFalse();
        viewModel.MultipleBranches[2].Delete = true;
        viewModel.PushChanges().Should().BeTrue();

        host.Requests.Single().PushActions.Select(a => a.ToString()).Should().Equal("refs/heads/main:refs/heads/main", "+refs/heads/feature:refs/heads/feature", ":refs/heads/old");
    }

    [Test]
    public void Without_remote_the_remotes_can_be_configured_and_a_url_must_be_valid()
    {
        FakePushHost host = new();
        host.Remotes.Clear();
        PushViewModel viewModel = Create(host, out FakeMessageBoxes messageBoxes);
        messageBoxes.ConfirmResult = false;

        viewModel.PushChanges().Should().BeFalse();
        messageBoxes.Confirmations.Should().ContainSingle().Which.Should().Contain("configure a remote");
        host.ManagedRemotes.Should().Be(0);

        messageBoxes.ConfirmResult = true;
        host.Remotes.Add(new ConfigFileRemote { Name = "origin", Url = "https://example.org/repo.git" });
        viewModel.PushChanges().Should().BeTrue("the remotes were configured");
        host.ManagedRemotes.Should().Be(1);

        viewModel.IsPushToUrl = true;
        viewModel.RecentUrls.Should().Equal("https://example.org/recent.git");
        viewModel.PushDestination = "not a url";
        viewModel.PushChanges().Should().BeFalse();
        messageBoxes.Errors.Should().ContainSingle().Which.Should().Contain("destination directory");
    }

    internal static PushViewModel Create(FakePushHost host, out FakeMessageBoxes messageBoxes)
    {
        messageBoxes = new FakeMessageBoxes();
        return new PushViewModel(new PushStrings(), host, messageBoxes);
    }

    internal static IGitRef Ref(string name, bool isHead, string remote = "", string trackingRemote = "", string mergeWith = "")
    {
        IGitRef gitRef = Substitute.For<IGitRef>();
        gitRef.Name.Returns(isHead ? name : $"{remote}/{name}");
        gitRef.LocalName.Returns(name);
        gitRef.IsHead.Returns(isHead);
        gitRef.IsRemote.Returns(!isHead);
        gitRef.Remote.Returns(remote);
        gitRef.TrackingRemote.Returns(trackingRemote);
        gitRef.MergeWith.Returns(mergeWith);
        return gitRef;
    }

    /// <summary>A repository on "main" tracking origin/main, with a local branch "feature".</summary>
    internal sealed class FakePushHost : IPushHost
    {
        public List<ConfigFileRemote> Remotes { get; } =
        [
            new() { Name = "origin", Url = "https://example.org/repo.git", PushUrl = "https://example.org/push.git" },
            new() { Name = "upstream", Url = "https://example.org/upstream.git" },
        ];

        public List<PushRequest> Requests { get; } = [];

        public bool ConfirmNewBranch { get; set; } = true;

        public int NewBranchConfirmations { get; private set; }

        public int ManagedRemotes { get; private set; }

        public string WorkingDirectory => @"C:\repo";

        public string CurrentBranch => "main";

        public bool IsBareRepository => false;

        public bool IsAutoSetupMergeDisabled => false;

        public bool CanCreatePullRequest => false;

        public int RecursiveSubmodules { get; set; }

        public bool AlwaysShowAdvancedOptions => false;

        public bool DontConfirmAddTrackingReference => false;

        public IReadOnlyList<IGitRef> GetRefs()
            => [Ref("main", isHead: true, trackingRemote: "origin", mergeWith: "main"), Ref("feature", isHead: true), Ref("main", isHead: false, remote: "origin")];

        public IReadOnlyList<string> GetTags() => ["v1.0"];

        public IReadOnlyList<ConfigFileRemote> LoadRemotes() => [.. Remotes];

        public string GetBranchRemote(string? branch) => branch == "main" ? "origin" : "";

        public string? GetDefaultPushRemote(ConfigFileRemote remote, string branch) => null;

        public IReadOnlyList<string> GetRecentUrls() => ["https://example.org/recent.git"];

        public bool ManageRemotes(string? selectedRemote)
        {
            ManagedRemotes++;
            return true;
        }

        public void Pull()
        {
        }

        public void StartPageant(string? remote)
        {
        }

        public bool ConfirmNewBranchForRemote(string text, string caption)
        {
            NewBranchConfirmations++;
            return ConfirmNewBranch;
        }

        public IReadOnlyList<PushBranchRow>? LoadMultipleBranches(string remote)
            => [new PushBranchRow("main", "main", "="), new PushBranchRow("feature", "", ""), new PushBranchRow(null, "old", "")];

        public bool Push(PushRequest request, out bool errorOccurred)
        {
            Requests.Add(request);
            errorOccurred = false;
            return true;
        }
    }
}
