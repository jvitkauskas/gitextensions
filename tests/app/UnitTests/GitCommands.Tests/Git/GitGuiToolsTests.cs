using GitCommands.Git;

namespace GitCommandsTests.Git;

/// <summary>gitk and git gui off Windows (docs/avalonia-port/CROSS-PLATFORM.md, phase 3).</summary>
[Platform(Exclude = "Win")]
public sealed class GitGuiToolsTests
{
    // A name that is on no PATH, so that only the fake files are found.
    private const string Tool = "git-gui-test-tool";

    [Test]
    public void The_tool_next_to_git_comes_first()
    {
        HashSet<string> files = ["/opt/homebrew/bin/" + Tool, "/usr/lib/git-core/" + Tool];

        GitGuiTools.Find(Tool, "/opt/homebrew/bin/git", () => "/usr/lib/git-core", files.Contains)
            .Should().Be("/opt/homebrew/bin/" + Tool);
    }

    [Test]
    public void Else_the_tool_in_the_exec_path_of_git()
    {
        HashSet<string> files = ["/usr/lib/git-core/" + Tool];

        GitGuiTools.Find(Tool, "/usr/bin/git", () => "/usr/lib/git-core", files.Contains)
            .Should().Be("/usr/lib/git-core/" + Tool);
    }

    [Test]
    public void Else_the_tool_in_the_usual_folders_when_git_is_found_on_the_PATH()
    {
        HashSet<string> files = ["/opt/homebrew/bin/" + Tool];

        GitGuiTools.Find(Tool, "git", () => null, files.Contains).Should().Be("/opt/homebrew/bin/" + Tool);
    }

    [Test]
    public void A_tool_that_is_not_installed_is_not_found()
    {
        GitGuiTools.Find(Tool, "/opt/homebrew/bin/git", () => "/opt/homebrew/opt/git/libexec/git-core", _ => false).Should().BeNull();
    }
}
