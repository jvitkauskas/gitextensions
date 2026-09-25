using GitUI.CommandsDialogs.SettingsDialog;

namespace GitUITests.CommandsDialogs.SettingsDialog;

/// <summary>Where git is looked for off Windows (docs/avalonia-port/CROSS-PLATFORM.md, phase 3).</summary>
public sealed class CheckSettingsLogicTests
{
    [TestCase(true)]
    [TestCase(false)]
    public void Repair_only_registers_nonportable_installations(bool isPortable)
    {
        string directory = Path.GetTempPath();
        List<string> writes = [];

        CheckSettingsLogic.SolveGitExtensionsDir(directory, isPortable, writes.Add).Should().BeTrue();

        writes.Should().Equal(isPortable ? [] : new[] { directory });
    }

    [Test]
    public void Repair_does_not_register_a_missing_installation()
    {
        List<string> writes = [];

        CheckSettingsLogic.SolveGitExtensionsDir(null, isPortable: false, writes.Add).Should().BeFalse();

        writes.Should().BeEmpty();
    }

    [Test]
    public void Off_Windows_git_is_the_chosen_path_then_the_configured_one_then_the_PATH_then_the_usual_folders()
    {
        HashSet<string> files = ["/home/user/bin/git", "/opt/git/bin/git", Path.Join("/usr/bin", "git"), Path.Join("/opt/homebrew/bin", "git")];

        CheckSettingsLogic.GetUnixGitCandidates("/home/user/bin/git", "/opt/git/bin/git", files.Contains)
            .Should().Equal("/home/user/bin/git", "/opt/git/bin/git", "git", Path.Join("/usr/bin", "git"), Path.Join("/opt/homebrew/bin", "git"));
    }

    [Test]
    public void Off_Windows_a_missing_or_relative_configured_git_is_not_tried_first()
    {
        CheckSettingsLogic.GetUnixGitCandidates(possibleNewPath: null, "git", _ => false).Should().Equal("git");
        CheckSettingsLogic.GetUnixGitCandidates("/gone/git", "/also/gone/git", _ => false).Should().Equal("git");
    }
}
