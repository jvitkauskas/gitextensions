using GitExtensions.Plugins.FindLargeFiles;

namespace FindLargeFilesTests;

/// <summary>The batch file that removes the large files from the history (as <c>FindLargeFilesForm</c> generated it).</summary>
public class FindLargeFilesCommandTests
{
    private const string GitCommand = @"C:\Program Files\Git\bin\git.exe";

    [Test]
    public async Task GenerateCommand_without_deletions_should_return_expected()
    {
        await Verifier.Verify(FindLargeFilesViewModel.GenerateBatchFile(GitCommand, []));
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

        await Verifier.Verify(FindLargeFilesViewModel.GenerateBatchFile(GitCommand, gitObjects));
    }

    [Test]
    public void GenerateShellScript_runs_the_same_commands_with_sh()
    {
        SortableObjectsList gitObjects =
            [
                new GitObject("sha1", "packages/file with spaces.bin", 1, "commit") { Delete = true },
                new GitObject("sha1", "readme.md", 1, "commit"),
            ];

        FindLargeFilesViewModel.GenerateShellScript("/usr/bin/git", gitObjects).Should().Be(
            "gitexe='/usr/bin/git'\n"
            + "\"$gitexe\" filter-branch --index-filter \"git rm -r -f --cached --ignore-unmatch 'packages/file with spaces.bin'\" --prune-empty -- --all\n"
            + "\"$gitexe\" for-each-ref --format='%(refname)' refs/original/ | while read -r ref; do \"$gitexe\" update-ref -d \"$ref\"; done\n"
            + "\"$gitexe\" reflog expire --expire=now --all\n"
            + "\"$gitexe\" gc --aggressive --prune=now\n");
    }

    [Test]
    public void GenerateShellScript_quotes_the_git_command()
    {
        FindLargeFilesViewModel.GenerateShellScript("/opt/it's/git", []).Should().StartWith("gitexe='/opt/it'\\''s/git'\n");
    }
}
