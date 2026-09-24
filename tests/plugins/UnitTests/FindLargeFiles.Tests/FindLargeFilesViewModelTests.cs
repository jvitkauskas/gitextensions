using GitCommands;
using GitExtensions.Plugins.FindLargeFiles;

namespace FindLargeFilesTests;

/// <summary>
///  The Avalonia port of <see cref="FindLargeFilesForm"/> duplicates its batch file generation; these tests fail when the
///  two differ (e.g. after an upstream change of the form).
/// </summary>
public class FindLargeFilesViewModelTests
{
    [Test]
    public void GenerateCommand_is_the_same_as_the_form()
    {
        SortableObjectsList gitObjects =
            [
                new GitObject("sha1", "intune/packages/file.intunewin", 1, "commit") { Delete = true },
                new GitObject("sha1", "intune/packages/file with spaces.intunewin", 1, "commit") { Delete = true },
                new GitObject("sha1", "readme.md", 1, "commit"),
            ];

        // Both read the git command as set up by the test settings, without changing it.
        string expected = FindLargeFilesForm.GetTestAccessor().GenerateCommand(gitObjects);

        FindLargeFilesViewModel.GenerateCommand(AppSettings.GitCommand, gitObjects).Should().Be(expected);
        FindLargeFilesViewModel.GenerateCommand(AppSettings.GitCommand, []).Should().Be(FindLargeFilesForm.GetTestAccessor().GenerateCommand([]));
    }
}
