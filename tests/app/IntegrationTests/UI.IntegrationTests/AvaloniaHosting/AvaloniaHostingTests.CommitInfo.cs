using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI;
using GitUI.AvaloniaHosting;
using GitUI.Presentation.UserControls;
using GitUIPluginInterfaces;
using Microsoft.VisualStudio.Threading;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 5: the commit info with the WinForms renderers, on a real repository.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void Commit_info_renders_the_header_message_and_containing_branches()
    {
        string commit = _referenceRepository.CreateCommit("Fix the bug\n\nThe details.", "fixed");
        _referenceRepository.CreateBranch("feature", commit);
        GitRevision revision = _referenceRepository.Module.GetRevision(ObjectId.Parse(commit), shortFormat: false, loadRefs: true)!;

        bool showBranches = AppSettings.CommitInfoShowContainedInBranchesLocal;
        AppSettings.CommitInfoShowContainedInBranchesLocal = true;
        try
        {
            CommitInfoHost host = new(_commands, new ResourceManager.LinkFactory());
            CommitInfoContent content = host.Render(revision, children: null, showRevisionsAsLinks: true);
            string message = ThreadHelper.JoinableTaskFactory.Run(() => host.LoadMessageAsync(revision, null, showRevisionsAsLinks: true, CancellationToken.None));
            string revisionInfo = ThreadHelper.JoinableTaskFactory.Run(() => host.LoadRevisionInfoAsync(revision, showBranchesAsLinks: true, new HashSet<string>(), CancellationToken.None));

            content.Header.Select(l => l.Label).Should().Contain(["Author:", "Commit hash:"]);
            content.Header.Single(l => l.Label == "Author:").ValueXhtml.Should().StartWith("<a href='mailto:");
            content.Header.Single(l => l.Label == "Commit hash:").ValueXhtml.Should().Be(commit);
            content.Header.Should().Contain(l => l.ValueXhtml.Contains("gitext://gotocommit/"), "the parent is a link");
            message.Should().Be("Fix the bug\n\nThe details.");
            revisionInfo.Should().Contain("gitext://gotobranch/feature").And.Contain("gitext://gotobranch/master");
        }
        finally
        {
            AppSettings.CommitInfoShowContainedInBranchesLocal = showBranches;
        }
    }
}
