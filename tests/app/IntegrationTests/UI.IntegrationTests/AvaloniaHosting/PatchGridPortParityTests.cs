using CommonTestUtils;
using GitCommands;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitUI;
using GitUI.AvaloniaHosting;
using GitUI.Presentation.UserControls;
using NSubstitute;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>
///  The Avalonia patch grid reads the rebase directory with a copy of the WinForms <see cref="PatchGrid"/> code
///  (<see cref="PatchGridHost"/>), which must list the same commits and patches (docs/avalonia-port/PLAN.md, phase 5).
///  A failure after an upstream merge points at a change to re-port.
/// </summary>
[Apartment(ApartmentState.STA)]
public sealed class PatchGridPortParityTests
{
    private ReferenceRepository _referenceRepository = null!;
    private GitUICommands _commands = null!;

    [SetUp]
    public void SetUp()
    {
        _referenceRepository = new ReferenceRepository();
        _commands = new GitUICommands(GlobalServiceContainer.CreateDefaultMockServiceContainer(), _referenceRepository.Module);
    }

    [TearDown]
    public void TearDown()
    {
        _referenceRepository.Dispose();
    }

    [Test]
    public void Rebase_with_conflicts_lists_the_same_commits()
    {
        RebaseScenarios.StartConflictingRebase(_referenceRepository);

        IReadOnlyList<string> avalonia = Describe(new PatchGridHost(_commands).LoadPatches());

        avalonia.Should().HaveCount(3);
        avalonia.Should().Equal(LoadWinFormsPatches());
    }

    [Test]
    public void Patches_with_conflicts_list_the_same_patches()
    {
        string patchDirectory = Path.Combine(_referenceRepository.Module.WorkingDir, "..", $"patches-{Guid.NewGuid():N}");
        try
        {
            RebaseScenarios.StartConflictingAm(_referenceRepository, patchDirectory);

            IReadOnlyList<string> avalonia = Describe(new PatchGridHost(_commands).LoadPatches());

            avalonia.Should().HaveCount(2);
            avalonia.Should().Equal(LoadWinFormsPatches());
        }
        finally
        {
            Directory.Delete(patchDirectory, recursive: true);
        }
    }

    private IReadOnlyList<string> LoadWinFormsPatches()
    {
        IReadOnlyList<string> patches = [];
        UITest.RunControl(
            createControl: form =>
            {
                IGitUICommandsSource uiCommandsSource = Substitute.For<IGitUICommandsSource>();
                uiCommandsSource.UICommands.Returns(x => _commands);
                return new PatchGrid { Dock = DockStyle.Fill, Parent = form, UICommandsSource = uiCommandsSource, IsManagingRebase = false };
            },
            runTestAsync: patchGrid =>
            {
                patchGrid.Initialize();
                patches = [.. patchGrid.PatchFiles!.Select(p => Describe(p.Action, p.Name, p.FullName, p.ObjectId, p.Author, p.Subject, p.Date, p.IsNext, p.IsApplied, p.Status))];
                return Task.CompletedTask;
            });
        return patches;
    }

    private static IReadOnlyList<string> Describe(IReadOnlyList<PatchItem> patches)
        => [.. patches.Select(p => Describe(p.Action, p.Name, p.FullName, p.ObjectId, p.Author, p.Subject, p.Date, p.IsNext, p.IsApplied, p.Status))];

    private static string Describe(string? action, string? name, string? fullName, ObjectId objectId, string? author, string? subject, string? date, bool isNext, bool isApplied, string status)
        => $"{action}|{name}|{fullName}|{objectId}|{author}|{subject}|{date}|next={isNext}|applied={isApplied}|{status}";
}

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
