using GitUI.Presentation.CommandsDialogs.BrowseDialog;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the git command log (port of <c>FormGitCommandLog</c>).</summary>
[TestFixture]
public sealed class GitCommandLogViewModelTests
{
    [Test]
    public void Start_lists_the_commands_with_the_last_one_selected()
    {
        FakeHost host = new();
        host.Add("status");
        host.Add("log");
        GitCommandLogViewModel viewModel = new(new GitCommandLogStrings(), host);

        viewModel.LogItems.Should().BeEmpty("the commands are listed once the window is shown");
        viewModel.Start();

        viewModel.LogItems.Select(i => i.ColumnLine).Should().Equal("git status", "git log");
        viewModel.SelectedLogItem!.ColumnLine.Should().Be("git log");
        viewModel.LogOutput.Should().Be("details of log");
        viewModel.CacheItems.Should().BeEmpty("only the visible tab is refreshed");
    }

    [Test]
    public void A_new_command_is_selected_if_the_last_one_was_and_the_selection_is_kept_otherwise()
    {
        FakeHost host = new();
        host.Add("status");
        host.Add("log");
        GitCommandLogViewModel viewModel = new(new GitCommandLogStrings(), host);
        viewModel.Start();

        host.Add("diff");
        viewModel.SelectedLogItem!.ColumnLine.Should().Be("git diff");

        viewModel.SelectedLogItem = viewModel.LogItems[0];
        host.Add("fetch");
        viewModel.LogItems.Should().HaveCount(4);
        viewModel.SelectedLogItem!.ColumnLine.Should().Be("git status");
        viewModel.LogOutput.Should().Be("details of status");
    }

    [Test]
    public void The_command_cache_is_listed_on_its_tab_with_its_printable_output()
    {
        FakeHost host = new();
        host.Cache["-c core.quotepath=false ls-files"] = ("a.txt\tb c\n", "");
        GitCommandLogViewModel viewModel = new(new GitCommandLogStrings(), host);
        viewModel.Start();

        viewModel.SelectedTabIndex = GitCommandLogViewModel.CommandCacheTabIndex;

        viewModel.CacheItems.Select(i => i.DisplayString).Should().Equal("ls-files");
        viewModel.SelectedCacheItem!.Key.Should().Be("-c core.quotepath=false ls-files");
        viewModel.CacheOutput.Should().Be(
            "-c core.quotepath=false ls-files\n-------------------------------------\n\na.txt»b·c\\n\n\n-------------------------------------\n\n");

        viewModel.ClearCacheCommand.Execute(null);
        host.Actions.Should().Equal("clear cache");
        viewModel.CacheItems.Should().BeEmpty();
    }

    [Test]
    public void PrintableChars_shows_the_control_characters()
        => GitCommandLogViewModel.PrintableChars("a\0b\r\n\t \u001b").Should().Be("a\\0b\\r\\n\n»·\\x1b");

    [Test]
    public void The_menu_commands_and_the_options_use_the_host()
    {
        FakeHost host = new();
        host.Add("status");
        GitCommandLogViewModel viewModel = new(new GitCommandLogStrings(), host);
        viewModel.Start();

        viewModel.CopyCommandLineCommand.Execute(null);
        viewModel.SaveToFileCommand.Execute(null);
        viewModel.ClearLogCommand.Execute(null);
        viewModel.CaptureCallStacks.Should().BeFalse();
        viewModel.CaptureCallStacks = true;

        host.Actions.Should().Equal("copy \"git.exe\" status", "save", "clear log");
        host.CaptureCallStacks.Should().BeTrue();
        viewModel.LogItems.Should().BeEmpty();
    }

    [Test]
    public void Dispose_stops_following_the_log()
    {
        FakeHost host = new();
        GitCommandLogViewModel viewModel = new(new GitCommandLogStrings(), host);
        viewModel.Start();
        host.Subscribers.Should().Be(2);

        viewModel.Dispose();

        host.Subscribers.Should().Be(0);
        host.Add("status");
        viewModel.LogItems.Should().BeEmpty();
    }

    internal sealed class FakeHost : IGitCommandLogHost
    {
        private readonly List<GitCommandLogItem> _commands = [];
        private EventHandler? _commandsChanged;
        private EventHandler? _cacheChanged;

        public event EventHandler? CommandsChanged
        {
            add => _commandsChanged += value;
            remove => _commandsChanged -= value;
        }

        public event EventHandler? CacheChanged
        {
            add => _cacheChanged += value;
            remove => _cacheChanged -= value;
        }

        public int Subscribers => (_commandsChanged?.GetInvocationList().Length ?? 0) + (_cacheChanged?.GetInvocationList().Length ?? 0);

        public Dictionary<string, (string Output, string Error)> Cache { get; } = [];

        public List<string> Actions { get; } = [];

        public bool CaptureCallStacks { get; set; }

        /// <summary>Logs a command, as <c>CommandLog.LogProcessStart</c>.</summary>
        public void Add(string arguments)
        {
            _commands.Add(new GitCommandLogItem($"git {arguments}", $"details of {arguments}", $"\"git.exe\" {arguments}"));
            _commandsChanged?.Invoke(this, EventArgs.Empty);
        }

        public IReadOnlyList<GitCommandLogItem> GetCommands() => [.. _commands];

        public IReadOnlyList<GitCommandCacheItem> GetCachedCommands() => [.. Cache.Keys.Select(key => new GitCommandCacheItem(key, key.Replace("-c core.quotepath=false ", "")))];

        public bool TryGetCachedOutput(string key, out string? output, out string? error)
        {
            bool found = Cache.TryGetValue(key, out (string Output, string Error) cached);
            (output, error) = cached;
            return found;
        }

        public void ClearCommands()
        {
            Actions.Add("clear log");
            _commands.Clear();
            _commandsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ClearCache()
        {
            Actions.Add("clear cache");
            Cache.Clear();
        }

        public void SaveCommandsToFile() => Actions.Add("save");

        public void CopyToClipboard(string text) => Actions.Add($"copy {text}");
    }
}
