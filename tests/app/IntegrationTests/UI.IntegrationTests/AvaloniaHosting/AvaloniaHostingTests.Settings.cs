using Avalonia.Threading;
using GitUI.Avalonia.CommandsDialogs.SettingsDialog;
using GitUI.CommandsDialogs.SettingsDialog.Pages;
using GitUI.Presentation.CommandsDialogs.SettingsDialog;
using GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 6: the settings dialog with its real pages.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void StartSettingsDialog_shows_every_page_and_cancels()
    {
        List<string> shown = [];
        DriveNextDialog(window =>
        {
            window.Should().BeOfType<SettingsWindow>();
            SettingsDialogViewModel viewModel = (SettingsDialogViewModel)window.DataContext!;
            viewModel.SelectedPage.Should().BeOfType<HotkeysSettingsPageViewModel>("the initial page");

            foreach (SettingsTreeNode node in viewModel.Nodes.SelectMany(n => n.DescendantsAndSelf()).ToList())
            {
                viewModel.SelectedNode = node;
                Dispatcher.UIThread.RunJobs();
                shown.Add(node.Page.PageName);
                ((SettingsWindow)window).PageContent.Content.Should().BeSameAs(node.Page);
            }

            viewModel.GotoPage("ChecklistSettingsPage");
            Dispatcher.UIThread.RunJobs();
            ChecklistSettingsPageViewModel checklist = (ChecklistSettingsPageViewModel)viewModel.SelectedPage!;
            checklist.Items.Single(i => i.Check == ChecklistCheck.GitFound).IsVisible.Should().BeTrue("git is checked");
            Capture(window, "settings-checklist");

            viewModel.GotoPage("ScriptsSettingsPage");
            Dispatcher.UIThread.RunJobs();
            ((ScriptsSettingsPageViewModel)viewModel.SelectedPage!).Icons.Should().NotBeEmpty("the images of the application");
            Capture(window, "settings-scripts");

            viewModel.CancelCommand.Execute(null);
        });

        _commands.StartSettingsDialog(_owner, new GitUI.CommandsDialogs.SettingsDialog.SettingsPageReferenceByName("HotkeysSettingsPage")).Should().BeFalse("cancelled");

        shown.Should().Contain(["ChecklistSettingsPage", "DetailedSettingsPage", "ScriptsSettingsPage", "HotkeysSettingsPage", "GitRootIntroductionPage", "PluginRootIntroductionPage"]);
    }
}
