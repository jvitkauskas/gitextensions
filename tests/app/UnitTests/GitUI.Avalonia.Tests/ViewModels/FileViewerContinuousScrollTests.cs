using GitUI.Presentation.Editor;
using GitUI.Presentation.UserControls.FileStatusList;
using static GitUI.AvaloniaTests.ViewModels.FileStatusListViewModelTests;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>The continuous scroll of the file viewer into the next file of the list (<c>ContinuousScrollEventManager</c>).</summary>
[TestFixture]
public sealed class FileViewerContinuousScrollTests
{
    [Test]
    public void Scrolling_on_shows_the_next_file_with_Alt_or_if_automatic_and_not_too_fast()
    {
        DiffViewModelTests.FakeViewerHost host = new();
        FileViewerViewModel viewer = new(host);
        DateTime now = new(2026, 9, 25, 12, 0, 0);
        viewer.Now = () => now;
        FileStatusListViewModel files = Create();
        files.SetDiff(First, Second, CreateStatuses());
        files.Select(entry => entry.Item.Name == "docs/readme.md");
        viewer.ScrollOnThrough(() => files);

        viewer.OnScrollReached(bottom: true, withAlt: false);
        files.SelectedEntry!.Item.Name.Should().Be("docs/readme.md", "the scroll is not automatic, and Alt is not pressed");

        viewer.OnScrollReached(bottom: true, withAlt: true);
        string next = files.SelectedEntry!.Item.Name;
        next.Should().NotBe("docs/readme.md");
        viewer.Editor.PendingScroll.Should().Be(TextScrollRequest.Top, "the next file is shown from its start");

        viewer.OnScrollReached(bottom: true, withAlt: true);
        files.SelectedEntry!.Item.Name.Should().Be(next, "sooner than AutomaticContinuousScrollDelay");

        // As ContinuousScrollToolStripMenuItemClick: saved, and without Alt then.
        viewer.ToggleAutomaticContinuousScrollCommand.Execute(null);
        host.Settings.AutomaticContinuousScroll.Should().BeTrue();
        now = now.AddMilliseconds(600);
        viewer.OnScrollReached(bottom: false, withAlt: false);
        files.SelectedEntry!.Item.Name.Should().Be("docs/readme.md");
        viewer.Editor.PendingScroll.Should().Be(TextScrollRequest.Bottom, "the previous file is shown at its end");

        // The first file: nothing else to show.
        now = now.AddMilliseconds(600);
        viewer.OnScrollReached(bottom: false, withAlt: false);
        files.SelectedEntry!.Item.Name.Should().Be("docs/readme.md");
        viewer.Editor.PendingScroll.Should().Be(TextScrollRequest.None);
    }
}
