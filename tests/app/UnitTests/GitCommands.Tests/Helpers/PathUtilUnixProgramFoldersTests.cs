using GitCommands;

namespace GitCommandsTests.Helpers;

/// <summary>Where programs are looked for off Windows (docs/avalonia-port/CROSS-PLATFORM.md, phase 3).</summary>
public sealed class PathUtilUnixProgramFoldersTests
{
    private const string Program = "no-such-program-of-git-extensions";

    [Test]
    public void A_program_not_on_the_PATH_is_found_in_the_folders_of_the_programs()
    {
        string snap = Path.Join("/snap/bin", Program);

        PathUtil.FindInUnixProgramFolders(Program, path => path == snap).Should().Be(snap);
    }

    [Test]
    public void The_folders_are_searched_in_order()
    {
        PathUtil.FindInUnixProgramFolders(Program, _ => true).Should().Be(Path.Join("/usr/local/bin", Program));
        PathUtil.FindInUnixProgramFolders(Program, _ => false).Should().BeNull();
    }
}
