using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Threading;
using GitUI.Avalonia.Controls.RevisionGrid;
using GitUI.Presentation.UserControls.RevisionGrid;
using static GitUI.AvaloniaTests.ViewModels.QuickItemSelectorViewModelTests;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the quick picker of the revision grid (port of <c>FormQuickItemSelector</c>).</summary>
[TestFixture]
public sealed class QuickItemSelectorViewTests : HeadlessTest
{
    [Test]
    public Task Opens_at_its_position_and_Enter_accepts_the_selected_ref() => OnUiThreadAsync(() =>
    {
        QuickItemSelectorViewModel viewModel = CreateRefs();
        QuickItemSelectorWindow window = Show(viewModel);

        window.Position.Should().Be(new PixelPoint(120, 80));
        window.ItemsListBox.SelectedIndex.Should().Be(1);
        window.FindControl<Button>("actionButton")!.Content.Should().Be("Delete");

        window.ItemsListBox.ContainerFromIndex(1)!.Focus();
        window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        window.DialogResult.Should().BeTrue();
        ((GitExtensions.Extensibility.Git.IGitRef)viewModel.SelectedValue!).Name.Should().Be("main");
    });

    [Test]
    public Task Escape_cancels() => OnUiThreadAsync(() =>
    {
        QuickItemSelectorViewModel viewModel = CreateRefs();
        QuickItemSelectorWindow window = Show(viewModel);
        bool closed = false;
        window.Closed += (_, _) => closed = true;

        window.ItemsListBox.Focus();
        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        closed.Should().BeTrue();
        window.DialogResult.Should().BeFalse();
        viewModel.SelectedValue.Should().BeNull();
    });

    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        QuickItemSelectorWindow window = Show(CreateRefs());
        SaveScreenshot(window.CaptureRenderedFrame(), $"quick-ref-selector-{theme}");
        window.Close();

        window = Show(QuickItemSelectorViewModel.ForStrings(new QuickItemSelectorStrings(), ["upstream", "origin", "fork"]));
        SaveScreenshot(window.CaptureRenderedFrame(), $"quick-string-selector-{theme}");
        window.Close();
    });

    private static QuickItemSelectorViewModel CreateRefs()
        => QuickItemSelectorViewModel.ForRefs(
            new QuickItemSelectorStrings(),
            QuickRefAction.Delete,
            [Ref("main"), Ref("feature/login-page"), Ref("origin/main", remote: true), Ref("v1.0", tag: true)]);

    private static QuickItemSelectorWindow Show(QuickItemSelectorViewModel viewModel)
    {
        QuickItemSelectorWindow window = new() { DataContext = viewModel, StartupScreenPosition = new PixelPoint(120, 80) };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }
}
