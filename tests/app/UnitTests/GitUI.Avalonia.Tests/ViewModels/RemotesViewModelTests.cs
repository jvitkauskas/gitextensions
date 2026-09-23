using GitCommands.Git;
using GitCommands.Remotes;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.CommandsDialogs;
using NSubstitute;
using static GitUI.AvaloniaTests.ViewModels.ProcessViewModelTests;
using static GitUI.AvaloniaTests.ViewModels.SmallDialogViewModelTests;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the remotes dialog (phase 2, batch 10).</summary>
[TestFixture]
public sealed class RemotesViewModelTests
{
    private IConfigFileRemoteSettingsManager _manager = null!;
    private FakeRemotesHost _host = null!;
    private FakeMessageBoxes _messageBoxes = null!;
    private List<ConfigFileRemote> _remotes = null!;

    [SetUp]
    public void SetUp()
    {
        _remotes =
        [
            new ConfigFileRemote { Name = "origin", Url = "https://github.com/owner/repo.git" },
            new ConfigFileRemote { Name = "old", Url = "https://example.org/legacy.git", Disabled = true },
            new ConfigFileRemote { Name = "upstream", Url = "https://github.com/upstream/repo.git", PushUrl = "git@github.com:upstream/repo.git", Color = "#FF0000" },
        ];
        _manager = Substitute.For<IConfigFileRemoteSettingsManager>();
        _manager.LoadRemotes(true).Returns(_ => _remotes);
        _manager.SaveRemote(default, default!, default!, default, default!, default, default).ReturnsForAnyArgs(new ConfigFileRemoteSaveResult(null, false));
        _manager.RemoveRemote(default!).ReturnsForAnyArgs("");
        _host = new FakeRemotesHost();
        _messageBoxes = new FakeMessageBoxes();
    }

    [Test]
    public void Lists_active_then_inactive_remotes_and_edits_the_first_active_one()
    {
        RemotesViewModel viewModel = Create();

        viewModel.Remotes.Select(r => (r.Name, r.GroupHeader)).Should().Equal(("origin", "Active"), ("upstream", null), ("old", "Inactive"));
        viewModel.SelectedRemote!.Name.Should().Be("origin");
        viewModel.ManagementHeader.Should().Be("Edit Remote Details");
        viewModel.RemoteName.Should().Be("origin");
        viewModel.Url.Should().Be("https://github.com/owner/repo.git");
        viewModel.IsSeparatePushUrl.Should().BeFalse();
        viewModel.UrlLabel.Should().Be("_Url");
        viewModel.UrlSuggestions.Should().Equal(_host.History);
    }

    [Test]
    public void Selecting_a_remote_shows_its_details_and_an_inactive_one_cannot_be_edited()
    {
        RemotesViewModel viewModel = Create(preselectRemote: "upstream");

        viewModel.IsSeparatePushUrl.Should().BeTrue();
        viewModel.UrlLabel.Should().Be("Fetch _Url");
        viewModel.PushUrl.Should().Be("git@github.com:upstream/repo.git");
        viewModel.Color.Should().Be("#FF0000");
        viewModel.IsManagementEnabled.Should().BeTrue();

        viewModel.SelectedRemote = viewModel.Remotes.Single(r => r.Name == "old");
        viewModel.IsManagementEnabled.Should().BeFalse();
        viewModel.ToggleStateTooltip.Should().Be("Activate the selected remote");
        viewModel.Color.Should().BeNull();
    }

    [Test]
    public void New_remote_with_an_existing_name_is_refused()
    {
        _manager.EnabledRemoteExists("origin").Returns(true);
        RemotesViewModel viewModel = Create();

        viewModel.NewCommand.Execute(null);
        viewModel.ManagementHeader.Should().Be("Create New Remote");
        viewModel.SaveCommand.CanExecute(null).Should().BeFalse();
        viewModel.RemoteName = "origin";
        viewModel.SaveCommand.Execute(null);

        _messageBoxes.Errors.Should().Equal("An active remote named \"origin\" already exists.");
        _manager.DidNotReceiveWithAnyArgs().SaveRemote(default, default!, default!, default, default!, default, default);
    }

    [Test]
    public void Saves_a_new_remote_then_fetches_it()
    {
        RemotesViewModel viewModel = Create();
        viewModel.NewCommand.Execute(null);
        viewModel.RemoteName = " fork ";
        viewModel.Url = "https://github.com/fork/repo.git ";
        viewModel.IsSeparatePushUrl = true;
        viewModel.PushUrl = "https://github.com/fork/repo.git";
        viewModel.Prefix = "f/";

        viewModel.SaveCommand.Execute(null);

        // A push URL equal to the URL is not separate.
        _manager.Received().SaveRemote(null, "fork", "https://github.com/fork/repo.git", null, "", null, "f/");
        _host.HistoryUpdates.Should().Equal((null, "https://github.com/fork/repo.git", null, null));
        _host.Saved.Should().Equal("fork");
        viewModel.IsSeparatePushUrl.Should().BeFalse();
    }

    [Test]
    public void Reports_the_message_of_a_failed_save()
    {
        _manager.SaveRemote(default, default!, default!, default, default!, default, default).ReturnsForAnyArgs(new ConfigFileRemoteSaveResult("fatal: bad url", false));
        RemotesViewModel viewModel = Create();

        viewModel.SaveCommand.Execute(null);

        _messageBoxes.Errors.Should().Equal("fatal: bad url");
        _host.Saved.Should().BeEmpty();
    }

    [Test]
    public void Deletes_after_confirmation_and_toggles_the_state()
    {
        RemotesViewModel viewModel = Create();

        _messageBoxes.ConfirmResult = false;
        viewModel.DeleteCommand.Execute(null);
        _manager.DidNotReceiveWithAnyArgs().RemoveRemote(default!);

        _messageBoxes.ConfirmResult = true;
        viewModel.DeleteCommand.Execute(null);
        _manager.Received().RemoveRemote(_remotes[0]);

        viewModel.SelectedRemote = viewModel.Remotes.Single(r => r.Name == "upstream");
        viewModel.ToggleStateCommand.Execute(null);
        _manager.Received().ToggleRemoteState("upstream", true);
        viewModel.SelectedRemote!.Name.Should().Be("upstream");
    }

    [Test]
    public void Suggests_urls_and_a_name_for_a_new_remote()
    {
        RemotesViewModel viewModel = Create();
        viewModel.NewCommand.Execute(null);

        viewModel.RemoteName = "alice";
        viewModel.SuggestUrls(push: false);
        viewModel.Url.Should().Be("https://github.com/alice/repo.git", "the single candidate is filled in for a specific remote name");
        viewModel.UrlSuggestions.Should().StartWith("https://github.com/alice/repo.git");

        viewModel.NewCommand.Execute(null);
        viewModel.Url = "https://github.com/bob/tool.git";
        viewModel.SuggestNameFromUrl();
        viewModel.RemoteName.Should().Be("bob");
    }

    [Test]
    public void Generic_remote_names_get_no_url_filled_in()
    {
        RemotesViewModel viewModel = Create();
        viewModel.NewCommand.Execute(null);

        viewModel.RemoteName = "origin";
        viewModel.SuggestUrls(push: false);

        viewModel.Url.Should().BeEmpty();
    }

    [Test]
    public void Normalises_the_prefix_and_picks_a_color()
    {
        RemotesViewModel viewModel = Create();
        viewModel.Prefix = "my prefix/";
        viewModel.NormalisePrefix();
        viewModel.Prefix.Should().Be("my_prefix/");

        _host.PickedColor = "#00FF00";
        viewModel.PickColorCommand.Execute(null);
        viewModel.Color.Should().Be("#00FF00");
        viewModel.ResetColorCommand.Execute(null);
        viewModel.HasColor.Should().BeFalse();
    }

    [Test]
    public void Pull_behavior_edits_the_tracking_of_the_selected_branch()
    {
        IGitRef main = Head("main", trackingRemote: "origin", mergeWith: "main");
        IGitRef feature = Head("feature");
        _host.Heads = [main, feature];

        RemotesViewModel viewModel = Create(preselectLocal: "main");

        viewModel.SelectedTabIndex.Should().Be(1);
        viewModel.Heads.Should().Equal(feature, main);
        viewModel.SelectedHead.Should().Be(main);
        viewModel.TrackingRemoteNames.Should().Equal("", "origin", "old", "upstream");
        viewModel.SelectedTrackingRemote.Should().Be("origin");
        viewModel.MergeWith.Should().Be("main");
        main.DidNotReceiveWithAnyArgs().TrackingRemote = default!;

        viewModel.SelectedTrackingRemote = "upstream";
        viewModel.MergeWith = "develop";
        main.Received().TrackingRemote = "upstream";
        main.Received().MergeWith = "develop";

        viewModel.LoadMergeWithCandidates();
        viewModel.MergeWithCandidates.Should().Equal("", "upstream/develop");
    }

    private RemotesViewModel Create(string? preselectRemote = null, string? preselectLocal = null)
    {
        RemotesViewModel viewModel = new(
            new RemotesStrings(),
            _manager,
            new FakeBranchNameNormaliser(),
            new GitBranchNameOptions("_"),
            isPuttyEnabled: false,
            showAdvancedOptions: true,
            [],
            _host,
            _messageBoxes,
            new FakeFileDialogs());
        viewModel.Initialize(preselectRemote, preselectLocal);
        return viewModel;
    }

    private static IGitRef Head(string name, string trackingRemote = "", string mergeWith = "")
    {
        IGitRef head = Substitute.For<IGitRef>();
        head.Name.Returns(name);
        head.LocalName.Returns(name);
        head.TrackingRemote.Returns(trackingRemote);
        head.MergeWith.Returns(mergeWith);
        return head;
    }

    internal sealed class FakeRemotesHost : IRemotesHost
    {
        public IReadOnlyList<string> History { get; } = ["https://example.org/a.git", "https://example.org/b.git"];

        public List<(string? OldUrl, string NewUrl, string? OldPushUrl, string? NewPushUrl)> HistoryUpdates { get; } = [];

        public List<string> Saved { get; } = [];

        public string? PickedColor { get; set; }

        public IReadOnlyList<IGitRef> Heads { get; set; } = [];

        public IReadOnlyList<string> LoadUrlHistory() => History;

        public void UpdateUrlHistory(string? oldUrl, string newUrl, string? oldPushUrl, string? newPushUrl)
            => HistoryUpdates.Add((oldUrl, newUrl, oldPushUrl, newPushUrl));

        public void OnRemoteSaved(string remoteName) => Saved.Add(remoteName);

        public string? PickColor(string? currentColor) => PickedColor;

        public string? BrowseSshKey(string filter, string title) => null;

        public void LoadSshKey(string keyFile)
        {
        }

        public void TestConnection(string url)
        {
        }

        public IReadOnlyList<IGitRef> LoadHeads() => Heads;

        public IReadOnlyList<string> GetMergeWithCandidates(string remoteName) => [$"{remoteName}/develop"];
    }
}
