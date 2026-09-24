using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Editor;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>The options of the viewer the changes of a file are read with (<see cref="FileViewRequest"/>).</summary>
[TestFixture]
public sealed class FileViewerRequestTests
{
    [Test]
    public async Task A_range_diff_is_limited_to_the_path_filter_of_the_grid()
    {
        DiffViewModelTests.FakeViewerHost host = new();
        FileViewerViewModel viewer = new(host);
        await viewer.ShowChangesAsync(FileViewerContextMenuTests.Entry(StagedStatus.None));
        host.Requests[^1].RangeDiffPathFilter.Should().BeEmpty();

        // As the additionalCommandInfo of RevisionDiffControl.ShowSelectedFileDiffAsync.
        string pathFilter = "src/";
        viewer.RangeDiffPathFilter = () => pathFilter;
        await viewer.ShowChangesAsync(FileViewerContextMenuTests.Entry(StagedStatus.None));
        host.Requests[^1].RangeDiffPathFilter.Should().Be("src/");
    }
}
