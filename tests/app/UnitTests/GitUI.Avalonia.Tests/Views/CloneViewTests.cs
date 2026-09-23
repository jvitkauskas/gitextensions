using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.AvaloniaTests.ViewModels;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the clone dialog (phase 2, batch 6).</summary>
[TestFixture]
public sealed class CloneViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        Capture(
            new CloneWindow { DataContext = CloneViewModelTests.Create(new CloneViewModelTests.FakeCloneHost(), "https://github.com/gitextensions/gitextensions.git", @"C:\repos") },
            $"clone-{theme}");
        Capture(new CloneWindow { DataContext = CloneViewModelTests.Create(new CloneViewModelTests.FakeCloneHost(), "", "") }, $"clone-incomplete-{theme}");
    });

    private static void Capture(DialogWindow window, string name)
    {
        window.Show();
        Dispatcher.UIThread.RunJobs();
        SaveScreenshot(window.CaptureRenderedFrame(), name);
        window.Close();
    }
}
