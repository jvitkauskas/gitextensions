using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the branch dialogs (phase 2, batch 3).</summary>
[TestFixture]
public sealed class BranchDialogViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        ProcessViewModelTests.FakeMessageBoxes messageBoxes = new();

        Capture(
            new DeleteBranchWindow
            {
                DataContext = new DeleteBranchViewModel(
                    new DeleteBranchStrings(), BranchDialogViewModelTests.CreateSelector(["main", "dev"], "dev"), "main", null, new BranchDialogViewModelTests.FakeDeleteBranchHost(), messageBoxes),
            },
            $"delete-branch-{theme}");
        Capture(
            new DeleteRemoteBranchWindow
            {
                DataContext = new DeleteRemoteBranchViewModel(
                    new DeleteRemoteBranchStrings(),
                    BranchDialogViewModelTests.CreateSelector(["origin/dev"], "origin/dev"),
                    new BranchDialogViewModelTests.FakeDeleteRemoteBranchHost { Tracking = { ["origin/dev"] = ["dev"] } },
                    messageBoxes),
            },
            $"delete-remote-branch-{theme}");
        Capture(
            new MergeBranchWindow { DataContext = BranchDialogViewModelTests.CreateMerge(new BranchDialogViewModelTests.FakeMergeBranchHost(), new MergeBranchOptions(false, false, true, 20, ShowAdvanced: true)) },
            $"merge-branch-{theme}");
        Capture(
            new MergeBranchWindow { DataContext = BranchDialogViewModelTests.CreateMerge(new BranchDialogViewModelTests.FakeMergeBranchHost(), new MergeBranchOptions(false, false, false, 20, ShowAdvanced: false)) },
            $"merge-branch-simple-{theme}");
    });

    [Test]
    public Task MergeBranch_advanced_options_are_shown_on_demand() => OnUiThreadAsync(() =>
    {
        MergeBranchViewModel viewModel = BranchDialogViewModelTests.CreateMerge(new BranchDialogViewModelTests.FakeMergeBranchHost(), new MergeBranchOptions(false, false, false, 20, ShowAdvanced: false));
        MergeBranchWindow window = Show(new MergeBranchWindow { DataContext = viewModel });
        Grid advanced = window.FindControl<Grid>("advancedPanel")!;

        advanced.IsVisible.Should().BeFalse();
        window.FindControl<CheckBox>("advancedCheckBox")!.IsChecked = true;
        Dispatcher.UIThread.RunJobs();
        advanced.IsVisible.Should().BeTrue();

        window.FindControl<ComboBox>("strategyComboBox")!.IsVisible.Should().BeFalse();
        window.FindControl<CheckBox>("strategyCheckBox")!.IsChecked = true;
        Dispatcher.UIThread.RunJobs();
        window.FindControl<ComboBox>("strategyComboBox")!.IsVisible.Should().BeTrue();

        window.FindControl<RadioButton>("noFastForwardRadioButton")!.IsChecked = true;
        Dispatcher.UIThread.RunJobs();
        window.FindControl<CheckBox>("squashCheckBox")!.IsEnabled.Should().BeFalse();
    });

    [Test]
    public Task DeleteRemoteBranch_Delete_is_enabled_by_the_confirmation() => OnUiThreadAsync(() =>
    {
        DeleteRemoteBranchViewModel viewModel = new(
            new DeleteRemoteBranchStrings(),
            BranchDialogViewModelTests.CreateSelector(["origin/dev"], "origin/dev"),
            new BranchDialogViewModelTests.FakeDeleteRemoteBranchHost(),
            new ProcessViewModelTests.FakeMessageBoxes());
        DeleteRemoteBranchWindow window = Show(new DeleteRemoteBranchWindow { DataContext = viewModel });
        Button delete = window.FindControl<Button>("deleteButton")!;

        delete.IsEffectivelyEnabled.Should().BeFalse();
        window.FindControl<CheckBox>("deleteRemoteCheckBox")!.IsChecked = true;
        Dispatcher.UIThread.RunJobs();
        delete.IsEffectivelyEnabled.Should().BeTrue();
        window.FindControl<CheckBox>("deleteTrackingCheckBox")!.IsEnabled.Should().BeFalse("there is no tracking branch");
    });

    private static T Show<T>(T window)
        where T : DialogWindow
    {
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static void Capture(DialogWindow window, string name)
    {
        Show(window);
        SaveScreenshot(window.CaptureRenderedFrame(), name);
        window.Close();
    }
}
