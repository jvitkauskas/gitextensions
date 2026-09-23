using GitExtensions.Extensibility.Git;
using GitUI.Presentation.CommandsDialogs;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 3: the view patch dialog on the diff mode of the Avalonia text editor, with a real patch file.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void View_patch_shows_the_patches_of_a_file()
    {
        string path = Path.Combine(_referenceRepository.Module.WorkingDir, "..", $"{Guid.NewGuid():N}.patch");
        File.WriteAllText(path, """
            diff --git a/A.txt b/A.txt
            index 5626abf..4b5fa63 100644
            --- a/A.txt
            +++ b/A.txt
            @@ -1 +1,2 @@
            -one
            +one
            +two
            diff --git a/B.txt b/B.txt
            new file mode 100644
            index 0000000..3f1a2f0
            --- /dev/null
            +++ b/B.txt
            @@ -0,0 +1 @@
            +new

            """.ReplaceLineEndings("\n"));
        try
        {
            List<(string FileName, PatchChangeType Change)> patches = [];
            string? shownDiff = null;
            DriveNextDialog(window =>
            {
                ViewPatchViewModel viewModel = (ViewPatchViewModel)window.DataContext!;
                patches.AddRange(viewModel.Patches.Select(p => (p.FileNameA, p.ChangeType)));
                viewModel.SelectedPatch = viewModel.Patches[1];
                shownDiff = viewModel.Diff.Text;
                viewModel.SelectedPatch = viewModel.Patches[0];
                Capture(window, "view-patch");
                window.Close();
            });

            _commands.StartViewPatchDialog(_owner, path).Should().BeTrue();

            patches.Should().Equal(("A.txt", PatchChangeType.ChangeFile), ("B.txt", PatchChangeType.NewFile));
            shownDiff.Should().Contain("+new");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
