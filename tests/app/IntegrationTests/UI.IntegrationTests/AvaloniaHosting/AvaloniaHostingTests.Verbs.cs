using CommonTestUtils;
using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.CommandsDialogs.BrowseDialog;
using GitUI.Avalonia.CommandsDialogs.CommitDialog;
using GitUI.Avalonia.CommandsDialogs.SettingsDialog;
using GitUI.Avalonia.HelperDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>The verbs of the command line (<c>RunCommandBasedOnArgument</c>) show their Avalonia windows.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void RunCommandBasedOnArgument_should_throw_on_null_args()
    {
        ((Action)(() => _commands.GetTestAccessor().RunCommandBasedOnArgument(null!)))
            .Should().Throw<NullReferenceException>();
    }

    [Test]
    public void RunCommandBasedOnArgument_should_throw_on_empty_args()
    {
        ((Action)(() => _commands.GetTestAccessor().RunCommandBasedOnArgument([])))
            .Should().Throw<ArgumentOutOfRangeException>();

        ((Action)(() => _commands.GetTestAccessor().RunCommandBasedOnArgument(["ge.exe"])))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void RunCommandBasedOnArgument_about()
        => RunVerb<AboutWindow>(["ge.exe", "about"]);

    [TestCase("add")]
    [TestCase("addfiles")]
    public void RunCommandBasedOnArgument_add(string command)
        => RunVerb<AddFilesWindow>(["ge.exe", command]);

    [TestCase("apply")]
    [TestCase("applypatch")]
    public void RunCommandBasedOnArgument_apply(string command)
        => RunVerb<ApplyPatchWindow>(["ge.exe", command]);

    [Test]
    public void RunCommandBasedOnArgument_blame_throws()
    {
        ((Action)(() => _commands.GetTestAccessor().RunCommandBasedOnArgument(["ge.exe", "blame"])))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void RunCommandBasedOnArgument_blame()
        => RunVerb<BlameWindow>(["ge.exe", "blame", "filename"]);

    [Test]
    public void RunCommandBasedOnArgument_branch()
        => RunVerb<CreateBranchWindow>(["ge.exe", "branch"]);

    [Test]
    public void RunCommandBasedOnArgument_browse()
    {
        ObjectId selected = ObjectId.Random();
        ObjectId first = ObjectId.Random();
        ObjectId otherIgnored = ObjectId.Random();
        RunVerb<BrowseWindow>(["ge.exe", "browse", $"-commit={selected},{first},,{otherIgnored}"]);
    }

    [TestCase("checkout")]
    [TestCase("checkoutbranch")]
    public void RunCommandBasedOnArgument_checkout(string command)
        => RunVerb<CheckoutBranchWindow>(["ge.exe", command], expectedResult: false);

    [Test]
    public void RunCommandBasedOnArgument_checkoutrevision()
        => RunVerb<CheckoutRevisionWindow>(["ge.exe", "checkoutrevision"], expectedResult: false);

    [Test]
    public void RunCommandBasedOnArgument_cherry()
        => RunVerb<CherryPickWindow>(["ge.exe", "cherry"], expectedResult: false);

    [Test]
    public void RunCommandBasedOnArgument_cleanup()
        => RunVerb<CleanupRepositoryWindow>(["ge.exe", "cleanup"]);

    [Test]
    public void RunCommandBasedOnArgument_clone()
        => RunVerb<CloneWindow>(["ge.exe", "clone"]);

    [Test]
    public void RunCommandBasedOnArgument_commit()
        => RunVerb<CommitWindow>(["ge.exe", "commit"]);

    [Test]
    public void RunCommandBasedOnArgument_difftool_returns_false_on_missing_argument()
    {
        _commands.GetTestAccessor().RunCommandBasedOnArgument(["ge.exe", "difftool"])
            .Should().BeFalse();
    }

    [Test]
    public void RunCommandBasedOnArgument_difftool()
    {
        ConfigureCmdAsDiffAndMergeTool();
        _commands.GetTestAccessor().RunCommandBasedOnArgument(["ge.exe", "difftool", "filename"]).Should().BeTrue();
    }

    [Test]
    public void RunCommandBasedOnArgument_history_throws(
        [Values("blamehistory", "filehistory")] string command)
    {
        ((Action)(() => _commands.GetTestAccessor().RunCommandBasedOnArgument(["ge.exe", command])))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void RunCommandBasedOnArgument_history(
        [Values("blamehistory", "filehistory")] string command,
        [Values(false, true)] bool commit,
        [Values(false, true)] bool filter)
    {
        FileHistoryTab expectedTab = command.Contains("blame") ? FileHistoryTab.Blame : FileHistoryTab.Diff;

        List<string> args = ["ge.exe", command, "filename"];
        if (commit)
        {
            args.Add(_referenceRepository.CommitHash!);
            if (filter)
            {
                args.Add("--filter-by-revision");
            }
        }

        bool useBrowse = AppSettings.UseBrowseForFileHistory.Value;
        AppSettings.UseBrowseForFileHistory.Value = false;
        try
        {
            RunVerb<FileHistoryWindow>([.. args], drive: window => ((FileHistoryViewModel)window.DataContext!).SelectedTab.Should().Be(expectedTab));
        }
        finally
        {
            AppSettings.UseBrowseForFileHistory.Value = useBrowse;
        }
    }

    [Test]
    public void RunCommandBasedOnArgument_history_returns_false(
        [Values("blamehistory", "filehistory")] string command,
        [Values(0, 1, 2, 3)] int invalidVariant)
    {
        const bool ignored = false;
        (bool commitValid, bool filter, bool filterValid)[] invalidVariants =
        [
            (false, false, ignored),
            (false, true, false),
            (false, true, true),
            (true, true, false)
        ];
        (bool commitValid, bool filter, bool filterValid) = invalidVariants[invalidVariant];

        List<string> args = ["ge.exe", command, "filename", commitValid ? _referenceRepository.CommitHash! : "no-commit"];
        if (filter)
        {
            args.Add(filterValid ? "--filter-by-revision" : "invalid");
        }

        _commands.GetTestAccessor().RunCommandBasedOnArgument([.. args]).Should().BeFalse();
    }

    [Test]
    public void RunCommandBasedOnArgument_fileeditor_throws()
    {
        ((Action)(() => _commands.GetTestAccessor().RunCommandBasedOnArgument(["ge.exe", "fileeditor"])))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void RunCommandBasedOnArgument_fileeditor()
    {
        File.WriteAllText(Path.Join(_referenceRepository.Module.WorkingDir, "filename"), "text");
        RunVerb<FileEditorWindow>(["ge.exe", "fileeditor", "filename"]);
    }

    [Test]
    public void RunCommandBasedOnArgument_formatpatch()
        => RunVerb<FormatPatchWindow>(["ge.exe", "formatpatch"]);

    [Test]
    public void RunCommandBasedOnArgument_gitignore()
        => RunVerb<GitIgnoreEditorWindow>(["ge.exe", "gitignore"]);

    [Test]
    public void RunCommandBasedOnArgument_init()
        => RunVerb<InitWindow>(["ge.exe", "init"]);

    [Test]
    public void RunCommandBasedOnArgument_merge()
        => RunVerb<MergeBranchWindow>(["ge.exe", "merge"]);

    [TestCase("mergeconflicts")]
    [TestCase("mergetool")]
    public void RunCommandBasedOnArgument_mergeconflicts(string command)
    {
        ConfigureCmdAsDiffAndMergeTool();
        RunVerb<ResolveConflictsWindow>(["ge.exe", command]);
    }

    [Test]
    public void RunCommandBasedOnArgument_openrepo()
        => RunVerb<BrowseWindow>(["ge.exe", "openrepo"]);

    [Test]
    public void RunCommandBasedOnArgument_pull()
        => RunVerb<PullWindow>(["ge.exe", "pull"], expectedResult: false);

    [Test]
    public void RunCommandBasedOnArgument_push()
        => RunVerb<PushWindow>(["ge.exe", "push"], expectedResult: false);

    [Test]
    public void RunCommandBasedOnArgument_rebase()
        => RunVerb<RebaseWindow>(["ge.exe", "rebase"]);

    [Test]
    public void RunCommandBasedOnArgument_remotes()
        => RunVerb<RemotesWindow>(["ge.exe", "remotes"]);

    [TestCase("revert")]
    [TestCase("reset")]
    public void RunCommandBasedOnArgument_reset(string command)
        => RunVerb<ResetChangesWindow>(["ge.exe", command], expectedResult: false);

    [Test]
    public void RunCommandBasedOnArgument_searchfile()
        => RunVerb<SearchWindow>(["ge.exe", "searchfile"], expectedResult: false);

    [Test]
    public void RunCommandBasedOnArgument_settings()
        => RunVerb<SettingsWindow>(["ge.exe", "settings"], expectedResult: false /* because dialog is not closed using OK button */);

    [Test]
    public void RunCommandBasedOnArgument_stash()
        => RunVerb<StashWindow>(["ge.exe", "stash"]);

    [Test]
    public void RunCommandBasedOnArgument_synchronize()
    {
        List<Type> shown = [];
        void Close(DialogWindow window)
        {
            shown.Add(window.GetType());
            window.Close();
        }

        DriveDialogs(Close, Close, Close);

        _commands.GetTestAccessor().RunCommandBasedOnArgument(["ge.exe", "synchronize"]).Should().BeFalse();

        shown.Should().Equal(typeof(CommitWindow), typeof(PullWindow), typeof(PushWindow));
    }

    [Test]
    public void RunCommandBasedOnArgument_tag()
        => RunVerb<CreateTagWindow>(["ge.exe", "tag"], expectedResult: false);

    [Test]
    public void RunCommandBasedOnArgument_viewdiff()
        => RunVerb<LogWindow>(["ge.exe", "viewdiff"], expectedResult: false);

    [TestCase("git://")]
    [TestCase("http://")]
    [TestCase("https://")]
    [TestCase("github-windows://openRepo/")]
    [TestCase("github-mac://openRepo/")]
    public void RunCommandBasedOnArgument_Url(string url)
        => RunVerb<CloneWindow>(["ge.exe", url]);

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("help")]
    [TestCase("nonsense")]
    public void RunCommandBasedOnArgument_unsupported(string command)
        => RunVerb<CommandlineHelpWindow>(["ge.exe", command]);

    /// <summary>
    ///  Runs the verb of <paramref name="args"/>, which is to show a <typeparamref name="TWindow"/> (modal or modeless); the
    ///  window is closed (after <paramref name="drive"/>, if any).
    /// </summary>
    private void RunVerb<TWindow>(string[] args, bool? expectedResult = null, Action<DialogWindow>? drive = null)
        where TWindow : DialogWindow
    {
        Type? shown = null;
        bool closed = false;
        DriveNextDialog(window =>
        {
            shown = window.GetType();
            try
            {
                drive?.Invoke(window);
            }
            finally
            {
                window.Close();
                closed = true;
            }
        });

        bool result = _commands.GetTestAccessor().RunCommandBasedOnArgument(args);

        // A modeless window (e.g. the file history) is still open.
        PumpUntil(() => closed);

        shown.Should().Be(typeof(TWindow));
        if (expectedResult is bool expected)
        {
            result.Should().Be(expected);
        }
    }

    /// <summary>The diff and merge tool of the repository: cmd, which starts (and ends) without a window.</summary>
    private void ConfigureCmdAsDiffAndMergeTool()
    {
        string cmdPath = (Environment.GetEnvironmentVariable("COMSPEC") ?? "C:/WINDOWS/system32/cmd.exe").ToPosixPath().QuoteNE();
        _referenceRepository.Module.GitExecutable.RunCommand($"config --local difftool.cmd.path {cmdPath}").Should().BeTrue();
        _referenceRepository.Module.GitExecutable.RunCommand($"config --local mergetool.cmd.path {cmdPath}").Should().BeTrue();
        _referenceRepository.Module.GitExecutable.RunCommand("config --local diff.guitool cmd").Should().BeTrue();
        _referenceRepository.Module.GitExecutable.RunCommand("config --local merge.guitool cmd").Should().BeTrue();
    }
}
