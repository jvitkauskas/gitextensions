using System.Text;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GitCommands.Git;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.CommandsDialogs.SettingsDialog;
using GitUI.Avalonia.HelperDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs.SettingsDialog;
using GitUI.Presentation.HelperDialogs;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the second batch of phase 2 dialogs.</summary>
[TestFixture]
public sealed class Batch2ViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        ProcessViewModelTests.FakeMessageBoxes messageBoxes = new();
        SmallDialogViewModelTests.FakeFileDialogs fileDialogs = new();

        Capture(new PuttyErrorWindow { DataContext = new PuttyErrorViewModel(new PuttyErrorStrings(), () => null) }, $"putty-error-{theme}");
        Capture(
            new BuildServerCredentialsWindow { DataContext = new BuildServerCredentialsViewModel("https://ci.example.org") { Authentication = BuildServerAuthentication.UsernameAndPassword, Username = "me" } },
            $"build-server-credentials-{theme}");
        Capture(
            new SelectMultipleBranchesWindow { DataContext = new SelectMultipleBranchesViewModel(new SelectMultipleBranchesStrings(), [("a", "main"), ("b", "feature/x"), ("c", "release")], ["release"]) },
            $"select-multiple-branches-{theme}");
        Capture(
            new ChooseTranslationWindow { DataContext = new ChooseTranslationViewModel(new ChooseTranslationStrings(), [new("English", null), new("Dutch", null), new("German", null)]) },
            $"choose-translation-{theme}");
        Capture(
            new AvailableEncodingsWindow { DataContext = new AvailableEncodingsViewModel(new AvailableEncodingsStrings(), [new UTF8Encoding(false), Encoding.Unicode]) },
            $"available-encodings-{theme}");
        Capture(
            new AddSubmoduleWindow { DataContext = new AddSubmoduleViewModel(new AddSubmoduleStrings(), ["https://example.org/lib.git"], _ => [], _ => { }, messageBoxes, fileDialogs) },
            $"add-submodule-{theme}");
        Capture(
            new CleanupRepositoryWindow
            {
                DataContext = new CleanupRepositoryViewModel(new CleanupRepositoryStrings(), @"C:\repo\", @"C:\repo\.git\", "src", _ => "", messageBoxes, fileDialogs)
                {
                    Output = "Would remove bin/\nWould remove obj/\n",
                },
            },
            $"cleanup-repository-{theme}");
        Capture(
            new MergeSubmoduleWindow
            {
                DataContext = new MergeSubmoduleViewModel(
                    new MergeSubmoduleStrings(), "externals/lib", null, "4b825dc642cb6eb9a060e54bf8d69288fbee4904", "a1b2c3d4e5f60718293a4b5c6d7e8f9012345678", new Batch2ViewModelTests.FakeMergeSubmoduleHost()),
            },
            $"merge-submodule-{theme}");
        Capture(new CreateWorktreeWindow { DataContext = CreateWorktree(["main", "feature/x"]) }, $"create-worktree-{theme}");
        Capture(
            new OpenDirectoryWindow { DataContext = new OpenDirectoryViewModel(new OpenDirectoryStrings(), [@"C:\repos\"], "Error", _ => false, messageBoxes, fileDialogs) },
            $"open-directory-{theme}");
    });

    [Test]
    public Task SelectMultipleBranches_Space_toggles_the_selected_branch() => OnUiThreadAsync(() =>
    {
        SelectMultipleBranchesViewModel viewModel = new(new SelectMultipleBranchesStrings(), [("a", "main"), ("b", "dev")], []);
        SelectMultipleBranchesWindow window = Show(new SelectMultipleBranchesWindow { DataContext = viewModel });
        ListBox list = window.FindControl<ListBox>("branchesListBox")!;
        list.SelectedIndex = 1;
        list.ContainerFromIndex(1)!.Focus();

        window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        viewModel.SelectedBranches.Should().Equal("b");
    });

    [Test]
    public Task BuildServerCredentials_enables_the_fields_of_the_chosen_method() => OnUiThreadAsync(() =>
    {
        BuildServerCredentialsViewModel viewModel = new("server");
        BuildServerCredentialsWindow window = Show(new BuildServerCredentialsWindow { DataContext = viewModel });
        TextBox userName = window.FindControl<TextBox>("userNameTextBox")!;
        TextBox token = window.FindControl<TextBox>("bearerTokenTextBox")!;

        userName.IsEnabled.Should().BeFalse();
        token.IsEnabled.Should().BeFalse();

        window.FindControl<RadioButton>("tokenRadioButton")!.IsChecked = true;
        Dispatcher.UIThread.RunJobs();

        viewModel.Authentication.Should().Be(BuildServerAuthentication.BearerToken);
        token.IsEnabled.Should().BeTrue();
        userName.IsEnabled.Should().BeFalse();
    });

    [Test]
    public Task ChooseTranslation_lists_a_button_per_language() => OnUiThreadAsync(() =>
    {
        ChooseTranslationViewModel viewModel = new(new ChooseTranslationStrings(), [new("English", null), new("German", null)]);
        ChooseTranslationWindow window = Show(new ChooseTranslationWindow { DataContext = viewModel });

        Button[] buttons = [.. window.FindControl<ItemsControl>("translationsItemsControl")!.GetVisualDescendants().OfType<Button>()];
        buttons.Should().HaveCount(2);

        buttons[1].Command!.Execute(buttons[1].CommandParameter);
        Dispatcher.UIThread.RunJobs();

        viewModel.SelectedTranslation.Should().Be("German");
        window.DialogResult.Should().BeTrue();
    });

    [Test]
    public Task CreateWorktree_enables_the_inputs_of_the_chosen_option() => OnUiThreadAsync(() =>
    {
        CreateWorktreeViewModel viewModel = CreateWorktree(["main", "dev"]);
        CreateWorktreeWindow window = Show(new CreateWorktreeWindow { DataContext = viewModel });
        ComboBox branches = window.FindControl<ComboBox>("branchesComboBox")!;
        TextBox newBranch = window.FindControl<TextBox>("newBranchNameTextBox")!;

        branches.IsEnabled.Should().BeTrue();
        newBranch.IsEnabled.Should().BeFalse();
        branches.SelectedItem.Should().Be("dev");

        window.FindControl<RadioButton>("createNewRadioButton")!.IsChecked = true;
        Dispatcher.UIThread.RunJobs();

        viewModel.IsCreateNewBranch.Should().BeTrue();
        branches.IsEnabled.Should().BeFalse();
        newBranch.IsEnabled.Should().BeTrue();
        window.FindControl<Button>("createButton")!.IsEffectivelyEnabled.Should().BeFalse("no branch name was entered");
    });

    private static CreateWorktreeViewModel CreateWorktree(IReadOnlyList<string> branches)
        => new(
            new CreateWorktreeStrings(),
            branches,
            "main",
            Path.Join(Path.GetTempPath(), "does-not-exist", "repo"),
            new FakeBranchNameNormaliser(),
            new GitBranchNameOptions("_"),
            autoNormalise: true,
            (_, _) => true,
            new SmallDialogViewModelTests.FakeFileDialogs());

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
