using GitCommands;
using GitUI;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs.BrowseDialog;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs.BrowseDialog;
using GitUI.Presentation.Editor;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUI.ScriptsEngine;
using GitUI.UserControls;
using NSubstitute;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>
///  The last small dialogs: the git command log, the git grep prompt of the file status list, the quick picker of the
///  revision grid and the log window of the <c>viewdiff</c> verb, through their WinForms entry points.
/// </summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void Git_command_log_is_modeless_single_and_closed_with_its_owner()
    {
        using Form browse = new() { Text = "Browse", Width = 600, Height = 400 };
        browse.Show(_owner);
        Application.DoEvents();
        _referenceRepository.Module.GetCurrentCheckout();

        DialogWindow? shown = null;
        AvaloniaDialogHost.DialogShowingForTests = window => shown = window;
        try
        {
            FormGitCommandLog.ShowOrActivate(browse);
            PumpUntil(() => shown?.IsVisible == true);

            shown.Should().NotBeNull();
            GitCommandLogViewModel viewModel = (GitCommandLogViewModel)shown!.DataContext!;
            PumpUntil(() => viewModel.LogItems.Count > 0);
            viewModel.LogItems.Should().NotBeEmpty("the commands of this process are logged");
            viewModel.SelectedLogItem.Should().Be(viewModel.LogItems[^1]);
            IsWindowEnabled(browse.Handle).Should().BeTrue("the window is modeless");
            Capture(shown, "git-command-log");

            DialogWindow first = shown;
            shown = null;
            FormGitCommandLog.ShowOrActivate(browse);
            shown.Should().BeNull("the open window is activated rather than a new one shown");

            bool closed = false;
            first.Closed += (_, _) => closed = true;
            browse.Close();
            PumpUntil(() => closed);
            closed.Should().BeTrue();
        }
        finally
        {
            AvaloniaDialogHost.DialogShowingForTests = null;
        }
    }

    [Test]
    public void Git_grep_prompt_searches_the_file_status_list_and_is_reused()
    {
        string userArguments = AppSettings.GitGrepUserArguments.Value;
        bool ignoreCase = AppSettings.GitGrepIgnoreCase.Value;
        bool matchWholeWord = AppSettings.GitGrepMatchWholeWord.Value;
        bool showSearchBox = AppSettings.ShowFindInCommitFilesGitGrep.Value;
        IGitUICommandsSource commandsSource = Substitute.For<IGitUICommandsSource>();
        commandsSource.UICommands.Returns(_ => _commands);
        using Form form = new() { Text = "Diff", Width = 600, Height = 400 };
        using FileStatusList fileStatusList = new() { Parent = form, Dock = DockStyle.Fill, UICommandsSource = commandsSource, CanUseFindInCommitFilesGitGrep = true };
        form.Show(_owner);
        Application.DoEvents();

        DialogWindow? shown = null;
        AvaloniaDialogHost.DialogShowingForTests = window => shown = window;
        try
        {
            fileStatusList.ShowFindInCommitFileGitGrepDialog("TODO");
            PumpUntil(() => shown?.IsVisible == true);

            shown.Should().NotBeNull();
            FindInCommitFilesGitGrepViewModel viewModel = (FindInCommitFilesGitGrepViewModel)shown!.DataContext!;
            viewModel.Expression.Should().Be("TODO");
            viewModel.MatchCase.Should().Be(!AppSettings.GitGrepIgnoreCase.Value);
            Capture(shown, "find-in-commit-files-git-grep");

            viewModel.SearchCommand.Execute(null);
            fileStatusList.FindInCommitFilesGitGrepActive.Should().BeTrue("the list searches the expression");

            DialogWindow first = shown;
            shown = null;
            fileStatusList.ShowFindInCommitFileGitGrepDialog("FIXME");
            shown.Should().BeNull("the open prompt is reused");
            viewModel.Expression.Should().Be("FIXME");

            bool closed = false;
            first.Closed += (_, _) => closed = true;
            first.Close();
            PumpUntil(() => closed);
            closed.Should().BeTrue();
        }
        finally
        {
            AvaloniaDialogHost.DialogShowingForTests = null;
            AppSettings.GitGrepUserArguments.Value = userArguments;
            AppSettings.GitGrepIgnoreCase.Value = ignoreCase;
            AppSettings.GitGrepMatchWholeWord.Value = matchWholeWord;
            AppSettings.ShowFindInCommitFilesGitGrep.Value = showSearchBox;
        }
    }

    [Test]
    public void Script_option_with_several_tags_asks_with_the_quick_picker()
    {
        _referenceRepository.CreateTag("v1.0", _referenceRepository.CommitHash!);
        _referenceRepository.CreateTag("v1.1", _referenceRepository.CommitHash!);
        IScriptOptionsProvider optionsProvider = Substitute.For<IScriptOptionsProvider>();
        optionsProvider.Options.Returns(Array.Empty<string>());

        List<string> labels = [];
        DriveNextDialog(window =>
        {
            QuickItemSelectorViewModel viewModel = (QuickItemSelectorViewModel)window.DataContext!;
            labels.AddRange(viewModel.Items.Select(i => i.Label));
            Capture(window, "quick-ref-selector");
            viewModel.SelectedItem = viewModel.Items[^1];
            viewModel.AcceptCommand.Execute(null);
        });

        (string? arguments, bool abort) = ScriptOptionsParser.Parse("--tag={cTag}", _commands, _owner, optionsProvider);

        abort.Should().BeFalse();
        arguments.Should().Be("--tag=v1.1");
        labels.Should().HaveCount(3).And.EndWith(new[] { "v1.0", "v1.1" });
    }

    [Test]
    public void Compare_revisions_dialog_shows_the_grid_the_files_and_the_diff()
    {
        _referenceRepository.CreateCommit("Change A", "changed content of A", "A.txt");

        List<string> files = [];
        string? diff = null;
        int rows = 0;
        DriveNextDialog(window => WaitUntil(
            () => window.DataContext is LogViewModel { Viewer.Editor.Text.Length: > 0 } model && model.Files.AllEntries.Any(),
            () =>
            {
                try
                {
                    LogViewModel viewModel = (LogViewModel)window.DataContext!;
                    rows = viewModel.Grid.Rows.Count;
                    files.AddRange(viewModel.Files.AllEntries.Select(e => e.Item.Name));
                    diff = viewModel.Viewer.Editor.Text;
                    Capture(window, "log");
                }
                finally
                {
                    window.Close();
                }
            }));

        _commands.StartCompareRevisionsDialog(_owner).Should().BeFalse("FormLog is never accepted");

        rows.Should().BeGreaterThanOrEqualTo(2);
        files.Should().Equal("A.txt");
        diff.Should().Contain("+changed content of A");
    }
}
