using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI;
using GitUI.AvaloniaHosting;
using GitUI.Presentation.Editor;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUIPluginInterfaces;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 3: the line patches of the file viewer, with a real repository.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void File_viewer_stages_and_unstages_the_selected_lines()
    {
        _referenceRepository.CreateCommit("Lines", "one\ntwo\nthree\n", "lines.txt");
        _referenceRepository.CreateRepoFile("lines.txt", "one\nTWO\nthree\nfour\n");
        FileViewerViewModel viewer = new(new FileViewerHost(_commands)) { LinePatchingBlocksUntilReload = true };
        int applied = 0;
        viewer.PatchApplied += (_, _) => applied++;

        GitItemStatus changed = new("lines.txt") { IsChanged = true, IsTracked = true, Staged = StagedStatus.WorkTree };
        FileStatusEntry workTree = new(new GitRevision(ObjectId.IndexId), new GitRevision(ObjectId.WorkTreeId) { ParentIds = [ObjectId.IndexId] }, changed);
        ThreadHelper.JoinableTaskFactory.Run(() => viewer.ShowChangesAsync(workTree));
        viewer.GetMenuState().CanStage.Should().BeTrue();

        // The text of the editor, without git's colors.
        string text = viewer.Editor.Text;
        viewer.ApplyLinePatch(LinePatchOperation.Stage, text.IndexOf("+four", StringComparison.Ordinal), "+four".Length).Should().BeTrue();
        applied.Should().Be(1);
        string index = _referenceRepository.Module.GitExecutable.GetOutput("diff --cached -- lines.txt");
        index.Should().Contain("+four").And.NotContain("TWO", "only the selected line is staged");

        GitItemStatus staged = new("lines.txt") { IsChanged = true, IsTracked = true, Staged = StagedStatus.Index };
        GitRevision head = new(_referenceRepository.Module.RevParse("HEAD")!);
        FileStatusEntry indexEntry = new(head, new GitRevision(ObjectId.IndexId) { ParentIds = [head.ObjectId] }, staged);
        ThreadHelper.JoinableTaskFactory.Run(() => viewer.ShowChangesAsync(indexEntry));
        viewer.GetMenuState().CanUnstage.Should().BeTrue();
        text = viewer.Editor.Text;
        viewer.ApplyLinePatch(LinePatchOperation.Unstage, text.IndexOf("+four", StringComparison.Ordinal), "+four".Length).Should().BeTrue();

        _referenceRepository.Module.GitExecutable.GetOutput("diff --cached -- lines.txt").Should().BeEmpty();
        File.ReadAllText(Path.Combine(_referenceRepository.Module.WorkingDir, "lines.txt")).Should().Be("one\nTWO\nthree\nfour\n", "the working directory is unchanged");
    }
}
