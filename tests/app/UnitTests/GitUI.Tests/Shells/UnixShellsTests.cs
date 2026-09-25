using GitUI.Shells;

namespace GitUITests.Shells;

/// <summary>The shells of the terminal tab off Windows (docs/avalonia-port/CROSS-PLATFORM.md, phase 3).</summary>
public sealed class UnixShellsTests
{
    [Test]
    public void The_shell_of_the_user_comes_first_then_the_other_installed_shells()
    {
        Dictionary<string, string> onPath = new() { ["bash"] = "/usr/bin/bash", ["zsh"] = "/usr/bin/zsh" };

        IShellDescriptor[] shells = ShellProvider.GetUnixShells("/usr/bin/zsh", onPath.GetValueOrDefault, _ => true);

        shells.Select(shell => (shell.Name, shell.ExecutablePath, shell.ExecutableCommandLine)).Should().Equal(
            ("zsh", "/usr/bin/zsh", "\"/usr/bin/zsh\" -i"),
            ("bash", "/usr/bin/bash", "\"/usr/bin/bash\" -i"));
        shells[0].GetChangeDirCommand("/home/user/my repo").Should().Be("cd \"/home/user/my repo\"");
    }

    [Test]
    public void Without_any_shell_bash_is_offered_without_an_executable()
    {
        IShellDescriptor[] shells = ShellProvider.GetUnixShells(userShell: null, _ => null, _ => false);

        shells.Should().ContainSingle().Which.Name.Should().Be(BashShell.ShellName);
    }
}
