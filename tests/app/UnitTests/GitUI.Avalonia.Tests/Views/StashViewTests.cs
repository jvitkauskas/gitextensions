using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Editor;
using GitUI.Presentation.UserControls.FileStatusList;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the stash dialog.</summary>
[TestFixture]
public sealed class StashViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        StashViewModelTests.FakeHost host = new() { Stashes = [new(0, "WIP on master: 1a2b3c4 Fix the bug"), new(1, "On feature: experiment")] };
        StashViewModel viewModel = new(
            new StashStrings(), host, new DiffViewModelTests.FakeViewerHost(), new FileStatusListStrings(), new FileStatusTreeOptions(), manageStashes: true);
        StashWindow window = new() { DataContext = viewModel };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        viewModel.Viewer.Show(new FileViewContent(FileViewKind.Diff, "diff --git a/docs/readme.md b/docs/readme.md\n@@ -1,2 +1,2 @@\n-old\n+new\n same\n"));
        Dispatcher.UIThread.RunJobs();

        window.FindControl<ComboBox>("stashesComboBox")!.SelectedItem.Should().BeSameAs(viewModel.Stashes[1]);
        window.FindControl<Button>("applyButton")!.IsEnabled.Should().BeTrue();
        window.FindControl<Button>("stashSelectedButton")!.IsEnabled.Should().BeFalse();
        window.FindControl<TextBox>("messageTextBox")!.IsReadOnly.Should().BeTrue();
        SaveScreenshot(window.CaptureRenderedFrame(), $"stash-{theme}");
        window.Close();
    });
}
