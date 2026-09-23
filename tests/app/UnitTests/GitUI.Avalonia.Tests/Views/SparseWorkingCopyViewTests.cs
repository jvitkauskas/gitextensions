using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Editor;
using GitUI.Presentation.CommandsDialogs;
using FakeMessageBoxes = GitUI.AvaloniaTests.ViewModels.ProcessViewModelTests.FakeMessageBoxes;
using FakeHost = GitUI.AvaloniaTests.ViewModels.SparseWorkingCopyViewModelTests.FakeHost;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the sparse working copy dialog.</summary>
[TestFixture]
public sealed class SparseWorkingCopyViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        foreach (bool enabled in new[] { false, true })
        {
            SparseWorkingCopyWindow window = Show(new FakeHost { Enabled = enabled, Rules = "/src/\n!/src/tests/\n# docs are not needed\n" });
            SaveScreenshot(window.CaptureRenderedFrame(), $"sparse-working-copy-{(enabled ? "enabled" : "disabled")}-{theme}");
            window.Close();
        }
    });

    [Test]
    public Task Enabling_shows_the_rules_and_disabling_hides_them() => OnUiThreadAsync(() =>
    {
        SparseWorkingCopyWindow window = Show(new FakeHost { Rules = "/src/\n" });
        SparseWorkingCopyViewModel viewModel = (SparseWorkingCopyViewModel)window.DataContext!;
        Panel disabledPanel = window.FindControl<StackPanel>("disabledPanel")!;
        Panel enabledPanel = window.FindControl<DockPanel>("enabledPanel")!;
        disabledPanel.IsVisible.Should().BeTrue();
        enabledPanel.IsVisible.Should().BeFalse();

        window.FindControl<Button>("enableButton")!.Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();
        viewModel.IsSparseCheckoutEnabled.Should().BeTrue();
        disabledPanel.IsVisible.Should().BeFalse();
        enabledPanel.IsVisible.Should().BeTrue();
        window.FindControl<TextEditorView>("rulesEditor")!.Editor.Text.Should().Be("/src/\n");

        window.FindControl<Button>("disableLink")!.Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();
        disabledPanel.IsVisible.Should().BeTrue();

        viewModel.IsSparseCheckoutEnabled = false; // No changes left, so closing does not ask.
        window.Close();
    });

    private static SparseWorkingCopyWindow Show(FakeHost host)
    {
        SparseWorkingCopyWindow window = new() { DataContext = new SparseWorkingCopyViewModel(new SparseWorkingCopyStrings(), host, new FakeMessageBoxes()) };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }
}
