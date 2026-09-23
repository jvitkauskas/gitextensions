using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the checkout branch dialog (phase 2, batch 5).</summary>
[TestFixture]
public sealed class CheckoutBranchViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        Capture(
            new CheckoutBranchWindow { DataContext = CheckoutBranchViewModelTests.Create(new CheckoutBranchViewModelTests.FakeCheckoutBranchHost(), CheckoutBranchViewModelTests.Options("dev")) },
            $"checkout-branch-{theme}");
        Capture(
            new CheckoutBranchWindow
            {
                DataContext = CheckoutBranchViewModelTests.Create(
                    new CheckoutBranchViewModelTests.FakeCheckoutBranchHost(), CheckoutBranchViewModelTests.Options("origin/main", remote: true) with { IsDirtyDir = true }),
            },
            $"checkout-branch-remote-{theme}");
    });

    [Test]
    public Task Remote_options_and_local_changes_are_shown_when_relevant() => OnUiThreadAsync(() =>
    {
        CheckoutBranchViewModel viewModel = CheckoutBranchViewModelTests.Create(new CheckoutBranchViewModelTests.FakeCheckoutBranchHost(), CheckoutBranchViewModelTests.Options("dev"));
        CheckoutBranchWindow window = Show(new CheckoutBranchWindow { DataContext = viewModel });
        StackPanel remoteOptions = window.FindControl<StackPanel>("remoteOptions")!;

        remoteOptions.IsVisible.Should().BeFalse();
        window.FindControl<CheckBox>("setAsDefaultCheckBox")!.IsEffectivelyVisible.Should().BeFalse("the working directory is clean");

        window.FindControl<RadioButton>("remoteRadioButton")!.IsChecked = true;
        Dispatcher.UIThread.RunJobs();

        remoteOptions.IsVisible.Should().BeTrue();
        window.FindControl<ComboBox>("branchesComboBox")!.ItemCount.Should().Be(2);
        window.FindControl<TextBox>("customBranchNameTextBox")!.IsEnabled.Should().BeFalse();

        window.FindControl<RadioButton>("customNameRadioButton")!.IsChecked = true;
        Dispatcher.UIThread.RunJobs();
        window.FindControl<TextBox>("customBranchNameTextBox")!.IsEnabled.Should().BeTrue();
        viewModel.IsResetBranch.Should().BeFalse();
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
