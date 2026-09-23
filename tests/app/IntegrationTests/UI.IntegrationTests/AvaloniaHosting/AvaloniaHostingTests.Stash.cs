using GitCommands;
using GitUI.Presentation.CommandsDialogs;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 5: the stash dialog on the Avalonia file status list and file viewer, with a real repository.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void Stash_shows_the_files_of_the_newest_stash_and_applies_it()
    {
        string path = Path.Combine(_referenceRepository.Module.WorkingDir, "A.txt");
        File.WriteAllText(path, "stashed change");
        _referenceRepository.Module.GitExecutable.GetOutput("stash push -m \"my stash\"");
        File.ReadAllText(path).Should().NotBe("stashed change");

        List<string> stashes = [];
        List<string> files = [];
        string? diff = null;
        DriveDialogs(
            window => WaitUntil(() => window.DataContext is StashViewModel { Files.AllEntries: var entries, Viewer.Editor.Text: { Length: > 0 } } && entries.Any(), () =>
            {
                StashViewModel viewModel = (StashViewModel)window.DataContext!;
                stashes.AddRange(viewModel.Stashes.Select(s => s.Summary));
                files.AddRange(viewModel.Files.AllEntries.Select(e => e.Item.Name));
                diff = viewModel.Viewer.Editor.Text;
                Capture(window, "stash");
                viewModel.ApplyCommand.Execute(null);
                window.Close();
            }),
            AcknowledgeWhenDone);

        _commands.StartStashDialog(_owner, manageStashes: true).Should().BeTrue();

        stashes.Should().Equal("Current working directory changes", "@{0}: On master: my stash");
        files.Should().Equal("A.txt");
        diff.Should().Contain("+stashed change");
        File.ReadAllText(path).Should().Be("stashed change", "the stash is applied");
    }
}
