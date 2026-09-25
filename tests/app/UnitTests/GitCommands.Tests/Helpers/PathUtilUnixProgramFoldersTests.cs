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

    [Test]
    public void A_program_of_an_application_bundle_is_found_in_the_Applications_folders_in_order()
    {
        string[] bundlePaths = ["Tool.app/Contents/MacOS/tool", "Tool.app/Contents/Resources/launch"];
        string userBundle = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Applications", bundlePaths[1]);

        PathUtil.FindInMacOSApplications(bundlePaths, _ => true).Should().Be(Path.Join("/Applications", bundlePaths[0]));
        PathUtil.FindInMacOSApplications(bundlePaths, path => path == userBundle).Should().Be(userBundle);
        PathUtil.FindInMacOSApplications(bundlePaths, _ => false).Should().BeNull();
        PathUtil.FindInMacOSApplications([], _ => true).Should().BeNull();
    }
}
