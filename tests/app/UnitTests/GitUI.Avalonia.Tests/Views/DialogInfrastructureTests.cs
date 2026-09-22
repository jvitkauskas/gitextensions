using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Threading;
using GitUI.Avalonia.HelperDialogs;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.HelperDialogs;
using GitUI.Presentation.Services;

namespace GitUI.AvaloniaTests.Views;

/// <summary>
///  The shared dialog infrastructure (position persistence, hotkeys) and the progress dialog view.
/// </summary>
[TestFixture]
public sealed class DialogInfrastructureTests : HeadlessTest
{
    [Test]
    public Task Dialog_restores_its_size_and_saves_it_on_close() => OnUiThreadAsync(() =>
    {
        InMemoryPositionStore store = new();
        store.Save("FormProcess", new WindowPlacement(40, 30, 800, 500, Dpi: 96, IsMaximized: false));
        ProcessWindow window = new()
        {
            DataContext = CreateProcessViewModel(),
            PositionName = "FormProcess",
            PositionStore = store,
        };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        window.Width.Should().Be(800);
        window.Height.Should().Be(500);

        window.Width = 640;
        Dispatcher.UIThread.RunJobs();
        window.Close();

        store.Load("FormProcess")!.Width.Should().Be(640);
    });

    [Test]
    public Task Size_saved_at_another_DPI_is_scaled() => OnUiThreadAsync(() =>
    {
        InMemoryPositionStore store = new();
        store.Save("FormProcess", new WindowPlacement(40, 30, 1600, 1000, Dpi: 192, IsMaximized: false));
        ProcessWindow window = new() { DataContext = CreateProcessViewModel(), PositionName = "FormProcess", PositionStore = store };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        window.Width.Should().Be(800);
        window.Height.Should().Be(500);
        window.Close();
    });

    [Test]
    public Task Size_of_a_dialog_sized_to_content_is_not_restored() => OnUiThreadAsync(() =>
    {
        // E.g. a height saved by the WinForms form, which would crop the Avalonia layout.
        InMemoryPositionStore store = new();
        store.Save("FormAddFiles", new WindowPlacement(40, 30, 700, 40, Dpi: 96, IsMaximized: false));
        GitUI.Avalonia.CommandsDialogs.AddFilesWindow window = new()
        {
            DataContext = new GitUI.Presentation.CommandsDialogs.AddFilesViewModel(new GitUI.Presentation.CommandsDialogs.AddFilesStrings(), ".", _ => true),
            PositionName = "FormAddFiles",
            PositionStore = store,
        };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        window.ClientSize.Height.Should().BeGreaterThan(60, "the height stays sized to the content");
        window.SizeToContent.Should().Be(SizeToContent.Height);
        window.Close();
    });

    [Test]
    public Task Configured_hotkey_reaches_the_view_model() => OnUiThreadAsync(() =>
    {
        HotkeyViewModel viewModel = new();
        ProcessWindow window = new()
        {
            DataContext = viewModel,
            Hotkeys = [new HotkeyBinding(CommandCode: 7, KeyData: 0x74 /* F5 */ | HotkeyBinding.Control)],
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        window.KeyPressQwerty(PhysicalKey.F5, RawInputModifiers.Control);
        window.KeyPressQwerty(PhysicalKey.F5, RawInputModifiers.None);

        viewModel.ExecutedCommands.Should().Equal(7);
        window.Close();
    });

    [Test]
    public Task Render_process_dialog_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        ProcessViewModelTests.FakeConsole console = new();
        ProcessViewModel running = CreateProcessViewModel(console);
        ProcessWindow window = new() { DataContext = running };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        console.Emit("Receiving objects:  42% (42/100)\r");
        console.Emit("Password for 'https://example.org': ");
        Dispatcher.UIThread.RunJobs();
        ProgressBar progressBar = window.FindControl<ProgressBar>("progressBar")!;
        progressBar.IsIndeterminate.Should().BeFalse();
        progressBar.Value.Should().Be(42);
        window.FindControl<TextBox>("passwordTextBox")!.IsEffectivelyVisible.Should().BeTrue();
        SaveScreenshot(window.CaptureRenderedFrame(), $"process-running-{theme}");

        console.Exit(1);
        Dispatcher.UIThread.RunJobs();
        SaveScreenshot(window.CaptureRenderedFrame(), $"process-failed-{theme}");
        window.Close();
    });

    private static ProcessViewModel CreateProcessViewModel(ProcessViewModelTests.FakeConsole? console = null)
        => new(
            new ProcessStrings(),
            "~/repo",
            console ?? new ProcessViewModelTests.FakeConsole(),
            new ProcessViewModelTests.FakeHost(),
            new ProcessViewModelTests.FakeMessageBoxes(),
            useDialogSettings: true,
            postToUiThread: action => action());

    private sealed class InMemoryPositionStore : IWindowPositionStore
    {
        private readonly Dictionary<string, WindowPlacement> _placements = [];

        public WindowPlacement? Load(string name) => _placements.GetValueOrDefault(name);

        public void Save(string name, WindowPlacement placement) => _placements[name] = placement;
    }

    /// <summary>A progress dialog view model that records hotkey commands (any dialog view model can override this).</summary>
    private sealed class HotkeyViewModel() : Presentation.DialogViewModel
    {
        public List<int> ExecutedCommands { get; } = [];

        public override bool ExecuteHotkeyCommand(int commandCode)
        {
            ExecutedCommands.Add(commandCode);
            return true;
        }
    }
}
