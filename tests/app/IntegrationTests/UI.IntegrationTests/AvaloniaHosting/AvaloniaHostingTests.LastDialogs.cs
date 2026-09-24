using GitCommands;
using GitExtensions.Extensibility;
using GitUI;
using GitUI.Avalonia.Hosting;
using GitUI.AvaloniaHosting;
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
        // The owner: an Avalonia window, as the main window.
        DialogWindow browse = new() { Title = "Browse", Width = 600, Height = 400 };
        AvaloniaDialogHost.Show(browse, _owner.Handle);
        PumpUntil(() => browse.IsVisible);
        IWin32Window browseOwner = new WindowOwner(browse.NativeHandle).ToWin32Window()!;
        _referenceRepository.Module.GetCurrentCheckout();

        DialogWindow? shown = null;
        AvaloniaDialogHost.DialogShowingForTests = window => shown = window;
        try
        {
            AvaloniaDialogs.TryShowGitCommandLog(browseOwner);
            PumpUntil(() => shown?.IsVisible == true);

            shown.Should().NotBeNull();
            GitCommandLogViewModel viewModel = (GitCommandLogViewModel)shown!.DataContext!;
            PumpUntil(() => viewModel.LogItems.Count > 0);
            viewModel.LogItems.Should().NotBeEmpty("the commands of this process are logged");
            viewModel.SelectedLogItem.Should().Be(viewModel.LogItems[^1]);
            IsWindowEnabled(browse.NativeHandle).Should().BeTrue("the window is modeless");
            Capture(shown, "git-command-log");

            DialogWindow first = shown;
            shown = null;
            AvaloniaDialogs.TryShowGitCommandLog(browseOwner);
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
