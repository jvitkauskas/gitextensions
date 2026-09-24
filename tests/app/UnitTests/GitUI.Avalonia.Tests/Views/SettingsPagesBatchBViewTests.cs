using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.Threading;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.CommandsDialogs.SettingsDialog;
using GitUI.Avalonia.CommandsDialogs.SettingsDialog.Pages;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs.SettingsDialog;
using GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;
using static GitUI.AvaloniaTests.ViewModels.SettingsPagesBatchBViewModelTests;

namespace GitUI.AvaloniaTests.Views;

/// <summary>
///  Headless view tests of the ported settings pages of the Git settings, SSH, build server integration, revision links and
///  shell extension.
/// </summary>
[TestFixture]
public sealed class SettingsPagesBatchBViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        using DistributedSettingsFiles files = new();
        (SettingsDialogViewModel viewModel, FakePagesHost host) = Create(files);
        SettingsWindow window = new() { DataContext = viewModel };
        window.Show();
        viewModel.Open("GitSettingsPage");
        Dispatcher.UIThread.RunJobs();

        Capture<GitSettingsPageView>("GitSettingsPage", "git");
        Capture<GitConfigSettingsPageView>("GitConfigSettingsPage", "git-config");
        Capture<GitConfigAdvancedSettingsPageView>("GitConfigAdvancedSettingsPage", "git-config-advanced");
        Capture<SshSettingsPageView>("SshSettingsPage", "ssh");
        Capture<BuildServerIntegrationSettingsPageView>("BuildServerIntegrationSettingsPage", "build-server");
        ((RevisionLinksSettingsPageViewModel)viewModel.Pages.Single(p => p.PageName == "RevisionLinksSettingsPage")).Templates[0].Command.Execute(null);
        Capture<RevisionLinksSettingsPageView>("RevisionLinksSettingsPage", "revision-links");
        Capture<ShellExtensionSettingsPageView>("ShellExtensionSettingsPage", "shell-extension");
        host.Should().NotBeNull();
        window.Close();

        void Capture<TView>(string pageName, string name)
            where TView : Control
        {
            viewModel.GotoPage(pageName);
            if (viewModel.SelectedPage!.Levels.Any(l => l.Level == SettingsLevel.Local))
            {
                // The settings of the repository, editable.
                viewModel.SelectedPage.Level = SettingsLevel.Local;
            }

            Dispatcher.UIThread.RunJobs();
            window.PageContent.GetLogicalDescendants().OfType<TView>().Should().ContainSingle($"the page {pageName} is shown by its view");
            SaveScreenshot(window.CaptureRenderedFrame(), $"settings-{name}-{theme}");
        }
    });

    [Test]
    public Task A_click_on_a_menu_item_of_the_shell_extension_cycles_as_the_WinForms_list() => OnUiThreadAsync(() =>
    {
        using DistributedSettingsFiles files = new();
        (SettingsDialogViewModel viewModel, _) = Create(files);
        SettingsWindow window = new() { DataContext = viewModel };
        window.Show();
        viewModel.Open("ShellExtensionSettingsPage");
        Dispatcher.UIThread.RunJobs();
        ShellExtensionSettingsPageViewModel page = (ShellExtensionSettingsPageViewModel)viewModel.SelectedPage!;
        ShellMenuEntryCheckBox checkBox = window.PageContent.GetLogicalDescendants().OfType<ShellMenuEntryCheckBox>().First();
        page.MenuEntries[0].State = false;
        Dispatcher.UIThread.RunJobs();

        Point point = checkBox.TranslatePoint(new Point(10, checkBox.Bounds.Height / 2), window)!.Value;
        List<bool?> states = [];
        for (int i = 0; i < 3; i++)
        {
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            states.Add(page.MenuEntries[0].State);
        }

        states.Should().Equal([null, true, false], "unchecked becomes indeterminate (cascaded), then checked (top level)");
        window.Close();
    });

    [Test]
    public Task The_git_config_page_disables_the_fields_of_the_tools_not_set() => OnUiThreadAsync(() =>
    {
        using DistributedSettingsFiles files = new();
        (SettingsDialogViewModel viewModel, FakePagesHost host) = Create(files);
        host.CanFindGit = false;
        SettingsWindow window = new() { DataContext = viewModel };
        window.Show();
        viewModel.Open("GitConfigSettingsPage");
        GitConfigSettingsPageViewModel page = (GitConfigSettingsPageViewModel)viewModel.SelectedPage!;
        page.Level = SettingsLevel.Local;
        page.DiffTool = "";
        Dispatcher.UIThread.RunJobs();

        GitConfigSettingsPageView view = window.PageContent.GetLogicalDescendants().OfType<GitConfigSettingsPageView>().Single();
        view.FindControl<Border>("invalidGitPath")!.IsVisible.Should().BeTrue("git does not run");
        view.FindControl<TextBox>("userName")!.IsEnabled.Should().BeFalse();
        view.FindControl<TextBox>("diffToolPath")!.IsEnabled.Should().BeFalse("no diff tool is set");
        window.Close();
    });

    private static (SettingsDialogViewModel ViewModel, FakePagesHost Host) Create(DistributedSettingsFiles files)
    {
        (SettingsDialogViewModel viewModel, _, SettingsDialogViewModelTests.FakeSources sources) = SettingsDialogViewModelTests.Create();
        FakePagesHost host = new()
        {
            GitEnvironment = (null, @"C:\Users\user"),
            Remotes = [new Remote("origin", "https://github.com/gitextensions/gitextensions.git", "https://github.com/gitextensions/gitextensions.git")],
        };
        host.BuildServerTypes.SetResult(["AppVeyor", "Azure DevOps", "Jenkins"]);
        GitConfigSources gitConfig = new();
        gitConfig.Local.SetValue("user.name", "John Doe");
        gitConfig.Local.SetValue("user.email", "john@example.com");
        gitConfig.Local.SetValue("core.autocrlf", "true");
        gitConfig.Local.SetValue("pull.rebase", "true");
        SmallDialogViewModelTests.FakeFileDialogs fileDialogs = new();

        const string gitExtensions = "GitExtensionsSettingsGroup";
        viewModel.AddPage(new RevisionLinksSettingsPageViewModel(new RevisionLinksSettingsPageStrings(), host), gitExtensions, "Link", files.Levels);
        viewModel.AddPage(new BuildServerIntegrationSettingsPageViewModel(new BuildServerIntegrationSettingsPageStrings(), host), gitExtensions, "Integration", sources.Distributed);
        viewModel.AddPage(new ShellExtensionSettingsPageViewModel(new ShellExtensionSettingsPageStrings(), host), gitExtensions, "ShellExtensions", sources.GlobalOnly);
        viewModel.AddPage(new SshSettingsPageViewModel(new SshSettingsPageStrings(), host, fileDialogs), gitExtensions, "Key", sources.GlobalOnly);
        const string git = "GitSettingsGroup";
        viewModel.AddPage(new GitSettingsPageViewModel(new GitSettingsPageStrings(), host, fileDialogs), git, "FolderOpen", sources.GlobalOnly);
        viewModel.AddPage(new GitConfigSettingsPageViewModel(new GitConfigSettingsPageStrings(), host, fileDialogs, workingDir: null), git, "GeneralSettings", gitConfig.Levels);
        viewModel.AddPage(new GitConfigAdvancedSettingsPageViewModel(new GitConfigAdvancedSettingsPageStrings(), host, supportsUpdateRefs: true), git, "AdvancedSettings", gitConfig.Levels);
        return (viewModel, host);
    }
}
