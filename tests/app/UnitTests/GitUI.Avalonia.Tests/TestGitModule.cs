using GitExtensions.Extensibility.Git;
using NSubstitute;

namespace GitUI.AvaloniaTests;

/// <summary>
///  A module for the references of the tests: its settings are empty (e.g. no branch tracks a remote branch, as
///  <c>GitRef.MergeWith</c> reads it).
/// </summary>
internal static class TestGitModule
{
    public static IGitModule Instance { get; } = Substitute.For<IGitModule>();
}
