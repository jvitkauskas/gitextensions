using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.Threading;
using GitUI.Avalonia.CommandsDialogs.SettingsDialog;
using GitUI.Avalonia.CommandsDialogs.SettingsDialog.Pages;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs.SettingsDialog;
using GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the settings dialog.</summary>
[TestFixture]
public sealed class SettingsViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        (SettingsDialogViewModel viewModel, _, _) = SettingsDialogViewModelTests.Create();
        SettingsWindow window = new() { DataContext = viewModel };
        window.Show();
        viewModel.Open("DetailedSettingsPage");
        Dispatcher.UIThread.RunJobs();

        window.Title.Should().Be("Settings - Detailed");
        window.PageContent.GetLogicalDescendants().OfType<DetailedSettingsPageView>().Should().ContainSingle("the page is shown by its view");
        window.GetLogicalDescendants().OfType<RadioButton>().Select(r => r.Content).Should()
            .Equal("Effective", "Local for current repository", "Distributed with current repository", "Global for all repositories");
        window.PageContent.IsEnabled.Should().BeFalse("the effective settings are read-only");
        SaveScreenshot(window.CaptureRenderedFrame(), $"settings-{theme}");

        ((DetailedSettingsPageViewModel)viewModel.SelectedPage!).Level = SettingsLevel.Local;
        Dispatcher.UIThread.RunJobs();
        window.PageContent.IsEnabled.Should().BeTrue();
        SaveScreenshot(window.CaptureRenderedFrame(), $"settings-local-{theme}");

        viewModel.GotoPage("GitSettingsGroup");
        Dispatcher.UIThread.RunJobs();
        window.PageContent.GetLogicalDescendants().OfType<IntroductionSettingsPageView>().Should().ContainSingle();
        window.GetLogicalDescendants().OfType<RadioButton>().Should().BeEmpty("the introduction has no header");
        window.Close();
    });
}
