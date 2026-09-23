using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the push dialog.</summary>
[TestFixture]
public sealed class PushViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        PushViewModel viewModel = PushViewModelTests.Create(new PushViewModelTests.FakePushHost(), out _);
        viewModel.ShowOptions = true;
        PushWindow window = new() { DataContext = viewModel };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        window.Title.Should().Be(@"Push (C:\repo)");
        SaveScreenshot(window.CaptureRenderedFrame(), $"push-{theme}");

        viewModel.SelectedTab = PushTab.MultipleBranches;
        Dispatcher.UIThread.RunJobs();
        window.BranchGrid.Columns.Select(c => c.Header is Button button ? button.Content : c.Header)
            .Should().Equal("Local Branch", "Remote Branch", "Ahead/Behind", "Push", "Force", "Delete Remote Branch");
        SaveScreenshot(window.CaptureRenderedFrame(), $"push-multiple-{theme}");
        window.Close();
    });
}
