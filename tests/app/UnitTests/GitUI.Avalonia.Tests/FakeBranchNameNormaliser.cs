using GitCommands.Git;

namespace GitUI.AvaloniaTests;

/// <summary>Replaces spaces with the replacement token; enough to test that view models normalise names.</summary>
internal sealed class FakeBranchNameNormaliser : IGitBranchNameNormaliser
{
    public string Normalise(string? branchName, GitBranchNameOptions options)
        => (branchName ?? "").Replace(" ", options.ReplacementToken);
}
