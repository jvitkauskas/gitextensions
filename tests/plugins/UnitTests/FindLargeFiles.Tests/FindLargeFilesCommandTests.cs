using GitExtensions.Plugins.FindLargeFiles;

namespace FindLargeFilesTests;

/// <summary>The batch file that removes the large files from the history (as <c>FindLargeFilesForm</c> generated it).</summary>
public class FindLargeFilesCommandTests
{
    private const string GitCommand = @"C:\Program Files\Git\bin\git.exe";

    [Test]
    public async Task GenerateCommand_without_deletions_should_return_expected()
    {
        await Verifier.Verify(FindLargeFilesViewModel.GenerateCommand(GitCommand, []));
    }

    [Test]
    public async Task GenerateCommand_with_deletions_should_return_expected()
    {
        SortableObjectsList gitObjects =
            [
                new GitObject("sha1", "intune/packages/file.intunewin", 1, "commit") { Delete = true },
                new GitObject("sha1", "intune/packages/file with spaces.intunewin", 1, "commit") { Delete = true },
                new GitObject("sha1", "intune/packages/XL Upload/xl-upload.intunewin", 1, "commit") { Delete = true },
                new GitObject("sha1", "readme.md", 1, "commit"),
            ];

        await Verifier.Verify(FindLargeFilesViewModel.GenerateCommand(GitCommand, gitObjects));
    }
}
