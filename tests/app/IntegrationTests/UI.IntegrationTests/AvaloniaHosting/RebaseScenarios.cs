using CommonTestUtils;
using GitCommands;
using GitExtensions.Extensibility;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Repositories stopped by conflicts in a rebase or while applying patches.</summary>
internal static class RebaseScenarios
{
    /// <summary>Rebases a branch "topic" of three commits on master, which stops at the second one with a conflict.</summary>
    public static void StartConflictingRebase(ReferenceRepository repository)
    {
        string initial = repository.CommitHash!;
        repository.CreateCommit("master: add the conflicting file", "master content", "conflict.txt");
        repository.CreateBranch("topic", initial);
        repository.CheckoutBranch("topic");
        repository.CreateCommit("topic: commit that will apply", "other content", "other.txt");
        repository.CreateCommit("topic: commit that will conflict", "topic content", "conflict.txt");
        repository.CreateCommit("topic: commit to do", "more content", "other.txt");

        ExecutionResult result = repository.Module.GitExecutable.Execute("rebase master", throwOnErrorExit: false);
        result.ExitCode.Should().NotBe(0, "the rebase stops at the conflict");
    }

    /// <summary>Applies two patches of a branch "topic" on master, which stops at the first one with a conflict.</summary>
    public static void StartConflictingAm(ReferenceRepository repository, string patchDirectory)
    {
        string initial = repository.CommitHash!;
        repository.CreateBranch("topic", initial);
        repository.CheckoutBranch("topic");
        repository.CreateCommit("topic: first patch", "patch content", "patched.txt");
        repository.CreateCommit("topic: second patch", "other content", "other.txt");
        Directory.CreateDirectory(patchDirectory);
        repository.Module.GitExecutable.Execute($"format-patch -2 -o {patchDirectory.ToPosixPath().Quote()}");
        repository.CheckoutBranch("master");
        repository.CreateCommit("master: add the conflicting file", "master content", "patched.txt");

        string[] patches = [.. Directory.GetFiles(patchDirectory, "*.patch").Order()];
        patches.Should().HaveCount(2);
        ExecutionResult result = repository.Module.GitExecutable.Execute(
            $"am --3way {string.Join(" ", patches.Select(p => p.ToPosixPath().Quote()))}", throwOnErrorExit: false);
        result.ExitCode.Should().NotBe(0, "the first patch conflicts");
    }
}
