using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Editor;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUIPluginInterfaces;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>Tests of the context menu of the file viewer (port of the menu of the WinForms <c>FileViewer</c>).</summary>
[TestFixture]
public sealed class FileViewerContextMenuTests
{
    internal const string Patch = "diff --git a/f b/f\n--- a/f\n+++ b/f\n@@ -1,3 +1,3 @@\n a\n-b\n+c\n d\n";

    internal static FileStatusEntry Entry(StagedStatus staged)
        => new(
            new GitRevision(staged == StagedStatus.WorkTree ? ObjectId.IndexId : ObjectId.Parse("a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1")),
            new GitRevision(staged == StagedStatus.WorkTree ? ObjectId.WorkTreeId : ObjectId.IndexId),
            new GitItemStatus("f") { IsChanged = true, IsTracked = true, Staged = staged });

    [Test]
    public async Task The_line_patches_depend_on_the_diff()
    {
        DiffViewModelTests.FakeViewerHost host = new() { Diff = Patch, SupportsLinePatching = true };
        FileViewerViewModel viewer = new(host);

        await viewer.ShowChangesAsync(Entry(StagedStatus.WorkTree));
        viewer.GetMenuState().Should().Be(new FileViewerMenuState(CanStage: true, CanUnstage: false, CanReset: true, CanCopyPatch: true, IsDiff: true));

        await viewer.ShowChangesAsync(Entry(StagedStatus.Index));
        viewer.GetMenuState().Should().Be(new FileViewerMenuState(CanStage: false, CanUnstage: true, CanReset: true, CanCopyPatch: true, IsDiff: true));

        host.SupportsLinePatching = false;
        await viewer.ShowChangesAsync(Entry(StagedStatus.WorkTree));
        viewer.GetMenuState().Should().Be(new FileViewerMenuState(CanStage: false, CanUnstage: false, CanReset: false, CanCopyPatch: true, IsDiff: true));
        viewer.ApplyLinePatch(LinePatchOperation.Stage, 0, 1).Should().BeFalse("the hotkey is not used");

        viewer.Show(new FileViewContent(FileViewKind.Text, "text"));
        viewer.GetMenuState().Should().Be(new FileViewerMenuState(false, false, false, CanCopyPatch: false, IsDiff: false));
    }

    [Test]
    public async Task A_line_patch_waits_for_the_diff_to_be_shown_again_if_set_so()
    {
        DiffViewModelTests.FakeViewerHost host = new() { Diff = Patch, SupportsLinePatching = true };
        FileViewerViewModel viewer = new(host) { LinePatchingBlocksUntilReload = true };
        int applied = 0;
        viewer.PatchApplied += (_, _) => applied++;
        await viewer.ShowChangesAsync(Entry(StagedStatus.WorkTree));

        viewer.ApplyLinePatch(LinePatchOperation.Stage, 60, 5).Should().BeTrue();
        viewer.ApplyLinePatch(LinePatchOperation.Reset, 60, 5).Should().BeTrue("the hotkey is used, the patch waits");
        viewer.ApplyLinePatch(LinePatchOperation.Unstage, 60, 5).Should().BeFalse("the working directory is not unstaged");
        host.Patches.Should().Equal("Stage WorkTree 60+5");
        applied.Should().Be(1);

        await viewer.ShowChangesAsync(Entry(StagedStatus.Index));
        viewer.ApplyLinePatch(LinePatchOperation.Unstage, 60, 5).Should().BeTrue();
        host.Patches.Should().Equal("Stage WorkTree 60+5", "Unstage Index 60+5");
    }

    [Test]
    public async Task Copying_removes_the_prefixes_of_the_diff_lines()
    {
        DiffViewModelTests.FakeViewerHost host = new() { Diff = Patch };
        FileViewerViewModel viewer = new(host);
        await viewer.ShowChangesAsync(Entry(StagedStatus.WorkTree));
        int start = Patch.IndexOf("\n a", StringComparison.Ordinal) + 1;

        viewer.Copy(" a\n-b\n+c", start);
        viewer.Copy("a\n-b", start + 1);
        viewer.Copy("--- a/f", Patch.IndexOf("---", StringComparison.Ordinal));
        viewer.CopyPatch("");
        viewer.CopyVersion(" a\n-b\n+c\n d\n", start, newVersion: true);
        viewer.CopyVersion(" a\n-b\n+c\n d\n", start, newVersion: false);

        host.Copied.Should().Equal(
            ("a\nb\nc", true),
            ("a\nb", true),
            ("--- a/f", true),
            (Patch, false),
            ("a\nc\nd\n", true),
            ("a\nb\nd\n", true));
    }

    [TestCase(" +x\n", false, "+x\n")]
    [TestCase("++x\n- y", true, "x\ny")]
    [TestCase("  x", true, "x")]
    public void Combined_diffs_have_two_prefix_characters(string selected, bool isCombined, string expected)
    {
        FileViewerViewModel.RemoveDiffPrefixes("@@@ -1 +1 @@@\n" + selected, selected, "@@@ -1 +1 @@@\n".Length, isCombined).Should().Be(expected);
    }
}
