using System.ComponentModel.Design;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI;
using GitUI.Avalonia.CommandsDialogs.BrowseDialog;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.ScriptsEngine;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUI.ScriptsEngine;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 7: the menu of the files of the diff tab of the Avalonia main window, on a real repository.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void The_diff_tab_menu_stages_and_unstages_a_file_of_the_working_directory()
    {
        Environment.SetEnvironmentVariable(AvaloniaUi.EnvironmentVariable, "all,FormBrowse");
        _referenceRepository.CreateCommit("Second commit", "committed content", "file.txt");
        File.WriteAllText(Path.Combine(_referenceRepository.Module.WorkingDir, "file.txt"), "changed content");

        bool? stageShown = null;
        bool? unstageShown = null;
        List<string> stagedFiles = [];
        List<string> unstagedAfterUnstage = [];
        bool closed = false;
        DriveNextDialog(window =>
        {
            window.Closed += (_, _) => closed = true;
            BrowseViewModel viewModel = (BrowseViewModel)window.DataContext!;
            FileStatusListViewModel files = viewModel.Files;
            ((BrowseWindow)window).Tabs.SelectedIndex = (int)BrowseTab.Diff;
            WaitUntil(
                () => viewModel.Grid.Rows.Any(row => row.ObjectId == ObjectId.WorkTreeId) && viewModel.Grid.SelectedRow is not null,
                () =>
                {
                    viewModel.Grid.SelectRevision(ObjectId.WorkTreeId).Should().BeTrue();
                    WaitUntil(() => files.AllEntries.Any(e => e.Item.Name == "file.txt"), StageAndUnstage);
                });

            void StageAndUnstage()
            {
                // Stage selected (the menu item and its hotkey command), in the working directory.
                files.Select(e => e.Item.Name == "file.txt");
                files.UpdateMenuState();
                stageShown = files.MenuState.ShowStage;
                Capture(window, "browse-diff-stage");
                viewModel.ExecuteRevisionDiffCommand(RevisionDiffHotkeyCommand.StageSelectedFile, fileTree: false).Should().BeTrue();

                // As RequestRefresh: the working directory no longer has the change.
                WaitUntil(
                    () => !files.AllEntries.Any(e => e.Item.Name == "file.txt"),
                    () =>
                    {
                        stagedFiles.AddRange(_referenceRepository.Module.GetIndexFiles().Select(item => item.Name));

                        // Unstage selected, in the index.
                        viewModel.Grid.SelectRevision(ObjectId.IndexId).Should().BeTrue();
                        WaitUntil(
                            () => files.AllEntries.Any(e => e.Item.Name == "file.txt" && e.SecondRevision.ObjectId == ObjectId.IndexId),
                            () =>
                            {
                                files.Select(e => e.Item.Name == "file.txt");
                                files.UpdateMenuState();
                                unstageShown = files.MenuState.ShowUnstage;
                                files.UnstageFilesCommand.Execute(null);
                                WaitUntil(
                                    () => !files.AllEntries.Any(e => e.Item.Name == "file.txt"),
                                    () =>
                                    {
                                        unstagedAfterUnstage.AddRange(_referenceRepository.Module.GetWorkTreeFiles().Select(item => item.Name));
                                        window.Close();
                                    });
                            });
                    });
            }
        });

        _commands.StartBrowseDialog(_owner, new BrowseArguments()).Should().BeTrue();

        WaitForMainWindowToClose(() => closed, seconds: 60);
        stageShown.Should().BeTrue("the file of the working directory can be staged");
        stagedFiles.Should().Contain("file.txt");
        unstageShown.Should().BeTrue("the file of the index can be unstaged");
        unstagedAfterUnstage.Should().Contain("file.txt");
        _referenceRepository.Module.GetIndexFiles().Should().BeEmpty();
    }

    [Test]
    public void The_diff_tab_menu_renames_a_file_of_a_commit_with_the_prompt()
    {
        Environment.SetEnvironmentVariable(AvaloniaUi.EnvironmentVariable, "all,FormBrowse");
        _referenceRepository.CreateCommit("Second commit", "committed content", "file.txt");
        string head = _referenceRepository.CommitHash!;

        string? defaultName = null;
        bool? moveShown = null;
        bool closed = false;
        DriveDialogs(
            window =>
            {
                window.Closed += (_, _) => closed = true;
                BrowseViewModel viewModel = (BrowseViewModel)window.DataContext!;
                FileStatusListViewModel files = viewModel.Files;
                ((BrowseWindow)window).Tabs.SelectedIndex = (int)BrowseTab.Diff;
                WaitUntil(
                    () => viewModel.Grid.SelectedRow?.ObjectId.ToString() == head && files.AllEntries.Any(e => e.Item.Name == "file.txt"),
                    () =>
                    {
                        files.Select(e => e.Item.Name == "file.txt");
                        files.UpdateMenuState();
                        moveShown = files.MenuState.ShowMove;

                        // As Move_Click: the prompt (driven below) asks the new name, then git mv.
                        viewModel.ExecuteRevisionDiffCommand(RevisionDiffHotkeyCommand.RenameMove, fileTree: false).Should().BeTrue();
                        window.Close();
                    });
            },
            prompt =>
            {
                SimplePromptViewModel viewModel = (SimplePromptViewModel)prompt.DataContext!;
                defaultName = viewModel.Input;
                viewModel.Input = "folder/renamed.txt";
                viewModel.OkCommand.Execute(null);
            });

        // The prompt of the application (the mock services have none).
        ServiceContainer services = GlobalServiceContainer.CreateDefaultMockServiceContainer();
        services.AddService<ISimplePromptCreator>(new SimplePromptCreator());
        GitUICommands commands = new(services, _referenceRepository.Module);
        commands.StartBrowseDialog(_owner, new BrowseArguments { SelectedId = ObjectId.Parse(head) }).Should().BeTrue();

        WaitForMainWindowToClose(() => closed, seconds: 60);
        moveShown.Should().BeTrue();
        defaultName.Should().Be("file.txt");
        string workingDir = _referenceRepository.Module.WorkingDir;
        File.Exists(Path.Combine(workingDir, "file.txt")).Should().BeFalse();
        File.Exists(Path.Combine(workingDir, "folder", "renamed.txt")).Should().BeTrue();
        _referenceRepository.Module.GetIndexFiles().Should().Contain(item => item.Name == "folder/renamed.txt");
    }
}
