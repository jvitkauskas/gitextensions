using GitCommands.DiffMergeTools;

namespace GitCommandsTests.DiffMergeTools;
public class RegisteredDiffMergeToolsTests
{
    [Test]
    public void All_DiffTools()
    {
        IEnumerable<string> tools = RegisteredDiffMergeTools.All(DiffMergeToolType.Diff);

        // The tools of Windows only are offered there; Araxis on Windows and macOS.
        tools.Should().BeEquivalentTo(OperatingSystem.IsWindows()
            ? ["araxis", "bc", "bc3", "diffmerge", "kdiff3", "meld", "p4merge", "semanticmerge", "smerge", "tortoisediff", "TortoiseGitIDiff", "vscode", "vsdiffmerge", "winmerge"]
            : OperatingSystem.IsMacOS()
                ? ["araxis", "bc", "diffmerge", "kdiff3", "meld", "p4merge", "semanticmerge", "smerge", "vscode"]
                : (string[])["bc", "diffmerge", "kdiff3", "meld", "p4merge", "semanticmerge", "smerge", "vscode"]);
    }

    [Test]
    public void All_MergeTools()
    {
        IEnumerable<string> tools = RegisteredDiffMergeTools.All(DiffMergeToolType.Merge);

        tools.Should().BeEquivalentTo(OperatingSystem.IsWindows()
            ? ["araxis", "bc", "bc3", "diffmerge", "kdiff3", "meld", "p4merge", "semanticmerge", "smerge", "tortoisediff", "tortoisemerge", "vscode", "vsdiffmerge", "winmerge"]
            : OperatingSystem.IsMacOS()
                ? ["araxis", "bc", "diffmerge", "kdiff3", "meld", "p4merge", "semanticmerge", "smerge", "vscode"]
                : (string[])["bc", "diffmerge", "kdiff3", "meld", "p4merge", "semanticmerge", "smerge", "vscode"]);
    }
}
