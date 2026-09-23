using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Controls;
using GitUI.Avalonia.Hosting;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.UserControls;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the rebase and apply patch dialogs.</summary>
[TestFixture]
public sealed class RebaseViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        RebaseViewModel rebase = RebaseViewModelTests.CreateRebase(new RebaseViewModelTests.FakeRebaseHost { IsDirty = true }, RebaseViewModelTests.Options(defaultBranch: "main"));
        Capture(new RebaseWindow { DataContext = rebase }, $"rebase-{theme}");

        RebaseViewModel inProgress = RebaseViewModelTests.CreateRebase(
            new RebaseViewModelTests.FakeRebaseHost { IsRebasing = true, HasConflicts = true, Patches = CreateCommits },
            RebaseViewModelTests.Options(defaultBranch: "main"));
        Capture(new RebaseWindow { DataContext = inProgress }, $"rebase-conflicts-{theme}");

        ApplyPatchViewModel applyPatch = RebaseViewModelTests.CreateApplyPatch(new RebaseViewModelTests.FakeApplyPatchHost(), patchFile: @"C:\patches\0001-Fix-the-bug.patch");
        Capture(new ApplyPatchWindow { DataContext = applyPatch }, $"apply-patch-{theme}");

        ApplyPatchViewModel applying = RebaseViewModelTests.CreateApplyPatch(
            new RebaseViewModelTests.FakeApplyPatchHost { IsInPatch = true, HasConflicts = true, Patches = CreatePatches },
            patchFile: "");
        Capture(new ApplyPatchWindow { DataContext = applying }, $"apply-patch-conflicts-{theme}");
    });

    [Test]
    public Task Rebase_shows_the_actions_of_a_rebase_in_progress() => OnUiThreadAsync(() =>
    {
        RebaseViewModelTests.FakeRebaseHost host = new() { IsRebasing = true, Patches = CreateCommits };
        RebaseViewModel viewModel = RebaseViewModelTests.CreateRebase(host, RebaseViewModelTests.Options());
        RebaseWindow window = Show(new RebaseWindow { DataContext = viewModel });

        window.FindControl<Button>("rebaseButton")!.IsVisible.Should().BeFalse();
        window.FindControl<Button>("continueButton")!.IsVisible.Should().BeTrue();
        window.FindControl<Button>("continueButton")!.IsDefault.Should().BeTrue();
        window.FindControl<Border>("mergeToolPanel")!.IsVisible.Should().BeFalse();
        window.FindControl<Button>("skipButton")!.IsVisible.Should().BeTrue();
        window.FindControl<Button>("unresolvedConflictsButton")!.IsVisible.Should().BeFalse();
        window.FindControl<StackPanel>("rebasePanel")!.IsVisible.Should().BeFalse("the dialog opened during a rebase");

        DataGrid grid = window.FindControl<PatchGridView>("patchGrid")!.FindControl<DataGrid>("patchesGrid")!;
        grid.Columns.Select(c => c.IsVisible).Should().Equal(true, true, false, true, true, true, true);
        grid.SelectedItem.Should().BeSameAs(viewModel.PatchGrid.Patches[1]);
        window.Close();
    });

    [Test]
    public Task Rebase_options_follow_the_check_boxes() => OnUiThreadAsync(() =>
    {
        RebaseViewModel viewModel = RebaseViewModelTests.CreateRebase(new RebaseViewModelTests.FakeRebaseHost(), RebaseViewModelTests.Options());
        RebaseWindow window = Show(new RebaseWindow { DataContext = viewModel });
        CheckBox autosquash = window.FindControl<CheckBox>("autosquashCheckBox")!;
        TextBox from = window.FindControl<TextBox>("fromTextBox")!;

        autosquash.IsEnabled.Should().BeFalse();
        window.FindControl<CheckBox>("interactiveCheckBox")!.IsChecked = true;
        Dispatcher.UIThread.RunJobs();
        autosquash.IsEnabled.Should().BeTrue();

        from.IsEnabled.Should().BeFalse();
        window.FindControl<CheckBox>("specificRangeCheckBox")!.IsChecked = true;
        Dispatcher.UIThread.RunJobs();
        from.IsEnabled.Should().BeTrue();

        window.FindControl<CheckBox>("ignoreDateCheckBox")!.IsChecked = true;
        Dispatcher.UIThread.RunJobs();
        autosquash.IsEnabled.Should().BeFalse();
        window.FindControl<CheckBox>("committerDateCheckBox")!.IsEnabled.Should().BeFalse();
        window.Close();
    });

    [Test]
    public Task ApplyPatch_enables_the_patch_actions_while_applying() => OnUiThreadAsync(() =>
    {
        ApplyPatchViewModel viewModel = RebaseViewModelTests.CreateApplyPatch(
            new RebaseViewModelTests.FakeApplyPatchHost { IsInPatch = true, Patches = CreatePatches },
            patchFile: "");
        ApplyPatchWindow window = Show(new ApplyPatchWindow { DataContext = viewModel });

        window.FindControl<Button>("applyButton")!.IsEnabled.Should().BeFalse();
        window.FindControl<Button>("resolvedButton")!.IsEnabled.Should().BeTrue();
        window.FindControl<Button>("resolvedButton")!.IsDefault.Should().BeTrue();
        window.FindControl<Button>("mergetoolButton")!.IsEnabled.Should().BeFalse();
        window.FindControl<Button>("solveMergeConflictsButton")!.IsVisible.Should().BeFalse();
        window.FindControl<Border>("continuePanel")!.Classes.Should().Contain("highlight");

        DataGrid grid = window.FindControl<PatchGridView>("patchGrid")!.FindControl<DataGrid>("patchesGrid")!;
        grid.Columns.Select(c => c.IsVisible).Should().Equal(true, false, true, true, true, true, false);
        window.Close();
    });

    private static IReadOnlyList<PatchItem> CreateCommits() =>
    [
        new PatchItem { Action = "pick", ObjectId = ObjectId.Parse("a1b2c3d4e5f60718293a4b5c6d7e8f9012345678"), Subject = "Add the rebase dialog", Author = "John Doe", Date = "01/09/2026 10:00:00", IsApplied = true },
        new PatchItem { Action = "pick", ObjectId = ObjectId.Parse("b1b2c3d4e5f60718293a4b5c6d7e8f9012345678"), Subject = "Port the patch grid\n\nWith its columns.", Author = "John Doe", Date = "02/09/2026 11:30:00", IsNext = true },
        new PatchItem { Action = "squash", ObjectId = ObjectId.Parse("c1b2c3d4e5f60718293a4b5c6d7e8f9012345678"), Subject = "Fix typo", Author = "Jane Roe", Date = "03/09/2026 09:15:00" },
    ];

    private static IReadOnlyList<PatchItem> CreatePatches() =>
    [
        new PatchItem { Name = "0001", Subject = "[PATCH 1/3] Add the file", Author = "John Doe", Date = "Mon, 1 Sep 2026 10:00:00", IsApplied = true },
        new PatchItem { Name = "0002", Subject = "[PATCH 2/3] Change the file", Author = "John Doe", Date = "Tue, 2 Sep 2026 11:30:00", IsNext = true },
        new PatchItem { Name = "0003", Subject = "[PATCH 3/3] Remove the file", Author = "Jane Roe", Date = "Wed, 3 Sep 2026 09:15:00" },
    ];

    private static T Show<T>(T window)
        where T : DialogWindow
    {
        window.Show();

        // Runs the InitializeView posted by the Opened handlers of the windows, then the bindings and the focus it causes.
        Dispatcher.UIThread.RunJobs();
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
